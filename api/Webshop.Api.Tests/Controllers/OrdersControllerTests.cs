using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Webshop.Api.Controllers;
using Webshop.Api.Data;
using Webshop.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using Webshop.Api.Services; // Add this using

namespace Webshop.Api.Tests.Controllers
{
    public class OrdersControllerTests
    {
        private OrdersController GetControllerWithDb(List<Order>? orders = null)
        {
            var options = new DbContextOptionsBuilder<WebshopDbContext>()
                .UseInMemoryDatabase(databaseName: "OrdersTestDb")
                .Options;
            var db = new WebshopDbContext(options);

            if (orders != null)
            {
                db.Orders.AddRange(orders);
                db.SaveChanges();
            }

            var config = new Mock<IConfiguration>().Object;
            var logger = new Mock<ILogger<OrdersController>>().Object;
            var sessionService = new Mock<ISessionService>().Object; // <-- Add this

            return new OrdersController(config, db, logger, sessionService); // <-- Pass sessionService
        }

        [Fact]
        public async Task ViewAllOrders_ReturnsHtml()
        {
            var controller = GetControllerWithDb(new List<Order>
        {
            new Order { Id = 1, SessionId = "sess1", CustomerEmail = "test@example.com", PaymentStatus = "paid", Currency = "DKK", TotalAmount = 10000, CreatedAt = System.DateTime.UtcNow }
        });

            var result = await controller.ViewAllOrders();
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Contains("Orders Dashboard", contentResult.Content);
        }

        [Fact]
        public async Task GetOrderDetails_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            var controller = GetControllerWithDb();
            var result = await controller.GetOrderDetails("nonexistent");

            Assert.IsType<NotFoundResult>(result);
        }
    }
}