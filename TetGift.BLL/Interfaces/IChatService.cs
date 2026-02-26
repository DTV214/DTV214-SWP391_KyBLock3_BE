using TetGift.BLL.Dtos;
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
    }
}
