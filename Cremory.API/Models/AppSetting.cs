using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cremory.API.Models
{
    [Table("APP_SETTINGS")]
    public class AppSetting
    {
        [Key]
        [Column("SETTING_KEY")]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Column("SETTING_VALUE")]
        public string Value { get; set; } = string.Empty;
    }
}
