using NUnit.Framework;
using System.Linq;

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

            var x = device["X"];
            var y = device["Y"];
            var button1 = device["button1"];
            var button2 = device["button2"];

            Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(5 * 8));

            Assert.That(x.stateBlock.byteOffset, Is.Zero);
            Assert.That(y.stateBlock.byteOffset, Is.EqualTo(2));
            Assert.That(x.stateBlock.bitOffset, Is.Zero);
            Assert.That(y.stateBlock.bitOffset, Is.Zero);

            Assert.That(button1.stateBlock.byteOffset, Is.EqualTo(4));
            Assert.That(button2.stateBlock.byteOffset, Is.EqualTo(4));
            Assert.That(button1.stateBlock.bitOffset, Is.EqualTo(0));
            Assert.That(button2.stateBlock.bitOffset, Is.EqualTo(1));
        }

        // There may be vendor-specific stuff in an input report which we don't know how to use so the
        // set of usable elements may be smaller than the set of actual elements in the report. The system
        // is fine with state events that are larger than the state we store for a device as long as the
        // format codes match. So, the total size of the state block for a device should correspond to
        // only the range of elements we actually use.
        [Test]
        [Category("Devices")]
        public void Devices_HIDsIgnoreUnusedExcessElements()
        {
            var hidDescriptor = new HID.HIDDeviceDescriptor
            {
                usage = (int)HID.GenericDesktop.MultiAxisController,
                usagePage = HID.UsagePage.GenericDesktop,
                inputReportSize = 36,
                elements = new[]
                {
                    new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 32 },
                    new HID.HIDElementDescriptor { usage = 0x23435, usagePage = (HID.UsagePage) 0x544314, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 32 }
                }
            };

            InputSystem.ReportAvailableDevice(
                new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                product = "MyHIDThing",
                capabilities = hidDescriptor.ToJson()
            });

            var device = InputSystem.devices.First(x => x is HID);
            Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(32));
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
