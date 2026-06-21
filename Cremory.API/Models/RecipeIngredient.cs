using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cremory.API.Models
{
    [Table("RECIPE_INGREDIENTS")]
    public class RecipeIngredient
    {
        [Key]
        [Column("RECIPE_INGREDIENT_ID")]
        public int RecipeIngredientId { get; set; }

        [Required]
        [Column("RECIPE_ID")]
        public int RecipeId { get; set; }

        [Required]
        [Column("INGREDIENT_ID")]
        public int IngredientId { get; set; }

        [Required]
        [Column("QUANTITY")]
        public decimal Quantity { get; set; }

        [ForeignKey(nameof(RecipeId))]
        public Recipe Recipe { get; set; } = null!;

        [ForeignKey(nameof(IngredientId))]
        public Ingredient Ingredient { get; set; } = null!;
    }
}
