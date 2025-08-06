using Microsoft.EntityFrameworkCore;
using Webshop.Api.Data;
using Webshop.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Register PostgreSQL
builder.Services.AddDbContext<WebshopDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Enable Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebshopDbContext>();
    db.Database.EnsureCreated(); // Optional: runs only if DB doesn’t exist
    DbInitializer.Seed(db);
}


// Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// GET all products
app.MapGet("/api/products", async (WebshopDbContext db) =>
    await db.Products.ToListAsync()
);

// POST a product
app.MapPost("/api/products", async (Product product, WebshopDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/api/products/{product.Id}", product);
});

app.UseCors(policy =>
    policy.WithOrigins("http://localhost:3000")
          .AllowAnyMethod()
          .AllowAnyHeader()
);


app.Run();
