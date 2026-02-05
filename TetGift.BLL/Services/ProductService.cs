using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class ProductService(IUnitOfWork uow) : IProductService
{
    private readonly IUnitOfWork _uow = uow;

    public async Task CreateNormalAsync(ProductDto dto)
    {
        if (dto.Categoryid == null || !dto.Categoryid.HasValue
            || dto.Accountid == null || !dto.Accountid.HasValue
            || string.IsNullOrWhiteSpace(dto.Sku)
            || string.IsNullOrWhiteSpace(dto.Productname)
            || dto.Price == null || !dto.Price.HasValue || dto.Price.Value <= 0
            || dto.Unit == null || !dto.Unit.HasValue || dto.Unit.Value <= 0)
        {
            throw new Exception("Thiếu thông tin bắt buộc cho sản phẩm thường (Category, Account, SKU, Name, Price, Unit).");
        }

        await ValidateForeignKeys(dto.Accountid, null, dto.Categoryid);

        var entity = MapToEntity(dto);
        await _uow.GetRepository<Product>().AddAsync(entity);
        await _uow.SaveAsync();
    }

    public async Task CreateCustomAsync(ProductDto dto)
    {
        if (dto.Accountid == null || !dto.Accountid.HasValue
            || string.IsNullOrWhiteSpace(dto.Productname))
        {
            throw new Exception("Thiếu thông tin bắt buộc cho sản phẩm tùy chỉnh (ConfigId, AccountId, ProductName).");
        }

        await ValidateForeignKeys(dto.Accountid, dto.Configid, null);
        dto.Unit = 0;
        dto.Price = 0;

        var entity = MapToEntity(dto);
        await _uow.GetRepository<Product>().AddAsync(entity);
        await _uow.SaveAsync();
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products = await _uow.GetRepository<Product>().GetAllAsync(
            p => (p.Account == null || !p.Account.Role.Equals(UserRole.CUSTOMER))
                && !p.Status.Equals(ProductStatus.DELETED)
                && !p.Status.Equals(ProductStatus.DRAFT)
                && (p.Status.Equals(ProductStatus.ACTIVE) || p.Status.Equals(ProductStatus.INACTIVE)),
            include: p => p.Include(p => p.ProductDetailProductparents).ThenInclude(pd => pd.Product)
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
            include: p => p.Include(p => p.ProductDetailProductparents).ThenInclude(pd => pd.Product)
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
            Status = product.Status,
            Unit = product.Unit,
            ImageUrl = product.ImageUrl
        };
    }

    public async Task<IEnumerable<ProductDto>> GetByAccountIdAsync(int accountId)
    {
        var products = await _uow.GetRepository<Product>()
            .GetAllAsync(p => p.Accountid == accountId && !p.Status.Equals(ProductStatus.DELETED),
            include: p => p.Include(p => p.ProductDetailProductparents).ThenInclude(pd => pd.Product)
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
                Status = p.Status,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl
            };
        });
    }

    private Product MapToEntity(ProductDto dto)
    {
        return new Product
        {
            Categoryid = dto.Categoryid,
            Configid = dto.Configid,
            Accountid = dto.Accountid,
            Sku = dto.Sku,
            Productname = dto.Productname,
            Description = dto.Description,
            Price = dto.Price,
            Status = dto.Status ?? ProductStatus.ACTIVE,
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

    public async Task<UpdateProductDto> UpdateCustomAsync(ProductDto dto, bool isCustomer)
    {
        var repo = _uow.GetRepository<Product>();
        var result = await repo.FindAsync(
            e => e.Productid == dto.Productid && !e.Status.Equals(ProductStatus.DELETED)
            );
        if (!result.Any()) throw new Exception("Không tìm thấy sản phẩm.");
        var entity = result.First();

        var response = new UpdateProductDto();

        #region Validation Account, Cate, Blank String, Number Property

        // 0. Validate same account
        if (isCustomer && entity.Accountid != dto.Accountid)
            throw new Exception("Người chỉnh sửa sản phẩm không phải người đã tạo ra sản phẩm.");

        // 1. Validate ConfigId và Logic Unit
        if (dto.Configid.HasValue)
        {
            var configResult = await _uow.GetRepository<ProductConfig>().FindAsync(
                            c => c.Configid == dto.Configid.Value && c.Isdeleted == false
                            );
            if (!configResult.Any()) throw new Exception($"ConfigId {dto.Configid} không tồn tại.");
            var config = configResult.First();

            entity.Configid = dto.Configid;

            // So sánh Unit của Product với Totalunit của Config
            decimal currentUnit = dto.Unit ?? entity.Unit ?? 0;
            if (config.Totalunit.HasValue && currentUnit > config.Totalunit.Value)
            {
                response.Warning = $"Cảnh báo: Đơn vị sản phẩm ({currentUnit}) vượt quá hạn mức cấu hình ({config.Totalunit.Value}).";
            }
        }

        // 2. Validate string không trống
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

        #endregion

        repo.Update(entity);
        await _uow.SaveAsync();
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
            p => p.Status == ProductStatus.TEMPLATE && p.Configid.HasValue,
            include: p => p
                .Include(p => p.Config)
                .Include(p => p.ProductDetailProductparents)
                    .ThenInclude(pd => pd.Product)
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
                Status = p.Status,
                Unit = p.Unit,
                ImageUrl = p.ImageUrl,
                IsCustom = true
            };
        });
    }

    public async Task<ProductDto> CloneBasketAsync(int templateId, int customerId, string? customName)
    {
        // Validate template exists and is actually a template
        var template = await _uow.GetRepository<Product>().FindAsync(
            p => p.Productid == templateId && p.Status == ProductStatus.TEMPLATE,
            include: q => q
                .Include(p => p.Config)
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

        return new ProductDto
        {
            Productid = newBasket.Productid,
            Configid = newBasket.Configid,
            Accountid = newBasket.Accountid,
            Productname = newBasket.Productname,
            Description = newBasket.Description,
            ImageUrl = newBasket.ImageUrl,
            Price = newBasket.Price,
            Unit = newBasket.Unit,
            Status = newBasket.Status,
            IsCustom = true
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