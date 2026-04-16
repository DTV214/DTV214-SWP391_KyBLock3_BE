using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Interfaces;
using TetGift.DAL.Entities;

namespace TetGift.BLL.Services
{
    public class DashboardComparisonService : IDashboardComparisonService
    {
        private readonly IUnitOfWork _uow;
        private const string PaymentSuccessStatus = "SUCCESS";

        public DashboardComparisonService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        //month
        public async Task<MonthlyComparisonChartDto> GetMonthlyOrderRevenueComparisonAsync(MonthComparisonRequest request)
        {
            ValidateRequest(request);

            var (baseStart, baseEnd, compareStart, compareEnd, compareYear, compareMonth) = ResolveMonths(request);

            var minStart = baseStart < compareStart ? baseStart : compareStart;
            var maxEnd = baseEnd > compareEnd ? baseEnd : compareEnd;

            var orderRepo = _uow.GetRepository<Order>();

            var orders = await orderRepo.Entities
                .Include(o => o.Payments)
                .AsNoTracking()
                .Where(o => o.Orderdatetime.HasValue &&
                            o.Orderdatetime.Value >= minStart &&
                            o.Orderdatetime.Value < maxEnd)
                .ToListAsync();

            var validOrders = orders
                .Where(IsQualifiedRevenueOrder)
                .ToList();

            return BuildComparisonResult(
                metric: "ORDER_REVENUE",
                orders: validOrders,
                baseYear: request.Year,
                baseMonth: request.Month,
                compareYear: compareYear,
                compareMonth: compareMonth,
                selector: o => o.Totalprice ?? 0m
            );
        }

        public async Task<MonthlyComparisonChartDto> GetMonthlyActualRevenueComparisonAsync(MonthComparisonRequest request)
        {
            ValidateRequest(request);

            var (baseStart, baseEnd, compareStart, compareEnd, compareYear, compareMonth) = ResolveMonths(request);

            var minStart = baseStart < compareStart ? baseStart : compareStart;
            var maxEnd = baseEnd > compareEnd ? baseEnd : compareEnd;

            var orderRepo = _uow.GetRepository<Order>();

            var orders = await orderRepo.Entities
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductDetailProductparents)
                            .ThenInclude(pd => pd.Product)
                .AsNoTracking()
                .Where(o => o.Orderdatetime.HasValue &&
                            o.Orderdatetime.Value >= minStart &&
                            o.Orderdatetime.Value < maxEnd)
                .ToListAsync();

            var validOrders = orders
                .Where(IsQualifiedRevenueOrder)
                .ToList();

            return BuildComparisonResult(
                metric: "ACTUAL_REVENUE",
                orders: validOrders,
                baseYear: request.Year,
                baseMonth: request.Month,
                compareYear: compareYear,
                compareMonth: compareMonth,
                selector: CalculateActualRevenue
            );
        }

        private static void ValidateRequest(MonthComparisonRequest request)
        {
            if (request.Year <= 0)
                throw new Exception("Year không hợp lệ.");

            if (request.Month < 1 || request.Month > 12)
                throw new Exception("Month phải nằm trong khoảng 1-12.");

            if (request.CompareMonth.HasValue &&
                (request.CompareMonth.Value < 1 || request.CompareMonth.Value > 12))
                throw new Exception("CompareMonth phải nằm trong khoảng 1-12.");
        }

        private static (DateTime baseStart, DateTime baseEnd, DateTime compareStart, DateTime compareEnd, int compareYear, int compareMonth)
            ResolveMonths(MonthComparisonRequest request)
        {
            var baseStart = new DateTime(request.Year, request.Month, 1);
            var baseEnd = baseStart.AddMonths(1);

            int compareYear;
            int compareMonth;

            if (request.CompareMonth.HasValue)
            {
                compareMonth = request.CompareMonth.Value;

                if (request.CompareYear.HasValue)
                {
                    compareYear = request.CompareYear.Value;
                }
                else
                {
                    // Nếu không truyền compareYear thì tự suy luận:
                    // ví dụ base là tháng 1 mà compare là 12 => năm trước
                    compareYear = compareMonth > request.Month ? request.Year - 1 : request.Year;
                }
            }
            else
            {
                var prevMonth = baseStart.AddMonths(-1);
                compareYear = request.CompareYear ?? prevMonth.Year;
                compareMonth = prevMonth.Month;
            }

            var compareStart = new DateTime(compareYear, compareMonth, 1);
            var compareEnd = compareStart.AddMonths(1);

            return (baseStart, baseEnd, compareStart, compareEnd, compareYear, compareMonth);
        }

