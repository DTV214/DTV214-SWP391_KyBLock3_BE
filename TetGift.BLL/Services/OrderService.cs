using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _emailTemplateRenderer;
    private readonly IConfiguration _configuration;

    public OrderService(IUnitOfWork uow, ICartService cartService, IPromotionService promotionService, IAccountPromotionService accountPromotionService, IEmailSender emailSender,
         IEmailTemplateRenderer emailTemplateRenderer,
         IConfiguration configuration)
    {
        _uow = uow;
        _cartService = cartService;
        _promotionService = promotionService;
        _accountPromotionService = accountPromotionService;
        _emailSender = emailSender;
        _emailTemplateRenderer = emailTemplateRenderer;
        _configuration = configuration;
    }

    public async Task<OrderResponseDto> CreateOrderFromCartAsync(int accountId, CreateOrderRequest request)
    {
        var cart = await _cartService.GetCartByAccountIdAsync(accountId);
        if (cart.ItemCount == 0)
            throw new Exception("Giỏ hàng trống, không thể tạo đơn hàng.");

        ValidateVatRequest(request);

        decimal finalPriceAfterPromotion = cart.TotalPrice;
        int promoId = 0;

        if (!string.IsNullOrWhiteSpace(request.PromotionCode))
        {
            var promoRepo = _uow.GetRepository<Promotion>();
            var promo = await _promotionService.GetCodeAsync(request.PromotionCode);

            var promoResult = promo.ApplyPromotion((double)cart.TotalPrice);
            if (!promoResult.Item2)
                throw new Exception(promoResult.Item3);

            promoId = promo.Promotionid;
            finalPriceAfterPromotion = RoundMoney((decimal)promoResult.Item1);

            if (promo.IsLimited ?? false)
            {
                var isApplied = await _accountPromotionService.UsePromotionAsync(accountId, promoId);
                if (!isApplied)
                    throw new Exception("Lỗi khi áp dụng mã giảm giá");

                promo.UsedCount++;
                await promoRepo.UpdateAsync(promo);
            }
        }

        // Validate stock
        var stockRepo = _uow.GetRepository<Stock>();
        var productRepo = _uow.GetRepository<Product>();

        foreach (var item in cart.Items)
        {
            var product = await productRepo.GetByIdAsync(item.ProductId)
                ?? throw new Exception($"Sản phẩm '{item.ProductName}' không tồn tại.");

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

        var requireVat = request.RequireVatInvoice;
        var vatRate = requireVat ? DefaultVatRate : 0m;
        var vatAmount = requireVat ? RoundMoney(finalPriceAfterPromotion * vatRate) : 0m;

        var orderRepo = _uow.GetRepository<Order>();
        var order = new Order
        {
            Accountid = accountId,
            Totalprice = finalPriceAfterPromotion, // GIỮ NGUYÊN LOGIC CŨ
            Status = OrderStatus.PENDING,
            Customername = request.CustomerName,
            Customerphone = request.CustomerPhone,
            Customeremail = request.CustomerEmail,
            Customeraddress = request.CustomerAddress,
            Note = request.Note,
            Orderdatetime = DateTime.Now,

            RequireVatInvoice = requireVat,
            VatRate = vatRate,
            VatAmount = vatAmount,
            VatCompanyName = requireVat ? request.VatCompanyName : null,
            VatCompanyTaxCode = requireVat ? request.VatCompanyTaxCode : null,
            VatCompanyAddress = requireVat ? request.VatCompanyAddress : null,
            VatInvoiceEmail = requireVat ? request.VatInvoiceEmail : null
        };

        if (promoId != 0)
            order.Promotionid = promoId;

        await orderRepo.AddAsync(order);
        await _uow.SaveAsync();

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

        await _cartService.ClearCartAsync(accountId);

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

    // New: product association analysis (bought together)
    public async Task<List<ProductAssociationDto>> GetProductAssociationsAsync(int productId, int top = 10, int minSupport = 1)
    {
        if (productId <= 0) throw new ArgumentException("productId is required.", nameof(productId));
        if (top <= 0) top = 10;
        if (minSupport < 1) minSupport = 1;

        var orderRepo = _uow.GetRepository<Order>();

        // Consider only paid/confirmed orders (same approach used elsewhere)
        var paidStatuses = new[] { OrderStatus.CONFIRMED, OrderStatus.PROCESSING, OrderStatus.SHIPPED, OrderStatus.DELIVERED, OrderStatus.PAID_WAITING_STOCK };

        var ordersWithTarget = await orderRepo.Entities
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.Payments)
            .Where(o =>
                o.OrderDetails.Any(od => od.Productid == productId)
                &&
                (o.Payments.Any(p => p.Status == PaymentStatus.SUCCESS) ||
                 (o.Status != null && paidStatuses.Contains(o.Status)))
            )
            .ToListAsync();

        if (ordersWithTarget == null || ordersWithTarget.Count == 0)
            return new List<ProductAssociationDto>();

        var coCounts = new Dictionary<int, (string? name, int orderCount)>();

        foreach (var order in ordersWithTarget)
        {
            var coProductIds = order.OrderDetails
                .Where(od => od.Productid.HasValue && od.Productid.Value != productId)
                .Select(od => od.Productid!.Value)
                .Distinct();

            foreach (var pid in coProductIds)
            {
                var prodName = order.OrderDetails.FirstOrDefault(od => od.Productid == pid)?.Product?.Productname ?? $"Product {pid}";
                if (coCounts.TryGetValue(pid, out var entry))
                    coCounts[pid] = (entry.name ?? prodName, entry.orderCount + 1);
                else
                    coCounts[pid] = (prodName, 1);
            }
        }

        var totalOrdersWithTarget = ordersWithTarget.Count;

        var results = coCounts
            .Where(kv => kv.Value.orderCount >= minSupport)
            .Select(kv => new ProductAssociationDto
            {
                ProductId = kv.Key,
                ProductName = kv.Value.name,
                CoPurchaseCount = kv.Value.orderCount,
                SupportPercentage = Math.Round((double)kv.Value.orderCount / totalOrdersWithTarget * 100.0, 2)
            })
            .OrderByDescending(x => x.CoPurchaseCount)
            .ThenByDescending(x => x.SupportPercentage)
            .Take(top)
            .ToList();

        return results;
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

        // Nếu HỦY ĐƠN trực tiếp từ hàm này (Trường hợp Admin ép hủy), hoàn lại stock
        if (newStatus == OrderStatus.CANCELLED && currentStatus != OrderStatus.CANCELLED)
        {
            await RestoreStockAsync(order);
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

    // ========================================================
    // LUỒNG HỦY ĐƠN 
    // ========================================================

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
        var normalizedRole = userRole.ToUpper();

        // 1. Validate: Cấm hủy nếu đã qua bước xử lý đóng gói
        if (currentStatus == OrderStatus.PROCESSING || currentStatus == OrderStatus.SHIPPED || currentStatus == OrderStatus.DELIVERED)
            throw new Exception("Đơn hàng đang xử lý hoặc đã giao, không thể yêu cầu hủy.");

        if (currentStatus == OrderStatus.CANCELLED || currentStatus == "CANCEL_REQUESTED")
            throw new Exception("Đơn hàng đã hủy hoặc đang chờ duyệt hủy.");

        // 2. Phân luồng xử lý (Chỉ các trạng thái PENDING, CONFIRMED, PAID_WAITING_STOCK mới lọt vào đây)
        if (normalizedRole == "ADMIN")
        {
            // Admin thì cho phép hủy thẳng và hoàn kho luôn (Quyền tối cao)
            _uow.BeginTransaction();
            try
            {
                await RestoreStockAsync(order);
                order.Status = OrderStatus.CANCELLED;
                orderRepo.Update(order);
                await _uow.SaveAsync();
                _uow.CommitTransaction();
            }
            catch
            {
                _uow.RollBack();
                throw;
            }
        }
        else
        {
            // Customer hoặc Staff: Validate sở hữu và chuyển sang trạng thái chờ duyệt (CANCEL_REQUESTED)
            if (normalizedRole != "STAFF" && order.Accountid != accountId)
                throw new Exception("Bạn không có quyền hủy đơn hàng này.");

            order.Status = "CANCEL_REQUESTED";
            orderRepo.Update(order);
            await _uow.SaveAsync();
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

    public async Task<OrderResponseDto> AdminApproveRefundAsync(int orderId, int adminAccountId)
    {
        var orderRepo = _uow.GetRepository<Order>();
        var paymentRepo = _uow.GetRepository<Payment>();

        var order = await orderRepo.FindAsync(
            o => o.Orderid == orderId,
            include: q => q.Include(o => o.OrderDetails).ThenInclude(od => od.Product)
        );
        if (order == null) throw new Exception("Không tìm thấy đơn hàng.");

        if (order.Status != "CANCEL_REQUESTED")
            throw new Exception("Đơn hàng không ở trạng thái yêu cầu hủy.");

        _uow.BeginTransaction();
        try
        {
            // 1. Chính thức hoàn kho
            await RestoreStockAsync(order);

            // 2. Chuyển trạng thái Payment sang REFUNDED (Để bảo vệ số liệu Dashboard)
            var payments = await paymentRepo.FindAsync(p => p.Orderid == orderId && p.Status == PaymentStatus.SUCCESS);
            var payment = payments.FirstOrDefault();
            if (payment != null)
            {
                payment.Status = "REFUNDED";
                paymentRepo.Update(payment);
            }

            // 3. Cập nhật Order sang CANCELLED
            order.Status = OrderStatus.CANCELLED;
            order.Note = (order.Note ?? "") + $"\n[ADMIN APPROVED] Đã hoàn tiền & hủy đơn bởi Admin Id: {adminAccountId} lúc {DateTime.Now}";
            orderRepo.Update(order);

            await _uow.SaveAsync();
            _uow.CommitTransaction();
        }
        catch (Exception)
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
            { OrderStatus.PENDING, new List<string> { OrderStatus.CONFIRMED, OrderStatus.CANCELLED, "CANCEL_REQUESTED" } },
            { OrderStatus.CONFIRMED, new List<string> { OrderStatus.PROCESSING, OrderStatus.CANCELLED, "CANCEL_REQUESTED" } },
            { OrderStatus.PAID_WAITING_STOCK, new List<string> { OrderStatus.CONFIRMED, "CANCEL_REQUESTED", OrderStatus.CANCELLED } },
            
            // XÓA CANCEL_REQUESTED Ở TRẠNG THÁI PROCESSING
            { OrderStatus.PROCESSING, new List<string> { OrderStatus.SHIPPED, OrderStatus.CANCELLED } },

            { OrderStatus.SHIPPED, new List<string> { OrderStatus.DELIVERED, OrderStatus.CANCELLED } },
            { "CANCEL_REQUESTED", new List<string> { OrderStatus.CANCELLED } },
            { OrderStatus.DELIVERED, new List<string> { } },
            { OrderStatus.CANCELLED, new List<string> { } }
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

    private string GetFriendlyOrderStatus(string status)
    {
        return (status ?? "").ToUpper() switch
        {
            OrderStatus.PENDING => "Chờ xác nhận",
            OrderStatus.CONFIRMED => "Đã thanh toán",
            "CANCEL_REQUESTED" => "Đang chờ duyệt hủy đơn",
            OrderStatus.PROCESSING => "Đang xử lý",
            OrderStatus.SHIPPED => "Đã giao",
            OrderStatus.DELIVERED => "Đã nhận",
            OrderStatus.CANCELLED => "Đã hủy",
            OrderStatus.PAID_WAITING_STOCK => "Đã thanh toán - chờ nhập kho",
            _ => status
        };
    }

    // ========================================================
    // CÁC HÀM CŨ GIỮ NGUYÊN (KHÔNG ĐỤNG ĐẾN)
    // ========================================================

    public async Task TryAllocateStockAfterPaymentAsync(int orderId)
    {
        if (orderId <= 0) throw new Exception("orderId is required.");

        var orderRepo = _uow.GetRepository<Order>();
        var stockRepo = _uow.GetRepository<Stock>();
        var movementRepo = _uow.GetRepository<StockMovement>();
        var productRepo = _uow.GetRepository<Product>();

        // load order + details + product (including child products' ImportPrice)
        var order = await orderRepo.FindAsync(
            o => o.Orderid == orderId,
            include: q => q.Include(x => x.OrderDetails)
                .ThenInclude(od => od.Product)
                    .ThenInclude(p => p.ProductDetailProductparents)
                        .ThenInclude(pd => pd.Product) // ensure child product import prices are available
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

        // 2) Deduct theo FIFO trong transaction + compute cost & actual revenue
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

        // After successful stock deduction compute costs and actual revenue
        // Compute total price before discount (sum of detail amounts or fallback)
        decimal totalBeforeDiscount = 0m;
        foreach (var od in order.OrderDetails)
        {
            totalBeforeDiscount += od.Amount ?? ((od.Product?.Price ?? 0m) * (od.Quantity ?? 0));
        }

        decimal finalPaid = order.Totalprice ?? totalBeforeDiscount;
        decimal discountValue = totalBeforeDiscount - finalPaid; // may be 0

        // Compute total cost (import price)
        decimal totalCost = 0m;
        foreach (var od in order.OrderDetails)
        {
            var qty = od.Quantity ?? 0;
            var product = od.Product;

            if (product != null && product.Configid != null && product.ProductDetailProductparents != null && product.ProductDetailProductparents.Any())
            {
                // Basket/combo: sum child's import price * childQty * number of baskets
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

        // Actual revenue = amount received after discount - totalCost
        decimal actualRevenue = finalPaid - totalCost;

        // Persist totals onto Order
        order.Totalprice = finalPaid;
        order.ActualRevenue = actualRevenue;

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
                order.Status = OrderStatus.PAID_WAITING_STOCK;
                orderRepo.Update(order);
                await _uow.SaveAsync();

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
            ? $"http://160.187.229.26/account/orders/{order.Orderid}"
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

        var finalPrice = order.Totalprice ?? totalPrice;
        var discountValue = totalPrice - finalPrice;
        if (discountValue < 0) discountValue = 0;

        var vatAmount = order.RequireVatInvoice ? order.VatAmount : 0m;
        var finalPayableAmount = finalPrice + vatAmount;

        return new OrderResponseDto
        {
            OrderId = order.Orderid,
            AccountId = order.Accountid ?? 0,
            OrderDateTime = order.Orderdatetime,

            TotalPrice = totalPrice,
            DiscountValue = discountValue > 0 ? discountValue : null,
            FinalPrice = finalPrice,

            ActualRevenue = order.ActualRevenue,
            Status = order.Status,
            CustomerName = order.Customername,
            CustomerPhone = order.Customerphone,
            CustomerEmail = order.Customeremail,
            CustomerAddress = order.Customeraddress,
            Note = order.Note,
            PromotionCode = order.Promotion?.Code,
            ShippedDate = order.Shippeddate,
            QuotationId = order.Quotationid,

            RequireVatInvoice = order.RequireVatInvoice,
            VatRate = order.VatRate,
            VatAmount = vatAmount,
            FinalPayableAmount = finalPayableAmount,
            VatCompanyName = order.VatCompanyName,
            VatCompanyTaxCode = order.VatCompanyTaxCode,
            VatCompanyAddress = order.VatCompanyAddress,
            VatInvoiceEmail = order.VatInvoiceEmail,

            Feedback = order.Feedbacks != null && order.Feedbacks.Any() && order.Feedbacks.First().Isdeleted != true
                ? new FeedbackResponseDto
                {
                    FeedbackId = order.Feedbacks.First().Feedbackid,
                    OrderId = order.Orderid,
                    Rating = order.Feedbacks.First().Rating ?? 0,
                    Comment = order.Feedbacks.First().Comment,
                    CustomerName = null
                }
                : null,

            Items = items
        };
    }

    //Helper VAT
    private const decimal DefaultVatRate = 0.08m;

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static void ValidateVatRequest(CreateOrderRequest request)
    {
        if (!request.RequireVatInvoice)
            return;

        if (string.IsNullOrWhiteSpace(request.VatCompanyName))
            throw new Exception("Vui lòng nhập tên công ty để xuất hóa đơn VAT.");

        if (string.IsNullOrWhiteSpace(request.VatCompanyTaxCode))
            throw new Exception("Vui lòng nhập mã số thuế để xuất hóa đơn VAT.");

        if (string.IsNullOrWhiteSpace(request.VatCompanyAddress))
            throw new Exception("Vui lòng nhập địa chỉ công ty để xuất hóa đơn VAT.");

        if (string.IsNullOrWhiteSpace(request.VatInvoiceEmail))
            request.VatInvoiceEmail = request.CustomerEmail;
    }
}

