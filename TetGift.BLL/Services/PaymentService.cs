using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
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
    private readonly IWalletService _walletService;
    private readonly IConfiguration _configuration;

    public PaymentService(IUnitOfWork uow, IWalletService walletService, IConfiguration configuration)
    {
        _uow = uow;
        _walletService = walletService;
        _configuration = configuration;
    }

    public async Task<PaymentResponseDto> CreatePaymentAsync(int orderId, int accountId, string? clientIp = null, string? paymentMethod = null)
    {
        // 1. Validate Order (query trực tiếp để tránh circular dependency)
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
        
        // Calculate FinalPrice từ Order
        decimal finalPrice = order.Totalprice ?? 0;

        // 2. Kiểm tra đã có payment thành công chưa
        var paymentRepo = _uow.GetRepository<Payment>();
        var existingPayments = await paymentRepo.FindAsync(
            p => p.Orderid == orderId && p.Status == PaymentStatus.SUCCESS
        );
        if (existingPayments.Any())
            throw new Exception("Đơn hàng này đã được thanh toán thành công.");

        // 3. Kiểm tra payment method
        var method = (paymentMethod ?? "VNPAY").ToUpper();
        if (method == "WALLET")
        {
            // Thanh toán bằng ví - gọi WalletService
            var walletPayment = await _walletService.PayWithWalletAsync(accountId, orderId);
            return new PaymentResponseDto
            {
                PaymentId = walletPayment.PaymentId,
                OrderId = orderId,
                Amount = walletPayment.Amount,
                PaymentUrl = "", // Không cần URL vì đã thanh toán trực tiếp
                Status = walletPayment.Status
            };
        }

        // 4. Tạo Payment record cho VNPay
        var payment = new Payment
        {
            Orderid = orderId,
            Amount = finalPrice,
            Status = PaymentStatus.PENDING,
            Type = "ORDER_PAYMENT",
            Paymentmethod = "VNPAY",
            Ispayonline = true
        };
        await paymentRepo.AddAsync(payment);
        await _uow.SaveAsync();

        // 5. Build VNPay URL
        var vnpUrl = _configuration["VnPay:Url"] ?? throw new Exception("Missing config: VnPay:Url");
        var vnpTmnCode = _configuration["VnPay:TmnCode"] ?? throw new Exception("Missing config: VnPay:TmnCode");
        var vnpHashSecret = _configuration["VnPay:HashSecret"] ?? throw new Exception("Missing config: VnPay:HashSecret");
        var vnpReturnUrl = _configuration["VnPay:ReturnUrl"] ?? throw new Exception("Missing config: VnPay:ReturnUrl");

        var vnpay = new VnPayLibrary();
        vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", vnpTmnCode);
        // VNPay yêu cầu timezone GMT+7 (Vietnam time)
        // Convert UTC to GMT+7
        var vietnamTime = DateTime.UtcNow.AddHours(7);
        
        vnpay.AddRequestData("vnp_Amount", ((long)(finalPrice * 100)).ToString()); // Nhân 100 để khử phần thập phân
        vnpay.AddRequestData("vnp_CreateDate", vietnamTime.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", clientIp ?? "127.0.0.1");
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang #{orderId}");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", vnpReturnUrl);
        vnpay.AddRequestData("vnp_TxnRef", payment.Paymentid.ToString());
        vnpay.AddRequestData("vnp_ExpireDate", vietnamTime.AddMinutes(60).ToString("yyyyMMddHHmmss")); // 60 phút để đảm bảo đủ thời gian test

        var paymentUrl = vnpay.CreateRequestUrl(vnpUrl, vnpHashSecret);

        return new PaymentResponseDto
        {
            PaymentId = payment.Paymentid,
            OrderId = orderId,
            Amount = finalPrice,
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

        // Kiểm tra nếu là WALLET_DEPOSIT (vnpTxnRef có format: WALLET_{WalletId}_{PaymentId}_{Timestamp})
        if (!string.IsNullOrEmpty(vnpTxnRef) && vnpTxnRef.StartsWith("WALLET_"))
        {
            // Nạp tiền vào ví - gọi WalletService để xử lý
            return await _walletService.ProcessDepositCallbackAsync(queryParams);
        }

        // ORDER_PAYMENT - parse paymentId trực tiếp từ vnpTxnRef
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
            include: q => q.Include(p => p.Order).Include(p => p.Wallet)
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

        // Validate amount
        var vnpAmount = long.Parse(vnpay.GetResponseData("vnp_Amount") ?? "0") / 100; // Chia 100 để lấy số tiền thực
        if (payment.Amount != vnpAmount)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "invalid amount",
                ResponseCode = "04"
            };
        }

        // ORDER_PAYMENT - xử lý như cũ
        // Kiểm tra payment đã được xử lý chưa
        if (payment.Status == PaymentStatus.SUCCESS)
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
            payment.Status = PaymentStatus.SUCCESS;
            payment.Transactionno = vnpTransactionNo;
        }
        else
        {
            payment.Status = PaymentStatus.FAILED;
        }

        paymentRepo.Update(payment);

        // Cập nhật Order status nếu payment thành công
        if (payment.Status == PaymentStatus.SUCCESS && payment.Order != null)
        {
            var orderRepo = _uow.GetRepository<Order>();
            var order = payment.Order;
            if (order.Status == OrderStatus.PENDING)
            {
                order.Status = OrderStatus.CONFIRMED;
                orderRepo.Update(order);
            }
        }

        await _uow.SaveAsync();

        return new PaymentResultDto
        {
            Success = payment.Status == PaymentStatus.SUCCESS,
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

        // Kiểm tra nếu là WALLET_DEPOSIT (vnpTxnRef có format: WALLET_{WalletId}_{PaymentId}_{Timestamp})
        if (!string.IsNullOrEmpty(vnpTxnRef) && vnpTxnRef.StartsWith("WALLET_"))
        {
            // Nạp tiền vào ví - gọi WalletService để xử lý
            return await _walletService.ProcessDepositReturnAsync(queryParams);
        }

        // ORDER_PAYMENT - parse paymentId trực tiếp từ vnpTxnRef
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
            include: q => q.Include(p => p.Order).Include(p => p.Wallet)
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

        // Validate amount
        if (payment.Amount != vnpAmount)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "invalid amount",
                ResponseCode = "04"
            };
        }

        // ORDER_PAYMENT - xử lý như cũ
        var success = vnpResponseCode == "00" && vnpTransactionStatus == "00";
        
        // Cập nhật Payment status nếu chưa được cập nhật (idempotent)
        if (payment.Status != PaymentStatus.SUCCESS && success)
        {
            payment.Status = PaymentStatus.SUCCESS;
            payment.Transactionno = vnpTransactionNo;
            paymentRepo.Update(payment);

            // Cập nhật Order status nếu payment thành công
            if (payment.Order != null)
            {
                var orderRepo = _uow.GetRepository<Order>();
                var order = payment.Order;
                if (order.Status == OrderStatus.PENDING)
                {
                    order.Status = OrderStatus.CONFIRMED;
                    orderRepo.Update(order);
                }
            }

            await _uow.SaveAsync();
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

        return new PaymentResultDto
        {
            Success = success,
            PaymentId = paymentId,
            OrderId = payment.Orderid ?? 0,
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
            WalletId = p.Walletid,
            Amount = p.Amount ?? 0,
            Status = p.Status ?? PaymentStatus.PENDING,
            Type = p.Type,
            PaymentMethod = p.Paymentmethod,
            IsPayOnline = p.Ispayonline ?? false,
            TransactionNo = p.Transactionno,
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
            WalletId = p.Walletid,
            Amount = p.Amount ?? 0,
            Status = p.Status ?? PaymentStatus.PENDING,
            Type = p.Type,
            PaymentMethod = p.Paymentmethod,
            IsPayOnline = p.Ispayonline ?? false,
            TransactionNo = p.Transactionno,
            CreatedDate = null
        });
    }

    // ========== WALLET DEPOSIT METHODS ==========

    public async Task<DepositResponseDto> CreateWalletDepositPaymentAsync(int accountId, decimal amount, string? clientIp = null)
    {
        return await _walletService.DepositToWalletAsync(accountId, amount, clientIp);
    }

    public async Task<PaymentResultDto> ProcessWalletDepositIpnAsync(Dictionary<string, string> queryParams)
    {
        return await _walletService.ProcessDepositCallbackAsync(queryParams);
    }

    public async Task<PaymentResultDto> ProcessWalletDepositReturnAsync(Dictionary<string, string> queryParams)
    {
        return await _walletService.ProcessDepositReturnAsync(queryParams);
    }
}