        private static bool IsQualifiedRevenueOrder(Order order)
        {
            var status = (order.Status ?? string.Empty).ToUpperInvariant();
            //Hard code status
            var excludedStatuses = new[]
            {
                "CANCELLED",
                "CANCEL_REQUESTED"
            };

            if (excludedStatuses.Contains(status))
                return false;

            var hasSuccessPayment = order.Payments != null &&
                                    order.Payments.Any(p => (p.Status ?? string.Empty).ToUpperInvariant() == "SUCCESS");

            var validStatuses = new[]
            {
                OrderStatus.PAID_WAITING_STOCK,
                OrderStatus.CONFIRMED,
                OrderStatus.PROCESSING,
                OrderStatus.SHIPPED,
                OrderStatus.DELIVERED
            };

            return hasSuccessPayment || validStatuses.Contains(status);
        }

        private MonthlyComparisonChartDto BuildComparisonResult(
            string metric,
            List<Order> orders,
            int baseYear,
            int baseMonth,
            int compareYear,
            int compareMonth,
            Func<Order, decimal> selector)
        {
            var baseDays = DateTime.DaysInMonth(baseYear, baseMonth);
            var compareDays = DateTime.DaysInMonth(compareYear, compareMonth);
            var axisMaxDays = Math.Max(baseDays, compareDays);

            var baseSeries = BuildSeries(orders, baseYear, baseMonth, axisMaxDays, selector);
            var compareSeries = BuildSeries(orders, compareYear, compareMonth, axisMaxDays, selector);

            return new MonthlyComparisonChartDto
            {
                Metric = metric,
                XAxisDays = Enumerable.Range(1, axisMaxDays).ToList(),
                BaseMonth = baseSeries,
                CompareMonth = compareSeries
            };
        }

        private static MonthlySeriesDto BuildSeries(
            List<Order> orders,
            int year,
            int month,
            int axisMaxDays,
            Func<Order, decimal> selector)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var dailyMap = Enumerable.Range(1, axisMaxDays)
                .ToDictionary(day => day, _ => 0m);

            var monthOrders = orders
                .Where(o => o.Orderdatetime.HasValue &&
                            o.Orderdatetime.Value.Year == year &&
                            o.Orderdatetime.Value.Month == month)
                .ToList();

            foreach (var order in monthOrders)
            {
                var day = order.Orderdatetime!.Value.Day;
                dailyMap[day] += selector(order);
            }

