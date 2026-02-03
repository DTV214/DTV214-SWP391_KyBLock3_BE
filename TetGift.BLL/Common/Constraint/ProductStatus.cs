namespace TetGift.BLL.Common.Constraint;

public static class ProductStatus
{
    public const string ACTIVE = "ACTIVE";
    public const string DELETED = "DELETED";
    public const string INACTIVE = "INACTIVE";
    public const string DRAFT = "DRAFT";        // Customer đang soạn giỏ quà
    public const string TEMPLATE = "TEMPLATE";  // Admin tạo giỏ mẫu
}
