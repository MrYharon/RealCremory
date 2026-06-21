using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cremory.API.Models
{
    [Table("CATEGORIES")]
    public class Category
    {
        [Key]
        [Column("CATEGORY_ID")]
        public int CategoryId { get; set; }

        [Required]
        [Column("NAME")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("DISPLAY_ORDER")]
        public int DisplayOrder { get; set; }
    }
}
