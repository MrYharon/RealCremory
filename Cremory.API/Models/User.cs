namespace Cremory.API.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = "staff";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
