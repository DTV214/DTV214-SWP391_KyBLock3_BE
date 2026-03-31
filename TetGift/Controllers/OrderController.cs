using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.BLL.Common.Constraint;

namespace TetGift.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IInvoiceService _invoiceService;

    public OrderController(IOrderService orderService, IInvoiceService invoiceService)
    {
        _orderService = orderService;
        _invoiceService = invoiceService;
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

    private string GetCurrentUserRole()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? TetGift.BLL.Common.Constraint.UserRole.CUSTOMER;
        return role.ToUpper(); // Normalize to uppercase
    }

    // ========== CUSTOMER ENDPOINTS ==========

    [HttpPost()]
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

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderQueryParameters queryParams)
    {
        queryParams.AccountId = GetCurrentAccountId();
        var result = await _orderService.GetAllOrdersAsync(queryParams);
        return Ok(result);
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

    [HttpDelete("{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var userRole = GetCurrentUserRole();
            var result = await _orderService.CancelOrderAsync(orderId, accountId, userRole);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{orderId}/shipping-info")]
    public async Task<IActionResult> UpdateShippingInfo(int orderId, [FromBody] UpdateOrderShippingRequest request)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var userRole = GetCurrentUserRole();
            var result = await _orderService.UpdateOrderShippingInfoAsync(orderId, accountId, userRole, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet()]
    [Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> GetAllOrders([FromQuery] OrderQueryParameters queryParams)
    {
        var result = await _orderService.GetAllOrdersAsync(queryParams);
        return Ok(result);
    }

    [HttpGet("/{orderId}")]
    [Authorize(Roles = "ADMIN,STAFF")]
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

    [HttpPut("{orderId}/status")]
    [Authorize(Roles = "ADMIN,STAFF,CUSTOMER")]
    public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var userRole = GetCurrentUserRole();
            var result = await _orderService.UpdateOrderStatusAsync(orderId, request, accountId, userRole);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{orderId:int}/allocate-stock")]
    [Authorize(Roles = "ADMIN,STAFF")]
    public async Task<IActionResult> AllocateStock(int orderId)
    {
        try
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out var actorId))
                return Unauthorized(new { message = "Không thể xác định người dùng." });

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value ?? "";

            await _orderService.ForceAllocateStockAsync(orderId, actorId, role);

            return Ok(new { message = "Allocate stock executed." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    
    // ========== INVOICE ENDPOINT ==========

    /// <summary>
    /// Tải xuống hóa đơn PDF cho đơn hàng
    /// GET /api/orders/{orderId}/invoice
    /// </summary>
    [HttpGet("{orderId}/invoice")]
    [Authorize]
    public async Task<IActionResult> DownloadInvoice(int orderId)
    {
        try
        {
            var role = GetCurrentUserRole();
            int? accountId = null;

            // Customer chỉ được tải invoice của mình; Admin/Staff được tải mọi invoice
            if (role != UserRole.ADMIN && role != UserRole.STAFF)
            {
                accountId = GetCurrentAccountId();
            }

            var pdfBytes = await _invoiceService.GenerateInvoicePdfAsync(orderId, accountId);
            return File(pdfBytes, "application/pdf", $"HoaDon_{orderId:D6}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
