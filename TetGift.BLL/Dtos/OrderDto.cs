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
}

public class UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    public string Status { get; set; } = null!;
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
}

public class OrderResponseDto
{
    public int OrderId { get; set; }
    public int AccountId { get; set; }
    public DateTime? OrderDateTime { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal FinalPrice { get; set; }
    public string? Status { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    public string? Note { get; set; }
    public string? PromotionCode { get; set; }
    public List<OrderDetailResponseDto> Items { get; set; } = new();
}
