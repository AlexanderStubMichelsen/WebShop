// Data/WebshopDbContext.cs
using Microsoft.EntityFrameworkCore;
using Webshop.Api.Models;

namespace Webshop.Api.Data
{
    public class WebshopDbContext : DbContext
    {
        public WebshopDbContext(DbContextOptions<WebshopDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Coffee Mug",
                    Description = "Ceramic mug for hot drinks",
                    Price = 49.95m,
                    ImageUrl = "https://fastly.picsum.photos/id/866/200/200.jpg?hmac=i0ngmQOk9dRZEzhEosP31m_vQnKBQ9C19TBP1CGoIUA"
                },
                new Product
                {
                    Id = 2,
                    Name = "T-Shirt",
                    Description = "100% cotton, unisex",
                    Price = 99.00m,
                    ImageUrl = "https://fastly.picsum.photos/id/866/200/200.jpg?hmac=i0ngmQOk9dRZEzhEosP31m_vQnKBQ9C19TBP1CGoIUA"
                },
                new Product
                {
                    Id = 3,
                    Name = "Notebook",
                    Description = "A5 notebook with grid paper",
                    Price = 39.50m,
                    ImageUrl = "https://fastly.picsum.photos/id/866/200/200.jpg?hmac=i0ngmQOk9dRZEzhEosP31m_vQnKBQ9C19TBP1CGoIUA"
                }
            );
        }
    }
}
