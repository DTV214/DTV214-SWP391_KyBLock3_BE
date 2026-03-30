using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Common.Constraint;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BackgroundJobs
{
    /// <summary>
    /// Background service tự động quét và khóa (chuyển sang EXPIRED) 
    /// các lô hàng đã vượt quá hạn sử dụng (ExpiryDate).
    /// Chu kỳ quét: 1 giờ / lần.
    /// </summary>
    public class ExpiredStockCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpiredStockCleanupService> _logger;

        public ExpiredStockCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<ExpiredStockCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Vòng lặp chạy ngầm liên tục khi ứng dụng còn sống
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredStocksAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ExpiredStockCleanup] Lỗi nghiêm trọng khi quét lô hàng hết hạn.");
                }

                // Chạy mỗi 1 giờ. 
                // Có thể đổi thành TimeSpan.FromDays(1) nếu chỉ muốn quét 1 lần/ngày vào ban đêm.
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CleanupExpiredStocksAsync(CancellationToken stoppingToken)
        {
            // Tạo Scope để lấy IUnitOfWork (bắt buộc đối với Background Service)
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stockRepo = uow.GetRepository<Stock>();
            var movementRepo = uow.GetRepository<StockMovement>();

            // Lấy ngày hiện tại (Ép kiểu về DateOnly để so sánh với ExpiryDate)
            var today = DateOnly.FromDateTime(DateTime.Now);

            // Tìm các lô hàng: ĐANG BÁN (ACTIVE) + CÓ HẠN SỬ DỤNG + ĐÃ QUÁ HẠN (< today)
            var expiredStocks = await stockRepo.Entities
                .Where(s => s.Status == StockStatus.ACTIVE
                         && s.Expirydate != null
                         && s.Expirydate < today)
                .ToListAsync(stoppingToken);

            // Nếu không có lô nào hết hạn thì thoát, đợi chu kỳ sau
            if (expiredStocks.Count == 0) return;

            _logger.LogInformation(
                "[ExpiredStockCleanup] Tìm thấy {Count} lô hàng đã hết hạn. Bắt đầu quá trình khóa...",
                expiredStocks.Count);

            foreach (var stock in expiredStocks)
            {
                // 1. Cập nhật Status lô hàng
                stock.Status = StockStatus.EXPIRED;
                stock.Lastupdated = DateTime.Now;

                stockRepo.Update(stock);

                // 2. Ghi log tự động vào StockMovement
                // Giữ nguyên Quantity thực tế (hàng vẫn ở trong kho), chỉ ghi chú lý do khóa
                var movement = new StockMovement
                {
                    Stockid = stock.Stockid,
                    Quantity = stock.Stockquantity,
                    Movementdate = DateTime.Now,
                    Note = $"[AUTO_EXPIRED] Hệ thống tự động khóa lô hàng do đã quá hạn sử dụng (Ngày hết hạn: {stock.Expirydate})"
                };

                await movementRepo.AddAsync(movement);
            }

            // Lưu toàn bộ thay đổi (Update Stocks + Insert Movements) vào DB trong 1 lần transaction
            await uow.SaveAsync();

            _logger.LogInformation(
                "[ExpiredStockCleanup] Đã khóa thành công {Count} lô hàng.",
                expiredStocks.Count);
        }
    }
}