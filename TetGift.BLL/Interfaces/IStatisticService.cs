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
        Task<List<TrendingProductDto>> GetTrendingProductsAsync(string period = "week", int top = 5);

        // API MỚI: Chỉ lấy Tháng, tự động dò Năm
        Task<EventTrendResponseDto> GetEventMonthTrendAsync(int month);
    }
}
