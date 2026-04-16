namespace TetGift.BLL.Dtos
{
    public class MonthComparisonRequest
    {
        public int Year { get; set; }
        public int Month { get; set; } // 1-12

        public int? CompareYear { get; set; }
        public int? CompareMonth { get; set; } // 1-12
    }

    public class DailyComparisonPointDto
    {
        public int Day { get; set; }
        public decimal Value { get; set; }
    }

    public class MonthlySeriesDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Label { get; set; } = string.Empty; // yyyy-MM
        public int DaysInMonth { get; set; }
        public decimal Total { get; set; }
        public List<DailyComparisonPointDto> Data { get; set; } = new();
    }

    public class MonthlyComparisonChartDto
    {
        public string Metric { get; set; } = string.Empty; // ORDER_REVENUE | ACTUAL_REVENUE
        public List<int> XAxisDays { get; set; } = new();  // 1..31
        public MonthlySeriesDto BaseMonth { get; set; } = new();
        public MonthlySeriesDto CompareMonth { get; set; } = new();
    }
}
