using System.Net.Http.Json;
using System.Text.Json;
using Cremory.App.Models;

namespace Cremory.App.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        public HttpClient HttpClient => _httpClient;

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

        private static bool IsConnected =>
            Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        private async Task<T?> HttpGetAsync<T>(string url, string cacheKey) where T : class
        {
            if (!IsConnected)
                return LoadFromCache<T>(cacheKey);

            try
            {
                var result = await _httpClient.GetFromJsonAsync<T>(url, JsonOptions);
                if (result != null)
                    SaveToCache(cacheKey, result);
                return result;
            }
            catch (HttpRequestException)
            {
                await Task.Delay(1000);
                try
                {
                    var retry = await _httpClient.GetFromJsonAsync<T>(url, JsonOptions);
                    if (retry != null)
                        SaveToCache(cacheKey, retry);
                    return retry;
                }
                catch
                {
                    return LoadFromCache<T>(cacheKey);
                }
            }
            catch (TaskCanceledException)
            {
                return LoadFromCache<T>(cacheKey);
            }
        }

        private static void SaveToCache<T>(string key, T data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, JsonOptions);
                Preferences.Set($"api_cache_{key}", json);
            }
            catch { }
        }

        private static T? LoadFromCache<T>(string key) where T : class
        {
            try
            {
                var json = Preferences.Get($"api_cache_{key}", "");
                if (string.IsNullOrEmpty(json)) return null;
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Ingredient>> GetIngredientsAsync()
        {
            return await HttpGetAsync<List<Ingredient>>("Ingredients", "ingredients") ?? [];
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
            return await HttpGetAsync<List<Category>>("Products/categories", "categories") ?? [];
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            return await HttpGetAsync<List<Product>>("Products", "products") ?? [];
        }

        public async Task<List<MenuCategoryDto>> GetMenuAsync()
        {
            return await HttpGetAsync<List<MenuCategoryDto>>("Products/menu", "menu") ?? [];
        }

        public async Task<List<ProductStockDto>> GetProductStockAsync()
        {
            return await HttpGetAsync<List<ProductStockDto>>("Products/stock", "stock") ?? [];
        }

        public async Task<bool> UpdateProductStockAsync(int productId, int newStock)
        {
            try
            {
                var payload = new { newStock };
                var response = await _httpClient.PutAsJsonAsync($"Products/{productId}/stock", payload, JsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> BatchUpdateStockAsync(List<ProductStockUpdate> updates)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("Products/stock/batch", updates, JsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
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
            var url = BuildOrdersUrl(status, search, dateFrom, dateTo, page, pageSize);
            var dtos = await HttpGetAsync<List<OrderDto>>(url, "orders");
            return dtos?.Select(OrderSummary.FromDto).ToList() ?? [];
        }

        public async Task<(List<OrderDto> Orders, int TotalCount)> GetOrdersPagedAsync(
            string? status = null, string? search = null,
            DateTime? dateFrom = null, DateTime? dateTo = null,
            int page = 1, int pageSize = 100)
        {
            var url = BuildOrdersUrl(status, search, dateFrom, dateTo, page, pageSize);
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return ([], 0);

                var json = await response.Content.ReadAsStringAsync();
                var orders = JsonSerializer.Deserialize<List<OrderDto>>(json, JsonOptions) ?? [];

                var totalCount = 0;
                if (response.Headers.TryGetValues("X-Total-Count", out var values) &&
                    int.TryParse(values.FirstOrDefault(), out var count))
                    totalCount = count;

                return (orders, totalCount);
            }
            catch
            {
                return ([], 0);
            }
        }

        public async Task<List<OrderDto>> GetOrdersRawAsync(
            string? status = null, string? search = null,
            DateTime? dateFrom = null, DateTime? dateTo = null,
            int page = 1, int pageSize = 100)
        {
            var url = BuildOrdersUrl(status, search, dateFrom, dateTo, page, pageSize);
            return await HttpGetAsync<List<OrderDto>>(url, "orders") ?? [];
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

        public async Task<(bool Success, string? Error)> UpdateOrderAsync(OrderDto order)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"Orders/{order.OrderId}", order, JsonOptions);
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

        public async Task<(bool Success, string? Error)> DeleteOrderAsync(string orderId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"Orders/{orderId}");
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

        public async Task<FinanceSummaryDto?> GetFinanceSummaryAsync()
        {
            return await HttpGetAsync<FinanceSummaryDto>("Analytics/finance", "finance");
        }

        public async Task<DashboardAnalyticsDto?> GetDashboardAnalyticsAsync()
        {
            return await HttpGetAsync<DashboardAnalyticsDto>("Analytics/dashboard", "dashboard");
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
            return await HttpGetAsync<List<Recipe>>("Recipes", "recipes") ?? [];
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
