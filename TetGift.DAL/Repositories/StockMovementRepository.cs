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
    public class StockMovementRepository : IStockMovementRepository
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<StockMovement> _repository;

        public StockMovementRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _repository = _unitOfWork.GetRepository<StockMovement>();
        }

        public async Task<IEnumerable<StockMovement>> GetAllMovementsAsync()
        {
            return await _repository.GetAllAsync(
                include: query => query
                    .Include(sm => sm.Stock)
                        .ThenInclude(s => s.Product)
                    .Include(sm => sm.Order)
            );
        }

        public async Task<StockMovement?> GetMovementByIdAsync(int movementId)
        {
            return await _repository.FindAsync(
                predicate: sm => sm.Stockmovementid == movementId,
                include: query => query
                    .Include(sm => sm.Stock)
                    .Include(sm => sm.Order)
            );
        }

        public async Task<IEnumerable<StockMovement>> GetMovementsByStockIdAsync(int stockId)
        {
            return await _repository.GetAllAsync(
                predicate: sm => sm.Stockid == stockId,
                include: query => query.Include(sm => sm.Order)
            );
        }

        public async Task<IEnumerable<StockMovement>> GetMovementsByOrderIdAsync(int orderId)
        {
            return await _repository.GetAllAsync(
                predicate: sm => sm.Orderid == orderId,
                include: query => query
                    .Include(sm => sm.Stock)
                        .ThenInclude(s => s.Product)
            );
        }

        public async Task<IEnumerable<StockMovement>> GetMovementsByDateRangeAsync(DateTime from, DateTime to)
        {
            return await _repository.GetAllAsync(
                predicate: sm => sm.Movementdate >= from && sm.Movementdate <= to,
                include: query => query
                    .Include(sm => sm.Stock)
                        .ThenInclude(s => s.Product)
                    .Include(sm => sm.Order)
            );
        }

        public async Task<IEnumerable<StockMovement>> GetMovementsByProductIdAsync(int productId)
        {
            return await _repository.GetAllAsync(
                predicate: sm => sm.Stock != null && sm.Stock.Productid == productId,
                include: query => query
                    .Include(sm => sm.Stock)
                        .ThenInclude(s => s.Product)
            );
        }

        public async Task UpdateMovementAsync(StockMovement movement)
        {
            _repository.Update(movement);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteMovementAsync(int movementId)
        {
            var movement = await _repository.GetByIdAsync(movementId);
            if (movement == null)
                throw new Exception("Stock movement không tồn tại.");

            _repository.Delete(movement);
            await _unitOfWork.SaveAsync();
        }
    }
}
