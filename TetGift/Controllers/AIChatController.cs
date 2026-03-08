using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Services;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/aichat")]
    [Authorize]
    public class AIChatController : BaseApiController
    {
        private readonly GeminiChatService _geminiChatService;

        public AIChatController(GeminiChatService geminiChatService)
        {
            _geminiChatService = geminiChatService;
        }

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

            var response = await _geminiChatService.ChatAsync(request.Message);

            return Ok(response);
        }
    }
}