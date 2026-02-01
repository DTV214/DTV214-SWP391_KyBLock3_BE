using System.ComponentModel.DataAnnotations;

//namespace TetGift.BLL.Dtos;

//public class ConfigDetailDto
//{
//    public int Configdetailid { get; set; }
//    public int? Configid { get; set; }

//    [Required(ErrorMessage = "Categoryid là bắt buộc")]
//    public int? Categoryid { get; set; }

//    [Required(ErrorMessage = "Số lượng là bắt buộc")]
//    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
//    public int? Quantity { get; set; }
//}

namespace TetGift.BLL.Dtos
{
    // DTO chi tiết ConfigDetail
    public class ConfigDetailDto
    {
        public int Configdetailid { get; set; }
        public int Configid { get; set; }
        public int Categoryid { get; set; }
        public string CategoryName { get; set; } = null!;
        public int Quantity { get; set; }
    }

    // DTO tạo/cập nhật ProductConfig kèm ConfigDetails
    public class CreateProductConfigRequest
    {
        [Required(ErrorMessage = "Tên cấu hình không được để trống")]
        public string Configname { get; set; } = null!;

        public string? Suitablesuggestion { get; set; }

        public string? Imageurl { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 chi tiết cấu hình")]
        public List<ConfigDetailRequest> ConfigDetails { get; set; } = new();
    }

    public class UpdateProductConfigRequest
    {
        [Required(ErrorMessage = "Tên cấu hình không được để trống")]
        public string Configname { get; set; } = null!;

        public string? Suitablesuggestion { get; set; }

        public string? Imageurl { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 chi tiết cấu hình")]
        public List<ConfigDetailRequest> ConfigDetails { get; set; } = new();
    }

    public class ConfigDetailRequest
    {
        [Required]
        public int Categoryid { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
    }

    // DTO trả về ProductConfig đầy đủ thông tin
    public class ProductConfigDetailDto
    {
        public int Configid { get; set; }
        public string Configname { get; set; } = null!;
        public string? Suitablesuggestion { get; set; }
        public decimal? Totalunit { get; set; }
        public string? Imageurl { get; set; }
        public List<ConfigDetailDto> ConfigDetails { get; set; } = new();
    }
}