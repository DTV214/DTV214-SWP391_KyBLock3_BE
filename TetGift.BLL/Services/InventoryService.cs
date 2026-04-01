using Microsoft.EntityFrameworkCore;
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

        // 1. READ ALL
        public async Task<IEnumerable<StockDto>> GetAllStocksAsync()
        {
            var stockRepo = _unitOfWork.GetRepository<Stock>();
            var stocks = await stockRepo.GetAllAsync(
                predicate: s => s.Status != StockStatus.DELETED,
                include: q => q.Include(s => s.Product)
            );
            return MapToDto(stocks);
        }

        // 2. READ BY ID
        public async Task<StockDto> GetStockByIdAsync(int id)
        {
            var stockRepo = _unitOfWork.GetRepository<Stock>();
            var stock = await stockRepo.FindAsync(s => s.Stockid == id, include: q => q.Include(s => s.Product));
            if (stock == null) throw new Exception("Lô hàng không tồn tại.");
            return MapToSingleDto(stock);
        }

        // 3. READ BY PRODUCT ID
        public async Task<IEnumerable<StockDto>> GetStocksByProductIdAsync(int productId)
        {
            var stockRepo = _unitOfWork.GetRepository<Stock>();
            var stocks = await stockRepo.GetAllAsync(
                predicate: s => s.Productid == productId && s.Status != StockStatus.DELETED,
                include: q => q.Include(s => s.Product)
            );
            return MapToDto(stocks);
        }

        // 4. CREATE
        public async Task<StockDto> CreateStockAsync(CreateStockRequest req)
        {
            var productRepo = _unitOfWork.GetRepository<Product>();
            var product = await productRepo.GetByIdAsync(req.ProductId);
            if (product == null) throw new Exception("Sản phẩm không tồn tại.");

            DateOnly? productionDateOnly = req.ProductionDate.HasValue ? DateOnly.FromDateTime(req.ProductionDate.Value) : null;
            DateOnly? expiryDateOnly = req.ExpiryDate.HasValue ? DateOnly.FromDateTime(req.ExpiryDate.Value) : null;
            DateOnly today = DateOnly.FromDateTime(DateTime.Now);

            // --- LÔGIC ƯU TIÊN TRẠNG THÁI (HIERARCHY) ---
            string finalStatus = StockStatus.ACTIVE; // Mặc định là bán bình thường

            if (expiryDateOnly.HasValue && expiryDateOnly.Value < today)
            {
                // Ưu tiên 1: Đã quá hạn -> Bỏ đi, không quan tâm số lượng
                finalStatus = StockStatus.EXPIRED;
            }
            else if (req.Quantity <= 0)
            {
                // Ưu tiên 2: Hạn dùng OK, nhưng kho trống trơn -> Hết hàng
                finalStatus = StockStatus.OUT_OF_STOCK;
            }

            var newStock = new Stock
            {
                Productid = req.ProductId,
                Stockquantity = req.Quantity,
                Productiondate = productionDateOnly,
                Expirydate = expiryDateOnly,
                Status = finalStatus, // Gán status đã được tính toán tự động
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
                Note = finalStatus == StockStatus.EXPIRED ? "Nhập kho ban đầu (Lô hàng đã hết hạn)"
                     : finalStatus == StockStatus.OUT_OF_STOCK ? "Nhập kho ban đầu (Số lượng bằng 0)"
                     : "Nhập kho ban đầu"
            };
            await _unitOfWork.GetRepository<StockMovement>().AddAsync(movement);
            await _unitOfWork.SaveAsync();

            return MapToSingleDto(newStock, product.Productname);
        }

        // 5. UPDATE
        public async Task UpdateStockAsync(int id, UpdateStockRequest req)
        {
            var stockRepo = _unitOfWork.GetRepository<Stock>();
            var stock = await stockRepo.GetByIdAsync(id);

            if (stock == null) throw new Exception("Lô hàng không tồn tại.");

            stock.Stockquantity = req.Quantity;
            stock.Productiondate = req.ProductionDate.HasValue ? DateOnly.FromDateTime(req.ProductionDate.Value) : null;
            stock.Expirydate = req.ExpiryDate.HasValue ? DateOnly.FromDateTime(req.ExpiryDate.Value) : null;

            DateOnly today = DateOnly.FromDateTime(DateTime.Now);

            // --- LÔGIC ƯU TIÊN TRẠNG THÁI (HIERARCHY) ---
            if (stock.Expirydate.HasValue && stock.Expirydate.Value < today)
            {
                stock.Status = StockStatus.EXPIRED;
            }
            else if (req.Quantity <= 0)
            {
                stock.Status = StockStatus.OUT_OF_STOCK;
            }
            else
            {
                stock.Status = StockStatus.ACTIVE;
            }

            stock.Lastupdated = DateTime.Now;
            await stockRepo.UpdateAsync(stock);
        }

        // 6. DELETE
        public async Task DeleteStockAsync(int id)
        {
            var stockRepo = _unitOfWork.GetRepository<Stock>();
            var stock = await stockRepo.GetByIdAsync(id);
            if (stock == null) throw new Exception("Lô hàng không tồn tại.");
            stock.Status = StockStatus.DELETED;
            await stockRepo.UpdateAsync(stock);
        }

        // REPORT (Đã Fix Lỗi Phân Loại)
        public async Task<IEnumerable<LowStockReportDto>> GetLowStockReportAsync(int threshold)
        {
            var stockRepo = _unitOfWork.GetRepository<Stock>();

            // Lấy tất cả lô hàng KHÔNG BỊ XÓA (để giữ lại OUT_OF_STOCK và EXPIRED nhằm tính toán logic)
            var stocks = await stockRepo.GetAllAsync(
                predicate: s => s.Status != StockStatus.DELETED,
                include: q => q.Include(s => s.Product)
            );

            return stocks
                .Where(s => s.Product != null)
                .GroupBy(s => s.Productid)
                .Select(g =>
                {
                    // LÔGIC MỚI: Chỉ tính tổng số lượng của các lô hàng CÓ THỂ BÁN ĐƯỢC (ACTIVE)
                    int totalSellableQuantity = g.Where(x => x.Status == StockStatus.ACTIVE)
                                                 .Sum(x => x.Stockquantity) ?? 0;

                    // Phân loại dựa trên số lượng CÓ THỂ BÁN
                    string statusLevel = totalSellableQuantity == 0 ? "Critical"
                                       : (totalSellableQuantity <= threshold ? "Low" : "OK");

                    return new LowStockReportDto
                    {
                        ProductId = g.Key ?? 0,
                        Sku = g.First().Product!.Sku ?? "N/A",
                        ProductName = g.First().Product!.Productname ?? "Unknown",
                        TotalStockQuantity = totalSellableQuantity, // Trả về số lượng thực tế bán được
                        Status = statusLevel
                    };
                })
                .Where(x => x.Status != "OK") // Chỉ lấy những thằng đang gặp báo động
                .OrderBy(r => r.TotalStockQuantity)
                .ToList();
        }

        public async Task<IEnumerable<StockMovementDto>> GetMovementsByDetailAsync(int orderId, int productId)
        {
            var movementRepo = _unitOfWork.GetRepository<StockMovement>();
            var stockRepo = _unitOfWork.GetRepository<Stock>();

            var productStocks = await stockRepo.FindAsync(s => s.Productid == productId);

            var stockIds = productStocks.Select(s => s.Stockid).ToList();
            var movements = await movementRepo.FindAsync(
                m => m.Orderid == orderId &&
                stockIds.Contains(m.Stockid.Value)
                );

            return [.. movements.Select(m => MapMovement(m))];
        }

        // --- HELPER METHODS ---
        private IEnumerable<StockDto> MapToDto(IEnumerable<Stock> stocks)
        {
            return stocks.Select(s => new StockDto
            {
                StockId = s.Stockid,
                ProductId = s.Productid ?? 0,
                ProductName = s.Product?.Productname ?? "Unknown",
                Quantity = s.Stockquantity ?? 0,
                ExpiryDate = s.Expirydate.HasValue ? s.Expirydate.Value.ToDateTime(TimeOnly.MinValue) : null,
                ProductionDate = s.Productiondate.HasValue ? s.Productiondate.Value.ToDateTime(TimeOnly.MinValue) : null,
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
                ProductionDate = s.Productiondate.HasValue ? s.Productiondate.Value.ToDateTime(TimeOnly.MinValue) : null,
                Status = s.Status ?? StockStatus.ACTIVE
            };
        }

        private StockMovementDto MapMovement(StockMovement movement)
        {
            return new StockMovementDto()
            {
                Stockid = movement.Stockid,
                Movementdate = movement.Movementdate,
                Stockmovementid = movement.Stockmovementid,
                Orderid = movement.Orderid,
                Quantity = movement.Quantity,
                Note = movement.Note
            };
        }
    }
}