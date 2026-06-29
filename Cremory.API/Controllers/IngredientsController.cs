using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cremory.API.Data;
using Cremory.API.Models;

namespace Cremory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IngredientsController : ControllerBase
    {
        private readonly CremoryDbContext _context;

        public IngredientsController(CremoryDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ingredient>>> GetIngredients()
        {
            return await _context.Ingredients.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Ingredient>> GetIngredient(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
                return NotFound(new { message = $"Ingredient with ID {id} not found." });

            return Ok(ingredient);
        }

        [HttpPost]
        public async Task<ActionResult<Ingredient>> CreateIngredient(Ingredient ingredient)
        {
            if (await _context.Ingredients.AnyAsync(i => i.Name == ingredient.Name))
                return Conflict(new { message = $"Ingredient '{ingredient.Name}' already exists." });

            try
            {
                _context.Ingredients.Add(ingredient);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetIngredient), new { id = ingredient.IngredientId }, ingredient);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIngredient(int id, Ingredient ingredient)
        {
            if (id != ingredient.IngredientId)
                return BadRequest(new { message = "ID mismatch." });

            var existing = await _context.Ingredients.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Ingredient with ID {id} not found." });

            if (await _context.Ingredients.AnyAsync(i => i.Name == ingredient.Name && i.IngredientId != id))
                return Conflict(new { message = $"Ingredient '{ingredient.Name}' already exists." });

            existing.Name = ingredient.Name;
            existing.StockQuantity = ingredient.StockQuantity;
            existing.Unit = ingredient.Unit;
            existing.ReorderLevel = ingredient.ReorderLevel;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient == null)
                return NotFound(new { message = $"Ingredient with ID {id} not found." });

            _context.Ingredients.Remove(ingredient);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
