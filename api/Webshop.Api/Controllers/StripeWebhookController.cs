using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Webshop.Api.Data;
using Webshop.Api.Models;


namespace Webshop.Api.Controllers
{
    [ApiController]
    [Route("api/webhooks/[controller]")] // => /api/webhooks/stripe
    public class StripeController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly OrderEmailService _orderEmailService; // register in DI (see below)
        private readonly ILogger<StripeController> _logger;
        private readonly WebshopDbContext _db; // ✅ Add this


        public StripeController(IConfiguration config, OrderEmailService orderEmailService, ILogger<StripeController> logger, WebshopDbContext db) // ✅ Add db parameter
        {
            _config = config;
            _orderEmailService = orderEmailService;
            _logger = logger;
            _db = db; // ✅ Add this

        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            _logger.LogInformation("=== Webhook received at {Time} ===", DateTime.UtcNow);

            var webhookSecret = _config["Stripe:WebhookSecret"];

            Event stripeEvent;
            string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            _logger.LogInformation("Webhook payload received: {PayloadLength} characters", json.Length);

            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], webhookSecret);
                _logger.LogInformation("Webhook event type: {EventType}", stripeEvent.Type);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Webhook signature verification failed");
                return BadRequest();
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                _logger.LogInformation("Processing checkout.session.completed event");

                var session = stripeEvent.Data.Object as Session;

                if (session != null && session.PaymentStatus == "paid" && !string.IsNullOrEmpty(session.Id))
                {
                    _logger.LogInformation("Session {SessionId} is paid, attempting to save order", session.Id);

                    // Save order to database
                    await SaveOrderToDatabase(session);

                    // Send email (existing code)
                    var email = session.CustomerEmail ?? session.CustomerDetails?.Email;
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        var (sent, reason) = await _orderEmailService.SendIfNotSentAsync(
                            session.Id, email, session.AmountTotal ?? 0L);
                        _logger.LogInformation("Order email for {SessionId}: {Sent} ({Reason})",
                            session.Id, sent, reason);
                    }
                    else
                    {
                        _logger.LogWarning("No email found for session {SessionId}", session.Id);
                    }
                }
                else
                {
                    _logger.LogWarning("Session not valid for saving: SessionId={SessionId}, PaymentStatus={PaymentStatus}",
                        session?.Id, session?.PaymentStatus);
                }
            }
            else
            {
                _logger.LogInformation("Ignoring event type: {EventType}", stripeEvent.Type);
            }

            // Acknowledge quickly so Stripe doesn’t retry
            return Ok();
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
                        System.Text.Json.JsonSerializer.Serialize(session.Metadata) : null
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
                // Don't throw - we don't want to fail the webhook
            }
        }
    }
}
