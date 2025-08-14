using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webshop.Api.Controllers;
using Webshop.Api.Data;
using Webshop.Api.Models;
using Xunit;

namespace Webshop.Api.Tests.Controllers
{
    public class ProductsControllerTests : IDisposable
    {
        private readonly WebshopDbContext _context;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            // Use in-memory database for testing
            var options = new DbContextOptionsBuilder<WebshopDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new WebshopDbContext(options);
            _controller = new ProductsController(_context);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var products = new[]
            {
                new Product { Id = 1, Name = "Test Product 1", Description = "Description 1", Price = 10.99M, ImageUrl = "image1.jpg" },
                new Product { Id = 2, Name = "Test Product 2", Description = "Description 2", Price = 20.99M, ImageUrl = "image2.jpg" },
                new Product { Id = 3, Name = "Test Product 3", Description = "Description 3", Price = 30.99M, ImageUrl = "image3.jpg" }
            };

            _context.Products.AddRange(products);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetProducts_ReturnsAllProducts()
        {
            // Act
            var result = await _controller.GetProducts();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<Product>>>(result);
            var products = actionResult.Value;
            Assert.NotNull(products);
            Assert.Equal(3, products.Count);
        }

        [Fact]
        public async Task GetProducts_ReturnsEmptyList_WhenNoProducts()
        {
            // Arrange - Clear all products
            _context.Products.RemoveRange(_context.Products);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProducts();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<Product>>>(result);
            var products = actionResult.Value;
            Assert.NotNull(products);
            Assert.Empty(products);
        }

        [Fact]
        public async Task GetProduct_ReturnsProduct_WhenProductExists()
        {
            // Arrange
            var productId = 1;

            // Act
            var result = await _controller.GetProduct(productId);

            // Assert
            var actionResult = Assert.IsType<ActionResult<Product>>(result);
            var product = actionResult.Value;
            Assert.NotNull(product);
            Assert.Equal(productId, product.Id);
            Assert.Equal("Test Product 1", product.Name);
        }

        [Fact]
        public async Task GetProduct_ReturnsNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            var nonExistentId = 999;

            // Act
            var result = await _controller.GetProduct(nonExistentId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateProduct_ReturnsCreatedProduct_WithValidData()
        {
            // Arrange
            var newProduct = new Product
            {
                Name = "New Test Product",
                Description = "New Description",
                Price = 15.99M,
                ImageUrl = "newimage.jpg"
            };

            // Act
            var result = await _controller.CreateProduct(newProduct);

            // Assert
            var actionResult = Assert.IsType<ActionResult<Product>>(result);
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var createdProduct = Assert.IsType<Product>(createdAtActionResult.Value);
            
            Assert.Equal("New Test Product", createdProduct.Name);
            Assert.Equal(15.99M, createdProduct.Price);
            Assert.True(createdProduct.Id > 0); // Should have an assigned ID

            // Verify it was actually saved to database
            var savedProduct = await _context.Products.FindAsync(createdProduct.Id);
            Assert.NotNull(savedProduct);
            Assert.Equal("New Test Product", savedProduct.Name);
        }

        [Theory]
        [InlineData("")]
        public async Task CreateProduct_WithInvalidName_ShouldHandleGracefully(string invalidName)
        {
            // Arrange
            var invalidProduct = new Product
            {
                Name = invalidName, // Empty string is allowed, but null is not
                Description = "Valid Description",
                Price = 10.99M,
                ImageUrl = "valid.jpg"
            };

            // Act & Assert
            // Note: This would normally trigger model validation in a real scenario
            // For this test, we're checking the controller behavior with empty name
            var result = await _controller.CreateProduct(invalidProduct);
            
            // The controller itself doesn't validate - that's handled by model binding
            // But we can verify the product gets saved (even with invalid data)
            var actionResult = Assert.IsType<ActionResult<Product>>(result);
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            Assert.NotNull(createdAtActionResult.Value);
        }

        [Fact]
        public async Task CreateProduct_WithNullName_ThrowsException()
        {
            // Arrange
            var invalidProduct = new Product
            {
                Name = null!, // This will cause the exception
                Description = "Valid Description",
                Price = 10.99M,
                ImageUrl = "valid.jpg"
            };

            // Act & Assert
            // We expect this to throw an exception because Name is required
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await _controller.CreateProduct(invalidProduct);
            });
        }

        [Fact]
        public async Task CreateProduct_CreatesProductWithCorrectRoute()
        {
            // Arrange
            var newProduct = new Product
            {
                Name = "Route Test Product",
                Description = "Test Description",
                Price = 25.99M,
                ImageUrl = "routetest.jpg"
            };

            // Act
            var result = await _controller.CreateProduct(newProduct);

            // Assert
            var actionResult = Assert.IsType<ActionResult<Product>>(result);
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            
            Assert.Equal("GetProduct", createdAtActionResult.ActionName);
            // Fix the warning by adding null check
            Assert.NotNull(createdAtActionResult.RouteValues);
            Assert.True(createdAtActionResult.RouteValues.ContainsKey("id"));
        }

        [Fact]
        public async Task CreateProduct_WithNegativePrice_StillCreatesProduct()
        {
            // Arrange
            var productWithNegativePrice = new Product
            {
                Name = "Negative Price Product",
                Description = "Test Description",
                Price = -10.99M,
                ImageUrl = "test.jpg"
            };

            // Act
            var result = await _controller.CreateProduct(productWithNegativePrice);

            // Assert
            var actionResult = Assert.IsType<ActionResult<Product>>(result);
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var createdProduct = Assert.IsType<Product>(createdAtActionResult.Value);
            
            Assert.Equal(-10.99M, createdProduct.Price);
        }

        [Fact]
        public async Task GetProducts_PerformanceTest_HandlesLargeDataset()
        {
            // Arrange - Add many products
            var manyProducts = new List<Product>();
            for (int i = 4; i <= 1000; i++)
            {
                manyProducts.Add(new Product
                {
                    Id = i,
                    Name = $"Product {i}",
                    Description = $"Description {i}",
                    Price = i * 1.99M,
                    ImageUrl = $"image{i}.jpg"
                });
            }

            _context.Products.AddRange(manyProducts);
            await _context.SaveChangesAsync();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _controller.GetProducts();
            stopwatch.Stop();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<Product>>>(result);
            var products = actionResult.Value;
            Assert.NotNull(products);
            Assert.Equal(1000, products.Count);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Should complete within 1 second");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}