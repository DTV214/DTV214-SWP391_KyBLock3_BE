using System.ComponentModel.DataAnnotations;
using TetGift.DAL.Entities;

namespace TetGift.BLL.Dtos
{
    public class ProductConfigDto
    {
        public int Configid { get; set; }

        [Required(ErrorMessage = "Tên cấu hình không được để trống")]
        public string? Configname { get; set; }

        public string? Suitablesuggestion { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Tổng đơn vị phải là số dương lớn hơn 0")]
        public decimal? Totalunit { get; set; }
        public string? Imageurl { get; set; }

        public List<ConfigDetailDto> ConfigDetails { get; set; } = new();

        public List<ProductDto> Products { get; set; } = new();
    }

    public class CreateConfigRequest
    {
        [Required(ErrorMessage = "Tên cấu hình không được để trống")]
        public string Configname { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Totalunit { get; set; }
        public Dictionary<int, int> CategoryQuantities { get; set; } = new();
    }

    public class UpdateConfigRequest
    {
        [Required(ErrorMessage = "Tên cấu hình không được để trống")]
        public string Configname { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Totalunit { get; set; }
        public Dictionary<int, int> CategoryQuantities { get; set; } = new();
    }

}
