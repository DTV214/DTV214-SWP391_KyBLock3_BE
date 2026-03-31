using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[ApiController]
[Route("api")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    private int GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out var accountId))
        {
            throw new UnauthorizedAccessException("Không thể xác định người dùng.");
        }
        return accountId;
    }

    // ========== ORDER FEEDBACK ==========

    [HttpPost("orders/{orderId}/feedback")]
    [Authorize]
    public async Task<IActionResult> AddFeedback(int orderId, [FromBody] CreateFeedbackRequest request)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var result = await _feedbackService.AddFeedbackAsync(orderId, accountId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("feedbacks/{feedbackId}")]
    [Authorize]
    public async Task<IActionResult> UpdateFeedback(int feedbackId, [FromBody] UpdateFeedbackRequest request)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var result = await _feedbackService.UpdateFeedbackAsync(feedbackId, accountId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("feedbacks/{feedbackId}")]
    [Authorize]
    public async Task<IActionResult> DeleteFeedback(int feedbackId)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            await _feedbackService.DeleteFeedbackAsync(feedbackId, accountId);
            return Ok(new { message = "Đã xóa bình luận." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ========== PRODUCT FEEDBACK ==========

    [HttpGet("products/{productId}/feedbacks")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductFeedbacks(int productId)
    {
        try
        {
            var feedbacks = await _feedbackService.GetFeedbacksForProductAsync(productId);
            return Ok(feedbacks);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
