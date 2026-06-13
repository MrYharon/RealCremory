using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cremory.API.Models
{
    [Table("PRODUCTS")]
    public class Product
    {
        [Key]
        [Column("PRODUCT_ID")]
        public int ProductId { get; set; }

        [Required]
        [Column("NAME")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("DESCRIPTION")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Column("SELLING_PRICE")]
        public decimal SellingPrice { get; set; }
    }
}