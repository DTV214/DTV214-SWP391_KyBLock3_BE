using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IDashboardService
{
    Task<RevenueChartDto> GetRevenueByTimeRangeAsync(TimeRangeRequest request);
    Task<PaymentChannelStatisticsDto> GetPaymentChannelStatisticsAsync(TimeRangeRequest? request = null);
    Task<AbandonedCartDto> GetAbandonedCartsAsync(int? days = null);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(TimeRangeRequest? request = null);
}
