using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos
{
    // DTO trả về thông tin Stock
    public class StockDto
    {
        public int StockId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? ProductionDate { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    // DTO Tạo mới lô hàng
    public class CreateStockRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        public DateTime? ProductionDate { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }

    // DTO Cập nhật lô hàng
    public class UpdateStockRequest
    {
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không hợp lệ")]
        public int Quantity { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public DateTime? ProductionDate { get; set; }

        public string? Status { get; set; } // Available, Out of Stock, Expired
    }

    // DTO báo cáo tồn kho thấp
    public class LowStockReportDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? Sku { get; set; }
        public int TotalQuantity { get; set; }
        public int Threshold { get; set; }
        public string CategoryName { get; set; } = null!;
    }

    // DTO báo cáo hàng sắp hết hạn
    public class ExpiringStockReportDto
    {
        public int StockId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public DateTime? ProductionDate { get; set; }
    }

    // DTO kiểm tra tồn kho
    public class StockAvailabilityDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int TotalAvailableQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public List<StockBatchDto> Batches { get; set; } = new();
    }

    public class StockBatchDto
    {
        public int StockId { get; set; }
        public int Quantity { get; set; }
        public DateTime? ProductionDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = null!;
    }
}