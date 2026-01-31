using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly ICartService _cartService;
    private readonly IPromotionService _promotionService;

    public OrderService(IUnitOfWork uow, ICartService cartService, IPromotionService promotionService)
    {
        _uow = uow;
        _cartService = cartService;
        _promotionService = promotionService;
    }

    public async Task<OrderResponseDto> CreateOrderFromCartAsync(int accountId, CreateOrderRequest request)
    {
        // 1. Lấy Cart (sử dụng ICartService - kế thừa code cũ)
        var cart = await _cartService.GetCartByAccountIdAsync(accountId);
        if (cart.ItemCount == 0)
            throw new Exception("Giỏ hàng trống, không thể tạo đơn hàng.");

        // 2. Validate và apply Promotion nếu có
        int? promotionId = null;
        decimal discountValue = 0;
        if (!string.IsNullOrWhiteSpace(request.PromotionCode))
        {
            try
            {
                var promotionResult = await _cartService.ApplyPromotionAsync(accountId, new ApplyPromotionRequest
                {
                    PromotionCode = request.PromotionCode
                });
                discountValue = promotionResult.DiscountValue ?? 0;

                // Lấy promotionId từ promotion service
                var allPromotions = await _promotionService.GetAllAsync();
                var promotion = allPromotions.FirstOrDefault(p => 
                    p.Code.Equals(request.PromotionCode, StringComparison.OrdinalIgnoreCase));
                if (promotion != null)
                    promotionId = promotion.PromotionId;
            }
            catch
            {
                throw new Exception("Mã giảm giá không hợp lệ hoặc đã hết hạn.");
            }
        }

        // 3. Validate Stock availability cho từng sản phẩm
        var stockRepo = _uow.GetRepository<Stock>();
        foreach (var item in cart.Items)
        {
            var stocks = await stockRepo.FindAsync(
                s => s.Productid == item.ProductId && s.Status == "ACTIVE"
            );

            var totalStock = stocks.Sum(s => s.Stockquantity ?? 0);
            if (totalStock < item.Quantity)
            {
                throw new Exception($"Sản phẩm '{item.ProductName}' không đủ số lượng trong kho. Còn lại: {totalStock}, yêu cầu: {item.Quantity}");
            }
        }

        // 4. Tạo Order
        var orderRepo = _uow.GetRepository<Order>();
        var order = new Order
        {
            Accountid = accountId,
            Promotionid = promotionId,
            Totalprice = cart.TotalPrice - discountValue,
            Status = "PENDING",
            Customername = request.CustomerName,
            Customerphone = request.CustomerPhone,
            Customeremail = request.CustomerEmail,
            Customeraddress = request.CustomerAddress,
            Note = request.Note,
            Orderdatetime = DateTime.Now
        };
        await orderRepo.AddAsync(order);
        await _uow.SaveAsync();

        // 5. Tạo OrderDetails và cập nhật Stock
        var orderDetailRepo = _uow.GetRepository<OrderDetail>();
        var stockMovementRepo = _uow.GetRepository<StockMovement>();

        foreach (var cartItem in cart.Items)
        {
            // Tạo OrderDetail
            var orderDetail = new OrderDetail
            {
                Orderid = order.Orderid,
                Productid = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                Amount = cartItem.SubTotal
            };
            await orderDetailRepo.AddAsync(orderDetail);

            // Cập nhật Stock (FIFO - First In First Out)
            var remainingQuantity = cartItem.Quantity ?? 0;
            var availableStocks = await stockRepo.FindAsync(
                s => s.Productid == cartItem.ProductId && s.Status == "ACTIVE"
            );

            foreach (var stock in availableStocks.OrderBy(s => s.Productiondate))
            {
                if (remainingQuantity <= 0) break;

                var stockQuantity = stock.Stockquantity ?? 0;
                var quantityToDeduct = Math.Min(remainingQuantity, stockQuantity);

                stock.Stockquantity = stockQuantity - quantityToDeduct;
                if (stock.Stockquantity <= 0)
                    stock.Status = "OUT_OF_STOCK";

                stockRepo.Update(stock);

                // Tạo StockMovement để ghi log
                var movement = new StockMovement
                {
                    Stockid = stock.Stockid,
                    Orderid = order.Orderid,
                    Quantity = -quantityToDeduct, // Số âm để thể hiện xuất kho
                    Movementdate = DateTime.Now,
                    Note = $"Xuất kho cho đơn hàng #{order.Orderid}"
                };
                await stockMovementRepo.AddAsync(movement);

                remainingQuantity -= quantityToDeduct;
            }
        }

        await _uow.SaveAsync();

        // 6. Clear Cart sau khi tạo đơn thành công
        await _cartService.ClearCartAsync(accountId);

        // 7. Load lại Order với đầy đủ thông tin
        var fullOrder = await orderRepo.FindAsync(
            o => o.Orderid == order.Orderid,
            include: q => q
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Promotion)
        );

        return MapToOrderResponseDto(fullOrder!);
    }

    public async Task<IEnumerable<OrderResponseDto>> GetOrdersByAccountIdAsync(int accountId)
    {
        var orderRepo = _uow.GetRepository<Order>();
        var orders = await orderRepo.GetAllAsync(
            o => o.Accountid == accountId,
            include: q => q
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Promotion)
        );

        return orders.Select(o => MapToOrderResponseDto(o));
    }

    public async Task<OrderResponseDto> GetOrderByIdAsync(int orderId, int accountId)
    {
        var orderRepo = _uow.GetRepository<Order>();
        var order = await orderRepo.FindAsync(
            o => o.Orderid == orderId && o.Accountid == accountId,
            include: q => q
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Promotion)
        );

        if (order == null)
            throw new Exception("Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn hàng này.");

        return MapToOrderResponseDto(order);
    }

    public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync(string? status = null)
    {
        var orderRepo = _uow.GetRepository<Order>();
        
        IEnumerable<Order> orders;
        if (!string.IsNullOrWhiteSpace(status))
        {
            orders = await orderRepo.GetAllAsync(
                o => o.Status == status,
                include: q => q
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .Include(o => o.Promotion)
                    .Include(o => o.Account)
            );
        }
        else
        {
            orders = await orderRepo.GetAllAsync(
                null,
                include: q => q
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .Include(o => o.Promotion)
                    .Include(o => o.Account)
            );
        }

        return orders.Select(o => MapToOrderResponseDto(o));
    }

    public async Task<OrderResponseDto> GetOrderByIdForAdminAsync(int orderId)
    {
        var orderRepo = _uow.GetRepository<Order>();
        var order = await orderRepo.FindAsync(
            o => o.Orderid == orderId,
            include: q => q
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Promotion)
                .Include(o => o.Account)
        );

        if (order == null)
            throw new Exception("Không tìm thấy đơn hàng.");

        return MapToOrderResponseDto(order);
    }

    public async Task<OrderResponseDto> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request)
    {
        var orderRepo = _uow.GetRepository<Order>();
        var order = await orderRepo.FindAsync(
            o => o.Orderid == orderId,
            include: q => q
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
        );

        if (order == null)
            throw new Exception("Không tìm thấy đơn hàng.");

        var currentStatus = order.Status ?? "PENDING";
        var newStatus = request.Status.ToUpper();

        // Validate status transition
        if (!IsValidStatusTransition(currentStatus, newStatus))
        {
            throw new Exception($"Không thể chuyển trạng thái từ '{currentStatus}' sang '{newStatus}'.");
        }

        // Nếu hủy đơn, hoàn lại stock
        if (newStatus == "CANCELLED" && currentStatus != "CANCELLED")
        {
            await RestoreStockAsync(order);
        }

        order.Status = newStatus;
        orderRepo.Update(order);
        await _uow.SaveAsync();

        // Load lại với đầy đủ thông tin
        var updatedOrder = await orderRepo.FindAsync(
            o => o.Orderid == orderId,
            include: q => q
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Promotion)
        );

        return MapToOrderResponseDto(updatedOrder!);
    }

    private bool IsValidStatusTransition(string currentStatus, string newStatus)
    {
        var validTransitions = new Dictionary<string, List<string>>
        {
            { "PENDING", new List<string> { "CONFIRMED", "CANCELLED" } },
            { "CONFIRMED", new List<string> { "PROCESSING", "CANCELLED" } },
            { "PROCESSING", new List<string> { "SHIPPED", "CANCELLED" } },
            { "SHIPPED", new List<string> { "DELIVERED", "CANCELLED" } },
            { "DELIVERED", new List<string> { } }, // Không thể chuyển từ DELIVERED
            { "CANCELLED", new List<string> { } } // Không thể chuyển từ CANCELLED
        };

        if (!validTransitions.ContainsKey(currentStatus))
            return false;

        return validTransitions[currentStatus].Contains(newStatus);
    }

    private async Task RestoreStockAsync(Order order)
    {
        var stockRepo = _uow.GetRepository<Stock>();
        var stockMovementRepo = _uow.GetRepository<StockMovement>();

        // Lấy các StockMovement liên quan đến đơn hàng này
        var movements = await stockMovementRepo.GetAllAsync(
            sm => sm.Orderid == order.Orderid && sm.Quantity < 0
        );

        foreach (var movement in movements)
        {
            var stock = await stockRepo.GetByIdAsync(movement.Stockid);
            if (stock != null)
            {
                stock.Stockquantity = (stock.Stockquantity ?? 0) + Math.Abs(movement.Quantity ?? 0);
                if (stock.Status == "OUT_OF_STOCK" && stock.Stockquantity > 0)
                    stock.Status = "ACTIVE";
                stockRepo.Update(stock);

                // Tạo StockMovement mới để ghi log hoàn lại
                var restoreMovement = new StockMovement
                {
                    Stockid = stock.Stockid,
                    Orderid = order.Orderid,
                    Quantity = Math.Abs(movement.Quantity ?? 0), // Số dương để thể hiện nhập lại
                    Movementdate = DateTime.Now,
                    Note = $"Hoàn lại kho do hủy đơn hàng #{order.Orderid}"
                };
                await stockMovementRepo.AddAsync(restoreMovement);
            }
        }
    }

    private OrderResponseDto MapToOrderResponseDto(Order order)
    {
        var items = new List<OrderDetailResponseDto>();
        decimal totalPrice = 0;

        if (order.OrderDetails != null)
        {
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Product != null)
                {
                    var price = detail.Product.Price ?? 0;
                    var quantity = detail.Quantity ?? 0;
                    var amount = detail.Amount ?? (price * quantity);

                    items.Add(new OrderDetailResponseDto
                    {
                        OrderDetailId = detail.Orderdetailid,
                        ProductId = detail.Product.Productid,
                        ProductName = detail.Product.Productname,
                        Sku = detail.Product.Sku,
                        Quantity = quantity,
                        Price = price,
                        Amount = amount,
                        ImageUrl = detail.Product.ImageUrl
                    });

                    totalPrice += amount;
                }
            }
        }

        var discountValue = 0m;
        var promotionCode = "";
        if (order.Promotion != null)
        {
            discountValue = order.Promotion.Discountvalue ?? 0;
            promotionCode = order.Promotion.Code ?? "";
        }

        var finalPrice = totalPrice - discountValue;
        if (finalPrice < 0) finalPrice = 0;

        return new OrderResponseDto
        {
            OrderId = order.Orderid,
            AccountId = order.Accountid ?? 0,
            OrderDateTime = order.Orderdatetime,
            TotalPrice = order.Totalprice ?? totalPrice,
            DiscountValue = discountValue > 0 ? discountValue : null,
            FinalPrice = finalPrice,
            Status = order.Status,
            CustomerName = order.Customername,
            CustomerPhone = order.Customerphone,
            CustomerEmail = order.Customeremail,
            CustomerAddress = order.Customeraddress,
            Note = order.Note,
            PromotionCode = !string.IsNullOrEmpty(promotionCode) ? promotionCode : null,
            Items = items
        };
    }
}
