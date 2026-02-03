using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductCategoriesController(IProductCategoryService service) : ControllerBase
{
    private readonly IProductCategoryService _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(ProductCategoryDto dto)
    {
        try
        {
            await _service.CreateAsync(dto);
            return Ok(new { message = "Tạo danh mục thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Update(ProductCategoryDto dto)
    {
        try
        {
            await _service.UpdateAsync(dto);
            return Ok(new { message = "Cập nhật danh mục thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Xóa danh mục thành công" });
    }
}