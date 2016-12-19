using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace TestWebApp.Controllers
{
    public class HealthController : ApiController
    {
        // GET api/Health
        public string GetHealth()
        {
            return "OK";
        }
    }
}
