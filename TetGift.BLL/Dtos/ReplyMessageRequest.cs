namespace TetGift.BLL.Dtos
{
    public class ReplyMessageRequest
    {
        public int? OrderId { get; set; }

        public string Content { get; set; } = null!;
    }
}
