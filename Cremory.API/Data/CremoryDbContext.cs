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
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<DeviceToken> DeviceTokens { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().ToTable("CATEGORIES");
            modelBuilder.Entity<Product>().ToTable("PRODUCTS");
            modelBuilder.Entity<Order>().ToTable("ORDERS");
            modelBuilder.Entity<DeviceToken>().ToTable("DEVICE_TOKENS");
            modelBuilder.Entity<AppSetting>().ToTable("APP_SETTINGS");

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
        }
    }
}