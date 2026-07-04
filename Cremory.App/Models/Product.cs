namespace Cremory.App.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Variant { get; set; }
        public string? Flavor { get; set; }
        public decimal BasePrice { get; set; }
        public string? AddOnDescription { get; set; }
        public decimal? AddOnPricePerUnit { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public int CurrentStock { get; set; }
        public int LowStockThreshold { get; set; } = 10;
        public double ActiveOpacity => IsActive ? 1.0 : 0.5;
        public bool IsInactive => !IsActive;
        public bool IsLowStock => CurrentStock <= LowStockThreshold;
    }

    public class MenuCategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<MenuItemDto> Items { get; set; } = [];
    }

    public class MenuItemDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Variant { get; set; }
        public string? Flavor { get; set; }
        public decimal BasePrice { get; set; }
        public string? AddOnDescription { get; set; }
        public decimal? AddOnPricePerUnit { get; set; }
        public int DisplayOrder { get; set; }
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

    public class ProductStockUpdate
    {
        public int ProductId { get; set; }
        public int NewStock { get; set; }
    }
}
