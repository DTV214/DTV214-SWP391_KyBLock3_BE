using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        // Load messages
        [HttpGet("messages/{conversationId}")]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            var result = await _chatService.GetMessagesAsync(conversationId);
            return Ok(result);
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
