using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Arcus.EventGrid.Security;

namespace Arcus.EventGrid.Tests.Security.Api
{
    public class WebhookController : ApiController
    {
        [HttpPost]
        [Route("events/test")]
        [EventGridSubscriptionValidator]
        [SecretKeyHandler("x-api-key", "event-grid")]
        public async Task<IHttpActionResult> TestHandshake(HttpRequestMessage message)
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
