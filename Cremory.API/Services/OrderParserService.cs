using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Cremory.API.Models;

namespace Cremory.API.Services
{
    public class OrderParserService
    {
        private readonly Regex _orderTrigger;
        private readonly Regex _customerRegex;
        private readonly Regex _itemsRegex;
        private readonly Regex _totalRegex;
        private readonly Regex _contactRegex;
        private readonly Regex _sourceRegex;

        public OrderParserService(IOptions<OrderParserOptions> options)
        {
            var cfg = options.Value;
            _orderTrigger = new Regex(cfg.TriggerPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
            _customerRegex = new Regex(cfg.CustomerPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _itemsRegex = new Regex(cfg.ItemsPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _totalRegex = new Regex(cfg.TotalPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _contactRegex = new Regex(cfg.ContactPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            _sourceRegex = new Regex(cfg.SourcePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public ParseResult TryParse(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
                return ParseResult.Failed("Empty message.");

            var match = _orderTrigger.Match(rawMessage.Trim());
            if (!match.Success)
                return ParseResult.Failed("No !ORDER SUMMARY trigger found.");

            var body = match.Groups[1].Value;

            if (string.IsNullOrWhiteSpace(body))
                body = rawMessage;

            var customer = ExtractValue(body, _customerRegex) ?? "Walk-in Customer";
            var items = ExtractValue(body, _itemsRegex) ?? "No items specified";
            var contact = ExtractValue(body, _contactRegex);
            var source = ExtractValue(body, _sourceRegex) ?? "Facebook";

            var total = 0m;
            var totalStr = ExtractValue(body, _totalRegex);
            if (totalStr != null)
            {
                totalStr = Regex.Replace(totalStr, @"[^\d.,]", "");
                decimal.TryParse(totalStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out total);
            }

            var order = new Order
            {
                CustomerName = customer.Trim(),
                Items = items.Trim(),
                TotalPrice = total,
                Source = source.Trim(),
                CustomerContact = contact?.Trim(),
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return ParseResult.Success(order);
        }

        private static string? ExtractValue(string text, Regex regex)
        {
            var match = regex.Match(text);
            return match.Success ? match.Groups[1].Value.Trim() : null;
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
