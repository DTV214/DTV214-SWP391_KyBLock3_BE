using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;

    public DashboardService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<RevenueChartDto> GetRevenueByTimeRangeAsync(TimeRangeRequest request)
    {
        var orderRepo = _uow.GetRepository<Order>();
        var paymentRepo = _uow.GetRepository<Payment>();

        // Lấy tất cả orders trong khoảng thời gian với Payment và Promotion
        var ordersQuery = orderRepo.Entities
            .Include(o => o.Promotion)
            .Include(o => o.Payments)
            .AsQueryable();

        // Filter theo thời gian
        if (request.StartDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.Orderdatetime >= request.StartDate.Value);
        }
        if (request.EndDate.HasValue)
        {
            var endDate = request.EndDate.Value.AddDays(1); // Include end date
            ordersQuery = ordersQuery.Where(o => o.Orderdatetime < endDate);
        }

        // Chỉ tính orders đã thanh toán thành công
        // Orders có Payment với Status = "SUCCESS" hoặc Order Status = "CONFIRMED", "PROCESSING", "SHIPPED", "DELIVERED"
        var paidOrders = await ordersQuery
            .Where(o => o.Payments.Any(p => p.Status == PaymentStatus.SUCCESS) ||
                       (o.Status != null && new[] { OrderStatus.CONFIRMED, OrderStatus.PROCESSING, OrderStatus.SHIPPED, OrderStatus.DELIVERED }.Contains(o.Status)))
            .ToListAsync();

        // Tính revenue theo period
        var revenueData = new List<RevenueChartDataDto>();
        var period = request.Period.ToLower();

        if (period == "day")
        {
            var grouped = paidOrders
                .GroupBy(o => o.Orderdatetime?.Date)
                .Where(g => g.Key.HasValue)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in grouped)
            {
                var date = group.Key!.Value;
                var orders = group.ToList();
                var revenue = orders.Sum(o => CalculateFinalPrice(o));
                var orderCount = orders.Count;

                revenueData.Add(new RevenueChartDataDto
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Revenue = revenue,
                    OrderCount = orderCount
                });
            }
        }
        else if (period == "month")
        {
            var grouped = paidOrders
                .GroupBy(o => o.Orderdatetime.HasValue 
                    ? new DateTime(o.Orderdatetime.Value.Year, o.Orderdatetime.Value.Month, 1)
                    : (DateTime?)null)
                .Where(g => g.Key.HasValue)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in grouped)
            {
                var date = group.Key!.Value;
                var orders = group.ToList();
                var revenue = orders.Sum(o => CalculateFinalPrice(o));
                var orderCount = orders.Count;

                revenueData.Add(new RevenueChartDataDto
                {
                    Date = date.ToString("yyyy-MM"),
                    Revenue = revenue,
                    OrderCount = orderCount
                });
            }
        }
        else if (period == "year")
        {
            var grouped = paidOrders
                .GroupBy(o => o.Orderdatetime?.Year)
                .Where(g => g.Key.HasValue)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in grouped)
            {
                var year = group.Key!.Value;
                var orders = group.ToList();
                var revenue = orders.Sum(o => CalculateFinalPrice(o));
                var orderCount = orders.Count;

                revenueData.Add(new RevenueChartDataDto
                {
                    Date = year.ToString(),
                    Revenue = revenue,
                    OrderCount = orderCount
                });
            }
        }

        return new RevenueChartDto
        {
            Period = period,
            Data = revenueData,
            TotalRevenue = revenueData.Sum(d => d.Revenue),
            TotalOrders = revenueData.Sum(d => d.OrderCount)
        };
    }

    public async Task<PaymentChannelStatisticsDto> GetPaymentChannelStatisticsAsync(TimeRangeRequest? request = null)
    {
        var paymentRepo = _uow.GetRepository<Payment>();
        var orderRepo = _uow.GetRepository<Order>();

        // Lấy payments thành công
        var paymentsQuery = paymentRepo.Entities
            .Include(p => p.Order)
            .Where(p => p.Status == PaymentStatus.SUCCESS && p.Type != null)
            .AsQueryable();

        // Filter theo thời gian từ Order
        if (request != null)
        {
            if (request.StartDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p => p.Order != null && p.Order.Orderdatetime >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value.AddDays(1);
                paymentsQuery = paymentsQuery.Where(p => p.Order != null && p.Order.Orderdatetime < endDate);
            }
        }

        var payments = await paymentsQuery.ToListAsync();

        // Group by Payment Type
        var grouped = payments
            .GroupBy(p => p.Type ?? "UNKNOWN")
            .ToList();

        var channelStats = new List<PaymentChannelStatsDto>();
        var totalCount = payments.Count;
        var totalAmount = payments.Sum(p => p.Amount ?? 0);

        foreach (var group in grouped)
        {
            var channel = group.Key;
            var count = group.Count();
            var amount = group.Sum(p => p.Amount ?? 0);
            var percentage = totalAmount > 0 ? (amount / totalAmount) * 100 : 0;

            channelStats.Add(new PaymentChannelStatsDto
            {
                Channel = channel,
                Count = count,
                TotalAmount = amount,
                Percentage = percentage
            });
        }

        return new PaymentChannelStatisticsDto
        {
            Data = channelStats.OrderByDescending(s => s.TotalAmount).ToList(),
            Total = new PaymentChannelStatsDto
            {
                Channel = "TOTAL",
                Count = totalCount,
                TotalAmount = totalAmount,
                Percentage = 100
            }
        };
    }

    public async Task<AbandonedCartDto> GetAbandonedCartsAsync(int? days = null)
    {
        var cartRepo = _uow.GetRepository<Cart>();
        var orderRepo = _uow.GetRepository<Order>();
        var cartDetailRepo = _uow.GetRepository<CartDetail>();

        // Lấy tất cả carts có items
        var cartsWithItems = await cartRepo.Entities
            .Include(c => c.CartDetails)
            .Where(c => c.CartDetails != null && c.CartDetails.Any())
            .ToListAsync();

        // Lấy tất cả orders để check cart nào đã tạo order
        var allOrders = await orderRepo.GetAllAsync();
        var orderAccountIds = allOrders
            .Where(o => o.Accountid.HasValue)
            .Select(o => o.Accountid!.Value)
            .Distinct()
            .ToHashSet();

        // Filter: Cart có items nhưng không có Order tương ứng
        // Hoặc nếu có days: Cart không tạo Order trong X ngày (cần logic khác vì Cart không có CreatedDate)
        var abandonedCarts = cartsWithItems
            .Where(c => !orderAccountIds.Contains(c.Accountid ?? 0))
            .ToList();

        // Nếu có filter theo days, cần check Order datetime
        if (days.HasValue)
        {
            var cutoffDate = DateTime.UtcNow.AddHours(7).AddDays(-days.Value);
            var recentOrderAccountIds = allOrders
                .Where(o => o.Accountid.HasValue && o.Orderdatetime >= cutoffDate)
                .Select(o => o.Accountid!.Value)
                .Distinct()
                .ToHashSet();

            // Cart không có Order trong X ngày
            abandonedCarts = cartsWithItems
                .Where(c => !recentOrderAccountIds.Contains(c.Accountid ?? 0))
                .ToList();
        }

        var cartItems = new List<AbandonedCartItemDto>();
        foreach (var cart in abandonedCarts)
        {
            var itemCount = cart.CartDetails?.Count ?? 0;
            var cartTotalValue = cart.Totalprice ?? 0;

            cartItems.Add(new AbandonedCartItemDto
            {
                CartId = cart.Cartid,
                AccountId = cart.Accountid ?? 0,
                TotalValue = cartTotalValue,
                ItemCount = itemCount
            });
        }

        var totalCarts = abandonedCarts.Count;
        var totalValue = abandonedCarts.Sum(c => c.Totalprice ?? 0);
        var averageValue = totalCarts > 0 ? totalValue / totalCarts : 0;

        return new AbandonedCartDto
        {
            TotalCarts = totalCarts,
            TotalValue = totalValue,
            AverageCartValue = averageValue,
            Carts = cartItems.OrderByDescending(c => c.TotalValue).Take(50).ToList() // Limit to top 50
        };
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(TimeRangeRequest? request = null)
    {
        var revenueRequest = request ?? new TimeRangeRequest { Period = "month" };
        var revenue = await GetRevenueByTimeRangeAsync(revenueRequest);
        var paymentChannels = await GetPaymentChannelStatisticsAsync(request);
        var abandonedCarts = await GetAbandonedCartsAsync();

        // Get order status statistics
        var orderRepo = _uow.GetRepository<Order>();
        var ordersQuery = orderRepo.Entities.AsQueryable();

        if (request != null)
        {
            if (request.StartDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.Orderdatetime >= request.StartDate.Value);
            }
            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value.AddDays(1);
                ordersQuery = ordersQuery.Where(o => o.Orderdatetime < endDate);
            }
        }

        var allOrders = await ordersQuery.ToListAsync();
        var orderStatusStats = new Dictionary<string, int>();
        foreach (var order in allOrders)
        {
            var status = order.Status ?? "UNKNOWN";
            if (!orderStatusStats.ContainsKey(status))
                orderStatusStats[status] = 0;
            orderStatusStats[status]++;
        }

        return new DashboardSummaryDto
        {
            Revenue = revenue,
            PaymentChannels = paymentChannels,
            AbandonedCarts = abandonedCarts,
            Orders = new OrderStatusStatsDto
            {
                Total = allOrders.Count,
                ByStatus = orderStatusStats
            }
        };
    }

    private decimal CalculateFinalPrice(Order order)
    {
        var totalPrice = order.Totalprice ?? 0;
        var discountValue = order.Promotion?.Discountvalue ?? 0;
        var finalPrice = totalPrice - discountValue;
        return finalPrice > 0 ? finalPrice : 0;
    }
}
