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

    // Thống kê đơn hàng cho admin (Customer Total Orders & Success Rate)
    Task<List<CustomerOrderStatisticsDto>> GetCustomerOrderStatisticsAsync(TimeRangeRequest? request = null);

    Task<DashboardHighlightsDto> GetDashboardInsightsAsync(DateTime? startDate = null, DateTime? endDate = null);
}
