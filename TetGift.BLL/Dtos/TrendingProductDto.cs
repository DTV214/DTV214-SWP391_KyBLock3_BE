namespace TetGift.BLL.Dtos;

public class TrendingProductDto
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ImageUrl { get; set; }

    // Tổng số lượng bán ra trong kỳ hiện tại (VD: 7 ngày qua)
    public int TotalSoldInPeriod { get; set; }

    // Tỷ lệ tăng trưởng (%) so với kỳ trước đó
    public decimal GrowthRate { get; set; }

    // Mảng dữ liệu theo từng ngày để Frontend vẽ Line Chart
    public List<TrendDataPointDto> TrendData { get; set; } = new();
}

public class TrendDataPointDto
{
    // Chuỗi ngày tháng hiển thị trên trục X của biểu đồ (VD: "11/04")
    public string Date { get; set; } = string.Empty;

    // Số lượng bán được trong ngày đó (Trục Y)
    public int Quantity { get; set; }
}

public class EventTrendResponseDto
{
    public int RequestedMonth { get; set; }

    // Năm thực tế mà Backend đã lấy dữ liệu (để FE hiển thị nhắc nhở Admin)
    public int DataYear { get; set; }

    public List<CategoryStatDto> TopCategories { get; set; } = new();
    public List<ProductTrendDto> TopProducts { get; set; } = new();
}

public class CategoryStatDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TotalSold { get; set; }
    public decimal Percentage { get; set; }
}

public class ProductTrendDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int TotalSold { get; set; }
}