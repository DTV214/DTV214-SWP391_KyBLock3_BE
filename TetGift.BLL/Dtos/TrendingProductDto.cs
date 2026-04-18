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