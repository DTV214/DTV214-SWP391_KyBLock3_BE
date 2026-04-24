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

        var ordersQuery = orderRepo.Entities
            .Include(o => o.Promotion)
            .Include(o => o.Payments)
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
            .AsQueryable();

        // Filter theo thời gian
        if (request.StartDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.Orderdatetime >= request.StartDate.Value);
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

        var revenueData = new List<RevenueChartDataDto>();
        var period = (request.Period ?? "day").ToLower();

        IEnumerable<IGrouping<object?, Order>> groups = period switch
        {
            "month" => paidOrders.GroupBy(o => o.Orderdatetime.HasValue
                        ? (object?)new DateTime(o.Orderdatetime.Value.Year, o.Orderdatetime.Value.Month, 1)
                        : null),
            "year" => paidOrders.GroupBy(o => o.Orderdatetime?.Year as object),
            _ => paidOrders.GroupBy(o => o.Orderdatetime?.Date as object)
        };

        foreach (var g in groups.Where(g => g.Key != null).OrderBy(g => g.Key))
        {
            decimal groupRevenueAfter = 0m;
            decimal groupRevenueBefore = 0m;
            var orders = g.ToList();

            foreach (var order in orders)
            {
                // compute sum of detail amounts (fallback to price * qty)
                decimal sumDetails = 0m;
                if (order.OrderDetails != null)
                {
                    foreach (var od in order.OrderDetails)
                    {
                        sumDetails += od.Amount ?? ((od.Product?.Price ?? 0m) * (od.Quantity ?? 0));
                    }
                }

                // revenue before discount = sum of details
                groupRevenueBefore += sumDetails;

                // revenue after discount: prefer stored Totalprice (final stored amount), otherwise fallback to sumDetails
                decimal finalPaid = order.Totalprice ?? sumDetails;
                groupRevenueAfter += finalPaid;
            }

            string label = period switch
            {
                "month" => ((DateTime)g.Key!).ToString("yyyy-MM"),
                "year" => g.Key!.ToString() ?? string.Empty,
                _ => ((DateTime)g.Key!).ToString("yyyy-MM-dd")
            };

            revenueData.Add(new RevenueChartDataDto
            {
                Date = label,
                Revenue = groupRevenueAfter,
                RevenueBeforeDiscount = groupRevenueBefore,
                OrderCount = orders.Count
            });
        }

        return new RevenueChartDto
        {
            Period = period,
            Data = revenueData,
            TotalRevenue = revenueData.Sum(d => d.Revenue),
            TotalRevenueBeforeDiscount = revenueData.Sum(d => d.RevenueBeforeDiscount),
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

    public async Task<AccountChartDto> GetAccountStatisticsAsync(TimeRangeRequest request)
    {
        var accountRepo = _uow.GetRepository<Account>();
        var query = accountRepo.Entities.AsQueryable();

        if (request.StartDate.HasValue)
        {
            query = query.Where(a => a.DayCreate >= request.StartDate.Value);
        }
        if (request.EndDate.HasValue)
        {
            var endDate = request.EndDate.Value.AddDays(1);
            query = query.Where(a => a.DayCreate < endDate);
        }

        var accounts = await query.ToListAsync();
        var period = request.Period?.ToLower() ?? "day";
        var chartData = new List<AccountChartDataDto>();

        if (period == "day")
        {
            chartData = accounts
                .GroupBy(a => a.DayCreate?.Date)
                .Where(g => g.Key.HasValue)
                .OrderBy(g => g.Key)
                .Select(g => new AccountChartDataDto
                {
                    Date = g.Key!.Value.ToString("yyyy-MM-dd"),
                    Count = g.Count()
                }).ToList();
        }
        else if (period == "week")
        {
            // Group by start of week (Sunday)
            chartData = accounts
                .GroupBy(a => a.DayCreate.HasValue ? a.DayCreate.Value.Date.AddDays(-(int)a.DayCreate.Value.DayOfWeek) : (DateTime?)null)
                .Where(g => g.Key.HasValue)
                .OrderBy(g => g.Key)
                .Select(g => new AccountChartDataDto
                {
                    Date = g.Key!.Value.ToString("yyyy-MM-dd"),
                    Count = g.Count()
                }).ToList();
        }
        else if (period == "month")
        {
            chartData = accounts
                .GroupBy(a => a.DayCreate.HasValue ? new DateTime(a.DayCreate.Value.Year, a.DayCreate.Value.Month, 1) : (DateTime?)null)
                .Where(g => g.Key.HasValue)
                .OrderBy(g => g.Key)
                .Select(g => new AccountChartDataDto
                {
                    Date = g.Key!.Value.ToString("yyyy-MM"),
                    Count = g.Count()
                }).ToList();
        }
        else if (period == "year")
        {
            chartData = accounts
                .GroupBy(a => a.DayCreate?.Year)
                .Where(g => g.Key.HasValue)
                .OrderBy(g => g.Key)
                .Select(g => new AccountChartDataDto
                {
                    Date = g.Key!.Value.ToString(),
                    Count = g.Count()
                }).ToList();
        }

        return new AccountChartDto
        {
            Period = period,
            Data = chartData,
            TotalCount = chartData.Sum(d => d.Count)
        };
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(TimeRangeRequest? request = null)
    {
        var statsRequest = request ?? new TimeRangeRequest { Period = "month" };
        var revenue = await GetRevenueByTimeRangeAsync(statsRequest);
        var paymentChannels = await GetPaymentChannelStatisticsAsync(request);
        var abandonedCarts = await GetAbandonedCartsAsync();
        var newAccounts = await GetAccountStatisticsAsync(statsRequest);

        // Get order status statistics
        var orderRepo = _uow.GetRepository<Order>();
        var ordersQuery = orderRepo.Entities
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .AsQueryable();

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

        // --- Thống kê tỉ lệ chuyển đổi (Conversion Rate) ---
        var accountRepo = _uow.GetRepository<Account>();
        var customerAccounts = await accountRepo.Entities
            .Where(a => a.Role == "CUSTOMER")
            .Include(a => a.Orders)
            .ToListAsync();

        var paidStatuses = new[] { 
            "DELIVERED", "CONFIRMED", "PROCESSING", "SHIPPED", "PAID_WAITING_STOCK" 
        };

        var totalCustomerAccounts = customerAccounts.Count;
        var accountsWithOrders = customerAccounts.Count(a => a.Orders.Any(o => o.Status != null && paidStatuses.Contains(o.Status.ToUpper())));
        var conversionRate = totalCustomerAccounts > 0 
            ? Math.Round((decimal)accountsWithOrders / totalCustomerAccounts * 100, 2) 
            : 0;

        // --- Thống kê Top 10 sản phẩm bán chạy ---
        var topProducts = allOrders
            .Where(o => o.Status != null && paidStatuses.Contains(o.Status.ToUpper()))
            .SelectMany(o => o.OrderDetails)
            .Where(od => od.Productid.HasValue && od.Product != null)
            .GroupBy(od => od.Productid!.Value)
            .Select(g => new HighlightProductDto
            {
                ProductId = g.Key,
                ProductName = g.First().Product!.Productname ?? $"Product {g.Key}",
                ImageUrl = g.First().Product!.ImageUrl,
                TotalQuantity = g.Sum(od => od.Quantity ?? 0),
                TotalRevenue = g.Sum(od => (od.Quantity ?? 0) * (od.Product!.Price ?? 0)),
                Price = g.First().Product!.Price ?? 0,
                ImportPrice = g.First().Product!.ImportPrice ?? 0,
                TotalProfit = g.Sum(od => (od.Quantity ?? 0) * ((od.Product!.Price ?? 0) - (od.Product!.ImportPrice ?? 0)))
            })
            .OrderByDescending(p => p.TotalQuantity)
            .Take(10)
            .ToList();

        return new DashboardSummaryDto
        {
            Revenue = revenue,
            PaymentChannels = paymentChannels,
            AbandonedCarts = abandonedCarts,
            Orders = new OrderStatusStatsDto
            {
                Total = allOrders.Count,
                ByStatus = orderStatusStats
            },
            NewAccounts = newAccounts,
            TotalCustomerAccounts = totalCustomerAccounts,
            AccountsWithOrders = accountsWithOrders,
            ConversionRate = conversionRate,
            TopProducts = topProducts
        };
    }

    private decimal CalculateFinalPrice(Order order)
    {
        var totalPrice = order.Totalprice ?? 0;
        var discountValue = order.Promotion?.Discountvalue ?? 0;
        var finalPrice = totalPrice - discountValue;
        return finalPrice > 0 ? finalPrice : 0;
    }

    public async Task<RevenueChartDto> GetActualRevenueByTimeRangeAsync(TimeRangeRequest request)
    {
        var orderRepo = _uow.GetRepository<Order>();

        var ordersQuery = orderRepo.Entities
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                    .ThenInclude(p => p.ProductDetailProductparents)
                        .ThenInclude(pd => pd.Product)
            .Include(o => o.Payments)
            .Include(o => o.Promotion)
            .AsQueryable();

        if (request.StartDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.Orderdatetime >= request.StartDate.Value);
        if (request.EndDate.HasValue)
        {
            var endDate = request.EndDate.Value.AddDays(1);
            ordersQuery = ordersQuery.Where(o => o.Orderdatetime < endDate);
        }

        // Only consider orders that were actually paid / confirmed or later statuses
        var paidStatuses = new[] { OrderStatus.CONFIRMED, OrderStatus.PROCESSING, OrderStatus.SHIPPED, OrderStatus.DELIVERED };
        var paidOrders = await ordersQuery
            .Where(o => o.Payments.Any(p => p.Status == PaymentStatus.SUCCESS) ||
                        (o.Status != null && paidStatuses.Contains(o.Status)))
            .ToListAsync();

        var period = (request.Period ?? "day").ToLower();
        var data = new List<RevenueChartDataDto>();

        IEnumerable<IGrouping<object?, Order>> groups = period switch
        {
            "month" => paidOrders
                .GroupBy(o => o.Orderdatetime.HasValue
                    ? (object?)new DateTime(o.Orderdatetime.Value.Year, o.Orderdatetime.Value.Month, 1)
                    : null),
            "year" => paidOrders
                .GroupBy(o => o.Orderdatetime?.Year as object),
            _ => paidOrders
                .GroupBy(o => o.Orderdatetime?.Date as object)
        };

        foreach (var g in groups.Where(g => g.Key != null).OrderBy(g => g.Key))
        {
            decimal groupRevenue = 0m;
            var orders = g.ToList();
            foreach (var order in orders)
            {
                if (order.ActualRevenue.HasValue)
                {
                    groupRevenue += order.ActualRevenue.Value;
                    continue;
                }

                // fallback compute actual revenue:
                // compute totalBeforeDiscount (sum of order detail amounts)
                decimal totalBeforeDiscount = 0m;
                if (order.OrderDetails != null)
                {
                    foreach (var od in order.OrderDetails)
                        totalBeforeDiscount += od.Amount ?? ((od.Product?.Price ?? 0m) * (od.Quantity ?? 0));
                }

                decimal finalPaid = order.Totalprice ?? totalBeforeDiscount;

                // compute total cost (import price)
                decimal totalCost = 0m;
                if (order.OrderDetails != null)
                {
                    foreach (var od in order.OrderDetails)
                    {
                        var qty = od.Quantity ?? 0;
                        var product = od.Product;

                        if (product != null && product.Configid != null && product.ProductDetailProductparents != null && product.ProductDetailProductparents.Any())
                        {
                            foreach (var child in product.ProductDetailProductparents)
                            {
                                var childImport = child.Product?.ImportPrice ?? 0m;
                                var childQty = child.Quantity ?? 0;
                                totalCost += childImport * childQty * qty;
                            }
                        }
                        else
                        {
                            var importPrice = product?.ImportPrice ?? 0m;
                            totalCost += importPrice * qty;
                        }
                    }
                }

                groupRevenue += (finalPaid - totalCost);
            }

            string label = period switch
            {
                "month" => ((DateTime)g.Key!).ToString("yyyy-MM"),
                "year" => g.Key!.ToString() ?? string.Empty,
                _ => ((DateTime)g.Key!).ToString("yyyy-MM-dd")
            };

            data.Add(new RevenueChartDataDto
            {
                Date = label,
                Revenue = groupRevenue,
                OrderCount = orders.Count
            });
        }

        return new RevenueChartDto
        {
            Period = period,
            Data = data,
            TotalRevenue = data.Sum(d => d.Revenue),
            TotalOrders = data.Sum(d => d.OrderCount)
        };
    }

    public async Task<List<CustomerOrderStatisticsDto>> GetCustomerOrderStatisticsAsync(TimeRangeRequest? request = null)
    {
        var accountRepo = _uow.GetRepository<Account>();
        
        // Lấy tất cả tài khoản có vai trò CUSTOMER hoặc có ít nhất 1 đơn hàng
        var accounts = await accountRepo.Entities
            .Where(a => a.Role == "CUSTOMER" || a.Orders.Any())
            .Include(a => a.Orders)
            .ToListAsync();

        var stats = new List<CustomerOrderStatisticsDto>();
        
        var paidStatuses = new[] { 
            OrderStatus.DELIVERED, 
            OrderStatus.CONFIRMED, 
            OrderStatus.PROCESSING, 
            OrderStatus.SHIPPED, 
            OrderStatus.PAID_WAITING_STOCK 
        };

        foreach (var c in accounts)
        {
            // Tính tổng chi tiêu TẤT CẢ các khoảng thời gian (All-Time) trước khi lọc (bao gồm các đơn đang xử lý/đã thanh toán)
            var totalSpentAllTime = c.Orders
                .Where(o => o.Status != null && paidStatuses.Contains(o.Status.ToUpper()))
                .Sum(o => o.Totalprice ?? 0);

            // Lọc đơn hàng theo thời gian
            var ordersInTimeRange = c.Orders.AsEnumerable();

            if (request != null)
            {
                if (request.StartDate.HasValue)
                {
                    ordersInTimeRange = ordersInTimeRange.Where(o => o.Orderdatetime >= request.StartDate.Value);
                }
                if (request.EndDate.HasValue)
                {
                    var endDate = request.EndDate.Value.AddDays(1);
                    ordersInTimeRange = ordersInTimeRange.Where(o => o.Orderdatetime < endDate);
                }
            }

            var ordersList = ordersInTimeRange.ToList();

            // CHỈ lấy khách đã từng có đơn hàng (>= 1).
            // Nếu khoảng thời gian này không có đơn hàng nào, bỏ qua.
            if (!ordersList.Any()) continue;

            var totalOrders = ordersList.Count;
            // Success includes both delivered and in-progress (non-cancelled) orders for efficiency metric
            var successfulOrders = ordersList.Count(o => 
                new[] { 
                    OrderStatus.DELIVERED, 
                    OrderStatus.CONFIRMED, 
                    OrderStatus.PROCESSING, 
                    OrderStatus.SHIPPED, 
                    OrderStatus.PAID_WAITING_STOCK,
                    OrderStatus.PENDING
                }.Contains((o.Status ?? "").ToUpper())
            );
            var cancelledOrders = ordersList.Count(o => (o.Status ?? "").ToUpper() == OrderStatus.CANCELLED);
            
            // Các trạng thái đang được xử lý hoặc đã thanh toán nhưng chưa hoàn tất
            var processingOrders = ordersList.Count(o => 
                (o.Status ?? "").ToUpper() == OrderStatus.CONFIRMED || 
                (o.Status ?? "").ToUpper() == OrderStatus.PROCESSING || 
                (o.Status ?? "").ToUpper() == OrderStatus.SHIPPED || 
                (o.Status ?? "").ToUpper() == OrderStatus.PAID_WAITING_STOCK);

            var totalSpent = ordersList
                .Where(o => o.Status != null && paidStatuses.Contains(o.Status.ToUpper()))
                .Sum(o => o.Totalprice ?? 0);

            var successRate = totalOrders > 0 
                ? Math.Round((double)successfulOrders / totalOrders * 100, 2) 
                : 0;

            stats.Add(new CustomerOrderStatisticsDto
            {
                AccountId = c.Accountid,
                FullName = c.Fullname,
                Email = c.Email,
                TotalOrders = totalOrders,
                SuccessfulOrders = successfulOrders,
                CancelledOrders = cancelledOrders,
                ProcessingOrders = processingOrders,
                TotalSpent = totalSpent,
                TotalSpentAllTime = totalSpentAllTime,
                SuccessRate = successRate
            });
        }

        // Sắp xếp theo tổng chi tiêu giảm dần
        return stats.OrderByDescending(s => s.TotalSpent).ToList();
    }

    public async Task<DashboardHighlightsDto> GetDashboardInsightsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var result = new DashboardHighlightsDto();
        
        var orderRepo = _uow.GetRepository<Order>();
        var accountRepo = _uow.GetRepository<Account>();
        var productRepo = _uow.GetRepository<Product>();
        var cartRepo = _uow.GetRepository<Cart>();

        // 1. Lọc đơn hàng theo thời gian
        var ordersQuery = orderRepo.Entities
            .Include(o => o.Account)
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .Include(o => o.Payments)
            .Include(o => o.Promotion)
            .AsQueryable();

        if (startDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.Orderdatetime >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            var end = endDate.Value.AddDays(1);
            ordersQuery = ordersQuery.Where(o => o.Orderdatetime < end);
        }

        var ordersList = await ordersQuery.ToListAsync();

        var paidStatuses = new[] { 
            OrderStatus.DELIVERED, 
            OrderStatus.CONFIRMED, 
            OrderStatus.PROCESSING, 
            OrderStatus.SHIPPED, 
            OrderStatus.PAID_WAITING_STOCK 
        };

        var validOrders = ordersList.Where(o => o.Status != null && paidStatuses.Contains(o.Status.ToUpper())).ToList();
        var cancelledOrders = ordersList.Where(o => (o.Status ?? "").ToUpper() == OrderStatus.CANCELLED).ToList();

        // 2. Cancellation Stats
        result.CancellationStats = new CancellationStatsDto
        {
            CancelledOrders = cancelledOrders.Count,
            ValidOrders = validOrders.Count,
            CancellationRate = (cancelledOrders.Count + validOrders.Count) > 0 
                ? Math.Round((double)cancelledOrders.Count / (cancelledOrders.Count + validOrders.Count) * 100, 2) 
                : 0
        };

        // 3. Average Order Value
        result.AverageOrderValue = validOrders.Count > 0 
            ? validOrders.Average(o => CalculateFinalPrice(o)) 
            : 0;

        // 4. Customer Highlights
        // Tính toán trên toàn bộ danh sách khách hàng có trong các đơn hợp lệ hoặc hủy
        var customerGroups = ordersList
            .Where(o => o.Accountid.HasValue && o.Account != null)
            .GroupBy(o => o.Accountid!.Value)
            .Select(g => new
            {
                AccountId = g.Key,
                Account = g.First().Account!,
                TotalOrders = g.Count(o => o.Status != null && paidStatuses.Contains(o.Status.ToUpper())),
                TotalSpent = g.Where(o => o.Status != null && paidStatuses.Contains(o.Status.ToUpper())).Sum(o => CalculateFinalPrice(o)),
                TotalCancelled = g.Count(o => (o.Status ?? "").ToUpper() == OrderStatus.CANCELLED)
            }).ToList();

        if (customerGroups.Any())
        {
            var topSpender = customerGroups.OrderByDescending(c => c.TotalSpent).FirstOrDefault();
            if (topSpender != null && topSpender.TotalSpent > 0)
            {
                result.TopSpender = new HighlightCustomerDto
                {
                    AccountId = topSpender.AccountId,
                    FullName = topSpender.Account.Fullname ?? topSpender.Account.Username,
                    Email = topSpender.Account.Email ?? "",
                    TotalValue = topSpender.TotalSpent,
                    OrderCount = topSpender.TotalOrders
                };
            }

            var mostFrequent = customerGroups.OrderByDescending(c => c.TotalOrders).FirstOrDefault();
            if (mostFrequent != null && mostFrequent.TotalOrders > 0)
            {
                result.MostFrequentBuyer = new HighlightCustomerDto
                {
                    AccountId = mostFrequent.AccountId,
                    FullName = mostFrequent.Account.Fullname ?? mostFrequent.Account.Username,
                    Email = mostFrequent.Account.Email ?? "",
                    TotalValue = mostFrequent.TotalSpent,
                    OrderCount = mostFrequent.TotalOrders
                };
            }

            var topCanceler = customerGroups.OrderByDescending(c => c.TotalCancelled).FirstOrDefault();
            if (topCanceler != null && topCanceler.TotalCancelled > 0)
            {
                result.TopCanceler = new HighlightCustomerDto
                {
                    AccountId = topCanceler.AccountId,
                    FullName = topCanceler.Account.Fullname ?? topCanceler.Account.Username,
                    Email = topCanceler.Account.Email ?? "",
                    TotalValue = topCanceler.TotalCancelled, // Đếm số đơn hủy
                    OrderCount = topCanceler.TotalOrders
                };
            }
        }

        // 5. Product Highlights (Chỉ đếm các đơn hợp lệ)
        var productGroups = validOrders
            .SelectMany(o => o.OrderDetails)
            .Where(od => od.Productid.HasValue && od.Product != null)
            .GroupBy(od => od.Productid!.Value)
            .Select(g => new
            {
                ProductId = g.Key,
                Product = g.First().Product!,
                TotalQuantity = g.Sum(od => od.Quantity ?? 0),
                TotalRevenue = g.Sum(od => (od.Quantity ?? 0) * (od.Product!.Price ?? 0)) // Tạm tính doanh thu theo giá gốc
            }).ToList();

        if (productGroups.Any())
        {
            var topSelling = productGroups.OrderByDescending(p => p.TotalQuantity).First();
            result.TopSellingProduct = new HighlightProductDto
            {
                ProductId = topSelling.ProductId,
                ProductName = topSelling.Product.Productname ?? $"Product {topSelling.ProductId}",
                ImageUrl = topSelling.Product.ImageUrl,
                TotalQuantity = topSelling.TotalQuantity,
                TotalRevenue = topSelling.TotalRevenue
            };
        }

        // Tạm tính Sản phẩm bán ế (Dựa trên tất cả sản phẩm đang ACTIVE)
        var activeProducts = await productRepo.Entities.Where(p => p.Status == "ACTIVE").ToListAsync();
        
        var worstSelling = activeProducts
            .Select(p => new
            {
                Product = p,
                TotalQuantity = productGroups.FirstOrDefault(pg => pg.ProductId == p.Productid)?.TotalQuantity ?? 0,
                TotalRevenue = productGroups.FirstOrDefault(pg => pg.ProductId == p.Productid)?.TotalRevenue ?? 0
            })
            .OrderBy(p => p.TotalQuantity)
            .FirstOrDefault();

        if (worstSelling != null)
        {
            result.UnderperformingProduct = new HighlightProductDto
            {
                ProductId = worstSelling.Product.Productid,
                ProductName = worstSelling.Product.Productname ?? "",
                ImageUrl = worstSelling.Product.ImageUrl,
                TotalQuantity = worstSelling.TotalQuantity,
                TotalRevenue = worstSelling.TotalRevenue
            };
        }


        // 6. Abandoned Cart Value

        var cartsWithItems = await cartRepo.Entities
            .Include(c => c.CartDetails)
            .Where(c => c.CartDetails != null && c.CartDetails.Any())
            .ToListAsync();
        
        var orderAccountIdsInPeriod = validOrders.Where(o => o.Accountid.HasValue).Select(o => o.Accountid!.Value).ToHashSet();
        var abandonedCarts = cartsWithItems.Where(c => c.Accountid.HasValue && !orderAccountIdsInPeriod.Contains(c.Accountid.Value)).ToList();

        result.AbandonedCartValue = new AbandonedCartValueDto
        {
            CartCount = abandonedCarts.Count,
            TotalLostValue = abandonedCarts.Sum(c => c.Totalprice ?? 0)
        };

        // 7. Inactive Customers (>= 7 days)
        var sevenDaysAgo = DateTime.UtcNow.AddHours(7).AddDays(-7);
        var allCustomerAccounts = await accountRepo.Entities
            .Where(a => a.Role == "CUSTOMER")
            .Include(a => a.Orders)
            .ToListAsync();

        result.InactiveCustomers = allCustomerAccounts
            .Where(a => a.Orders.Any() && a.Orders.Max(o => o.Orderdatetime) < sevenDaysAgo)
            .Select(a => {
                var lastOrder = a.Orders.Max(o => o.Orderdatetime);
                return new InactiveCustomerDto
                {
                    AccountId = a.Accountid,
                    FullName = a.Fullname ?? a.Username,
                    Email = a.Email ?? "",
                    Phone = a.Phone,
                    LastOrderDate = lastOrder,
                    DaysSinceLastOrder = lastOrder.HasValue ? (int)(DateTime.UtcNow.AddHours(7) - lastOrder.Value).TotalDays : 0
                };
            })
            .OrderByDescending(c => c.DaysSinceLastOrder)
            .Take(20)
            .ToList();

        return result;
    }
}
