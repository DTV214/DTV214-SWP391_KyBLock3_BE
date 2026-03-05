using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [Route("api/promotions/accounts")]
    [ApiController]
    public class AccountPromotionsController(IAccountPromotionService accountPromotionService) : BaseApiController
    {
        private readonly IAccountPromotionService _accountPromotionService = accountPromotionService;

        [HttpPost]
        public async Task<IActionResult> SavePromotion([FromBody] AssignPromotionRequest req)
        {
            req.AccountId = GetId();
            await _accountPromotionService.SaveToAccountAsync(req);
            return Ok(new { message = "Lưu mã giảm giá thành công." });
        }
    }
}
