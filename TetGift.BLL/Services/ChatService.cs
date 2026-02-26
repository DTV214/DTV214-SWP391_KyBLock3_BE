using Microsoft.AspNetCore.SignalR;
using TetGift.BLL.Hubs;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatService(IUnitOfWork unitOfWork,
                           IHubContext<ChatHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        public async Task<Conversation> GetOrCreateConversationAsync(int userId)
        {
            var conversationRepo = _unitOfWork.GetRepository<Conversation>();

            var conversation = await conversationRepo
                .FindAsync(x => x.UserId == userId);

            if (conversation.Any())
                return conversation.First();

            var newConversation = new Conversation
            {
                UserId = userId
            };

            await conversationRepo.AddAsync(newConversation);
            await _unitOfWork.SaveAsync();

            return newConversation;
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(int conversationId)
        {
            var messageRepo = _unitOfWork.GetRepository<Message>();

            return await messageRepo.GetAllAsync(
                x => x.ConversationId == conversationId,
                include: q => q
                    .OrderBy(m => m.CreatedAt)
            );
        }

        public async Task<Message> SendMessageAsync(int senderId, int? orderId, string content)
        {
            _unitOfWork.BeginTransaction();

            try
            {
                var conversationRepo = _unitOfWork.GetRepository<Conversation>();
                var messageRepo = _unitOfWork.GetRepository<Message>();

                // 1 user = 1 conversation
                var conversation = (await conversationRepo
                    .FindAsync(x => x.UserId == senderId))
                    .FirstOrDefault();

                if (conversation == null)
                {
                    conversation = new Conversation
                    {
                        UserId = senderId
                    };

                    await conversationRepo.AddAsync(conversation);
                    await _unitOfWork.SaveAsync();
                }

                var message = new Message
                {
                    ConversationId = conversation.Id,
                    SenderId = senderId,
                    OrderId = orderId,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                await messageRepo.AddAsync(message);

                conversation.LastMessageAt = DateTime.UtcNow;
                await conversationRepo.UpdateAsync(conversation);

                await _unitOfWork.SaveAsync();
                _unitOfWork.CommitTransaction();

                // 🔥 Realtime push
                await _hubContext.Clients
                    .Group($"conversation_{conversation.Id}")
                    .SendAsync("ReceiveMessage", message);

                return message;
            }
            catch
            {
                _unitOfWork.RollBack();
                throw;
            }
        }

        public async Task<IEnumerable<Conversation>> GetAllConversationsAsync()
        {
            var repo = _unitOfWork.GetRepository<Conversation>();

            return await repo.GetAllAsync(
                include: q => q.OrderByDescending(x => x.LastMessageAt)
            );
        }

        public async Task MarkAsReadAsync(int conversationId, int readerId)
        {
            var messageRepo = _unitOfWork.GetRepository<Message>();

            var messages = await messageRepo.FindAsync(
                x => x.ConversationId == conversationId &&
                     x.SenderId != readerId &&
                     !x.IsRead
            );

            foreach (var msg in messages)
            {
                msg.IsRead = true;
                await messageRepo.UpdateAsync(msg);
            }

            await _unitOfWork.SaveAsync();
        }
    }
}
