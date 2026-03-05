namespace TetGift.DAL.Entities;

public partial class Promotion
{
    public int Promotionid { get; set; }

    public string? Code { get; set; }
    public decimal? MinPriceToApply { get; set; }
    public decimal? MaxDiscountPrice { get; set; }
    public DateTime? StartTime { get; set; }
    public int? LimitedCount { get; set; }
    public int? UsedCount { get; set; }
    public decimal? Discountvalue { get; set; }
    public DateTime? Expirydate { get; set; }
    public bool? Isdeleted { get; set; }
    public bool? IsPercentage { get; set; }
    public bool? IsLimited { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<AccountPromotion> AccountPromotions { get; set; } = new List<AccountPromotion>();

    public bool IsValid()
    {
        var now = DateTime.Now;
        // Kiểm tra cơ bản: chưa xóa, trong thời hạn hiệu lực
        bool basicCheck = (Isdeleted != true)
                          && (Expirydate.HasValue && now <= Expirydate.Value)
                          && (StartTime.HasValue && now >= StartTime.Value);

        if (!basicCheck) return false;

        // Nếu có giới hạn số lượng, kiểm tra xem còn lượt dùng không
        if (IsLimited == true)
        {
            return (UsedCount ?? 0) < (LimitedCount ?? 0);
        }

        return true;
    }

    public bool StillNotYet()
    {
        var now = DateTime.Now;
        // Kiểm tra cơ bản: chưa xóa, trước thời hạn hiệu lực
        bool basicCheck = (Isdeleted != true)
                          && (StartTime.HasValue && now <= StartTime.Value);

        if (!basicCheck) return false;

        return true;
    }

    public (double appliedPrice, bool isApplied, string message) ApplyPromotion(double price)
    {
        // 1. Kiểm tra điều kiện cơ bản (Thời gian, Trạng thái xóa)
        if (!IsValid())
            return (price, false, "Mã giảm giá đã hết hạn hoặc không còn hiệu lực.");

        // 2. Kiểm tra giới hạn số lượng (IsLimited)
        if (IsLimited == true && (UsedCount ?? 0) >= (LimitedCount ?? 0))
        {
            return (price, false, "Mã giảm giá đã hết lượt sử dụng.");
        }

        // 3. Kiểm tra giá trị đơn hàng tối thiểu (MinPriceToApply)
        decimal minPrice = MinPriceToApply ?? 0;
        if ((decimal)price < minPrice)
        {
            return (price, false, $"Đơn hàng chưa đạt giá trị tối thiểu {minPrice:N0}đ để áp dụng mã.");
        }

        double discountAmount = 0;
        double val = (double)(Discountvalue ?? 0);

        // 4. Tính toán số tiền được giảm
        if (IsPercentage == true)
        {
            // Tính theo %
            discountAmount = price * (val / 100);

            // Kiểm tra mức giảm tối đa (MaxDiscountPrice)
            if (MaxDiscountPrice.HasValue && discountAmount > (double)MaxDiscountPrice.Value)
            {
                discountAmount = (double)MaxDiscountPrice.Value;
            }
        }
        else
        {
            // Tính theo số tiền cố định
            discountAmount = val;
        }

        // 5. Tính giá cuối cùng (đảm bảo không âm)
        double finalPrice = price - discountAmount;
        if (finalPrice < 0) finalPrice = 0;

        return (finalPrice, true, "Áp dụng mã giảm giá thành công.");
    }
}