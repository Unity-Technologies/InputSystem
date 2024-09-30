using NUnit.Framework;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem.Experimental
{
    public class EndpointTests
    {
        private static readonly object[] EndpointTestCases = new object[]
        {
            new object[]{ Endpoint.FromUsage(Usages.Gamepad.ButtonEast), 
                Usages.Gamepad.ButtonEast, Endpoint.AnySource, EndpointKind.Device },
            new object[]{ Endpoint.FromDeviceAndUsage(13, Usages.Gamepad.ButtonSouth), 
                Usages.Gamepad.ButtonSouth, 13, EndpointKind.Device }
        };
        
        [Test]
        [Description("Verifies endpoint encoding and decoding")]
        [TestCaseSource(nameof(EndpointTestCases))]
        public void Endpoint_Test(Endpoint endPoint, Usage expectedUsage, int expectedSourceId, EndpointKind expectedEndpointKind)
        {
            Assert.That(endPoint.usage, Is.EqualTo(expectedUsage));
            Assert.That(endPoint.sourceId, Is.EqualTo(expectedSourceId));
            Assert.That(endPoint.endpointKind, Is.EqualTo(expectedEndpointKind));
        }
    }
}