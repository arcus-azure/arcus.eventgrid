using Arcus.EventGrid.WebApi.Security;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.EventGrid.Tests.Integration.WebApi.Security.Controllers
{
    [ApiController]
    public class EventGridAuthorizationController : ControllerBase
    {
        public const string GetAuthorizedWithHeaderRoute = "authz/event-grid/header",
                            GetAuthorizedWithHeaderEmitsSecurityEventsRoute = "authz/event-grid/header/security-events",
                            GetAuthorizedWithQueryRoute = "authz/event-grid/query",
                            GetAuthorizedWithQueryEmitsSecurityEventsRoute = "authz/event-grid/query/security-events",
                            HttpRequestHeaderName = "X-Auth-Header",
                            HttpRequestQueryParameterName = "x-auth-param",
                            SecretName = "auth-key";

        [HttpGet]
        [Route(GetAuthorizedWithHeaderRoute)]
        [EventGridAuthorization(HttpRequestProperty.Header, HttpRequestHeaderName, SecretName)]
        public IActionResult GetAuthorizedWithHeader()
        {
            return Ok();
        }
        
        [HttpGet]
        [Route(GetAuthorizedWithHeaderEmitsSecurityEventsRoute)]
        [EventGridAuthorization(HttpRequestProperty.Header, HttpRequestHeaderName, SecretName, EmitSecurityEvents = true)]
        public IActionResult GetAuthorizedWithHeaderEmitsSecurityEvents()
        {
            return Ok();
        }
        
        [HttpGet]
        [Route(GetAuthorizedWithQueryRoute)]
        [EventGridAuthorization(HttpRequestProperty.Query, HttpRequestQueryParameterName, SecretName)]
        public IActionResult GetAuthorizedWithQuery()
        {
            return Ok();
        }
        
        [HttpGet]
        [Route(GetAuthorizedWithQueryEmitsSecurityEventsRoute)]
        [EventGridAuthorization(HttpRequestProperty.Query, HttpRequestQueryParameterName, SecretName, EmitSecurityEvents = true)]
        public IActionResult GetAuthorizedWithQueryEmitsSecurityEvents()
        {
            return Ok();
        }
    }
}
