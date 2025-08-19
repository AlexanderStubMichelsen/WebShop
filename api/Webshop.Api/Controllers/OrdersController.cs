using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;
using Stripe;
using Stripe.Checkout;
using System.ComponentModel.DataAnnotations;
using Webshop.Api.Data;
using Webshop.Api.Models;
using Webshop.Api.Services;

namespace Webshop.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly WebshopDbContext _db;
        private readonly ILogger<OrdersController> _logger;
        private readonly ISessionService _sessionService;

        public OrdersController(IConfiguration config, WebshopDbContext db, ILogger<OrdersController> logger, ISessionService sessionService)
        {
            _config = config;
            _db = db;
            _logger = logger;
            _sessionService = sessionService;
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
                var session = _sessionService.Get(sessionId, new SessionGetOptions
                {
                    Expand = new List<string> {
                        "line_items",
                        "line_items.data.price.product",
                        "customer"
                    }
                });

                if (session == null)
                {
                    return NotFound();
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

        [HttpGet("view")]
        public async Task<IActionResult> ViewAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var orders = await _db.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await _db.Orders.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Orders - Page {page}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }}
        .container {{ max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; margin-bottom: 20px; }}
        .stats {{ background: #e3f2fd; padding: 15px; border-radius: 5px; margin-bottom: 20px; }}
        .order {{ border: 1px solid #ddd; margin-bottom: 20px; border-radius: 8px; overflow: hidden; }}
        .order-header {{ background: #f8f9fa; padding: 15px; border-bottom: 1px solid #ddd; }}
        .order-body {{ padding: 15px; }}
        .status-paid {{ color: #28a745; font-weight: bold; }}
        .status-pending {{ color: #ffc107; font-weight: bold; }}
        .status-failed {{ color: #dc3545; font-weight: bold; }}
        .items {{ margin-top: 10px; }}
        .item {{ background: #f8f9fa; padding: 8px; margin: 5px 0; border-radius: 4px; }}
        .pagination {{ text-align: center; margin: 20px 0; }}
        .pagination a {{ display: inline-block; padding: 8px 12px; margin: 0 4px; background: #007bff; color: white; text-decoration: none; border-radius: 4px; }}
        .pagination a:hover {{ background: #0056b3; }}
        .pagination .current {{ background: #6c757d; }}
        .no-orders {{ text-align: center; padding: 40px; color: #6c757d; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
        .refresh {{ float: right; background: #28a745; color: white; padding: 8px 16px; text-decoration: none; border-radius: 4px; }}
        .refresh:hover {{ background: #218838; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>
            📦 Orders Dashboard
            <a href='/api/orders/view?page={page}&pageSize={pageSize}' class='refresh'>🔄 Refresh</a>
        </h1>
        
        <div class='stats'>
            <strong>📊 Stats:</strong> 
            {total} total orders | 
            Page {page} of {totalPages} | 
            Showing {Math.Min(pageSize, orders.Count)} orders
        </div>";

    if (!orders.Any())
    {
        html += @"
        <div class='no-orders'>
            <h2>No orders found</h2>
            <p>Complete a purchase to see orders here!</p>
        </div>";
    }
    else
    {
        foreach (var order in orders)
        {
            var statusClass = order.PaymentStatus?.ToLower() switch
            {
                "paid" => "status-paid",
                "pending" => "status-pending", 
                _ => "status-failed"
            };

            html += $@"
        <div class='order'>
            <div class='order-header'>
                <strong>Order #{order.Id}</strong> | 
                <span class='{statusClass}'>{order.PaymentStatus?.ToUpper()}</span> | 
                <strong>{order.TotalAmount / 100:F2} {order.Currency}</strong> | 
                {order.CreatedAt:yyyy-MM-dd HH:mm:ss}
            </div>
            <div class='order-body'>
                <table>
                    <tr><td><strong>Session ID:</strong></td><td>{order.SessionId}</td></tr>
                    <tr><td><strong>Customer:</strong></td><td>{order.CustomerEmail}{(string.IsNullOrEmpty(order.CustomerName) ? "" : $" ({order.CustomerName})")}</td></tr>
                    <tr><td><strong>Payment Method:</strong></td><td>{order.PaymentMethod ?? "N/A"}</td></tr>
                    <tr><td><strong>Items:</strong></td><td>{order.OrderItems.Count} item(s)</td></tr>
                </table>";

            if (order.OrderItems.Any())
            {
                html += @"
                <div class='items'>
                    <h4>📦 Items:</h4>";
                
                foreach (var item in order.OrderItems)
                {
                    html += $@"
                    <div class='item'>
                        <strong>{item.ProductName}</strong> | 
                        Qty: {item.Quantity} | 
                        {item.UnitPrice / 100:F2} {item.Currency} each | 
                        Total: <strong>{item.TotalPrice / 100:F2} {item.Currency}</strong>
                        {(string.IsNullOrEmpty(item.Description) ? "" : $"<br><small>{item.Description}</small>")}
                    </div>";
                }
                
                html += "</div>";
            }

            html += @"
            </div>
        </div>";
        }
    }

    // Pagination
    if (totalPages > 1)
    {
        html += "<div class='pagination'>";
        
        if (page > 1)
        {
            html += $"<a href='/api/orders/view?page={page - 1}&pageSize={pageSize}'>← Previous</a>";
        }
        
        for (int i = Math.Max(1, page - 2); i <= Math.Min(totalPages, page + 2); i++)
        {
            var currentClass = i == page ? " current" : "";
            html += $"<a href='/api/orders/view?page={i}&pageSize={pageSize}' class='{currentClass}'>{i}</a>";
        }
        
        if (page < totalPages)
        {
            html += $"<a href='/api/orders/view?page={page + 1}&pageSize={pageSize}'>Next →</a>";
        }
        
        html += "</div>";
    }

    html += @"
        <div style='margin-top: 30px; padding: 20px; background: #f8f9fa; border-radius: 5px; text-align: center;'>
            <p><strong>🔗 API Endpoints:</strong></p>
            <p>
                <a href='/api/orders/list'>JSON API</a> | 
                <a href='/api/orders/view?pageSize=50'>Show 50 per page</a> | 
                <a href='/api/orders/view?pageSize=100'>Show 100 per page</a>
            </p>
        </div>
    </div>
</body>
</html>";

    return Content(html, "text/html");
}

[HttpGet("items/view")]
public async Task<IActionResult> ViewAllOrderItems([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
{
    var orderItems = await _db.OrderItems
        .Include(oi => oi.Order)
        .OrderByDescending(oi => oi.Order.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var total = await _db.OrderItems.CountAsync();
    var totalPages = (int)Math.Ceiling(total / (double)pageSize);

    var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Order Items - Page {page}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }}
        .container {{ max-width: 1400px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; margin-bottom: 20px; }}
        .stats {{ background: #e3f2fd; padding: 15px; border-radius: 5px; margin-bottom: 20px; }}
        .table-container {{ overflow-x: auto; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        th, td {{ border: 1px solid #ddd; padding: 12px; text-align: left; }}
        th {{ background-color: #f2f2f2; font-weight: bold; position: sticky; top: 0; }}
        .item-row:nth-child(even) {{ background-color: #f9f9f9; }}
        .item-row:hover {{ background-color: #e3f2fd; }}
        .pagination {{ text-align: center; margin: 20px 0; }}
        .pagination a {{ display: inline-block; padding: 8px 12px; margin: 0 4px; background: #007bff; color: white; text-decoration: none; border-radius: 4px; }}
        .pagination a:hover {{ background: #0056b3; }}
        .pagination .current {{ background: #6c757d; }}
        .no-items {{ text-align: center; padding: 40px; color: #6c757d; }}
        .refresh {{ float: right; background: #28a745; color: white; padding: 8px 16px; text-decoration: none; border-radius: 4px; }}
        .refresh:hover {{ background: #218838; }}
        .order-link {{ color: #007bff; text-decoration: none; }}
        .order-link:hover {{ text-decoration: underline; }}
        .currency {{ font-weight: bold; color: #28a745; }}
        .product-name {{ font-weight: bold; color: #333; }}
        .nav-links {{ margin-bottom: 20px; }}
        .nav-links a {{ display: inline-block; padding: 8px 16px; margin-right: 10px; background: #6c757d; color: white; text-decoration: none; border-radius: 4px; }}
        .nav-links a:hover {{ background: #5a6268; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>
            📦 Order Items Dashboard
            <a href='/api/orders/items/view?page={page}&pageSize={pageSize}' class='refresh'>🔄 Refresh</a>
        </h1>
        
        <div class='nav-links'>
            <a href='/api/orders/view'>← Back to Orders</a>
            <a href='/api/orders/items/view?pageSize=25'>25 per page</a>
            <a href='/api/orders/items/view?pageSize=100'>100 per page</a>
        </div>
        
        <div class='stats'>
            <strong>📊 Stats:</strong> 
            {total} total order items | 
            Page {page} of {totalPages} | 
            Showing {Math.Min(pageSize, orderItems.Count)} items
        </div>";

    if (!orderItems.Any())
    {
        html += @"
        <div class='no-items'>
            <h2>No order items found</h2>
            <p>Complete a purchase to see order items here!</p>
        </div>";
    }
    else
    {
        html += @"
        <div class='table-container'>
            <table>
                <thead>
                    <tr>
                        <th>Item ID</th>
                        <th>Order ID</th>
                        <th>Product Name</th>
                        <th>Description</th>
                        <th>Quantity</th>
                        <th>Unit Price</th>
                        <th>Total Price</th>
                        <th>Currency</th>
                        <th>Customer</th>
                        <th>Order Date</th>
                    </tr>
                </thead>
                <tbody>";

        foreach (var item in orderItems)
        {
            html += $@"
                    <tr class='item-row'>
                        <td>{item.Id}</td>
                        <td><a href='/api/orders/view?search={item.Order.SessionId}' class='order-link'>#{item.OrderId}</a></td>
                        <td class='product-name'>{item.ProductName}</td>
                        <td>{(string.IsNullOrEmpty(item.Description) ? "N/A" : item.Description)}</td>
                        <td style='text-align: center;'>{item.Quantity}</td>
                        <td class='currency'>{item.UnitPrice / 100:F2}</td>
                        <td class='currency'>{item.TotalPrice / 100:F2}</td>
                        <td>{item.Currency}</td>
                        <td>{item.Order.CustomerEmail}</td>
                        <td>{item.Order.CreatedAt:yyyy-MM-dd HH:mm}</td>
                    </tr>";
        }

        html += @"
                </tbody>
            </table>
        </div>";
    }

    // Pagination
    if (totalPages > 1)
    {
        html += "<div class='pagination'>";
        
        if (page > 1)
        {
            html += $"<a href='/api/orders/items/view?page={page - 1}&pageSize={pageSize}'>← Previous</a>";
        }
        
        for (int i = Math.Max(1, page - 2); i <= Math.Min(totalPages, page + 2); i++)
        {
            var currentClass = i == page ? " current" : "";
            html += $"<a href='/api/orders/items/view?page={i}&pageSize={pageSize}' class='{currentClass}'>{i}</a>";
        }
        
        if (page < totalPages)
        {
            html += $"<a href='/api/orders/items/view?page={page + 1}&pageSize={pageSize}'>Next →</a>";
        }
        
        html += "</div>";
    }

    html += @"
        <div style='margin-top: 30px; padding: 20px; background: #f8f9fa; border-radius: 5px; text-align: center;'>
            <p><strong>🔗 Quick Actions:</strong></p>
            <p>
                <a href='/api/orders/view'>View All Orders</a> | 
                <a href='/api/orders/list'>JSON API - Orders</a> | 
                <a href='/api/orders/items/json'>JSON API - Items</a>
            </p>
        </div>
    </div>
</body>
</html>";

    return Content(html, "text/html");
}

[HttpGet("items/json")]
public async Task<IActionResult> GetAllOrderItems([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
{
    var orderItems = await _db.OrderItems
        .Include(oi => oi.Order)
        .OrderByDescending(oi => oi.Order.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var total = await _db.OrderItems.CountAsync();

    return Ok(new
    {
        OrderItems = orderItems.Select(oi => new
        {
            oi.Id,
            oi.OrderId,
            OrderSessionId = oi.Order.SessionId,
            oi.ProductId,
            oi.ProductName,
            oi.Description,
            oi.Quantity,
            oi.UnitPrice,
            oi.TotalPrice,
            oi.Currency,
            FormattedUnitPrice = $"{oi.UnitPrice / 100:F2} {oi.Currency}",
            FormattedTotalPrice = $"{oi.TotalPrice / 100:F2} {oi.Currency}",
            CustomerEmail = oi.Order.CustomerEmail,
            OrderCreatedAt = oi.Order.CreatedAt,
            OrderStatus = oi.Order.PaymentStatus
        }),
        Total = total,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(total / (double)pageSize)
    });
}

[HttpGet("stats")]
public async Task<IActionResult> GetOrderStats()
{
    var totalOrders = await _db.Orders.CountAsync();
    var totalOrderItems = await _db.OrderItems.CountAsync();
    var totalRevenue = await _db.Orders.Where(o => o.PaymentStatus == "paid").SumAsync(o => o.TotalAmount);
    var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
    
    var topProducts = await _db.OrderItems
        .GroupBy(oi => oi.ProductName)
        .Select(g => new
        {
            ProductName = g.Key,
            TotalQuantity = g.Sum(oi => oi.Quantity),
            TotalRevenue = g.Sum(oi => oi.TotalPrice),
            OrderCount = g.Count()
        })
        .OrderByDescending(p => p.TotalQuantity)
        .Take(5)
        .ToListAsync();

    return Ok(new
    {
        TotalOrders = totalOrders,
        TotalOrderItems = totalOrderItems,
        TotalRevenue = totalRevenue,
        FormattedRevenue = $"{totalRevenue / 100:F2} DKK",
        AverageOrderValue = avgOrderValue,
        FormattedAvgOrderValue = $"{avgOrderValue / 100:F2} DKK",
        TopProducts = topProducts.Select(p => new
        {
            p.ProductName,
            p.TotalQuantity,
            p.OrderCount,
            FormattedRevenue = $"{p.TotalRevenue / 100:F2} DKK"
        })
    });
}
    }
}
