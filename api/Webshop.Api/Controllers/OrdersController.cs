using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;
using Stripe;
using Stripe.Checkout;
using System.ComponentModel.DataAnnotations;
using Webshop.Api.Data;
using Webshop.Api.Models;

namespace Webshop.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly WebshopDbContext _db;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IConfiguration config, WebshopDbContext db, ILogger<OrdersController> logger)
        {
            _config = config;
            _db = db;
            _logger = logger;
        }

        [HttpPost("send-confirmation")]
        public async Task<IActionResult> SendOrderConfirmation(
            [FromBody] OrderConfirmationRequest request,
            [FromQuery] bool resend = false)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Idempotency check
            var existing = await _db.EmailLogs.AsNoTracking()
                .FirstOrDefaultAsync(x => x.SessionId == request.SessionId);

            if (existing != null && !resend)
            {
                return Ok(new { ok = true, sent = false, reason = "already-sent" });
            }

            // Config / API key
            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY")
                         ?? _config["SendGrid:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(500, new { message = "Email service not configured" });

            var fromEmail = _config["SendGrid:FromEmail"];
            if (string.IsNullOrWhiteSpace(fromEmail))
                return StatusCode(500, new { message = "Sender email not configured" });

            var fromName = _config["SendGrid:FromName"] ?? "Webshop";
            var sandbox = bool.TryParse(_config["SendGrid:Sandbox"], out var s) && s;

            // Build message
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(request.CustomerEmail);
            var subject = "Order Confirmation - Thank you for your purchase!";
            var html = CreateOrderConfirmationEmail(request);
            var text = $"Thank you for your order!\nSession ID: {request.SessionId}\nAmount: {request.AmountTotal / 100:F2} DKK";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, text, html);
            msg.SetReplyTo(from);
            msg.AddCategory("order-confirmation");
            msg.AddCustomArg("session_id", request.SessionId);
            if (sandbox)
                msg.MailSettings = new MailSettings { SandboxMode = new SandboxMode { Enable = true } };

            // Simple retry on transient errors (429/5xx)
            var attempts = 0;
            while (true)
            {
                attempts++;
                var response = await client.SendEmailAsync(msg);

                var code = (int)response.StatusCode;
                var transient = code == 429 || code >= 500;

                if (!transient)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                    {
                        await UpsertEmailLogAsync(existing, request.SessionId, request.CustomerEmail);
                        return Ok(new { ok = true, sent = true, reason = resend ? "resent" : "sent" });
                    }

                    var body = response.Body is null ? "" : await response.Body.ReadAsStringAsync();
                    _logger.LogWarning("SendGrid non-success: {Status} {Body}", response.StatusCode, body);
                    return StatusCode(code, new { message = "Failed to send email" });
                }

                if (attempts >= 3)
                {
                    var body = response.Body is null ? "" : await response.Body.ReadAsStringAsync();
                    _logger.LogError("SendGrid failed after retries: {Status} {Body}", response.StatusCode, body);
                    return StatusCode(502, new { message = "Email provider unavailable, please try again later" });
                }

                await Task.Delay((int)Math.Pow(2, attempts) * 200); // 400ms, 800ms
            }
        }

        private async Task UpsertEmailLogAsync(EmailLog? existing, string sessionId, string to)
        {
            if (existing == null)
            {
                _db.EmailLogs.Add(new EmailLog
                {
                    SessionId = sessionId,
                    To = to,
                    SentAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.To = to;
                existing.SentAtUtc = DateTime.UtcNow;
                _db.EmailLogs.Update(existing);
            }

            await _db.SaveChangesAsync();
        }

        private static string CreateOrderConfirmationEmail(OrderConfirmationRequest request) => $@"
<html>
<body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
  <div style='background:#f8f9fa; padding:20px; text-align:center;'>
    <h1 style='color:#28a745;margin:0;'>✅ Order Confirmation</h1>
    <p style='margin:8px 0 0'>Thank you for your purchase!</p>
  </div>
  <div style='padding:20px;'>
    <h2 style='margin-top:0;'>Order Details</h2>
    <table style='width:100%; border-collapse:collapse;'>
      <tr><td style='padding:10px; border-bottom:1px solid #dee2e6;'><strong>Order ID:</strong></td>
          <td style='padding:10px; border-bottom:1px solid #dee2e6;'>{System.Net.WebUtility.HtmlEncode(request.SessionId)}</td></tr>
      <tr><td style='padding:10px; border-bottom:1px solid #dee2e6;'><strong>Customer Email:</strong></td>
          <td style='padding:10px; border-bottom:1px solid #dee2e6;'>{System.Net.WebUtility.HtmlEncode(request.CustomerEmail)}</td></tr>
      <tr><td style='padding:10px; border-bottom:1px solid #dee2e6;'><strong>Total Amount:</strong></td>
          <td style='padding:10px; border-bottom:1px solid #dee2e6;'><strong>{request.AmountTotal / 100:F2} DKK</strong></td></tr>
    </table>
    <div style='margin-top:30px; padding:20px; background:#e9ecef; border-radius:5px;'>
      <p><strong>What's next?</strong></p>
      <p>Your order is being processed. We'll email tracking details once it ships.</p>
    </div>
    <div style='margin-top:20px; text-align:center;'>
      <a href='https://shop.devdisplay.online' style='background:#007bff; color:#fff; padding:10px 20px; text-decoration:none; border-radius:5px;'>Continue Shopping</a>
    </div>
  </div>
  <div style='background:#f8f9fa; padding:20px; text-align:center; color:#6c757d;'>
    <p style='margin:0;'>Thank you for shopping with us!</p>
    <p style='margin:4px 0 0;'><small>Questions? Just reply to this email.</small></p>
  </div>
</body>
</html>";

        public class OrderConfirmationRequest
        {
            [Required, StringLength(200)]
            public string SessionId { get; set; } = string.Empty;

            [Required, EmailAddress, StringLength(254)]
            public string CustomerEmail { get; set; } = string.Empty;

            [Range(0, long.MaxValue)]
            public long AmountTotal { get; set; }
        }

        [HttpGet("order/{sessionId}")]
        public async Task<IActionResult> GetOrderDetails(string sessionId)
        {
            try
            {
                // Get Stripe session with expanded line items
                var sessionService = new SessionService();
                var session = sessionService.Get(sessionId, new SessionGetOptions
                {
                    Expand = new List<string> {
                        "line_items",
                        "line_items.data.price.product",
                        "customer"
                    }
                });

                if (session == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                // Get customer email
                string customerEmail = session.CustomerEmail;
                if (string.IsNullOrEmpty(customerEmail) && session.Customer != null)
                {
                    var customerService = new Stripe.CustomerService();
                    var customer = customerService.Get(session.Customer.Id);
                    customerEmail = customer.Email;
                }

                // Extract line items with product details
                var orderItems = session.LineItems.Data.Select(item => new
                {
                    ProductId = item.Price.Product.Id,
                    ProductName = item.Price.Product.Name,
                    Description = item.Description ?? item.Price.Product.Description,
                    Quantity = item.Quantity ?? 0,
                    UnitPrice = item.Price.UnitAmount ?? 0,
                    Currency = item.Price.Currency,
                    TotalPrice = item.AmountTotal,
                    ProductMetadata = item.Price.Product.Metadata
                }).ToList();

                // Check if confirmation email was sent
                var emailLog = await _db.EmailLogs.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SessionId == sessionId);

                // Build comprehensive order response
                var orderDetails = new
                {
                    // Order Info
                    OrderId = session.Id,
                    PaymentIntentId = session.PaymentIntentId,
                    CreatedAt = session.Created,

                    // Customer Info
                    CustomerEmail = customerEmail,
                    CustomerName = session.CustomerDetails?.Name,

                    // Payment Info
                    PaymentStatus = session.PaymentStatus,
                    PaymentMethod = session.PaymentMethodTypes?.FirstOrDefault(),
                    Currency = session.Currency?.ToUpper(),

                    // Amounts
                    SubtotalAmount = session.AmountSubtotal,
                    TaxAmount = session.TotalDetails?.AmountTax ?? 0,
                    TotalAmount = session.AmountTotal,

                    // Formatted amounts for display
                    FormattedAmounts = new
                    {
                        Subtotal = $"{(session.AmountSubtotal ?? 0) / 100:F2} {session.Currency?.ToUpper()}",
                        Tax = $"{(session.TotalDetails?.AmountTax ?? 0) / 100:F2} {session.Currency?.ToUpper()}",
                        Total = $"{(session.AmountTotal ?? 0) / 100:F2} {session.Currency?.ToUpper()}"
                    },

                    // Items
                    Items = orderItems,
                    ItemCount = orderItems.Sum(x => x.Quantity),

                    // Email Status
                    EmailConfirmation = new
                    {
                        Sent = emailLog != null,
                        SentAt = emailLog?.SentAtUtc,
                        CanResend = true
                    },

                    // Session URLs (if available)
                    SuccessUrl = session.SuccessUrl,
                    CancelUrl = session.CancelUrl,

                    // Metadata
                    Metadata = session.Metadata
                };

                return Ok(orderDetails);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error retrieving order {SessionId}", sessionId);
                return StatusCode(502, new { message = "Error retrieving order from payment provider" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details for {SessionId}", sessionId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("order/{sessionId}/summary")]
        public async Task<IActionResult> GetOrderSummary(string sessionId)
        {
            try
            {
                var sessionService = new SessionService();
                var session = await Task.Run(() => sessionService.Get(sessionId, new SessionGetOptions
                {
                    Expand = new List<string> { "line_items" }
                }));

                if (session == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                var summary = new
                {
                    OrderId = session.Id,
                    Status = session.PaymentStatus,
                    TotalAmount = session.AmountTotal,
                    FormattedTotal = $"{(session.AmountTotal ?? 0) / 100:F2} {session.Currency?.ToUpper()}",
                    ItemCount = session.LineItems.Data.Sum(x => x.Quantity ?? 0),
                    CreatedAt = session.Created
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order summary for {SessionId}", sessionId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("database/{sessionId}")]
        public async Task<IActionResult> GetOrderFromDatabase(string sessionId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.SessionId == sessionId);

            if (order == null)
            {
                return NotFound(new { message = "Order not found in database" });
            }

            return Ok(new
            {
                order.Id,
                order.SessionId,
                order.CustomerEmail,
                order.CustomerName,
                order.PaymentStatus,
                order.TotalAmount,
                FormattedTotal = $"{order.TotalAmount / 100:F2} {order.Currency}",
                order.CreatedAt,
                Items = order.OrderItems.Select(item => new
                {
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice,
                    item.TotalPrice,
                    FormattedPrice = $"{item.TotalPrice / 100:F2} {item.Currency}"
                })
            });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var orders = await _db.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _db.Orders.CountAsync();

            return Ok(new
            {
                Orders = orders.Select(o => new
                {
                    o.Id,
                    o.SessionId,
                    o.CustomerEmail,
                    o.PaymentStatus,
                    o.TotalAmount,
                    FormattedTotal = $"{o.TotalAmount / 100:F2} {o.Currency}",
                    o.CreatedAt,
                    ItemCount = o.OrderItems.Count
                }),
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }
    }
}
