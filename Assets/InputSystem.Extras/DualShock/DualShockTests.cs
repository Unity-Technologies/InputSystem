using NUnit.Framework;

#if DEVELOPMENT_BUILD || UNITY_EDITOR

namespace ISX.DualShock
{
    public class DualShockTests : InputTestFixture
    {
        public override void Setup()
        {
            base.Setup();

            DualShockSupport.Initialize();
        }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
        [Test]
        [Category("Devices")]
        public void Devices_SupportsDualShockAsHID()
        {
            var device = InputSystem.AddDevice(new InputDeviceDescription
            {
                product = "Wireless Controller",
                manufacturer = "Sony Interactive Entertainment",
                interfaceName = "HID"
            });

            Assert.That(device, Is.TypeOf<DualShockGamepad>());
        }

#endif
    }
}

#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
