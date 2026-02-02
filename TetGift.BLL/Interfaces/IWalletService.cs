using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IWalletService
{
    /// <summary>
    /// Lấy thông tin ví của account
    /// </summary>
    Task<WalletResponseDto> GetWalletByAccountIdAsync(int accountId);

    /// <summary>
    /// Lấy lịch sử giao dịch ví
    /// </summary>
    Task<WalletTransactionHistoryDto> GetWalletTransactionsAsync(int accountId, int? page = 1, int? limit = 20);

    /// <summary>
    /// Tạo ví cho account (tự động, lazy creation)
    /// </summary>
    Task<WalletResponseDto> CreateWalletForAccountAsync(int accountId);

    /// <summary>
    /// Nạp tiền vào ví (tạo Payment → VNPay)
    /// </summary>
    Task<DepositResponseDto> DepositToWalletAsync(int accountId, decimal amount, string? clientIp = null);

    /// <summary>
    /// Xử lý IPN callback từ VNPay khi nạp tiền
    /// </summary>
    Task<PaymentResultDto> ProcessDepositCallbackAsync(Dictionary<string, string> queryParams);

    /// <summary>
    /// Xử lý Return URL từ VNPay khi nạp tiền
    /// </summary>
    Task<PaymentResultDto> ProcessDepositReturnAsync(Dictionary<string, string> queryParams);

    /// <summary>
    /// Thanh toán đơn hàng bằng ví
    /// </summary>
    Task<WalletPaymentResponseDto> PayWithWalletAsync(int accountId, int orderId);

    /// <summary>
    /// Hoàn tiền vào ví khi hủy đơn hàng
    /// </summary>
    Task RefundToWalletAsync(int orderId);


}
