using System.Net.Http.Json;
using System.Text.Json;
using Cremory.App.Models;

namespace Cremory.App.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiService()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(AppConfig.ApiEndpoint),
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public async Task<List<Ingredient>> GetIngredientsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Ingredient>>("Ingredients", JsonOptions);
                return response ?? [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return [];
            }
        }

        public async Task<(bool Success, string? Error)> CreateIngredientAsync(Ingredient ingredient)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("Ingredients", ingredient, JsonOptions);
                if (response.IsSuccessStatusCode) return (true, null);
                var body = await response.Content.ReadAsStringAsync();
                return (false, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> UpdateIngredientAsync(Ingredient ingredient)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"Ingredients/{ingredient.IngredientId}", ingredient, JsonOptions);
                if (response.IsSuccessStatusCode) return (true, null);
                var body = await response.Content.ReadAsStringAsync();
                return (false, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return (false, ex.Message);
            }
        }

        public async Task<bool> DeleteIngredientAsync(int ingredientId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"Ingredients/{ingredientId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Category>>("Products/categories", JsonOptions);
                return response ?? [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return [];
            }
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Product>>("Products", JsonOptions);
                return response ?? [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return [];
            }
        }

        public async Task<List<MenuCategoryDto>> GetMenuAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<MenuCategoryDto>>("Products/menu", JsonOptions);
                return response ?? [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return [];
            }
        }

        public async Task<OrderDto?> CreateWalkInOrderAsync(string customerName, string items, decimal totalPrice, string? contact)
        {
            try
            {
                var payload = new
                {
                    CustomerName = customerName,
                    Items = items,
                    TotalPrice = totalPrice,
                    CustomerContact = contact,
                    Source = "Walk-in"
                };
                var response = await _httpClient.PostAsJsonAsync("Orders", payload, JsonOptions);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<OrderSummary>> GetOrdersAsync(
            string? status = null, string? search = null,
            DateTime? dateFrom = null, DateTime? dateTo = null,
            int page = 1, int pageSize = 100)
        {
            try
            {
                var url = BuildOrdersUrl(status, search, dateFrom, dateTo, page, pageSize);
                var dtos = await _httpClient.GetFromJsonAsync<List<OrderDto>>(url, JsonOptions);
                return dtos?.Select(OrderSummary.FromDto).ToList() ?? [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return [];
            }
        }

        public async Task<List<OrderDto>> GetOrdersRawAsync(
            string? status = null, string? search = null,
            DateTime? dateFrom = null, DateTime? dateTo = null,
            int page = 1, int pageSize = 100)
        {
            try
            {
                var url = BuildOrdersUrl(status, search, dateFrom, dateTo, page, pageSize);
                return await _httpClient.GetFromJsonAsync<List<OrderDto>>(url, JsonOptions) ?? [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return [];
            }
        }

        private static string BuildOrdersUrl(
            string? status, string? search,
            DateTime? dateFrom, DateTime? dateTo,
            int page, int pageSize)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(status)) parts.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrWhiteSpace(search)) parts.Add($"search={Uri.EscapeDataString(search)}");
            if (dateFrom.HasValue) parts.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
            if (dateTo.HasValue) parts.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
            parts.Add($"page={page}");
            parts.Add($"pageSize={pageSize}");

            return $"Orders?{string.Join("&", parts)}";
        }

        public async Task<bool> UpdateOrderStatusAsync(string orderId, OrderStatus status)
        {
            try
            {
                var payload = new { Status = status };
                var response = await _httpClient.PutAsJsonAsync($"Orders/{orderId}/status", payload, JsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return false;
            }
        }

        public async Task<FinanceSummaryDto?> GetFinanceSummaryAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<FinanceSummaryDto>("Analytics/finance", JsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return null;
            }
        }

        public async Task<DashboardAnalyticsDto?> GetDashboardAnalyticsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<DashboardAnalyticsDto>("Analytics/dashboard", JsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return null;
            }
        }

        public async Task<OrderDto?> PostOrderParseAsync(string rawText)
        {
            try
            {
                var payload = new { rawText };
                var response = await _httpClient.PostAsJsonAsync("Orders/parse", payload, JsonOptions);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return null;
            }
        }

        public async Task<(bool Success, string? Error)> CreateProductAsync(Product product)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("Products", product, JsonOptions);
                if (response.IsSuccessStatusCode)
                    return (true, null);

                var body = await response.Content.ReadAsStringAsync();
                return (false, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> UpdateProductAsync(Product product)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"Products/{product.ProductId}", product, JsonOptions);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    return (false, errorBody);
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return (false, ex.Message);
            }
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"Products/{productId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Recipe>> GetRecipesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<Recipe>>("Recipes", JsonOptions);
                return response ?? [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return [];
            }
        }

        public async Task<(bool Success, string? Error)> CreateRecipeAsync(Recipe recipe)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("Recipes", recipe, JsonOptions);
                if (response.IsSuccessStatusCode) return (true, null);
                var body = await response.Content.ReadAsStringAsync();
                return (false, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> UpdateRecipeAsync(Recipe recipe)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"Recipes/{recipe.RecipeId}", recipe, JsonOptions);
                if (response.IsSuccessStatusCode) return (true, null);
                var body = await response.Content.ReadAsStringAsync();
                return (false, body);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return (false, ex.Message);
            }
        }

        public async Task<bool> DeleteRecipeAsync(int recipeId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"Recipes/{recipeId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                return false;
            }
        }
    }
}