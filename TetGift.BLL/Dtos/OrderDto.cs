using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos;

public class CreateOrderRequest
{
    [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
    [StringLength(255, ErrorMessage = "Tên khách hàng tối đa 255 ký tự")]
    public string CustomerName { get; set; } = null!;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
    public string CustomerPhone { get; set; } = null!;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(255, ErrorMessage = "Email tối đa 255 ký tự")]
    public string CustomerEmail { get; set; } = null!;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
    public string CustomerAddress { get; set; } = null!;

    public string? Note { get; set; }
    public string? PromotionCode { get; set; }

    // ===== VAT =====
    public bool RequireVatInvoice { get; set; } = false;

    [StringLength(255)]
    public string? VatCompanyName { get; set; }

    [StringLength(50)]
    public string? VatCompanyTaxCode { get; set; }

    [StringLength(500)]
    public string? VatCompanyAddress { get; set; }

    [EmailAddress]
    [StringLength(255)]
    public string? VatInvoiceEmail { get; set; }
}

public class UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    public string Status { get; set; } = null!;
}

public class UpdateOrderShippingRequest
{
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    public string? Note { get; set; }
}

public class OrderDetailResponseDto
{
    public int OrderDetailId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Sku { get; set; }
    public int? Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal Amount { get; set; }
    public string? ImageUrl { get; set; }
    public List<ProductDetailResponse>? ProductDetails { get; set; }
}

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public int AccountId { get; set; }
    public DateTime? OrderDateTime { get; set; }

    // Giữ logic cũ
    public decimal TotalPrice { get; set; }           // tổng line trước giảm giá
    public decimal? DiscountValue { get; set; }
    public decimal FinalPrice { get; set; }           // = Order.Totalprice (sau promotion, chưa VAT)

    public decimal? ActualRevenue { get; set; }
    public string? Status { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    public string? Note { get; set; }
    public string? PromotionCode { get; set; }
    public DateTime? ShippedDate { get; set; }
    public int? QuotationId { get; set; }
    public bool IsFromQuotation => QuotationId.HasValue;

    // ===== VAT =====
    public bool RequireVatInvoice { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal FinalPayableAmount { get; set; }   // = FinalPrice + VatAmount
    public string? VatCompanyName { get; set; }
    public string? VatCompanyTaxCode { get; set; }
    public string? VatCompanyAddress { get; set; }
    public string? VatInvoiceEmail { get; set; }

    public FeedbackResponseDto? Feedback { get; set; }

    public List<OrderDetailResponseDto> Items { get; set; } = new();
}