using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetGift.BLL.Dtos
{
    public class LowStockReportDto
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public int TotalStockQuantity { get; set; }
        public string Status { get; set; } = null!; // "Critical" | "Low"
    }
}
