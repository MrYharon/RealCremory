using System.Text;
using System.Text.Json;

namespace Cremory.App.Services
{
    public static class FcmRegistrationService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string? CurrentToken { get; set; }
        private static bool _registered;

        public static async Task RegisterTokenAsync(string token)
        {
            if (_registered || token == CurrentToken) return;

            try
            {
                var payload = new { Token = token, Platform = "Android" };
                var json = JsonSerializer.Serialize(payload, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await App.ApiService!.HttpClient.PostAsync(
                    $"{AppConfig.ApiUrl}/api/DeviceTokens", content);

                if (response.IsSuccessStatusCode)
                {
                    _registered = true;
                    CurrentToken = token;
                    System.Diagnostics.Debug.WriteLine("FCM token registered successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FCM token registration failed: {ex.Message}");
            }
        }

        public static void ResetRegistration()
        {
            _registered = false;
        }
    }
}
