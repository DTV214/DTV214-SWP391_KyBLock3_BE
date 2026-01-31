namespace TetGift.BLL.Dtos;

public class TimeRangeRequest
{
    public string Period { get; set; } = "day"; // day, month, year
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class RevenueChartDataDto
{
    public string Date { get; set; } = string.Empty; // Format: "2026-01-01" or "2026-01" or "2026"
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class RevenueChartDto
{
    public string Period { get; set; } = string.Empty;
    public List<RevenueChartDataDto> Data { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
}

public class PaymentChannelStatsDto
{
    public string Channel { get; set; } = string.Empty; // VNPAY, COD, etc.
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Percentage { get; set; }
}

public class PaymentChannelStatisticsDto
{
    public List<PaymentChannelStatsDto> Data { get; set; } = new();
    public PaymentChannelStatsDto Total { get; set; } = new();
}

public class AbandonedCartItemDto
{
    public int CartId { get; set; }
    public int AccountId { get; set; }
    public decimal TotalValue { get; set; }
    public int ItemCount { get; set; }
}

public class AbandonedCartDto
{
    public int TotalCarts { get; set; }
    public decimal TotalValue { get; set; }
    public decimal AverageCartValue { get; set; }
    public List<AbandonedCartItemDto> Carts { get; set; } = new();
}

public class OrderStatusStatsDto
{
    public int Total { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
}

public class DashboardSummaryDto
{
    public RevenueChartDto Revenue { get; set; } = new();
    public PaymentChannelStatisticsDto PaymentChannels { get; set; } = new();
    public AbandonedCartDto AbandonedCarts { get; set; } = new();
    public OrderStatusStatsDto Orders { get; set; } = new();
}
