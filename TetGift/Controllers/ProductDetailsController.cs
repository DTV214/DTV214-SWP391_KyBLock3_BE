using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[Route("api/product/details")]
[ApiController]
public class ProductDetailsController(IProductDetailService service) : ControllerBase
{
    private readonly IProductDetailService _service = service;

    [HttpPost]
    public async Task<IActionResult> Create(ProductDetailRequest dto)
    {
        try
        {
            await _service.CreateAsync(dto);
            return Ok(new { message = "Thêm chi tiết thành công và đã cập nhật Unit cho Parent" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> Update(ProductDetailRequest dto)
    {
        try
        {
            await _service.UpdateAsync(dto);
            return Ok(new { message = "Cập nhật thành công và đã cập nhật Unit cho Parent" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = "Xóa chi tiết thành công và đã cập nhật Unit cho Parent" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("parent/{parentId}")]
    public async Task<IActionResult> GetByParent(int parentId)
    {
        var result = await _service.GetByParentIdAsync(parentId);
        return Ok(result);
    }
}