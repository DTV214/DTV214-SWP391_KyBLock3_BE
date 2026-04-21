using System;
using System.Collections.Generic;

namespace TetGift.BLL.Dtos;

public class DashboardHighlightsDto
{
    public HighlightCustomerDto? TopSpender { get; set; }
    public HighlightCustomerDto? MostFrequentBuyer { get; set; }
    public HighlightCustomerDto? TopCanceler { get; set; }
    public HighlightProductDto? TopSellingProduct { get; set; }
    public HighlightProductDto? UnderperformingProduct { get; set; }
    public CancellationStatsDto CancellationStats { get; set; } = new();
    public decimal AverageOrderValue { get; set; }
    public AbandonedCartValueDto AbandonedCartValue { get; set; } = new();
    public List<InactiveCustomerDto> InactiveCustomers { get; set; } = new();
}

public class HighlightCustomerDto
{
    public int AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal TotalValue { get; set; } 
    public int OrderCount { get; set; }
}

public class HighlightProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal Price { get; set; }
    public decimal ImportPrice { get; set; }
    public decimal TotalProfit { get; set; }
}

public class CancellationStatsDto
{
    public int CancelledOrders { get; set; }
    public int ValidOrders { get; set; } // Successful + Processing
    public double CancellationRate { get; set; } // %
}

public class AbandonedCartValueDto
{
    public int CartCount { get; set; }
    public decimal TotalLostValue { get; set; }
}

public class InactiveCustomerDto
{
    public int AccountId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public int DaysSinceLastOrder { get; set; }
}
