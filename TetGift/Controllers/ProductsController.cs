using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    public ProductsController(IProductService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("account/{accountId}")]
    public async Task<IActionResult> GetByAccount(int accountId)
    {
        try
        {
            var products = await _service.GetByAccountIdAsync(accountId);
            return Ok(products);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("normal")]
    public async Task<IActionResult> CreateNormal(ProductDto dto)
    {
        try
        {
            await _service.CreateNormalAsync(dto);
            return Ok(new { message = "Tạo sản phẩm thường thành công" });
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("custom")]
    public async Task<IActionResult> CreateCustom(ProductDto dto)
    {
        try
        {
            await _service.CreateCustomAsync(dto);
            return Ok(new { message = "Tạo sản phẩm tùy chỉnh thành công" });
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPut("normal")]
    public async Task<IActionResult> UpdateNormal(ProductDto dto)
    {
        try
        {
            var result = await _service.UpdateNormalAsync(dto);
            return Ok(result);
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("custom")]
    public async Task<IActionResult> UpdateCustom(ProductDto dto)
    {
        try
        {
            var result = await _service.UpdateCustomAsync(dto);
            return Ok(result);
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Sản phẩm đã được xóa" });
    }
}