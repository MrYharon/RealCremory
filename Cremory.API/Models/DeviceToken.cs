using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cremory.API.Models
{
    [Table("DEVICE_TOKENS")]
    public class DeviceToken
    {
        [Key]
        [Column("TOKEN_ID")]
        public int TokenId { get; set; }

        [Required]
        [Column("TOKEN")]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;

        [Column("PLATFORM")]
        [MaxLength(20)]
        public string Platform { get; set; } = "Android";

        [Column("CREATED_AT")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("LAST_USED_AT")]
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    }
}
