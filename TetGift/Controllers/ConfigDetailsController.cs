using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[Route("api/config/details")]
[ApiController]
public class ConfigDetailsController(IConfigDetailService service) : ControllerBase
{
    private readonly IConfigDetailService _service = service;

    [HttpGet("/config/{configId}")]
    public async Task<IActionResult> GetByConfig(int configId)
    {
        var details = await _service.GetByConfigAsync(configId);
        return Ok(details);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ConfigDetailDto dto)
    {
        try
        {
            await _service.CreateAsync(dto);
            return Ok(new { message = "Thêm chi tiết cấu hình thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update(ConfigDetailDto dto)
    {
        try
        {
            await _service.UpdateAsync(dto);
            return Ok(new { message = "Cập nhật chi tiết cấu hình thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Xóa chi tiết thành công" });
    }
}