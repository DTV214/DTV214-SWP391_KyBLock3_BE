using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChatHub> _logger;
        private readonly IChatService _chatService;

        public ChatHub(
            IUnitOfWork unitOfWork,
            ILogger<ChatHub> logger,
            IChatService chatService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        }

        private int GetCurrentUserId()
        {
            var idClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var id))
                throw new HubException("Could not determine user id from claims.");
            return id;
        }

        private string GetCurrentUserRole()
        {
            return Context.User?.FindFirst(ClaimTypes.Role)?.Value?.ToUpper() ?? string.Empty;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            _logger.LogInformation(
                "Connection established: ConnectionId={ConnectionId}, UserId={UserId}, Role={Role}",
                Context.ConnectionId, userId, role);

            if (role == "STAFF" || role == "ADMIN")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "staff_conversations");
                _logger.LogInformation("User {UserId} joined staff_conversations", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = 0;
            try
            {
                userId = GetCurrentUserId();
            }
            catch
            {
            }

            _logger.LogInformation(
                "Connection disconnected: ConnectionId={ConnectionId}, UserId={UserId}, Reason={Reason}",
                Context.ConnectionId, userId, exception?.Message);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinConversation(int conversationId)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            var conversationRepo = _unitOfWork.GetRepository<Conversation>();
            var conversation = await conversationRepo.GetByIdAsync(conversationId);

            if (conversation == null)
            {
                _logger.LogWarning("JoinConversation: conversation not found {ConversationId} by user {UserId}", conversationId, userId);
                throw new HubException("Conversation not found.");
            }

            if (role != "STAFF" && role != "ADMIN" && conversation.UserId != userId)
            {
                _logger.LogWarning("JoinConversation: unauthorized join attempt ConversationId={ConversationId} by User={UserId}", conversationId, userId);
                throw new HubException("You are not allowed to join this conversation.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            _logger.LogInformation("User {UserId} joined conversation {ConversationId}", userId, conversationId);

            await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                .SendAsync("ParticipantJoined", new
                {
                    ConversationId = conversationId,
                    UserId = userId
                });
        }

        public async Task LeaveConversation(int conversationId)
        {
            var userId = GetCurrentUserId();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            _logger.LogInformation("User {UserId} left conversation {ConversationId}", userId, conversationId);

            await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                .SendAsync("ParticipantLeft", new
                {
                    ConversationId = conversationId,
                    UserId = userId
                });
        }

        public async Task SendTyping(int conversationId, bool isTyping)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            var canAccess = await _chatService.CanAccessConversationAsync(conversationId, userId, role);
            if (!canAccess)
                throw new HubException("You are not allowed to access this conversation.");

            await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                .SendAsync("UserTyping", new
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    IsTyping = isTyping
                });
        }

        public async Task PingConversation(int conversationId)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            var canAccess = await _chatService.CanAccessConversationAsync(conversationId, userId, role);
            if (!canAccess)
                throw new HubException("You are not allowed to access this conversation.");

            await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                .SendAsync("ParticipantPing", new
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    Timestamp = DateTime.UtcNow
                });
        }
    }
}