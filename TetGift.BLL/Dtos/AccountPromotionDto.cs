namespace TetGift.BLL.Dtos
{
    public class AssignPromotionRequest
    {
        public int AccountId { get; set; }
        public int PromotionId { get; set; }
        public int Quantity { get; set; }
        public int? UsedQuantity { get; set; }
    }
}