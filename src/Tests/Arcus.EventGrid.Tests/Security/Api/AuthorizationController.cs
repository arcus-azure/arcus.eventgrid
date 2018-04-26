using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Arcus.EventGrid.Security;
namespace Arcus.EventGrid.Tests.Security.Api
{
    public class AuthorizationController : ApiController
    {

        [HttpPost]
        [Route("authz/keyname")]
        [SecretKeyHandler("key-name")]
        public IHttpActionResult TestConfiguredKey(HttpRequestMessage message)
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
        [Route("authz/keynamevalue")]
        [SecretKeyHandler("key-name", "key-value")]
        public IHttpActionResult TestConfiguredKeyValue(HttpRequestMessage message)
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
