using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/dashboard-rankings")]
    [Authorize(Roles = "ADMIN")]
    public class DashboardRankingController : ControllerBase
    {
        private readonly IDashboardRankingService _dashboardRankingService;

        public DashboardRankingController(IDashboardRankingService dashboardRankingService)
        {
            _dashboardRankingService = dashboardRankingService;
        }

        /// <summary>
        /// Ranking doanh thu/lợi nhuận/số lượng bán theo product category trong tuần/tháng/năm
        /// </summary>
        [HttpGet("category-performance")]
        public async Task<IActionResult> GetCategoryPerformance([FromQuery] DashboardRankingRequest request)
        {
            try
            {
                var result = await _dashboardRankingService.GetCategoryPerformanceAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Ranking doanh thu/lợi nhuận/số lượng bán theo product của một category trong tuần/tháng/năm
        /// </summary>
        [HttpGet("category-products-performance")]
        public async Task<IActionResult> GetCategoryProductsPerformance(
            [FromQuery] int categoryId,
            [FromQuery] DashboardRankingRequest request)
        {
            try
            {
                var result = await _dashboardRankingService.GetCategoryProductsPerformanceAsync(categoryId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
