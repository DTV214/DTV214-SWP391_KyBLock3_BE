using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/dashboard-comparisons")]
    [Authorize(Roles = "ADMIN")]
    public class DashboardComparisonController : ControllerBase
    {
        private readonly IDashboardComparisonService _comparisonService;

        public DashboardComparisonController(IDashboardComparisonService comparisonService)
        {
            _comparisonService = comparisonService;
        }

        /// <summary>
        /// So sánh doanh thu theo ngày giữa 2 tháng (dùng Totalprice của Order)
        /// </summary>
        [HttpGet("monthly-order-revenue")]
        public async Task<IActionResult> GetMonthlyOrderRevenueComparison([FromQuery] MonthComparisonRequest request)
        {
            try
            {
                var result = await _comparisonService.GetMonthlyOrderRevenueComparisonAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// So sánh lợi nhuận thực tế theo ngày giữa 2 tháng
        /// </summary>
        [HttpGet("monthly-actual-revenue")]
        public async Task<IActionResult> GetMonthlyActualRevenueComparison([FromQuery] MonthComparisonRequest request)
        {
            try
            {
                var result = await _comparisonService.GetMonthlyActualRevenueComparisonAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// So sánh doanh thu theo 12 tháng giữa 2 năm (dùng Totalprice của Order)
        /// </summary>
        [HttpGet("yearly-order-revenue-comparison")]
        public async Task<IActionResult> GetYearlyOrderRevenueComparison([FromQuery] YearComparisonRequest request)
        {
            try
            {
                var result = await _comparisonService.GetYearlyOrderRevenueComparisonAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// So sánh lợi nhuận thực tế theo 12 tháng giữa 2 năm
        /// </summary>
        [HttpGet("yearly-actual-revenue-comparison")]
        public async Task<IActionResult> GetYearlyActualRevenueComparison([FromQuery] YearComparisonRequest request)
        {
            try
            {
                var result = await _comparisonService.GetYearlyActualRevenueComparisonAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
