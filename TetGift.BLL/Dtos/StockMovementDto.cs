using System.ComponentModel.DataAnnotations;

namespace TetGift.BLL.Dtos
{
    // DTO trả về thông tin Stock Movement
    public class StockMovementDto
    {
        public int Stockmovementid { get; set; }
        public int? Stockid { get; set; }
        public int? Orderid { get; set; }
        public int? Quantity { get; set; }
        public DateTime? Movementdate { get; set; }
        public string? Note { get; set; }
        public string? ProductName { get; set; }
        public string? MovementType { get; set; } // "IN" hoặc "OUT"
    }

    // DTO Tạo Stock Movement (Nhập/Xuất kho)
    public class CreateStockMovementRequest
    {
        [Required]
        public int StockId { get; set; }

        public int? OrderId { get; set; } // Null nếu là nhập kho

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        public string? Note { get; set; }
    }

    // DTO cho báo cáo Stock Movement theo khoảng thời gian
    public class StockMovementReportRequest
    {
        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        public int? ProductId { get; set; } // Null = tất cả sản phẩm
    }
}