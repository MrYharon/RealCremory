namespace Cremory.API.Models
{
    public class MessengerOptions
    {
        public const string SectionName = "Messenger";

        public string VerifyToken { get; set; } = string.Empty;
        public string PageAccessToken { get; set; } = string.Empty;
        public string AppSecret { get; set; } = string.Empty;
    }
}

