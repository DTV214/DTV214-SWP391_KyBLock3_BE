using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/quotations")]
    public class AdminQuotationsController : BaseApiController
    {
        private readonly IQuotationService _svc;

        public AdminQuotationsController(IQuotationService svc)
        {
            _svc = svc;
        }

        // GET /api/admin/quotations?status=WAITING_ADMIN
        [HttpGet]
        public async Task<IActionResult> GetQuotations([FromQuery] string? status)
        {
            var data = await _svc.GetAdminQuotationsAsync(status);
            return Ok(data);
        }

        // GET /api/admin/quotations/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var data = await _svc.GetAdminQuotationDetailAsync(id);
            return Ok(data);
        }

        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] AdminDecisionRequest req)
        {
            req.AdminAccountId = GetAccountId();

            await _svc.AdminApproveAsync(id, req);
            return Ok(new { message = "Approved." });
        }

        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] AdminDecisionRequest req)
        {
            req.AdminAccountId = GetAccountId();

            await _svc.AdminRejectAsync(id, req);
            return Ok(new { message = "Rejected." });
        }
    }
}
