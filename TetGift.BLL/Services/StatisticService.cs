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
}