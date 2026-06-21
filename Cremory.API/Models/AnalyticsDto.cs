namespace Cremory.API.Models
{
    public class FinanceSummaryDto
    {
        public decimal TodayRevenue { get; set; }
        public int TodayOrders { get; set; }
        public decimal WeekRevenue { get; set; }
        public decimal WeekAverage { get; set; }
        public decimal MonthRevenue { get; set; }
        public int TotalOrdersMonth { get; set; }
        public decimal AvgOrderValue { get; set; }
        public decimal ProfitMargin { get; set; } = 68;
        public decimal NetProfit { get; set; }
        public List<RecentTransactionDto> RecentTransactions { get; set; } = [];
    }

    public class RecentTransactionDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Timestamp { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }

    public class DashboardAnalyticsDto
    {
        public List<PopularItemDto> PopularItems { get; set; } = [];
        public int FacebookPct { get; set; }
        public int WalkInPct { get; set; }
        public List<decimal> WeeklySales { get; set; } = [];
        public List<string> WeekLabels { get; set; } = [];
        public int LowStockCount { get; set; }
        public decimal AvgOrderValue { get; set; }
    }

    public class PopularItemDto
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
