using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;
using DnsClient;

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
        public async Task<IActionResult> Domains(List<string> domains)
        {
            List<Domain> filledDomains = new List<Domain>();

            foreach (var domain in domains)
            {

                using var client = new HttpClient();
                Ping ping = new Ping();
                Uri uri;
                string uriHost = string.Empty, fullUriHost = string.Empty;
                Domain filledDomain = new Domain();

                try
                {
                    UriBuilder urlb = new UriBuilder("http", domain);
                    uriHost = urlb.Uri.Host;
                    fullUriHost = urlb.Uri.AbsoluteUri;
                }
                catch (Exception ex)
                {
                    // We ignore malformed domains...
                }

                try
                {
                    var lookup = new LookupClient();
                    var result = await lookup.QueryAsync("google.com", QueryType.A);

                    var record = result.Answers.ARecords().FirstOrDefault();
                    var ip = record?.Address;

                    var clientResult = await client.GetAsync(fullUriHost);
                    string server = clientResult.Headers.GetValues("Server").First();

                    if (IsNginx(server))
                    {
                        PingReply reply = ping.Send(uriHost);
                        filledDomain.address = domain;
                        filledDomain.IP = reply.Address.ToString();
                        filledDomains.Add(filledDomain);
                    }
                }
                catch (Exception ex)
                {
                    // We let it pass for now...
                    //throw;
                }
            }

            return Ok(filledDomains);

        }

        private bool IsNginx(string server)
        {
            return true;
        }
    }
}
