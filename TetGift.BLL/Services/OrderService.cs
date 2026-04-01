using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly IAccountPromotionService _accountPromotionService;
    private readonly IWalletService _walletService;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _emailTemplateRenderer;
    private readonly IConfiguration _configuration;

    public OrderService(IUnitOfWork uow, ICartService cartService, IPromotionService promotionService, IWalletService walletService, IAccountPromotionService accountPromotionService, IEmailSender emailSender,
        IEmailTemplateRenderer emailTemplateRenderer,
        IConfiguration configuration)
    {
        _uow = uow;
        _cartService = cartService;
        _promotionService = promotionService;
        _walletService = walletService;
        _accountPromotionService = accountPromotionService;
        _emailSender = emailSender;
        _emailTemplateRenderer = emailTemplateRenderer;
        _configuration = configuration;
    }

    public async Task<OrderResponseDto> CreateOrderFromCartAsync(int accountId, CreateOrderRequest request)
    {
        // 1. Lấy Cart (sử dụng ICartService - kế thừa code cũ)
        var cart = await _cartService.GetCartByAccountIdAsync(accountId);
        if (cart.ItemCount == 0)
            throw new Exception("Giỏ hàng trống, không thể tạo đơn hàng.");

        var discountValue = 0d;
        var promoId = 0;

        if (!string.IsNullOrWhiteSpace(request.PromotionCode))
        {
            var promoRepo = _uow.GetRepository<Promotion>();
            var promoResult = (0d, false, "");
            var promo = await _promotionService.GetCodeAsync(request.PromotionCode);

            promoResult = promo.ApplyPromotion((double)cart.TotalPrice);
            if (promoResult.Item2 == false)
            {
                throw new Exception(promoResult.Item3);
            }

            promoId = promo.Promotionid;
            discountValue = promoResult.Item1;

            // Nếu là limited thì chỉnh bảng
            if (promo.IsLimited ?? false)
            {
                var isApplied = await _accountPromotionService.UsePromotionAsync(accountId, promoId);
                if (!isApplied)
                    throw new Exception("Lỗi khi áp dụng mã giảm giá");
                promo.UsedCount++;
                await promoRepo.UpdateAsync(promo);
            }
        }

        // 3. Validate Stock availability cho từng sản phẩm
        var stockRepo = _uow.GetRepository<Stock>();
        var productRepo = _uow.GetRepository<Product>();
        foreach (var item in cart.Items)
        {
            var product = await productRepo.GetByIdAsync(item.ProductId)
                ?? throw new Exception($"Sản phẩm '{item.ProductName}' không tồn tại.");

            // Xử lí sản phẩm giỏ
            if (product.Configid != null && product.Configid != 0)
            {
                var productDetails = product.ProductDetailProductparents;
                foreach (var productItem in productDetails)
                {
                    var stocks = await stockRepo.FindAsync(
                        s => s.Productid == productItem.Productid && s.Status == StockStatus.ACTIVE
                        );

                    var totalStock = stocks.Sum(s => s.Stockquantity ?? 0);
                    if (totalStock < productItem.Quantity)
                    {
                        throw new Exception($"Sản phẩm '{item.ProductName}' không đủ số lượng trong kho. Còn lại: {totalStock}, yêu cầu: {productItem.Quantity}");
                    }
                }
            }

            // Xử lí sản phẩm thường
            else
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
        }

        // 4. Tạo Order
        var orderRepo = _uow.GetRepository<Order>();
        var order = new Order
        {
            Accountid = accountId,
            Totalprice = discountValue == 0 ? cart.TotalPrice : (decimal)discountValue,
            Status = OrderStatus.PENDING,
            Customername = request.CustomerName,
            Customerphone = request.CustomerPhone,
            Customeremail = request.CustomerEmail,
            Customeraddress = request.CustomerAddress,
            Note = request.Note,
            Orderdatetime = DateTime.Now
        };

        if (promoId != 0)
            order.Promotionid = promoId;

        await orderRepo.AddAsync(order);
        await _uow.SaveAsync();

        // 5. Tạo OrderDetails (Stock sẽ được trừ sau khi thanh toán thành công qua TryAllocateStockAfterPaymentAsync)
        var orderDetailRepo = _uow.GetRepository<OrderDetail>();

        foreach (var cartItem in cart.Items)
        {
            var orderDetail = new OrderDetail
            {
                Orderid = order.Orderid,
                Productid = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                Amount = cartItem.SubTotal
            };
            await orderDetailRepo.AddAsync(orderDetail);
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
                .Include(o => o.Feedbacks)
        );

        return MapToOrderResponseDto(fullOrder!);
    }

    public async Task<PagedResponse<OrderResponseDto>> GetAllOrdersAsync(OrderQueryParameters queryParams)
    {
        var orderRepo = _uow.GetRepository<Order>();
        var query = orderRepo.Entities.AsQueryable();

        #region Filter

        // Account Id
        if (queryParams.AccountId != 0)
            query = query.Where(o => o.Accountid == queryParams.AccountId);

        // Status
        if (!string.IsNullOrWhiteSpace(queryParams.Status))
            query = query.Where(o => o.Status == queryParams.Status);

        #endregion

        #region Paging

        var totalItems = query.Count();
        var pageNumber = queryParams.PageNumber ?? 1;
        var pageSize = queryParams.PageSize ?? 10;
        var orders = await query
            .Include(o => o.OrderDetails).ThenInclude(od => od.Product).ThenInclude(p => p.ProductDetailProductparents)
            .Include(o => o.Promotion)
            .Include(o => o.Feedbacks)
            .OrderByDescending(o => o.Orderdatetime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var pagedOrders = orders.Select(o => MapToOrderResponseDto(o)).ToList();

        #endregion

        return new PagedResponse<OrderResponseDto>(pagedOrders, totalItems, pageNumber, pageSize);
    }

    public async Task<OrderResponseDto> UpdateOrderShippingInfoAsync(int orderId, int accountId, string userRole, UpdateOrderShippingRequest request)
    {
        var orderRepo = _uow.GetRepository<Order>();
        var orders = await orderRepo.FindAsync(o => o.Orderid == orderId);

        if (!orders.Any())
            throw new Exception("Không tìm thấy đơn hàng.");

        var order = orders.FirstOrDefault();
        if (order == null)
            throw new Exception("Không tìm thấy đơn hàng.");

        // Kiểm tra quyền: Chỉ ADMIN hoặc chính chủ đơn hàng mới được sửa
        var normalizedRole = userRole.ToUpper();
        if (normalizedRole != "ADMIN" && order.Accountid != accountId)
        {
            throw new Exception("Bạn không có quyền chỉnh sửa thông tin đơn hàng này.");
        }

        // Chỉ cho phép sửa khi đơn hàng chưa giao hoặc chưa hủy (tùy nghiệp vụ của bạn)
        if (order.Status == OrderStatus.DELIVERED || order.Status == OrderStatus.CANCELLED)
        {
            throw new Exception("Không thể cập nhật thông tin cho đơn hàng đã hoàn tất hoặc đã hủy.");
        }

        // Cập nhật các trường nếu không null hoặc empty
        if (!string.IsNullOrWhiteSpace(request.CustomerName))
            order.Customername = request.CustomerName;

        if (!string.IsNullOrWhiteSpace(request.CustomerPhone))
            order.Customerphone = request.CustomerPhone;

        if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            order.Customeremail = request.CustomerEmail;

        if (!string.IsNullOrWhiteSpace(request.CustomerAddress))
            order.Customeraddress = request.CustomerAddress;

        // Riêng Note có thể cho phép xóa trắng nếu cần, 
        // nhưng theo yêu cầu của bạn "null/empty thì không sửa" nên tôi áp dụng logic tương tự
        if (!string.IsNullOrWhiteSpace(request.Note))
            order.Note = request.Note;

        orderRepo.Update(order);
        await _uow.SaveAsync();

        // Load lại dữ liệu đầy đủ để trả về
        return await GetOrderByIdAsync(orderId, order.Accountid ?? accountId);
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
                .Include(o => o.Feedbacks)
        );

        if (order == null)
            throw new Exception("Không tìm thấy đơn hàng.");

        return MapToOrderResponseDto(order);
    }

    public async Task<OrderResponseDto> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request, int accountId, string userRole)
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
        var normalizedRole = userRole.ToUpper();

        // === CUSTOMER: chỉ được xác nhận đã nhận hàng (SHIPPED → DELIVERED) ===
        if (normalizedRole == "CUSTOMER")
        {
            // Validate ownership: chỉ chủ đơn hàng
            if (order.Accountid != accountId)
                throw new Exception("Bạn không có quyền thao tác với đơn hàng này.");

            // Customer chỉ được phép: SHIPPED → DELIVERED
            if (currentStatus != OrderStatus.SHIPPED || newStatus != OrderStatus.DELIVERED)
                throw new Exception("Bạn chỉ có thể xác nhận đã nhận hàng khi đơn hàng đang ở trạng thái đang giao.");
        }

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

        // Ghi nhận thời điểm giao hàng để auto-confirm sau 3 ngày
        if (newStatus == OrderStatus.SHIPPED)
        {
            order.Shippeddate = DateTime.Now;
        }

        orderRepo.Update(order);
        await _uow.SaveAsync(); // Save tất cả thay đổi (bao gồm cả stock đã được restore)

        // Gửi mail sau khi update thành công
        try
        {
            await SendOrderStatusChangedEmailAsync(order);
        }
        catch
        {
            // Không throw để tránh update status thành công nhưng bị fail chỉ vì email
            // Sau này có thể log lại bằng ILogger nếu muốn
        }

        // Load lại với đầy đủ thông tin
        var updatedOrder = await orderRepo.FindAsync(
            o => o.Orderid == orderId,
            include: q => q
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Promotion)
                .Include(o => o.Feedbacks)
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

            // Validate order status
            if (order.Status != OrderStatus.PENDING &&
                order.Status != OrderStatus.CONFIRMED &&
                order.Status != OrderStatus.PAID_WAITING_STOCK)
            {
                throw new Exception("Chỉ có thể hủy đơn hàng trong giai đoạn chưa xử lí đơn hàng.");
            }
        }

        _uow.BeginTransaction();

        try
        {
            // Process cancellation
            await RestoreStockAsync(order);

            // Hoàn tiền vào ví (nếu đã thanh toán)
            await _walletService.RefundToWalletAsync(orderId);

            // Update order status
            order.Status = OrderStatus.CANCELLED;
            orderRepo.Update(order);
            await _uow.SaveAsync();

            _uow.CommitTransaction();
        }
        catch (Exception e)
        {
            _uow.RollBack();
            throw;
        }

        // Load lại với đầy đủ thông tin
        var updatedOrder = await orderRepo.FindAsync(
            o => o.Orderid == orderId,
            include: q => q
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.Promotion)
                .Include(o => o.Feedbacks)
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

        if (movements == null || !movements.Any()) return;

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
                        ImageUrl = detail.Product.ImageUrl,
                        ProductDetails = detail.Product.ProductDetailProductparents?.Select(pd => new ProductDetailResponse
                        {
                            Productdetailid = pd.Productdetailid,
                            Productparentid = pd.Productparentid,
                            Productid = pd.Productid,
                            Categoryid = pd.Product?.Categoryid,
                            Productname = pd.Product?.Productname,
                            Unit = pd.Product?.Unit,
                            Price = pd.Product?.Price,
                            Imageurl = pd.Product?.ImageUrl,
                            Quantity = pd.Quantity,
                            ChildProduct = null
                        }).ToList()
                    });

                    totalPrice += amount;
                }
            }
        }

        var discountValue = 0m;
        var promotionCode = "";
        if (order.Promotion != null)
        {
            if (order.Promotion.IsPercentage ?? false)
            {
                discountValue = order.Totalprice ?? 0 * (order.Promotion.Discountvalue ?? 0 / 100);

                if (discountValue > order.Promotion.MaxDiscountPrice)
                {
                    discountValue = order.Promotion.MaxDiscountPrice ?? 0;
                }
            }
            else
            {
                discountValue = order.Promotion.Discountvalue ?? 0;
            }



            promotionCode = order.Promotion.Code ?? "";
        }

        return new OrderResponseDto
        {
            OrderId = order.Orderid,
            AccountId = order.Accountid ?? 0,
            OrderDateTime = order.Orderdatetime,
            TotalPrice = totalPrice,
            DiscountValue = discountValue > 0 ? discountValue : null,
            FinalPrice = order.Totalprice.Value,
            Status = order.Status,
            CustomerName = order.Customername,
            CustomerPhone = order.Customerphone,
            CustomerEmail = order.Customeremail,
            CustomerAddress = order.Customeraddress,
            Note = order.Note,
            PromotionCode = !string.IsNullOrEmpty(promotionCode) ? promotionCode : null,
            ShippedDate = order.Shippeddate,
            isQuotation = order.isQuotation,
            Feedback = order.Feedbacks != null && order.Feedbacks.Any() && order.Feedbacks.First().Isdeleted != true ? new FeedbackResponseDto
            {
                FeedbackId = order.Feedbacks.First().Feedbackid,
                OrderId = order.Orderid,
                Rating = order.Feedbacks.First().Rating ?? 0,
                Comment = order.Feedbacks.First().Comment,
                CustomerName = null // Mặc định trong OrderResponseDto không cần lấy tên, nếu cần thì phải Join với Account
            } : null,
            Items = items
        };
    }

    public async Task TryAllocateStockAfterPaymentAsync(int orderId)
    {
        if (orderId <= 0) throw new Exception("orderId is required.");

        var orderRepo = _uow.GetRepository<Order>();
        var stockRepo = _uow.GetRepository<Stock>();
        var movementRepo = _uow.GetRepository<StockMovement>();
        var productRepo = _uow.GetRepository<Product>();

        // load order + details + product (bao gồm cả ProductDetail cho sản phẩm giỏ)
        var order = await orderRepo.FindAsync(
            o => o.Orderid == orderId,
            include: q => q.Include(x => x.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.ProductDetailProductparents)
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

            var product = d.Product;

            // Sản phẩm giỏ: check stock từng sản phẩm con
            if (product != null && product.Configid != null && product.Configid != 0)
            {
                var childProducts = product.ProductDetailProductparents;
                if (childProducts != null)
                {
                    foreach (var child in childProducts)
                    {
                        var childPid = child.Productid ?? 0;
                        var childNeed = (child.Quantity ?? 0) * qty; // số lượng con * số giỏ
                        if (childPid <= 0 || childNeed <= 0) continue;

                        var stocks = await stockRepo.FindAsync(s => s.Productid == childPid && s.Status == StockStatus.ACTIVE);
                        var totalStock = stocks.Sum(s => s.Stockquantity ?? 0);

                        if (totalStock < childNeed)
                        {
                            order.Status = OrderStatus.PAID_WAITING_STOCK;
                            orderRepo.Update(order);
                            await _uow.SaveAsync();
                            return;
                        }
                    }
                }
            }
            // Sản phẩm thường: check stock trực tiếp
            else
            {
                var stocks = await stockRepo.FindAsync(s => s.Productid == pid && s.Status == StockStatus.ACTIVE);
                var totalStock = stocks.Sum(s => s.Stockquantity ?? 0);

                if (totalStock < qty)
                {
                    order.Status = OrderStatus.PAID_WAITING_STOCK;
                    orderRepo.Update(order);
                    await _uow.SaveAsync();
                    return;
                }
            }
        }

        // 2) Deduct theo FIFO trong transaction
        _uow.BeginTransaction();
        try
        {
            foreach (var d in order.OrderDetails)
            {
                var pid = d.Productid ?? 0;
                var qty = d.Quantity ?? 0;
                if (pid <= 0 || qty <= 0) continue;

                var product = d.Product;

                // Sản phẩm giỏ: trừ stock từng sản phẩm con
                if (product != null && product.Configid != null && product.Configid != 0)
                {
                    var childProducts = product.ProductDetailProductparents;
                    if (childProducts != null)
                    {
                        foreach (var child in childProducts)
                        {
                            var childPid = child.Productid ?? 0;
                            var childNeed = (child.Quantity ?? 0) * qty;
                            if (childPid <= 0 || childNeed <= 0) continue;

                            var availableStocks = (await stockRepo.FindAsync(
                                    s => s.Productid == childPid && s.Status == StockStatus.ACTIVE
                                ))
                                .OrderBy(s => s.Productiondate) // FIFO
                                .ToList();

                            var remaining = childNeed;

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
                                    Note = $"Xuất kho sản phẩm con (ProductId={childPid}) cho đơn hàng #{order.Orderid}"
                                });

                                remaining -= deduct;
                            }

                            if (remaining > 0)
                                throw new Exception($"Unexpected thiếu hàng khi xuất kho sản phẩm con (ProductId={childPid}).");
                        }
                    }
                }
                // Sản phẩm thường: trừ stock trực tiếp
                else
                {
                    var need = qty;

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
            }

            // Status giữ nguyên CONFIRMED — Staff/Admin sẽ chuyển sang PROCESSING thủ công
            orderRepo.Update(order);

            await _uow.SaveAsync();
            _uow.CommitTransaction();
        }
        catch (Exception ex)
        {
            _uow.RollBack();

            // nếu fail thì PAID_WAITING_STOCK
            order.Status = OrderStatus.PAID_WAITING_STOCK;
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
                //order.Note = string.IsNullOrWhiteSpace(order.Note)
                //    ? $"[PAID_WAITING_STOCK] Thiếu hàng ProductId={pid}. Còn {totalStock}, cần {need}."
                //    : $"{order.Note}\n[PAID_WAITING_STOCK] Thiếu hàng ProductId={pid}. Còn {totalStock}, cần {need}.";

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

    private string GetFriendlyOrderStatus(string status)
    {
        return (status ?? "").ToUpper() switch
        {
            OrderStatus.PENDING => "Chờ xác nhận",
            OrderStatus.CONFIRMED => "Đã thanh toán",
            OrderStatus.PROCESSING => "Đang xử lý",
            OrderStatus.SHIPPED => "Đã giao",
            OrderStatus.DELIVERED => "Đã nhận",
            OrderStatus.CANCELLED => "Đã hủy",
            OrderStatus.PAID_WAITING_STOCK => "Đã thanh toán - chờ nhập kho",
            _ => status
        };
    }

    private async Task SendOrderStatusChangedEmailAsync(Order order)
    {
        if (order == null) return;
        if (string.IsNullOrWhiteSpace(order.Customeremail)) return;

        var customerName = string.IsNullOrWhiteSpace(order.Customername)
            ? "Quý khách"
            : order.Customername;

        var friendlyStatus = GetFriendlyOrderStatus(order.Status ?? string.Empty);

        var orderBaseUrl = _configuration["AppUrls:OrderDetail"];
        var orderLink = string.IsNullOrWhiteSpace(orderBaseUrl)
            ? $"http://14.225.207.221/account/orders/{order.Orderid}"
            : $"{orderBaseUrl.TrimEnd('/')}/{order.Orderid}";

        var orderItemsHtml = BuildOrderItemsHtml(order);

        var subject = $"[TetGift] Đơn hàng #{order.Orderid} đã được cập nhật trạng thái";
        var htmlBody = _emailTemplateRenderer.RenderOrderStatusChanged(
            customerName,
            order.Orderid,
            friendlyStatus,
            orderLink,
            orderItemsHtml
        );

        await _emailSender.SendAsync(order.Customeremail, subject, htmlBody);
    }

    private string BuildOrderItemsHtml(Order order)
    {
        if (order.OrderDetails == null || !order.OrderDetails.Any())
        {
            return @"<p style='margin:0; color:#777777; font-size:14px; line-height:1.6;'>
                    Không có thông tin sản phẩm.
                 </p>";
        }

        var rows = order.OrderDetails
            .Where(x => x.Product != null)
            .Select(detail =>
            {
                var productName = detail.Product?.Productname ?? "Sản phẩm";
                var quantity = detail.Quantity ?? 0;
                var amount = detail.Amount ?? 0;

                return $@"
                <div style='padding: 14px 0; border-bottom: 1px solid #F1D9D9;'>
                    <div style='font-size: 15px; font-weight: 700; color: #690000; margin-bottom: 6px;'>
                        {System.Net.WebUtility.HtmlEncode(productName)}
                    </div>
                    <div style='font-size: 14px; color: #666666; line-height: 1.7;'>
                        Số lượng: {quantity}<br/>
                        Thành tiền: {amount:N0} VNĐ
                    </div>
                </div>";
            });

        return string.Join("", rows);
    }

}
