namespace Cremory.API.Models
{
    public class OrderParserOptions
    {
        public const string SectionName = "OrderParser";

        public string TriggerPattern { get; set; } = @"^!ORDER\s*SUMMARY:?\s*(.*)";
        public string CustomerPattern { get; set; } = @"Customer:\s*(.+)";
        public string ItemsPattern { get; set; } = @"Items?:\s*(.+)";
        public string TotalPattern { get; set; } = @"Total:\s*(.+?)(?:\s*\(.*\))?$";
        public string ContactPattern { get; set; } = @"Contact:\s*(.+)";
        public string SourcePattern { get; set; } = @"Source:\s*(.+)";
    }
}
