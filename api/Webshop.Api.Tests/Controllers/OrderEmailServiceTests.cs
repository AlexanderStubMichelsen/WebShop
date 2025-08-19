using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Webshop.Api.Controllers;
using Webshop.Api.Data;
using Webshop.Api.Models;
using System.Threading.Tasks;

public class OrderEmailServiceTests
{
    [Fact]
    public async Task SendIfNotSentAsync_ReturnsSendgridNotConfigured_IfNoApiKey()
    {
        var options = new DbContextOptionsBuilder<WebshopDbContext>()
            .UseInMemoryDatabase("EmailTestDb")
            .Options;
        var db = new WebshopDbContext(options);

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["SendGrid:ApiKey"]).Returns((string?)null);

        var logger = new Mock<ILogger<OrderEmailService>>().Object;
        var service = new OrderEmailService(db, configMock.Object, logger);

        var result = await service.SendIfNotSentAsync("sess1", "test@example.com", 10000);

        Assert.False(result.sent);
        Assert.Equal("sendgrid-not-configured", result.reason);
    }
}