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

        public OrderParserService(IOptions<OrderParserOptions> options)
        {
            var cfg = options.Value;
            _orderTrigger = new Regex(cfg.TriggerPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _totalRegex = new Regex(cfg.TotalPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _contactRegex = new Regex(cfg.ContactPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
