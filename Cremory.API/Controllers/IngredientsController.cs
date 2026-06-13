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

        // GET: api/ingredients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Ingredient>>> GetIngredients()
        {
            // Pulls the ingredients straight from your Oracle Database table
            var ingredients = await _context.Ingredients.ToListAsync();
            return Ok(ingredients);
        }

        // GET: api/ingredients/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Ingredient>> GetIngredient(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);

            if (ingredient == null)
            {
                return NotFound(new { message = $"Ingredient with ID {id} not found." });
            }

            return Ok(ingredient);
        }
    }
}