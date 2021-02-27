using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DnsClient;
using System.Collections.Concurrent;

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

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Domains(List<string> domains)
        {
            List<Domain> filledDomains = new List<Domain>();

            await CheckDomains(domains, filledDomains);


            ConcurrentDictionary<string,List<string>> dict = new ConcurrentDictionary<string, List<string>>();


            Parallel.ForEach(filledDomains, (currentDomain) =>
            {
                dict.TryAdd(currentDomain.Address, currentDomain.IP);
            });

            var ndic = dict.ToDictionary(e => e.Key, e => e.Value);

            return Ok(ndic);

        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> DomainsWithIP(string domainsWithIP)
        {
            List<Domain> filledDomains = new List<Domain>();

            //await CheckDomains(domains, filledDomains);


            //Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();


            //Parallel.ForEach(filledDomains, (currentDomain) =>
            //{
            //    dict.Add(currentDomain.Address, currentDomain.IP);
            //});


            return Ok("test");

        }

        private async Task CheckDomains(List<string> domains, List<Domain> filledDomains)
        {
            foreach (var domain in domains)
            {

                using var client = new HttpClient();
                client.Timeout = new TimeSpan(0, 0, 5);
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
                    _logger.LogError($"{domain} does not have a correct Uri format: {ex.Message}");
                    // We ignore malformed domains...
                }

                try
                {
                    var clientResult = await client.GetAsync(fullUriHost);
                    string server = clientResult.Headers.GetValues("Server").First();

                    if (IsNginx(server))
                    {
                        var lookup = new LookupClient();
                        var result = await lookup.QueryAsync(domain, QueryType.A);

                        filledDomain.Address = domain;
                        foreach (var arecord in result.Answers.ARecords())
                        {
                            filledDomain.IP.Add(arecord.Address.ToString());
                        }

                        filledDomains.Add(filledDomain);
                    }
                }
                catch (Exception ex)
                {
                    // We let it pass for now...
                    _logger.LogError($"{domain} - some problem getting the server or IP: {ex.Message}");
                }
            }
        }

        private static bool IsNginx(string server)
        {
            return server.ToUpper().Contains("NGINX");
        }

    }
}
