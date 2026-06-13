using Cremory.App.Models; // Change "API" to "App"

using System.Net.Http.Json;

namespace Cremory.App.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        // CRITICAL: Match this base address to your Swagger URL port!
        // For Windows Machine execution, localhost works perfectly.
        // Change it back to localhost for Windows execution
        private const string BaseUrl = "http://localhost:5105/api/";

        public ApiService()
        {
            // Simple handler configuration to bypass local SSL certificate complaints during dev testing
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