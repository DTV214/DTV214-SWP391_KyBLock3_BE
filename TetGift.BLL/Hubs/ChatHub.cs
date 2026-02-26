using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Hubs
{
    /// <summary>
    /// SignalR hub for realtime chat.
    /// - Clients must call JoinConversation(conversationId) to receive messages for that conversation.
    /// - Role-based access: users can only join their own conversation; STAFF/ADMIN can join any.
    /// - Exposes lightweight realtime events (ReceiveMessage is pushed by server-side service).
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IUnitOfWork unitOfWork, ILogger<ChatHub> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Connection established: ConnectionId={ConnectionId}, User={User}", Context.ConnectionId, Context.UserIdentifier);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Connection disconnected: ConnectionId={ConnectionId}, Reason={Reason}", Context.ConnectionId, exception?.Message);
            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join group for a conversation. Server will push ReceiveMessage to group "conversation_{conversationId}".
        /// Users can only join their own conversation unless role is STAFF/ADMIN.
        /// </summary>
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

            // Allow STAFF/ADMIN to join any conversation, otherwise require ownership
            if (role != "STAFF" && role != "ADMIN" && conversation.UserId != userId)
            {
                _logger.LogWarning("JoinConversation: unauthorized join attempt ConversationId={ConversationId} by User={UserId}", conversationId, userId);
                throw new HubException("You are not allowed to join this conversation.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            _logger.LogInformation("User {UserId} joined conversation {ConversationId}", userId, conversationId);

            // Notify other participants in the conversation (excluding caller) that someone joined
            await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                         .SendAsync("ParticipantJoined", new { UserId = userId });
        }

        /// <summary>
        /// Leave conversation group.
        /// </summary>
        public async Task LeaveConversation(int conversationId)
        {
            var userId = GetCurrentUserId();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            _logger.LogInformation("User {UserId} left conversation {ConversationId}", userId, conversationId);

            await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                         .SendAsync("ParticipantLeft", new { UserId = userId });
        }

        /// <summary>
        /// Notify other participants that current user is typing (or stopped typing).
        /// </summary>
        public Task SendTyping(int conversationId, bool isTyping)
        {
            var userId = GetCurrentUserId();
            // notify others in the conversation
            return Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                          .SendAsync("UserTyping", new { UserId = userId, IsTyping = isTyping });
        }

        /// <summary>
        /// Lightweight helper to allow clients to broadcast a transient presence ping to conversation members.
        /// (Does not persist to DB — persistence should be done via HTTP / service)
        /// </summary>
        public Task PingConversation(int conversationId)
        {
            var userId = GetCurrentUserId();
            return Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                          .SendAsync("ParticipantPing", new { UserId = userId, Timestamp = DateTime.UtcNow });
        }
    }
}