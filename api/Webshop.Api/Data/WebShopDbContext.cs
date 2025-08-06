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
    }
}
