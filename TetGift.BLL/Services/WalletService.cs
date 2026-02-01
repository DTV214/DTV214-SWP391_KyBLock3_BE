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

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _uow;
    private readonly IPaymentService _paymentService;
    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration;

    public WalletService(IUnitOfWork uow, IPaymentService paymentService, IOrderService orderService, IConfiguration configuration)
    {
        _uow = uow;
        _paymentService = paymentService;
        _orderService = orderService;
        _configuration = configuration;
    }

    public async Task<WalletResponseDto> GetWalletByAccountIdAsync(int accountId)
    {
        var walletRepo = _uow.GetRepository<Wallet>();
        var wallet = await walletRepo.FindAsync(w => w.Accountid == accountId, include: null);

        if (wallet == null)
        {
            // Tự động tạo ví nếu chưa có (lazy creation)
            return await CreateWalletForAccountAsync(accountId);
        }

        return new WalletResponseDto
        {
            WalletId = wallet.Walletid,
            AccountId = wallet.Accountid,
            Balance = wallet.Balance,
            Status = wallet.Status ?? WalletStatus.ACTIVE,
            CreatedAt = wallet.Createdat,
            UpdatedAt = wallet.Updatedat
        };
    }

    public async Task<WalletTransactionHistoryDto> GetWalletTransactionsAsync(int accountId, int? page = 1, int? limit = 20)
    {
        var walletRepo = _uow.GetRepository<Wallet>();
        var wallet = await walletRepo.FindAsync(w => w.Accountid == accountId, include: null);

        if (wallet == null)
        {
            return new WalletTransactionHistoryDto
            {
                Transactions = new List<WalletTransactionDto>(),
                TotalCount = 0,
                Page = page ?? 1,
                Limit = limit ?? 20
            };
        }

        var transactionRepo = _uow.GetRepository<WalletTransaction>();
        var pageNumber = page ?? 1;
        var pageSize = limit ?? 20;
        var pageIndex = pageNumber - 1;

        // Lấy tất cả transactions và paginate manually
        var allTransactions = await transactionRepo.GetAllAsync(
            t => t.Walletid == wallet.Walletid
        );

        var totalCount = allTransactions.Count();
        var transactions = allTransactions
            .OrderByDescending(t => t.Createdat)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        return new WalletTransactionHistoryDto
        {
            Transactions = transactions.Select(t => new WalletTransactionDto
            {
                TransactionId = t.Transactionid,
                WalletId = t.Walletid,
                OrderId = t.Orderid,
                TransactionType = t.Transactiontype ?? "",
                Amount = t.Amount,
                BalanceBefore = t.Balancebefore,
                BalanceAfter = t.Balanceafter,
                Status = t.Status ?? WalletTransactionStatus.SUCCESS,
                Description = t.Description,
                CreatedAt = t.Createdat
            }).ToList(),
            TotalCount = totalCount,
            Page = pageNumber,
            Limit = pageSize
        };
    }

    public async Task<WalletResponseDto> CreateWalletForAccountAsync(int accountId)
    {
        var walletRepo = _uow.GetRepository<Wallet>();
        
        // Kiểm tra đã có ví chưa
        var existingWallet = await walletRepo.FindAsync(w => w.Accountid == accountId, include: null);
        if (existingWallet != null)
        {
            return new WalletResponseDto
            {
                WalletId = existingWallet.Walletid,
                AccountId = existingWallet.Accountid,
                Balance = existingWallet.Balance,
                Status = existingWallet.Status ?? WalletStatus.ACTIVE,
                CreatedAt = existingWallet.Createdat,
                UpdatedAt = existingWallet.Updatedat
            };
        }

        // Tạo ví mới
        var wallet = new Wallet
        {
            Accountid = accountId,
            Balance = 0,
            Status = WalletStatus.ACTIVE,
            Createdat = DateTime.Now
        };

        await walletRepo.AddAsync(wallet);
        await _uow.SaveAsync();

        return new WalletResponseDto
        {
            WalletId = wallet.Walletid,
            AccountId = wallet.Accountid,
            Balance = wallet.Balance,
            Status = wallet.Status,
            CreatedAt = wallet.Createdat,
            UpdatedAt = wallet.Updatedat
        };
    }

    public async Task<DepositResponseDto> DepositToWalletAsync(int accountId, decimal amount, string? clientIp = null)
    {
        // Validation
        if (amount < 10000)
            throw new Exception("Số tiền nạp tối thiểu là 10,000 VNĐ.");
        if (amount > 50000000)
            throw new Exception("Số tiền nạp tối đa là 50,000,000 VNĐ.");

        // Đảm bảo có ví
        var wallet = await GetWalletByAccountIdAsync(accountId);
        if (wallet.Status != WalletStatus.ACTIVE)
            throw new Exception("Ví của bạn đang bị khóa, không thể nạp tiền.");

        // Tạo Payment record với Type = WALLET_DEPOSIT
        var paymentRepo = _uow.GetRepository<Payment>();
        var payment = new Payment
        {
            Walletid = wallet.WalletId,
            Amount = amount,
            Status = PaymentStatus.PENDING,
            Type = "WALLET_DEPOSIT",
            Paymentmethod = "VNPAY",
            Ispayonline = true
        };
        await paymentRepo.AddAsync(payment);
        await _uow.SaveAsync();

        // Build VNPay URL
        var vnpUrl = _configuration["VnPay:Url"] ?? throw new Exception("Missing config: VnPay:Url");
        var vnpTmnCode = _configuration["VnPay:TmnCode"] ?? throw new Exception("Missing config: VnPay:TmnCode");
        var vnpHashSecret = _configuration["VnPay:HashSecret"] ?? throw new Exception("Missing config: VnPay:HashSecret");
        var vnpReturnUrl = _configuration["VnPay:DepositReturnUrl"] ?? _configuration["VnPay:ReturnUrl"] ?? throw new Exception("Missing config: VnPay:ReturnUrl");
        var vnpIpnUrl = _configuration["VnPay:DepositIpnUrl"] ?? _configuration["VnPay:IpnUrl"] ?? throw new Exception("Missing config: VnPay:IpnUrl");

        var vnpay = new VnPayLibrary();
        vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", vnpTmnCode);
        
        var vietnamTime = DateTime.UtcNow.AddHours(7);
        vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString());
        vnpay.AddRequestData("vnp_CreateDate", vietnamTime.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", clientIp ?? "127.0.0.1");
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", $"Nap tien vao vi - So tien: {amount:N0} VND");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", vnpReturnUrl);
        vnpay.AddRequestData("vnp_TxnRef", $"WALLET_{wallet.WalletId}_{payment.Paymentid}_{DateTime.Now:yyyyMMddHHmmss}");
        vnpay.AddRequestData("vnp_ExpireDate", vietnamTime.AddMinutes(60).ToString("yyyyMMddHHmmss"));

        var paymentUrl = vnpay.CreateRequestUrl(vnpUrl, vnpHashSecret);

        return new DepositResponseDto
        {
            PaymentId = payment.Paymentid,
            Amount = amount,
            PaymentUrl = paymentUrl,
            Status = "PENDING"
        };
    }

    public async Task<PaymentResultDto> ProcessDepositCallbackAsync(Dictionary<string, string> queryParams)
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
        var vnpSecureHash = queryParams.ContainsKey("vnp_SecureHash") ? queryParams["vnp_SecureHash"] : "";

        // Validate signature
        if (!vnpay.ValidateSignature(vnpSecureHash, vnpHashSecret))
        {
            return new PaymentResultDto
            {
                Success = false,
                PaymentId = 0,
                OrderId = 0,
                Message = "Invalid signature",
                Amount = 0,
                ResponseCode = "97"
            };
        }

        // Parse vnp_TxnRef để lấy PaymentId
        // Format: WALLET_{WalletId}_{PaymentId}_{Timestamp}
        if (string.IsNullOrEmpty(vnpTxnRef) || !vnpTxnRef.StartsWith("WALLET_"))
        {
            return new PaymentResultDto
            {
                Success = false,
                PaymentId = 0,
                OrderId = 0,
                Message = "Invalid transaction reference",
                Amount = 0,
                ResponseCode = "99"
            };
        }

        var parts = vnpTxnRef.Split('_');
        if (parts.Length < 3 || !int.TryParse(parts[2], out var paymentId))
        {
            return new PaymentResultDto
            {
                Success = false,
                PaymentId = 0,
                OrderId = 0,
                Message = "Invalid transaction reference format",
                Amount = 0,
                ResponseCode = "99"
            };
        }

        var paymentRepo = _uow.GetRepository<Payment>();
        var payment = await paymentRepo.FindAsync(
            p => p.Paymentid == paymentId && p.Type == "WALLET_DEPOSIT",
            include: q => q.Include(p => p.Wallet)
        );

        if (payment == null)
        {
            return new PaymentResultDto
            {
                Success = false,
                PaymentId = paymentId,
                OrderId = 0,
                Message = "Payment not found",
                Amount = 0,
                ResponseCode = "01"
            };
        }

        var vnpResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var vnpTransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
        var vnpAmount = long.Parse(vnpay.GetResponseData("vnp_Amount") ?? "0") / 100;
        var vnpTransactionNo = vnpay.GetResponseData("vnp_TransactionNo");

        var success = vnpResponseCode == "00" && vnpTransactionStatus == "00";

        // Kiểm tra số tiền
        if (vnpAmount != (long)payment.Amount)
        {
            return new PaymentResultDto
            {
                Success = false,
                PaymentId = paymentId,
                OrderId = 0,
                Message = "Invalid amount",
                Amount = payment.Amount ?? 0,
                ResponseCode = "04"
            };
        }

        // Cập nhật Payment status nếu chưa được cập nhật (idempotent)
        if (payment.Status != PaymentStatus.SUCCESS && success)
        {
            payment.Status = PaymentStatus.SUCCESS;
            payment.Transactionno = vnpTransactionNo;
            paymentRepo.Update(payment);

            // Cộng tiền vào ví
            if (payment.Wallet != null)
            {
                var walletRepo = _uow.GetRepository<Wallet>();
                var wallet = payment.Wallet;
                var balanceBefore = wallet.Balance;

                wallet.Balance += payment.Amount ?? 0;
                wallet.Updatedat = DateTime.Now;
                walletRepo.Update(wallet);

                // Tạo WalletTransaction để ghi log
                var transactionRepo = _uow.GetRepository<WalletTransaction>();
                var transaction = new WalletTransaction
                {
                    Walletid = wallet.Walletid,
                    Transactiontype = "DEPOSIT",
                    Amount = payment.Amount ?? 0,
                    Balancebefore = balanceBefore,
                    Balanceafter = wallet.Balance,
                    Status = WalletTransactionStatus.SUCCESS,
                    Description = $"Nạp tiền vào ví qua VNPay - Payment #{payment.Paymentid}",
                    Createdat = DateTime.Now
                };
                await transactionRepo.AddAsync(transaction);
            }

            await _uow.SaveAsync();
        }
        else if (!success && payment.Status != PaymentStatus.FAILED)
        {
            payment.Status = PaymentStatus.FAILED;
            paymentRepo.Update(payment);
            await _uow.SaveAsync();
        }

        return new PaymentResultDto
        {
            Success = success,
            PaymentId = paymentId,
            OrderId = 0,
            TransactionNo = vnpTransactionNo,
            Message = success ? "Nạp tiền thành công" : $"Nạp tiền thất bại. Mã lỗi: {vnpResponseCode}",
            Amount = payment.Amount ?? 0,
            ResponseCode = vnpResponseCode
        };
    }

    public async Task<PaymentResultDto> ProcessDepositReturnAsync(Dictionary<string, string> queryParams)
    {
        // Tương tự ProcessDepositCallbackAsync nhưng chỉ để hiển thị kết quả cho user
        return await ProcessDepositCallbackAsync(queryParams);
    }

    public async Task<WalletPaymentResponseDto> PayWithWalletAsync(int accountId, int orderId)
    {
        // 1. Validate Order
        var order = await _orderService.GetOrderByIdAsync(orderId, accountId);
        if (order.Status != OrderStatus.PENDING)
            throw new Exception("Chỉ có thể thanh toán cho đơn hàng đang chờ xác nhận.");

        // 2. Kiểm tra đã có payment thành công chưa
        var paymentRepo = _uow.GetRepository<Payment>();
        var existingPayments = await paymentRepo.GetAllAsync(
            p => p.Orderid == orderId && p.Status == PaymentStatus.SUCCESS
        );
        if (existingPayments.Any())
            throw new Exception("Đơn hàng này đã được thanh toán thành công.");

        // 3. Lấy ví và kiểm tra số dư
        var walletRepo = _uow.GetRepository<Wallet>();
        var wallet = await walletRepo.FindAsync(w => w.Accountid == accountId, include: null);
        
        if (wallet == null)
        {
            // Tự động tạo ví nếu chưa có
            await CreateWalletForAccountAsync(accountId);
            wallet = await walletRepo.FindAsync(w => w.Accountid == accountId, include: null);
            if (wallet == null)
                throw new Exception("Không thể tạo ví cho tài khoản này.");
        }

        if (wallet.Status != WalletStatus.ACTIVE)
            throw new Exception("Ví của bạn đang bị khóa, không thể thanh toán.");

        if (wallet.Balance < order.FinalPrice)
        {
            throw new Exception($"Số dư ví không đủ để thanh toán đơn hàng này. Số dư hiện tại: {wallet.Balance:N0} VNĐ, Cần thanh toán: {order.FinalPrice:N0} VNĐ, Thiếu: {order.FinalPrice - wallet.Balance:N0} VNĐ");
        }

        // 4. Trừ tiền từ ví và tạo Payment (trong transaction)
        var balanceBefore = wallet.Balance;
        wallet.Balance -= order.FinalPrice;
        wallet.Updatedat = DateTime.Now;
        walletRepo.Update(wallet);

        // Tạo Payment record
        var payment = new Payment
        {
            Orderid = orderId,
            Walletid = wallet.Walletid,
            Amount = order.FinalPrice,
            Status = PaymentStatus.SUCCESS,
            Type = "ORDER_PAYMENT",
            Paymentmethod = "WALLET",
            Ispayonline = false
        };
        await paymentRepo.AddAsync(payment);
        await _uow.SaveAsync();

        // Tạo WalletTransaction để ghi log
        var transactionRepo = _uow.GetRepository<WalletTransaction>();
        var transaction = new WalletTransaction
        {
            Walletid = wallet.Walletid,
            Orderid = orderId,
            Transactiontype = "PAYMENT",
            Amount = -order.FinalPrice, // Số âm để thể hiện trừ tiền
            Balancebefore = balanceBefore,
            Balanceafter = wallet.Balance,
            Status = "SUCCESS",
            Description = $"Thanh toán đơn hàng #{orderId}",
            Createdat = DateTime.Now
        };
        await transactionRepo.AddAsync(transaction);

        // 5. Cập nhật Order status
        var orderRepo = _uow.GetRepository<Order>();
        var orderEntity = await orderRepo.FindAsync(o => o.Orderid == orderId, include: null);
        if (orderEntity != null)
        {
            orderEntity.Status = OrderStatus.CONFIRMED;
            orderRepo.Update(orderEntity);
        }

        await _uow.SaveAsync();

        return new WalletPaymentResponseDto
        {
            PaymentId = payment.Paymentid,
            OrderId = orderId,
            Amount = order.FinalPrice,
            Status = "SUCCESS",
            Message = "Thanh toán bằng ví thành công"
        };
    }

    public async Task RefundToWalletAsync(int orderId)
    {
        var paymentRepo = _uow.GetRepository<Payment>();
        var orderRepo = _uow.GetRepository<Order>();
        
        // Tìm payment thành công (WALLET hoặc VNPAY)
        var payment = await paymentRepo.FindAsync(
            p => p.Orderid == orderId && 
                 (p.Paymentmethod == "WALLET" || p.Paymentmethod == "VNPAY") && 
                 p.Status == PaymentStatus.SUCCESS,
            include: q => q.Include(p => p.Wallet)
        );

        if (payment == null)
            return; // Chưa thanh toán hoặc thanh toán thất bại

        // Kiểm tra đã được hoàn lại chưa
        var transactionRepo = _uow.GetRepository<WalletTransaction>();
        var existingRefunds = await transactionRepo.GetAllAsync(
            t => t.Orderid == orderId && t.Transactiontype == "REFUND"
        );
        if (existingRefunds.Any())
            return; // Đã được hoàn lại rồi

        Wallet? wallet = null;

        // Nếu thanh toán bằng WALLET, dùng wallet từ payment
        if (payment.Paymentmethod == "WALLET" && payment.Wallet != null)
        {
            wallet = payment.Wallet;
        }
        // Nếu thanh toán bằng VNPAY, lấy wallet từ account của order
        else if (payment.Paymentmethod == "VNPAY")
        {
            var order = await orderRepo.FindAsync(
                o => o.Orderid == orderId,
                include: q => q.Include(o => o.Account).ThenInclude(a => a.Wallet)
            );

            if (order?.Account?.Wallet == null)
                return; // Không tìm thấy ví của customer

            wallet = order.Account.Wallet;
        }

        if (wallet == null)
            return; // Không tìm thấy ví

        var balanceBefore = wallet.Balance;
        var refundAmount = payment.Amount ?? 0;

        // Cộng tiền vào ví
        wallet.Balance += refundAmount;
        wallet.Updatedat = DateTime.Now;
        var walletRepo = _uow.GetRepository<Wallet>();
        walletRepo.Update(wallet);

        // Tạo WalletTransaction để ghi log
        var transaction = new WalletTransaction
        {
            Walletid = wallet.Walletid,
            Orderid = orderId,
            Transactiontype = "REFUND",
            Amount = refundAmount, // Số dương để thể hiện cộng tiền
            Balancebefore = balanceBefore,
            Balanceafter = wallet.Balance,
            Status = WalletTransactionStatus.SUCCESS,
            Description = $"Hoàn tiền do hủy đơn hàng #{orderId} (Thanh toán bằng {payment.Paymentmethod ?? "UNKNOWN"})",
            Createdat = DateTime.Now
        };
        await transactionRepo.AddAsync(transaction);

        // Cập nhật Payment status
        payment.Status = PaymentStatus.REFUNDED;
        paymentRepo.Update(payment);

        await _uow.SaveAsync();
    }
}
