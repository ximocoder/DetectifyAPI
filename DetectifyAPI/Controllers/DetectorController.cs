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
        private static HttpClient _httpClient;


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
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.CheckCertificateRevocationList = false;
            httpClientHandler.AllowAutoRedirect = false;
            //https://docs.microsoft.com/en-us/dotnet/api/system.net.security.remotecertificatevalidationcallback
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };
            _httpClient = new HttpClient(httpClientHandler);
            _httpClient.Timeout = new TimeSpan(0, 0, 10);
            List<string> domainsWithoutIps = new List<string>();

            // With async parallel on net core gets more tricky:
            // https://timdeschryver.dev/blog/process-your-list-in-parallel-to-make-it-faster-in-dotnet
            await domains.ParallelForEachAsync(async domain =>
            {                
                string uriHost = string.Empty, fullUriHost = string.Empty;
                Domain filledDomain = new Domain();

                UriBuilder urlb = new UriBuilder("http", domain);
                uriHost = urlb.Uri.Host;
                fullUriHost = urlb.Uri.AbsoluteUri;

                if (!Uri.IsWellFormedUriString(fullUriHost, UriKind.Absolute))
                {
                    _logger.LogError($"{domain} - URL format is not ok");
                }
                else
                {
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

                                filledDomain.Address = domain;
                                foreach (var arecord in result.Answers.ARecords())
                                {
                                    filledDomain.IP.Add(arecord.Address.ToString());
                                }

                                filledDomainsDictionary.TryAdd(filledDomain.Address, filledDomain.IP);
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
