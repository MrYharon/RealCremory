using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cremory.API.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Creating = 1,
        Completed = 2,
        Cancelled = 3
    }

    [Table("ORDERS")]
    public class Order
    {
        [Key]
        [Column("ORDER_ID")]
        [StringLength(50)]
        public string OrderId { get; set; } = string.Empty;

        [Required]
        [Column("CUSTOMER_NAME")]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Column("ITEMS")]
        [StringLength(2000)]
        public string Items { get; set; } = string.Empty;

        [Column("TOTAL_PRICE")]
        public decimal TotalPrice { get; set; }

        [Column("STATUS")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required]
        [Column("SOURCE")]
        [StringLength(20)]
        public string Source { get; set; } = "Walk-in";

        [Column("CUSTOMER_CONTACT")]
        [StringLength(50)]
        public string? CustomerContact { get; set; }

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UPDATED_AT")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
