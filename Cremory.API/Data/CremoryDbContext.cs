using Microsoft.EntityFrameworkCore;
using Cremory.API.Models;

namespace Cremory.API.Data
{
    public class CremoryDbContext : DbContext
    {
        public CremoryDbContext(DbContextOptions<CremoryDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<DeviceToken> DeviceTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().ToTable("CATEGORIES");
            modelBuilder.Entity<Ingredient>().ToTable("INGREDIENTS");
            modelBuilder.Entity<Product>().ToTable("PRODUCTS");
            modelBuilder.Entity<User>().ToTable("USERS");
            modelBuilder.Entity<Recipe>().ToTable("RECIPES");
            modelBuilder.Entity<RecipeIngredient>().ToTable("RECIPE_INGREDIENTS");
            modelBuilder.Entity<Order>().ToTable("ORDERS");
            modelBuilder.Entity<DeviceToken>().ToTable("DEVICE_TOKENS");

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasOne(p => p.Category)
                    .WithMany()
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(o => o.Status).HasConversion<int>();
            });

            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Recipe)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(ri => ri.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Ingredient)
                .WithMany()
                .HasForeignKey(ri => ri.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}