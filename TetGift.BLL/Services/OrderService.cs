using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Common.Constraint;
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
    private readonly IWalletService _walletService;

    public OrderService(IUnitOfWork uow, ICartService cartService, IPromotionService promotionService, IWalletService walletService)
    {
        _uow = uow;
        _cartService = cartService;
        _promotionService = promotionService;
        _walletService = walletService;
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
                s => s.Productid == item.ProductId && s.Status == StockStatus.ACTIVE
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
            Status = OrderStatus.PENDING,
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
                s => s.Productid == cartItem.ProductId && s.Status == StockStatus.ACTIVE
            );

            foreach (var stock in availableStocks.OrderBy(s => s.Productiondate))
            {
                if (remainingQuantity <= 0) break;

                var stockQuantity = stock.Stockquantity ?? 0;
                var quantityToDeduct = Math.Min(remainingQuantity, stockQuantity);

                stock.Stockquantity = stockQuantity - quantityToDeduct;
                if (stock.Stockquantity <= 0)
                    stock.Status = StockStatus.OUT_OF_STOCK;

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

        var currentStatus = order.Status ?? OrderStatus.PENDING;
        var newStatus = request.Status.ToUpper();

        // Validate status transition
        if (!IsValidStatusTransition(currentStatus, newStatus))
        {
            throw new Exception($"Không thể chuyển trạng thái từ '{currentStatus}' sang '{newStatus}'.");
        }

        // Nếu hủy đơn, hoàn lại stock và hoàn tiền vào ví (nếu thanh toán bằng ví)
        if (newStatus == OrderStatus.CANCELLED && currentStatus != OrderStatus.CANCELLED)
        {
            await RestoreStockAsync(order);
            
            // Hoàn tiền vào ví nếu thanh toán bằng ví
            await _walletService.RefundToWalletAsync(orderId);
        }

        order.Status = newStatus;
        orderRepo.Update(order);
        await _uow.SaveAsync(); // Save tất cả thay đổi (bao gồm cả stock đã được restore)

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

    public async Task<OrderResponseDto> CancelOrderAsync(int orderId, int accountId, string userRole)
    {
        var orderRepo = _uow.GetRepository<Order>();
        var order = await orderRepo.FindAsync(
            o => o.Orderid == orderId,
            include: q => q
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Account)
        );

        if (order == null)
            throw new Exception("Không tìm thấy đơn hàng.");

        var currentStatus = order.Status ?? OrderStatus.PENDING;

        // Validate: Không thể hủy nếu đã DELIVERED hoặc CANCELLED
        if (currentStatus == OrderStatus.DELIVERED)
            throw new Exception("Không thể hủy đơn hàng đã được giao.");

        if (currentStatus == OrderStatus.CANCELLED)
            throw new Exception("Đơn hàng đã được hủy trước đó.");

        // Validate ownership: Customer chỉ được hủy order của chính mình
        var normalizedRole = userRole.ToUpper();
        if (normalizedRole != "ADMIN")
        {
            if (order.Accountid != accountId)
                throw new Exception("Bạn không có quyền hủy đơn hàng này.");

            // Validate time limit: 15 phút cho Customer
            if (order.Orderdatetime.HasValue)
            {
                var timeElapsed = DateTime.Now - order.Orderdatetime.Value;
                if (timeElapsed.TotalMinutes > 15)
                    throw new Exception("Chỉ có thể hủy đơn hàng trong vòng 15 phút kể từ khi đặt hàng.");
            }
        }

        // Process cancellation
        await RestoreStockAsync(order);
        
        // Hoàn tiền vào ví (nếu đã thanh toán)
        await _walletService.RefundToWalletAsync(orderId);

        // Update order status
        order.Status = OrderStatus.CANCELLED;
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
            { OrderStatus.PENDING, new List<string> { OrderStatus.CONFIRMED, OrderStatus.CANCELLED } },
            { OrderStatus.CONFIRMED, new List<string> { OrderStatus.PROCESSING, OrderStatus.CANCELLED } },
            { OrderStatus.PROCESSING, new List<string> { OrderStatus.SHIPPED, OrderStatus.CANCELLED } },
            { OrderStatus.SHIPPED, new List<string> { OrderStatus.DELIVERED, OrderStatus.CANCELLED } },
            { OrderStatus.DELIVERED, new List<string> { } }, // Không thể chuyển từ DELIVERED
            { OrderStatus.CANCELLED, new List<string> { } } // Không thể chuyển từ CANCELLED
        };

        if (!validTransitions.ContainsKey(currentStatus))
            return false;

        return validTransitions[currentStatus].Contains(newStatus);
    }

    private async Task RestoreStockAsync(Order order)
    {
        var stockRepo = _uow.GetRepository<Stock>();
        var stockMovementRepo = _uow.GetRepository<StockMovement>();

        // Lấy các StockMovement liên quan đến đơn hàng này (chỉ lấy những record xuất kho - Quantity < 0)
        var movements = await stockMovementRepo.GetAllAsync(
            sm => sm.Orderid == order.Orderid && sm.Quantity.HasValue && sm.Quantity < 0
        );

        if (movements == null || !movements.Any())
        {
            // Nếu không tìm thấy StockMovement, có thể đơn hàng này chưa có stock được trừ
            // Hoặc đã được hoàn lại rồi - không cần làm gì
            return;
        }

        foreach (var movement in movements)
        {
            if (movement.Stockid == null)
                continue;

            var stock = await stockRepo.GetByIdAsync(movement.Stockid);
            if (stock != null)
            {
                var quantityToRestore = Math.Abs(movement.Quantity ?? 0);
                stock.Stockquantity = (stock.Stockquantity ?? 0) + quantityToRestore;
                
                // Nếu stock đang OUT_OF_STOCK và sau khi hoàn lại có số lượng > 0, chuyển về ACTIVE
                if (stock.Status == StockStatus.OUT_OF_STOCK && stock.Stockquantity > 0)
                    stock.Status = StockStatus.ACTIVE;
                
                stockRepo.Update(stock);

                // Tạo StockMovement mới để ghi log hoàn lại
                var restoreMovement = new StockMovement
                {
                    Stockid = stock.Stockid,
                    Orderid = order.Orderid,
                    Quantity = quantityToRestore, // Số dương để thể hiện nhập lại
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

    public async Task TryAllocateStockAfterPaymentAsync(int orderId)
    {
        if (orderId <= 0) throw new Exception("orderId is required.");

        var orderRepo = _uow.GetRepository<Order>();
        var stockRepo = _uow.GetRepository<Stock>();
        var movementRepo = _uow.GetRepository<StockMovement>();

        // load order + details
        var order = await orderRepo.FindAsync(
            o => o.Orderid == orderId,
            include: q => q.Include(x => x.OrderDetails)
        );

        if (order == null) throw new Exception("Order not found.");
        if (order.OrderDetails == null || order.OrderDetails.Count == 0)
            throw new Exception("Order has no details.");


        var status = (order.Status ?? OrderStatus.PENDING).ToUpper();

        // Idempotent: nếu đã có movement OUT rồi thì coi như đã allocate
        var existingOutMovements = await movementRepo.GetAllAsync(sm =>
            sm.Orderid == orderId && sm.Quantity.HasValue && sm.Quantity < 0
        );
        if (existingOutMovements.Any()) return;

        // Chỉ chạy auto allocate sau payment nếu đang CONFIRMED
        if (status != OrderStatus.CONFIRMED)
            return;

        // 1) Check đủ kho cho tất cả items trước khi trừ
        foreach (var d in order.OrderDetails)
        {
            var pid = d.Productid ?? 0;
            var qty = d.Quantity ?? 0;
            if (pid <= 0 || qty <= 0) continue;

            var stocks = await stockRepo.FindAsync(s => s.Productid == pid && s.Status == StockStatus.ACTIVE);
            var totalStock = stocks.Sum(s => s.Stockquantity ?? 0);

            if (totalStock < qty)
            {
                order.Status = OrderStatus.PAID_WAITING_STOCK;

                order.Note = string.IsNullOrWhiteSpace(order.Note)
                    ? $"[PAID_WAITING_STOCK] Thiếu hàng ProductId={pid}. Còn {totalStock}, cần {qty}."
                    : $"{order.Note}\n[PAID_WAITING_STOCK] Thiếu hàng ProductId={pid}. Còn {totalStock}, cần {qty}.";

                orderRepo.Update(order);
                await _uow.SaveAsync();
                return;
            }
        }

        // 2) Deduct theo FIFO trong transaction
        _uow.BeginTransaction();
        try
        {
            foreach (var d in order.OrderDetails)
            {
                var pid = d.Productid ?? 0;
                var need = d.Quantity ?? 0;
                if (pid <= 0 || need <= 0) continue;

                var availableStocks = (await stockRepo.FindAsync(
                        s => s.Productid == pid && s.Status == StockStatus.ACTIVE
                    ))
                    .OrderBy(s => s.Productiondate) // FIFO
                    .ToList();

                var remaining = need;

                foreach (var stock in availableStocks)
                {
                    if (remaining <= 0) break;

                    var stockQty = stock.Stockquantity ?? 0;
                    if (stockQty <= 0) continue;

                    var deduct = Math.Min(remaining, stockQty);

                    stock.Stockquantity = stockQty - deduct;
                    if ((stock.Stockquantity ?? 0) <= 0)
                        stock.Status = StockStatus.OUT_OF_STOCK;

                    stockRepo.Update(stock);

                    await movementRepo.AddAsync(new StockMovement
                    {
                        Stockid = stock.Stockid,
                        Orderid = order.Orderid,
                        Quantity = -deduct,
                        Movementdate = DateTime.Now,
                        Note = $"Xuất kho cho đơn hàng #{order.Orderid}"
                    });

                    remaining -= deduct;
                }

                if (remaining > 0)
                    throw new Exception($"Unexpected thiếu hàng khi xuất kho (ProductId={pid}).");
            }

            order.Status = OrderStatus.PROCESSING;
            order.Note = string.IsNullOrWhiteSpace(order.Note)
                ? $"[ALLOCATED] Auto allocated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                : $"{order.Note}\n[ALLOCATED] Auto allocated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            orderRepo.Update(order);

            await _uow.SaveAsync();
            _uow.CommitTransaction();
        }
        catch (Exception ex)
        {
            _uow.RollBack();

            // nếu fail thì PAID_WAITING_STOCK
            order.Status = OrderStatus.PAID_WAITING_STOCK;
            order.Note = string.IsNullOrWhiteSpace(order.Note)
                ? $"[PAID_WAITING_STOCK] {ex.Message}"
                : $"{order.Note}\n[PAID_WAITING_STOCK] {ex.Message}";

            orderRepo.Update(order);
            await _uow.SaveAsync();
        }
    }


    public async Task AllocateStockForWaitingOrderAsync(int orderId)
    {
        if (orderId <= 0) throw new Exception("orderId is required.");

        var orderRepo = _uow.GetRepository<Order>();

        var order = (await orderRepo.FindAsync(
            o => o.Orderid == orderId,
            include: q => q.Include(o => o.OrderDetails)
        ));

        if (order == null) throw new Exception("Order not found.");

        var status = (order.Status ?? "").ToUpper();
        if (status != OrderStatus.PAID_WAITING_STOCK)
            throw new Exception("Order is not in PAID_WAITING_STOCK.");

        throw new Exception("Use ForceAllocateStockAsync for STAFF/ADMIN allocation retry.");
    }


    public async Task ForceAllocateStockAsync(int orderId, int actorAccountId, string actorRole)
    {
        if (orderId <= 0) throw new Exception("orderId is required.");
        if (actorAccountId <= 0) throw new Exception("actorAccountId is required.");
        if (string.IsNullOrWhiteSpace(actorRole)) throw new Exception("actorRole is required.");

        var role = actorRole.Trim().ToUpper();
        if (role != "ADMIN" && role != "STAFF")
            throw new Exception("Forbidden.");

        var orderRepo = _uow.GetRepository<Order>();
        var stockRepo = _uow.GetRepository<Stock>();
        var movementRepo = _uow.GetRepository<StockMovement>();

        var order = await orderRepo.FindAsync(
            o => o.Orderid == orderId,
            include: q => q.Include(o => o.OrderDetails)
        );

        if (order == null) throw new Exception("Order not found.");
        if (order.OrderDetails == null || order.OrderDetails.Count == 0)
            throw new Exception("Order has no details.");

        var status = (order.Status ?? OrderStatus.PENDING).ToUpper();

        if (status != OrderStatus.PAID_WAITING_STOCK && status != OrderStatus.CONFIRMED)
            throw new Exception("Order is not eligible for allocation.");

        var existingOutMovements = await movementRepo.GetAllAsync(sm =>
            sm.Orderid == orderId && sm.Quantity.HasValue && sm.Quantity < 0
        );
        if (existingOutMovements.Any())
            throw new Exception("Order already allocated stock.");

        foreach (var d in order.OrderDetails)
        {
            var pid = d.Productid ?? 0;
            var need = d.Quantity ?? 0;
            if (pid <= 0 || need <= 0) continue;

            var stocks = await stockRepo.FindAsync(s => s.Productid == pid && s.Status == StockStatus.ACTIVE);
            var totalStock = stocks.Sum(s => s.Stockquantity ?? 0);

            if (totalStock < need)
            {
                // vẫn có thể set PAID_WAITING_STOCK để lưu trạng thái
                order.Status = OrderStatus.PAID_WAITING_STOCK;
                order.Note = string.IsNullOrWhiteSpace(order.Note)
                    ? $"[PAID_WAITING_STOCK] Thiếu hàng ProductId={pid}. Còn {totalStock}, cần {need}."
                    : $"{order.Note}\n[PAID_WAITING_STOCK] Thiếu hàng ProductId={pid}. Còn {totalStock}, cần {need}.";

                orderRepo.Update(order);
                await _uow.SaveAsync();

                // throw để controller trả lỗi thiếu hàng
                throw new Exception($"Thiếu hàng (ProductId={pid}). Còn {totalStock}, cần {need}.");
            }
        }

        var originalStatus = order.Status;

        if ((order.Status ?? "").ToUpper() == OrderStatus.PAID_WAITING_STOCK)
        {
            order.Status = OrderStatus.CONFIRMED;
            orderRepo.Update(order);
            await _uow.SaveAsync();
        }

        await TryAllocateStockAfterPaymentAsync(orderId);

        var outMovementsAfter = await movementRepo.GetAllAsync(sm =>
            sm.Orderid == orderId && sm.Quantity.HasValue && sm.Quantity < 0
        );

        var updated = (await orderRepo.FindAsync(o => o.Orderid == orderId)).FirstOrDefault();
        if (updated == null) throw new Exception("Order not found after allocation.");

        if (outMovementsAfter.Any())
        {
            updated.Status = OrderStatus.PROCESSING;
            updated.Note = string.IsNullOrWhiteSpace(updated.Note)
                ? $"[ALLOCATED] Allocated by {role}:{actorAccountId} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                : $"{updated.Note}\n[ALLOCATED] Allocated by {role}:{actorAccountId} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            orderRepo.Update(updated);
            await _uow.SaveAsync();
            return;
        }

        foreach (var d in updated.OrderDetails ?? new List<OrderDetail>())
        {
            var pid = d.Productid ?? 0;
            var need = d.Quantity ?? 0;
            if (pid <= 0 || need <= 0) continue;

            var stocks = await stockRepo.FindAsync(s => s.Productid == pid && s.Status == StockStatus.ACTIVE);
            var totalStock = stocks.Sum(s => s.Stockquantity ?? 0);

            if (totalStock < need)
            {
                throw new Exception($"Thiếu hàng (ProductId={pid}). Còn {totalStock}, cần {need}.");
            }
        }

        if ((originalStatus ?? "").ToUpper() == OrderStatus.PAID_WAITING_STOCK)
        {
            updated.Status = OrderStatus.PAID_WAITING_STOCK;
            orderRepo.Update(updated);
            await _uow.SaveAsync();
        }

        throw new Exception("Allocate failed unexpectedly. Please check stock data and allocation logic.");
    }



}
