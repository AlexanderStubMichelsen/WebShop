using Stripe.Checkout;

namespace Webshop.Api.Services
{
    public interface ISessionService
    {
        Session Get(string sessionId, SessionGetOptions options);
    }
}
