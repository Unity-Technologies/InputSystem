#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using NUnit.Framework;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;

////REVIEW: Is there some way we can center axis controls of HIDs correctly and automatically?
////        If this code picks up a PS4 controller, for example, then X and Y are the left stick,
////        Z and Rz are the right stick, and Rx and Ry are the triggers. The triggers work fine.
////        They go from 0 to 1. However, the axes on the sticks end up centered at 0.5 instead
////        of going from -1 to 1 with the center at 0. The problem is that from the HID descriptor
////        data we can't really tell. All those axes look exactly identical. In the HID spec, too;
////        there's no special meaning applied to the individual axes.
////
////        Maybe it's good enough to center X and Y in the -1..1 range. Will do the trick for
////        joysticks.

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
                    new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportBitOffset = 0, reportSizeInBits = 16 },
                    new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.Y, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportBitOffset = 16, reportSizeInBits = 16 },
                    // 1bit primary and secondary buttons.
                    new HID.HIDElementDescriptor { usage = (int)HID.Button.Primary, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1, reportBitOffset = 32, reportSizeInBits = 1 },
                    new HID.HIDElementDescriptor { usage = (int)HID.Button.Secondary, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1, reportBitOffset = 33, reportSizeInBits = 1 },
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

        [Test]
        [Category("Devices")]
        public void Devices_CanCreateGenericHID_FromDeviceWithBinaryReportDescriptor()
        {
            // This is several snippets from the PS4 controller's HID report descriptor
            // pasted together.
            var reportDescriptor = new byte[]
            {
                0x05, 0x01, // Usage Page (Generic Desktop)
                0x09, 0x05, // Usage (Gamepad)
                0xA1, 0x01, // Collection (Application)
                0x85, 0x01,     // Report ID (1)
                0x09, 0x30,     // Usage (X)
                0x09, 0x31,     // Usage (Y)
                0x09, 0x32,     // Usage (Z)
                0x09, 0x35,     // Usage (Rz)
                0x15, 0x00,     // Logical Minimum (0)
                0x26, 0xFF, 0x00,     // Logical Maximum (255)
                0x75, 0x08,     // Report Size (8)
                0x95, 0x04,     // Report Count (4)
                0x81, 0x02,     // Input (Data, Var, Abs, NWrp, Lin, Pref, NNul, Bit)
                0x09, 0x39,     // Usage (Hat Switch)
                0x15, 0x00,     // Logical Minimum (0)
                0x25, 0x07,     // Logical Maximum (7)
                0x35, 0x00,     // Physical Maximum (0)
                0x46, 0x3B, 0x01,     // Physical Maximum (315)
                0x65, 0x14,     // Unit (Eng Rot: Degree)
                0x75, 0x04,     // Report Size (4)
                0x95, 0x01,     // Report Count (1)
                0x81, 0x42,     // Input (Data, Var, Abs, NWrp, Lin, Pref, Null, Bit)
                0x65, 0x00,     // Unit (None)
                0x05, 0x09,     // Usage Page (Button)
                0x19, 0x01,     // Usage Minimum (Button 1)
                0x29, 0x0E,     // Usage Maximum (Button 14)
                0x15, 0x00,     // Logical Minimum (0)
                0x25, 0x01,     // Logical Maximum (1)
                0x75, 0x01,     // Report Size (1)
                0x95, 0x0E,     // Report Count (14)
                0x81, 0x02,     // Input (Data, Var, Abs, NWrp, Lin, Pref, NNul, Bit)
                0x06, 0x00, 0xFF,     // Usage Page (Vendor-Defined 1)
                0x09, 0x21,     // Usage (Vendor-Defined 33)
                0x95, 0x36,     // Report Count (54)
                0x81, 0x02,     // Input (Data, Var, Abs, NWrp, Lin, Pref, NNul, Bit)
                0x85, 0x05,     // Report ID (5)
                0x09, 0x22,     // Usage (Vendor-Defined 34)
                0x95, 0x1F,     // Report Count (31)
                0x91, 0x02,     // Output (Data, Var, Abs, NWrp, Lin, Pref, NNul, NVol, Bit)
                0xC0, // End Collection
            };

            const int kNumElements = 4 + 1 + 14 + 54 + 31;

            // The HID report descriptor is fetched from the device via an IOCTL.
            var deviceId = testRuntime.AllocateDeviceId();
            testRuntime.SetIOCTLCallback(deviceId,
                (code, buffer, bufferSize) =>
                {
                    if (code == HID.IOCTLQueryHIDReportDescriptorSize)
                        return reportDescriptor.Length;

                    if (code == HID.IOCTLQueryHIDReportDescriptor
                        && bufferSize >= reportDescriptor.Length)
                    {
                        unsafe
                        {
                            fixed(byte* ptr = reportDescriptor)
                            {
                                UnsafeUtility.MemCpy(buffer.ToPointer(), ptr, reportDescriptor.Length);
                                return reportDescriptor.Length;
                            }
                        }
                    }
                    return InputDevice.kIOCTLFailure;
                });

            // Report device.
            testRuntime.ReportNewInputDevice(
                new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                product = "TestHID",
                capabilities = new HID.HIDDeviceDescriptor
                {
                    vendorId = 0x54C,     // Sony
                    productId = 0x9CC     // PS4 Wireless Controller
                }.ToJson()
            }.ToJson(), deviceId);
            InputSystem.Update();

            // Grab device.
            var device = (HID)InputSystem.TryGetDeviceById(deviceId);
            Assert.That(device, Is.Not.Null);
            Assert.That(device, Is.TypeOf<HID>());

            // Check HID descriptor.
            Assert.That(device.hidDescriptor.vendorId, Is.EqualTo(0x54C));
            Assert.That(device.hidDescriptor.productId, Is.EqualTo(0x9CC));
            Assert.That(device.hidDescriptor.usagePage, Is.EqualTo(HID.UsagePage.GenericDesktop));
            Assert.That(device.hidDescriptor.usage, Is.EqualTo((int)HID.GenericDesktop.Gamepad));
            Assert.That(device.hidDescriptor.elements.Length, Is.EqualTo(kNumElements));

            Assert.That(device.hidDescriptor.elements[0].usagePage, Is.EqualTo(HID.UsagePage.GenericDesktop));
            Assert.That(device.hidDescriptor.elements[0].usage, Is.EqualTo((int)HID.GenericDesktop.X));
            Assert.That(device.hidDescriptor.elements[0].reportId, Is.EqualTo(1));
            Assert.That(device.hidDescriptor.elements[0].reportBitOffset, Is.EqualTo(8)); // Descriptor has report ID so that's the first thing in reports.
            Assert.That(device.hidDescriptor.elements[0].reportSizeInBits, Is.EqualTo(8));
            Assert.That(device.hidDescriptor.elements[0].logicalMin, Is.EqualTo(0));
            Assert.That(device.hidDescriptor.elements[0].logicalMax, Is.EqualTo(255));

            Assert.That(device.hidDescriptor.elements[1].usagePage, Is.EqualTo(HID.UsagePage.GenericDesktop));
            Assert.That(device.hidDescriptor.elements[1].usage, Is.EqualTo((int)HID.GenericDesktop.Y));
            Assert.That(device.hidDescriptor.elements[1].reportId, Is.EqualTo(1));
            Assert.That(device.hidDescriptor.elements[1].reportBitOffset, Is.EqualTo(16));
            Assert.That(device.hidDescriptor.elements[1].reportSizeInBits, Is.EqualTo(8));
            Assert.That(device.hidDescriptor.elements[1].logicalMin, Is.EqualTo(0));
            Assert.That(device.hidDescriptor.elements[1].logicalMax, Is.EqualTo(255));

            Assert.That(device.hidDescriptor.elements[4].hasNullState, Is.True);
            Assert.That(device.hidDescriptor.elements[4].physicalMax, Is.EqualTo(315));
            Assert.That(device.hidDescriptor.elements[4].unit, Is.EqualTo(0x14));

            Assert.That(device.hidDescriptor.elements[5].unit, Is.Zero);

            Assert.That(device.hidDescriptor.elements[5].reportBitOffset, Is.EqualTo(5 * 8 + 4));
            Assert.That(device.hidDescriptor.elements[5].usagePage, Is.EqualTo(HID.UsagePage.Button));
            Assert.That(device.hidDescriptor.elements[6].usagePage, Is.EqualTo(HID.UsagePage.Button));
            Assert.That(device.hidDescriptor.elements[7].usagePage, Is.EqualTo(HID.UsagePage.Button));
            Assert.That(device.hidDescriptor.elements[5].usage, Is.EqualTo(1));
            Assert.That(device.hidDescriptor.elements[6].usage, Is.EqualTo(2));
            Assert.That(device.hidDescriptor.elements[7].usage, Is.EqualTo(3));

            Assert.That(device.hidDescriptor.collections.Length, Is.EqualTo(1));
            Assert.That(device.hidDescriptor.collections[0].type, Is.EqualTo(HID.HIDCollectionType.Application));
            Assert.That(device.hidDescriptor.collections[0].childCount, Is.EqualTo(kNumElements));

            ////TODO: check hat switch
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
        public void Devices_CanGetDescriptorFromHID()
        {
            var hidDescriptor = new HID.HIDDeviceDescriptor
            {
                usage = (int)HID.GenericDesktop.MultiAxisController,
                usagePage = HID.UsagePage.GenericDesktop,
                productId = 1234,
                vendorId = 5678,
                elements = new[]
                {
                    new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 32 },
                }
            };

            InputSystem.ReportAvailableDevice(
                new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                product = "MyHIDThing",
                capabilities = hidDescriptor.ToJson()
            });

            var device = (HID)InputSystem.devices.First(x => x is HID);
            Assert.That(device.hidDescriptor.productId, Is.EqualTo(1234));
            Assert.That(device.hidDescriptor.vendorId, Is.EqualTo(5678));
            Assert.That(device.hidDescriptor.elements.Length, Is.EqualTo(1));
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

        [Test]
        [Category("Utilities")]
        public void Utilities_CanRecognizeVendorDefinedUsages()
        {
            string usagePage;
            string usage;

            HID.UsageToString((HID.UsagePage) 0xff01, 0x33, out usagePage, out usage);

            Assert.That(usagePage, Is.EqualTo("Vendor-Defined"));
            Assert.That(usage, Is.EqualTo("Vendor-Defined"));
        }
    }
}
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
