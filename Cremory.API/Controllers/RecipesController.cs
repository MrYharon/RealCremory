using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cremory.API.Data;
using Cremory.API.Models;

namespace Cremory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly CremoryDbContext _context;

        public RecipesController(CremoryDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Recipe>>> GetRecipes()
        {
            return await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Recipe>> GetRecipe(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (recipe == null)
                return NotFound(new { message = $"Recipe with ID {id} not found." });

            return Ok(recipe);
        }

        [HttpPost]
        public async Task<ActionResult<Recipe>> CreateRecipe(Recipe recipe)
        {
            try
            {
                if (recipe.RecipeIngredients != null)
                {
                    foreach (var ri in recipe.RecipeIngredients)
                    {
                        if (ri.Ingredient != null)
                            _context.Entry(ri.Ingredient).State = EntityState.Unchanged;
                    }
                }

                _context.Recipes.Add(recipe);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetRecipe), new { id = recipe.RecipeId }, recipe);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecipe(int id, Recipe recipe)
        {
            if (id != recipe.RecipeId)
                return BadRequest(new { message = "ID mismatch." });

            var existing = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (existing == null)
                return NotFound(new { message = $"Recipe with ID {id} not found." });

            existing.Name = recipe.Name;
            existing.Description = recipe.Description;
            existing.SellingPrice = recipe.SellingPrice;
            existing.IsActive = recipe.IsActive;

            _context.RecipeIngredients.RemoveRange(existing.RecipeIngredients);

            foreach (var ri in recipe.RecipeIngredients)
            {
                _context.Entry(ri.Ingredient).State = EntityState.Unchanged;
                existing.RecipeIngredients.Add(new RecipeIngredient
                {
                    RecipeId = id,
                    IngredientId = ri.IngredientId,
                    Quantity = ri.Quantity
                });
            }

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
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null)
                return NotFound(new { message = $"Recipe with ID {id} not found." });

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
