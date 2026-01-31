using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos;

public class AddToCartRequest
{
    [Required(ErrorMessage = "ProductId là bắt buộc")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Quantity là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int Quantity { get; set; }
}

public class UpdateCartItemRequest
{
    [Required(ErrorMessage = "Quantity là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public int Quantity { get; set; }
}

public class ApplyPromotionRequest
{
    [Required(ErrorMessage = "Mã giảm giá là bắt buộc")]
    public string PromotionCode { get; set; } = null!;
}

public class CartItemResponseDto
{
    public int CartDetailId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Sku { get; set; }
    public decimal? Price { get; set; }
    public int? Quantity { get; set; }
    public decimal SubTotal { get; set; }
    public string? ImageUrl { get; set; }
}

public class CartResponseDto
{
    public int CartId { get; set; }
    public int AccountId { get; set; }
    public List<CartItemResponseDto> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal FinalPrice { get; set; }
    public int ItemCount { get; set; }
    public string? PromotionCode { get; set; }
}
