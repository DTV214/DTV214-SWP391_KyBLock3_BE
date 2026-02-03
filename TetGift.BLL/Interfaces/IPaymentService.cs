using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponseDto> CreatePaymentAsync(int orderId, int accountId, string? clientIp = null, string? paymentMethod = null);
    Task<PaymentResultDto> ProcessIpnCallbackAsync(Dictionary<string, string> queryParams);
    Task<PaymentResultDto> ProcessReturnUrlAsync(Dictionary<string, string> queryParams);
    Task<IEnumerable<PaymentHistoryDto>> GetPaymentsByOrderIdAsync(int orderId);
    Task<IEnumerable<PaymentHistoryDto>> GetPaymentsByAccountIdAsync(int accountId);
    
    // Wallet deposit methods
    Task<DepositResponseDto> CreateWalletDepositPaymentAsync(int accountId, decimal amount, string? clientIp = null);
    Task<PaymentResultDto> ProcessWalletDepositIpnAsync(Dictionary<string, string> queryParams);
    Task<PaymentResultDto> ProcessWalletDepositReturnAsync(Dictionary<string, string> queryParams);
}
