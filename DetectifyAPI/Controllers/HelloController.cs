using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace DetectifyAPI.Controllers
{
    [ApiController]
    [Route("/")]
    public class HelloController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Hi!!!\r\n\r\nPlease do any of this calls:\r\n\r\nPOST http://localhost:8080/Detector/Domains\r\n\r\n" +
                      "POST http://localhost:8080/Detector/DomainsWithIp\r\n\r\nBeing 8080 the port that the docker container has opened.");
        }

    }
}
