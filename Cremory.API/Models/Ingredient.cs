using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cremory.API.Models
{
    [Table("INGREDIENTS")]
    public class Ingredient
    {
        [Key]
        [Column("INGREDIENT_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IngredientId { get; set; }

        [Required]
        [Column("NAME")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("STOCK_QUANTITY")]
        [Range(0, double.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public decimal StockQuantity { get; set; }

        [Required]
        [Column("UNIT")]
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Column("REORDER_LEVEL")]
        [Range(0, double.MaxValue, ErrorMessage = "Reorder level cannot be negative")]
        public decimal ReorderLevel { get; set; }
    }
}