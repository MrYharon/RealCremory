using Microsoft.EntityFrameworkCore;
using Cremory.API.Models;

namespace Cremory.API.Data
{
    public static class DbInitializer
    {
        public static void Seed(CremoryDbContext context)
        {
            try
            {
                context.Database.ExecuteSqlRaw("DELETE FROM \"ORDERS\"");
                context.Database.ExecuteSqlRaw("DELETE FROM \"PRODUCTS\"");
                context.Database.ExecuteSqlRaw("DELETE FROM \"CATEGORIES\"");
                context.Database.ExecuteSqlRaw("DELETE FROM \"APP_SETTINGS\"");
                context.Database.ExecuteSqlRaw("DELETE FROM \"USERS\"");
            }
            catch { }

            var basque = new Category { Name = "Basque Burnt Cheesecake", DisplayOrder = 1 };
            var bread = new Category { Name = "Korean Cream Cheese Bread", DisplayOrder = 2 };
            context.Categories.AddRange(basque, bread);
            context.SaveChanges();

            var products = new List<Product>
            {
                // Basque Burnt Cheesecake - Solo
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "Solo", Flavor = "Classic Cheesecake", BasePrice = 35, Unit = "piece", CurrentStock = 50, LowStockThreshold = 10, DisplayOrder = 1, AutoDeduct = true },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "Solo", Flavor = "Biscoff", BasePrice = 40, Unit = "piece", CurrentStock = 50, LowStockThreshold = 10, DisplayOrder = 2, AutoDeduct = true },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "Solo", Flavor = "Chocolate", BasePrice = 40, Unit = "piece", CurrentStock = 50, LowStockThreshold = 10, DisplayOrder = 3, AutoDeduct = true },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "Solo", Flavor = "Cookies and Cream", BasePrice = 40, Unit = "piece", CurrentStock = 50, LowStockThreshold = 10, DisplayOrder = 4, AutoDeduct = true },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "Solo", Flavor = "Blueberry Cream", BasePrice = 40, Unit = "piece", CurrentStock = 50, LowStockThreshold = 10, DisplayOrder = 5, AutoDeduct = true },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "Solo", Flavor = "Cookie Biscoff", BasePrice = 45, Unit = "piece", CurrentStock = 50, LowStockThreshold = 10, DisplayOrder = 6, AutoDeduct = true },

                // Basque Burnt Cheesecake - Box of 4
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "Box of 4", Flavor = "All Classic", BasePrice = 135, Unit = "box", CurrentStock = 30, LowStockThreshold = 5, DisplayOrder = 7, AutoDeduct = false },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "Box of 4", Flavor = "Mix of Flavors", BasePrice = 150, AddOnDescription = "Choose any from the available flavors. Add P5 per piece on every Cookie Biscoff", AddOnPricePerUnit = 5, Unit = "box", CurrentStock = 30, LowStockThreshold = 5, DisplayOrder = 8, AutoDeduct = false },

                // Basque Burnt Cheesecake - 6 Inch Round
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "6 Inch Round", Flavor = "Classic Cheesecake", BasePrice = 460, Unit = "cake", CurrentStock = 10, LowStockThreshold = 2, DisplayOrder = 9, AutoDeduct = true },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "6 Inch Round", Flavor = "Biscoff", BasePrice = 575, Unit = "cake", CurrentStock = 10, LowStockThreshold = 2, DisplayOrder = 10, AutoDeduct = true },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "6 Inch Round", Flavor = "Chocolate", BasePrice = 575, Unit = "cake", CurrentStock = 10, LowStockThreshold = 2, DisplayOrder = 11, AutoDeduct = true },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "6 Inch Round", Flavor = "Cookies and Cream", BasePrice = 575, Unit = "cake", CurrentStock = 10, LowStockThreshold = 2, DisplayOrder = 12, AutoDeduct = true },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "6 Inch Round", Flavor = "Blueberry Cream", BasePrice = 575, Unit = "cake", CurrentStock = 10, LowStockThreshold = 2, DisplayOrder = 13, AutoDeduct = true },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "6 Inch Round", Flavor = "Cookie Biscoff", BasePrice = 600, Unit = "cake", CurrentStock = 10, LowStockThreshold = 2, DisplayOrder = 14, AutoDeduct = true },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "6 Inch Round", Flavor = "Blueberry and Biscoff", BasePrice = 575, Unit = "cake", CurrentStock = 10, LowStockThreshold = 2, DisplayOrder = 15, AutoDeduct = true },
                new() { CategoryId = basque.CategoryId, Name = "Basque Burnt Cheesecake", Variant = "6 Inch Round", Flavor = "Cookies & Cream and Cookie Biscoff", BasePrice = 585, Unit = "cake", CurrentStock = 10, LowStockThreshold = 2, DisplayOrder = 16, AutoDeduct = true },

                // Korean Cream Cheese Bread - Solo
                new() { CategoryId = bread.CategoryId, Name = "Korean Cream Cheese Bread", Variant = "Solo", Flavor = "Garlic Original", BasePrice = 38, Unit = "piece", CurrentStock = 40, LowStockThreshold = 10, DisplayOrder = 1, AutoDeduct = true },
                new() { CategoryId = bread.CategoryId, Name = "Korean Cream Cheese Bread", Variant = "Solo", Flavor = "Garlic Pork Floss", BasePrice = 38, Unit = "piece", CurrentStock = 40, LowStockThreshold = 10, DisplayOrder = 2, AutoDeduct = true },
                new() { CategoryId = bread.CategoryId, Name = "Korean Cream Cheese Bread", Variant = "Solo", Flavor = "Cheesy Bacon", BasePrice = 45, Unit = "piece", CurrentStock = 40, LowStockThreshold = 10, DisplayOrder = 3, AutoDeduct = true },
                new() { CategoryId = bread.CategoryId, Name = "Korean Cream Cheese Bread", Variant = "Solo", Flavor = "Choco Dream", BasePrice = 38, Unit = "piece", CurrentStock = 40, LowStockThreshold = 10, DisplayOrder = 4, AutoDeduct = true },

                // Korean Cream Cheese Bread - Box of 2
                new() { CategoryId = bread.CategoryId, Name = "Korean Cream Cheese Bread", Variant = "Box of 2", Flavor = "Mix of Flavors", BasePrice = 75, AddOnDescription = "Add P5 per piece on every Cheesy Bacon", AddOnPricePerUnit = 5, Unit = "box", CurrentStock = 30, LowStockThreshold = 5, DisplayOrder = 5, AutoDeduct = false },
            };

            context.Products.AddRange(products);
            context.SaveChanges();

            if (!context.Users.Any())
            {
                context.Users.Add(new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    DisplayName = "Admin",
                    Role = "admin",
                    CreatedAt = DateTime.UtcNow
                });
                context.SaveChanges();
            }
        }
    }
}
