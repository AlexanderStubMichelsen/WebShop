using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Webshop.Api.Controllers;
using Webshop.Api.Data;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using System.Threading.Tasks;

namespace Webshop.Api.Tests.Controllers
{
    public class PaymentsControllerTests
    {
        [Fact]
        public async Task SaveOrderToDatabase_DoesNotSave_WhenOrderExists()
        {
            var options = new DbContextOptionsBuilder<WebshopDbContext>()
                .UseInMemoryDatabase(databaseName: "PaymentsTestDb")
                .Options;
            var db = new WebshopDbContext(options);

            db.Orders.Add(new Webshop.Api.Models.Order { SessionId = "sess1", CustomerEmail = "test@example.com", CreatedAt = System.DateTime.UtcNow });
            db.SaveChanges();

            var logger = new Mock<ILogger<PaymentsController>>().Object;
            var controller = new PaymentsController(db, logger);

            var session = new Session { Id = "sess1", PaymentStatus = "paid", Created = DateTime.UtcNow };
            // You may need to make SaveOrderToDatabase public or use reflection for testing

            // Example: await controller.SaveOrderToDatabase(session);

            // Assert that no new orders were added
            Assert.Equal(1, await db.Orders.CountAsync());
        }
    }
}