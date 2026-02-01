using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.DAL.Entities;

namespace TetGift.DAL.Interfaces
{
    public interface IStockMovementRepository
    {
        // READ methods
        Task<IEnumerable<StockMovement>> GetAllMovementsAsync();
        Task<StockMovement?> GetMovementByIdAsync(int movementId);
        Task<IEnumerable<StockMovement>> GetMovementsByStockIdAsync(int stockId);
        Task<IEnumerable<StockMovement>> GetMovementsByOrderIdAsync(int orderId);
        Task<IEnumerable<StockMovement>> GetMovementsByDateRangeAsync(DateTime from, DateTime to);
        Task<IEnumerable<StockMovement>> GetMovementsByProductIdAsync(int productId);
        Task UpdateMovementAsync(StockMovement movement);
        Task DeleteMovementAsync(int movementId);
    }
}
