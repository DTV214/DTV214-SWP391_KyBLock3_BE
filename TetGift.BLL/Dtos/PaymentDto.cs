using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos;

public class CreatePaymentRequest
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }

    public string? PaymentMethod { get; set; }
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

    // backward compatibility
    public decimal Amount { get; set; }

    public decimal BaseAmount { get; set; }           // = Order.Totalprice
    public decimal VatAmount { get; set; }
    public decimal FinalPayableAmount { get; set; }   // = Payment.Amount
    public bool RequireVatInvoice { get; set; }

    public string PaymentUrl { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? CreatedDate { get; set; }
}

public class PaymentResultDto
{
    public bool Success { get; set; }
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public string? TransactionNo { get; set; }
    public string Message { get; set; } = null!;

    // backward compatibility
    public decimal Amount { get; set; }

    public decimal BaseAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal FinalPayableAmount { get; set; }
    public bool RequireVatInvoice { get; set; }

    public string? BankCode { get; set; }
    public string? ResponseCode { get; set; }
}

public class PaymentHistoryDto
{
    public int PaymentId { get; set; }
    public int? OrderId { get; set; }
    public int? WalletId { get; set; }

    // backward compatibility
    public decimal Amount { get; set; }

    public decimal BaseAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal FinalPayableAmount { get; set; }
    public bool RequireVatInvoice { get; set; }

    public string Status { get; set; } = null!;
    public string? Type { get; set; }
    public string? PaymentMethod { get; set; }
    public bool IsPayOnline { get; set; }
    public string? TransactionNo { get; set; }
    public DateTime? CreatedDate { get; set; }
}