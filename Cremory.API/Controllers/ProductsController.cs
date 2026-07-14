using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cremory.API.Data;
using Cremory.API.Models;

namespace Cremory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly CremoryDbContext _context;

        public ProductsController(CremoryDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }

        [HttpGet("menu")]
        public async Task<ActionResult<IEnumerable<MenuCategoryDto>>> GetMenu()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Category != null)
                .OrderBy(p => p.Category!.DisplayOrder)
                .ThenBy(p => p.DisplayOrder)
                .ToListAsync();

            var menu = products
                .GroupBy(p => new { p.CategoryId, CategoryName = p.Category!.Name })
                .Select(g => new MenuCategoryDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    Items = g.Select(p => new MenuItemDto
                    {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        Variant = p.Variant,
                        Flavor = p.Flavor,
                        BasePrice = p.BasePrice,
                        AddOnDescription = p.AddOnDescription,
                        AddOnPricePerUnit = p.AddOnPricePerUnit,
                        DisplayOrder = p.DisplayOrder
                    }).ToList()
                })
                .ToList();

            return Ok(menu);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound(new { message = $"Product with ID {id} not found." });

            return Ok(product);
        }

        [HttpPost("categories")]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategories), category);
        }

        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            if (await _context.Products.AnyAsync(p => p.Name == product.Name && p.Variant == product.Variant))
                return Conflict(new { message = $"Product '{product.Name}' already exists." });

            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.ProductId)
                return BadRequest(new { message = "ID mismatch." });

            var existing = await _context.Products.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Product with ID {id} not found." });

            if (await _context.Products.AnyAsync(p => p.Name == product.Name && p.Variant == product.Variant && p.ProductId != id))
                return Conflict(new { message = $"Product '{product.Name}' already exists." });

            existing.CategoryId = product.CategoryId;
            existing.Name = product.Name;
            existing.Variant = product.Variant;
            existing.Flavor = product.Flavor;
            existing.BasePrice = product.BasePrice;
            existing.AddOnDescription = product.AddOnDescription;
            existing.AddOnPricePerUnit = product.AddOnPricePerUnit;
            existing.IsActive = product.IsActive;
            existing.DisplayOrder = product.DisplayOrder;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = $"Product with ID {id} not found." });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("stock")]
        public async Task<ActionResult<IEnumerable<ProductStockDto>>> GetStock()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new ProductStockDto
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    Variant = p.Variant,
                    Flavor = p.Flavor,
                    CurrentStock = p.CurrentStock,
                    LowStockThreshold = p.LowStockThreshold,
                    IsLowStock = p.CurrentStock <= p.LowStockThreshold
                })
                .ToListAsync();

            return Ok(products);
        }

        [HttpPut("{id}/stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = $"Product with ID {id} not found." });

            if (request.NewStock < 0)
                return BadRequest(new { message = "Stock cannot be negative." });

            product.CurrentStock = request.NewStock;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("stock/batch")]
        public async Task<IActionResult> BatchUpdateStock([FromBody] List<BatchStockUpdate> updates)
        {
            var errors = new List<string>();
            foreach (var update in updates)
            {
                var product = await _context.Products.FindAsync(update.ProductId);
                if (product == null)
                {
                    errors.Add($"Product {update.ProductId} not found.");
                    continue;
                }
                if (update.NewStock < 0)
                {
                    errors.Add($"Stock for product {update.ProductId} cannot be negative.");
                    continue;
                }
                product.CurrentStock = update.NewStock;
            }

            await _context.SaveChangesAsync();

            if (errors.Count > 0)
                return Ok(new { updated = updates.Count - errors.Count, errors });

            return NoContent();
        }
    }

    public class ProductStockDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Variant { get; set; }
        public string? Flavor { get; set; }
        public int CurrentStock { get; set; }
        public int LowStockThreshold { get; set; }
        public bool IsLowStock { get; set; }
    }

    public class UpdateStockRequest
    {
        public int NewStock { get; set; }
    }

    public class BatchStockUpdate
    {
        public int ProductId { get; set; }
        public int NewStock { get; set; }
    }
}
