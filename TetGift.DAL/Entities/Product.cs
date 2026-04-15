namespace TetGift.DAL.Entities;

public partial class Product
{
    public int Productid { get; set; }

    public int? Categoryid { get; set; }

    public int? Configid { get; set; }

    public int? Accountid { get; set; }

    public string? Sku { get; set; }

    public string? Productname { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }
    public decimal? ImportPrice { get; set; } // giá nhập (cost price)
    public string? Status { get; set; }
    public string? ImageUrl { get; set; }

    public decimal? Unit { get; set; }

    /// <summary>
    /// Kích thước vật lý của sản phẩm để validate khi xếp vào giỏ dynamic
    /// </summary>
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }

    // Computed property: Thể tích (Dài x Rộng x Cao)
    public decimal? Volume => (Length ?? 0) * (Width ?? 0) * (Height ?? 0);

    public virtual Account? Account { get; set; }

    public virtual ICollection<CartDetail> CartDetails { get; set; } = [];

    public virtual ProductCategory? Category { get; set; }

    public virtual ProductConfig? Config { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = [];

    public virtual ICollection<ProductDetail> ProductDetailProductparents { get; set; } = [];

    public virtual ICollection<ProductDetail> ProductDetailProducts { get; set; } = [];

    public virtual ICollection<QuotationItem> QuotationItems { get; set; } = [];

    public virtual ICollection<Stock> Stocks { get; set; } = [];

    /// <summary>
    /// Calculates import price by summing up the import prices of all child products in ProductDetail
    /// </summary>
    public void CalculateImportPrice()
    {
        // Nếu là product thường
        if (Configid == null)
            return;

        // Nếu là combo
        if (ProductDetailProductparents == null || !ProductDetailProductparents.Any())
        {
            ImportPrice = 0;
            return;
        }

        decimal total = 0;

        foreach (var item in ProductDetailProductparents)
        {
            var child = item.Product;
            if (child == null) continue;

            var importPrice = child.ImportPrice ?? 0;
            var quantity = item.Quantity ?? 1;

            total += importPrice * quantity;
        }

        ImportPrice = total;
    }

    /// <summary>
    /// Calculates total weight by summing up all ProductDetail items
    /// Used for basket products that contain multiple individual products
    /// </summary>
    public void CalculateUnit()
    {
        if (ProductDetailProductparents != null && ProductDetailProductparents.Count != 0)
        {
            decimal total = 0;
            foreach (var detail in ProductDetailProductparents)
            {
                if (detail.Product != null)
                {
                    total += (detail.Quantity ?? 0) * (detail.Product.Unit ?? 0);
                }
            }
            this.Unit = total;
        }
    }

    /// <summary>
    /// Calculates total price by summing up all ProductDetail items
    /// Used for basket products that contain multiple individual products
    /// </summary>
    public void CalculateTotalPrice()
    {
        if (ProductDetailProductparents != null && ProductDetailProductparents.Count != 0)
        {
            decimal total = 0;
            foreach (var detail in ProductDetailProductparents)
            {
                if (detail.Product != null)
                {
                    total += (detail.Quantity ?? 0) * (detail.Product.Price ?? 0);
                }
            }
            this.Price = total;
        }
    }

    /// <summary>
    /// Validates if adding a product to this basket would exceed ConfigDetail quantity limits
    /// Throws exception if validation fails
    /// Legacy method - consider using ValidateCanAddProduct for better error messages
    /// </summary>
    public void ValidateDetailAgainstConfig(int? categoryId, int? requestedQuantity)
    {
        if (Config == null) return;

        if (categoryId == null || requestedQuantity == null) throw new Exception("Sản phẩm thiếu danh mục hoặc số lượng.");

        if (!Config.ConfigDetails.Any()) return;

        // Kiểm tra danh mục
        var configDetail = Config.ConfigDetails
            .FirstOrDefault(cd => cd.Categoryid == categoryId)
            ?? throw new Exception("Sản phẩm thuộc danh mục này không hợp lệ với cấu hình hiện tại.");

        // Kiểm tra giới hạn số lượng cho phép
        int currentCategoryCount = ProductDetailProductparents
            .Where(pd => pd.Product?.Categoryid == categoryId)
            .Sum(pd => pd.Quantity ?? 0);

        if (currentCategoryCount + requestedQuantity > configDetail.Quantity)
        {
            throw new Exception($"Số lượng vượt quá giới hạn cấu hình. Tối đa cho phép: {configDetail.Quantity}, hiện tại nếu cộng thêm: {currentCategoryCount + requestedQuantity}.");
        }
    }

    /// <summary>
    /// Validates if updating a ProductDetail would exceed ConfigDetail quantity limits
    /// Excludes the current detail being updated from the count
    /// Legacy method - consider using ValidateCanAddProduct for new code
    /// </summary>
    public void ValidateUpdateDetailAgainstConfig(int? categoryId, int? requestedQuantity, int excludeDetailId)
    {
        if (Config == null) return;

        if (categoryId == null || requestedQuantity == null) throw new Exception("Sản phẩm thiếu danh mục hoặc số lượng.");

        if (Config.ConfigDetails.Count == 0) return;

        // Kiểm tra danh mục
        var configDetail = Config.ConfigDetails
            .FirstOrDefault(cd => cd.Categoryid == categoryId)
            ?? throw new Exception("Sản phẩm thuộc danh mục này không hợp lệ với cấu hình hiện tại.");

        // Kiểm tra giới hạn số lượng cho phép
        int currentCategoryCount = ProductDetailProductparents
            .Where(pd => pd.Product?.Categoryid == categoryId && pd.Productdetailid != excludeDetailId)
            .Sum(pd => pd.Quantity ?? 0);

        if (currentCategoryCount + requestedQuantity > configDetail.Quantity)
        {
            throw new Exception($"Số lượng vượt quá giới hạn cấu hình. Tối đa cho phép: {configDetail.Quantity}, hiện tại nếu cộng thêm: {currentCategoryCount + requestedQuantity}.");
        }
    }

    /// <summary>
    /// Ensures a product is not added twice to the same basket
    /// Prevents duplicate ProductDetail entries for the same child product
    /// </summary>
    public void EnsureProductNotDuplicate(int childProductId)
    {
        bool isAlreadyExisted = ProductDetailProductparents
            .Any(pd => pd.Productid == childProductId);

        if (isAlreadyExisted)
        {
            throw new Exception("Sản phẩm này đã tồn tại trong danh sách chi tiết của sản phẩm cha.");
        }
    }

    /// <summary>
    /// Validates if total weight exceeds ProductConfig.Totalunit limit
    /// </summary>
    public void ValidateWeightAgainstConfig()
    {
        if (Config == null || Config.Totalunit == null) return;

        CalculateUnit(); // Ensure Unit is up-to-date

        if (Unit > Config.Totalunit)
        {
            throw new Exception($"Tổng trọng lượng giỏ quà ({Unit}) vượt quá giới hạn cấu hình cho phép ({Config.Totalunit}).");
        }
    }

    /// <summary>
    /// Thuật toán đóng gói hàng (3D Bin packing validation) kiểm tra xem sản phẩm có lọt vào giỏ không.
    /// 1. Kiểm tra kích thước hình học 3 chiều (đảm bảo không bị quá khổ nắp hộp)
    /// 2. Kiểm tra tổng không gian thể tích (kèm theo hệ số chứa thực tế ~85%)
    /// </summary>
    public void ValidateSpaceDynamic(Product itemToAdd, int quantityToAdd = 1)
    {
        // 0. Bỏ qua nếu giỏ không bị ràng buộc bởi Config hoặc thiết lập kích thước chưa đầy đủ
        if (Config == null || Config.MaxLength == null || Config.MaxWidth == null || Config.MaxHeight == null)
            return; 

        if (itemToAdd.Length == null || itemToAdd.Width == null || itemToAdd.Height == null)
            throw new Exception($"Sản phẩm '{itemToAdd.Productname}' chưa có thông tin kích thước (Dài Rộng Cao) nên không thể đóng gói vào giỏ này.");

        // TEST 1: Kích thước tuyệt đối (Sản phẩm có lọt vô khung giỏ không?)
        // Sắp xếp các cạnh để mô phỏng việc xoay dọc ngang vật thể khi xếp vào hộp
        var itemDims = new[] { itemToAdd.Length.Value, itemToAdd.Width.Value, itemToAdd.Height.Value }.OrderBy(x => x).ToArray();
        var boxDims = new[] { Config.MaxLength.Value, Config.MaxWidth.Value, Config.MaxHeight.Value }.OrderBy(x => x).ToArray();

        if (itemDims[0] > boxDims[0] || itemDims[1] > boxDims[1] || itemDims[2] > boxDims[2])
        {
            throw new Exception($"Kích thước của món '{itemToAdd.Productname}' ({itemToAdd.Length}x{itemToAdd.Width}x{itemToAdd.Height}) quá lớn, không lọt vừa giỏ quà này.");
        }

        // TEST 2: Thể tích còn lại có đủ chứa không? (Sử dụng hệ số lấp đầy 85% để mô phỏng kẽ hở xoay sở ngoài đời)
        decimal currentTotalVolume = 0;
        if (ProductDetailProductparents != null)
        {
            foreach (var detail in ProductDetailProductparents)
            {
                if (detail.Product != null)
                {
                    currentTotalVolume += (detail.Quantity ?? 0) * (detail.Product.Volume ?? 0);
                }
            }
        }

        decimal newItemsVolume = (itemToAdd.Volume ?? 0) * quantityToAdd;
        decimal maxAllowedVolume = Config.MaxVolume ?? 0;
        decimal packingFactor = 0.85m; // Thực tế không gian hộp chỉ chứa tối đa 85% vật phẩm, 15% là rỗng/kẽ hở

        if (currentTotalVolume + newItemsVolume > maxAllowedVolume * packingFactor)
        {
            throw new Exception($"Tổng thể tích của giỏ/hộp đã quá đầy. Không còn đủ không gian để chứa thêm {quantityToAdd} x '{itemToAdd.Productname}'.");
        }
    }

    /// <summary>
    /// Checks if all ConfigDetail requirements are met (useful for warnings, not strict validation)
    /// Returns dictionary with categoryId as key and (current, required) as value
    /// </summary>
    public Dictionary<int, (int Current, int Required)> GetConfigDetailStatus()
    {
        var result = new Dictionary<int, (int Current, int Required)>();

        if (Config == null || !Config.ConfigDetails.Any())
            return result;

        foreach (var configDetail in Config.ConfigDetails)
        {
            if (!configDetail.Categoryid.HasValue || !configDetail.Quantity.HasValue)
                continue;

            int currentCount = ProductDetailProductparents
                .Where(pd => pd.Product?.Categoryid == configDetail.Categoryid)
                .Sum(pd => pd.Quantity ?? 0);

            result[configDetail.Categoryid.Value] = (currentCount, configDetail.Quantity.Value);
        }

        return result;
    }

    /// <summary>
    /// Gets validation warnings for incomplete basket configuration
    /// Returns list of warning messages (doesn't throw exceptions)
    /// </summary>
    public List<string> GetConfigValidationWarnings()
    {
        var warnings = new List<string>();

        if (Config == null || !Config.ConfigDetails.Any())
            return warnings;

        var status = GetConfigDetailStatus();
        var categoryRepo = Category?.GetType().Assembly;

        foreach (var configDetail in Config.ConfigDetails)
        {
            if (!configDetail.Categoryid.HasValue || !configDetail.Quantity.HasValue)
                continue;

            if (status.TryGetValue(configDetail.Categoryid.Value, out var counts))
            {
                if (counts.Current < counts.Required)
                {
                    var categoryName = configDetail.Category?.Categoryname ?? $"CategoryID {configDetail.Categoryid}";
                    warnings.Add($"Giỏ chưa đủ món theo cấu hình: Cần {counts.Required} món từ '{categoryName}', hiện có {counts.Current} món.");
                }
            }
        }

        return warnings;
    }

    /// <summary>
    /// Validates if a product can be added without exceeding category limits
    /// More detailed validation with better error messages
    /// </summary>
    public void ValidateCanAddProduct(int? categoryId, int? requestedQuantity)
    {
        if (Config == null) return;

        if (categoryId == null)
            throw new Exception("Không thể thêm sản phẩm không có danh mục vào giỏ quà có cấu hình.");

        if (requestedQuantity == null || requestedQuantity <= 0)
            throw new Exception("Số lượng phải là số nguyên dương lớn hơn 0.");

        if (!Config.ConfigDetails.Any())
            return;

        // Check if category is allowed in this config
        var configDetail = Config.ConfigDetails
            .FirstOrDefault(cd => cd.Categoryid == categoryId);

        if (configDetail == null)
        {
            var allowedCategories = Config.ConfigDetails
                .Where(cd => cd.Category != null)
                .Select(cd => cd.Category!.Categoryname)
                .ToList();

            var allowedCategoriesStr = allowedCategories.Any() 
                ? string.Join(", ", allowedCategories) 
                : "không có danh mục nào";

            throw new Exception($"Món này không hợp lệ với cấu hình giỏ quà này! Cấu hình chỉ cho phép các danh mục: {allowedCategoriesStr}.");
        }

        // Check quantity limits
        int currentCategoryCount = ProductDetailProductparents
            .Where(pd => pd.Product?.Categoryid == categoryId)
            .Sum(pd => pd.Quantity ?? 0);

        int remainingSlots = (configDetail.Quantity ?? 0) - currentCategoryCount;

        if (requestedQuantity > remainingSlots)
        {
            var categoryName = configDetail.Category?.Categoryname ?? "danh mục này";
            throw new Exception($"Vượt quá giới hạn cấu hình! '{categoryName}' chỉ còn được thêm tối đa {remainingSlots} món (yêu cầu: {requestedQuantity}).");
        }
    }

    /// <summary>
    /// Gets the flattened list of individual products needed for this basket
    /// Returns dictionary with ProductId as key and total quantity needed as value
    /// Used for stock validation when confirming orders containing basket products
    /// </summary>
    public Dictionary<int, int> GetRequiredProducts()
    {
        var result = new Dictionary<int, int>();

        if (ProductDetailProductparents == null || !ProductDetailProductparents.Any())
            return result;

        foreach (var detail in ProductDetailProductparents)
        {
            if (detail.Productid.HasValue && detail.Quantity.HasValue)
            {
                if (result.ContainsKey(detail.Productid.Value))
                {
                    result[detail.Productid.Value] += detail.Quantity.Value;
                }
                else
                {
                    result[detail.Productid.Value] = detail.Quantity.Value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if this product is a basket (has ProductConfig)
    /// Basket products contain multiple individual products via ProductDetail
    /// </summary>
    public bool IsBasket()
    {
        return Configid.HasValue && Config != null;
    }

    /// <summary>
    /// Checks if this product is an individual item (has Category but no Config)
    /// Individual items are tracked in Stock
    /// </summary>
    public bool IsIndividualProduct()
    {
        return Categoryid.HasValue && !Configid.HasValue;
    }
}
