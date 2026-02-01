using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos;

public class WalletResponseDto
{
    public int WalletId { get; set; }
    public int AccountId { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class WalletTransactionDto
{
    public int TransactionId { get; set; }
    public int WalletId { get; set; }
    public int? OrderId { get; set; }
    public string TransactionType { get; set; } = null!; // DEPOSIT, PAYMENT, REFUND
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Status { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDepositRequest
{
    [Required(ErrorMessage = "Số tiền nạp là bắt buộc")]
    [Range(10000, 50000000, ErrorMessage = "Số tiền nạp phải từ 10,000 VNĐ đến 50,000,000 VNĐ")]
    public decimal Amount { get; set; }
}

public class DepositResponseDto
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentUrl { get; set; } = null!;
    public string Status { get; set; } = null!;
}

public class WalletPaymentRequest
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }
}

public class WalletTransactionHistoryDto
{
    public List<WalletTransactionDto> Transactions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
}

public class WalletPaymentResponseDto
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!;
    public string Message { get; set; } = null!;
}

public class WalletInsufficientBalanceDto
{
    public decimal CurrentBalance { get; set; }
    public decimal RequiredAmount { get; set; }
    public decimal Shortage { get; set; }
    public string Message { get; set; } = null!;
}
