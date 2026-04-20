using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[Route("api/statistics")]
[ApiController]
// [Authorize(Roles = "ADMIN,STAFF")] // Nhớ mở comment dòng này để bảo mật nếu cần
public class StatisticsController : ControllerBase
{
    private readonly IStatisticService _statisticService;

    public StatisticsController(IStatisticService statisticService)
    {
        _statisticService = statisticService;
    }

    /// <summary>
    /// Thống kê doanh thu, lợi nhuận chi tiết của 1 Sản phẩm
    /// GET /api/statistics/product/{productId}
    /// </summary>
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetProductStatistic(int productId)
    {
        try
        {
            var result = await _statisticService.GetProductStatisticAsync(productId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrendingProducts([FromQuery] string period = "week", [FromQuery] int top = 5)
    {
        try
        {
            var result = await _statisticService.GetTrendingProductsAsync(period, top);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpGet("event-trend")]
    // [Authorize(Roles = "ADMIN,STAFF")] 
    public async Task<IActionResult> GetEventMonthTrend([FromQuery] int month)
    {
        try
        {
            if (month < 1 || month > 12)
                return BadRequest(new { message = "Tháng không hợp lệ (Phải từ 1 đến 12)." });

            var result = await _statisticService.GetEventMonthTrendAsync(month);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}