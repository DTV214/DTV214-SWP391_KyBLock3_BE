using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Common.Constraint;
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
    //[Authorize]
    public async Task<IActionResult> GetByAccount()
    {
        int accountId = GetAccountId();
        var products = await _service.GetByAccountIdAsync(accountId);
        return Ok(products);
    }

    /// <summary>
    /// Get customer's draft baskets (work in progress)
    /// </summary>
    [HttpGet("drafts")]
    //[Authorize]
    public async Task<IActionResult> GetDrafts()
    {
        int accountId = GetAccountId();
        var products = await _service.GetByAccountIdAsync(accountId);
        var drafts = products.Where(p => p.Status == ProductStatus.DRAFT);
        return Ok(drafts);
    }

    /// <summary>
    /// Get customer's custom baskets with detailed info
    /// Returns baskets with child products and stock information
    /// </summary>
    [HttpGet("my-baskets")]
    //[Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> GetMyBaskets()
    {
        int accountId = GetAccountId();
        var baskets = await _service.GetCustomerBasketsByAccountIdAsync(accountId);
        return Ok(baskets);
    }

    /// <summary>
    /// Create normal product (Admin/Staff only)
    /// </summary>
    [HttpPost("normal")]
    //[Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> CreateNormal([FromBody] CreateSingleProductRequest dto)
    {
        dto.Accountid = GetAccountId();
        await _service.CreateNormalAsync(dto);
        return Ok(new { message = "Tạo sản phẩm thường thành công" });
    }

    /// <summary>
    /// Create custom gift basket (Customer only)
    /// </summary>
    [HttpPost("custom")]
    //[Authorize(Roles = "CUSTOMER")]
    public async Task<IActionResult> CreateCustom([FromBody] CreateComboProductRequest dto)
    {
        dto.Accountid = GetAccountId();
        await _service.CreateCustomAsync(dto);
        return Ok(new { message = "Tạo giỏ quà tùy chỉnh thành công" });
    }

    /// <summary>
    /// Admin/Staff: Create template basket for config
    /// </summary>
    [HttpPost("templates")]
    //[Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateComboProductRequest dto)
    {
        dto.Accountid = GetAccountId();
        await _service.CreateCustomAsync(dto);
        return Ok(new { message = "Tạo template thành công" });
    }

    /// <summary>
    /// Update normal product (Admin/Staff only)
    /// </summary>
    [HttpPut("normal/{id}")]
    //[Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> UpdateNormal(int id, [FromBody] ProductDto dto)
    {
        dto.Productid = id;
        var result = await _service.UpdateNormalAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Update custom gift basket
    /// Customer can only update their own baskets
    /// Admin cannot update customer baskets (validation in service layer)
    /// </summary>
    [HttpPut("{id}/custom")]
    //[Authorize]
    public async Task<IActionResult> UpdateCustom(int id, [FromBody] UpdateComboProductRequest dto)
    {
        int? accountId = GetAccountId();
        var result = await _service.UpdateCustomAsync(id, dto, accountId);
        return Ok(result);
    }

    /// <summary>
    /// Soft delete product (Admin/Staff only)
    /// </summary>
    [HttpDelete("{id}")]
    //[Authorize(Roles = "ADMIN,STAFF")]
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

    /// <summary>
    /// Get all template baskets (pre-made by admin)
    /// For customer to choose and clone
    /// </summary>
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _service.GetTemplatesAsync();
        return Ok(templates);
    }

    /// <summary>
    /// Clone a template basket to customer's account
    /// Creates a copy of Product + all ProductDetails
    /// </summary>
    [HttpPost("templates/{templateId}/clone")]
    //[Authorize]
    public async Task<IActionResult> CloneTemplate(int templateId, [FromBody] CloneBasketRequest request)
    {
        int customerId = GetAccountId();
        var newBasket = await _service.CloneBasketAsync(templateId, customerId, request.CustomName);
        return Ok(new { 
            message = "Giỏ quà đã được sao chép. Bạn có thể chỉnh sửa trước khi đặt hàng.",
            basketId = newBasket.Productid 
        });
    }

    /// <summary>
    /// Admin: Set a basket product as template for customers to clone
    /// </summary>
    [HttpPost("{id}/set-as-template")]
    //[Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> SetAsTemplate(int id)
    {
        await _service.SetAsTemplateAsync(id);
        return Ok(new { message = "Giỏ quà đã được đặt làm template." });
    }

    /// <summary>
    /// Admin: Remove template status from a product
    /// </summary>
    [HttpDelete("{id}/remove-template")]
    //[Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> RemoveTemplate(int id)
    {
        await _service.RemoveTemplateAsync(id);
        return Ok(new { message = "Đã xóa trạng thái template." });
    }
}