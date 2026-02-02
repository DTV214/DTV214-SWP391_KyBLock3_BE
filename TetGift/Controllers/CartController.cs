using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[ApiController]
[Route("api/carts")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    private int GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out var accountId))
        {
            throw new UnauthorizedAccessException("Không thể xác định người dùng.");
        }
        return accountId;
    }


    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var result = await _cartService.GetCartByAccountIdAsync(accountId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCartItemCount()
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var count = await _cartService.GetCartItemCountAsync(accountId);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var result = await _cartService.AddItemAsync(accountId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("items/{cartDetailId}")]
    public async Task<IActionResult> UpdateItem(int cartDetailId, [FromBody] UpdateCartItemRequest request)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            // Validate cartDetailId thuộc về accountId này
            await _cartService.ValidateCartDetailOwnershipAsync(cartDetailId, accountId);
            var result = await _cartService.UpdateItemAsync(cartDetailId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("items/{cartDetailId}")]
    public async Task<IActionResult> RemoveItem(int cartDetailId)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            // Validate cartDetailId thuộc về accountId này
            await _cartService.ValidateCartDetailOwnershipAsync(cartDetailId, accountId);
            var result = await _cartService.RemoveItemAsync(cartDetailId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("apply-promotion")]
    public async Task<IActionResult> ApplyPromotion([FromBody] ApplyPromotionRequest request)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var result = await _cartService.ApplyPromotionAsync(accountId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        try
        {
            var accountId = GetCurrentAccountId();
            await _cartService.ClearCartAsync(accountId);
            return Ok(new { message = "Đã xóa toàn bộ giỏ hàng" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
