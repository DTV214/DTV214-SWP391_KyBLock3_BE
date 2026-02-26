namespace TetGift.DAL.Entities
{
    public class Message
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }
        public int SenderId { get; set; }

        public string Content { get; set; } = null!;

        // Optional: bind order trong từng tin nhắn
        public int? OrderId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Conversation Conversation { get; set; } = null!;
        public Account Sender { get; set; } = null!;
        public Order? Order { get; set; }
    }
}