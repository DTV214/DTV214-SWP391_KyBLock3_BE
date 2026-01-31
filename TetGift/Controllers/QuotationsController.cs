using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [Authorize] // user phải đăng nhập
    [Route("api/quotations")]
    public class QuotationsController : BaseApiController
    {
        private readonly IQuotationService _svc;

        public QuotationsController(IQuotationService svc)
        {
            _svc = svc;
        }

        // Flow 1: create manual quotation
        [HttpPost("manual")]
        public async Task<IActionResult> CreateManual([FromBody] QuotationCreateManualRequest req)
        {
            // lấy accountId từ JWT, user không cần nhập
            req.AccountId = GetAccountId();

            var result = await _svc.CreateManualAsync(req);
            return Ok(result);
        }

        // edit draft/submitted (only allowed before staff start review)
        [HttpPut("{id:int}/draft")]
        public async Task<IActionResult> UpdateDraft(int id, [FromBody] QuotationUpdateDraftRequest req)
        {
            req.AccountId = GetAccountId();

            var result = await _svc.UpdateDraftAsync(id, req);
            return Ok(result);
        }

        // GET /api/quotations?status=SUBMITTED
        [HttpGet]
        public async Task<IActionResult> GetMyQuotations([FromQuery] string? status)
        {
            var accountId = GetAccountId();

            var data = await _svc.GetCustomerQuotationsAsync(accountId, status);
            return Ok(data);
        }

        // GET /api/quotations/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetMyQuotationDetail(int id)
        {
            var accountId = GetAccountId();

            var data = await _svc.GetCustomerQuotationDetailAsync(id, accountId);
            return Ok(data);
        }

        [HttpPost("{id:int}/submit")]
        public async Task<IActionResult> Submit(int id, [FromBody] QuotationSubmitRequest req)
        {
            req.AccountId = GetAccountId();

            await _svc.SubmitAsync(id, req);
            return Ok(new { message = "Submitted." });
        }

        [HttpPost("{id:int}/customer-accept")]
        public async Task<IActionResult> CustomerAccept(int id, [FromBody] CustomerDecisionRequest req)
        {
            req.AccountId = GetAccountId();

            await _svc.CustomerAcceptAsync(id, req);
            return Ok(new { message = "Accepted. Order created if configured." });
        }

        [HttpPost("{id:int}/customer-reject")]
        public async Task<IActionResult> CustomerReject(int id, [FromBody] CustomerDecisionRequest req)
        {
            req.AccountId = GetAccountId();

            await _svc.CustomerRejectAsync(id, req);
            return Ok(new { message = "Rejected. Sent back to staff." });
        }

        // Flow 2: recommend
        [HttpPost("recommend/request")]
        public async Task<IActionResult> RequestRecommend([FromBody] QuotationRecommendRequest req)
        {
            req.AccountId = GetAccountId();

            var preview = await _svc.RequestRecommendAsync(req);
            return Ok(preview);
        }

        [HttpPost("{id:int}/recommend/confirm")]
        public async Task<IActionResult> ConfirmRecommend(int id, [FromBody] QuotationRecommendConfirmRequest req)
        {
            req.AccountId = GetAccountId();

            await _svc.CustomerConfirmRecommendAsync(id, req);
            return Ok(new { message = "Confirmed. Order created if configured." });
        }
    }
}
