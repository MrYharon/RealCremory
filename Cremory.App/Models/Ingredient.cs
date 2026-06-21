namespace Cremory.App.Models
{
    public class Ingredient
    {
        public int IngredientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal StockQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal ReorderLevel { get; set; }

        public bool IsLowStock => StockQuantity <= ReorderLevel;

        public double StockPercentage
        {
            get
            {
                if (ReorderLevel <= 0) return 1.0;
                var max = ReorderLevel * 3;
                var pct = (double)(StockQuantity / max);
                return Math.Min(pct, 1.0);
            }
        }
    }
}