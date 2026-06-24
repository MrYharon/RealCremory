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
            [FromQuery] int pageSize = 100)
        {
            var query = _context.Orders.AsQueryable();

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

            _context.Orders.Remove(order);
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
