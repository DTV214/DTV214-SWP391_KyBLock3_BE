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

        // Nơi đây sau này bạn và team sẽ khai báo thêm các hàm:
        // Task<DashboardOverviewDto> GetDashboardOverviewAsync();
        // Task<CustomerStatisticDto> GetCustomerStatisticAsync();
    }
}
