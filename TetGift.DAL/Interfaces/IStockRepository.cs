using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.DAL.Entities;

namespace TetGift.DAL.Interfaces
{
    public interface IStockRepository
    {
        // READ methods
        Task<IEnumerable<Stock>> GetAllActiveStocksAsync();
        Task<Stock?> GetStockByIdAsync(int stockId);
        Task<IEnumerable<Stock>> GetStocksByProductIdAsync(int productId);
        Task<IEnumerable<Stock>> GetStocksByProductIdOrderByFIFOAsync(int productId); // Lấy theo FIFO
        Task<IEnumerable<Stock>> GetExpiredStocksAsync();
        Task<IEnumerable<Stock>> GetStocksExpiringSoonAsync(int daysThreshold);
        Task<IEnumerable<Stock>> GetLowStockProductsAsync(int threshold);
        Task<Stock?> GetStockWithMovementsAsync(int stockId);
        Task UpdateStockAsync(Stock stock);
        Task DeleteStockAsync(int stockId);
    }
}
