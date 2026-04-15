using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponseDto> CreatePaymentAsync(int orderId, int accountId, string? clientIp = null, string? paymentMethod = null);
    Task<PaymentResultDto> ProcessIpnCallbackAsync(Dictionary<string, string> queryParams);
    Task<PaymentResultDto> ProcessReturnUrlAsync(Dictionary<string, string> queryParams);
    Task<IEnumerable<PaymentHistoryDto>> GetPaymentsByOrderIdAsync(int orderId);
    Task<IEnumerable<PaymentHistoryDto>> GetPaymentsByAccountIdAsync(int accountId);
}
