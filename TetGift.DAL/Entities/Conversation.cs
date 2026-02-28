namespace TetGift.DAL.Entities
{
    public class Conversation
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastMessageAt { get; set; }

        public Account User { get; set; } = null!;

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}