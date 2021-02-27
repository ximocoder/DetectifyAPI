using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DnsClient;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using System.Net;

namespace DetectifyAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DetectorController : ControllerBase
    {
        private readonly ILogger<DetectorController> _logger;
        private static ConcurrentDictionary<string, List<string>> _filledDomainsDictionary;
        //private static HttpClient _httpClient;


        public DetectorController(ILogger<DetectorController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Domains(List<string> domains)
        {
            _filledDomainsDictionary = new ConcurrentDictionary<string, List<string>>();

            var domainsWithNginx = await CheckDomains(domains, _filledDomainsDictionary, false);

            return Ok(domainsWithNginx);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> DomainsWithIP(List<string> domains)
        {
            _filledDomainsDictionary = new ConcurrentDictionary<string, List<string>>();

            await CheckDomains(domains, _filledDomainsDictionary, true);
            var ndic = _filledDomainsDictionary.ToDictionary(e => e.Key, e => e.Value);

            return Ok(ndic);

        }

        private async Task<List<string>> CheckDomains(List<string> domains, ConcurrentDictionary<string, List<string>> filledDomainsDictionary, bool withIps)
        {
            //https://docs.microsoft.com/en-us/dotnet/api/system.net.security.remotecertificatevalidationcallback
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };
            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                CheckCertificateRevocationList = false,
                AllowAutoRedirect = false
            };
            HttpClient _httpClient;
            _httpClient = new HttpClient(httpClientHandler)
            {
                Timeout = new TimeSpan(0, 0, 10)
            };
            List<string> domainsWithoutIps = new List<string>();

            // With async parallel on net core gets more tricky:
            // https://timdeschryver.dev/blog/process-your-list-in-parallel-to-make-it-faster-in-dotnet
            await domains.ParallelForEachAsync(async domain =>
            {                
                string uriHost = string.Empty, fullUriHost = string.Empty;

                if (!Uri.IsWellFormedUriString(domain, UriKind.RelativeOrAbsolute))
                {
                    _logger.LogError($"{domain} - URL format is not ok");
                }
                else
                {
                    UriBuilder urlb = new UriBuilder("http", domain);
                    uriHost = urlb.Uri.Host;
                    fullUriHost = urlb.Uri.AbsoluteUri;

                    try
                    {
                        // Cut connection once responce header is read, we want it NOW
                        var clientResult = await _httpClient.GetAsync(fullUriHost, HttpCompletionOption.ResponseHeadersRead);
                        string server = clientResult.Headers.GetValues("Server").First();

                        if (IsNginx(server))
                        {
                            if (withIps)
                            {
                                var lookup = new LookupClient();
                                var result = await lookup.QueryAsync(domain, QueryType.A);

                                List<string> listOfIps = new List<string>();
                                foreach (var arecord in result.Answers.ARecords())
                                {
                                    listOfIps.Add(arecord.Address.ToString());
                                }

                                filledDomainsDictionary.TryAdd(domain, listOfIps);
                            }
                            else
                            {
                                domainsWithoutIps.Add(domain);
                            }                            
                        }
                    }
                    catch (Exception ex)
                    {
                        // We let it pass for now...
                        _logger.LogError($"{domain} - some problem getting the server and IP: {ex.Message}");
                    }
                }
            },
            Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 2.0)));

            return domainsWithoutIps;
        }

        private static bool IsNginx(string server)
        {
            return server.ToUpper().Contains("NGINX");
        }
    }

    public static class AsyncExtensions
    {
        public static Task ParallelForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body, int maxDop = DataflowBlockOptions.Unbounded, TaskScheduler scheduler = null)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDop
            };
            if (scheduler != null)
                options.TaskScheduler = scheduler;

            var block = new ActionBlock<T>(body, options);

            foreach (var item in source)
                block.Post(item);

            block.Complete();
            return block.Completion;

        }
    }
}
