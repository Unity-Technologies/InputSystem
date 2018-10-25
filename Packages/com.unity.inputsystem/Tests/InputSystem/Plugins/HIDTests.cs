using System.Globalization;
using NUnit.Framework;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.HID;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.TestTools.Utils;

////TODO: add test to make sure we're not grabbing HIDs that have more specific layouts

public class HIDTests : InputTestFixture
{
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
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 0, reportSizeInBits = 16 },
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.Y, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 16, reportSizeInBits = 16 },
                // 1bit primary and secondary buttons.
                new HID.HIDElementDescriptor { usage = (int)HID.Button.Primary, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 32, reportSizeInBits = 1 },
                new HID.HIDElementDescriptor { usage = (int)HID.Button.Secondary, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 33, reportSizeInBits = 1 },
            }
        };

        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                manufacturer = "TestVendor",
                product = "TestHID",
                capabilities = hidDescriptor.ToJson()
            }.ToJson());
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));

        var device = InputSystem.devices[0];
        Assert.That(device, Is.TypeOf<HID>());
        Assert.That(device.description.interfaceName, Is.EqualTo(HID.kHIDInterface));
        Assert.That(device.children, Has.Count.EqualTo(4));
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("x").And.TypeOf<AxisControl>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("y").And.TypeOf<AxisControl>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("button1").And.TypeOf<ButtonControl>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("button2").And.TypeOf<ButtonControl>());

        var x = device["x"];
        var y = device["y"];
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
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == HID.QueryHIDReportDescriptorSizeDeviceCommandType)
                        return reportDescriptor.Length;

                    if (commandPtr->type == HID.QueryHIDReportDescriptorDeviceCommandType
                        && commandPtr->payloadSizeInBytes >= reportDescriptor.Length)
                    {
                        fixed(byte* ptr = reportDescriptor)
                        {
                            UnsafeUtility.MemCpy(commandPtr->payloadPtr, ptr, reportDescriptor.Length);
                            return reportDescriptor.Length;
                        }
                    }

                    return InputDeviceCommand.kGenericFailure;
                });
        }
        // Report device.
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                manufacturer = "TestVendor",
                product = "TestHID",
                capabilities = new HID.HIDDeviceDescriptor
                {
                    vendorId = 0x54C, // Sony
                    productId = 0x9CC // PS4 Wireless Controller
                }.ToJson()
            }.ToJson(), deviceId);
        InputSystem.Update();

        // Grab device.
        var device = (HID)InputSystem.GetDeviceById(deviceId);
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
        Assert.That(device.hidDescriptor.elements[0].reportOffsetInBits, Is.EqualTo(8)); // Descriptor has report ID so that's the first thing in reports.
        Assert.That(device.hidDescriptor.elements[0].reportSizeInBits, Is.EqualTo(8));
        Assert.That(device.hidDescriptor.elements[0].logicalMin, Is.EqualTo(0));
        Assert.That(device.hidDescriptor.elements[0].logicalMax, Is.EqualTo(255));

        Assert.That(device.hidDescriptor.elements[1].usagePage, Is.EqualTo(HID.UsagePage.GenericDesktop));
        Assert.That(device.hidDescriptor.elements[1].usage, Is.EqualTo((int)HID.GenericDesktop.Y));
        Assert.That(device.hidDescriptor.elements[1].reportId, Is.EqualTo(1));
        Assert.That(device.hidDescriptor.elements[1].reportOffsetInBits, Is.EqualTo(16));
        Assert.That(device.hidDescriptor.elements[1].reportSizeInBits, Is.EqualTo(8));
        Assert.That(device.hidDescriptor.elements[1].logicalMin, Is.EqualTo(0));
        Assert.That(device.hidDescriptor.elements[1].logicalMax, Is.EqualTo(255));

        Assert.That(device.hidDescriptor.elements[4].hasNullState, Is.True);
        Assert.That(device.hidDescriptor.elements[4].physicalMax, Is.EqualTo(315));
        Assert.That(device.hidDescriptor.elements[4].unit, Is.EqualTo(0x14));

        Assert.That(device.hidDescriptor.elements[5].unit, Is.Zero);

        Assert.That(device.hidDescriptor.elements[5].reportOffsetInBits, Is.EqualTo(5 * 8 + 4));
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

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateGenericHID_FromDeviceWithParsedReportDescriptor()
    {
        var deviceId = testRuntime.AllocateDeviceId();
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == HID.QueryHIDParsedReportDescriptorDeviceCommandType)
                    {
                        var hidDescriptor = new HID.HIDDeviceDescriptor
                        {
                            usage = (int)HID.GenericDesktop.MultiAxisController,
                            usagePage = HID.UsagePage.GenericDesktop,
                            elements = new[]
                            {
                                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 0, reportSizeInBits = 16 },
                                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.Y, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 16, reportSizeInBits = 16 },
                                new HID.HIDElementDescriptor { usage = (int)HID.Button.Primary, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 32, reportSizeInBits = 1 },
                                new HID.HIDElementDescriptor { usage = (int)HID.Button.Secondary, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 33, reportSizeInBits = 1 },
                            }
                        };

                        var hidDescriptorString = hidDescriptor.ToJson();
                        var utf8 = Encoding.UTF8.GetBytes(hidDescriptorString);
                        var utf8Length = utf8.Length;

                        if (commandPtr->payloadSizeInBytes < utf8Length)
                            return -utf8Length;

                        fixed(byte* utf8Ptr = utf8)
                        {
                            UnsafeUtility.MemCpy(commandPtr->payloadPtr, utf8Ptr, utf8Length);
                        }

                        return utf8Length;
                    }
                    return -1;
                });
        }
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                manufacturer = "TestVendor",
                product = "TestHID",
            }.ToJson(), deviceId);

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));

        var device = InputSystem.devices[0];
        Assert.That(device, Is.TypeOf<HID>());
        Assert.That(device.description.interfaceName, Is.EqualTo(HID.kHIDInterface));

        var hid = (HID)device;
        Assert.That(hid.hidDescriptor.elements, Is.Not.Null);
        Assert.That(hid.hidDescriptor.elements.Length, Is.EqualTo(4));

        Assert.That(device.children, Has.Count.EqualTo(4));
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("x").And.TypeOf<AxisControl>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("y").And.TypeOf<AxisControl>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("button1").And.TypeOf<ButtonControl>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("button2").And.TypeOf<ButtonControl>());
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

        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            interfaceName = HID.kHIDInterface,
            manufacturer = "TestVendor",
            product = "TestHID",
            capabilities = hidDescriptor.ToJson()
        }.ToJson());
        InputSystem.Update();

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

        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                manufacturer = "TestVendor",
                product = "TestHID",
                capabilities = hidDescriptor.ToJson()
            }.ToJson());
        InputSystem.Update();

        var device = (HID)InputSystem.devices.First(x => x is HID);
        Assert.That(device.hidDescriptor.productId, Is.EqualTo(1234));
        Assert.That(device.hidDescriptor.vendorId, Is.EqualTo(5678));
        Assert.That(device.hidDescriptor.elements.Length, Is.EqualTo(1));
    }

    [StructLayout(LayoutKind.Explicit)]
    struct SimpleAxisState : IInputStateTypeInfo
    {
        [FieldOffset(0)] public byte reportId;
        [FieldOffset(1)] public ushort x;
        [FieldOffset(3)] public short y;
        [FieldOffset(5)] public byte rx;
        [FieldOffset(6)] public sbyte ry;
        [FieldOffset(7)] public ushort vx;
        [FieldOffset(9)] public short vy;

        public FourCC GetFormat()
        {
            return new FourCC('H', 'I', 'D');
        }
    }

    // There's too little data in HID descriptors to reliably normalize and center HID axes. For example,
    // a PS4 controller will report the left stick as X and Y, the right stick as Z and Rz, and the triggers
    // as Rx and Ry. Each of these will be reported as a single byte with a [0..255] range. However, the
    // triggers need to be centered at 0 (i.e. byte 0) and go from [0..1] whereas the left and right stick
    // need to be centered at 0 (i.e. byte 127) and go from [-1..1]. From the data in the HID descriptor this
    // is impossible to differentiate automatically and a different piece of hardware may well use the same
    // axes in a different way.
    //
    // So we have to make a choice to go one way or the other. Given that the sticks are more important to
    // work out of the box than the triggers, we lean that way and accept the triggers misbehaving (i.e.
    // ending up being centered when half pressed). This way we can at least make joysticks behave correctly
    // out of the box.
    //
    // The only reliable fix for a device is to put a layout in place that provides the missing data
    // (i.e. how to interpret axis values) to the system.
    [Test]
    [Category("Devices")]
    public void Devices_HIDAxesAreCenteredBetweenMinAndMax()
    {
        // Make up a HID that has both 16bit and 8bit axes in both signed and unsigned form.
        var hidDescriptor =
            new HID.HIDDeviceDescriptorBuilder(HID.GenericDesktop.MultiAxisController)
                .StartReport(HID.HIDReportType.Input)
                // 16bit [0..65535]
                .AddElement(HID.GenericDesktop.X, 16).WithLogicalMinMax(0, 65535)
                // 16bit [-32768..32767]
                .AddElement(HID.GenericDesktop.Y, 16).WithLogicalMinMax(-32768, 32767)
                // 8bit [0..255]
                .AddElement(HID.GenericDesktop.Rx, 8).WithLogicalMinMax(0, 255)
                // 8bit [-128..127]
                .AddElement(HID.GenericDesktop.Ry, 8).WithLogicalMinMax(-128, 127)
                // 16bit [0..10000]
                .AddElement(HID.GenericDesktop.Vx, 16).WithLogicalMinMax(0, 10000)
                // 16bit [-10000..10000]
                .AddElement(HID.GenericDesktop.Vy, 16).WithLogicalMinMax(-10000, 10000)
                .Finish();

        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                manufacturer = "TestVendor",
                product = "TestHID",
                capabilities = hidDescriptor.ToJson()
            }.ToJson());
        InputSystem.Update();

        var device = InputSystem.devices[0];

        // Test lower bound.
        InputSystem.QueueStateEvent(device, new SimpleAxisState
        {
            reportId = 1,
            x = ushort.MinValue,
            y = short.MinValue,
            rx = byte.MinValue,
            ry = sbyte.MinValue,
            vx = 0,
            vy = -10000,
        });
        InputSystem.Update();

        Assert.That(device["X"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001));
        Assert.That(device["Y"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001));
        Assert.That(device["Rx"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001));
        Assert.That(device["Ry"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001));
        Assert.That(device["Vx"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001));
        Assert.That(device["Vy"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001));

        // Test upper bound.
        InputSystem.QueueStateEvent(device, new SimpleAxisState
        {
            reportId = 1,
            x = ushort.MaxValue,
            y = short.MaxValue,
            rx = byte.MaxValue,
            ry = sbyte.MaxValue,
            vx = 10000,
            vy = 10000,
        });
        InputSystem.Update();

        Assert.That(device["X"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001));
        Assert.That(device["Y"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001));
        Assert.That(device["Rx"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001));
        Assert.That(device["Ry"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001));
        Assert.That(device["Vx"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001));
        Assert.That(device["Vy"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001));

        // Test center.
        InputSystem.QueueStateEvent(device, new SimpleAxisState
        {
            reportId = 1,
            x = ushort.MaxValue / 2,
            y = 0,
            rx = byte.MaxValue / 2,
            ry = 0,
            vx = 10000 / 2,
            vy = 0,
        });
        InputSystem.Update();

        Assert.That(device["X"].ReadValueAsObject(), Is.EqualTo(0).Within(0.0001));
        Assert.That(device["Y"].ReadValueAsObject(), Is.EqualTo(0).Within(0.0001));
        ////FIXME: these accumulate some rather large errors
        Assert.That(device["Rx"].ReadValueAsObject(), Is.EqualTo(0).Within(0.004));
        Assert.That(device["Ry"].ReadValueAsObject(), Is.EqualTo(0).Within(0.004));
        Assert.That(device["Vx"].ReadValueAsObject(), Is.EqualTo(0).Within(0.0001));
        Assert.That(device["Vy"].ReadValueAsObject(), Is.EqualTo(0).Within(0.0001));
    }

    // https://github.com/Unity-Technologies/InputSystem/issues/134
    [Test]
    [Category("Devices")]
    public void Devices_HIDAxisLimits_DoNotUseDecimalFormatOfCurrentCulture()
    {
        var oldCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            // French locale uses comma as decimal separator.
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

            var hidDescriptor =
                new HID.HIDDeviceDescriptorBuilder(HID.GenericDesktop.MultiAxisController)
                    .StartReport(HID.HIDReportType.Input)
                    .AddElement(HID.GenericDesktop.X, 16).WithLogicalMinMax(0, 65535).Finish();

            testRuntime.ReportNewInputDevice(
                new InputDeviceDescription
                {
                    interfaceName = HID.kHIDInterface,
                    manufacturer = "TestVendor",
                    product = "TestHID",
                    capabilities = hidDescriptor.ToJson()
                }.ToJson());

            Assert.That(() => InputSystem.Update(), Throws.Nothing);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = oldCulture;
        }
    }

    // Would be nicer to just call them "HID" but ATM the layout builder mechanism doesn't have
    // direct control over the naming.
    [Test]
    [Category("Devices")]
    public void Devices_HIDsWithoutProductName_AreNamedByTheirVendorAndProductID()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            usage = (int)HID.GenericDesktop.MultiAxisController,
            usagePage = HID.UsagePage.GenericDesktop,
            vendorId = 0x1234,
            productId = 0x5678,
            inputReportSize = 4,
            elements = new[]
            {
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 32 },
            }
        };

        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());
        InputSystem.Update();

        var device = (HID)InputSystem.devices.First(x => x is HID);
        Assert.That(device.name, Is.EqualTo("1234-5678"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_HIDDescriptorSurvivesReload()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            usage = (int)HID.GenericDesktop.MultiAxisController,
            usagePage = HID.UsagePage.GenericDesktop,
            vendorId = 0x1234,
            productId = 0x5678,
            inputReportSize = 4,
            elements = new[]
            {
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportSizeInBits = 32 },
            }
        };

        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());
        InputSystem.Update();

        InputSystem.SaveAndReset();
        InputSystem.Restore();

        var hid = (HID)InputSystem.devices.First(x => x is HID);

        Assert.That(hid.hidDescriptor.vendorId, Is.EqualTo(0x1234));
        Assert.That(hid.hidDescriptor.productId, Is.EqualTo(0x5678));
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_SupportsHIDHatSwitches()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            usage = (int)HID.GenericDesktop.MultiAxisController,
            usagePage = HID.UsagePage.GenericDesktop,
            vendorId = 0x1234,
            productId = 0x5678,
            inputReportSize = 4,
            elements = new[]
            {
                new HID.HIDElementDescriptor
                {
                    usage = (int)HID.GenericDesktop.HatSwitch,
                    usagePage = HID.UsagePage.GenericDesktop,
                    reportType = HID.HIDReportType.Input,
                    reportId = 1,
                    reportSizeInBits = 4,
                    reportOffsetInBits = 0,
                    logicalMin = 0,
                    logicalMax = 7, // This combination of min/max means that 8 (given we have 4 bits) is out of range and thus the null state.
                    physicalMin = 0,
                    physicalMax = 315,
                    flags = HID.HIDElementFlags.NullState
                }
            }
        };

        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());

        InputSystem.Update();
        var hid = (HID)InputSystem.devices.First(x => x is HID);

        Assert.That(hid["dpad"], Is.TypeOf<DpadControl>());

        // Assert that default state is set correctly.
        Assert.That(hid["dpad/up"].ReadValueAsObject(), Is.EqualTo(0).Within(0.00001));
        Assert.That(hid["dpad/down"].ReadValueAsObject(), Is.EqualTo(0).Within(0.00001));
        Assert.That(hid["dpad/left"].ReadValueAsObject(), Is.EqualTo(0).Within(0.00001));
        Assert.That(hid["dpad/right"].ReadValueAsObject(), Is.EqualTo(0).Within(0.00001));

        InputEventPtr eventPtr;
        using (StateEvent.From(hid, out eventPtr))
        {
            var stateData = (byte*)StateEvent.From(eventPtr)->state;

            const int kNull = 8;
            const int kUp = 0;
            const int kUpRight = 1;
            const int kRight = 2;
            const int kRightDown = 3;
            const int kDown = 4;
            const int kDownLeft = 5;
            const int kLeft = 6;
            const int kLeftUp = 7;

            stateData[0] = kNull;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["dpad"].ReadValueAsObject(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kUp;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["dpad"].ReadValueAsObject(), Is.EqualTo(Vector2.up).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kUpRight;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["dpad"].ReadValueAsObject(), Is.EqualTo((Vector2.up + Vector2.right).normalized).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kRight;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["dpad"].ReadValueAsObject(), Is.EqualTo(Vector2.right).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kRightDown;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["dpad"].ReadValueAsObject(), Is.EqualTo((Vector2.right + Vector2.down).normalized).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kDown;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["dpad"].ReadValueAsObject(), Is.EqualTo(Vector2.down).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kDownLeft;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["dpad"].ReadValueAsObject(), Is.EqualTo((Vector2.down + Vector2.left).normalized).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kLeft;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["dpad"].ReadValueAsObject(), Is.EqualTo(Vector2.left).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kLeftUp;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["dpad"].ReadValueAsObject(), Is.EqualTo((Vector2.left + Vector2.up).normalized).Using(Vector2EqualityComparer.Instance));
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsMultipleHIDHatSwitches()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            usage = (int)HID.GenericDesktop.MultiAxisController,
            usagePage = HID.UsagePage.GenericDesktop,
            vendorId = 0x1234,
            productId = 0x5678,
            inputReportSize = 4,
            elements = new[]
            {
                new HID.HIDElementDescriptor
                {
                    usage = (int)HID.GenericDesktop.HatSwitch,
                    usagePage = HID.UsagePage.GenericDesktop,
                    reportType = HID.HIDReportType.Input,
                    reportId = 1,
                    reportSizeInBits = 4,
                    reportOffsetInBits = 0,
                    logicalMin = 0,
                    logicalMax = 7,
                    physicalMin = 0,
                    physicalMax = 315,
                    flags = HID.HIDElementFlags.NullState
                },
                new HID.HIDElementDescriptor
                {
                    usage = (int)HID.GenericDesktop.HatSwitch,
                    usagePage = HID.UsagePage.GenericDesktop,
                    reportType = HID.HIDReportType.Input,
                    reportId = 1,
                    reportSizeInBits = 4,
                    reportOffsetInBits = 4,
                    logicalMin = 0,
                    logicalMax = 7,
                    physicalMin = 0,
                    physicalMax = 315,
                    flags = HID.HIDElementFlags.NullState
                }
            }
        };

        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());

        InputSystem.Update();
        var hid = (HID)InputSystem.devices.First(x => x is HID);

        Assert.That(hid["dpad"], Is.TypeOf<DpadControl>());
        Assert.That(hid["dpad1"], Is.TypeOf<DpadControl>());

        Assert.That(hid["dpad"].stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(hid["dpad"].stateBlock.bitOffset, Is.EqualTo(0));
        Assert.That(hid["dpad1"].stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(hid["dpad1"].stateBlock.bitOffset, Is.EqualTo(4));
    }

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_SupportsHIDDpads()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_GenericHIDJoystickIsTurnedIntoJoystick()
    {
        Assert.Fail();
    }

    // Based on the HID spec, we can't make *any* guarantees on where a HID-only gamepad puts its axes
    // and buttons. Generic buttons in HID are simply numbered with no specific meaning and axes equally
    // carry no guarantees about how they are arranged on a device.
    //
    // This means we cannot turn a HID-only gamepad into a `Gamepad` instance and make any guarantees
    // about buttonSouth etc or leftStick etc. So, we opt to not turn HID-only gamepads into gamepads at
    // all but rather turn them into joysticks instead.
    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_GenericHIDGamepadIsTurnedIntoJoystick()
    {
        Assert.Fail();
    }

    // It should be possible to reuse parts of the HID layout builder for building custom HID-based layouts
    // without having to individually hardwire each element. Or at least it should be possible to leverage
    // the descriptor processing part of the HID layout builder to help building layouts.
    [Test]
    [Category("Layouts")]
    [Ignore("TODO")]
    public void TODO_Layouts_CanBuildCustomLayoutsBasedOnTheHIDLayoutBuilder()
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
