using Microsoft.EntityFrameworkCore;
using Webshop.Api.Data;
using Webshop.Api.Models;
using Stripe;
using WebshopProduct = Webshop.Api.Models.Product; // ✅ Alias to avoid conflict with Stripe.Product

var builder = WebApplication.CreateBuilder(args);

// Stripe configuration
var stripeKey = builder.Configuration["Stripe:SecretKey"];
if (string.IsNullOrEmpty(stripeKey))
{
    Console.WriteLine("❌ Stripe secret key is missing!");
}
else
{
    Console.WriteLine("✅ Stripe secret key loaded.");
    StripeConfiguration.ApiKey = stripeKey;
}

// Register SQLite
builder.Services.AddDbContext<WebshopDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger + CORS
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddControllers(); // MUST be before `builder.Build()`


var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebshopDbContext>();
    db.Database.EnsureCreated(); // Create DB if it doesn't exist
    DbInitializer.Seed(db);     // Seed with sample data
}

// Swagger UI for development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers(); // Enables [ApiController]-based routing


// Enable CORS for frontend
app.UseCors(policy =>
    policy.WithOrigins("http://localhost:3000")
          .AllowAnyMethod()
          .AllowAnyHeader()
);

// --- API Endpoints ---

// GET all products
app.MapGet("/api/products", async (WebshopDbContext db) =>
    await db.Products.ToListAsync()
);

// POST a new product
app.MapPost("/api/products", async (WebshopProduct product, WebshopDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
});

app.Run();
