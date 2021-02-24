using NUnit.Framework;
using DetectifyAPI.Controllers;
using System.Collections.Generic;

namespace DetectifyAPITests
{
    public class ControllerTests
    {
        private DetectorController detectorController;

        [SetUp]
        public void Setup()
        {
            detectorController = new DetectorController(null);
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

            var domains = new List<string>(new string[] { "www.google.com", "www.detectify.com", "www.sl.se" });

            var res = detectorController.Domains(domains);

            Assert.AreNotEqual(res.Result, null);
        }
    }
}