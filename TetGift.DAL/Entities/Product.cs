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

    public string? Status { get; set; }

    public decimal? Unit { get; set; }

    public virtual Account? Account { get; set; }

    public virtual ICollection<CartDetail> CartDetails { get; set; } = [];

    public virtual ProductCategory? Category { get; set; }

    public virtual ProductConfig? Config { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = [];

    public virtual ICollection<ProductDetail> ProductDetailProductparents { get; set; } = [];

    public virtual ICollection<ProductDetail> ProductDetailProducts { get; set; } = [];

    public virtual ICollection<QuotationItem> QuotationItems { get; set; } = [];

    public virtual ICollection<Stock> Stocks { get; set; } = [];

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

    public void EnsureProductNotDuplicate(int childProductId)
    {
        bool isAlreadyExisted = ProductDetailProductparents
            .Any(pd => pd.Productid == childProductId);

        if (isAlreadyExisted)
        {
            throw new Exception("Sản phẩm này đã tồn tại trong danh sách chi tiết của sản phẩm cha.");
        }
    }
}
