using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/promotions")]
    public class PromotionsController(IPromotionService promotionService) : BaseApiController
    {
        private readonly IPromotionService _promotionService = promotionService;

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Create([FromBody] PromotionRequest req)
        {
            var result = await _promotionService.CreateAsync(req);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "ADMIN,STAFF,CUSTOMER")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _promotionService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("limited")]
        [Authorize(Roles = "ADMIN,STAFF,CUSTOMER")]
        public async Task<IActionResult> GetAllLimited()
        {
            var result = await _promotionService.GetAllAsync(true, GetAccountId());
            return Ok(result);
        }

        [HttpGet("limited/public")]
        public async Task<IActionResult> GetAllPublicLimited()
        {
            var result = await _promotionService.GetAllAsync(true);
            return Ok(result);
        }

        [HttpGet("accounts")]
        [Authorize(Roles = "CUSTOMER")]
        public async Task<IActionResult> GetAccountPromos()
        {
            var result = await _promotionService.GetByAccount(GetAccountId());
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _promotionService.DeleteAsync(id);
            return Ok(new { message = "Deleted successfully." });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _promotionService.GetByIdAsync(id);
            return Ok(result);
        }

        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var result = await _promotionService.GetByCodeAsync(code);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Update(int id, [FromBody] PromotionRequest req)
        {
            await _promotionService.UpdateAsync(id, req);
            return Ok(new { message = "Cập nhật thành công." });
        }
    }
}
