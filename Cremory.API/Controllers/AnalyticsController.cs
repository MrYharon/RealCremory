using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cremory.API.Data;
using Cremory.API.Models;

namespace Cremory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly CremoryDbContext _context;

        public AnalyticsController(CremoryDbContext context)
        {
            _context = context;
        }

        [HttpGet("finance")]
        public async Task<ActionResult<FinanceSummaryDto>> GetFinanceSummary()
        {
            var now = DateTime.UtcNow;
            var orders = await _context.Orders.ToListAsync();

            var today = orders.Where(o => o.CreatedAt.Date == now.Date
                                          && o.Status == OrderStatus.Completed).ToList();
            var thisWeek = orders.Where(o => o.CreatedAt > now.AddDays(-7)
                                             && o.Status == OrderStatus.Completed).ToList();
            var thisMonth = orders.Where(o => o.CreatedAt > now.AddDays(-30)
                                              && o.Status == OrderStatus.Completed).ToList();

            var todayRev = today.Sum(o => o.TotalPrice);
            var weekRev = thisWeek.Sum(o => o.TotalPrice);
            var monthRev = thisMonth.Sum(o => o.TotalPrice);
            var daysElapsed = Math.Max(1, (int)(now - now.AddDays(-30).Date).TotalDays);
            var estimatedCost = monthRev * 0.32m;
            var netProfit = monthRev - estimatedCost;

            var recent = orders
                .Where(o => o.Status == OrderStatus.Completed)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new RecentTransactionDto
                {
                    CustomerName = $"{o.CustomerName} - {o.Source}",
                    TotalPrice = o.TotalPrice,
                    Timestamp = FormatTime(o.CreatedAt),
                    Source = o.Source
                })
                .ToList();

            return Ok(new FinanceSummaryDto
            {
                TodayRevenue = todayRev,
                TodayOrders = today.Count,
                WeekRevenue = weekRev,
                WeekAverage = daysElapsed > 0 ? weekRev / Math.Max(1, thisWeek.Count) : 0,
                MonthRevenue = monthRev,
                TotalOrdersMonth = thisMonth.Count,
                AvgOrderValue = thisMonth.Count > 0 ? monthRev / thisMonth.Count : 0,
                ProfitMargin = 68,
                NetProfit = netProfit,
                RecentTransactions = recent
            });
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardAnalyticsDto>> GetDashboard()
        {
            var now = DateTime.UtcNow;
            var orders = await _context.Orders.ToListAsync();
            var ingredients = await _context.Ingredients.ToListAsync();

            var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

            var fbCount = completed.Count(o => o.Source == "Facebook");
            var walkInCount = completed.Count(o => o.Source == "Walk-in");
            var totalCompleted = completed.Count;
            var fbPct = totalCompleted > 0 ? (int)Math.Round(fbCount * 100.0 / totalCompleted) : 0;
            var walkInPct = totalCompleted > 0 ? 100 - fbPct : 0;

            var weeklySales = new List<decimal>();
            var weekLabels = new List<string>();
            for (int i = 6; i >= 0; i--)
            {
                var day = now.AddDays(-i);
                var dayTotal = completed
                    .Where(o => o.CreatedAt.Date == day.Date)
                    .Sum(o => o.TotalPrice);
                weeklySales.Add(dayTotal);
                weekLabels.Add(day.ToString("ddd"));
            }

            var itemCounts = new Dictionary<string, int>();
            foreach (var order in completed)
            {
                var lines = order.Items.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var cleaned = line.Trim();
                    if (string.IsNullOrEmpty(cleaned)) continue;

                    var xIndex = cleaned.IndexOf('x');
                    if (xIndex > 0)
                    {
                        var itemName = cleaned[(xIndex + 1)..].Trim();
                        if (itemName.Length > 0)
                        {
                            itemCounts.TryGetValue(itemName, out var count);
                            itemCounts[itemName] = count + 1;
                        }
                    }
                }
            }

            var popular = itemCounts
                .OrderByDescending(kv => kv.Value)
                .Take(5)
                .Select(kv => new PopularItemDto { Name = kv.Key, Count = kv.Value })
                .ToList();

            var lowStock = ingredients.Count(i => i.StockQuantity <= i.ReorderLevel);

            return Ok(new DashboardAnalyticsDto
            {
                PopularItems = popular,
                FacebookPct = fbPct,
                WalkInPct = walkInPct,
                WeeklySales = weeklySales,
                WeekLabels = weekLabels,
                LowStockCount = lowStock,
                AvgOrderValue = completed.Count > 0
                    ? Math.Round(completed.Sum(o => o.TotalPrice) / completed.Count, 2)
                    : 0
            });
        }

        private static string FormatTime(DateTime dateTime)
        {
            var diff = DateTime.UtcNow - dateTime;
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 2) return "Yesterday";
            return dateTime.ToString("MMM dd");
        }
    }
}
