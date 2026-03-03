using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Services;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly GeminiChatService _geminiChatService;

        public ChatController(GeminiChatService geminiChatService)
        {
            _geminiChatService = geminiChatService;
        }

        /// <summary>
        /// Chat endpoint for Gemini AI assistant
        /// </summary>
        /// <param name="request">Chat request containing history and current message</param>
        /// <returns>AI response</returns>
        [HttpPost]
        public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new ChatResponse
                {
                    Reply = "Vui lòng nhập tin nhắn."
                });
            }

            var response = await _geminiChatService.ChatAsync(request.History, request.Message);
            return Ok(response);
        }
    }
}