using NUnit.Framework;
using DetectifyAPI.Controllers;
using System.Collections.Generic;
using Moq;
using Microsoft.Extensions.Logging;

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

            var domains = new List<string>(new string[] { "www.detectify.com" });

            var res = detectorController.Domains(domains);

            Assert.AreNotEqual(res.Result, null);
        }

        [Test]
        public void TestArray()
        {

            var domains = new List<string>(new string[] { "www.google.com", "www.detectify.com", "www.sl.se", "wordpress.org", "nginx.com" });

            var res = detectorController.Domains(domains);

            var objectResult = ((Microsoft.AspNetCore.Mvc.ObjectResult)res.Result);

            Assert.AreNotEqual(res.Result, null);
        }

        [Test]
        public void TestIncorrectHostName()
        {

            var domains = new List<string>(new string[] { "www.detectifyyyyyy" });

            var res = detectorController.Domains(domains);


            Assert.AreNotEqual(res.Result, null);
        }
    }
}