using NUnit.Framework;
using UnityEngine;

namespace ISX.HID
{
    public class HIDTests : InputTestsBase
    {
        [Test]
        [Category("Devices")]
        public void TODO_Devices_CanCreateGenericHID()
        {
            // Construct a HID descriptor for a bogus multi-axis controller.
            var hidDescriptor = new HID.HIDDeviceDescriptor
            {
                usageId = (int)HID.GenericDesktop.MultiAxisController,
                usagePageId = (int)HID.UsagePage.GenericDesktop,
                elements = new[]
                {
                    // 16bit X and Y axes.
                    new HID.HIDElementDescriptor { usageId = (int)HID.GenericDesktop.X, usagePageId = (int)HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 16 },
                    new HID.HIDElementDescriptor { usageId = (int)HID.GenericDesktop.Y, usagePageId = (int)HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 16 },
                    // 1bit primary and secondary buttons.
                    new HID.HIDElementDescriptor { usageId = (int)HID.Button.Primary, usagePageId = (int)HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 1 },
                    new HID.HIDElementDescriptor { usageId = (int)HID.Button.Secondary, usagePageId = (int)HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 1 },
                }
            };

            InputSystem.ReportAvailableDevice(
                new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                product = "MyHIDThing",
                capabilities = JsonUtility.ToJson(hidDescriptor)
            });

            Assert.That(InputSystem.devices, Has.Count.EqualTo(1));

            var device = InputSystem.devices[0];
            Assert.That(device.description.interfaceName, Is.EqualTo(HID.kHIDInterface));
            Assert.That(device.children, Has.Count.EqualTo(4));
            Assert.That(InputControlPath.FindControl(device, "x"), Is.TypeOf<AxisControl>());
            Assert.That(InputControlPath.FindControl(device, "y"), Is.TypeOf<AxisControl>());
            Assert.That(InputControlPath.FindControl(device, "button1"), Is.TypeOf<ButtonControl>());
            Assert.That(InputControlPath.FindControl(device, "button2"), Is.TypeOf<AxisControl>());
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
