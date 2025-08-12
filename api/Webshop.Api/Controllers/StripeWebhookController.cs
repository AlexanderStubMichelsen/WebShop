using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;


namespace Webshop.Api.Controllers
{
    [ApiController]
    [Route("api/webhooks/[controller]")] // => /api/webhooks/stripe
    public class StripeController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly OrderEmailService _orderEmailService; // register in DI (see below)
        private readonly ILogger<StripeController> _logger;

        public StripeController(IConfiguration config, OrderEmailService orderEmailService, ILogger<StripeController> logger)
        {
            _config = config;
            _orderEmailService = orderEmailService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var sigHeader = Request.Headers["Stripe-Signature"];
            var webhookSecret = _config["Stripe:WebhookSecret"];

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json: json,
                    stripeSignatureHeader: sigHeader,
                    secret: webhookSecret,
                    tolerance: 300,
                    throwOnApiVersionMismatch: false // <-- add this
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stripe webhook signature verification failed");
                return BadRequest();
            }


            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;

                if (session != null && session.PaymentStatus == "paid" && !string.IsNullOrEmpty(session.Id))
                {
                    // Prefer CustomerDetails.Email, fallback to CustomerEmail
                    var email = session.CustomerDetails?.Email ?? session.CustomerEmail;
                    var amount = session.AmountTotal ?? 0;

                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        // Idempotent send (EmailLog ensures no duplicates on retries)
                        var (sent, reason) = await _orderEmailService
                            .SendIfNotSentAsync(session.Id, email, amount, forceResend: false);

                        _logger.LogInformation("Webhook processed session {SessionId}. Sent={Sent} Reason={Reason}", session.Id, sent, reason);
                    }
                    else
                    {
                        _logger.LogWarning("CheckoutSessionCompleted missing email for session {SessionId}", session.Id);
                    }
                }
            }

            // Acknowledge quickly so Stripe doesn’t retry
            return Ok();
        }
    }
}
