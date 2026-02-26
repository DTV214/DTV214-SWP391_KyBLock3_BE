namespace TetGift.BLL.Dtos
{
    public class SendMessageRequest
    {
        public int SenderId { get; set; }

        //bind order
        public int? OrderId { get; set; }

        public string Content { get; set; } = null!;
    }
}
