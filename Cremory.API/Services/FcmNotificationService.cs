using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Cremory.API.Data;
using Cremory.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Cremory.API.Services
{
    public class FcmNotificationService
    {
        private readonly CremoryDbContext _context;
        private readonly ILogger<FcmNotificationService> _logger;

        public FcmNotificationService(CremoryDbContext context, ILogger<FcmNotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public static void InitializeFirebase(IWebHostEnvironment env)
        {
            if (FirebaseApp.DefaultInstance != null)
                return;

            var json = Environment.GetEnvironmentVariable("FCM_SERVICE_ACCOUNT_JSON");
            if (!string.IsNullOrEmpty(json))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(json)
                });
                return;
            }

            var filePath = Path.Combine(env.ContentRootPath, "fcm-service-account.json");
            if (File.Exists(filePath))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(filePath)
                });
                return;
            }

            FirebaseApp.Create();
        }

        public async Task SendOrderNotification(Order order)
        {
            try
            {
                var tokens = await _context.DeviceTokens
                    .Select(t => t.Token)
                    .ToListAsync();

                if (tokens.Count == 0)
                    return;

                var total = order.TotalPrice.ToString("N0");
                var source = order.Source == "Facebook" ? "Messenger" : order.Source;

                var message = new MulticastMessage
                {
                    Tokens = tokens,
                    Data = new Dictionary<string, string>
                    {
                        ["title"] = $"New Order from {source}",
                        ["body"] = $"{order.CustomerName} - {order.Items} - P{total}",
                        ["orderId"] = order.OrderId,
                        ["type"] = "order_created"
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                _logger.LogInformation("FCM sent to {Count} devices, success: {Success}, failure: {Failure}",
                    tokens.Count, response.SuccessCount, response.FailureCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send FCM notification");
            }
        }
    }
}
