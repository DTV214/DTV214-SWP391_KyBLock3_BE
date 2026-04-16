using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces;

public interface IDashboardService
{
    Task<RevenueChartDto> GetRevenueByTimeRangeAsync(TimeRangeRequest request);
    Task<PaymentChannelStatisticsDto> GetPaymentChannelStatisticsAsync(TimeRangeRequest? request = null);
    Task<AbandonedCartDto> GetAbandonedCartsAsync(int? days = null);
    Task<AccountChartDto> GetAccountStatisticsAsync(TimeRangeRequest request);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(TimeRangeRequest? request = null);
    Task<RevenueChartDto> GetActualRevenueByTimeRangeAsync(TimeRangeRequest request);
}
