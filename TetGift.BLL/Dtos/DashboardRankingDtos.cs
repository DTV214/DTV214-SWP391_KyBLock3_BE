namespace TetGift.BLL.Dtos
{
    public class DashboardRankingRequest
    {
        /// <summary>
        /// week | month | year
        /// </summary>
        public string Period { get; set; } = "month";

        /// <summary>
        /// Dùng cho week. Tuần được tính từ thứ 2 đến chủ nhật.
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        /// Dùng cho month / year
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// Dùng cho month
        /// </summary>
        public int? Month { get; set; }
    }

    public class RankingPeriodInfoDto
    {
        public string Period { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; } // inclusive để FE dễ hiển thị
    }

    public class CategoryRankingItemDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public int QuantitySold { get; set; }
    }

    public class CategoryRankingResponseDto
    {
        public RankingPeriodInfoDto Range { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalQuantitySold { get; set; }
        public List<CategoryRankingItemDto> Data { get; set; } = new();
    }

    public class ProductRankingItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public int QuantitySold { get; set; }
    }

    public class CategoryProductRankingResponseDto
    {
        public RankingPeriodInfoDto Range { get; set; } = new();
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalQuantitySold { get; set; }
        public List<ProductRankingItemDto> Data { get; set; } = new();
    }
}
