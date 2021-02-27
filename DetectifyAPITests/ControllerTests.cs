using NUnit.Framework;
using DetectifyAPI.Controllers;
using System.Collections.Generic;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace DetectifyAPITests
{
    public class ControllerTests
    {
        private DetectorController detectorController;
        private Mock<ILogger<DetectorController>> logger = new Mock<ILogger<DetectorController>>();

        [SetUp]
        public void Setup()
        {
            detectorController = new DetectorController(logger.Object);
        }

        [Test]
        public void TestOne()
        {
            // ["example.com","blog.detectify.com"]
            var domains = new List<string>(new string[] { "example.com", "www.detectify.com" });

            var res = detectorController.Domains(domains);
            var objectResult = (OkObjectResult)(res.Result);
            var list = (List<string>)objectResult.Value;

            Assert.AreNotEqual(res.Result, null);
            Assert.GreaterOrEqual(list.Count, 1);
        }

        [Test]
        public void TestOneWithIp()
        {
            // ["example.com","blog.detectify.com"]
            var domains = new List<string>(new string[] { "example.com", "www.detectify.com" });

            var res = detectorController.DomainsWithIP(domains);
            var objectResult = (OkObjectResult)(res.Result);
            var dic = (Dictionary<string, List<string>>)objectResult.Value;

            Assert.AreNotEqual(res.Result, null);
            Assert.GreaterOrEqual(dic["www.detectify.com"].Count, 1);
        }

        [Test]
        public void TestArray()
        {
            var domains = new List<string>(new string[] { "www.google.com", "www.detectify.com", "www.sl.se", "wordpress.org", "nginx.com" });

            var res = detectorController.Domains(domains);
            var objectResult = (OkObjectResult)(res.Result);
            var dic = (Dictionary<string, List<string>>)objectResult.Value;

            Assert.AreNotEqual(res.Result, null);
            Assert.GreaterOrEqual(dic["www.detectify.com"].Count, 1);
        }

        [Test]
        public void TestAlexaTop1k()
        {
            var myJsonString = File.ReadAllText("AlexaTop1k.json");
            var hostsList = JsonConvert.DeserializeObject<List<string>>(myJsonString);

            var res = detectorController.DomainsWithIP(hostsList);
            var objectResult = (OkObjectResult)(res.Result);
            var dic = (Dictionary<string, List<string>>)objectResult.Value;

            Assert.AreNotEqual(res.Result, null);
        }

        //[Test, Ignore("long execution time")]
        [Test]
        public void TestAlexaTop100()
        {
            var myJsonString = File.ReadAllText("AlexaTop1k.json");
            var hostsList = JsonConvert.DeserializeObject<List<string>>(myJsonString);

            var res = detectorController.DomainsWithIP(hostsList.GetRange(0, 100));
            var objectResult = (OkObjectResult)(res.Result);
            var dic = (Dictionary<string, List<string>>)objectResult.Value;

            Assert.AreNotEqual(res.Result, null);
        }

        [Test]
        public void TestIncorrectHostName()
        {
            var domains = new List<string>(new string[] { "www.detectifyyyyyy" });

            var res = detectorController.DomainsWithIP(domains);

            Assert.AreNotEqual(res.Result, null);
        }
    }
}