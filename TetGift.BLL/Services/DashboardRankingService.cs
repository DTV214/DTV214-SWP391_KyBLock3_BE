using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class DashboardRankingService : IDashboardRankingService
    {
        private readonly IUnitOfWork _uow;

        private static readonly HashSet<string> ExcludedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        OrderStatus.PENDING,
        OrderStatus.CANCELLED,
        "CANCEL_REQUESTED"
    };

        public DashboardRankingService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<CategoryRankingResponseDto> GetCategoryPerformanceAsync(DashboardRankingRequest request)
        {
            var range = ResolveRange(request);
            var rows = await BuildFlattenedRowsAsync(range.StartInclusive, range.EndExclusive);

            var grouped = rows
                .GroupBy(x => new { x.CategoryId, x.CategoryName })
                .Select(g => new CategoryRankingItemDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    Revenue = g.Sum(x => x.Revenue),
                    Profit = g.Sum(x => x.Profit),
                    QuantitySold = g.Sum(x => x.QuantitySold)
                })
                .OrderByDescending(x => x.Revenue)
                .ThenByDescending(x => x.QuantitySold)
                .ToList();

            return new CategoryRankingResponseDto
            {
                Range = new RankingPeriodInfoDto
                {
                    Period = range.Period,
                    Label = range.Label,
                    StartDate = range.StartInclusive,
                    EndDate = range.EndExclusive.AddDays(-1)
                },
                TotalRevenue = grouped.Sum(x => x.Revenue),
                TotalProfit = grouped.Sum(x => x.Profit),
                TotalQuantitySold = grouped.Sum(x => x.QuantitySold),
                Data = grouped
            };
        }

        public async Task<CategoryProductRankingResponseDto> GetCategoryProductsPerformanceAsync(int categoryId, DashboardRankingRequest request)
        {
            var range = ResolveRange(request);
            var rows = await BuildFlattenedRowsAsync(range.StartInclusive, range.EndExclusive);

            var categoryRows = rows
                .Where(x => x.CategoryId == categoryId)
                .ToList();

            var categoryName = categoryRows
                .Select(x => x.CategoryName)
                .FirstOrDefault() ?? string.Empty;

            var grouped = categoryRows
                .GroupBy(x => new { x.ProductId, x.ProductName, x.CategoryId, x.CategoryName })
                .Select(g => new ProductRankingItemDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    Revenue = g.Sum(x => x.Revenue),
                    Profit = g.Sum(x => x.Profit),
                    QuantitySold = g.Sum(x => x.QuantitySold)
                })
                .OrderByDescending(x => x.Revenue)
                .ThenByDescending(x => x.QuantitySold)
                .ToList();

            return new CategoryProductRankingResponseDto
            {
                Range = new RankingPeriodInfoDto
                {
                    Period = range.Period,
                    Label = range.Label,
                    StartDate = range.StartInclusive,
                    EndDate = range.EndExclusive.AddDays(-1)
                },
                CategoryId = categoryId,
                CategoryName = categoryName,
                TotalRevenue = grouped.Sum(x => x.Revenue),
                TotalProfit = grouped.Sum(x => x.Profit),
                TotalQuantitySold = grouped.Sum(x => x.QuantitySold),
                Data = grouped
            };
        }

        private async Task<List<FlattenedSaleRow>> BuildFlattenedRowsAsync(DateTime startInclusive, DateTime endExclusive)
        {
            var orderRepo = _uow.GetRepository<Order>();

            var orders = await orderRepo.Entities
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Category)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductDetailProductparents)
                            .ThenInclude(pd => pd.Product)
                                .ThenInclude(p => p.Category)
                .AsNoTracking()
                .Where(o => o.Orderdatetime.HasValue
                            && o.Orderdatetime.Value >= startInclusive
                            && o.Orderdatetime.Value < endExclusive)
                .ToListAsync();

            var validOrders = orders
                .Where(IsIncludedOrder)
                .ToList();

            var result = new List<FlattenedSaleRow>();

            foreach (var order in validOrders)
            {
                var orderRows = ExpandOrderToRows(order);
                result.AddRange(orderRows);
            }

            return result;
        }

        private static bool IsIncludedOrder(Order order)
        {
            var status = (order.Status ?? string.Empty).Trim().ToUpperInvariant();
            return !ExcludedStatuses.Contains(status);
        }

        private static List<FlattenedSaleRow> ExpandOrderToRows(Order order)
        {
            var result = new List<FlattenedSaleRow>();

            var details = (order.OrderDetails ?? [])
                .Where(od => od.Product != null && (od.Quantity ?? 0) > 0)
                .ToList();

            if (details.Count == 0)
                return result;

            var lineBases = details.Select(GetLineBaseAmount).ToList();
            var totalBaseAmount = lineBases.Sum();

            // Totalprice là tổng cuối cùng của đơn sau voucher/promotion
            var orderNetRevenue = order.Totalprice ?? totalBaseAmount;
            if (orderNetRevenue < 0) orderNetRevenue = 0;

            decimal allocatedOrderRevenueSoFar = 0m;

            for (int i = 0; i < details.Count; i++)
            {
                var detail = details[i];
                var product = detail.Product!;
                var orderQty = detail.Quantity ?? 0;
                var lineBaseAmount = lineBases[i];

                decimal allocatedLineRevenue;

                // Reconcile dòng cuối để tổng line revenue luôn khớp đúng order.Totalprice
                if (i == details.Count - 1)
                {
                    allocatedLineRevenue = orderNetRevenue - allocatedOrderRevenueSoFar;
                }
                else if (totalBaseAmount > 0)
                {
                    allocatedLineRevenue = orderNetRevenue * (lineBaseAmount / totalBaseAmount);
                }
                else
                {
                    allocatedLineRevenue = details.Count > 0 ? orderNetRevenue / details.Count : 0m;
                }

                if (allocatedLineRevenue < 0)
                    allocatedLineRevenue = 0;

                allocatedOrderRevenueSoFar += allocatedLineRevenue;

                // Product config = giỏ preset => không tính product cha, chỉ tính child
                if (product.Configid.HasValue)
                {
                    result.AddRange(ExpandConfiguredProduct(detail, product, allocatedLineRevenue));
                    continue;
                }

                var categoryId = product.Categoryid ?? 0;
                var categoryName = product.Category?.Categoryname ?? "UNCATEGORIZED";
                var cost = (product.ImportPrice ?? 0m) * orderQty;

                result.Add(new FlattenedSaleRow
                {
                    CategoryId = categoryId,
                    CategoryName = categoryName,
                    ProductId = product.Productid,
                    ProductName = product.Productname ?? $"Product {product.Productid}",
                    Revenue = allocatedLineRevenue,
                    Cost = cost,
                    Profit = 0m, // set sau
                    QuantitySold = orderQty
                });
            }

            NormalizeOrderProfit(order, result);
            return result;
        }

        private static void NormalizeOrderProfit(Order order, List<FlattenedSaleRow> rows)
        {
            if (rows.Count == 0)
                return;

            // Profit raw từ revenue - cost
            foreach (var row in rows)
            {
                row.Profit = row.Revenue - row.Cost;
            }

            // Nếu không có ActualRevenue thì giữ raw profit
            if (!order.ActualRevenue.HasValue)
                return;

            var targetProfit = order.ActualRevenue.Value;
            var currentProfit = rows.Sum(x => x.Profit);
            var diff = targetProfit - currentProfit;

            // Không lệch thì thôi
            if (diff == 0)
                return;

            var totalRevenue = rows.Sum(x => x.Revenue);

            // Chia phần chênh lệch theo tỷ trọng revenue để tổng profit khớp đúng ActualRevenue
            if (totalRevenue > 0)
            {
                decimal allocatedDiffSoFar = 0m;

                for (int i = 0; i < rows.Count; i++)
                {
                    decimal profitAdjustment;

                    if (i == rows.Count - 1)
                    {
                        profitAdjustment = diff - allocatedDiffSoFar;
                    }
                    else
                    {
                        profitAdjustment = diff * (rows[i].Revenue / totalRevenue);
                    }

                    rows[i].Profit += profitAdjustment;
                    allocatedDiffSoFar += profitAdjustment;
                }
            }
            else
            {
                // Không có revenue thì dồn chênh lệch vào row cuối
                rows[^1].Profit += diff;
            }
        }

        private static List<FlattenedSaleRow> ExpandConfiguredProduct(OrderDetail detail, Product parentProduct, decimal allocatedLineRevenue)
        {
            var result = new List<FlattenedSaleRow>();
            var orderQty = detail.Quantity ?? 0;

            var childDetails = (parentProduct.ProductDetailProductparents ?? [])
                .Where(x => x.Product != null && (x.Quantity ?? 0) > 0)
                .ToList();

            // Product có ConfigId nhưng không có child -> không tính product cha
            // Vì business của bạn đã chốt: giỏ thì chỉ tính child product
            if (childDetails.Count == 0)
                return result;

            var childBaseValues = childDetails
                .Select(cd => (cd.Product?.Price ?? 0m) * (cd.Quantity ?? 0))
                .ToList();

            var totalChildBaseValue = childBaseValues.Sum();
            var totalChildUnitCount = childDetails.Sum(x => x.Quantity ?? 0);

            decimal allocatedChildRevenueSoFar = 0m;

            for (int i = 0; i < childDetails.Count; i++)
            {
                var childDetail = childDetails[i];
                var childProduct = childDetail.Product!;
                var childQtyInBasket = childDetail.Quantity ?? 0;
                var totalChildQtySold = childQtyInBasket * orderQty;

                decimal allocatedChildRevenue;

                // Reconcile child cuối để tổng child revenue luôn khớp đúng line revenue
                if (i == childDetails.Count - 1)
                {
                    allocatedChildRevenue = allocatedLineRevenue - allocatedChildRevenueSoFar;
                }
                else if (totalChildBaseValue > 0)
                {
                    allocatedChildRevenue = allocatedLineRevenue * (childBaseValues[i] / totalChildBaseValue);
                }
                else if (totalChildUnitCount > 0)
                {
                    allocatedChildRevenue = allocatedLineRevenue * ((decimal)childQtyInBasket / totalChildUnitCount);
                }
                else
                {
                    allocatedChildRevenue = childDetails.Count > 0 ? allocatedLineRevenue / childDetails.Count : 0m;
                }

                if (allocatedChildRevenue < 0)
                    allocatedChildRevenue = 0;

                allocatedChildRevenueSoFar += allocatedChildRevenue;

                var cost = (childProduct.ImportPrice ?? 0m) * totalChildQtySold;
                var categoryId = childProduct.Categoryid ?? 0;
                var categoryName = childProduct.Category?.Categoryname ?? "UNCATEGORIZED";

                result.Add(new FlattenedSaleRow
                {
                    CategoryId = categoryId,
                    CategoryName = categoryName,
                    ProductId = childProduct.Productid,
                    ProductName = childProduct.Productname ?? $"Product {childProduct.Productid}",
                    Revenue = allocatedChildRevenue,
                    Cost = cost,
                    Profit = 0m, // set sau
                    QuantitySold = totalChildQtySold
                });
            }

            return result;
        }

        private static decimal GetLineBaseAmount(OrderDetail detail)
        {
            if (detail.Amount.HasValue && detail.Amount.Value > 0)
                return detail.Amount.Value;

            var qty = detail.Quantity ?? 0;
            var unitPrice = detail.Product?.Price ?? 0m;
            return unitPrice * qty;
        }

        private static ResolvedRange ResolveRange(DashboardRankingRequest request)
        {
            var period = (request.Period ?? "month").Trim().ToLowerInvariant();

            return period switch
            {
                "week" => ResolveWeekRange(request),
                "month" => ResolveMonthRange(request),
                "year" => ResolveYearRange(request),
                _ => throw new Exception("Period chỉ hỗ trợ: week, month, year.")
            };
        }

        private static ResolvedRange ResolveWeekRange(DashboardRankingRequest request)
        {
            if (!request.Date.HasValue)
                throw new Exception("Period = week yêu cầu truyền date.");

            var date = request.Date.Value.Date;

            // Monday = start of week
            var diff = ((int)date.DayOfWeek + 6) % 7;
            var start = date.AddDays(-diff);
            var end = start.AddDays(7);

            return new ResolvedRange
            {
                Period = "week",
                StartInclusive = start,
                EndExclusive = end,
                Label = $"{start:yyyy-MM-dd} -> {end.AddDays(-1):yyyy-MM-dd}"
            };
        }

        private static ResolvedRange ResolveMonthRange(DashboardRankingRequest request)
        {
            if (!request.Year.HasValue || !request.Month.HasValue)
                throw new Exception("Period = month yêu cầu truyền year và month.");

            var year = request.Year.Value;
            var month = request.Month.Value;

            if (month < 1 || month > 12)
                throw new Exception("Month phải nằm trong khoảng 1-12.");

            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            return new ResolvedRange
            {
                Period = "month",
                StartInclusive = start,
                EndExclusive = end,
                Label = $"{year}-{month:D2}"
            };
        }

        private static ResolvedRange ResolveYearRange(DashboardRankingRequest request)
        {
            if (!request.Year.HasValue)
                throw new Exception("Period = year yêu cầu truyền year.");

            var year = request.Year.Value;
            var start = new DateTime(year, 1, 1);
            var end = start.AddYears(1);

            return new ResolvedRange
            {
                Period = "year",
                StartInclusive = start,
                EndExclusive = end,
                Label = year.ToString()
            };
        }

        private sealed class FlattenedSaleRow
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public decimal Revenue { get; set; }
            public decimal Cost { get; set; }
            public decimal Profit { get; set; }
            public int QuantitySold { get; set; }
        }

        private sealed class ResolvedRange
        {
            public string Period { get; set; } = string.Empty;
            public DateTime StartInclusive { get; set; }
            public DateTime EndExclusive { get; set; }
            public string Label { get; set; } = string.Empty;
        }
    }
}
