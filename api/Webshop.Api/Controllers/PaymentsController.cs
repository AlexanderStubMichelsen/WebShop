using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ✅ Add this
using Stripe;
using Stripe.Checkout;
using Webshop.Api.Data; // ✅ Add this
using Webshop.Api.Models;
using WebshopProduct = Webshop.Api.Models.Product;

namespace Webshop.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly WebshopDbContext _db; // ✅ Add this
        private readonly ILogger<PaymentsController> _logger; // ✅ Add this

        public PaymentsController(WebshopDbContext db, ILogger<PaymentsController> logger) // ✅ Add constructor
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost("create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] List<WebshopProduct> products)
        {
            try
            {
                var lineItems = new List<SessionLineItemOptions>();
                foreach (var product in products)
                {
                    lineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(product.Price * 100),
                            Currency = "dkk",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = product.Name,
                                Description = product.Description,
                            },
                        },
                        Quantity = product.Quantity,
                    });
                }

                var frontendUrl = GetFrontendUrl(Request);

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card", "mobilepay" },
                    LineItems = lineItems,
                    Mode = "payment",
                    CustomerCreation = "always",
                    BillingAddressCollection = "required",
                    SuccessUrl = $"{frontendUrl}/receipt?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{frontendUrl}/cart",
                };

                var service = new SessionService();
                Session session = await service.CreateAsync(options);

                return Ok(new { url = session.Url });
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating checkout session");
                return StatusCode(500, new { message = "Stripe error: " + ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error creating checkout session");
                return StatusCode(500, new { message = "Server error: " + ex.Message });
            }
        }

        [HttpGet("session/{sessionId}")]
        public IActionResult GetCheckoutSession(string sessionId)
        {
            var service = new SessionService();
            var session = service.Get(sessionId, new SessionGetOptions
            {
                Expand = new List<string> { "customer" }
            });

            string customerEmail = session.CustomerEmail;

            // If session doesn't have email, try to get it from the customer object
            if (string.IsNullOrEmpty(customerEmail) && session.Customer != null)
            {
                var customerService = new Stripe.CustomerService();
                var customer = customerService.Get(session.Customer.Id);
                customerEmail = customer.Email;
            }

            return Ok(new
            {
                id = session.Id,
                amount_total = session.AmountTotal,
                customer_email = customerEmail,
                payment_status = session.PaymentStatus
            });
        }

        [HttpPost("test-save-order/{sessionId}")]
        public async Task<IActionResult> TestSaveOrder(string sessionId)
        {
            try
            {
                var sessionService = new SessionService();
                var session = sessionService.Get(sessionId);

                if (session?.PaymentStatus == "paid")
                {
                    await SaveOrderToDatabase(session);
                    return Ok(new { message = "Order saved successfully" });
                }

                return BadRequest(new { message = $"Session not paid (status: {session?.PaymentStatus}) or not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving order for session {SessionId}", sessionId);
                return StatusCode(500, new { message = ex.Message });
            }
        }

        private async Task SaveOrderToDatabase(Session session)
        {
            try
            {
                _logger.LogInformation("Attempting to save order for session {SessionId}", session.Id);

                // Check if order already exists
                var existingOrder = await _db.Orders.FirstOrDefaultAsync(o => o.SessionId == session.Id);
                if (existingOrder != null)
                {
                    _logger.LogInformation("Order {SessionId} already exists in database", session.Id);
                    return;
                }

                // Get detailed session with line items
                var sessionService = new SessionService();
                var detailedSession = sessionService.Get(session.Id, new SessionGetOptions
                {
                    Expand = new List<string> { "line_items", "line_items.data.price.product" }
                });

                _logger.LogInformation("Retrieved detailed session with {ItemCount} line items",
                    detailedSession.LineItems?.Data?.Count ?? 0);

                // Create order
                var order = new Order
                {
                    SessionId = session.Id,
                    PaymentIntentId = session.PaymentIntentId,
                    CustomerEmail = session.CustomerEmail ?? session.CustomerDetails?.Email ?? "",
                    CustomerName = session.CustomerDetails?.Name,
                    PaymentStatus = session.PaymentStatus,
                    PaymentMethod = session.PaymentMethodTypes?.FirstOrDefault(),
                    Currency = session.Currency?.ToUpper() ?? "DKK",
                    SubtotalAmount = session.AmountSubtotal ?? 0,
                    TaxAmount = session.TotalDetails?.AmountTax ?? 0,
                    TotalAmount = session.AmountTotal ?? 0,
                    CreatedAt = session.Created,
                    UpdatedAt = DateTime.UtcNow,
                    Metadata = session.Metadata.Any() ?
                        System.Text.Json.JsonSerializer.Serialize(session.Metadata) : null,
                    AddressLine1 = session.CustomerDetails?.Address?.Line1,
                    AddressLine2 = session.CustomerDetails?.Address?.Line2,
                    City = session.CustomerDetails?.Address?.City,
                    PostalCode = session.CustomerDetails?.Address?.PostalCode,
                    Country = session.CustomerDetails?.Address?.Country
                };

                // Add order items
                if (detailedSession.LineItems?.Data != null)
                {
                    foreach (var lineItem in detailedSession.LineItems.Data)
                    {
                        order.OrderItems.Add(new OrderItem
                        {
                            ProductId = lineItem.Price.Product.Id,
                            ProductName = lineItem.Price.Product.Name,
                            Description = lineItem.Description ?? lineItem.Price.Product.Description,
                            Quantity = (int)(lineItem.Quantity ?? 0),
                            UnitPrice = lineItem.Price.UnitAmount ?? 0,
                            TotalPrice = lineItem.AmountTotal,
                            Currency = lineItem.Price.Currency?.ToUpper() ?? "DKK"
                        });
                    }
                }

                _logger.LogInformation("Created order with {ItemCount} items", order.OrderItems.Count);

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Successfully saved order {SessionId} to database with ID {OrderId}",
                    session.Id, order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save order {SessionId} to database", session.Id);
                throw; // Re-throw for the controller to handle
            }
        }

        private string GetFrontendUrl(HttpRequest request)
        {
            // Check if we're in production by looking at the request host
            var host = request.Headers["Host"].ToString();

            if (host.Contains("webshop-api.devdisplay.online"))
            {
                return "https://shop.devdisplay.online";
            }

            return "http://localhost:3000";
        }

        // Add this to your PaymentsController for testing
        [HttpGet("test-config")]
        public IActionResult TestConfig()
        {
            var stripeKey = StripeConfiguration.ApiKey;
            var hasKey = !string.IsNullOrEmpty(stripeKey);
            var keyPrefix = hasKey ? stripeKey.Substring(0, 12) + "..." : "none";
            
            return Ok(new { 
                HasStripeKey = hasKey,
                KeyPrefix = keyPrefix,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }
    }
}
