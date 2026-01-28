namespace TetGift.BLL.Dtos;

public class ProductDetailResponse
{
    public int Productdetailid { get; set; }
    public int? Productparentid { get; set; }
    public int? Productid { get; set; }
    public string? Productname { get; set; }
    public decimal? Unit { get; set; }
    public decimal? Price { get; set; }
    public string? Imageurl { get; set; }
    public int? Quantity { get; set; }
}