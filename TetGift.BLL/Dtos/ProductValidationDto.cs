namespace TetGift.BLL.Dtos;

public class ProductValidationDto
{
    public int Productid { get; set; }
    public string? Productname { get; set; }
    public bool IsValid { get; set; }
    public List<string> Warnings { get; set; } = [];
    public Dictionary<string, CategoryRequirementDto> CategoryStatus { get; set; } = [];
    public decimal? CurrentWeight { get; set; }
    public decimal? MaxWeight { get; set; }
    public bool WeightExceeded { get; set; }
}

public class CategoryRequirementDto
{
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int CurrentCount { get; set; }
    public int RequiredCount { get; set; }
    public bool IsSatisfied => CurrentCount >= RequiredCount;
}
