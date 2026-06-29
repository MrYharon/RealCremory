using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Cremory.API.Data;
using Cremory.API.Models;

namespace Cremory.API.Tests
{
    public class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private WebApplicationFactory<Program> CreateFactory()
        {
            var dbName = Guid.NewGuid().ToString();
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<CremoryDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<CremoryDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));
                });
            });
        }

        [Fact]
        public async Task GetOrders_ReturnsOkWithHeaders()
        {
            var client = CreateFactory().CreateClient();
            var response = await client.GetAsync("/api/orders?page=1&pageSize=10");

            response.EnsureSuccessStatusCode();
            Assert.True(response.Headers.Contains("X-Total-Count"));
            Assert.True(response.Headers.Contains("X-Total-Pages"));
        }

        [Fact]
        public async Task CreateOrder_WithValidData_ReturnsCreated()
        {
            var client = CreateFactory().CreateClient();
            var order = new
            {
                CustomerName = "Test Customer",
                Items = "1x Ube Cheesecake",
                TotalPrice = 450m,
                Source = "Walk-in"
            };

            var response = await client.PostAsJsonAsync("/api/orders", order);
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Expected Created but got {response.StatusCode}: {err}");
            }

            var body = await response.Content.ReadFromJsonAsync<Order>(JsonOptions);
            Assert.NotNull(body);
            Assert.Equal("Test Customer", body!.CustomerName);
            Assert.StartsWith("ORD-WLK", body.OrderId);
        }

        [Fact]
        public async Task CreateOrder_MissingCustomerName_ReturnsBadRequest()
        {
            var client = CreateFactory().CreateClient();
            var order = new
            {
                Items = "1x Ube Cheesecake",
                TotalPrice = 450m,
                Source = "Walk-in"
            };

            var response = await client.PostAsJsonAsync("/api/orders", order);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_ZeroTotalPrice_ReturnsBadRequest()
        {
            var client = CreateFactory().CreateClient();
            var order = new
            {
                CustomerName = "Test",
                Items = "1x Cake",
                TotalPrice = 0m,
                Source = "Walk-in"
            };

            var response = await client.PostAsJsonAsync("/api/orders", order);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderStatus_ValidTransition_ReturnsNoContent()
        {
            var client = CreateFactory().CreateClient();

            var create = await client.PostAsJsonAsync("/api/orders", new
            {
                CustomerName = "Status Test",
                Items = "1x Cake",
                TotalPrice = 300m,
                Source = "Walk-in"
            });
            var order = await create.Content.ReadFromJsonAsync<Order>(JsonOptions);

            var response = await client.PutAsJsonAsync($"/api/orders/{order!.OrderId}/status",
                new { Status = OrderStatus.Creating });

            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteOrder_ExistingOrder_ReturnsNoContent()
        {
            var client = CreateFactory().CreateClient();

            var create = await client.PostAsJsonAsync("/api/orders", new
            {
                CustomerName = "Delete Test",
                Items = "1x Cake",
                TotalPrice = 300m,
                Source = "Walk-in"
            });
            var order = await create.Content.ReadFromJsonAsync<Order>(JsonOptions);

            var response = await client.DeleteAsync($"/api/orders/{order!.OrderId}");
            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task GetOrders_WithStatusFilter_ReturnsFiltered()
        {
            var client = CreateFactory().CreateClient();

            await client.PostAsJsonAsync("/api/orders", new
            {
                CustomerName = "Pending Order",
                Items = "1x Cake",
                TotalPrice = 300m,
                Source = "Walk-in"
            });

            var all = await client.GetFromJsonAsync<List<Order>>("/api/orders?status=Pending", JsonOptions);
            Assert.NotNull(all);
            Assert.Contains(all!, o => o.CustomerName == "Pending Order");
        }
    }
}
