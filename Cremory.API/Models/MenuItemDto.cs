namespace Cremory.API.Models
{
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

    public class MenuCategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<MenuItemDto> Items { get; set; } = [];
    }
}
