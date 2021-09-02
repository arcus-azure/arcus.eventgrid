using Arcus.EventGrid.WebApi.Security;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.EventGrid.Tests.Integration.WebApi.Security.Controllers
{
    [ApiController]
    public class EventGridSubscriptionValidationController : ControllerBase
    {
        public const string GetSubscriptionValidationRoute = "subscr-val/event-grid";

        [HttpGet, HttpOptions]
        [Route(GetSubscriptionValidationRoute)]
        [EventGridSubscriptionValidation]
        public IActionResult Get()
        {
            return Accepted();
        }
    }
}
