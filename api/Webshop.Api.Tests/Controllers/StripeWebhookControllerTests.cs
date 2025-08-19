using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Webshop.Api.Controllers;
using Webshop.Api.Data;
using Webshop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class StripeWebhookControllerTests
{
    [Fact]
    public async Task Post_ReturnsBadRequest_WhenSignatureInvalid()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WebshopDbContext>()
            .UseInMemoryDatabase("StripeWebhookTestDb")
            .Options;
        var db = new WebshopDbContext(options);

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Stripe:WebhookSecret"]).Returns("wrong_secret");

        var emailServiceMock = new Mock<OrderEmailService>(db, configMock.Object, new Mock<ILogger<OrderEmailService>>().Object);
        var logger = new Mock<ILogger<StripeController>>().Object;

        var controller = new StripeController(configMock.Object, emailServiceMock.Object, logger, db);

        // Simulate a request with invalid signature
        var json = "{}";
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        context.Request.Headers["Stripe-Signature"] = "invalid_signature";
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        // Act
        var result = await controller.Post();

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }
}