            return new MonthlySeriesDto
            {
                Year = year,
                Month = month,
                Label = $"{year}-{month:D2}",
                DaysInMonth = daysInMonth,
                Total = monthOrders.Sum(selector),
                Data = Enumerable.Range(1, axisMaxDays)
                    .Select(day => new DailyComparisonPointDto
                    {
                        Day = day,
                        Value = day <= daysInMonth ? dailyMap[day] : 0m
                    })
                    .ToList()
            };
        }

        private static decimal CalculateActualRevenue(Order order)
        {
            if (order.ActualRevenue.HasValue)
                return order.ActualRevenue.Value;

            decimal totalBeforeDiscount = 0m;

            if (order.OrderDetails != null)
            {
                foreach (var od in order.OrderDetails)
                {
                    totalBeforeDiscount += od.Amount ?? ((od.Product?.Price ?? 0m) * (od.Quantity ?? 0));
                }
            }

            var finalPaid = order.Totalprice ?? totalBeforeDiscount;

            decimal totalCost = 0m;

            if (order.OrderDetails != null)
            {
                foreach (var od in order.OrderDetails)
                {
                    var qty = od.Quantity ?? 0;
                    var product = od.Product;

                    if (product != null &&
                        product.Configid != null &&
                        product.ProductDetailProductparents != null &&
                        product.ProductDetailProductparents.Any())
                    {
                        foreach (var child in product.ProductDetailProductparents)
                        {
                            var childImportPrice = child.Product?.ImportPrice ?? 0m;
                            var childQty = child.Quantity ?? 0;
                            totalCost += childImportPrice * childQty * qty;
                        }
                    }
                    else
                    {
                        var importPrice = product?.ImportPrice ?? 0m;
                        totalCost += importPrice * qty;
                    }
                }
            }

            return finalPaid - totalCost;
        }

        //Year
        public async Task<YearComparisonChartDto> GetYearlyOrderRevenueComparisonAsync(YearComparisonRequest request)
        {
            ValidateYearComparisonRequest(request);

            var compareYear = request.CompareYear ?? (request.Year - 1);

            var startYear = Math.Min(request.Year, compareYear);
            var endYear = Math.Max(request.Year, compareYear);

            var start = new DateTime(startYear, 1, 1);
            var end = new DateTime(endYear + 1, 1, 1);

            var orderRepo = _uow.GetRepository<Order>();

            var orders = await orderRepo.Entities
                .Include(o => o.Payments)
                .AsNoTracking()
                .Where(o => o.Orderdatetime.HasValue
                            && o.Orderdatetime.Value >= start
                            && o.Orderdatetime.Value < end)
                .ToListAsync();

            var validOrders = orders
                .Where(IsQualifiedRevenueOrder)
                .ToList();

            return BuildYearComparisonResult(
                metric: "ORDER_REVENUE",
                orders: validOrders,
                baseYear: request.Year,
                compareYear: compareYear,
                selector: o => o.Totalprice ?? 0m
            );
        }

        public async Task<YearComparisonChartDto> GetYearlyActualRevenueComparisonAsync(YearComparisonRequest request)
        {
            ValidateYearComparisonRequest(request);

            var compareYear = request.CompareYear ?? (request.Year - 1);

            var startYear = Math.Min(request.Year, compareYear);
            var endYear = Math.Max(request.Year, compareYear);

            var start = new DateTime(startYear, 1, 1);
            var end = new DateTime(endYear + 1, 1, 1);

            var orderRepo = _uow.GetRepository<Order>();

            var orders = await orderRepo.Entities
                .Include(o => o.Payments)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductDetailProductparents)
                            .ThenInclude(pd => pd.Product)
                .AsNoTracking()
                .Where(o => o.Orderdatetime.HasValue
                            && o.Orderdatetime.Value >= start
                            && o.Orderdatetime.Value < end)
                .ToListAsync();

            var validOrders = orders
                .Where(IsQualifiedRevenueOrder)
                .ToList();

            return BuildYearComparisonResult(
                metric: "ACTUAL_REVENUE",
                orders: validOrders,
                baseYear: request.Year,
                compareYear: compareYear,
                selector: CalculateActualRevenue
            );
        }

        private static void ValidateYearComparisonRequest(YearComparisonRequest request)
        {
            if (request.Year <= 0)
                throw new Exception("Year không hợp lệ.");

            if (request.CompareYear.HasValue && request.CompareYear.Value <= 0)
                throw new Exception("CompareYear không hợp lệ.");
        }

        private YearComparisonChartDto BuildYearComparisonResult(
            string metric,
            List<Order> orders,
            int baseYear,
            int compareYear,
            Func<Order, decimal> selector)
        {
            var baseSeries = BuildYearSeries(orders, baseYear, selector);
            var compareSeries = BuildYearSeries(orders, compareYear, selector);

            return new YearComparisonChartDto
            {
                Metric = metric,
                XAxisMonths = Enumerable.Range(1, 12).ToList(),
                BaseYear = baseSeries,
                CompareYear = compareSeries
            };
        }

        private static YearlySeriesDto BuildYearSeries(
            List<Order> orders,
            int year,
            Func<Order, decimal> selector)
        {
            var monthMap = Enumerable.Range(1, 12)
                .ToDictionary(m => m, _ => 0m);

            var yearOrders = orders
                .Where(o => o.Orderdatetime.HasValue && o.Orderdatetime.Value.Year == year)
                .ToList();

            foreach (var order in yearOrders)
            {
                var month = order.Orderdatetime!.Value.Month;
                monthMap[month] += selector(order);
            }

            return new YearlySeriesDto
            {
                Year = year,
                Label = year.ToString(),
                Total = yearOrders.Sum(selector),
                Data = Enumerable.Range(1, 12)
                    .Select(month => new MonthlyComparisonPointDto
                    {
                        Month = month,
                        Value = monthMap[month]
                    })
                    .ToList()
            };
        }
    }
}
