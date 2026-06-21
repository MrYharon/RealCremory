namespace Cremory.App.Models
{
    public class Recipe
    {
        public int RecipeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal SellingPrice { get; set; }
        public bool IsActive { get; set; } = true;
        public List<RecipeIngredient> RecipeIngredients { get; set; } = [];
    }

    public class RecipeIngredient
    {
        public int RecipeIngredientId { get; set; }
        public int RecipeId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public Ingredient? Ingredient { get; set; }

        public string IngredientDisplay => Ingredient != null
            ? $"{Ingredient.Name} x{Quantity} {Ingredient.Unit}"
            : $"Ingredient #{IngredientId} x{Quantity}";
    }
}
