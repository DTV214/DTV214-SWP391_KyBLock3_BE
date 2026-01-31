using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetGift.BLL.Dtos
{
    using System;
    using System.ComponentModel.DataAnnotations;

    namespace TetGift.BLL.Dtos
    {
        // DTO trả về thông tin
        public class StockDto
        {
            public int StockId { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; } = null!;
            public int Quantity { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public string Status { get; set; } = null!;
            public DateTime? ProductionDate { get; set; }
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
        }
    }
}