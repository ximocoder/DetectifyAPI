using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace DetectifyAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DetectorController : ControllerBase
    {
        private readonly ILogger<DetectorController> _logger;

        public DetectorController(ILogger<DetectorController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            return "test";
        }

        [HttpPost]
        public async Task<IActionResult> IndexAsync(List<string> domains)
        {
            JsonResult result = new JsonResult("test");

            foreach (var domain in domains)
            {
                using var client = new HttpClient();
                Ping ping = new Ping();

                string domainToCheck = "http://www.sl.se";

                Uri uri = new Uri(domainToCheck);
                string uriHost = uri.Host;
                string fullusiHost = uri.AbsoluteUri;

                try
                {
                    var clientResult = await client.GetAsync(domainToCheck);
                    string server = clientResult.Headers.GetValues("Server").First();

                    if (IsNginx(server))
                    {
                        PingReply reply = ping.Send(uriHost);
                        string address = reply.Address.ToString();
                    }
                }
                catch (Exception ex)
                {
                    // We let it pass for now...
                    throw;
                }  


            }

            return Ok("test");
      
        }

        private bool IsNginx(string server)
        {
            return true;
        }
    }
}
