using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
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

    // ========== CUSTOMER ENDPOINTS ==========

    [HttpPost("create-from-cart")]
    public async Task<IActionResult> CreateOrderFromCart([FromBody] CreateOrderRequest request)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var result = await _orderService.CreateOrderFromCartAsync(accountId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var result = await _orderService.GetOrdersByAccountIdAsync(accountId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrderById(int orderId)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var result = await _orderService.GetOrderByIdAsync(orderId, accountId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ========== ADMIN ENDPOINTS ==========

    [HttpGet("admin/all")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAllOrders([FromQuery] string? status = null)
    {
        try
        {
            var result = await _orderService.GetAllOrdersAsync(status);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("admin/{orderId}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetOrderByIdForAdmin(int orderId)
    {
        try
        {
            var result = await _orderService.GetOrderByIdForAdminAsync(orderId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("admin/{orderId}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            var result = await _orderService.UpdateOrderStatusAsync(orderId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
