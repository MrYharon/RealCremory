using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Cremory.API.Models;

namespace Cremory.API.Services
{
    public class OrderParserService
    {
        private readonly Regex _orderTrigger;
        private readonly Regex _totalRegex;
        private readonly Regex _contactRegex;
        private readonly Regex _deliveryRegex;
        private readonly Regex _addressRegex;
        private readonly Regex _paymentRegex;

        public OrderParserService(IOptions<OrderParserOptions> options)
        {
            var cfg = options.Value;
            _orderTrigger = new Regex(cfg.TriggerPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _totalRegex = new Regex(cfg.TotalPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _contactRegex = new Regex(cfg.ContactPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _deliveryRegex = new Regex(cfg.DeliveryPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _addressRegex = new Regex(cfg.AddressPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _paymentRegex = new Regex(cfg.PaymentPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public ParseResult TryParse(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
                return ParseResult.Failed("Empty message.");

            var lines = rawMessage.Split('\n', StringSplitOptions.None);
            if (lines.Length == 0)
                return ParseResult.Failed("Empty message.");

            var triggerMatch = _orderTrigger.Match(lines[0].Trim());
            if (!triggerMatch.Success)
                return ParseResult.Failed("No !ORDER SUMMARY trigger found.");

            var customerName = triggerMatch.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(customerName))
                return ParseResult.Failed("Customer name is required.");

            string totalStr = null!;
            string? contact = null;
            string? deliveryType = null;
            string? address = null;
            string? paymentStatus = null;
            var itemLines = new List<string>();

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();

                var totalMatch = _totalRegex.Match(trimmed);
                if (totalMatch.Success)
                {
                    totalStr = totalMatch.Groups[1].Value.Trim();
                    continue;
                }

                var contactMatch = _contactRegex.Match(trimmed);
                if (contactMatch.Success)
                {
                    contact = contactMatch.Groups[1].Value.Trim();
                    continue;
                }

                var deliveryMatch = _deliveryRegex.Match(trimmed);
                if (deliveryMatch.Success)
                {
                    var val = deliveryMatch.Groups[1].Value.Trim().ToLower();
                    if (val.Contains("pick") || val.Contains("pickup"))
                        deliveryType = "Pick Up";
                    else
                        deliveryType = "Delivery";
                    continue;
                }

                var addressMatch = _addressRegex.Match(trimmed);
                if (addressMatch.Success)
                {
                    if (deliveryType == "Pick Up")
                        continue;
                    address = addressMatch.Groups[1].Value.Trim();
                    deliveryType ??= "Delivery";
                    continue;
                }

                var paymentMatch = _paymentRegex.Match(trimmed);
                if (paymentMatch.Success)
                {
                    paymentStatus = paymentMatch.Groups[1].Value.Trim();
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(line))
                    itemLines.Add(line.TrimEnd());
            }

            var items = string.Join("\n", itemLines).Trim();
            if (string.IsNullOrWhiteSpace(items))
                return ParseResult.Failed("No items specified.");

            var total = 0m;
            if (totalStr != null)
            {
                totalStr = Regex.Replace(totalStr, @"[^\d.,]", "");
                decimal.TryParse(totalStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out total);
            }

            var order = new Order
            {
                CustomerName = customerName,
                Items = items,
                TotalPrice = total,
                Source = "Facebook",
                CustomerContact = contact,
                DeliveryType = deliveryType,
                Address = address,
                PaymentStatus = paymentStatus,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return ParseResult.Success(order);
        }
    }

    public class ParseResult
    {
        public bool IsSuccess { get; private set; }
        public string? Error { get; private set; }
        public Order? Order { get; private set; }

        public static ParseResult Success(Order order) => new()
        {
            IsSuccess = true,
            Order = order
        };

        public static ParseResult Failed(string error) => new()
        {
            IsSuccess = false,
            Error = error
        };
    }
}
