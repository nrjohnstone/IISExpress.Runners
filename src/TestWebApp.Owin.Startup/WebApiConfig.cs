using System.Web.Http;

namespace TestWebApp.Owin.Startup
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            SetupXmlFormatter(config);

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}", new { id = RouteParameter.Optional }
            );

        }

        private static void SetupXmlFormatter(HttpConfiguration config)
        {
        }
    }
}