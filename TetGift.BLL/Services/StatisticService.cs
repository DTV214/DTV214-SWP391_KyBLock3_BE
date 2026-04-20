using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class StatisticService : IStatisticService
{
    private readonly IUnitOfWork _uow;

    public StatisticService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ProductStatisticResponseDto> GetProductStatisticAsync(int productId)
    {
        var orderRepo = _uow.GetRepository<Order>();
        var productRepo = _uow.GetRepository<Product>();

        // 1. Phân loại sản phẩm: Hàng thường hay Giỏ Quà (Template)?
        var targetProduct = await productRepo.GetByIdAsync(productId);
        if (targetProduct == null) throw new Exception("Không tìm thấy sản phẩm.");

        bool isBasketTemplate = targetProduct.Configid.HasValue;
        int? targetConfigId = targetProduct.Configid;

        // 2. Chỉ lấy các trạng thái đơn hàng mang lại dòng tiền thực tế
        var validStatuses = new List<string> {
            OrderStatus.CONFIRMED,
            OrderStatus.PROCESSING,
            OrderStatus.SHIPPED,
            OrderStatus.DELIVERED
        };

        // 3. QUERY SIÊU CẤP: Quét 2 tầng (Bán lẻ + Clone Giỏ Quà + Món con trong giỏ)
        var orders = await orderRepo.Entities
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                    .ThenInclude(p => p.ProductDetailProductparents) // Thọc sâu vào Giỏ quà để lấy món con
                        .ThenInclude(pd => pd.Product)               // Lấy Giá/Vốn của món con
            .Include(o => o.Promotion)
            .Where(o => validStatuses.Contains(o.Status) &&
                        o.OrderDetails.Any(od =>
                            // TH1: Mua chính xác ID này (Bán lẻ)
                            od.Productid == productId ||

                            // TH2: Admin xem GIỎ QUÀ MẪU -> Lấy TẤT CẢ các giỏ quà Clone có chung ConfigId
                            (isBasketTemplate && od.Product != null && od.Product.Configid == targetConfigId) ||

                            // TH3: Admin xem HÀNG THƯỜNG -> Tìm xem nó có nằm giấu trong bất kỳ Giỏ quà nào không
                            (!isBasketTemplate && od.Product != null && od.Product.Configid != null && od.Product.ProductDetailProductparents.Any(pd => pd.Productid == productId))
                        ))
            .ToListAsync();

        var response = new ProductStatisticResponseDto();

        // 4. Thuật toán bóc tách và chia tỷ trọng Khuyến Mãi
        foreach (var order in orders)
        {
            // Tổng tiền hàng gốc của toàn bộ đơn (SubTotal)
            decimal subTotal = order.OrderDetails.Sum(d => d.Amount ?? (d.Product?.Price ?? 0) * (d.Quantity ?? 0));

            // Tổng tiền Khuyến Mãi của toàn đơn
            decimal discount = 0;
            if (order.Promotion != null)
            {
                if (order.Promotion.IsPercentage ?? false)
                {
                    discount = subTotal * ((order.Promotion.Discountvalue ?? 0) / 100);
                    if (order.Promotion.MaxDiscountPrice.HasValue && discount > order.Promotion.MaxDiscountPrice.Value)
                        discount = order.Promotion.MaxDiscountPrice.Value;
                }
                else
                {
                    discount = order.Promotion.Discountvalue ?? 0;
                }
            }

            // Lọc ra các OrderDetail thỏa mãn 1 trong 3 trường hợp trên
            var targetDetails = order.OrderDetails.Where(od =>
                od.Productid == productId ||
                (isBasketTemplate && od.Product != null && od.Product.Configid == targetConfigId) ||
                (!isBasketTemplate && od.Product != null && od.Product.Configid != null && od.Product.ProductDetailProductparents.Any(pd => pd.Productid == productId))
            ).ToList();

            // Các biến cộng dồn cục bộ cho RIÊNG đơn hàng này
            int orderTotalQty = 0;
            decimal orderTotalGross = 0;
            decimal orderTotalCapital = 0;

            foreach (var targetOd in targetDetails)
            {
                if (isBasketTemplate)
                {
                    // Đang thống kê Giỏ Quà -> Tính thẳng doanh thu/vốn của cả cái giỏ quà Clone đó
                    int qty = targetOd.Quantity ?? 0;
                    orderTotalQty += qty;
                    orderTotalGross += targetOd.Amount ?? (targetOd.Product?.Price ?? 0) * qty;
                    orderTotalCapital += (targetOd.Product?.ImportPrice ?? 0) * qty;
                }
                else
                {
                    // Đang thống kê Món lẻ
                    if (targetOd.Productid == productId)
                    {
                        // Hàng được mua lẻ trực tiếp không qua giỏ
                        int qty = targetOd.Quantity ?? 0;
                        orderTotalQty += qty;
                        orderTotalGross += targetOd.Amount ?? (targetOd.Product?.Price ?? 0) * qty;
                        orderTotalCapital += (targetOd.Product?.ImportPrice ?? 0) * qty;
                    }
                    else
                    {
                        // Hàng bị đóng gói trong một giỏ quà (Combo) -> Cần bóc tách ra
                        var childPd = targetOd.Product!.ProductDetailProductparents.First(pd => pd.Productid == productId);

                        int itemsPerBasket = childPd.Quantity ?? 1; // 1 giỏ chứa bao nhiêu cái bánh này?
                        int basketQty = targetOd.Quantity ?? 0;     // Khách mua bao nhiêu giỏ?

                        int actualQty = itemsPerBasket * basketQty; // Tổng số cái bánh xuất kho

                        orderTotalQty += actualQty;
                        // Doanh thu của món đồ này = Giá của nó * Số lượng xuất kho
                        orderTotalGross += (childPd.Product?.Price ?? 0) * actualQty;
                        // Vốn của món đồ này
                        orderTotalCapital += (childPd.Product?.ImportPrice ?? 0) * actualQty;
                    }
                }
            }

            // Nếu đơn hàng này thực sự có đóng góp doanh thu cho sản phẩm đang xét
            if (orderTotalQty > 0)
            {
                // Tính tỷ trọng gánh Khuyến Mãi cho phần doanh thu này
                decimal ratio = subTotal > 0 ? (orderTotalGross / subTotal) : 0;
                decimal itemDiscount = discount * ratio;

                // Doanh thu thực nhận & Lợi nhuận
                decimal itemNet = orderTotalGross - itemDiscount;
                decimal itemProfit = itemNet - orderTotalCapital;

                // Cộng dồn vào chỉ số tổng trên Dashboard
                response.TotalGrossRevenue += orderTotalGross;
                response.TotalNetRevenue += itemNet;
                response.TotalProfit += itemProfit;
                response.TotalQuantitySold += orderTotalQty;

                // Thêm vào danh sách chi tiết đơn hàng (Mỗi đơn hàng chỉ xuất hiện 1 dòng duy nhất)
                response.Orders.Add(new ProductOrderStatDto
                {
                    OrderId = order.Orderid,
                    OrderDate = order.Orderdatetime,
                    CustomerName = order.Customername,
                    Quantity = orderTotalQty,
                    GrossRevenue = orderTotalGross,
                    NetRevenue = itemNet,
                    Profit = itemProfit
                });
            }
        }

        // Sort danh sách đơn hàng theo ngày mới nhất
        response.Orders = response.Orders.OrderByDescending(o => o.OrderDate).ToList();

        return response;
    }
    public async Task<List<TrendingProductDto>> GetTrendingProductsAsync(string period = "week", int top = 5)
    {
        var orderRepo = _uow.GetRepository<Order>();

        DateTime now = DateTime.Now;
        DateTime currentStart, previousStart;
        string dateFormat;
        int pointsCount;
        Func<DateTime, DateTime> stepFunc;

        // 1. Máy bẻ ghi thời gian (Time-boxing)
        switch (period.ToLower())
        {
            case "month":
                // 30 ngày qua
                currentStart = now.AddDays(-30).Date;
                previousStart = currentStart.AddDays(-30);
                dateFormat = "dd/MM";
                pointsCount = 30;
                stepFunc = d => d.AddDays(1);
                break;
            case "year":
                // 12 tháng qua (Tính từ ngày mùng 1 của 11 tháng trước đến hiện tại)
                currentStart = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
                previousStart = currentStart.AddMonths(-12);
                dateFormat = "MM/yyyy"; // Group biểu đồ theo Tháng/Năm
                pointsCount = 12;
                stepFunc = d => d.AddMonths(1);
                break;
            case "week":
            default:
                // 7 ngày qua
                currentStart = now.AddDays(-7).Date;
                previousStart = currentStart.AddDays(-7);
                dateFormat = "dd/MM";
                pointsCount = 7;
                stepFunc = d => d.AddDays(1);
                break;
        }

        var validStatuses = new List<string> { OrderStatus.CONFIRMED, OrderStatus.PROCESSING, OrderStatus.SHIPPED, OrderStatus.DELIVERED };

        // 2. Chỉ query 1 lần duy nhất lấy toàn bộ đơn trong khoảng thời gian cần so sánh
        var orders = await orderRepo.Entities
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Where(o => validStatuses.Contains(o.Status) && o.Orderdatetime >= previousStart && o.Orderdatetime <= now)
            .ToListAsync();

        // Dictionary dùng để tracking số liệu từng sản phẩm
        var tracker = new Dictionary<int, (Product Product, int CurrentQty, int PrevQty, List<(DateTime Date, int Qty)> Sales)>();

        // 3. Phân loại đơn hàng vào Kỳ Hiện Tại hoặc Kỳ Trước
        foreach (var order in orders)
        {
            bool isCurrent = order.Orderdatetime >= currentStart;

            foreach (var detail in order.OrderDetails)
            {
                if (detail.Productid == null || detail.Product == null) continue;

                int pId = detail.Productid.Value;
                int qty = detail.Quantity ?? 0;

                if (!tracker.ContainsKey(pId))
                {
                    tracker[pId] = (detail.Product, 0, 0, new List<(DateTime, int)>());
                }

                var data = tracker[pId];
                if (isCurrent)
                {
                    data.CurrentQty += qty;
                    data.Sales.Add((order.Orderdatetime.Value, qty));
                }
                else
                {
                    data.PrevQty += qty;
                }
                tracker[pId] = data; // Cập nhật lại vào dict
            }
        }

        // 4. Lọc TOP sản phẩm (Xếp hạng theo số lượng bán ở kỳ hiện tại)
        var topProducts = tracker.Values
            .Where(x => x.CurrentQty > 0) // Chỉ lấy thằng nào có bán được
            .OrderByDescending(x => x.CurrentQty)
            .Take(top)
            .ToList();

        var response = new List<TrendingProductDto>();

        // 5. Đóng gói JSON & Xử lý biểu đồ
        foreach (var item in topProducts)
        {
            var dto = new TrendingProductDto
            {
                ProductId = item.Product.Productid,
                ProductName = item.Product.Productname ?? "Sản phẩm",
                ImageUrl = item.Product.ImageUrl,
                TotalSoldInPeriod = item.CurrentQty
            };

            // Tính % tăng trưởng (Xử lý lỗi chia cho 0 nếu kỳ trước không bán được cái nào)
            if (item.PrevQty == 0)
            {
                dto.GrowthRate = item.CurrentQty > 0 ? 100 : 0;
            }
            else
            {
                dto.GrowthRate = Math.Round((decimal)(item.CurrentQty - item.PrevQty) / item.PrevQty * 100, 1);
            }

            // Dựng khung sườn biểu đồ (Fill các ngày trống bằng số 0 để line chart không đứt khúc)
            var chartDict = new Dictionary<string, int>();
            DateTime stepDate = currentStart;
            for (int i = 0; i < pointsCount; i++)
            {
                chartDict[stepDate.ToString(dateFormat)] = 0;
                stepDate = stepFunc(stepDate);
            }

            // Đổ dữ liệu thật vào khung sườn
            foreach (var sale in item.Sales)
            {
                string key = sale.Date.ToString(dateFormat);
                if (chartDict.ContainsKey(key))
                {
                    chartDict[key] += sale.Qty;
                }
            }

            dto.TrendData = chartDict.Select(kv => new TrendDataPointDto { Date = kv.Key, Quantity = kv.Value }).ToList();
            response.Add(dto);
        }

        return response;
    }
    public async Task<EventTrendResponseDto> GetEventMonthTrendAsync(int month)
    {
        var orderRepo = _uow.GetRepository<Order>();

        DateTime now = DateTime.Now;
        int queryYear = now.Year; // Mặc định lấy năm hiện tại

        // 1. THUẬT TOÁN TỰ ĐỘNG DÒ NĂM
        // Nếu Admin chọn một tháng ở TƯƠNG LAI (VD: bây giờ tháng 4, chọn tháng 10) 
        // -> Lùi về năm ngoái để xem trend của sự kiện đó
        if (month > now.Month)
        {
            queryYear = now.Year - 1;
        }
        // Nếu chọn tháng quá khứ hoặc hiện tại -> Giữ nguyên năm nay

        DateTime startDate = new DateTime(queryYear, month, 1);
        DateTime endDate = startDate.AddMonths(1).AddDays(-1);

        // Nếu là tháng hiện tại, ta giới hạn đến ngày hôm nay thôi (tuỳ chọn)
        if (queryYear == now.Year && month == now.Month)
        {
            endDate = now;
        }

        var validStatuses = new List<string> {
            OrderStatus.CONFIRMED, OrderStatus.PROCESSING, OrderStatus.SHIPPED, OrderStatus.DELIVERED
        };

        // 2. QUERY SIÊU TỐI ƯU KÈM THEO GIỎ QUÀ
        var orders = await orderRepo.Entities
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                    .ThenInclude(p => p.Category)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                    .ThenInclude(p => p.ProductDetailProductparents)
                        .ThenInclude(pd => pd.Product)
                            .ThenInclude(p => p.Category)
            .Where(o => validStatuses.Contains(o.Status) &&
                        o.Orderdatetime >= startDate &&
                        o.Orderdatetime <= endDate)
            .ToListAsync();

        var productCounts = new Dictionary<int, (Product Product, int Count)>();
        var categoryCounts = new Dictionary<int, (string Name, int Count)>();
        int totalItemsSoldInMonth = 0;

        // 3. THUẬT TOÁN BÓC TÁCH COMBO
        foreach (var order in orders)
        {
            foreach (var od in order.OrderDetails)
            {
                if (od.Product == null) continue;
                int orderQty = od.Quantity ?? 0;

                // Nếu là Combo / Giỏ Quà -> Mổ bụng đếm món con
                if (od.Product.Configid != null)
                {
                    foreach (var child in od.Product.ProductDetailProductparents)
                    {
                        if (child.Product == null) continue;
                        int actualQty = (child.Quantity ?? 1) * orderQty;

                        // Cập nhật Product
                        if (productCounts.ContainsKey(child.Product.Productid))
                            productCounts[child.Product.Productid] = (child.Product, productCounts[child.Product.Productid].Count + actualQty);
                        else
                            productCounts[child.Product.Productid] = (child.Product, actualQty);

                        // Cập nhật Category
                        if (child.Product.Categoryid.HasValue)
                        {
                            string catName = child.Product.Category?.Categoryname ?? "Khác";
                            if (categoryCounts.ContainsKey(child.Product.Categoryid.Value))
                                categoryCounts[child.Product.Categoryid.Value] = (catName, categoryCounts[child.Product.Categoryid.Value].Count + actualQty);
                            else
                                categoryCounts[child.Product.Categoryid.Value] = (catName, actualQty);
                        }

                        totalItemsSoldInMonth += actualQty;
                    }
                }
                else // Nếu là Hàng lẻ -> Đếm thẳng
                {
                    if (productCounts.ContainsKey(od.Product.Productid))
                        productCounts[od.Product.Productid] = (od.Product, productCounts[od.Product.Productid].Count + orderQty);
                    else
                        productCounts[od.Product.Productid] = (od.Product, orderQty);

                    if (od.Product.Categoryid.HasValue)
                    {
                        string catName = od.Product.Category?.Categoryname ?? "Khác";
                        if (categoryCounts.ContainsKey(od.Product.Categoryid.Value))
                            categoryCounts[od.Product.Categoryid.Value] = (catName, categoryCounts[od.Product.Categoryid.Value].Count + orderQty);
                        else
                            categoryCounts[od.Product.Categoryid.Value] = (catName, orderQty);
                    }

                    totalItemsSoldInMonth += orderQty;
                }
            }
        }

        // 4. ĐÓNG GÓI JSON TRẢ VỀ FE
        var response = new EventTrendResponseDto
        {
            RequestedMonth = month,
            DataYear = queryYear,
            TopProducts = productCounts.Values
                .OrderByDescending(x => x.Count)
                .Take(10) // Lấy Top 10 sản phẩm
                .Select(x => new ProductTrendDto
                {
                    ProductId = x.Product.Productid,
                    ProductName = x.Product.Productname ?? "N/A",
                    ImageUrl = x.Product.ImageUrl,
                    TotalSold = x.Count
                }).ToList(),
            TopCategories = categoryCounts.Values
                .OrderByDescending(x => x.Count)
                .Take(5) // Lấy Top 5 Danh mục
                .Select(x => new CategoryStatDto
                {
                    CategoryName = x.Name,
                    TotalSold = x.Count,
                    Percentage = totalItemsSoldInMonth > 0 ? Math.Round((decimal)x.Count / totalItemsSoldInMonth * 100, 1) : 0
                }).ToList()
        };

        return response;
    }
    // Hàm helper để tránh lặp code khi đếm
    private void UpdateCounts(Product p, int qty,
        Dictionary<int, (Product Product, int Count)> pDict,
        Dictionary<int, (string Name, int Count)> cDict)
    {
        // Đếm sản phẩm
        if (pDict.ContainsKey(p.Productid))
            pDict[p.Productid] = (p, pDict[p.Productid].Count + qty);
        else
            pDict[p.Productid] = (p, qty);

        // Đếm danh mục
        if (p.Categoryid.HasValue)
        {
            string catName = p.Category?.Categoryname ?? "Khác";
            if (cDict.ContainsKey(p.Categoryid.Value))
                cDict[p.Categoryid.Value] = (catName, cDict[p.Categoryid.Value].Count + qty);
            else
                cDict[p.Categoryid.Value] = (catName, qty);
        }
    }
}