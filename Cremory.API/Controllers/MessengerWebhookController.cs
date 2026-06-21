using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using Cremory.API.Data;
using Cremory.API.Hubs;
using Cremory.API.Models;
using Cremory.API.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cremory.API.Controllers
{
    [ApiController]
    [Route("api/messenger")]
    public class MessengerWebhookController : ControllerBase
    {
        private readonly MessengerOptions _messenger;
        private readonly OrderParserService _orderParser;
        private readonly CremoryDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public MessengerWebhookController(
            IOptions<MessengerOptions> messenger,
            OrderParserService orderParser,
            CremoryDbContext context,
            IHubContext<OrderHub> hubContext)
        {
            _messenger = messenger.Value;
            _orderParser = orderParser;
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet("webhook")]
        public IActionResult Verify([FromQuery] string hub_mode, [FromQuery] string hub_verify_token, [FromQuery] string hub_challenge)
        {
            if (hub_mode == "subscribe" && hub_verify_token == _messenger.VerifyToken)
                return Ok(hub_challenge);

            return Forbid();
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Receive([FromBody] JsonElement body)
        {
            if (body.ValueKind == JsonValueKind.Undefined)
                return Ok();

            if (_messenger.AppSecret != string.Empty)
            {
                var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
                if (!IsValidSignature(body.GetRawText(), signature))
                    return Ok();
            }

            FacebookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<FacebookPayload>(body.GetRawText(), _jsonOptions);
            }
            catch
            {
                return Ok();
            }

            if (payload?.Entry == null)
                return Ok();

            foreach (var entry in payload.Entry)
            {
                if (entry.Messaging == null)
                    continue;

                foreach (var msg in entry.Messaging)
                {
                    var text = msg.Message?.Text;
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    var result = _orderParser.TryParse(text);
                    if (!result.IsSuccess || result.Order == null)
                        continue;

                    var order = result.Order;
                    order.OrderId = GenerateOrderId(order.Source);

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.All.SendAsync("OrderCreated", order);
                }
            }

            return Ok();
        }

        private static string GenerateOrderId(string source)
        {
            var prefix = source switch
            {
                "Facebook" => "ORD-FB",
                "Walk-in" => "ORD-WLK",
                _ => "ORD"
            };
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"{prefix}-{timestamp}";
        }

        private bool IsValidSignature(string rawBody, string? signatureHeader)
        {
            if (string.IsNullOrEmpty(signatureHeader))
                return false;

            var prefix = "sha256=";
            if (!signatureHeader.StartsWith(prefix))
                return false;

            var expectedSignature = signatureHeader[prefix.Length..];
            var key = Encoding.UTF8.GetBytes(_messenger.AppSecret);
            var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(bodyBytes);
            var actualSignature = Convert.ToHexString(hash).ToLowerInvariant();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(actualSignature),
                Encoding.UTF8.GetBytes(expectedSignature));
        }
    }

    public class FacebookPayload
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("entry")]
        public List<FacebookEntry>? Entry { get; set; }
    }

    public class FacebookEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("messaging")]
        public List<FacebookMessaging>? Messaging { get; set; }
    }

    public class FacebookMessaging
    {
        [JsonPropertyName("sender")]
        public FacebookSender? Sender { get; set; }

        [JsonPropertyName("recipient")]
        public FacebookRecipient? Recipient { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("message")]
        public FacebookMessage? Message { get; set; }
    }

    public class FacebookSender
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class FacebookRecipient
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class FacebookMessage
    {
        [JsonPropertyName("mid")]
        public string? Mid { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("is_echo")]
        public bool IsEcho { get; set; }
    }
}
