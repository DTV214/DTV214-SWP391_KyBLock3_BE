using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IStatisticService
    {
        Task<ProductStatisticResponseDto> GetProductStatisticAsync(int productId);

        // API Mới: Lấy Top sản phẩm Trending (Mặc định lấy 7 ngày)
        Task<List<TrendingProductDto>> GetTrendingProductsAsync(string period = "week", int top = 5);
        // Nơi đây sau này bạn và team sẽ khai báo thêm các hàm:
        // Task<DashboardOverviewDto> GetDashboardOverviewAsync();
        // Task<CustomerStatisticDto> GetCustomerStatisticAsync();
    }
}
