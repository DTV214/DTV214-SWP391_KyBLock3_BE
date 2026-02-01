using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "ADMIN")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Lấy dữ liệu revenue theo khoảng thời gian (day/month/year)
    /// </summary>
    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue([FromQuery] TimeRangeRequest request)
    {
        try
        {
            // Set default period if not provided
            if (string.IsNullOrWhiteSpace(request.Period))
                request.Period = "day";

            // Set default date range if not provided (last 30 days)
            if (!request.StartDate.HasValue)
                request.StartDate = DateTime.UtcNow.AddHours(7).AddDays(-30);
            if (!request.EndDate.HasValue)
                request.EndDate = DateTime.UtcNow.AddHours(7);

            var result = await _dashboardService.GetRevenueByTimeRangeAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy thống kê kênh thanh toán
    /// </summary>
    [HttpGet("payment-channels")]
    public async Task<IActionResult> GetPaymentChannels([FromQuery] TimeRangeRequest? request = null)
    {
        try
        {
            var result = await _dashboardService.GetPaymentChannelStatisticsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách giỏ hàng bỏ dở
    /// </summary>
    [HttpGet("abandoned-carts")]
    public async Task<IActionResult> GetAbandonedCarts([FromQuery] int? days = null)
    {
        try
        {
            var result = await _dashboardService.GetAbandonedCartsAsync(days);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy tổng hợp dashboard (tất cả metrics)
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] TimeRangeRequest? request = null)
    {
        try
        {
            // Set default to current month if not provided
            if (request == null)
            {
                request = new TimeRangeRequest
                {
                    Period = "month",
                    StartDate = new DateTime(DateTime.UtcNow.AddHours(7).Year, DateTime.UtcNow.AddHours(7).Month, 1),
                    EndDate = DateTime.UtcNow.AddHours(7)
                };
            }
            else if (string.IsNullOrWhiteSpace(request.Period))
            {
                request.Period = "month";
            }

            var result = await _dashboardService.GetDashboardSummaryAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
