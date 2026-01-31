using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using TetGift.BLL.Common.VnPay;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration;

    public PaymentService(IUnitOfWork uow, IOrderService orderService, IConfiguration configuration)
    {
        _uow = uow;
        _orderService = orderService;
        _configuration = configuration;
    }

    public async Task<PaymentResponseDto> CreatePaymentAsync(int orderId, int accountId, string? clientIp = null)
    {
        // 1. Validate Order (sử dụng IOrderService - kế thừa code cũ)
        var order = await _orderService.GetOrderByIdAsync(orderId, accountId);
        if (order.Status != "PENDING")
            throw new Exception("Chỉ có thể thanh toán cho đơn hàng đang chờ xác nhận.");

        // 2. Kiểm tra đã có payment thành công chưa
        var paymentRepo = _uow.GetRepository<Payment>();
        var existingPayments = await paymentRepo.FindAsync(
            p => p.Orderid == orderId && p.Status == "SUCCESS"
        );
        if (existingPayments.Any())
            throw new Exception("Đơn hàng này đã được thanh toán thành công.");

        // 3. Tạo Payment record
        var payment = new Payment
        {
            Orderid = orderId,
            Amount = order.FinalPrice,
            Status = "PENDING",
            Type = "VNPAY",
            Ispayonline = true
        };
        await paymentRepo.AddAsync(payment);
        await _uow.SaveAsync();

        // 4. Build VNPay URL
        var vnpUrl = _configuration["VnPay:Url"] ?? throw new Exception("Missing config: VnPay:Url");
        var vnpTmnCode = _configuration["VnPay:TmnCode"] ?? throw new Exception("Missing config: VnPay:TmnCode");
        var vnpHashSecret = _configuration["VnPay:HashSecret"] ?? throw new Exception("Missing config: VnPay:HashSecret");
        var vnpReturnUrl = _configuration["VnPay:ReturnUrl"] ?? throw new Exception("Missing config: VnPay:ReturnUrl");

        var vnpay = new VnPayLibrary();
        vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", vnpTmnCode);
        vnpay.AddRequestData("vnp_Amount", ((long)(order.FinalPrice * 100)).ToString()); // Nhân 100 để khử phần thập phân
        vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", clientIp ?? "127.0.0.1");
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang #{orderId}");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", vnpReturnUrl);
        vnpay.AddRequestData("vnp_TxnRef", payment.Paymentid.ToString());
        vnpay.AddRequestData("vnp_ExpireDate", DateTime.Now.AddMinutes(30).ToString("yyyyMMddHHmmss"));

        var paymentUrl = vnpay.CreateRequestUrl(vnpUrl, vnpHashSecret);

        return new PaymentResponseDto
        {
            PaymentId = payment.Paymentid,
            OrderId = orderId,
            Amount = order.FinalPrice,
            PaymentUrl = paymentUrl,
            Status = "PENDING"
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
        var vnpSecureHash = queryParams.ContainsKey("vnp_SecureHash") ? queryParams["vnp_SecureHash"] : "";

        // Validate signature
        if (!vnpay.ValidateSignature(vnpSecureHash, vnpHashSecret))
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Invalid signature",
                ResponseCode = "97"
            };
        }

        // Lấy Payment
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
        );

        if (payment == null)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Order not found",
                ResponseCode = "01"
            };
        }

        // Validate amount
        var vnpAmount = long.Parse(vnpay.GetResponseData("vnp_Amount")) / 100; // Chia 100 để lấy số tiền thực
        if (payment.Amount != vnpAmount)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "invalid amount",
                ResponseCode = "04"
            };
        }

        // Kiểm tra payment đã được xử lý chưa
        if (payment.Status == "SUCCESS")
        {
            return new PaymentResultDto
            {
                Success = true,
                PaymentId = payment.Paymentid,
                OrderId = payment.Orderid ?? 0,
                TransactionNo = vnpTransactionNo,
                Message = "Order already confirmed",
                ResponseCode = "02"
            };
        }

        // Cập nhật Payment status
        if (vnpResponseCode == "00" && vnpTransactionStatus == "00")
        {
            payment.Status = "SUCCESS";
            // Lưu transaction no vào một field nào đó (có thể cần thêm field vào Payment entity)
            // Tạm thời lưu vào Note hoặc tạo field mới
        }
        else
        {
            payment.Status = "FAILED";
        }

        paymentRepo.Update(payment);

        // Cập nhật Order status nếu payment thành công
        if (payment.Status == "SUCCESS" && payment.Order != null)
        {
            var orderRepo = _uow.GetRepository<Order>();
            var order = payment.Order;
            if (order.Status == "PENDING")
            {
                order.Status = "CONFIRMED";
                orderRepo.Update(order);
            }
        }

        await _uow.SaveAsync();

        return new PaymentResultDto
        {
            Success = payment.Status == "SUCCESS",
            PaymentId = payment.Paymentid,
            OrderId = payment.Orderid ?? 0,
            TransactionNo = vnpTransactionNo,
            Message = "Confirm Success",
            Amount = payment.Amount ?? 0,
            ResponseCode = "00"
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
        var vnpSecureHash = queryParams.ContainsKey("vnp_SecureHash") ? queryParams["vnp_SecureHash"] : "";
        var vnpAmount = long.Parse(vnpay.GetResponseData("vnp_Amount")) / 100;
        var bankCode = vnpay.GetResponseData("vnp_BankCode");

        // Validate signature
        if (!vnpay.ValidateSignature(vnpSecureHash, vnpHashSecret))
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "Có lỗi xảy ra trong quá trình xử lý",
                ResponseCode = "97"
            };
        }

        // Lấy Payment (chỉ để hiển thị, không cập nhật - IPN đã xử lý)
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
        var payment = await paymentRepo.GetByIdAsync(paymentId);

        var success = vnpResponseCode == "00" && vnpTransactionStatus == "00";
        var message = success
            ? "Giao dịch được thực hiện thành công. Cảm ơn quý khách đã sử dụng dịch vụ"
            : $"Có lỗi xảy ra trong quá trình xử lý. Mã lỗi: {vnpResponseCode}";

        return new PaymentResultDto
        {
            Success = success,
            PaymentId = paymentId,
            OrderId = payment?.Orderid ?? 0,
            TransactionNo = vnpTransactionNo,
            Message = message,
            Amount = vnpAmount,
            BankCode = bankCode,
            ResponseCode = vnpResponseCode
        };
    }

    public async Task<IEnumerable<PaymentHistoryDto>> GetPaymentsByOrderIdAsync(int orderId)
    {
        var paymentRepo = _uow.GetRepository<Payment>();
        var payments = await paymentRepo.FindAsync(p => p.Orderid == orderId);

        return payments.Select(p => new PaymentHistoryDto
        {
            PaymentId = p.Paymentid,
            OrderId = p.Orderid ?? 0,
            Amount = p.Amount ?? 0,
            Status = p.Status ?? "PENDING",
            Type = p.Type,
            IsPayOnline = p.Ispayonline ?? false,
            CreatedDate = null // Payment entity không có CreatedDate, có thể thêm sau
        });
    }

    public async Task<IEnumerable<PaymentHistoryDto>> GetPaymentsByAccountIdAsync(int accountId)
    {
        var paymentRepo = _uow.GetRepository<Payment>();
        var payments = await paymentRepo.GetAllAsync(
            null,
            include: q => q.Include(p => p.Order)
        );

        // Filter payments by accountId through Order
        var userPayments = payments.Where(p => p.Order?.Accountid == accountId);

        return userPayments.Select(p => new PaymentHistoryDto
        {
            PaymentId = p.Paymentid,
            OrderId = p.Orderid ?? 0,
            Amount = p.Amount ?? 0,
            Status = p.Status ?? "PENDING",
            Type = p.Type,
            IsPayOnline = p.Ispayonline ?? false,
            CreatedDate = null
        });
    }
}
