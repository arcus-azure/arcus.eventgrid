using System.Threading.Tasks;
using System.Web.Http;
using Arcus.EventGrid.Security;
using Arcus.EventGrid.Security.Attributes;
using Owin;

namespace Arcus.EventGrid.Tests.InMemoryApi
{
    public class InMemoryTestApiStartup
    {
        public static string SecretKey { private get; set; } = null;

        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration
            {
                IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always
            };

            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            app.UseWebApi(config);

            if (!string.IsNullOrEmpty(SecretKey))
            {
                DynamicEventGridAuthorizationAttribute.RetrieveAuthenticationSecret = () => Task.FromResult(SecretKey);
            }
        }
    }
}
