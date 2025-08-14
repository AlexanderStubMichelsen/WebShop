using Microsoft.EntityFrameworkCore;
using Webshop.Api.Data;
using Stripe;
using WebshopProduct = Webshop.Api.Models.Product;

var builder = WebApplication.CreateBuilder(args);

// --- Stripe configuration (env var or appsettings) ---
var stripeKey = Environment.GetEnvironmentVariable("STRIPE__SecretKey")
               ?? builder.Configuration["Stripe:SecretKey"];
if (string.IsNullOrWhiteSpace(stripeKey))
{
    Console.WriteLine("❌ Stripe secret key is missing!");
}
else
{
    StripeConfiguration.ApiKey = stripeKey;
    Console.WriteLine("✅ Stripe secret key loaded.");
}

// --- Services ---
builder.Services.AddDbContext<WebshopDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// CORS: frontend (Next.js) + your production shop
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", p => p
        .WithOrigins("http://localhost:3000", "https://shop.devdisplay.online")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Register email service used by webhook/controller
builder.Services.AddScoped<OrderEmailService>();

var app = builder.Build();

// --- DB migrations on startup ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebshopDbContext>();
    db.Database.Migrate();
}

// --- Middleware ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AppCors");
app.MapControllers();


// Health
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
