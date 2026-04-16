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

        // 1. Chỉ lấy các trạng thái hợp lệ (Đã xác nhận, Đang xử lý, Đã giao)
        var validStatuses = new List<string> {
            OrderStatus.CONFIRMED,
            OrderStatus.PROCESSING,
            OrderStatus.SHIPPED,
            OrderStatus.DELIVERED
        };

        // 2. Query tối ưu: Lấy các Order CÓ CHỨA productId này, kèm OrderDetail và Promotion
        var orders = await orderRepo.Entities
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.Promotion)
            .Where(o => validStatuses.Contains(o.Status) && o.OrderDetails.Any(od => od.Productid == productId))
            .ToListAsync();

        var response = new ProductStatisticResponseDto();

        // 3. Xử lý thuật toán chia tỷ trọng Khuyến Mãi
        foreach (var order in orders)
        {
            decimal subTotal = order.OrderDetails.Sum(d => d.Amount ?? (d.Product?.Price ?? 0) * (d.Quantity ?? 0));

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

            var targetDetails = order.OrderDetails.Where(od => od.Productid == productId).ToList();

            foreach (var targetOd in targetDetails)
            {
                var product = targetOd.Product;
                int qty = targetOd.Quantity ?? 0;

                decimal itemGross = targetOd.Amount ?? (product?.Price ?? 0) * qty;

                // Tỷ trọng
                decimal ratio = subTotal > 0 ? (itemGross / subTotal) : 0;
                decimal itemDiscount = discount * ratio;

                // Doanh thu thực nhận
                decimal itemNet = itemGross - itemDiscount;

                // Lợi nhuận = Doanh thu thực - Vốn
                decimal itemCapital = (product?.ImportPrice ?? 0) * qty;
                decimal itemProfit = itemNet - itemCapital;

                response.TotalGrossRevenue += itemGross;
                response.TotalNetRevenue += itemNet;
                response.TotalProfit += itemProfit;
                response.TotalQuantitySold += qty;

                response.Orders.Add(new ProductOrderStatDto
                {
                    OrderId = order.Orderid,
                    OrderDate = order.Orderdatetime,
                    CustomerName = order.Customername,
                    Quantity = qty,
                    GrossRevenue = itemGross,
                    NetRevenue = itemNet,
                    Profit = itemProfit
                });
            }
        }

        response.Orders = response.Orders.OrderByDescending(o => o.OrderDate).ToList();

        return response;
    }
}