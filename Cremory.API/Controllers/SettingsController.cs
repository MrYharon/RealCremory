using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cremory.API.Data;
using Cremory.API.Models;

namespace Cremory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly CremoryDbContext _context;

        public SettingsController(CremoryDbContext context)
        {
            _context = context;
        }

        [HttpGet("auto-deduct")]
        public async Task<ActionResult<object>> GetAutoDeduct()
        {
            var setting = await _context.AppSettings.FindAsync("auto_deduct");
            var enabled = setting?.Value != "false";
            return Ok(new { autoDeduct = enabled });
        }

        [HttpPut("auto-deduct")]
        public async Task<IActionResult> SetAutoDeduct([FromBody] SetAutoDeductRequest request)
        {
            var setting = await _context.AppSettings.FindAsync("auto_deduct");
            if (setting == null)
            {
                _context.AppSettings.Add(new AppSetting
                {
                    Key = "auto_deduct",
                    Value = request.Enabled ? "true" : "false"
                });
            }
            else
            {
                setting.Value = request.Enabled ? "true" : "false";
            }
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("low-stock")]
        public async Task<ActionResult<IEnumerable<object>>> GetLowStockProducts()
        {
            var products = await _context.Products
                .Where(p => p.IsActive && p.CurrentStock <= p.LowStockThreshold)
                .OrderBy(p => p.CurrentStock)
                .Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    p.Variant,
                    p.Flavor,
                    p.CurrentStock,
                    p.LowStockThreshold,
                    p.Unit
                })
                .ToListAsync();
            return Ok(products);
        }

        public class SetAutoDeductRequest
        {
            public bool Enabled { get; set; }
        }
    }
}
