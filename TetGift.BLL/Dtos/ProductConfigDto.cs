using System.ComponentModel.DataAnnotations;

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
    }
}
