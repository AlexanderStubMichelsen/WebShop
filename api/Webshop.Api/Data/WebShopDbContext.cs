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

        public DbSet<Product> Products { get; set; } = default!;
        public DbSet<EmailLog> EmailLogs { get; set; } = default!;
        public DbSet<Order> Orders { get; set; } = default!;          // ✅ Add this
        public DbSet<OrderItem> OrderItems { get; set; } = default!;  // ✅ Add this

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique index for idempotency
            modelBuilder.Entity<EmailLog>()
                .HasIndex(e => e.SessionId)
                .IsUnique();

            // Your existing seed data...
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Eco-Friendly Backpack", Description = "Durable and stylish, made from recycled materials.", Price = 49.99M, ImageUrl = "https://picsum.photos/300?random=1" },
                new Product { Id = 2, Name = "Noise Cancelling Headphones", Description = "High-quality sound and comfort for work or play.", Price = 129.99M, ImageUrl = "https://picsum.photos/300?random=2" },
                new Product { Id = 3, Name = "Smart LED Lamp", Description = "Touch control and adjustable lighting modes.", Price = 24.99M, ImageUrl = "https://picsum.photos/300?random=3" },
                new Product { Id = 4, Name = "Minimalist Wristwatch", Description = "Sleek and simple design with a leather strap.", Price = 89.99M, ImageUrl = "https://picsum.photos/300?random=4" },
                new Product { Id = 5, Name = "Wireless Bluetooth Speaker", Description = "Portable speaker with crystal clear sound and 12-hour battery life.", Price = 79.99M, ImageUrl = "https://picsum.photos/300?random=5" },
                new Product { Id = 6, Name = "Organic Cotton T-Shirt", Description = "Soft, breathable, and sustainably made.", Price = 19.99M, ImageUrl = "https://picsum.photos/300?random=6" },
                new Product { Id = 7, Name = "Stainless Steel Water Bottle", Description = "Keep drinks cold for 24 hours or hot for 12 hours.", Price = 34.99M, ImageUrl = "https://picsum.photos/300?random=7" },
                new Product { Id = 8, Name = "Ergonomic Office Chair", Description = "Lumbar support and adjustable height for all-day comfort.", Price = 199.99M, ImageUrl = "https://picsum.photos/300?random=8" },
                new Product { Id = 9, Name = "Smartphone Stand", Description = "Adjustable aluminum stand compatible with all phone sizes.", Price = 14.99M, ImageUrl = "https://picsum.photos/300?random=9" },
                new Product { Id = 10, Name = "Cozy Throw Blanket", Description = "Ultra-soft fleece blanket perfect for movie nights.", Price = 39.99M, ImageUrl = "https://picsum.photos/300?random=10" },
                new Product { Id = 11, Name = "Bamboo Cutting Board Set", Description = "Eco-friendly kitchen essential with 3 different sizes.", Price = 29.99M, ImageUrl = "https://picsum.photos/300?random=11" },
                new Product { Id = 12, Name = "Fitness Resistance Bands", Description = "Complete set of 5 bands for full-body workouts at home.", Price = 24.99M, ImageUrl = "https://picsum.photos/300?random=12" }
            );

            // Configure Order relationships
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasIndex(e => e.SessionId).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasOne(oi => oi.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
