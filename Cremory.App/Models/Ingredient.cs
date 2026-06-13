namespace Cremory.App.Models
{
    public class Ingredient
    {
        public int IngredientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal StockQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal ReorderLevel { get; set; }

        // Useful property for a quick status indicator UI later
        public bool IsLowStock => StockQuantity <= ReorderLevel;
    }
}