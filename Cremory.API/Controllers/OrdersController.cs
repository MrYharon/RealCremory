using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Cremory.API.Data;
using Cremory.API.Hubs;
using Cremory.API.Models;
using Cremory.API.Services;

namespace Cremory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly CremoryDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly OrderParserService _orderParser;

        public OrdersController(CremoryDbContext context, IHubContext<OrderHub> hubContext,
            OrderParserService orderParser)
        {
            _context = context;
            _hubContext = hubContext;
            _orderParser = orderParser;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] bool? isArchived = null)
        {
            var query = _context.Orders.AsQueryable();

            if (isArchived.HasValue)
                query = query.Where(o => o.IsArchived == isArchived.Value);
            else
                query = query.Where(o => !o.IsArchived);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var statusFilter))
                query = query.Where(o => o.Status == statusFilter);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(o =>
                    o.CustomerName.ToLower().Contains(searchLower) ||
                    o.OrderId.ToLower().Contains(searchLower));
            }

            if (dateFrom.HasValue)
                query = query.Where(o => o.CreatedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(o => o.CreatedAt <= dateTo.Value);

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Total-Pages"] = ((int)Math.Ceiling(totalCount / (double)pageSize)).ToString();

            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(string id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { message = $"Order with ID {id} not found." });

            return Ok(order);
        }

        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(Order order)
        {
            order.OrderId = GenerateOrderId(order.Source);
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("OrderCreated", order);

            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
        }

        [HttpPost("parse")]
        public async Task<ActionResult<Order>> ParseOrder([FromBody] ParseOrderRequest request)
        {
            var result = _orderParser.TryParse(request.RawText);
            if (!result.IsSuccess || result.Order == null)
                return BadRequest(new { error = result.Error });

            var order = result.Order;
            order.OrderId = GenerateOrderId(order.Source);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("OrderCreated", order);

            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(string id, Order order)
        {
            if (id != order.OrderId)
                return BadRequest(new { message = "ID mismatch." });

            var existing = await _context.Orders.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Order with ID {id} not found." });

            existing.CustomerName = order.CustomerName;
            existing.Items = order.Items;
            existing.TotalPrice = order.TotalPrice;
            existing.Source = order.Source;
            existing.CustomerContact = order.CustomerContact;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("OrderUpdated", existing);

            return NoContent();
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] UpdateStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { message = $"Order with ID {id} not found." });

            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;

            if (request.Status == OrderStatus.Completed)
            {
                var setting = await _context.AppSettings.FindAsync("auto_deduct");
                if (setting?.Value != "false")
                {
                    await DeductStockFromItems(order.Items, order.TotalPrice);
                }
            }

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("OrderUpdated", order);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(string id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { message = $"Order with ID {id} not found." });

            order.IsArchived = true;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("OrderDeleted", id);

            return NoContent();
        }

        private static string GenerateOrderId(string source)
        {
            var prefix = source switch
            {
                "Facebook" => "ORD-FB",
                "Walk-in" => "ORD-WLK",
                _ => "ORD"
            };

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"{prefix}-{timestamp}";
        }

        private async Task DeductStockFromItems(string itemsText, decimal totalPrice)
        {
            if (string.IsNullOrWhiteSpace(itemsText))
                return;

            var products = await _context.Products
                .Where(p => p.IsActive && p.AutoDeduct)
                .ToListAsync();

            var parsedItems = ParseBulletItems(itemsText);
            var deductions = new Dictionary<int, int>();

            foreach (var item in parsedItems)
            {
                if (item.SubItems.Count > 0)
                {
                    foreach (var sub in item.SubItems)
                    {
                        var cleanFlavor = Regex.Replace(sub.Flavor, @"\[.*?\]", "").Trim();
                        var product = MatchByFlavor(products, cleanFlavor, p => p.Variant == "Solo");
                        if (product != null)
                        {
                            if (!deductions.ContainsKey(product.ProductId))
                                deductions[product.ProductId] = 0;
                            deductions[product.ProductId] += sub.Qty;
                        }
                    }
                }
                else
                {
                    var cleanName = Regex.Replace(item.Name, @"\[.*?\]", "").Trim();
                    var isRound = cleanName.Contains("round", StringComparison.OrdinalIgnoreCase)
                        || cleanName.Contains("inch", StringComparison.OrdinalIgnoreCase);
                    var product = MatchByFlavor(products, cleanName,
                        p => isRound
                            ? p.Variant == "6 Inch Round"
                            : p.Variant == "Solo");
                    if (product != null)
                    {
                        if (!deductions.ContainsKey(product.ProductId))
                            deductions[product.ProductId] = 0;
                        deductions[product.ProductId] += item.Qty;
                    }
                }
            }

            foreach (var kvp in deductions)
            {
                var product = products.FirstOrDefault(p => p.ProductId == kvp.Key);
                if (product != null)
                    product.CurrentStock = Math.Max(0, product.CurrentStock - kvp.Value);
            }
        }

        private static List<ParsedItem> ParseBulletItems(string text)
        {
            var result = new List<ParsedItem>();
            var lines = text.Split('\n');
            ParsedItem? current = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                var topMatch = Regex.Match(trimmed, @"^\*\s*(\d+)\s*x\s*(.+)$", RegexOptions.IgnoreCase);
                if (topMatch.Success)
                {
                    current = new ParsedItem
                    {
                        Qty = int.Parse(topMatch.Groups[1].Value),
                        Name = topMatch.Groups[2].Value.Trim(),
                        SubItems = []
                    };
                    result.Add(current);
                    continue;
                }

                var subMatch = Regex.Match(trimmed, @"^[-•]\s*(\d+)\s+(.+)$");
                if (subMatch.Success && current != null)
                {
                    current.SubItems.Add(new SubItem
                    {
                        Qty = int.Parse(subMatch.Groups[1].Value),
                        Flavor = subMatch.Groups[2].Value.Trim()
                    });
                }
            }

            return result;
        }

        private static Product? MatchByFlavor(IEnumerable<Product> products, string searchText, Func<Product, bool> filter)
        {
            var searchLower = searchText.ToLowerInvariant().Trim();
            return products.FirstOrDefault(p =>
                filter(p) &&
                p.Flavor != null &&
                p.Flavor.Trim().Equals(searchLower, StringComparison.OrdinalIgnoreCase));
        }

        private class ParsedItem
        {
            public int Qty { get; set; }
            public string Name { get; set; } = string.Empty;
            public List<SubItem> SubItems { get; set; } = [];
        }

        private class SubItem
        {
            public int Qty { get; set; }
            public string Flavor { get; set; } = string.Empty;
        }
    }

    public class UpdateStatusRequest
    {
        public OrderStatus Status { get; set; }
    }

    public class ParseOrderRequest
    {
        public string RawText { get; set; } = string.Empty;
    }
}
