using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos
{

    public class ProductDto
    {
        public int Productid { get; set; }
        public int? Categoryid { get; set; }
        public int? Configid { get; set; }
        public int? Accountid { get; set; }
        public string? Sku { get; set; }
        public string? Productname { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal? Price { get; set; }
        public string? Status { get; set; }
        public decimal? Unit { get; set; }
        public bool IsCustom { get; set; } = false;
    }
    //DTO tạo sản phẩm đơn lẻ
    public class CreateSingleProductRequest
    {
        [Required]
        public int Categoryid { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        public string Productname { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Đơn vị (trọng lượng) phải lớn hơn 0")]
        public decimal Unit { get; set; }

        public string? Sku { get; set; }

        public string? ImageUrl { get; set; }
    }

    // DTO tạo sản phẩm combo/giỏ quà
    public class CreateComboProductRequest
    {
        [Required]
        public int Configid { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        public string Productname { get; set; } = null!;

        public string? Description { get; set; }

        public int? Accountid { get; set; } // Null nếu Admin tạo, có giá trị nếu Customer tự thiết kế

        public string? ImageUrl { get; set; }

        public string Status { get; set; } = "DRAFT"; // DRAFT, ACTIVE, INACTIVE

        [Required]
        [MinLength(1, ErrorMessage = "Giỏ quà phải có ít nhất 1 món")]
        public List<ProductDetailRequest> ProductDetails { get; set; } = new();
    }

    // DTO cập nhật sản phẩm đơn lẻ
    public class UpdateSingleProductRequest
    {
        public int Categoryid { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        public string Productname { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Đơn vị (trọng lượng) phải lớn hơn 0")]
        public decimal Unit { get; set; }

        public string? Sku { get; set; }

        public string? ImageUrl { get; set; }

        public string? Status { get; set; }
    }

    // DTO cập nhật sản phẩm combo
    public class UpdateComboProductRequest
    {
        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        public string Productname { get; set; } = null!;

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public string? Status { get; set; }
    }

    // DTO trả về Product đầy đủ thông tin
    public class ProductDetailDto
    {
        public int Productid { get; set; }
        public int? Categoryid { get; set; }
        public string? CategoryName { get; set; }
        public int? Configid { get; set; }
        public string? ConfigName { get; set; }
        public int? Accountid { get; set; }
        public string? Sku { get; set; }
        public string? Productname { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? Status { get; set; }
        public decimal? Unit { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsCustom { get; set; } = false;
        public List<ProductDetailResponse> ProductDetails { get; set; } = new();
        public ProductConfigDetailDto? Config { get; set; }
        public StockAvailabilityDto? StockInfo { get; set; } // Thông tin tồn kho (chỉ cho sản phẩm đơn)
    }

    // DTO validate giỏ quà theo Config
    public class ValidateComboRequest
    {
        [Required]
        public int Configid { get; set; }

        [Required]
        public List<ProductDetailRequest> ProductDetails { get; set; } = new();
    }

    public class ValidateComboResponse
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public decimal? TotalPrice { get; set; }
        public decimal? TotalUnit { get; set; }
        public Dictionary<int, ConfigCategoryValidation> CategoryValidations { get; set; } = new();
    }

    public class ConfigCategoryValidation
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public int RequiredQuantity { get; set; }
        public int CurrentQuantity { get; set; }
        public bool IsSatisfied { get; set; }
    }

    // DTO cho clone basket request
    public class CloneBasketRequest
    {
        public string? CustomName { get; set; }
    }
}