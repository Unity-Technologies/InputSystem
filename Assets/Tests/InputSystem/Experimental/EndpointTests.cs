using NUnit.Framework;
using UnityEngine.InputSystem.Experimental;
using Usages = UnityEngine.InputSystem.Experimental.Devices.Usages;

namespace Tests.InputSystem.Experimental
{
    public class EndpointTests
    {
        private static readonly object[] EndpointTestCases = new object[]
        {
            new object[]{ Endpoint.FromUsage(Usages.GamepadUsages.ButtonEast), 
                Usages.GamepadUsages.ButtonEast, Endpoint.AnySource, SourceType.Device },
            new object[]{ Endpoint.FromDeviceAndUsage(13, Usages.GamepadUsages.ButtonSouth), 
                Usages.GamepadUsages.ButtonSouth, 13, SourceType.Device }
        };
        
        [Test]
        [Description("Verifies endpoint encoding and decoding")]
        [TestCaseSource(nameof(EndpointTestCases))]
        public void Endpoint_Test(Endpoint endPoint, Usage expectedUsage, int expectedSourceId, SourceType expectedSourceType)
        {
            Assert.That(endPoint.usage, Is.EqualTo(expectedUsage));
            Assert.That(endPoint.sourceId, Is.EqualTo(expectedSourceId));
            Assert.That(endPoint.sourceType, Is.EqualTo(expectedSourceType));
        }
    }
}