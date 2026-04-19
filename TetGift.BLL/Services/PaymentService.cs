using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Common.VnPay;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IOrderService _orderService;
    private readonly IInvoiceService _invoiceService;

    public PaymentService(
        IUnitOfWork uow,
        IConfiguration configuration,
        IEmailSender emailSender,
        IEmailTemplateRenderer templateRenderer,
        IOrderService orderService,
        IInvoiceService invoiceService)
    {
        _uow = uow;
        _configuration = configuration;
        _emailSender = emailSender;
        _templateRenderer = templateRenderer;
        _orderService = orderService;
        _invoiceService = invoiceService;
    }

    public async Task<PaymentResponseDto> CreatePaymentAsync(int orderId, int accountId, string? clientIp = null, string? paymentMethod = null)
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
            throw new Exception("Không tìm thấy đơn hàng hoặc bạn không có quyền thanh toán đơn hàng này.");

        if (order.Status != OrderStatus.PENDING)
            throw new Exception("Chỉ có thể thanh toán cho đơn hàng đang chờ xác nhận.");

        var baseAmount = GetBaseAmount(order);
        var vatAmount = GetVatAmount(order);
        var payableAmount = GetFinalPayableAmount(order);

        var paymentRepo = _uow.GetRepository<Payment>();
        var existingPayments = await paymentRepo.FindAsync(
            p => p.Orderid == orderId && p.Status == PaymentStatus.SUCCESS
        );

        if (existingPayments.Any())
            throw new Exception("Đơn hàng này đã được thanh toán thành công.");

        var payment = new Payment
        {
            Orderid = orderId,
            Amount = payableAmount,
            Status = PaymentStatus.PENDING,
            Type = "ORDER_PAYMENT",
            Paymentmethod = paymentMethod ?? "VNPAY",
            CreatedDate = DateTime.UtcNow,
            Ispayonline = true
        };

        await paymentRepo.AddAsync(payment);
        await _uow.SaveAsync();

        var vnpUrl = _configuration["VnPay:Url"] ?? throw new Exception("Missing config: VnPay:Url");
        var vnpTmnCode = _configuration["VnPay:TmnCode"] ?? throw new Exception("Missing config: VnPay:TmnCode");
        var vnpHashSecret = _configuration["VnPay:HashSecret"] ?? throw new Exception("Missing config: VnPay:HashSecret");
        var vnpReturnUrl = _configuration["VnPay:ReturnUrl"] ?? throw new Exception("Missing config: VnPay:ReturnUrl");

        var vnpay = new VnPayLibrary();
        vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", vnpTmnCode);

        var vietnamTime = DateTime.Now.AddHours(7);

        vnpay.AddRequestData("vnp_Amount", ((long)(payableAmount * 100)).ToString());
        vnpay.AddRequestData("vnp_CreateDate", vietnamTime.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", clientIp ?? "127.0.0.1");
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang #{orderId}");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", vnpReturnUrl);
        vnpay.AddRequestData("vnp_TxnRef", payment.Paymentid.ToString());
        vnpay.AddRequestData("vnp_ExpireDate", vietnamTime.AddMinutes(60).ToString("yyyyMMddHHmmss"));

        var paymentUrl = vnpay.CreateRequestUrl(vnpUrl, vnpHashSecret);

        return new PaymentResponseDto
        {
            PaymentId = payment.Paymentid,
            OrderId = orderId,
            Amount = payableAmount,
            BaseAmount = baseAmount,
            VatAmount = vatAmount,
            FinalPayableAmount = payableAmount,
            RequireVatInvoice = order.RequireVatInvoice,
            PaymentUrl = paymentUrl,
            CreatedDate = payment.CreatedDate,
            Status = PaymentStatus.PENDING
        };
    }

    public async Task<PaymentResultDto> ProcessIpnCallbackAsync(Dictionary<string, string> queryParams)
    {
        var vnpHashSecret = _configuration["VnPay:HashSecret"] ?? throw new Exception("Missing config: VnPay:HashSecret");

        var vnpay = new VnPayLibrary();
        foreach (var kv in queryParams)
        {
            if (!string.IsNullOrEmpty(kv.Key) && kv.Key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(kv.Key, kv.Value);
            }
        }

        var vnpTxnRef = vnpay.GetResponseData("vnp_TxnRef");
        var vnpTransactionNo = vnpay.GetResponseData("vnp_TransactionNo");
        var vnpResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var vnpTransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
        var vnpSecureHash = queryParams.TryGetValue("vnp_SecureHash", out var hash) ? hash : "";

        if (!vnpay.ValidateSignature(vnpSecureHash, vnpHashSecret))
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Invalid signature",
                ResponseCode = "97"
            };
        }

        if (!int.TryParse(vnpTxnRef, out var paymentId))
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Order not found",
                ResponseCode = "01"
            };
        }

        var paymentRepo = _uow.GetRepository<Payment>();
        var payment = await paymentRepo.FindAsync(
            p => p.Paymentid == paymentId,
            include: q => q.Include(p => p.Order)
                          .ThenInclude(o => o.OrderDetails)
                              .ThenInclude(od => od.Product)
        );

        if (payment == null)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Payment not found",
                ResponseCode = "01"
            };
        }

        var vnpAmount = ParseVnpAmount(vnpay.GetResponseData("vnp_Amount"));
        if (!MoneyEquals(payment.Amount ?? 0m, vnpAmount))
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "invalid amount",
                ResponseCode = "04"
            };
        }

        bool justConfirmedOrder = false;
        Order? confirmedOrder = null;

        if (payment.Status == PaymentStatus.SUCCESS)
        {
            var existingOrder = payment.Order;
            var existingBaseAmount = existingOrder != null ? GetBaseAmount(existingOrder) : 0m;
            var existingVatAmount = existingOrder != null ? GetVatAmount(existingOrder) : 0m;
            var existingPayableAmount = payment.Amount ?? 0m;

            return new PaymentResultDto
            {
                Success = true,
                PaymentId = payment.Paymentid,
                OrderId = payment.Orderid ?? 0,
                TransactionNo = vnpTransactionNo,
                Message = "Order already confirmed",
                Amount = existingPayableAmount,
                BaseAmount = existingBaseAmount,
                VatAmount = existingVatAmount,
                FinalPayableAmount = existingPayableAmount,
                RequireVatInvoice = existingOrder?.RequireVatInvoice ?? false,
                ResponseCode = "02"
            };
        }

        if (vnpResponseCode == "00" && vnpTransactionStatus == "00")
        {
            payment.Status = PaymentStatus.SUCCESS;
            payment.Transactionno = vnpTransactionNo;
        }
        else
        {
            payment.Status = PaymentStatus.FAILED;
        }

        paymentRepo.Update(payment);

        if (payment.Status == PaymentStatus.SUCCESS && payment.Order != null)
        {
            var orderRepo = _uow.GetRepository<Order>();
            var order = payment.Order;

            if (order.Status == OrderStatus.PENDING)
            {
                order.Status = OrderStatus.CONFIRMED;
                orderRepo.Update(order);
                justConfirmedOrder = true;
                confirmedOrder = order;
            }
        }

        await _uow.SaveAsync();

        if (justConfirmedOrder && confirmedOrder != null)
        {
            try
            {
                await _orderService.TryAllocateStockAfterPaymentAsync(confirmedOrder.Orderid);
            }
            catch
            {
            }

            try
            {
                await SendOrderPaymentSuccessEmailAsync(confirmedOrder);
            }
            catch
            {
            }
        }

        var baseAmount = payment.Order != null ? GetBaseAmount(payment.Order) : 0m;
        var vatAmount = payment.Order != null ? GetVatAmount(payment.Order) : 0m;
        var payableAmount = payment.Amount ?? 0m;

        return new PaymentResultDto
        {
            Success = payment.Status == PaymentStatus.SUCCESS,
            PaymentId = payment.Paymentid,
            OrderId = payment.Orderid ?? 0,
            TransactionNo = vnpTransactionNo,
            Message = payment.Status == PaymentStatus.SUCCESS ? "Confirm Success" : "Payment failed",
            Amount = payableAmount,
            BaseAmount = baseAmount,
            VatAmount = vatAmount,
            FinalPayableAmount = payableAmount,
            RequireVatInvoice = payment.Order?.RequireVatInvoice ?? false,
            ResponseCode = payment.Status == PaymentStatus.SUCCESS ? "00" : vnpResponseCode
        };
    }

    public async Task<PaymentResultDto> ProcessReturnUrlAsync(Dictionary<string, string> queryParams)
    {
        var vnpHashSecret = _configuration["VnPay:HashSecret"] ?? throw new Exception("Missing config: VnPay:HashSecret");

        var vnpay = new VnPayLibrary();
        foreach (var kv in queryParams)
        {
            if (!string.IsNullOrEmpty(kv.Key) && kv.Key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(kv.Key, kv.Value);
            }
        }

        var vnpTxnRef = vnpay.GetResponseData("vnp_TxnRef");
        var vnpTransactionNo = vnpay.GetResponseData("vnp_TransactionNo");
        var vnpResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var vnpTransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
        var vnpSecureHash = queryParams.TryGetValue("vnp_SecureHash", out var hash) ? hash : "";
        var vnpAmount = ParseVnpAmount(vnpay.GetResponseData("vnp_Amount"));
        var bankCode = vnpay.GetResponseData("vnp_BankCode");

        if (!vnpay.ValidateSignature(vnpSecureHash, vnpHashSecret))
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Có lỗi xảy ra trong quá trình xử lý",
                ResponseCode = "97"
            };
        }

        if (!int.TryParse(vnpTxnRef, out var paymentId))
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Không tìm thấy giao dịch",
                ResponseCode = "01"
            };
        }

        var paymentRepo = _uow.GetRepository<Payment>();
        var payment = await paymentRepo.FindAsync(
            p => p.Paymentid == paymentId,
            include: q => q.Include(p => p.Order)
                          .ThenInclude(o => o.OrderDetails)
                              .ThenInclude(od => od.Product)
        );

        if (payment == null)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Không tìm thấy giao dịch",
                ResponseCode = "01"
            };
        }

        if (!MoneyEquals(payment.Amount ?? 0m, vnpAmount))
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "invalid amount",
                ResponseCode = "04"
            };
        }

        var success = vnpResponseCode == "00" && vnpTransactionStatus == "00";

        if (payment.Status != PaymentStatus.SUCCESS && success)
        {
            payment.Status = PaymentStatus.SUCCESS;
            payment.Transactionno = vnpTransactionNo;
            paymentRepo.Update(payment);

            bool justConfirmedOrder = false;
            Order? confirmedOrder = null;

            if (payment.Order != null)
            {
                var orderRepo = _uow.GetRepository<Order>();
                var order = payment.Order;
                if (order.Status == OrderStatus.PENDING)
                {
                    order.Status = OrderStatus.CONFIRMED;
                    orderRepo.Update(order);
                    justConfirmedOrder = true;
                    confirmedOrder = order;
                }
            }

            await _uow.SaveAsync();

            if (justConfirmedOrder && confirmedOrder != null)
            {
                try
                {
                    await _orderService.TryAllocateStockAfterPaymentAsync(confirmedOrder.Orderid);
                }
                catch
                {
                }

                try
                {
                    await SendOrderPaymentSuccessEmailAsync(confirmedOrder);
                }
                catch
                {
                }
            }
        }
        else if (!success && payment.Status != PaymentStatus.FAILED)
        {
            payment.Status = PaymentStatus.FAILED;
            paymentRepo.Update(payment);
            await _uow.SaveAsync();
        }

        var message = success
            ? "Giao dịch được thực hiện thành công. Cảm ơn quý khách đã sử dụng dịch vụ"
            : $"Có lỗi xảy ra trong quá trình xử lý. Mã lỗi: {vnpResponseCode}";

        var baseAmount = payment.Order != null ? GetBaseAmount(payment.Order) : 0m;
        var vatAmount = payment.Order != null ? GetVatAmount(payment.Order) : 0m;
        var payableAmount = payment.Amount ?? 0m;

        return new PaymentResultDto
        {
            Success = success,
            PaymentId = paymentId,
            OrderId = payment.Orderid ?? 0,
            TransactionNo = vnpTransactionNo,
            Message = message,
            Amount = payableAmount,
            BaseAmount = baseAmount,
            VatAmount = vatAmount,
            FinalPayableAmount = payableAmount,
            RequireVatInvoice = payment.Order?.RequireVatInvoice ?? false,
            BankCode = bankCode,
            ResponseCode = vnpResponseCode
        };
    }

    public async Task<IEnumerable<PaymentHistoryDto>> GetPaymentsByOrderIdAsync(int orderId)
    {
        var paymentRepo = _uow.GetRepository<Payment>();
        var payments = await paymentRepo.GetAllAsync(
            p => p.Orderid == orderId,
            include: q => q.Include(p => p.Order)
        );

        return payments.Select(p => new PaymentHistoryDto
        {
            PaymentId = p.Paymentid,
            OrderId = p.Orderid ?? 0,
            WalletId = p.Walletid,
            Amount = p.Amount ?? 0m,
            BaseAmount = p.Order?.Totalprice ?? 0m,
            VatAmount = p.Order?.RequireVatInvoice == true ? p.Order.VatAmount : 0m,
            FinalPayableAmount = p.Amount ?? 0m,
            RequireVatInvoice = p.Order?.RequireVatInvoice ?? false,
            Status = p.Status ?? PaymentStatus.PENDING,
            Type = p.Type,
            PaymentMethod = p.Paymentmethod,
            IsPayOnline = p.Ispayonline ?? false,
            TransactionNo = p.Transactionno,
            CreatedDate = p.CreatedDate
        });
    }

    public async Task<IEnumerable<PaymentHistoryDto>> GetPaymentsByAccountIdAsync(int accountId)
    {
        var paymentRepo = _uow.GetRepository<Payment>();
        var payments = await paymentRepo.GetAllAsync(
            null,
            include: q => q.Include(p => p.Order)
        );

        var userPayments = payments.Where(p => p.Order?.Accountid == accountId);

        return userPayments.Select(p => new PaymentHistoryDto
        {
            PaymentId = p.Paymentid,
            OrderId = p.Orderid ?? 0,
            WalletId = p.Walletid,
            Amount = p.Amount ?? 0m,
            BaseAmount = p.Order?.Totalprice ?? 0m,
            VatAmount = p.Order?.RequireVatInvoice == true ? p.Order.VatAmount : 0m,
            FinalPayableAmount = p.Amount ?? 0m,
            RequireVatInvoice = p.Order?.RequireVatInvoice ?? false,
            Status = p.Status ?? PaymentStatus.PENDING,
            Type = p.Type,
            PaymentMethod = p.Paymentmethod,
            IsPayOnline = p.Ispayonline ?? false,
            TransactionNo = p.Transactionno,
            CreatedDate = p.CreatedDate
        });
    }

    private static string FormatVnd(decimal amount)
    {
        return string.Format("{0:N0} VNĐ", amount);
    }

    private async Task SendOrderPaymentSuccessEmailAsync(Order order)
    {
        if (string.IsNullOrWhiteSpace(order.Customeremail))
            throw new Exception($"Order #{order.Orderid} không có Customeremail.");

        var customerName = string.IsNullOrWhiteSpace(order.Customername) ? "quý khách" : order.Customername;

        var orderBaseUrl = _configuration["AppUrls:OrderDetail"];
        var orderLink = string.IsNullOrWhiteSpace(orderBaseUrl)
            ? $"http://14.225.207.221/account/orders/{order.Orderid}"
            : $"{orderBaseUrl.TrimEnd('/')}/{order.Orderid}";

        var payableAmount = GetFinalPayableAmount(order);
        var baseAmount = GetBaseAmount(order);
        var vatAmount = GetVatAmount(order);
        var orderItemsHtml = BuildOrderItemsEmailHtml(order);

        var normalHtmlBody = _templateRenderer.RenderOrderPaymentSuccess(
            customerName,
            order.Orderid,
            FormatVnd(payableAmount),
            FormatVnd(baseAmount),
            FormatVnd(vatAmount),
            orderLink,
            orderItemsHtml
        );

        var normalPdfBytes = await _invoiceService.GenerateNormalInvoicePdfAsync(order.Orderid, order.Accountid);
        var normalFileName = await _invoiceService.GetNormalInvoiceFileNameAsync(order.Orderid, order.Accountid);

        var normalAttachments = new List<EmailAttachmentDto>
        {
            new EmailAttachmentDto
            {
                FileName = normalFileName,
                ContentBytes = normalPdfBytes,
                ContentType = "application/pdf"
            }
        };

        await _emailSender.SendAsync(
            order.Customeremail,
            $"TetGift - Thanh toán đơn hàng #{order.Orderid} thành công",
            normalHtmlBody,
            normalAttachments
        );

        if (order.RequireVatInvoice && !string.IsNullOrWhiteSpace(order.VatInvoiceEmail))
        {
            var vatHtmlBody = _templateRenderer.RenderOrderPaymentSuccess(
                customerName,
                order.Orderid,
                FormatVnd(payableAmount),
                FormatVnd(baseAmount),
                FormatVnd(vatAmount),
                orderLink,
                orderItemsHtml
            );

            var vatPdfBytes = await _invoiceService.GenerateVatInvoicePdfAsync(order.Orderid, order.Accountid);
            var vatFileName = await _invoiceService.GetVatInvoiceFileNameAsync(order.Orderid, order.Accountid);

            var vatAttachments = new List<EmailAttachmentDto>
            {
                new EmailAttachmentDto
                {
                    FileName = vatFileName,
                    ContentBytes = vatPdfBytes,
                    ContentType = "application/pdf"
                }
            };

            await _emailSender.SendAsync(
                order.VatInvoiceEmail,
                $"TetGift - Hóa đơn VAT đơn hàng #{order.Orderid}",
                vatHtmlBody,
                vatAttachments
            );
        }
    }

    private static string BuildOrderItemsEmailHtml(Order order)
    {
        if (order.OrderDetails == null || !order.OrderDetails.Any())
        {
            return @"<p style='margin:0; color:#777777; font-size:14px; line-height:1.6;'>Không có thông tin sản phẩm.</p>";
        }

        var rows = order.OrderDetails
            .Where(x => x.Product != null)
            .Select(detail =>
            {
                var productName = detail.Product?.Productname ?? "Sản phẩm";
                var quantity = detail.Quantity ?? 0;
                var amount = detail.Amount ?? 0;
                var imageUrl = detail.Product?.ImageUrl ?? "";

                var imageBlock = string.IsNullOrWhiteSpace(imageUrl)
                    ? ""
                    : $@"<div style='width:72px; min-width:72px; height:72px; border-radius:10px; overflow:hidden; border:1px solid #eee; background:#fafafa;'>
                            <img src='{System.Net.WebUtility.HtmlEncode(imageUrl)}' style='width:100%; height:100%; object-fit:cover; display:block;' />
                        </div>";

                return $@"
                <div style='display:flex; gap:14px; padding:14px 0; border-bottom:1px solid #F1D9D9; align-items:flex-start;'>
                    {imageBlock}
                    <div style='flex:1;'>
                        <div style='font-size:15px; font-weight:700; color:#690000; margin-bottom:6px;'>
                            {System.Net.WebUtility.HtmlEncode(productName)}
                        </div>
                        <div style='font-size:14px; color:#666666; line-height:1.7;'>
                            Số lượng: {quantity}<br/>
                            Thành tiền: {amount:N0} VNĐ
                        </div>
                    </div>
                </div>";
            });

        return string.Join("", rows);
    }

    private static decimal GetBaseAmount(Order order)
    {
        return order.Totalprice ?? 0m;
    }

    private static decimal GetVatAmount(Order order)
    {
        return order.RequireVatInvoice ? order.VatAmount : 0m;
    }

    private static decimal GetFinalPayableAmount(Order order)
    {
        return GetBaseAmount(order) + GetVatAmount(order);
    }

    private static decimal ParseVnpAmount(string? rawAmount)
    {
        if (string.IsNullOrWhiteSpace(rawAmount))
            return 0m;

        if (!decimal.TryParse(rawAmount, out var parsed))
            return 0m;

        return parsed / 100m;
    }

    private static bool MoneyEquals(decimal a, decimal b)
    {
        return Math.Round(a, 2, MidpointRounding.AwayFromZero) ==
               Math.Round(b, 2, MidpointRounding.AwayFromZero);
    }
}