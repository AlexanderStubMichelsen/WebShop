using Stripe.Checkout;

namespace Webshop.Api.Services
{
    public class SessionServiceImpl : ISessionService
    {
        public Session Get(string sessionId, SessionGetOptions options)
        {
            var service = new SessionService();
            return service.Get(sessionId, options);
        }
    }
}