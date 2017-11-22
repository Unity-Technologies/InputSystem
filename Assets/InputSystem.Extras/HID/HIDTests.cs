using NUnit.Framework;

namespace ISX.HID
{
    public class HIDTests : InputTestFixture
    {
        public override void Setup()
        {
            base.Setup();

            HIDSupport.Initialize();
        }

        [Test]
        [Category("Devices")]
        public void Devices_CanCreateGenericHID()
        {
            // Construct a HID descriptor for a bogus multi-axis controller.
            var hidDescriptor = new HID.HIDDeviceDescriptor
            {
                usage = (int)HID.GenericDesktop.MultiAxisController,
                usagePage = HID.UsagePage.GenericDesktop,
                elements = new[]
                {
                    // 16bit X and Y axes.
                    new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 16 },
                    new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.Y, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 16 },
                    // 1bit primary and secondary buttons.
                    new HID.HIDElementDescriptor { usage = (int)HID.Button.Primary, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 1 },
                    new HID.HIDElementDescriptor { usage = (int)HID.Button.Secondary, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 1 },
                }
            };

            InputSystem.ReportAvailableDevice(
                new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                product = "MyHIDThing",
                capabilities = hidDescriptor.ToJson()
            });

            Assert.That(InputSystem.devices, Has.Count.EqualTo(1));

            var device = InputSystem.devices[0];
            Assert.That(device.description.interfaceName, Is.EqualTo(HID.kHIDInterface));
            Assert.That(device.children, Has.Count.EqualTo(4));
            Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("X").And.TypeOf<AxisControl>());
            Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("Y").And.TypeOf<AxisControl>());
            Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("button1").And.TypeOf<ButtonControl>());
            Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("button2").And.TypeOf<ButtonControl>());

            ////TODO: test sending events against HIDs to make sure we get the control offsets right
        }

        [Test]
        [Category("Devices")]
        public void TODO_Devices_GenericHIDJoystickIsTurnedIntoJoystick()
        {
            Assert.Fail();
        }

        [Test]
        [Category("Devices")]
        public void TODO_Devices_GenericHIDGamepadIsTurnedIntoJoystick()
        {
            Assert.Fail();
        }
    }
}
