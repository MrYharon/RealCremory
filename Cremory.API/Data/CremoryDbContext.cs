using Microsoft.EntityFrameworkCore;
using Cremory.API.Models;

namespace Cremory.API.Data
{
    public class CremoryDbContext : DbContext
    {
        public CremoryDbContext(DbContextOptions<CremoryDbContext> options) : base(options)
        {
        }

        
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Ingredient>().ToTable("INGREDIENTS");
            modelBuilder.Entity<Product>().ToTable("PRODUCTS");
            modelBuilder.Entity<User>().ToTable("USERS");
        }
    }
}