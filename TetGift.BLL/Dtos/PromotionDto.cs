using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetGift.BLL.Dtos
{
    public class CreatePromotionRequest
    {
        [Required(ErrorMessage = "Mã giảm giá là bắt buộc")]
        [StringLength(20, ErrorMessage = "Mã giảm giá tối đa 20 ký tự")]
        public string Code { get; set; } = null!;

        [Required(ErrorMessage = "Giá trị giảm là bắt buộc")]
        [Range(1000, double.MaxValue, ErrorMessage = "Giá trị giảm phải lớn hơn 1000 VND")]
        public decimal DiscountValue { get; set; }

        [Required(ErrorMessage = "Ngày hết hạn là bắt buộc")]
        public DateTime ExpiryDate { get; set; }
    }
    public class UpdatePromotionRequest
    {
        public string Code { get; set; } = null!;
        public decimal DiscountValue { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    // DTO cho Response trả về
    public class PromotionResponseDto
    {
        public int PromotionId { get; set; }
        public string Code { get; set; } = null!;
        public decimal DiscountValue { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
    }
}
