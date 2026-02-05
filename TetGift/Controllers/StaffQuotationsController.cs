using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [Authorize(Roles = "STAFF,ADMIN")]
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

        // GET /api/staff/quotations/{id}/items/{itemId}/fees
        [HttpGet("{id:int}/items/{itemId:int}/fees")]
        public async Task<IActionResult> GetFeesByItem(int id, int itemId)
        {
            var data = await _svc.GetFeesByQuotationItemAsync(id, itemId);
            return Ok(data);
        }

        //[HttpPut("{id:int}/propose-price")]
        //public async Task<IActionResult> ProposePrice(int id, [FromBody] StaffReviewFeesRequest req)
        //{
        //    req.StaffAccountId = GetAccountId();
        //    await _svc.StaffReviewFeesAsync(id, req);
        //    return Ok(new { message = "Reviewed fees saved." });
        //}
        // POST /api/staff/quotations/{id}/fees
        [HttpPost("{id:int}/fees")]
        public async Task<IActionResult> CreateFee(int id, [FromBody] StaffCreateFeeRequest req)
        {
            req.StaffAccountId = GetAccountId(); // override từ JWT
            await _svc.CreateQuotationFeeAsync(id, req);
            return Ok(new { message = "Fee created." });
        }

        // PUT /api/staff/quotations/{id}/fees/{feeId}
        [HttpPut("{id:int}/fees/{feeId:int}")]
        public async Task<IActionResult> UpdateFee(int id, int feeId, [FromBody] StaffUpdateFeeRequest req)
        {
            req.StaffAccountId = GetAccountId();
            req.QuotationFeeId = feeId;
            await _svc.UpdateQuotationFeeAsync(id, req);
            return Ok(new { message = "Fee updated." });
        }

        // DELETE /api/staff/quotations/{id}/fees/{feeId}
        [HttpDelete("{id:int}/fees/{feeId:int}")]
        public async Task<IActionResult> DeleteFee(int id, int feeId)
        {
            var staffAccountId = GetAccountId();
            await _svc.DeleteQuotationFeeAsync(id, feeId, staffAccountId);
            return Ok(new { message = "Fee deleted." });
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
