using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos;

public class CreatePaymentRequest
{
    [Required(ErrorMessage = "OrderId là bắt buộc")]
    public int OrderId { get; set; }
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
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!;
    public string? Type { get; set; }
    public bool IsPayOnline { get; set; }
    public string? TransactionNo { get; set; }
    public DateTime? CreatedDate { get; set; }
}
