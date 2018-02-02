#if DEVELOPMENT_BUILD || UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

namespace ISX.Plugins.DualShock
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

            Assert.That(device, Is.AssignableTo<DualShockGamepad>());
        }

        [Test]
        [Category("Devices")]
        public void Devices_CanSetLightBarColorAndMotorSpeedsOnDualShockHID()
        {
            var gamepad = InputSystem.AddDevice<DualShockGamepadHID>();

            DualShockHIDOutputReport? receivedCommand = null;
            testRuntime.SetDeviceCommandCallback(gamepad.id,
                (id, commandPtr) =>
                {
                    unsafe
                    {
                        if (commandPtr->type == DualShockHIDOutputReport.Type)
                        {
                            Assert.That(receivedCommand.HasValue, Is.False);
                            receivedCommand = *((DualShockHIDOutputReport*)commandPtr);
                            return 1;
                        }

                        Assert.Fail("Received wrong type of command");
                        return InputDevice.kCommandResultFailure;
                    }
                });

            ////REVIEW: This illustrates a weekness of the current haptics API; each call results in a separate output command whereas
            ////        what the device really wants is to receive both motor speed and light bar settings in one single command

            gamepad.SetMotorSpeeds(0.1234f, 0.5678f);

            Assert.That(receivedCommand.HasValue, Is.True);
            Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.EqualTo((byte)(0.1234 * 255)));
            Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.EqualTo((byte)(0.56787 * 255)));

            receivedCommand = null;
            gamepad.SetLightBarColor(new Color(0.123f, 0.456f, 0.789f));

            Assert.That(receivedCommand.HasValue, Is.True);
            Assert.That(receivedCommand.Value.redColor, Is.EqualTo((byte)(0.123f * 255)));
            Assert.That(receivedCommand.Value.greenColor, Is.EqualTo((byte)(0.456f * 255)));
            Assert.That(receivedCommand.Value.blueColor, Is.EqualTo((byte)(0.789f * 255)));
        }

#endif
    }
}

#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
