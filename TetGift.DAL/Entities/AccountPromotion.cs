namespace TetGift.DAL.Entities;

public partial class AccountPromotion
{
    public int Id { get; set; }

    public int AccountId { get; set; }

    public int PromotionId { get; set; }

    public int? Quantity { get; set; }

    public int? UsedQuantity { get; set; }

    public virtual Promotion Promotion { get; set; } = null!;
    public virtual Account Account { get; set; } = null!;

    public bool IsUsed()
    {
        return (Quantity ?? 0) == (UsedQuantity ?? 0);
    }
}