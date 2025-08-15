using Microsoft.EntityFrameworkCore;
using Webshop.Api.Data;
using Stripe;
using WebshopProduct = Webshop.Api.Models.Product;

var builder = WebApplication.CreateBuilder(args);

// This should automatically be included, but make sure it's there:
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
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

// --- Build the app FIRST ---
var app = builder.Build();

// ✅ NOW configure Stripe AFTER the app is built so it can access user secrets
StripeConfiguration.ApiKey = app.Configuration["Stripe:SecretKey"];

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var stripeKey = app.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrEmpty(stripeKey))
{
    logger.LogInformation("Stripe key loaded: {KeyPrefix}...{KeySuffix}", 
        stripeKey.Substring(0, 12), 
        stripeKey.Substring(stripeKey.Length - 6));
}
else
{
    logger.LogWarning("No Stripe key found in configuration!");
}

// Also log what StripeConfiguration is actually using
logger.LogInformation("StripeConfiguration.ApiKey: {KeyPrefix}...{KeySuffix}", 
    StripeConfiguration.ApiKey?.Substring(0, 12), 
    StripeConfiguration.ApiKey?.Substring(StripeConfiguration.ApiKey.Length - 6));

// --- DB migrations on startup ---
// Build the app before using it
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
