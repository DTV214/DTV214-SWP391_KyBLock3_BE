using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderService _orderService;

    public PaymentController(IPaymentService paymentService, IOrderService orderService)
    {
        _paymentService = paymentService;
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

    private string GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',');
            if (ips.Length > 0)
                return ips[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }

    // ========== CUSTOMER ENDPOINTS ==========

    [HttpPost()]
    [Authorize]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var clientIp = GetClientIpAddress();
            var paymentMethod = request.PaymentMethod ?? "VNPAY";
            var result = await _paymentService.CreatePaymentAsync(request.OrderId, accountId, clientIp, paymentMethod);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("order/{orderId}")]
    [Authorize]
    public async Task<IActionResult> GetPaymentsByOrder(int orderId)
    {
        try
        {
            var result = await _paymentService.GetPaymentsByOrderIdAsync(orderId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my-payments")]
    [Authorize]
    public async Task<IActionResult> GetMyPayments()
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var result = await _paymentService.GetPaymentsByAccountIdAsync(accountId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ========== VNPAY CALLBACK ENDPOINTS (Không cần [Authorize]) ==========

    [HttpGet("vnpay-ipn")]
    [HttpPost("vnpay-ipn")]
    public async Task<IActionResult> VnPayIpn()
    {
        try
        {
            var queryParams = new Dictionary<string, string>();

            if (Request.Method == "POST" && Request.HasFormContentType)
            {
                foreach (var key in Request.Form.Keys)
                {
                    queryParams[key] = Request.Form[key].ToString();
                }
            }
            else
            {
                queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            }

            var result = await _paymentService.ProcessIpnCallbackAsync(queryParams);

            if (result.Success && result.OrderId > 0)
            {
                await _orderService.TryAllocateStockAfterPaymentAsync(result.OrderId);
            }

            var response = new
            {
                RspCode = result.ResponseCode ?? "99",
                Message = result.Message
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return Ok(new { RspCode = "99", Message = ex.Message });
        }
    }

    [HttpGet("vnpay-return")]
    public async Task<IActionResult> VnPayReturn()
    {
        try
        {
            var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            var result = await _paymentService.ProcessReturnUrlAsync(queryParams);

            if (result.Success && result.OrderId > 0)
            {
                await _orderService.TryAllocateStockAfterPaymentAsync(result.OrderId);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}