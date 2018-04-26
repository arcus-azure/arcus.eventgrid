using System.Web.Http;
using Owin;
using System.Configuration;
using Arcus.EventGrid.Security;

namespace Arcus.EventGrid.Tests.Security
{
    public class TestStartup
    {
        public static string SecretKey { private get; set; }

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
                SecretKeyHandler.SecretKeyRetriever = () => SecretKey;
            }
        }
    }
}
