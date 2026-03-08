using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos
{
    public class PromotionRequest
    {
        [Required(ErrorMessage = "Mã giảm giá là bắt buộc")]
        [StringLength(20, ErrorMessage = "Mã giảm giá tối đa 20 ký tự")]
        public string Code { get; set; } = null!;

        public decimal? MinPriceToApply { get; set; }

        [Required(ErrorMessage = "Giá trị giảm là bắt buộc")]
        public decimal DiscountValue { get; set; }

        public decimal? MaxDiscountPrice { get; set; }

        public bool IsPercentage { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        public DateTime ExpiryDate { get; set; }

        public bool IsLimited { get; set; }
        public int? LimitedCount { get; set; }
    }

    // DTO cho Response trả về
    public class PromotionResponseDto
    {
        public int PromotionId { get; set; }
        public string Code { get; set; } = null!;
        public decimal? MinPriceToApply { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountPrice { get; set; }
        public bool IsPercentage { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsLimited { get; set; }
        public int? LimitedCount { get; set; }
        public int? UsedCount { get; set; }
        public string? Status { get; set; }
        public bool IsAlreadySave { get; set; }
    }
}
