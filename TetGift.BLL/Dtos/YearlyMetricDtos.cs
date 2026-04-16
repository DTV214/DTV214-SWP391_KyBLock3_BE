namespace TetGift.BLL.Dtos
{
    public class YearComparisonRequest
    {
        public int Year { get; set; }
        public int? CompareYear { get; set; }
    }

    public class MonthlyComparisonPointDto
    {
        public int Month { get; set; }   // 1..12
        public decimal Value { get; set; }
    }

    public class YearlySeriesDto
    {
        public int Year { get; set; }
        public string Label { get; set; } = string.Empty; // "2026"
        public decimal Total { get; set; }
        public List<MonthlyComparisonPointDto> Data { get; set; } = new();
    }

    public class YearComparisonChartDto
    {
        public string Metric { get; set; } = string.Empty; // ORDER_REVENUE | ACTUAL_REVENUE
        public List<int> XAxisMonths { get; set; } = new(); // 1..12
        public YearlySeriesDto BaseYear { get; set; } = new();
        public YearlySeriesDto CompareYear { get; set; } = new();
    }
}
