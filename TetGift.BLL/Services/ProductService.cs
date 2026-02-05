using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class ProductService(IUnitOfWork uow, IInventoryService inventoryService) : IProductService
{
    private readonly IUnitOfWork _uow = uow;

    public async Task CreateNormalAsync(CreateSingleProductRequest dto)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.Sku)
            || string.IsNullOrWhiteSpace(dto.Productname)
            || dto.Price <= 0
            || dto.Unit <= 0)
        {
            throw new Exception("Thiếu thông tin bắt buộc cho sản phẩm thường (SKU, Name, Price, Unit).");
        }

        // Note: Accountid will be set from authenticated user context in controller
        await ValidateForeignKeys(dto.Accountid, null, dto.Categoryid);

        // Create Product entity
        var entity = MapToEntity(dto);
        await _uow.GetRepository<Product>().AddAsync(entity);
        await _uow.SaveAsync();
    }

    public async Task CreateCustomAsync(CreateComboProductRequest dto)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.Productname))
        {
            throw new Exception("Tên sản phẩm không được để trống.");
        }

        if (dto.ProductDetails == null || !dto.ProductDetails.Any())
        {
            throw new Exception("Giỏ quà phải có ít nhất 1 sản phẩm.");
        }

        // Validate foreign keys (Config and Account)
        await ValidateForeignKeys(dto.Accountid, dto.Configid, null);

        // Load ProductConfig with ConfigDetails to validate category requirements
        var config = await _uow.GetRepository<ProductConfig>().FindAsync(
            c => c.Configid == dto.Configid && c.Isdeleted == false,
            include: q => q.Include(c => c.ConfigDetails)
                .ThenInclude(cd => cd.Category)
        );
        
        if (config == null)
            throw new Exception($"Không tìm thấy cấu hình giỏ quà với ID {dto.Configid}.");

        // Validate all ProductIds in ProductDetails exist and match config requirements
        decimal totalWeight = 0;
        var validCategoryIds = config.ConfigDetails?.Select(cd => cd.Categoryid).ToHashSet() ?? new HashSet<int?>();
        
        foreach (var detail in dto.ProductDetails)
        {
            if (!detail.Productid.HasValue)
                throw new Exception("ProductId không được để trống trong ProductDetails.");
            
            var product = await _uow.GetRepository<Product>().FindAsync(
                p => p.Productid == detail.Productid.Value && !p.Status.Equals(ProductStatus.DELETED),
                include: p => p.Include(pr => pr.Category)
            );
            
            if (product == null)
                throw new Exception($"Sản phẩm với ID {detail.Productid} không tồn tại hoặc đã bị xóa.");
            
            // Validate product category belongs to config's allowed categories
            if (validCategoryIds.Any())
            {
                if (!product.Categoryid.HasValue || !validCategoryIds.Contains(product.Categoryid))
                {
                    var allowedCategories = string.Join(", ", 
                        config.ConfigDetails?.Select(cd => cd.Category?.Categoryname ?? "N/A") ?? new List<string>());
                    throw new Exception(
                        $"Sản phẩm '{product.Productname}' thuộc danh mục không hợp lệ. " +
                        $"Danh mục cho phép: {allowedCategories}");
                }
            }
            
            // Calculate total weight
            var quantity = detail.Quantity ?? 1;
            var productWeight = product.Unit ?? 0;
            totalWeight += productWeight * quantity;
        }

        // Validate total weight doesn't exceed config limit
        if (config.Totalunit.HasValue && totalWeight > config.Totalunit.Value)
        {
            throw new Exception(
                $"Tổng trọng lượng giỏ quà ({totalWeight}g) vượt quá giới hạn cho phép ({config.Totalunit.Value}g). " +
                $"Vui lòng giảm số lượng sản phẩm.");
        }

        // Create Product entity (combo/basket)
        var entity = new Product
        {
            Configid = dto.Configid,
            Accountid = dto.Accountid,
            Productname = dto.Productname,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            Status = dto.Status ?? ProductStatus.DRAFT,
            Unit = 0,  // Will be calculated from ProductDetails
            Price = 0  // Will be calculated from ProductDetails
        };

        await _uow.GetRepository<Product>().AddAsync(entity);
        await _uow.SaveAsync();

        // Create ProductDetails
        if (dto.ProductDetails.Any())
        {
            var detailRepo = _uow.GetRepository<ProductDetail>();
            var productDetails = dto.ProductDetails.Select(d => new ProductDetail
            {
                Productparentid = entity.Productid,
                Productid = d.Productid,
                Quantity = d.Quantity ?? 1
            }).ToList();

            await detailRepo.AddRangeAsync(productDetails);
            await _uow.SaveAsync();
            
            // Recalculate Unit and Price after adding details
            var createdProduct = await _uow.GetRepository<Product>().FindAsync(
                p => p.Productid == entity.Productid,
                include: q => q.Include(p => p.ProductDetailProductparents)
                    .ThenInclude(pd => pd.Product)
            );
            
            if (createdProduct != null)
            {
                createdProduct.CalculateUnit();
                createdProduct.CalculateTotalPrice();
                _uow.GetRepository<Product>().Update(createdProduct);
                await _uow.SaveAsync();
            }
        }
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products = await _uow.GetRepository<Product>().GetAllAsync(
            p => (p.Account == null || !p.Account.Role.Equals(UserRole.CUSTOMER))
                && !p.Status.Equals(ProductStatus.DELETED)
                && !p.Status.Equals(ProductStatus.DRAFT)
                && (p.Status.Equals(ProductStatus.ACTIVE) || p.Status.Equals(ProductStatus.INACTIVE)),
            include: p => p.Include(p => p.Stocks)
                .Include(p => p.ProductDetailProductparents).ThenInclude(pd => pd.Product)
            );
        return products.Select(p =>
        {
            p.CalculateUnit();
            p.CalculateTotalPrice();

            return new ProductDto()
            {
                Productid = p.Productid,
                Categoryid = p.Categoryid,
                Configid = p.Configid,
                Accountid = p.Accountid,
                Sku = p.Sku,
                Productname = p.Productname,
                Description = p.Description,
                Price = p.Price,
                TotalQuantity = p.Stocks?.Sum(s => s.Stockquantity) ?? 0,
                Stocks = p.Stocks?.Select(s => new StockDto
                {
                    StockId = s.Stockid,
                    ProductId = s.Productid ?? 0,
                    ProductName = p.Productname ?? string.Empty,
                    Quantity = s.Stockquantity ?? 0,
                    ExpiryDate = s.Expirydate.HasValue ? s.Expirydate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    Status = s.Status ?? string.Empty,
                    ProductionDate = s.Productiondate.HasValue ? s.Productiondate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    LastUpdated = s.Lastupdated
                }).ToList(),
                Status = p.Status,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl
            };
        });
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _uow.GetRepository<Product>().FindAsync(
            p => p.Productid == id && !p.Status.Equals(ProductStatus.DELETED),
            include: p => p.Include(p => p.Stocks)
                .Include(p => p.ProductDetailProductparents).ThenInclude(pd => pd.Product).ThenInclude(s => s.Stocks)
            );
        if (product == null) return null;
        product.CalculateUnit();
        product.CalculateTotalPrice();

        return new ProductDto
        {
            Productid = product.Productid,
            Categoryid = product.Categoryid,
            Configid = product.Configid,
            Accountid = product.Accountid,
            Sku = product.Sku,
            Productname = product.Productname,
            Description = product.Description,
            Price = product.Price,
            TotalQuantity = product.Stocks?.Sum(s => s.Stockquantity) ?? 0,
            Stocks = product.Stocks?.Select(s => new StockDto
            {
                StockId = s.Stockid,
                ProductId = s.Productid ?? 0,
                ProductName = product.Productname ?? string.Empty,
                Quantity = s.Stockquantity ?? 0,
                ExpiryDate = s.Expirydate.HasValue ? s.Expirydate.Value.ToDateTime(TimeOnly.MinValue) : null,
                Status = s.Status ?? string.Empty,
                ProductionDate = s.Productiondate.HasValue ? s.Productiondate.Value.ToDateTime(TimeOnly.MinValue) : null,
                LastUpdated = s.Lastupdated
            }).ToList(),
            Status = product.Status,
            Unit = product.Unit,
            ImageUrl = product.ImageUrl
        };
    }

    public async Task<IEnumerable<ProductDto>> GetByAccountIdAsync(int accountId)
    {
        var products = await _uow.GetRepository<Product>()
            .GetAllAsync(p => p.Accountid == accountId && !p.Status.Equals(ProductStatus.DELETED),
            include: p => p.Include(p => p.Stocks)
                .Include(p => p.ProductDetailProductparents).ThenInclude(pd => pd.Product)
            );

        return products.Select(p =>
        {
            p.CalculateUnit();
            p.CalculateTotalPrice();
            return new ProductDto
            {
                Productid = p.Productid,
                Categoryid = p.Categoryid,
                Configid = p.Configid,
                Accountid = p.Accountid,
                Sku = p.Sku,
                Productname = p.Productname,
                Description = p.Description,
                Price = p.Price,
                TotalQuantity = p.Stocks?.Sum(s => s.Stockquantity) ?? 0,
                Stocks = p.Stocks?.Select(s => new StockDto
                {
                    StockId = s.Stockid,
                    ProductId = s.Productid ?? 0,
                    ProductName = p.Productname ?? string.Empty,
                    Quantity = s.Stockquantity ?? 0,
                    ExpiryDate = s.Expirydate.HasValue ? s.Expirydate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    Status = s.Status ?? string.Empty,
                    ProductionDate = s.Productiondate.HasValue ? s.Productiondate.Value.ToDateTime(TimeOnly.MinValue) : null,
                    LastUpdated = s.Lastupdated
                }).ToList(),
                Status = p.Status,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl
            };
        });
    }

    /// <summary>
    /// Get customer's custom baskets (giỏ quà tự tạo)
    /// Returns parent product (basket) with child products details
    /// </summary>
    public async Task<IEnumerable<CustomerBasketDto>> GetCustomerBasketsByAccountIdAsync(int accountId)
    {
        var products = await _uow.GetRepository<Product>()
            .GetAllAsync(
                p => p.Accountid == accountId 
                    && p.Configid.HasValue  // Only custom baskets
                    && !p.Status.Equals(ProductStatus.DELETED)
                    && !p.Status.Equals(ProductStatus.TEMPLATE),
                include: p => p
                    .Include(p => p.Config)
                    .Include(p => p.ProductDetailProductparents)
                        .ThenInclude(pd => pd.Product)
                        .ThenInclude(prod => prod.Stocks)
            );

        return products.Select(basket =>
        {
            basket.CalculateUnit();
            basket.CalculateTotalPrice();

            return new CustomerBasketDto
            {
                Productid = basket.Productid,
                Configid = basket.Configid,
                ConfigName = basket.Config?.Configname,
                Productname = basket.Productname,
                Description = basket.Description,
                ImageUrl = basket.ImageUrl,
                Status = basket.Status,
                TotalPrice = basket.Price ?? 0,
                TotalWeight = basket.Unit ?? 0,
                ProductDetails = basket.ProductDetailProductparents.Select(pd => new BasketProductDetailDto
                {
                    Productdetailid = pd.Productdetailid,
                    Productid = pd.Productid ?? 0,
                    Productname = pd.Product?.Productname ?? "Unknown",
                    Sku = pd.Product?.Sku,
                    Price = pd.Product?.Price ?? 0,
                    Unit = pd.Product?.Unit ?? 0,
                    Quantity = pd.Quantity ?? 1,
                    ImageUrl = pd.Product?.ImageUrl,
                    TotalQuantityInStock = pd.Product?.Stocks?.Sum(s => s.Stockquantity) ?? 0,
                    Subtotal = (pd.Product?.Price ?? 0) * (pd.Quantity ?? 1)
                }).ToList()
            };
        });
    }

    private Product MapToEntity(CreateSingleProductRequest dto)
    {
        return new Product
        {
            Categoryid = dto.Categoryid,
            Accountid = dto.Accountid,
            Sku = dto.Sku,
            Productname = dto.Productname,
            Description = dto.Description,
            Price = dto.Price,
            Status = ProductStatus.ACTIVE,
            Unit = dto.Unit,
            ImageUrl = dto.ImageUrl
        };
    }

    public async Task DeleteAsync(int id)
    {
        var repo = _uow.GetRepository<Product>();
        var entity = await repo.GetByIdAsync(id);
        if (entity != null)
        {
            entity.Status = ProductStatus.DELETED;
            repo.Update(entity);
            await _uow.SaveAsync();
        }
    }

    private async Task ValidateForeignKeys(int? accountId, int? configId, int? categoryId)
    {
        if (accountId.HasValue)
        {
            var account = await _uow.GetRepository<Account>().FindAsync(
                a => a.Accountid == accountId.Value && a.Status.Equals(ProductStatus.ACTIVE)
                );
            if (!account.Any()) throw new Exception($"AccountId {accountId} không tồn tại.");
        }

        if (configId.HasValue)
        {
            var config = await _uow.GetRepository<ProductConfig>().FindAsync(
                c => c.Configid == configId.Value && c.Isdeleted == false
                );
            if (!config.Any()) throw new Exception($"ConfigId {configId} không tồn tại.");
        }

        if (categoryId.HasValue)
        {
            var category = await _uow.GetRepository<ProductCategory>().FindAsync(
                c => c.Categoryid == categoryId.Value && c.Isdeleted == false
                );
            if (!category.Any()) throw new Exception($"CategoryId {categoryId} không tồn tại.");
        }
    }

    public async Task<UpdateProductDto> UpdateNormalAsync(ProductDto dto)
    {
        var repo = _uow.GetRepository<Product>();
        var result = await repo.FindAsync(
            e => e.Productid == dto.Productid && !e.Status.Equals(ProductStatus.DELETED)
            );
        if (!result.Any()) throw new Exception("Không tìm thấy sản phẩm.");
        var entity = result.First();

        #region Validation Cate, Blank String, Number Property

        // 1. Validate CategoryId
        if (dto.Categoryid.HasValue)
        {
            await ValidateForeignKeys(null, null, dto.Categoryid);
            entity.Categoryid = dto.Categoryid;
        }

        // 2. Validate blank string 
        if (dto.Sku != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Sku)) throw new Exception("SKU không được để trống.");
            entity.Sku = dto.Sku;
        }
        if (dto.Productname != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Productname)) throw new Exception("Tên không được để trống.");
            entity.Productname = dto.Productname;
        }
        if (dto.Status != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Status)
                || (!dto.Status.Equals(ProductStatus.ACTIVE) && !dto.Status.Equals(ProductStatus.INACTIVE)
                && !dto.Status.Equals("OUT_OF_STOCK")
                )) throw new Exception("Trạng thái không được để trống hoặc sai syntax.");
            entity.Status = dto.Status;
        }

        // 3. Validate số dương
        if (dto.Price.HasValue)
        {
            if (dto.Price <= 0) throw new Exception("Giá phải lớn hơn 0.");
            entity.Price = dto.Price;
        }
        if (dto.Unit.HasValue)
        {
            if (dto.Unit <= 0) throw new Exception("Đơn vị phải lớn hơn 0.");
            entity.Unit = dto.Unit;
        }

        #endregion

        repo.Update(entity);
        await _uow.SaveAsync();
        return new UpdateProductDto();
    }

    /// <summary>
    /// Update custom basket/combo product (customer's custom gift basket)
    /// </summary>
    public async Task<UpdateProductDto> UpdateCustomAsync(int productId, UpdateComboProductRequest dto, int? requestingAccountId)
    {
        var repo = _uow.GetRepository<Product>();
        var userRepo = _uow.GetRepository<Account>();
        
        // Check if requesting user is customer
        var requestingUser = await userRepo.FindAsync(
            a => a.Accountid == requestingAccountId
        );
        bool isCustomer = requestingUser.Any() && requestingUser.First().Role.Equals(UserRole.CUSTOMER);

        // Build query with additional filter for customer
        var productQuery = repo.FindAsync(
            p => p.Productid == productId 
                && p.Configid.HasValue  // Must be custom basket
                && !p.Status.Equals(ProductStatus.DELETED)
                && (!isCustomer || p.Accountid == requestingAccountId),  // Customer can only see their own baskets
            include: q => q.Include(p => p.Config)
                    .ThenInclude(c => c.ConfigDetails)
                    .ThenInclude(cd => cd.Category)
                .Include(p => p.Account)
                .Include(p => p.ProductDetailProductparents)
        );

        var product = await productQuery;

        if (product == null) 
        {
            if (isCustomer)
                throw new Exception("Không tìm thấy giỏ quà của bạn hoặc bạn không có quyền chỉnh sửa giỏ quà này.");
            else
                throw new Exception("Không tìm thấy giỏ quà hoặc sản phẩm không phải giỏ quà tùy chỉnh.");
        }

        var response = new UpdateProductDto();

        // Check if basket belongs to a customer
        bool basketBelongsToCustomer = product.Account != null && product.Account.Role.Equals(UserRole.CUSTOMER);

        // Admin cannot update customer's custom baskets
        if (!isCustomer && basketBelongsToCustomer)
        {
            throw new Exception("Admin không thể chỉnh sửa giỏ quà tự tạo của khách hàng. Chỉ khách hàng mới có quyền chỉnh sửa giỏ quà của mình.");
        }

        // Additional validation for customer-owned baskets
        if (isCustomer)
        {
            // Customer can only update their own DRAFT or ACTIVE baskets
            if (product.Status == ProductStatus.TEMPLATE)
                throw new Exception("Không thể chỉnh sửa template giỏ quà. Vui lòng clone template trước.");
            
            // Customer cannot change basket to certain statuses
            if (dto.Status != null && !new[] { ProductStatus.DRAFT, ProductStatus.ACTIVE }.Contains(dto.Status))
                throw new Exception("Khách hàng chỉ có thể đặt trạng thái DRAFT hoặc ACTIVE.");
        }

        // Update basic info
        if (!string.IsNullOrWhiteSpace(dto.Productname))
            product.Productname = dto.Productname;
        
        if (dto.Description != null)
            product.Description = dto.Description;
        
        if (dto.ImageUrl != null)
            product.ImageUrl = dto.ImageUrl;

        // Validate and update Status
        if (dto.Status != null)
        {
            var validStatuses = isCustomer 
                ? new[] { ProductStatus.DRAFT, ProductStatus.ACTIVE }
                : new[] { ProductStatus.DRAFT, ProductStatus.ACTIVE, ProductStatus.INACTIVE };
            
            if (!validStatuses.Contains(dto.Status))
                throw new Exception($"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validStatuses)}");
            
            product.Status = dto.Status;
        }

        // Update ProductDetails if provided
        if (dto.ProductDetails != null)
        {
            // Load config details to validate categories
            var validCategoryIds = product.Config?.ConfigDetails?.Select(cd => cd.Categoryid).ToHashSet() ?? new HashSet<int?>();
            decimal totalWeight = 0;
            
            // Validate all ProductIds exist and are available
            foreach (var detail in dto.ProductDetails)
            {
                if (!detail.Productid.HasValue)
                    throw new Exception("ProductId không được để trống trong ProductDetails.");
                
                var childProduct = await _uow.GetRepository<Product>().FindAsync(
                    p => p.Productid == detail.Productid.Value 
                        && p.Status == ProductStatus.ACTIVE,
                    include: p => p.Include(pr => pr.Category)
                );
                
                if (childProduct == null)
                    throw new Exception($"Sản phẩm với ID {detail.Productid} không tồn tại hoặc không khả dụng.");
                
                // Validate product category belongs to config's allowed categories
                if (product.Config != null && validCategoryIds.Any())
                {
                    if (!childProduct.Categoryid.HasValue || !validCategoryIds.Contains(childProduct.Categoryid))
                    {
                        var allowedCategories = string.Join(", ", 
                            product.Config.ConfigDetails?.Select(cd => cd.Category?.Categoryname ?? "N/A") ?? new List<string>());
                        throw new Exception(
                            $"Sản phẩm '{childProduct.Productname}' thuộc danh mục không hợp lệ. " +
                            $"Danh mục cho phép: {allowedCategories}");
                    }
                }
                
                // Calculate total weight
                var quantity = detail.Quantity ?? 1;
                var productWeight = childProduct.Unit ?? 0;
                totalWeight += productWeight * quantity;
                
                // For customer: validate product has sufficient stock if quantity is high
                if (isCustomer && quantity > 10)
                {
                    var stocks = await _uow.GetRepository<Stock>().GetAllAsync(
                        s => s.Productid == detail.Productid && s.Status == StockStatus.ACTIVE
                    );
                    var totalStock = stocks.Sum(s => s.Stockquantity) ?? 0;
                    
                    if (totalStock == 0)
                        throw new Exception($"Sản phẩm '{childProduct.Productname}' hiện đang hết hàng.");
                }
            }

            // Validate total weight doesn't exceed config limit
            if (product.Config?.Totalunit.HasValue == true && totalWeight > product.Config.Totalunit.Value)
            {
                throw new Exception(
                    $"Tổng trọng lượng giỏ quà ({totalWeight}g) vượt quá giới hạn cho phép ({product.Config.Totalunit.Value}g). " +
                    $"Vui lòng giảm số lượng sản phẩm.");
            }

            // Remove old ProductDetails
            var detailRepo = _uow.GetRepository<ProductDetail>();
            var oldDetails = product.ProductDetailProductparents.ToList();
            foreach (var oldDetail in oldDetails)
            {
                await detailRepo.DeleteAsync(oldDetail);
            }

            // Add new ProductDetails
            var newDetails = dto.ProductDetails.Select(d => new ProductDetail
            {
                Productparentid = product.Productid,
                Productid = d.Productid,
                Quantity = d.Quantity ?? 1
            }).ToList();

            await detailRepo.AddRangeAsync(newDetails);
        }

        repo.Update(product);
        await _uow.SaveAsync();

        // Recalculate Unit and Price after ProductDetails change
        if (dto.ProductDetails != null)
        {
            // Reload with updated details
            var updatedProduct = await repo.FindAsync(
                p => p.Productid == productId,
                include: q => q.Include(p => p.Config)
                    .Include(p => p.ProductDetailProductparents)
                    .ThenInclude(pd => pd.Product)
            );
            
            if (updatedProduct != null)
            {
                updatedProduct.CalculateUnit();
                updatedProduct.CalculateTotalPrice();
                
                // Check config limits
                if (updatedProduct.Config?.Totalunit.HasValue == true 
                    && updatedProduct.Unit > updatedProduct.Config.Totalunit.Value)
                {
                    response.Warning = $"Cảnh báo: Trọng lượng giỏ quà ({updatedProduct.Unit}) vượt quá giới hạn cấu hình ({updatedProduct.Config.Totalunit.Value}).";
                }
                
                repo.Update(updatedProduct);
                await _uow.SaveAsync();
            }
        }

        return response;
    }

    public async Task<ProductValidationDto> GetProductValidationStatus(int productId)
    {
        var repo = _uow.GetRepository<Product>();
        var product = await repo.FindAsync(
            p => p.Productid == productId && !p.Status.Equals(ProductStatus.DELETED),
            include: q => q
                .Include(p => p.Config)
                    .ThenInclude(c => c.ConfigDetails)
                    .ThenInclude(cd => cd.Category)
                .Include(p => p.ProductDetailProductparents)
                    .ThenInclude(pd => pd.Product)
                    .ThenInclude(p => p.Category)
        );

        if (product == null)
            throw new Exception("Không tìm thấy sản phẩm.");

        // Recalculate to ensure up-to-date values
        product.CalculateUnit();
        product.CalculateTotalPrice();

        var result = new ProductValidationDto
        {
            Productid = product.Productid,
            Productname = product.Productname,
            CurrentWeight = product.Unit,
            MaxWeight = product.Config?.Totalunit,
            WeightExceeded = product.Config?.Totalunit != null && product.Unit > product.Config.Totalunit
        };

        // Get warnings
        result.Warnings = product.GetConfigValidationWarnings();

        // Get category status
        var configStatus = product.GetConfigDetailStatus();
        foreach (var kvp in configStatus)
        {
            var configDetail = product.Config?.ConfigDetails.FirstOrDefault(cd => cd.Categoryid == kvp.Key);
            var categoryName = configDetail?.Category?.Categoryname ?? $"Category {kvp.Key}";

            result.CategoryStatus[categoryName] = new CategoryRequirementDto
            {
                CategoryId = kvp.Key,
                CategoryName = categoryName,
                CurrentCount = kvp.Value.Current,
                RequiredCount = kvp.Value.Required
            };
        }

        // Determine if valid (all requirements met and weight not exceeded)
        result.IsValid = !result.WeightExceeded
            && result.CategoryStatus.Values.All(cs => cs.IsSatisfied);

        return result;
    }

    public async Task<IEnumerable<ProductDto>> GetTemplatesAsync()
    {
        var templates = await _uow.GetRepository<Product>().GetAllAsync(
            p => p.Status == ProductStatus.TEMPLATE || p.Configid.HasValue,
            include: p => p
                .Include(p => p.Config)
                .Include(p => p.Account)
                .Include(p => p.Stocks)
                .Include(p => p.ProductDetailProductparents)
                    .ThenInclude(pd => pd.Product)
                    .ThenInclude(prod => prod.Stocks)
        );

        return templates.Select(p =>
        {
            p.CalculateUnit();
            p.CalculateTotalPrice();

            return new ProductDto
            {
                Productid = p.Productid,
                Categoryid = p.Categoryid,
                Configid = p.Configid,
                Accountid = p.Accountid,
                Sku = p.Sku,
                Productname = p.Productname,
                Description = p.Description,
                Price = p.Price,
                TotalQuantity = p.Stocks?.Sum(s => s.Stockquantity) ?? 0,
                Status = p.Status,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl,
                IsCustom = true,
                ProductDetails = p.ProductDetailProductparents?.Select(pd => new ProductDetailResponse
                {
                    Productdetailid = pd.Productdetailid,
                    Productid = pd.Productid,
                    Quantity = pd.Quantity,
                    ChildProduct = pd.Product != null ? new ProductDto
                    {
                        Productid = pd.Product.Productid,
                        Categoryid = pd.Product.Categoryid,
                        Sku = pd.Product.Sku,
                        Productname = pd.Product.Productname,
                        Description = pd.Product.Description,
                        Price = pd.Product.Price,
                        TotalQuantity = pd.Product.Stocks?.Sum(s => s.Stockquantity) ?? 0,
                        Status = pd.Product.Status,
                        Unit = pd.Product.Unit,
                        ImageUrl = pd.Product.ImageUrl
                    } : null
                }).ToList()
            };
        }).ToList();
    }

    /// <summary>
    /// Get custom product by ID with full details (for editing)
    /// Returns product with ProductDetails and nested child products
    /// </summary>
    public async Task<ProductDto?> GetCustomProductByIdAsync(int productId)
    {
        var product = await _uow.GetRepository<Product>().FindAsync(
            p => p.Productid == productId
                && p.Configid.HasValue
                && !p.Status.Equals(ProductStatus.DELETED),
            include: p => p
                .Include(p => p.Config)
                .Include(p => p.Account)
                .Include(p => p.Stocks)
                .Include(p => p.ProductDetailProductparents)
                    .ThenInclude(pd => pd.Product)
                    .ThenInclude(prod => prod.Stocks)
        );

        if (product == null) return null;

        product.CalculateUnit();
        product.CalculateTotalPrice();

        return new ProductDto
        {
            Productid = product.Productid,
            Categoryid = product.Categoryid,
            Configid = product.Configid,
            Accountid = product.Accountid,
            Sku = product.Sku,
            Productname = product.Productname,
            Description = product.Description,
            Price = product.Price,
            TotalQuantity = product.Stocks?.Sum(s => s.Stockquantity) ?? 0,
            Status = product.Status,
            Unit = product.Unit,
            ImageUrl = product.ImageUrl,
            IsCustom = true,
            ProductDetails = product.ProductDetailProductparents?.Select(pd => new ProductDetailResponse
            {
                Productdetailid = pd.Productdetailid,
                Productid = pd.Productid,
                Quantity = pd.Quantity,
                ChildProduct = pd.Product != null ? new ProductDto
                {
                    Productid = pd.Product.Productid,
                    Categoryid = pd.Product.Categoryid,
                    Sku = pd.Product.Sku,
                    Productname = pd.Product.Productname,
                    Description = pd.Product.Description,
                    Price = pd.Product.Price,
                    TotalQuantity = pd.Product.Stocks?.Sum(s => s.Stockquantity) ?? 0,
                    Status = pd.Product.Status,
                    Unit = pd.Product.Unit,
                    ImageUrl = pd.Product.ImageUrl
                } : null
            }).ToList()
        };
    }

    /// <summary>
    /// Get all baskets (products with ConfigId) created by admin/staff
    /// Excludes customer-created baskets
    /// </summary>
    public async Task<IEnumerable<ProductDto>> GetAdminBasketsAsync()
    {
        var baskets = await _uow.GetRepository<Product>().GetAllAsync(
            p => p.Configid.HasValue
                && (p.Account == null || !p.Account.Role.Equals(UserRole.CUSTOMER))
                && !p.Status.Equals(ProductStatus.DELETED),
            include: p => p
                .Include(p => p.Config)
                .Include(p => p.Account)
                .Include(p => p.Stocks)
                .Include(p => p.ProductDetailProductparents)
                    .ThenInclude(pd => pd.Product)
                    .ThenInclude(prod => prod.Stocks)
        );

        return baskets.Select(p =>
        {
            p.CalculateUnit();
            p.CalculateTotalPrice();

            return new ProductDto
            {
                Productid = p.Productid,
                Categoryid = p.Categoryid,
                Configid = p.Configid,
                Accountid = p.Accountid,
                Sku = p.Sku,
                Productname = p.Productname,
                Description = p.Description,
                Price = p.Price,
                TotalQuantity = p.Stocks?.Sum(s => s.Stockquantity) ?? 0,
                Status = p.Status,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl,
                IsCustom = true,
                ProductDetails = p.ProductDetailProductparents?.Select(pd => new ProductDetailResponse
                {
                    Productdetailid = pd.Productdetailid,
                    Productid = pd.Productid,
                    Quantity = pd.Quantity,
                    ChildProduct = pd.Product != null ? new ProductDto
                    {
                        Productid = pd.Product.Productid,
                        Categoryid = pd.Product.Categoryid,
                        Sku = pd.Product.Sku,
                        Productname = pd.Product.Productname,
                        Description = pd.Product.Description,
                        Price = pd.Product.Price,
                        TotalQuantity = pd.Product.Stocks?.Sum(s => s.Stockquantity) ?? 0,
                        Status = pd.Product.Status,
                        Unit = pd.Product.Unit,
                        ImageUrl = pd.Product.ImageUrl
                    } : null
                }).ToList()
            };
        }).ToList();
    }

    public async Task<ProductDto> CloneBasketAsync(int templateId, int customerId, string? customName)
    {
        // Validate template exists and is actually a template
        var template = await _uow.GetRepository<Product>().FindAsync(
            p => p.Productid == templateId && p.Status == ProductStatus.TEMPLATE,
            include: q => q
                .Include(p => p.Config)
                    .ThenInclude(c => c.ConfigDetails)
                .Include(p => p.ProductDetailProductparents)
                    .ThenInclude(pd => pd.Product)
        );

        if (template == null)
            throw new Exception("Template giỏ quà không tồn tại hoặc không khả dụng.");

        if (!template.Configid.HasValue)
            throw new Exception("Template không hợp lệ (thiếu cấu hình).");

        // Validate customer account exists
        await ValidateForeignKeys(customerId, null, null);

        // 1. Clone Product as DRAFT
        var newBasket = new Product
        {
            Configid = template.Configid,
            Accountid = customerId,
            Productname = customName ?? $"Bản sao của {template.Productname}",
            Description = template.Description,
            ImageUrl = template.ImageUrl,
            Status = ProductStatus.DRAFT,  // Customer có thể chỉnh sửa
            Unit = template.Unit,
            Price = template.Price
        };

        await _uow.GetRepository<Product>().AddAsync(newBasket);
        await _uow.SaveAsync();

        // 2. Clone all ProductDetails (batch insert for performance)
        if (template.ProductDetailProductparents.Any())
        {
            var detailRepo = _uow.GetRepository<ProductDetail>();
            var newDetails = template.ProductDetailProductparents.Select(detail => new ProductDetail
            {
                Productparentid = newBasket.Productid,
                Productid = detail.Productid,
                Quantity = detail.Quantity
            }).ToList();

            await detailRepo.AddRangeAsync(newDetails);
        }

        await _uow.SaveAsync();
        
        // 3. Reload with full details to return complete DTO (similar to GetCustomProductByIdAsync but without Stocks)
        var clonedBasket = await _uow.GetRepository<Product>().FindAsync(
            p => p.Productid == newBasket.Productid,
            include: p => p
                .Include(p => p.Config)
                    .ThenInclude(c => c.ConfigDetails)
                .Include(p => p.Account)
                .Include(p => p.ProductDetailProductparents)
                    .ThenInclude(pd => pd.Product)
        );
        
        if (clonedBasket == null)
            throw new Exception("Lỗi khi tạo giỏ quà clone.");
        
        clonedBasket.CalculateUnit();
        clonedBasket.CalculateTotalPrice();
        
        return new ProductDto
        {
            Productid = clonedBasket.Productid,
            Categoryid = clonedBasket.Categoryid,
            Configid = clonedBasket.Configid,
            Accountid = clonedBasket.Accountid,
            Sku = clonedBasket.Sku,
            Productname = clonedBasket.Productname,
            Description = clonedBasket.Description,
            Price = clonedBasket.Price,
            TotalQuantity = 0,  // Not including stock data as per requirement
            Status = clonedBasket.Status,
            Unit = clonedBasket.Unit,
            ImageUrl = clonedBasket.ImageUrl,
            IsCustom = true,
            ProductDetails = clonedBasket.ProductDetailProductparents?.Select(pd => new ProductDetailResponse
            {
                Productdetailid = pd.Productdetailid,
                Productid = pd.Productid,
                Quantity = pd.Quantity,
                ChildProduct = pd.Product != null ? new ProductDto
                {
                    Productid = pd.Product.Productid,
                    Categoryid = pd.Product.Categoryid,
                    Sku = pd.Product.Sku,
                    Productname = pd.Product.Productname,
                    Description = pd.Product.Description,
                    Price = pd.Product.Price,
                    TotalQuantity = 0,  // Not including stock data
                    Status = pd.Product.Status,
                    Unit = pd.Product.Unit,
                    ImageUrl = pd.Product.ImageUrl
                } : null
            }).ToList()
        };
    }

    /// <summary>
    /// Admin: Set a basket product as template
    /// Validates that product is a valid basket before setting as template
    /// </summary>
    public async Task SetAsTemplateAsync(int productId)
    {
        var repo = _uow.GetRepository<Product>();
        var product = await repo.FindAsync(
            p => p.Productid == productId && !p.Status.Equals(ProductStatus.DELETED),
            include: p => p.Include(pr => pr.Config)
        );

        if (product == null)
            throw new Exception("Sản phẩm không tồn tại.");

        if (!product.Configid.HasValue)
            throw new Exception("Chỉ có thể đặt giỏ quà (có cấu hình) làm template.");

        product.Status = ProductStatus.TEMPLATE;
        repo.Update(product);
        await _uow.SaveAsync();
    }

    /// <summary>
    /// Admin: Remove template status (set back to ACTIVE)
    /// </summary>
    public async Task RemoveTemplateAsync(int productId)
    {
        var repo = _uow.GetRepository<Product>();
        var product = await repo.GetByIdAsync(productId);

        if (product == null)
            throw new Exception("Template không tồn tại.");

        if (product.Status != ProductStatus.TEMPLATE)
            throw new Exception("Sản phẩm này không phải template.");

        product.Status = ProductStatus.ACTIVE;
        repo.Update(product);
        await _uow.SaveAsync();
    }

    public async Task<IEnumerable<ProductDto>> GetWithQueryAsync(ProductQueryParameters productQuery)
    {
        var repo = _uow.GetRepository<Product>();

        // 1. Khởi tạo query từ repo
        var query = repo.Entities.AsQueryable();

        #region Filter

        // 2. Validate & Filter theo Search]
        if (!string.IsNullOrWhiteSpace(productQuery.Search))
        {
            string search = productQuery.Search.Trim().ToLower();
            query = query.Where(p => p.Productname.ToLower().Contains(search)
                                  || p.Description.ToLower().Contains(search));
        }

        // 3. Filter theo Categories
        if (productQuery.Categories != null && productQuery.Categories.Any())
        {
            query = query.Where(p => p.Categoryid.HasValue && productQuery.Categories.Contains(p.Categoryid.Value));
        }

        // 4. Filter theo Price Range
        if (productQuery.MinPrice > 0)
        {
            query = query.Where(p => p.Price >= productQuery.MinPrice);
        }
        if (productQuery.MaxPrice > 0 && productQuery.MaxPrice >= productQuery.MinPrice)
        {
            query = query.Where(p => p.Price <= productQuery.MaxPrice);
        }

        #endregion

        #region Sort

        // 5. Xử lý Sort
        if (!string.IsNullOrWhiteSpace(productQuery.Sort))
        {
            query = productQuery.Sort.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Productname),
                "name_desc" => query.OrderByDescending(p => p.Productname),
                _ => query.OrderBy(p => p.Productid)
            };
        }
        // Default
        else
        {
            query = query.OrderBy(p => p.Productid);
        }

        #endregion

        // Common constraint
        query = query.Where(p
            => p.Status.Equals(ProductStatus.ACTIVE)
            && (p.Account == null || !p.Account.Role.Equals(UserRole.CUSTOMER))
            );

        // 6. Thực thi query và map sang Dto
        List<Product> products;

        if (IsPageRequest(productQuery))
        {
            // Phân trang
            int skip = (productQuery.PageNumber!.Value - 1) * productQuery.PageSize!.Value;
            products = await query
                .Skip(skip)
                .Take(productQuery.PageSize.Value)
                .ToListAsync();
        }
        else
        {
            products = await query.ToListAsync();
        }


        return products.Select(p =>
        {
            return new ProductDto
            {
                Productid = p.Productid,
                Categoryid = p.Categoryid,
                Configid = p.Configid,
                Accountid = p.Accountid,
                Sku = p.Sku,
                Productname = p.Productname,
                Description = p.Description,
                Price = p.Price,
                Status = p.Status,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl
            };
        });
    }

    private bool IsPageRequest(ProductQueryParameters productQuery)
    {
        return productQuery.PageNumber.HasValue && productQuery.PageNumber > 0
                          && productQuery.PageSize.HasValue && productQuery.PageSize > 0;
    }
}