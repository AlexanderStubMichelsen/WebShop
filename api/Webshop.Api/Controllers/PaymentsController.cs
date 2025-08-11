using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Webshop.Api.Models;
using WebshopProduct = Webshop.Api.Models.Product;

namespace Webshop.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        [HttpPost("create-checkout-session")]
        public IActionResult CreateCheckoutSession([FromBody] List<WebshopProduct> products)
        {
            var lineItems = products.Select(product => new SessionLineItemOptions
            {
                Quantity = product.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "dkk",
                    UnitAmount = (long)(product.Price * 100), // convert to øre
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = product.Name,
                        Description = product.Description,
                        Images = new List<string> { product.ImageUrl }
                    }
                }
            }).ToList();

            var frontendUrl = GetFrontendUrl(Request);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                CustomerCreation = "always", // This ensures a customer is created
                BillingAddressCollection = "required", // Optional: also collect billing address
                SuccessUrl = $"{frontendUrl}/receipt?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{frontendUrl}/cart",
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Ok(new { url = session.Url });
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
    }
}
