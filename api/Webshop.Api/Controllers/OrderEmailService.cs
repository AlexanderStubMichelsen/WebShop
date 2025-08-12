using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;
using Webshop.Api.Data;
using Webshop.Api.Models;

public class OrderEmailService
{
    private readonly WebshopDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<OrderEmailService> _logger;

    public OrderEmailService(WebshopDbContext db, IConfiguration config, ILogger<OrderEmailService> logger)
    {
        _db = db; _config = config; _logger = logger;
    }

    public async Task<(bool sent, string reason)> SendIfNotSentAsync(string sessionId, string to, long amountTotal, bool forceResend = false)
    {
        var existing = await _db.EmailLogs.AsNoTracking().FirstOrDefaultAsync(x => x.SessionId == sessionId);
        if (existing != null && !forceResend)
            return (false, "already-sent");

        var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? _config["SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return (false, "sendgrid-not-configured");

        var fromEmail = _config["SendGrid:FromEmail"];
        var fromName  = _config["SendGrid:FromName"] ?? "WebShop";
        var sandbox   = bool.TryParse(_config["SendGrid:Sandbox"], out var s) && s;

        var client = new SendGridClient(apiKey);
        var from = new EmailAddress(fromEmail, fromName);
        var toAddr = new EmailAddress(to);
        var subject = "Order Confirmation - Thank you for your purchase!";

        string html = $@"
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
          <td style='padding:10px; border-bottom:1px solid #dee2e6;'>{System.Net.WebUtility.HtmlEncode(sessionId)}</td></tr>
      <tr><td style='padding:10px; border-bottom:1px solid #dee2e6;'><strong>Customer Email:</strong></td>
          <td style='padding:10px; border-bottom:1px solid #dee2e6;'>{System.Net.WebUtility.HtmlEncode(to)}</td></tr>
      <tr><td style='padding:10px; border-bottom:1px solid #dee2e6;'><strong>Total Amount:</strong></td>
          <td style='padding:10px; border-bottom:1px solid #dee2e6;'><strong>{amountTotal / 100.0:F2} DKK</strong></td></tr>
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

        string text = $"Thank you for your order!\nSession ID: {sessionId}\nAmount: {amountTotal / 100.0:F2} DKK";

        var msg = MailHelper.CreateSingleEmail(from, toAddr, subject, text, html);
        msg.SetReplyTo(from);
        msg.AddCategory("order-confirmation");
        msg.AddCustomArg("session_id", sessionId);
        if (sandbox)
            msg.MailSettings = new MailSettings { SandboxMode = new SandboxMode { Enable = true } };

        var resp = await client.SendEmailAsync(msg);
        if (resp.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            var body = resp.Body is null ? "" : await resp.Body.ReadAsStringAsync();
            _logger.LogWarning("SendGrid non-success: {Status} {Body}", resp.StatusCode, body);
            return (false, "provider-failed");
        }

        if (existing == null)
            _db.EmailLogs.Add(new EmailLog { SessionId = sessionId, To = to, SentAtUtc = DateTime.UtcNow });
        else
        {
            existing.To = to; existing.SentAtUtc = DateTime.UtcNow;
            _db.EmailLogs.Update(existing);
        }

        await _db.SaveChangesAsync();
        return (true, existing == null ? "sent" : "resent");
    }
}
