using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos;

public class CreatePaymentRequest
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }

    public string? PaymentMethod { get; set; } // VNPAY, WALLET (optional, default VNPAY)
}

public class CreateWalletDepositPaymentRequest
{
    [Required(ErrorMessage = "Số tiền nạp là bắt buộc")]
    [Range(10000, 50000000, ErrorMessage = "Số tiền nạp phải từ 10,000 VNĐ đến 50,000,000 VNĐ")]
    public decimal Amount { get; set; }
}

public class PaymentResponseDto
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentUrl { get; set; } = null!;
    public string Status { get; set; } = null!;
}

public class PaymentResultDto
{
    public bool Success { get; set; }
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public string? TransactionNo { get; set; }
    public string Message { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? BankCode { get; set; }
    public string? ResponseCode { get; set; }
}

public class PaymentHistoryDto
{
    public int PaymentId { get; set; }
    public int? OrderId { get; set; }
    public int? WalletId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!;
    public string? Type { get; set; } // ORDER_PAYMENT, WALLET_DEPOSIT
    public string? PaymentMethod { get; set; } // VNPAY, WALLET
    public bool IsPayOnline { get; set; }
    public string? TransactionNo { get; set; }
    public DateTime? CreatedDate { get; set; }
}
