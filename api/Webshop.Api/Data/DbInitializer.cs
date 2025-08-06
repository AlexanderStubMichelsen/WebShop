// Data/DbInitializer.cs
using Webshop.Api.Models;
using Webshop.Api.Data; // ✅ Add this

namespace Webshop.Api.Data;

public static class DbInitializer
{
    public static void Seed(WebshopDbContext context)
    {
        if (context.Products.Any()) return; // Already seeded

        var products = new[]
        {
            new Product { Name = "Eco-Friendly Backpack", Description = "Durable and stylish, made from recycled materials.", Price = 49.99M, ImageUrl = "https://picsum.photos/300?random=1" },
            new Product { Name = "Noise Cancelling Headphones", Description = "High-quality sound and comfort for work or play.", Price = 129.99M, ImageUrl = "https://picsum.photos/300?random=1" },
            new Product { Name = "Smart LED Lamp", Description = "Touch control and adjustable lighting modes.", Price = 24.99M, ImageUrl = "https://picsum.photos/300?random=1" },
            new Product { Name = "Minimalist Wristwatch", Description = "Sleek and simple design with a leather strap.", Price = 89.99M, ImageUrl = "https://picsum.photos/300?random=1" }
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }
}
