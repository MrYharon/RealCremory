using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cremory.API.Models
{
    [Table("RECIPES")]
    public class Recipe
    {
        [Key]
        [Column("RECIPE_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RecipeId { get; set; }

        [Required]
        [Column("NAME")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("DESCRIPTION")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Column("SELLING_PRICE")]
        public decimal SellingPrice { get; set; }

        [Column("IS_ACTIVE")]
        public bool IsActive { get; set; } = true;

        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    }
}
