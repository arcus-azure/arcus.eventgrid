using Microsoft.AspNetCore.Mvc;

namespace Arcus.EventGrid.Tests.Integration.WebApi.Fixture
{
    [Route(GetRoute)]
    [ApiController]
    public class HealthController : ControllerBase
    {
        public const string GetRoute = "api/v1/health";

        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
