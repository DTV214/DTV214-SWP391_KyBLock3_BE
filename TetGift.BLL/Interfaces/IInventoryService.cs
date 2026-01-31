using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.BLL.Dtos;
using TetGift.BLL.Dtos.TetGift.BLL.Dtos;

namespace TetGift.BLL.Interfaces
{
    public interface IInventoryService
    {
        // Báo cáo tồn kho thấp 
        Task<IEnumerable<LowStockReportDto>> GetLowStockReportAsync(int threshold);

        // --- THÊM CRUD MỚI ---
        Task<IEnumerable<StockDto>> GetAllStocksAsync();
        Task<StockDto> GetStockByIdAsync(int id);
        Task<StockDto> CreateStockAsync(CreateStockRequest req);
        Task UpdateStockAsync(int id, UpdateStockRequest req);
        Task DeleteStockAsync(int id);

        Task<IEnumerable<StockDto>> GetStocksByProductIdAsync(int productId);
    }
}
