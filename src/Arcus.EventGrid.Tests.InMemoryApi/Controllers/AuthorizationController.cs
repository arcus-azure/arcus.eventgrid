using System;
using System.Net.Http;
using System.Web.Http;
using Arcus.EventGrid.Security.Attributes;

namespace Arcus.EventGrid.Tests.InMemoryApi.Controllers
{
    public class AuthorizationController : ApiController
    {
        [HttpPost]
        [Route("authz/hardcoded")]
        [EventGridAuthorization("x-auth", "auth-key")]
        public IHttpActionResult TestConfiguredKeyWithHardcodedKey(HttpRequestMessage message)
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

        [HttpPost]
        [Route("authz/dynamic")]
        [DynamicEventGridAuthorization("x-auth")]
        public IHttpActionResult TestConfiguredKeyWithDynamicKey(HttpRequestMessage message)
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
