using Cremory.App.Models;
using System.Net.Http.Json;

namespace Cremory.App.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        // Dynamically switch the URL based on the device running the app
        private static string BaseUrl = DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5105/api/"
            : "http://localhost:5105/api/";

        public ApiService()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl)
            };
        }

        public async Task<List<Ingredient>> GetIngredientsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Ingredient>>("Ingredients");
                return response ?? new List<Ingredient>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return new List<Ingredient>();
            }
        }
    }
}