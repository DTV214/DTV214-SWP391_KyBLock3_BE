namespace TetGift.BLL.Dtos;

public class ProductQueryParameters
{
    public string? Search { get; set; }
    public List<int>? Categories { get; set; }
    public string? Sort { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int? PageNumber { get; set; } = 1;
    public int? PageSize { get; set; } = 10;
}