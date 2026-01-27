using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductConfigsController(IProductConfigService service) : ControllerBase
    {
        private readonly IProductConfigService _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id) => Ok(await _service.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Create(ProductConfigDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _service.CreateAsync(dto);
            return Ok(new { message = "Thêm mới thành công" });
        }

        [HttpPut]
        public async Task<IActionResult> Update(ProductConfigDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _service.UpdateAsync(dto);
                return Ok(new { message = "Cập nhật thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = "Xóa thành công" });
        }
    }
}
