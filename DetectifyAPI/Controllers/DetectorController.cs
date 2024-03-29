﻿using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> DomainsWithIp(List<string> domains)
        {
            _filledDomainsDictionary = new ConcurrentDictionary<string, List<string>>();

            await CheckDomains(domains, _filledDomainsDictionary, true);
            var ndic = _filledDomainsDictionary.ToDictionary(e => e.Key, e => e.Value);

            return Ok(ndic);

        }

        private async Task<List<string>> CheckDomains(List<string> domains, ConcurrentDictionary<string, List<string>> filledDomainsDictionary, bool withIps)
        {
            var _httpClient = SetUpHttpClient();

            var domainsWithoutIps = new List<string>();

            // With async: parallel on net core gets more tricky, more info in the link
            // https://timdeschryver.dev/blog/process-your-list-in-parallel-to-make-it-faster-in-dotnet
            await domains.ParallelForEachAsync(async domain =>
            {
                if (!Uri.IsWellFormedUriString(domain, UriKind.RelativeOrAbsolute))
                {
                    _logger.LogError($"{domain} - URL format is not ok");
                }
                else
                {
                    var urlb = new UriBuilder("http", domain);
                    var fullUriHost = string.Empty;
                    fullUriHost = urlb.Uri.AbsoluteUri;

                    try
                    {
                        await GetDomainInfo(filledDomainsDictionary, withIps, domain, _httpClient, domainsWithoutIps, fullUriHost);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"{domain} - some problem getting the server and IP for domain {domain}: {ex.Message}");
                    }
                }
            },
            Convert.ToInt32(Math.Ceiling(Environment.ProcessorCount * 0.75 * 2.0)));

            return domainsWithoutIps;
        }

        private static HttpClient SetUpHttpClient()
        {
            // Even with cert errors we go all in
            //https://docs.microsoft.com/en-us/dotnet/api/system.net.security.remotecertificatevalidationcallback
            System.Net.Security.RemoteCertificateValidationCallback p = (sender, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.ServerCertificateValidationCallback += p;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            var httpClientHandler = new HttpClientHandler
            {
                CheckCertificateRevocationList = false,
                AllowAutoRedirect = false
            };
            var _httpClient = new HttpClient(httpClientHandler)
            {
                Timeout = new TimeSpan(0, 0, 10)
            };
            return _httpClient;
        }

        private static async Task GetDomainInfo(ConcurrentDictionary<string, List<string>> filledDomainsDictionary, bool withIps, string domain, HttpClient _httpClient, List<string> domainsWithoutIps, string fullUriHost)
        {
            // Cut connection once response header is read, we want it NOW
            var clientResult = await _httpClient.GetAsync(fullUriHost, HttpCompletionOption.ResponseHeadersRead);
            var server = clientResult.Headers.GetValues("Server").First();

            if (IsNginx(server))
            {
                if (withIps)
                {
                    var lookup = new LookupClient();
                    var result = await lookup.QueryAsync(domain, QueryType.A);

                    var listOfIps = new List<string>();
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
