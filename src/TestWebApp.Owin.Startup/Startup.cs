using System.Web.Http;
using Microsoft.Owin;
using Owin;
using TestWebApp.Owin.Startup;

[assembly: OwinStartup(typeof(Startup))]

namespace TestWebApp.Owin.Startup
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var httpConfiguration = new HttpConfiguration();
            WebApiConfig.Register(httpConfiguration);
            app.UseWebApi(httpConfiguration);
        }
    }
}