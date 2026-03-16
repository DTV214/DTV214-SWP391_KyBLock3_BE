using TetGift.DAL.Entities;

namespace TetGift.BLL.Interfaces
{
    public interface IChatService
    {
        Task<Conversation> GetOrCreateConversationAsync(int userId);
        Task<IEnumerable<Message>> GetMessagesAsync(int conversationId);
        Task<Message> SendMessageAsync(int senderId, int? orderId, string content);
        Task<IEnumerable<Conversation>> GetAllConversationsAsync();
        Task MarkAsReadAsync(int conversationId, int readerId);

        // Mới: trả messages nhưng đảm bảo conversation thuộc về userId
        Task<IEnumerable<Message>> GetMessagesForUserAsync(int conversationId, int userId);

        // Mới: staff/admin reply cho 1 conversation
        Task<Message> ReplyToConversationAsync(int staffId, int conversationId, int? orderId, string content);

        Task<bool> CanAccessConversationAsync(int conversationId, int userId, string role);
    }
}