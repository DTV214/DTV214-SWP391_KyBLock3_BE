using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.DAL.Repositories
{
    public class StockRepository : IStockRepository
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Stock> _repository;

        public StockRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _repository = _unitOfWork.GetRepository<Stock>();
        }

        public async Task<IEnumerable<Stock>> GetAllActiveStocksAsync()
        {
            return await _repository.GetAllAsync(
                predicate: s => s.Status != "Expired"
            );
        }

        public async Task<Stock?> GetStockByIdAsync(int stockId)
        {
            return await _repository.GetByIdAsync(stockId);
        }

        public async Task<IEnumerable<Stock>> GetStocksByProductIdAsync(int productId)
        {
            return await _repository.GetAllAsync(
                predicate: s => s.Productid == productId
            );
        }

        public async Task<IEnumerable<Stock>> GetStocksByProductIdOrderByFIFOAsync(int productId)
        {
            var stocks = await _repository.GetAllAsync(
                predicate: s => s.Productid == productId && s.Status == "Available" && s.Stockquantity > 0
            );

            return stocks.OrderBy(s => s.Productiondate).ThenBy(s => s.Stockid);
        }

        public async Task<IEnumerable<Stock>> GetExpiredStocksAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            return await _repository.GetAllAsync(
                predicate: s => s.Expirydate != null && s.Expirydate < today
            );
        }

        public async Task<IEnumerable<Stock>> GetStocksExpiringSoonAsync(int daysThreshold)
        {
            var thresholdDate = DateOnly.FromDateTime(DateTime.Now.AddDays(daysThreshold));
            return await _repository.GetAllAsync(
                predicate: s => s.Expirydate != null && s.Expirydate <= thresholdDate && s.Status == "Available",
                include: query => query.Include(s => s.Product)
            );
        }

        public async Task<IEnumerable<Stock>> GetLowStockProductsAsync(int threshold)
        {
            return await _repository.GetAllAsync(
                predicate: s => s.Stockquantity <= threshold && s.Status == "Available",
                include: query => query.Include(s => s.Product)
            );
        }

        public async Task<Stock?> GetStockWithMovementsAsync(int stockId)
        {
            return await _repository.FindAsync(
                predicate: s => s.Stockid == stockId,
                include: query => query
                    .Include(s => s.StockMovements)
                    .Include(s => s.Product)
            );
        }

        public async Task UpdateStockAsync(Stock stock)
        {
            stock.Lastupdated = DateTime.Now;
            _repository.Update(stock);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteStockAsync(int stockId)
        {
            var stock = await _repository.GetByIdAsync(stockId);
            if (stock == null)
                throw new Exception("Stock không tồn tại.");

            _repository.Delete(stock);
            await _unitOfWork.SaveAsync();
        }
    }
}
