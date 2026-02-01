using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TetGift.BLL.Common.Constraint;
using TetGift.BLL.Dtos;
using TetGift.BLL.Interfaces;
using TetGift.DAL.Entities;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        public InventoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // 1. READ ALL (Giữ nguyên)
        public async Task<IEnumerable<StockDto>> GetAllStocksAsync()
        {
            var stockRepo = _unitOfWork.GetRepository<Stock>();
            var stocks = await stockRepo.GetAllAsync(
                predicate: s => s.Status != StockStatus.DELETED,
                include: q => q.Include(s => s.Product)
            );
            return MapToDto(stocks);
        }

        // 2. READ BY ID (Giữ nguyên)
        public async Task<StockDto> GetStockByIdAsync(int id)
        {
            var stockRepo = _unitOfWork.GetRepository<Stock>();
            var stock = await stockRepo.FindAsync(s => s.Stockid == id, include: q => q.Include(s => s.Product));
            if (stock == null) throw new Exception("Lô hàng không tồn tại.");
            return MapToSingleDto(stock);
        }

        // --- 3. [NEW] READ BY PRODUCT ID ---
        public async Task<IEnumerable<StockDto>> GetStocksByProductIdAsync(int productId)
        {
            var stockRepo = _unitOfWork.GetRepository<Stock>();

            // Lấy tất cả lô hàng của ProductId này mà chưa bị xóa
            var stocks = await stockRepo.GetAllAsync(
                predicate: s => s.Productid == productId && s.Status != StockStatus.DELETED,
                include: q => q.Include(s => s.Product)
            );

            return MapToDto(stocks);
        }
        // -----------------------------------

        // 4. CREATE (Giữ nguyên)
        public async Task<StockDto> CreateStockAsync(CreateStockRequest req)
        {
            var productRepo = _unitOfWork.GetRepository<Product>();
            var product = await productRepo.GetByIdAsync(req.ProductId);
            if (product == null) throw new Exception("Sản phẩm không tồn tại.");

            var newStock = new Stock
            {
                Productid = req.ProductId,
                Stockquantity = req.Quantity,
                // Convert DateTime? -> DateOnly?
                Productiondate = req.ProductionDate.HasValue ? DateOnly.FromDateTime(req.ProductionDate.Value) : null,
                Expirydate = req.ExpiryDate.HasValue ? DateOnly.FromDateTime(req.ExpiryDate.Value) : null,
                Status = StockStatus.ACTIVE,
                Lastupdated = DateTime.Now
            };

            await _unitOfWork.GetRepository<Stock>().AddAsync(newStock);
            await _unitOfWork.SaveAsync();

            // Ghi Log Movement
            var movement = new StockMovement
            {
                Stockid = newStock.Stockid,
                Quantity = req.Quantity,
                Movementdate = DateTime.Now,
                Note = "Nhập kho ban đầu"
            };
            await _unitOfWork.GetRepository<StockMovement>().AddAsync(movement);
            await _unitOfWork.SaveAsync();

            return MapToSingleDto(newStock, product.Productname);
        }

        // 5. UPDATE (Đã cập nhật logic ProductionDate)
        public async Task UpdateStockAsync(int id, UpdateStockRequest req)
        {
            var stockRepo = _unitOfWork.GetRepository<Stock>();
            var stock = await stockRepo.GetByIdAsync(id);

            if (stock == null) throw new Exception("Lô hàng không tồn tại.");

            // Update số lượng
            stock.Stockquantity = req.Quantity;

            // Update Hạn sử dụng (DateTime -> DateOnly)
            stock.Expirydate = req.ExpiryDate.HasValue ? DateOnly.FromDateTime(req.ExpiryDate.Value) : null;

            // --- NEW: Update Ngày sản xuất ---
            stock.Productiondate = req.ProductionDate.HasValue ? DateOnly.FromDateTime(req.ProductionDate.Value) : null;
            // --------------------------------

            stock.Lastupdated = DateTime.Now;

            await stockRepo.UpdateAsync(stock);
        }

        // 6. DELETE (Giữ nguyên)
        public async Task DeleteStockAsync(int id)
        {
            var stockRepo = _unitOfWork.GetRepository<Stock>();
            var stock = await stockRepo.GetByIdAsync(id);
            if (stock == null) throw new Exception("Lô hàng không tồn tại.");
            stock.Status = StockStatus.DELETED;
            await stockRepo.UpdateAsync(stock);
        }

        // REPORT (Giữ nguyên)
        public async Task<IEnumerable<LowStockReportDto>> GetLowStockReportAsync(int threshold)
        {
            var stockRepo = _unitOfWork.GetRepository<Stock>();
            var stocks = await stockRepo.GetAllAsync(predicate: s => s.Status != StockStatus.EXPIRED, include: q => q.Include(s => s.Product));

            return stocks
                .Where(s => s.Product != null)
                .GroupBy(s => s.Productid)
                .Select(g => new LowStockReportDto
                {
                    ProductId = g.Key ?? 0,
                    Sku = g.First().Product!.Sku ?? "N/A",
                    ProductName = g.First().Product!.Productname ?? "Unknown",
                    TotalStockQuantity = g.Sum(s => s.Stockquantity) ?? 0,
                    Status = g.Sum(s => s.Stockquantity) == 0 ? "Critical" : (g.Sum(s => s.Stockquantity) <= threshold ? "Low" : "OK")
                })
                .Where(x => x.Status != "OK")
                .OrderBy(r => r.TotalStockQuantity)
                .ToList();
        }

        // --- HELPER METHODS (Để code gọn hơn) ---
        private IEnumerable<StockDto> MapToDto(IEnumerable<Stock> stocks)
        {
            return stocks.Select(s => new StockDto
            {
                StockId = s.Stockid,
                ProductId = s.Productid ?? 0,
                ProductName = s.Product?.Productname ?? "Unknown",
                Quantity = s.Stockquantity ?? 0,
                // Convert DateOnly -> DateTime
                ExpiryDate = s.Expirydate.HasValue ? s.Expirydate.Value.ToDateTime(TimeOnly.MinValue) : null,
                ProductionDate = s.Productiondate.HasValue ? s.Productiondate.Value.ToDateTime(TimeOnly.MinValue) : null, // Trả về thêm ProductionDate nếu DTO có trường này
                Status = s.Status ?? StockStatus.ACTIVE
            });
        }

        private StockDto MapToSingleDto(Stock s, string? productName = null)
        {
            return new StockDto
            {
                StockId = s.Stockid,
                ProductId = s.Productid ?? 0,
                ProductName = productName ?? s.Product?.Productname ?? "Unknown",
                Quantity = s.Stockquantity ?? 0,
                ExpiryDate = s.Expirydate.HasValue ? s.Expirydate.Value.ToDateTime(TimeOnly.MinValue) : null,
                Status = s.Status ?? StockStatus.ACTIVE
            };
        }
    }
}