using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;

namespace TetGift.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
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

    /// <summary>
    /// Xem thông tin ví (số dư, status)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWallet()
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var result = await _walletService.GetWalletByAccountIdAsync(accountId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lịch sử giao dịch ví
    /// </summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int? page = 1, [FromQuery] int? limit = 20)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var result = await _walletService.GetWalletTransactionsAsync(accountId, page, limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Yêu cầu nạp tiền vào ví (tạo Payment → VNPay)
    /// </summary>
    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] CreateDepositRequest request)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var clientIp = GetClientIpAddress();
            var result = await _walletService.DepositToWalletAsync(accountId, request.Amount, clientIp);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
