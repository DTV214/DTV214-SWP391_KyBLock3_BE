namespace TetGift.BLL.Dtos;

public class ProductDto
{
    public int Productid { get; set; }
    public int? Categoryid { get; set; }
    public int? Configid { get; set; }
    public int? Accountid { get; set; }
    public string? Sku { get; set; }
    public string? Productname { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Price { get; set; }
    public string? Status { get; set; }
    public decimal? Unit { get; set; }
    public bool IsCustom { get; set; } = false;
}