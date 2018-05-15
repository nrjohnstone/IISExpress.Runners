using System.Web.Http;

namespace TestWebApp.Owin.Startup.Controllers
{
    public class HealthController : ApiController
    {
        // GET api/Health
        [Route("api/health")]
        [HttpGet]
        public string GetHealth()
        {
            return "OK";
        }
    }
}
