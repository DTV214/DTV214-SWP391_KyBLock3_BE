using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [ApiController]
    [Route("api/promotions")]
    public class PromotionsController(IPromotionService promotionService) : ControllerBase
    {
        private readonly IPromotionService _promotionService = promotionService;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PromotionRequest req)
        {
            var result = await _promotionService.CreateAsync(req);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _promotionService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("/limited")]
        public async Task<IActionResult> GetAllLimited()
        {
            var result = await _promotionService.GetAllAsync(true);
            return Ok(result);
        }

        [HttpGet("/unlimited")]
        public async Task<IActionResult> GetAllUnLimited()
        {
            var result = await _promotionService.GetAllAsync(false);
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
        public async Task<IActionResult> Update(int id, [FromBody] PromotionRequest req)
        {
            await _promotionService.UpdateAsync(id, req);
            return Ok(new { message = "Cập nhật thành công." });
        }
    }
}
