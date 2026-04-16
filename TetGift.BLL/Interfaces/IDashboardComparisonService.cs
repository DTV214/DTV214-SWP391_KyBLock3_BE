using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IDashboardComparisonService
    {
        Task<MonthlyComparisonChartDto> GetMonthlyOrderRevenueComparisonAsync(MonthComparisonRequest request);
        Task<MonthlyComparisonChartDto> GetMonthlyActualRevenueComparisonAsync(MonthComparisonRequest request);

        Task<YearComparisonChartDto> GetYearlyOrderRevenueComparisonAsync(YearComparisonRequest request);
        Task<YearComparisonChartDto> GetYearlyActualRevenueComparisonAsync(YearComparisonRequest request);
    }
}
