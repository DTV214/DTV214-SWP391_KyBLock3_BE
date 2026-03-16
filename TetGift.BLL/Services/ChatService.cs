using Microsoft.AspNetCore.SignalR;
using TetGift.BLL.Hubs;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;
using TetGift.BLL.Dtos;

namespace TetGift.BLL.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatService(IUnitOfWork unitOfWork, IHubContext<ChatHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
        }

        public async Task<Conversation> GetOrCreateConversationAsync(int userId)
        {
            var conversationRepo = _unitOfWork.GetRepository<Conversation>();

            var conversation = await conversationRepo.FindAsync(x => x.UserId == userId);

            if (conversation.Any())
                return conversation.First();

            var newConversation = new Conversation
            {
                UserId = userId,
                LastMessageAt = DateTime.UtcNow
            };

            await conversationRepo.AddAsync(newConversation);
            await _unitOfWork.SaveAsync();

            // notify staff/admin conversation list
            await _hubContext.Clients.Group("staff_conversations")
                .SendAsync("ConversationUpdated", new
                {
                    ConversationId = newConversation.Id,
                    UserId = newConversation.UserId,
                    LastMessageAt = newConversation.LastMessageAt,
                    HasNewMessage = false
                });

            return newConversation;
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(int conversationId)
        {
            var messageRepo = _unitOfWork.GetRepository<Message>();

            return await messageRepo.GetAllAsync(
                x => x.ConversationId == conversationId,
                include: q => q.OrderBy(m => m.CreatedAt)
            );
        }

        public async Task<IEnumerable<Message>> GetMessagesForUserAsync(int conversationId, int userId)
        {
            var conversationRepo = _unitOfWork.GetRepository<Conversation>();
            var messageRepo = _unitOfWork.GetRepository<Message>();

            var conversation = await conversationRepo.GetByIdAsync(conversationId);
            if (conversation == null)
                throw new Exception("Conversation not found.");

            if (conversation.UserId != userId)
                throw new UnauthorizedAccessException("Bạn không có quyền xem conversation này.");

            return await messageRepo.GetAllAsync(
                x => x.ConversationId == conversationId,
                include: q => q.OrderBy(m => m.CreatedAt)
            );
        }

        public async Task<Message> SendMessageAsync(int senderId, int? orderId, string content)
        {
            _unitOfWork.BeginTransaction();

            try
            {
                var conversationRepo = _unitOfWork.GetRepository<Conversation>();
                var messageRepo = _unitOfWork.GetRepository<Message>();
                var orderRepo = _unitOfWork.GetRepository<Order>();

                if (orderId.HasValue)
                {
                    var existingOrder = await orderRepo.GetByIdAsync(orderId.Value);
                    if (existingOrder == null)
                        throw new InvalidOperationException($"Order with ID {orderId.Value} does not exist.");
                }

                var conversation = (await conversationRepo.FindAsync(x => x.UserId == senderId)).FirstOrDefault();

                if (conversation == null)
                {
                    conversation = new Conversation
                    {
                        UserId = senderId,
                        LastMessageAt = DateTime.UtcNow
                    };

                    await conversationRepo.AddAsync(conversation);
                    await _unitOfWork.SaveAsync();
                }

                var now = DateTime.UtcNow;

                var message = new Message
                {
                    ConversationId = conversation.Id,
                    SenderId = senderId,
                    OrderId = orderId,
                    Content = content,
                    CreatedAt = now,
                    IsRead = false
                };

                await messageRepo.AddAsync(message);

                conversation.LastMessageAt = now;
                await conversationRepo.UpdateAsync(conversation);

                await _unitOfWork.SaveAsync();
                _unitOfWork.CommitTransaction();

                var pushDto = new MessageResponse
                {
                    Id = message.Id,
                    ConversationId = message.ConversationId,
                    SenderId = message.SenderId,
                    OrderId = message.OrderId,
                    Content = message.Content,
                    IsRead = message.IsRead,
                    CreatedAt = message.CreatedAt
                };

                // push message to conversation members
                await _hubContext.Clients
                    .Group($"conversation_{conversation.Id}")
                    .SendAsync("ReceiveMessage", pushDto);

                // push update to staff/admin list screen
                await _hubContext.Clients
                    .Group("staff_conversations")
                    .SendAsync("ConversationUpdated", new
                    {
                        ConversationId = conversation.Id,
                        UserId = conversation.UserId,
                        LastMessageAt = conversation.LastMessageAt,
                        LastMessage = message.Content,
                        LastSenderId = message.SenderId,
                        HasNewMessage = true
                    });

                return message;
            }
            catch
            {
                _unitOfWork.RollBack();
                throw;
            }
        }

        public async Task<Message> ReplyToConversationAsync(int staffId, int conversationId, int? orderId, string content)
        {
            _unitOfWork.BeginTransaction();

            try
            {
                var conversationRepo = _unitOfWork.GetRepository<Conversation>();
                var messageRepo = _unitOfWork.GetRepository<Message>();
                var orderRepo = _unitOfWork.GetRepository<Order>();

                var conversation = await conversationRepo.GetByIdAsync(conversationId);
                if (conversation == null)
                    throw new Exception("Conversation not found.");

                if (orderId.HasValue)
                {
                    var existingOrder = await orderRepo.GetByIdAsync(orderId.Value);
                    if (existingOrder == null)
                        throw new InvalidOperationException($"Order with ID {orderId.Value} does not exist.");
                }

                var now = DateTime.UtcNow;

                var message = new Message
                {
                    ConversationId = conversationId,
                    SenderId = staffId,
                    OrderId = orderId,
                    Content = content,
                    CreatedAt = now,
                    IsRead = false
                };

                await messageRepo.AddAsync(message);

                conversation.LastMessageAt = now;
                await conversationRepo.UpdateAsync(conversation);

                await _unitOfWork.SaveAsync();
                _unitOfWork.CommitTransaction();

                var pushDto = new MessageResponse
                {
                    Id = message.Id,
                    ConversationId = message.ConversationId,
                    SenderId = message.SenderId,
                    OrderId = message.OrderId,
                    Content = message.Content,
                    IsRead = message.IsRead,
                    CreatedAt = message.CreatedAt
                };

                // push message to members in this conversation
                await _hubContext.Clients
                    .Group($"conversation_{conversationId}")
                    .SendAsync("ReceiveMessage", pushDto);

                // push update to staff/admin list screen
                await _hubContext.Clients
                    .Group("staff_conversations")
                    .SendAsync("ConversationUpdated", new
                    {
                        ConversationId = conversation.Id,
                        UserId = conversation.UserId,
                        LastMessageAt = conversation.LastMessageAt,
                        LastMessage = message.Content,
                        LastSenderId = message.SenderId,
                        HasNewMessage = true
                    });

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

            var updatedMessageIds = new List<int>();

            foreach (var msg in messages)
            {
                msg.IsRead = true;
                updatedMessageIds.Add(msg.Id);
                await messageRepo.UpdateAsync(msg);
            }

            await _unitOfWork.SaveAsync();

            // realtime read receipt
            if (updatedMessageIds.Any())
            {
                await _hubContext.Clients
                    .Group($"conversation_{conversationId}")
                    .SendAsync("MessagesRead", new
                    {
                        ConversationId = conversationId,
                        ReaderId = readerId,
                        MessageIds = updatedMessageIds
                    });

                await _hubContext.Clients
                    .Group("staff_conversations")
                    .SendAsync("ConversationReadUpdated", new
                    {
                        ConversationId = conversationId,
                        ReaderId = readerId,
                        MessageIds = updatedMessageIds
                    });
            }
        }

        public async Task<bool> CanAccessConversationAsync(int conversationId, int userId, string role)
        {
            var conversationRepo = _unitOfWork.GetRepository<Conversation>();
            var conversation = await conversationRepo.GetByIdAsync(conversationId);

            if (conversation == null)
                return false;

            if (role == "STAFF" || role == "ADMIN")
                return true;

            return conversation.UserId == userId;
        }
    }
}