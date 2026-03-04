namespace TetGift.DAL.Entities;

public partial class Promotion
{
    public int Promotionid { get; set; }

    public string? Code { get; set; }
    public decimal? MinPriceToApply { get; set; }
    public decimal? MaxDiscountPrice { get; set; }
    public DateTime? EndTime { get; set; }
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
        if (!Isdeleted.HasValue) Isdeleted = false;
        if (!IsLimited.HasValue) IsLimited = false;
        if (!UsedCount.HasValue) UsedCount = 0;

        return !Isdeleted.Value
            && DateTime.Now < EndTime
            && DateTime.Now > StartTime
            && (!IsLimited.Value || (IsLimited.Value && UsedCount.Value > 0));
    }

    public double ApplyPromotion(double price)
    {
        if (!IsPercentage.HasValue) IsPercentage = false;

        if (IsPercentage.Value)
        {

        }
        else
        {

        }
    }
}
