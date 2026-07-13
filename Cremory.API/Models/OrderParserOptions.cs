namespace Cremory.API.Models
{
    public class OrderParserOptions
    {
        public const string SectionName = "OrderParser";

        public string TriggerPattern { get; set; } = @"^!ORDER\s*SUMMARY:\s*(.+)";
        public string TotalPattern { get; set; } = @"Total:\s*(.+)";
        public string ContactPattern { get; set; } = @"Contact:\s*(.+)";
    }
}
