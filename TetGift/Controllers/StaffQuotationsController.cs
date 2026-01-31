using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [Authorize(Roles = "Staff,Admin")]
    [Route("api/staff/quotations")]
    public class StaffQuotationsController : BaseApiController
    {
        private readonly IQuotationService _svc;

        public StaffQuotationsController(IQuotationService svc)
        {
            _svc = svc;
        }

        // GET /api/staff/quotations?status=SUBMITTED
        [HttpGet]
        public async Task<IActionResult> GetQuotations([FromQuery] string? status)
        {
            var data = await _svc.GetStaffQuotationsAsync(status);
            return Ok(data);
        }

        // GET /api/staff/quotations/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var data = await _svc.GetStaffQuotationDetailAsync(id);
            return Ok(data);
        }

        [HttpPost("{id:int}/start-review")]
        public async Task<IActionResult> StartReview(int id)
        {
            var staffAccountId = GetAccountId();
            await _svc.StartReviewAsync(id, staffAccountId);
            return Ok(new { message = "Staff reviewing started." });
        }

        [HttpPut("{id:int}/propose-price")]
        public async Task<IActionResult> ProposePrice(int id, [FromBody] StaffProposeItemDiscountRequest req)
        {
            req.StaffAccountId = GetAccountId();
            await _svc.ProposeItemDiscountsAsync(id, req);
            return Ok(new { message = "Proposed item discounts." });
        }

        [HttpPost("{id:int}/send-admin")]
        public async Task<IActionResult> SendAdmin(int id, [FromQuery] string? message)
        {
            var staffAccountId = GetAccountId();
            await _svc.SendToAdminAsync(id, staffAccountId, message);
            return Ok(new { message = "Sent to admin." });
        }
    }
}
