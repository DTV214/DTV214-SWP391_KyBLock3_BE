using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IDashboardRankingService
    {
        Task<CategoryRankingResponseDto> GetCategoryPerformanceAsync(DashboardRankingRequest request);
        Task<CategoryProductRankingResponseDto> GetCategoryProductsPerformanceAsync(int categoryId, DashboardRankingRequest request);
    }
}
