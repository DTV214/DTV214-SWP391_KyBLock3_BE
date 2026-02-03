using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[Route("api/products")]
[ApiController]
public class ProductsController(IProductService service) : BaseApiController
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

    [HttpGet("account")]
    [Authorize]
    public async Task<IActionResult> GetByAccount()
    {
        int accountId = GetAccountId();
        var products = await _service.GetByAccountIdAsync(accountId);
        return Ok(products);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(ProductDto dto)
    {
        int accountId = GetAccountId();
        dto.Accountid = accountId;
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
    [Authorize]
    public async Task<IActionResult> Update(ProductDto dto, int id)
    {
        dto.Productid = id;
        dto.Accountid = GetAccountId();

        if (dto.IsCustom)
        {
            var result = await _service.UpdateCustomAsync(dto, GetRole().Equals("Customer"));
            return Ok(result);
        }
        else
        {
            var result = await _service.UpdateNormalAsync(dto);
            return Ok(result);
        }

    }

    [HttpDelete("{id}")]
    [Authorize]
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