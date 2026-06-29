using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cremory.API.Models
{
    [Table("PRODUCTS")]
    public class Product
    {
        [Key]
        [Column("PRODUCT_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductId { get; set; }

        [Required]
        [Column("CATEGORY_ID")]
        public int CategoryId { get; set; }

        [Required]
        [Column("NAME")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column("VARIANT")]
        [StringLength(50)]
        public string? Variant { get; set; }

        [Column("FLAVOR")]
        [StringLength(100)]
        public string? Flavor { get; set; }

        [Column("BASE_PRICE")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Base price must be greater than 0")]
        public decimal BasePrice { get; set; }

        [Column("ADD_ON_DESCRIPTION")]
        [StringLength(200)]
        public string? AddOnDescription { get; set; }

        [Column("ADD_ON_PRICE_PER_UNIT")]
        public decimal? AddOnPricePerUnit { get; set; }

        [Column("IS_ACTIVE")]
        public bool IsActive { get; set; } = true;

        [Column("DISPLAY_ORDER")]
        public int DisplayOrder { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }
    }
}
