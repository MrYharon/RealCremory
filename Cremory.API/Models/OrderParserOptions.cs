namespace Cremory.API.Models
{
    public class OrderParserOptions
    {
        public const string SectionName = "OrderParser";

        public string TriggerPattern { get; set; } = @"^!ORDER\s*SUMMARY:\s*(.+)";
        public string TotalPattern { get; set; } = @"Total:\s*(.+)";
        public string ContactPattern { get; set; } = @"Contact:\s*(.+)";
        public string DeliveryPattern { get; set; } = @"Delivery:\s*(.+)";
        public string AddressPattern { get; set; } = @"Address:\s*(.+)";
        public string PaymentPattern { get; set; } = @"Payment:\s*(.+)";
    }
}
