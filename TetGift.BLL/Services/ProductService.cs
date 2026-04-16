using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class ProductService(IUnitOfWork uow, IInventoryService inventoryService, ICacheService cacheService, IMediaService mediaService) : IProductService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ICacheService _cacheService = cacheService;
    private readonly IMediaService _mediaService = mediaService;

    public async Task CreateNormalAsync(CreateSingleProductRequest dto)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.Sku)
            || string.IsNullOrWhiteSpace(dto.Productname)
            || dto.Price <= 0
            || dto.Unit <= 0)
        {
            throw new Exception("Thiáº¿u thĂ´ng tin báº¯t buá»™c cho sáº£n pháº©m thÆ°á»ng (SKU, Name, Price, Unit).");
        }

        // Note: Accountid will be set from authenticated user context in controller
        await ValidateForeignKeys(dto.Accountid, null, dto.Categoryid);

        // Create Product entity
        var entity = MapToEntity(dto);
        await _uow.GetRepository<Product>().AddAsync(entity);
        await _uow.SaveAsync();

        await _cacheService.IncreaseVersionAsync("Products");
    }

    public async Task<int> CreateCustomAsync(CreateComboProductRequest dto)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.Productname))
        {
            throw new Exception("TĂªn sáº£n pháº©m khĂ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        }

        if (dto.ProductDetails == null || !dto.ProductDetails.Any())
        {
            throw new Exception("Giá» quĂ  pháº£i cĂ³ Ă­t nháº¥t 1 sáº£n pháº©m.");
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
            throw new Exception($"KhĂ´ng tĂ¬m tháº¥y cáº¥u hĂ¬nh giá» quĂ  vá»›i ID {dto.Configid}.");

        // Validate all ProductIds in ProductDetails exist and match config requirements
        decimal totalWeight = 0;
        decimal totalVolume = 0;
        var validCategoryIds = config.ConfigDetails?.Select(cd => cd.Categoryid).ToHashSet() ?? new HashSet<int?>();

        foreach (var detail in dto.ProductDetails)
        {
            if (!detail.Productid.HasValue)
                throw new Exception("ProductId khĂ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng trong ProductDetails.");

            var product = await _uow.GetRepository<Product>().FindAsync(
                p => p.Productid == detail.Productid.Value && !p.Status.Equals(ProductStatus.DELETED),
                include: p => p.Include(pr => pr.Category)
            );

            if (product == null)
                throw new Exception($"Sáº£n pháº©m vá»›i ID {detail.Productid} khĂ´ng tá»“n táº¡i hoáº·c Ä‘Ă£ bá»‹ xĂ³a.");

            // Validate product category belongs to config's allowed categories
            if (validCategoryIds.Any())
            {
                if (!product.Categoryid.HasValue || !validCategoryIds.Contains(product.Categoryid))
                {
                    var allowedCategories = string.Join(", ",
                        config.ConfigDetails?.Select(cd => cd.Category?.Categoryname ?? "N/A") ?? new List<string>());
                    throw new Exception(
                        $"Sáº£n pháº©m '{product.Productname}' thuá»™c danh má»¥c khĂ´ng há»£p lá»‡. " +
                        $"Danh má»¥c cho phĂ©p: {allowedCategories}");
                }
            }

            // Calculate total weight and validate Space (3D Bin packing logic)
            var quantity = detail.Quantity ?? 1;

            if (config.MaxLength.HasValue && config.MaxWidth.HasValue && config.MaxHeight.HasValue)
            {
                if (!product.Length.HasValue || !product.Width.HasValue || !product.Height.HasValue)
                {
                    throw new Exception($"Sáº£n pháº©m '{product.Productname}' thiáº¿u thĂ´ng tin kĂ­ch thÆ°á»›c nĂªn khĂ´ng thá»ƒ xáº¿p vĂ o giá».");
                }

                // TEST 1: Chiá»u lá»t khung
                var itemDims = new[] { product.Length.Value, product.Width.Value, product.Height.Value }.OrderBy(x => x).ToArray();
                var boxDims = new[] { config.MaxLength.Value, config.MaxWidth.Value, config.MaxHeight.Value }.OrderBy(x => x).ToArray();

                if (itemDims[0] > boxDims[0] || itemDims[1] > boxDims[1] || itemDims[2] > boxDims[2])
                {
                    throw new Exception($"KĂ­ch thÆ°á»›c cá»§a mĂ³n '{product.Productname}' ({product.Length}x{product.Width}x{product.Height}) quĂ¡ lá»›n, khĂ´ng lá»t vá»«a giá» quĂ .");
                }

                totalVolume += (product.Volume ?? 0) * quantity;
            }

            var productWeight = product.Unit ?? 0;
            totalWeight += productWeight * quantity;
        }

        // Validate total Volume doesn't exceed 85% of box capacity (packing factor)
        if (config.MaxVolume.HasValue && totalVolume > config.MaxVolume.Value * 0.85m)
        {
            throw new Exception($"Tá»•ng thá»ƒ tĂ­ch cá»§a giá»/há»™p Ä‘Ă£ quĂ¡ Ä‘áº§y do sá»©c chá»©a váº­t lĂ½ cĂ³ háº¡n. Vui lĂ²ng giáº£m sá»‘ lÆ°á»£ng cĂ¡c mĂ³n Ä‘á»“.");
        }

        // Validate total weight doesn't exceed config limit
        if (config.Totalunit.HasValue && totalWeight > config.Totalunit.Value)
        {
            throw new Exception(
                $"Tá»•ng trá»ng lÆ°á»£ng giá» quĂ  ({totalWeight}g) vÆ°á»£t quĂ¡ giá»›i háº¡n cho phĂ©p ({config.Totalunit.Value}g). " +
                $"Vui lĂ²ng giáº£m sá»‘ lÆ°á»£ng sáº£n pháº©m.");
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
                createdProduct.CalculateImportPrice();
                _uow.GetRepository<Product>().Update(createdProduct);
                await _uow.SaveAsync();
            }
        }

        await _cacheService.IncreaseVersionAsync("Products");
        return entity.Productid;
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
            p.CalculateImportPrice();

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
                ImportPrice = p.ImportPrice,
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
                Length = p.Length,
                Width = p.Width,
                Height = p.Height,
                ImageUrl = p.ImageUrl
            };
        });
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _uow.GetRepository<Product>().FindAsync(
            p => p.Productid == id,
            include: p => p.Include(p => p.Stocks)
                .Include(p => p.ProductDetailProductparents).ThenInclude(pd => pd.Product).ThenInclude(s => s.Stocks)
            );
        if (product == null) return null;
        product.CalculateUnit();
        product.CalculateTotalPrice();
        product.CalculateImportPrice();

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
            ImportPrice = product.ImportPrice,
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
            Length = product.Length,
            Width = product.Width,
            Height = product.Height,
            ImageUrl = product.ImageUrl,
            ProductDetails = product.ProductDetailProductparents?.Select(pd => new ProductDetailResponse
            {
                Productdetailid = pd.Productdetailid,
                Productparentid = pd.Productparentid,
                Productid = pd.Productid,
                Categoryid = pd.Product?.Categoryid,
                Productname = pd.Product?.Productname,
                Unit = pd.Product?.Unit,
                Price = pd.Product?.Price,
                //ImportPrice = pd.Product?.ImportPrice,
                Imageurl = pd.Product?.ImageUrl,
                Quantity = pd.Quantity,
                ChildProduct = null
            }).ToList()
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
            p.CalculateImportPrice();
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
                ImportPrice = p.ImportPrice,
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
                Length = p.Length,
                Width = p.Width,
                Height = p.Height,
                ImageUrl = p.ImageUrl
            };
        });
    }

    /// <summary>
    /// Get customer's custom baskets (giá» quĂ  tá»± táº¡o)
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
            basket.CalculateImportPrice();

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
            ImportPrice = dto.ImportPrice,
            Status = ProductStatus.ACTIVE,
            Unit = dto.Unit,
                Length = dto.Length,
                Width = dto.Width,
                Height = dto.Height,
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

            await _cacheService.IncreaseVersionAsync("Products");
        }
    }

    public async Task HardDeleteTemplateAsync(int id)
    {
        var productRepo = _uow.GetRepository<Product>();
        var productDetailRepo = _uow.GetRepository<ProductDetail>();

        var product = await productRepo.FindAsync(
            p => p.Productid == id,
            include: q => q.Include(p => p.ProductDetailProductparents)
        );

        if (product == null)
            throw new Exception("KhĂ´ng tĂ¬m tháº¥y sáº£n pháº©m.");

        // XĂ³a táº¥t cáº£ ProductDetail liĂªn káº¿t (child items) cá»§a template nĂ y
        if (product.ProductDetailProductparents?.Count > 0)
            productDetailRepo.DeleteRange(product.ProductDetailProductparents.ToList());

        // XĂ³a vÄ©nh viá»…n sáº£n pháº©m template
        productRepo.Delete(product);
        await _uow.SaveAsync();

        await _cacheService.IncreaseVersionAsync("Products");
    }

    private async Task ValidateForeignKeys(int? accountId, int? configId, int? categoryId)
    {
        if (accountId.HasValue)
        {
            var account = await _uow.GetRepository<Account>().FindAsync(
                a => a.Accountid == accountId.Value && a.Status.Equals(ProductStatus.ACTIVE)
                );
            if (!account.Any()) throw new Exception($"AccountId {accountId} khĂ´ng tá»“n táº¡i.");
        }

        if (configId.HasValue)
        {
            var config = await _uow.GetRepository<ProductConfig>().FindAsync(
                c => c.Configid == configId.Value && c.Isdeleted == false
                );
            if (!config.Any()) throw new Exception($"ConfigId {configId} khĂ´ng tá»“n táº¡i.");
        }

        if (categoryId.HasValue)
        {
            var category = await _uow.GetRepository<ProductCategory>().FindAsync(
                c => c.Categoryid == categoryId.Value && c.Isdeleted == false
                );
            if (!category.Any()) throw new Exception($"CategoryId {categoryId} khĂ´ng tá»“n táº¡i.");
        }
    }

    public async Task<UpdateProductDto> UpdateNormalAsync(ProductDto dto)
    {
        var repo = _uow.GetRepository<Product>();
        var result = await repo.FindAsync(
            e => e.Productid == dto.Productid && !e.Status.Equals(ProductStatus.DELETED)
            );
        if (!result.Any()) throw new Exception("KhĂ´ng tĂ¬m tháº¥y sáº£n pháº©m.");
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
            if (string.IsNullOrWhiteSpace(dto.Sku)) throw new Exception("SKU khĂ´ng Ä‘Æ°á»£c Ä‘ă»ƒ trá»‘ng.");
            entity.Sku = dto.Sku;
        }
        if (dto.Productname != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Productname)) throw new Exception("TĂªn khĂ´ng Ä‘Æ°á»£c Ä‘ă»ƒ trá»‘ng.");
            entity.Productname = dto.Productname;
        }
        if (dto.Status != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Status)
                || (!dto.Status.Equals(ProductStatus.ACTIVE) && !dto.Status.Equals(ProductStatus.INACTIVE)
                && !dto.Status.Equals("OUT_OF_STOCK")
                )) throw new Exception("Tráº¡ng thĂ¡i khĂ´ng Ä‘Æ°á»£c Ä‘ă»ƒ trá»‘ng hoáº·c sai syntax.");
            entity.Status = dto.Status;
        }

        // 3. Validate sá»‘ dÆ°Æ¡ng
        if (dto.Price.HasValue)
        {
            if (dto.Price <= 0) throw new Exception("GiĂ¡ pháº£i lá»›n hÆ¡n 0.");
            entity.Price = dto.Price;
        }
        if (dto.Unit.HasValue)
        {
            if (dto.Unit <= 0) throw new Exception("ÄÆ¡n vá»‹ pháº£i lá»›n hÆ¡n 0.");
            entity.Unit = dto.Unit;
            entity.Length = dto.Length;
            entity.Width = dto.Width;
            entity.Height = dto.Height;
        }

        #endregion

        if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
            entity.ImageUrl = dto.ImageUrl;

        repo.Update(entity);
        await _uow.SaveAsync();
        await _cacheService.IncreaseVersionAsync("Products");
        return new UpdateProductDto();
    }

    /// <summary>
    /// Update custom basket/combo product.
    /// 
    /// Business rules:
    ///   â€¢ Admin/Staff can only edit baskets they (or other admin/staff) created â€” NOT customer baskets.
    ///   â€¢ Customer can only edit their own baskets.
    ///   â€¢ A TEMPLATE basket (admin-created) cannot be edited by customers â€” they must clone it first.
    ///   â€¢ Admin/Staff may transition status freely (DRAFT / ACTIVE / INACTIVE / TEMPLATE).
    ///   â€¢ Customer may only use DRAFT or ACTIVE.
    /// </summary>
    public async Task<UpdateProductDto> UpdateCustomAsync(int productId, UpdateComboProductRequest dto, int? requestingAccountId)
    {
        var repo = _uow.GetRepository<Product>();

        // â”€â”€â”€ 1. Identify the requesting user â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var requestorList = await _uow.GetRepository<Account>().FindAsync(
            a => a.Accountid == requestingAccountId
        );
        var requestor = requestorList.FirstOrDefault();

        bool requestorIsCustomer = requestor?.Role.Equals(UserRole.CUSTOMER) == true;
        bool requestorIsAdminOrStaff = requestor?.Role.Equals(UserRole.ADMIN) == true
                                     || requestor?.Role.Equals(UserRole.STAFF) == true;

        // â”€â”€â”€ 2. Fetch the basket (customers may only see their own) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var product = await repo.FindAsync(
            p => p.Productid == productId
                && p.Configid.HasValue                                             // must be a basket/combo
                && !p.Status.Equals(ProductStatus.DELETED)
                && (!requestorIsCustomer || p.Accountid == requestingAccountId),   // customers see their own only
            include: q => q
                .Include(p => p.Config)
                    .ThenInclude(c => c.ConfigDetails)
                    .ThenInclude(cd => cd.Category)
                .Include(p => p.Account)
                .Include(p => p.ProductDetailProductparents)
        );

        if (product == null)
            throw new Exception(requestorIsCustomer
                ? "KhĂ´ng tĂ¬m tháº¥y giá» quĂ  cá»§a báº¡n hoáº·c báº¡n khĂ´ng cĂ³ quyá»n chá»‰nh sá»­a."
                : "KhĂ´ng tĂ¬m tháº¥y giá» quĂ  hoáº·c sáº£n pháº©m khĂ´ng pháº£i giá» quĂ  tĂ¹y chá»‰nh.");

        var response = new UpdateProductDto();

        // â”€â”€â”€ 3. Determine basket origin â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //   isCustomerBasket  = basket was created by a customer (cloned from template or self-composed)
        //   isAdminBasket     = basket was created by admin / staff (includes templates)
        bool isCustomerBasket = product.Account?.Role.Equals(UserRole.CUSTOMER) == true;
        bool isAdminBasket = !isCustomerBasket;

        // â”€â”€â”€ 4. Access-control â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Admin/Staff must NOT touch customer baskets â€” those belong to the customer.
        if (requestorIsAdminOrStaff && isCustomerBasket)
            throw new Exception(
                "Admin/Staff khĂ´ng thá»ƒ chá»‰nh sá»­a giá» quĂ  cá»§a khĂ¡ch hĂ ng. " +
                "Chá»‰ chĂ­nh khĂ¡ch hĂ ng má»›i cĂ³ quyá»n chá»‰nh sá»­a giá» quĂ  cá»§a há».");

        // Customer must NOT touch admin-managed (template) baskets directly â€” they must clone first.
        if (requestorIsCustomer && isAdminBasket)
            throw new Exception(
                "KhĂ´ng thá»ƒ chá»‰nh sá»­a giá» quĂ  máº«u cá»§a admin. " +
                "Vui lĂ²ng clone template Ä‘á»ƒ táº¡o báº£n sao riĂªng trÆ°á»›c khi chá»‰nh sá»­a.");

        // â”€â”€â”€ 5. Per-role field & status restrictions â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (requestorIsCustomer)
        {
            // Customer's own basket: must not be in TEMPLATE state
            if (product.Status == ProductStatus.TEMPLATE)
                throw new Exception("KhĂ´ng thá»ƒ chá»‰nh sá»­a template giá» quĂ . Vui lĂ²ng clone template trÆ°á»›c.");

            // Customer may only transition between DRAFT â†” ACTIVE
            if (dto.Status != null &&
                !new[] { ProductStatus.DRAFT, ProductStatus.ACTIVE }.Contains(dto.Status))
                throw new Exception("KhĂ¡ch hĂ ng chá»‰ cĂ³ thá»ƒ Ä‘áº·t tráº¡ng thĂ¡i DRAFT hoáº·c ACTIVE.");
        }
        else
        {
            // Admin/Staff: any status except DELETED (use DELETE API for that)
            if (dto.Status != null && dto.Status.Equals(ProductStatus.DELETED))
                throw new Exception(
                    "KhĂ´ng thá»ƒ Ä‘áº·t tráº¡ng thĂ¡i DELETED qua API nĂ y. " +
                    "Vui lĂ²ng sá»­ dá»¥ng API xĂ³a sáº£n pháº©m (DELETE /products/{id}).");
        }

        // â”€â”€â”€ 6. Apply basic field updates â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (!string.IsNullOrWhiteSpace(dto.Productname))
            product.Productname = dto.Productname;

        if (dto.Description != null)
            product.Description = dto.Description;

        if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
            product.ImageUrl = dto.ImageUrl;

        // â”€â”€â”€ 7. Apply status update â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (dto.Status != null)
        {
            // By this point per-role validation above has already ensured the status is valid.
            product.Status = dto.Status;
        }

        // â”€â”€â”€ 8. Update ProductDetails if provided â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (dto.ProductDetails != null)
        {
            var validCategoryIds = product.Config?.ConfigDetails?
                .Select(cd => cd.Categoryid)
                .ToHashSet() ?? new HashSet<int?>();

            decimal totalWeight = 0;
            decimal totalVolume = 0;

            foreach (var detail in dto.ProductDetails)
            {
                if (!detail.Productid.HasValue)
                    throw new Exception("ProductId khĂ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng trong ProductDetails.");

                var childProduct = await _uow.GetRepository<Product>().FindAsync(
                    p => p.Productid == detail.Productid.Value
                        && p.Status == ProductStatus.ACTIVE,
                    include: p => p.Include(pr => pr.Category)
                );

                if (childProduct == null)
                    throw new Exception($"Sáº£n pháº©m vá»›i ID {detail.Productid} khĂ´ng tá»“n táº¡i hoáº·c khĂ´ng kháº£ dá»¥ng.");

                // Validate product category is allowed by this config
                if (product.Config != null && validCategoryIds.Any())
                {
                    if (!childProduct.Categoryid.HasValue || !validCategoryIds.Contains(childProduct.Categoryid))
                    {
                        var allowed = string.Join(", ",
                            product.Config.ConfigDetails?
                                .Select(cd => cd.Category?.Categoryname ?? "N/A")
                            ?? Enumerable.Empty<string>());
                        throw new Exception(
                            $"Sáº£n pháº©m '{childProduct.Productname}' thuá»™c danh má»¥c khĂ´ng há»£p lá»‡. " +
                            $"Danh má»¥c cho phĂ©p: {allowed}");
                    }
                }

                var quantity = detail.Quantity ?? 1;

                // Validate space/dimension
                if (product.Config?.MaxLength.HasValue == true && product.Config?.MaxWidth.HasValue == true && product.Config?.MaxHeight.HasValue == true)
                {
                    if (!childProduct.Length.HasValue || !childProduct.Width.HasValue || !childProduct.Height.HasValue)
                    {
                        throw new Exception($"Sáº£n pháº©m '{childProduct.Productname}' thiáº¿u thĂ´ng tin kĂ­ch thÆ°á»›c.");
                    }
                    var itemDims = new[] { childProduct.Length.Value, childProduct.Width.Value, childProduct.Height.Value }.OrderBy(x => x).ToArray();
                    var boxDims = new[] { product.Config.MaxLength.Value, product.Config.MaxWidth.Value, product.Config.MaxHeight.Value }.OrderBy(x => x).ToArray();

                    if (itemDims[0] > boxDims[0] || itemDims[1] > boxDims[1] || itemDims[2] > boxDims[2])
                    {
                        throw new Exception($"KĂ­ch thÆ°á»›c cá»§a '{childProduct.Productname}' khĂ´ng lá»t vá»«a giá»/há»™p.");
                    }
                    totalVolume += (childProduct.Volume ?? 0) * quantity;
                }

                var productWeight = childProduct.Unit ?? 0;
                totalWeight += productWeight * quantity;

                // For customers only: out-of-stock guard for large quantities
                if (requestorIsCustomer && quantity > 10)
                {
                    var stocks = await _uow.GetRepository<Stock>().GetAllAsync(
                        s => s.Productid == detail.Productid && s.Status == StockStatus.ACTIVE
                    );
                    if ((stocks.Sum(s => s.Stockquantity) ?? 0) == 0)
                        throw new Exception($"Sáº£n pháº©m '{childProduct.Productname}' hiá»‡n Ä‘ang háº¿t hĂ ng.");
                }
            }

            // â”€â”€ Volume limit check â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            if (product.Config?.MaxVolume.HasValue == true && totalVolume > product.Config.MaxVolume.Value * 0.85m)
            {
                throw new Exception($"Tá»•ng thá»ƒ tĂch cá»§a giá»/há»™p Ä‘Ă£ quĂ¡ Ä‘áº§y do sá»©c chá»©a váº­t lĂ½ cĂ³ háº¡n. Vui lĂ²ng giáº£m sá»‘ lÆ°á»£ng cĂ¡c mĂ³n Ä‘á»“.");
            }

            // â”€â”€ Weight limit check â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            if (product.Config?.Totalunit.HasValue == true && totalWeight > product.Config.Totalunit.Value)
            {
                throw new Exception(
                    $"Tá»•ng trá»ng lÆ°á»£ng giá» quĂ  ({totalWeight}g) vÆ°á»£t quĂ¡ giá»›i háº¡n cho phĂ©p ({product.Config.Totalunit.Value}g). " +
                    $"Vui lĂ²ng giáº£m sá»‘ lÆ°á»£ng sáº£n pháº©m.");
            }

            // â”€â”€ Replace ProductDetails (delete old, insert new) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            var detailRepo = _uow.GetRepository<ProductDetail>();
            var oldDetails = product.ProductDetailProductparents.ToList();
            if (oldDetails.Any())
            {
                await detailRepo.DeleteRangeAsync(oldDetails);
            }

            var newDetails = dto.ProductDetails.Select(d => new ProductDetail
            {
                Productparentid = product.Productid,
                Productid = d.Productid,
                Quantity = d.Quantity ?? 1
            }).ToList();

            await detailRepo.AddRangeAsync(newDetails);
        }

        // â”€â”€â”€ 9. Persist and recalculate weight/price â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        repo.Update(product);
        await _uow.SaveAsync();

        if (dto.ProductDetails != null)
        {
            var updatedProduct = await repo.FindAsync(
                p => p.Productid == productId,
                include: q => q
                    .Include(p => p.Config)
                    .Include(p => p.ProductDetailProductparents)
                    .ThenInclude(pd => pd.Product)
            );

            if (updatedProduct != null)
            {
                updatedProduct.CalculateUnit();
                updatedProduct.CalculateTotalPrice();

                if (updatedProduct.Config?.Totalunit.HasValue == true
                    && updatedProduct.Unit > updatedProduct.Config.Totalunit.Value)
                {
                    response.Warning = $"Cáº£nh bĂ¡o: Trá»ng lÆ°á»£ng giá» quĂ  ({updatedProduct.Unit}) vÆ°á»£t quĂ¡ giá»›i háº¡n cáº¥u hĂ¬nh ({updatedProduct.Config.Totalunit.Value}).";
                }

                repo.Update(updatedProduct);
                await _uow.SaveAsync();
            }
        }

        await _cacheService.IncreaseVersionAsync("Products");

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
            throw new Exception("KhĂ´ng tĂ¬m tháº¥y sáº£n pháº©m.");

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
            p.CalculateImportPrice();

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
                Length = p.Length,
                Width = p.Width,
                Height = p.Height,
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
                Length = product.Length,
                Width = product.Width,
                Height = product.Height,
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
            p.CalculateImportPrice();

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
                Length = p.Length,
                Width = p.Width,
                Height = p.Height,
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
    /// Láº¥y giá» quĂ  ACTIVE cá»§a admin/staff cho trang shop (khĂ¡ch hĂ ng duyá»‡t)
    /// </summary>
    public async Task<IEnumerable<ProductDto>> GetShopBasketsAsync()
    {
        var baskets = await _uow.GetRepository<Product>().GetAllAsync(
            p => p.Configid.HasValue
                && (p.Account == null || !p.Account.Role.Equals(UserRole.CUSTOMER))
                && p.Status.Equals(ProductStatus.ACTIVE),
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
            p.CalculateImportPrice();

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
                Length = p.Length,
                Width = p.Width,
                Height = p.Height,
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
        // Validate template exists and is ACTIVE or TEMPLATE status
        var template = await _uow.GetRepository<Product>().FindAsync(
            p => p.Productid == templateId
                && (p.Status == ProductStatus.ACTIVE || p.Status == ProductStatus.TEMPLATE || p.Accountid == customerId),
            include: q => q
                .Include(p => p.Config)
                    .ThenInclude(c => c.ConfigDetails)
                .Include(p => p.ProductDetailProductparents)
                    .ThenInclude(pd => pd.Product)
        );

        if (template == null)
            throw new Exception("Giá» quĂ  khĂ´ng tá»“n táº¡i hoáº·c khĂ´ng kháº£ dá»¥ng.");

        if (!template.Configid.HasValue)
            throw new Exception("Giá» quĂ  khĂ´ng há»£p lá»‡ (thiáº¿u cáº¥u hĂ¬nh).");

        // Validate customer account exists
        await ValidateForeignKeys(customerId, null, null);

        // 1. Clone Product as DRAFT
        var newBasket = new Product
        {
            Configid = template.Configid,
            Accountid = customerId,
            Productname = customName ?? $"Báº£n sao cá»§a {template.Productname}",
            Description = template.Description,
            ImageUrl = template.ImageUrl,
            Status = ProductStatus.DRAFT,  // Customer cĂ³ thá»ƒ chá»‰nh sá»­a
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
            throw new Exception("Lá»—i khi táº¡o giá» quĂ  clone.");

        clonedBasket.CalculateUnit();
        clonedBasket.CalculateTotalPrice();
        await _cacheService.IncreaseVersionAsync("Products");

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
            throw new Exception("Sáº£n pháº©m khĂ´ng tá»“n táº¡i.");

        if (!product.Configid.HasValue)
            throw new Exception("Chá»‰ cĂ³ thá»ƒ Ä‘áº·t giá» quĂ  (cĂ³ cáº¥u hĂ¬nh) lĂ m template.");

        product.Status = ProductStatus.TEMPLATE;
        repo.Update(product);
        await _uow.SaveAsync();
        await _cacheService.IncreaseVersionAsync("Products");
    }

    /// <summary>
    /// Admin: Remove template status (set back to ACTIVE)
    /// </summary>
    public async Task RemoveTemplateAsync(int productId)
    {
        var repo = _uow.GetRepository<Product>();
        var product = await repo.GetByIdAsync(productId);

        if (product == null)
            throw new Exception("Template khĂ´ng tá»“n táº¡i.");

        if (product.Status != ProductStatus.TEMPLATE)
            throw new Exception("Sáº£n pháº©m nĂ y khĂ´ng pháº£i template.");

        product.Status = ProductStatus.ACTIVE;
        repo.Update(product);
        await _uow.SaveAsync();
        await _cacheService.IncreaseVersionAsync("Products");
    }

    public async Task<PagedResponse<ProductDto>> GetWithQueryAsync(ProductQueryParameters productQuery)
    {
        #region Redis Cache (Láº¥y thá»­ trong Redis)

        // Láº¥y version hiá»‡n táº¡i cá»§a module Products
        string version = await _cacheService.GetVersionAsync("Products");

        // 1. Äá»‹nh nghÄ©a tiá»n tá»‘ cho Cache key
        string cachePrefix = $"TetGift:Products:V{version}:Search";

        // 2. Táº¡o Key duy nháº¥t dá»±a trĂªn ná»™i dung cá»§a productQuery (Search, Page, Sort...)
        string cacheKey = _cacheService.GenerateCacheKey(productQuery, cachePrefix);

        // 3. Thá»­ láº¥y tá»« Redis trÆ°á»›c
        var cachedResult = await _cacheService.GetAsync<PagedResponse<ProductDto>>(cacheKey);
        if (cachedResult != null) return cachedResult;

        #endregion

        var repo = _uow.GetRepository<Product>();

        // 1. Khá»Ÿi táº¡o query tá»« repo
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

        // 5. Filter sáº£n pháº©m Ä‘Æ¡n
        if (productQuery.IsSingleProduct ?? false)
        {
            query = query.Where(p => p.Configid == null);
        }

        #endregion

        #region Sort

        // 5. Xá»­ lĂ½ Sort
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

        // 6. Thá»±c thi query vĂ  map sang Dto
        List<Product> products;

        #region Paging

        int totalItems = await query.CountAsync();
        bool isPagingRequested = IsPageRequest(productQuery);

        if (isPagingRequested)
        {
            // PhĂ¢n trang
            int skip = (productQuery.PageNumber!.Value - 1) * productQuery.PageSize!.Value;
            products = await query
                .Include(p => p.Stocks)
                .Skip(skip)
                .Take(productQuery.PageSize.Value)
                .ToListAsync();
        }
        else
        {
            products = await query.ToListAsync();
        }

        #endregion

        #region Mapping

        var dtos = products.Select(p =>
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
                ImportPrice = p.ImportPrice,
                Status = p.Status,
                Unit = p.Unit,
                Length = p.Length,
                Width = p.Width,
                Height = p.Height,
                TotalQuantity = p.Stocks?.Sum(s => s.Stockquantity) ?? 0,
                ImageUrl = p.ImageUrl
            };
        });

        if (productQuery.IsNotShowAll ?? false)
        {
            dtos = dtos.Where(p => p.TotalQuantity > 0);
        }

        #endregion

        var finalResponse = new PagedResponse<ProductDto>(dtos, totalItems, productQuery.PageNumber ?? 1, productQuery.PageSize ?? totalItems);

        await _cacheService.SetAsync(cacheKey, finalResponse, TimeSpan.FromMinutes(15));

        return finalResponse;
    }

    private bool IsPageRequest(ProductQueryParameters productQuery)
    {
        return productQuery.PageNumber.HasValue && productQuery.PageNumber > 0
                          && productQuery.PageSize.HasValue && productQuery.PageSize > 0;
    }
}


