using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController(IProductService service) : ControllerBase
{
    private readonly IProductService _service = service;

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
        var products = await _service.GetByAccountIdAsync(accountId);
        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductDto dto)
    {
        if (dto.IsCustom)
        {
            await _service.CreateCustomAsync(dto);
            return Ok(new { message = "Tạo sản phẩm tùy chỉnh thành công" });
        }
        else
        {
            await _service.CreateNormalAsync(dto);
            return Ok(new { message = "Tạo sản phẩm thường thành công" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(ProductDto dto, int id)
    {
        dto.Productid = id;

        if (dto.IsCustom)
        {
            var result = await _service.UpdateCustomAsync(dto);
            return Ok(result);
        }
        else
        {
            var result = await _service.UpdateNormalAsync(dto);
            return Ok(result);
        }

    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Sản phẩm đã được xóa" });
    }

    /// <summary>
    /// Get validation status for a product (basket) against its ConfigDetail requirements
    /// Returns warnings if requirements are not met, category status, and weight validation
    /// </summary>
    [HttpGet("{id}/validation-status")]
    public async Task<IActionResult> GetValidationStatus(int id)
    {
        var status = await _service.GetProductValidationStatus(id);
        return Ok(status);
    }
}