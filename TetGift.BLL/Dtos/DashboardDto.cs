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
    public decimal RevenueBeforeDiscount { get; set; }
    public int OrderCount { get; set; }
}

public class RevenueChartDto
{
    public string Period { get; set; } = string.Empty;
    public List<RevenueChartDataDto> Data { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal TotalRevenueBeforeDiscount { get; set; } = 0;
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

public class AccountChartDataDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AccountChartDto
{
    public string Period { get; set; } = string.Empty;
    public List<AccountChartDataDto> Data { get; set; } = new();
    public int TotalCount { get; set; }
}

public class DashboardSummaryDto
{
    public RevenueChartDto Revenue { get; set; } = new();
    public PaymentChannelStatisticsDto PaymentChannels { get; set; } = new();
    public AbandonedCartDto AbandonedCarts { get; set; } = new();
    public OrderStatusStatsDto Orders { get; set; } = new();
    public AccountChartDto NewAccounts { get; set; } = new();

    // Thống kê tài khoản & Tỉ lệ chuyển đổi
    public int TotalCustomerAccounts { get; set; }
    public int AccountsWithOrders { get; set; }
    public decimal ConversionRate { get; set; }
    public List<HighlightProductDto> TopProducts { get; set; } = new();
}

public class CustomerOrderStatisticsDto
{
    public int AccountId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public int TotalOrders { get; set; }
    public int SuccessfulOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal TotalSpentAllTime { get; set; }
    public double SuccessRate { get; set; } // (SuccessfulOrders / TotalOrders) * 100
}
