using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetGift.BLL.Dtos
{
    public class ProductStatisticResponseDto
    {
        public decimal TotalGrossRevenue { get; set; } // Tổng doanh thu gốc
        public decimal TotalNetRevenue { get; set; }   // Tổng doanh thu thực nhận
        public decimal TotalProfit { get; set; }       // Tổng lợi nhuận
        public int TotalQuantitySold { get; set; }     // Tổng số lượng bán ra
        public List<ProductOrderStatDto> Orders { get; set; } = new();
    }
    public class ProductOrderStatDto
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? CustomerName { get; set; }
        public int Quantity { get; set; }
        public decimal GrossRevenue { get; set; }      // Doanh thu gốc đơn này
        public decimal NetRevenue { get; set; }        // Doanh thu thực đơn này
        public decimal Profit { get; set; }            // Lợi nhuận đơn này
    }
}
