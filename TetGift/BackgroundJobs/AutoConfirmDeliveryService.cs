using Microsoft.EntityFrameworkCore;
using TetGift.BLL.Common.Constraint;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BackgroundJobs
{
    /// <summary>
    /// Background service tự động xác nhận giao hàng (SHIPPED → DELIVERED)
    /// nếu customer không xác nhận sau N ngày (mặc định 3 ngày).
    /// Chạy mỗi 1 giờ.
    /// </summary>
    public class AutoConfirmDeliveryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _cfg;
        private readonly ILogger<AutoConfirmDeliveryService> _logger;

        public AutoConfirmDeliveryService(
            IServiceScopeFactory scopeFactory,
            IConfiguration cfg,
            ILogger<AutoConfirmDeliveryService> logger)
        {
            _scopeFactory = scopeFactory;
            _cfg = cfg;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await AutoConfirmShippedOrdersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AutoConfirmDelivery] Lỗi khi tự động xác nhận giao hàng.");
                }

                // Chạy mỗi 1 giờ
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task AutoConfirmShippedOrdersAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var orderRepo = uow.GetRepository<Order>();

            // Đọc số ngày từ config, mặc định 3 ngày
            var days = int.TryParse(_cfg["Order:AutoConfirmAfterDays"], out var d) ? d : 3;
            var cutoff = DateTime.Now.AddDays(-days);

            // Tìm order SHIPPED quá hạn
            var shippedOrders = await orderRepo.Entities
                .Where(o => o.Status == OrderStatus.SHIPPED
                         && o.Shippeddate != null
                         && o.Shippeddate < cutoff)
                .ToListAsync(stoppingToken);

            if (shippedOrders.Count == 0) return;

            _logger.LogInformation(
                "[AutoConfirmDelivery] Tìm thấy {Count} đơn hàng SHIPPED quá {Days} ngày, tự động chuyển DELIVERED.",
                shippedOrders.Count, days);

            foreach (var order in shippedOrders)
            {
                order.Status = OrderStatus.DELIVERED;
                order.Note = string.IsNullOrWhiteSpace(order.Note)
                    ? $"[AUTO_CONFIRMED] Tự động xác nhận giao hàng sau {days} ngày ({DateTime.Now:yyyy-MM-dd HH:mm:ss})"
                    : $"{order.Note}\n[AUTO_CONFIRMED] Tự động xác nhận giao hàng sau {days} ngày ({DateTime.Now:yyyy-MM-dd HH:mm:ss})";

                orderRepo.Update(order);
            }

            await uow.SaveAsync();

            _logger.LogInformation(
                "[AutoConfirmDelivery] Đã tự động xác nhận {Count} đơn hàng.",
                shippedOrders.Count);
        }
    }
}
