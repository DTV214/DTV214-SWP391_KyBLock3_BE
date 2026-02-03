using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductDetailsController(IProductDetailService service) : ControllerBase
{
    private readonly IProductDetailService _service = service;

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(ProductDetailRequest dto)
    {
        await _service.CreateAsync(dto);
        return Ok(new { message = "Thêm chi tiết thành công và đã cập nhật Unit cho Parent" });
    }

    [HttpPut]
    [Authorize]
    public async Task<IActionResult> Update(ProductDetailRequest dto)
    {
        await _service.UpdateAsync(dto);
        return Ok(new { message = "Cập nhật thành công và đã cập nhật Unit cho Parent" });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(new { message = "Xóa chi tiết thành công và đã cập nhật Unit cho Parent" });
    }

    [HttpGet("parent/{parentId}")]
    public async Task<IActionResult> GetByParent(int parentId)
    {
        var result = await _service.GetByParentIdAsync(parentId);
        return Ok(result);
    }
}