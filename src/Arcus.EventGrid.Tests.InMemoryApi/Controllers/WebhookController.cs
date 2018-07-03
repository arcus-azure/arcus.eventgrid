using System;
using System.Net.Http;
using System.Web.Http;
using Arcus.EventGrid.Security;
using Arcus.EventGrid.Security.Attributes;

namespace Arcus.EventGrid.Tests.InMemoryApi.Controllers
{
    public class WebhookController : ApiController
    {
        [HttpPost]
        [Route("events/test")]
        [EventGridSubscriptionValidator]
        [EventGridAuthorization("x-api-key", "event-grid")]
        public IHttpActionResult TestHandshake(HttpRequestMessage message)
        {
            try
            {
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

    }
}
