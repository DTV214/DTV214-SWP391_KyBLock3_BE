using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : BaseApiController
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        // Lấy conversation của chính mình
        [HttpGet("conversation")]
        public async Task<IActionResult> GetConversation()
        {
            var userId = GetAccountId();

            var result = await _chatService.GetOrCreateConversationAsync(userId);
            return Ok(result);
        }

        // Load messages - CHỈ DÀNH CHO STAFF/ADMIN (xem tất cả)
        [HttpGet("messages/{conversationId}")]
        [Authorize(Roles = "STAFF,ADMIN")]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            var messages = await _chatService.GetMessagesAsync(conversationId);

            var response = messages.Select(m => new MessageResponse
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                OrderId = m.OrderId,
                Content = m.Content,
                IsRead = m.IsRead,
                CreatedAt = m.CreatedAt
            });

            return Ok(response);
        }

        // Load messages cho user: chỉ được xem conversation của chính mình
        [HttpGet("messages/me/{conversationId}")]
        public async Task<IActionResult> GetMyMessages(int conversationId)
        {
            var userId = GetAccountId();

            try
            {
                var messages = await _chatService.GetMessagesForUserAsync(conversationId, userId);

                var response = messages.Select(m => new MessageResponse
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    OrderId = m.OrderId,
                    Content = m.Content,
                    IsRead = m.IsRead,
                    CreatedAt = m.CreatedAt
                });

                return Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Gửi tin nhắn (User hoặc Staff đều dùng được)
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest("Content is required.");

            var senderId = GetAccountId();

            var message = await _chatService.SendMessageAsync(
                senderId,
                request.OrderId,
                request.Content);

            var response = new MessageResponse
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                OrderId = message.OrderId,
                Content = message.Content,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt
            };

            return Ok(response);
        }

        // Mới: staff/admin reply cho conversation
        [HttpPost("reply/{conversationId}")]
        [Authorize(Roles = "STAFF,ADMIN")]
        public async Task<IActionResult> ReplyToConversation(int conversationId, [FromBody] ReplyMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest("Content is required.");

            var staffId = GetAccountId();

            var message = await _chatService.ReplyToConversationAsync(
                staffId,
                conversationId,
                request.OrderId,
                request.Content);

            var response = new MessageResponse
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                SenderId = message.SenderId,
                OrderId = message.OrderId,
                Content = message.Content,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt
            };

            return Ok(response);
        }

        // Staff xem tất cả conversation
        [HttpGet("all")]
        [Authorize(Roles = "STAFF,ADMIN")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _chatService.GetAllConversationsAsync();
            return Ok(result);
        }

        [HttpPut("read/{conversationId}")]
        public async Task<IActionResult> MarkRead(int conversationId)
        {
            var readerId = GetAccountId();

            await _chatService.MarkAsReadAsync(conversationId, readerId);
            return Ok();
        }
    }
}