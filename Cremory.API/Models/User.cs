using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cremory.API.Models
{
    [Table("USERS")]
    public class User
    {
        [Key]
        [Column("USER_ID")]
        public int UserId { get; set; }

        [Required]
        [Column("NAME")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("EMAIL")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Column("CONTACT_NUMBER")]
        [StringLength(20)]
        public string? ContactNumber { get; set; }

        [Required]
        [Column("ACCOUNT_TYPE")]
        [StringLength(20)]
        public string AccountType { get; set; } = "Staff";
    }
}