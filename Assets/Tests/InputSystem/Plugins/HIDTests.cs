using System.Globalization;
using NUnit.Framework;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.HID;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

////TODO: add test to make sure we're not grabbing HIDs that have more specific layouts

// No HID devices on Android
#if (!UNITY_ANDROID && !UNITY_IOS) || UNITY_EDITOR
internal class HIDTests : InputTestFixture
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

        runtime.ReportNewInputDevice(
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
        Assert.That(device.children, Has.Count.EqualTo(3));
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("trigger").And.TypeOf<ButtonControl>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("button2").And.TypeOf<ButtonControl>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("stick").And.TypeOf<StickControl>());

        var x = device["stick/x"];
        var y = device["stick/y"];
        var button1 = device["trigger"];
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
    public void Devices_DevicesNotAllowedBySupportedHIDUsagesAreSkipped()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            usage = 1234,
            usagePage = (HID.UsagePage) 5678,
            // need at least one valid element for the device not to be ignored
            elements = new[]
            {
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 0, reportSizeInBits = 16 },
            }
        };

        var descriptionJson = new InputDeviceDescription
        {
            interfaceName = HID.kHIDInterface,
            manufacturer = "TestVendor",
            product = "TestHID",
            capabilities = hidDescriptor.ToJson()
        }.ToJson();
        var deviceId = runtime.AllocateDeviceId();

        runtime.ReportNewInputDevice(descriptionJson, deviceId);
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(0));
        Assert.That(InputSystem.GetDeviceById(deviceId), Is.Null);
        Assert.That(InputSystem.GetUnsupportedDevices(), Has.Count.EqualTo(1));
        Assert.That(InputSystem.GetUnsupportedDevices()[0].product, Is.EqualTo("TestHID"));

        HIDSupport.supportedHIDUsages = new ReadOnlyArray<HIDSupport.HIDPageUsage>(
            new[] {new HIDSupport.HIDPageUsage((HID.UsagePage) 5678, 1234)}
        );

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.GetDeviceById(deviceId), Is.Not.Null);
        Assert.That(InputSystem.devices[0], Is.TypeOf<HID>());
        Assert.That(InputSystem.devices[0].description.product, Is.EqualTo("TestHID"));
        Assert.That(((HID)InputSystem.devices[0]).hidDescriptor.usagePage, Is.EqualTo((HID.UsagePage) 5678));
        Assert.That(((HID)InputSystem.devices[0]).hidDescriptor.usage, Is.EqualTo(1234));

        HIDSupport.supportedHIDUsages = new ReadOnlyArray<HIDSupport.HIDPageUsage>(
            new[] {new HIDSupport.HIDPageUsage((HID.UsagePage) 5678, 1234)}
        );

        // No change.
        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.GetDeviceById(deviceId), Is.Not.Null);
        Assert.That(InputSystem.devices[0], Is.TypeOf<HID>());
        Assert.That(InputSystem.devices[0].description.product, Is.EqualTo("TestHID"));
        Assert.That(((HID)InputSystem.devices[0]).hidDescriptor.usagePage, Is.EqualTo((HID.UsagePage) 5678));
        Assert.That(((HID)InputSystem.devices[0]).hidDescriptor.usage, Is.EqualTo(1234));

        // Add another.
        var descriptionJson2 = new InputDeviceDescription
        {
            interfaceName = HID.kHIDInterface,
            manufacturer = "TestVendor",
            product = "OtherTestHID",
            capabilities = hidDescriptor.ToJson()
        }.ToJson();
        var deviceId2 = runtime.AllocateDeviceId();

        runtime.ReportNewInputDevice(descriptionJson2, deviceId2);
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(2));
        Assert.That(InputSystem.GetDeviceById(deviceId2), Is.Not.Null);
        Assert.That(InputSystem.devices[1], Is.TypeOf<HID>());
        Assert.That(InputSystem.devices[1].description.product, Is.EqualTo("OtherTestHID"));
        Assert.That(((HID)InputSystem.devices[1]).hidDescriptor.usagePage, Is.EqualTo((HID.UsagePage) 5678));
        Assert.That(((HID)InputSystem.devices[1]).hidDescriptor.usage, Is.EqualTo(1234));

        HIDSupport.supportedHIDUsages = new ReadOnlyArray<HIDSupport.HIDPageUsage>();

        Assert.That(InputSystem.devices, Is.Empty);
        Assert.That(InputSystem.GetDeviceById(deviceId), Is.Null);
        Assert.That(InputSystem.GetDeviceById(deviceId2), Is.Null);
        Assert.That(InputSystem.GetUnsupportedDevices(), Has.Count.EqualTo(2));
        Assert.That(InputSystem.GetUnsupportedDevices(), Has.Exactly(1).With.Property("product").EqualTo("TestHID"));
        Assert.That(InputSystem.GetUnsupportedDevices(), Has.Exactly(1).With.Property("product").EqualTo("OtherTestHID"));
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
        var deviceId = runtime.AllocateDeviceId();
        unsafe
        {
            runtime.SetDeviceCommandCallback(deviceId,
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

                    return InputDeviceCommand.GenericFailure;
                });
        }
        // Report device.
        runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                manufacturer = "TestVendor",
                product = "TestHID",
                capabilities = new HID.HIDDeviceDescriptor
                {
                    vendorId = 0x123,
                    productId = 0x234
                }.ToJson()
            }.ToJson(), deviceId);
        InputSystem.Update();

        // Grab device.
        var device = (Joystick)InputSystem.GetDeviceById(deviceId);
        Assert.That(device, Is.Not.Null);
        Assert.That(device, Is.TypeOf<Joystick>());

        var deviceDescription = device.description;
        Assert.That(deviceDescription.interfaceName, Is.EqualTo(HID.kHIDInterface));

        var hidDescriptor = HID.ReadHIDDeviceDescriptor(ref deviceDescription,
            (ref InputDeviceCommand command) => runtime.DeviceCommand(device.deviceId, ref command));

        // Check HID descriptor.
        Assert.That(hidDescriptor.vendorId, Is.EqualTo(0x123));
        Assert.That(hidDescriptor.productId, Is.EqualTo(0x234));
        Assert.That(hidDescriptor.usagePage, Is.EqualTo(HID.UsagePage.GenericDesktop));
        Assert.That(hidDescriptor.usage, Is.EqualTo((int)HID.GenericDesktop.Gamepad));
        Assert.That(hidDescriptor.elements.Length, Is.EqualTo(kNumElements));

        Assert.That(hidDescriptor.elements[0].usagePage, Is.EqualTo(HID.UsagePage.GenericDesktop));
        Assert.That(hidDescriptor.elements[0].usage, Is.EqualTo((int)HID.GenericDesktop.X));
        Assert.That(hidDescriptor.elements[0].reportId, Is.EqualTo(1));
        Assert.That(hidDescriptor.elements[0].reportOffsetInBits, Is.EqualTo(8)); // Descriptor has report ID so that's the first thing in reports.
        Assert.That(hidDescriptor.elements[0].reportSizeInBits, Is.EqualTo(8));
        Assert.That(hidDescriptor.elements[0].logicalMin, Is.EqualTo(0));
        Assert.That(hidDescriptor.elements[0].logicalMax, Is.EqualTo(255));

        Assert.That(hidDescriptor.elements[1].usagePage, Is.EqualTo(HID.UsagePage.GenericDesktop));
        Assert.That(hidDescriptor.elements[1].usage, Is.EqualTo((int)HID.GenericDesktop.Y));
        Assert.That(hidDescriptor.elements[1].reportId, Is.EqualTo(1));
        Assert.That(hidDescriptor.elements[1].reportOffsetInBits, Is.EqualTo(16));
        Assert.That(hidDescriptor.elements[1].reportSizeInBits, Is.EqualTo(8));
        Assert.That(hidDescriptor.elements[1].logicalMin, Is.EqualTo(0));
        Assert.That(hidDescriptor.elements[1].logicalMax, Is.EqualTo(255));

        Assert.That(hidDescriptor.elements[4].hasNullState, Is.True);
        Assert.That(hidDescriptor.elements[4].physicalMax, Is.EqualTo(315));
        Assert.That(hidDescriptor.elements[4].unit, Is.EqualTo(0x14));

        Assert.That(hidDescriptor.elements[5].unit, Is.Zero);

        Assert.That(hidDescriptor.elements[5].reportOffsetInBits, Is.EqualTo(5 * 8 + 4));
        Assert.That(hidDescriptor.elements[5].usagePage, Is.EqualTo(HID.UsagePage.Button));
        Assert.That(hidDescriptor.elements[6].usagePage, Is.EqualTo(HID.UsagePage.Button));
        Assert.That(hidDescriptor.elements[7].usagePage, Is.EqualTo(HID.UsagePage.Button));
        Assert.That(hidDescriptor.elements[5].usage, Is.EqualTo(1));
        Assert.That(hidDescriptor.elements[6].usage, Is.EqualTo(2));
        Assert.That(hidDescriptor.elements[7].usage, Is.EqualTo(3));

        Assert.That(hidDescriptor.collections.Length, Is.EqualTo(1));
        Assert.That(hidDescriptor.collections[0].type, Is.EqualTo(HID.HIDCollectionType.Application));
        Assert.That(hidDescriptor.collections[0].childCount, Is.EqualTo(kNumElements));

        ////TODO: check hat switch
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateGenericHID_FromDeviceWithParsedReportDescriptor()
    {
        var deviceId = runtime.AllocateDeviceId();
        unsafe
        {
            runtime.SetDeviceCommandCallback(deviceId,
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
        runtime.ReportNewInputDevice(
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

        Assert.That(device.children, Has.Count.EqualTo(3));
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("stick").And.TypeOf<StickControl>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("trigger").And.TypeOf<ButtonControl>());
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

        runtime.ReportNewInputDevice(new InputDeviceDescription
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

        runtime.ReportNewInputDevice(
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

    [Test]
    [Category("Devices")]
    public void Devices_HIDDevicesDifferingOnlyByUsageGetSeparateLayouts()
    {
        var hidDescriptor1 = new HID.HIDDeviceDescriptor
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

        var hidDescriptor2 = hidDescriptor1;
        hidDescriptor2.usage = (int)HID.GenericDesktop.Gamepad;

        runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                manufacturer = "TestVendor",
                product = "TestHID",
                capabilities = hidDescriptor1.ToJson()
            }.ToJson());
        runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                manufacturer = "TestVendor",
                product = "TestHID",
                capabilities = hidDescriptor2.ToJson()
            }.ToJson());
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(2));
        Assert.That(InputSystem.devices[0].layout, Is.Not.EqualTo(InputSystem.devices[1].layout));
    }

    [StructLayout(LayoutKind.Explicit)]
    struct SimpleAxisState : IInputStateTypeInfo
    {
        [FieldOffset(0)] public byte reportId;
        [FieldOffset(1)] public ushort rz;
        [FieldOffset(3)] public short vz;
        [FieldOffset(5)] public byte rx;
        [FieldOffset(6)] public sbyte ry;
        [FieldOffset(7)] public ushort vx;
        [FieldOffset(9)] public short vy;
        [FieldOffset(11)] public int x;

        public FourCC format => new FourCC('H', 'I', 'D');
    }

    ////change this...
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
                .AddElement(HID.GenericDesktop.Rz, 16).WithLogicalMinMax(0, 65535)
                // 16bit [-32768..32767]
                .AddElement(HID.GenericDesktop.Vz, 16).WithLogicalMinMax(-32768, 32767)
                // 8bit [0..255]
                .AddElement(HID.GenericDesktop.Rx, 8).WithLogicalMinMax(0, 255)
                // 8bit [-128..127]
                .AddElement(HID.GenericDesktop.Ry, 8).WithLogicalMinMax(-128, 127)
                // 16bit [0..10000]
                .AddElement(HID.GenericDesktop.Vx, 16).WithLogicalMinMax(0, 10000)
                // 16bit [-10000..10000]
                .AddElement(HID.GenericDesktop.Vy, 16).WithLogicalMinMax(-10000, 10000)
                // 32bit [int min..int max]
                .AddElement(HID.GenericDesktop.X, 32).WithLogicalMinMax(int.MinValue, int.MaxValue)
                .Finish();

        runtime.ReportNewInputDevice(
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
            rz = ushort.MinValue,
            vz = short.MinValue,
            rx = byte.MinValue,
            ry = sbyte.MinValue,
            vx = 0,
            vy = -10000,
            x = int.MinValue
        });
        InputSystem.Update();

        Assert.That(device["Rz"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001));
        Assert.That(device["Vz"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001));
        Assert.That(device["Rx"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001));
        Assert.That(device["Ry"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001)); // Inverted
        Assert.That(device["Vx"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001));
        Assert.That(device["Vy"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001)); // Inverted
        Assert.That(device["X"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001));

        // Test upper bound.
        InputSystem.QueueStateEvent(device, new SimpleAxisState
        {
            reportId = 1,
            rz = ushort.MaxValue,
            vz = short.MaxValue,
            rx = byte.MaxValue,
            ry = sbyte.MaxValue,
            vx = 10000,
            vy = 10000,
            x = int.MaxValue
        });
        InputSystem.Update();

        Assert.That(device["Rz"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001));
        Assert.That(device["Vz"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001));
        Assert.That(device["Rx"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001));
        Assert.That(device["Ry"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001)); // Inverted
        Assert.That(device["Vx"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001));
        Assert.That(device["Vy"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.0001)); // Inverted
        Assert.That(device["X"].ReadValueAsObject(), Is.EqualTo(1).Within(0.0001));

        // Test center.
        InputSystem.QueueStateEvent(device, new SimpleAxisState
        {
            reportId = 1,
            rz = ushort.MaxValue / 2,
            vz = 0,
            rx = byte.MaxValue / 2,
            ry = 0,
            vx = 10000 / 2,
            vy = 0,
            x = 0
        });
        InputSystem.Update();

        Assert.That(device["Rz"].ReadValueAsObject(), Is.EqualTo(0).Within(0.0001));
        Assert.That(device["Vz"].ReadValueAsObject(), Is.EqualTo(0).Within(0.0001));
        ////FIXME: these accumulate some rather large errors
        Assert.That(device["Rx"].ReadValueAsObject(), Is.EqualTo(0).Within(0.004));
        Assert.That(device["Ry"].ReadValueAsObject(), Is.EqualTo(0).Within(0.004));
        Assert.That(device["Vx"].ReadValueAsObject(), Is.EqualTo(0).Within(0.0001));
        Assert.That(device["Vy"].ReadValueAsObject(), Is.EqualTo(0).Within(0.0001));
        Assert.That(device["X"].ReadValueAsObject(), Is.EqualTo(0).Within(0.0001));
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

            runtime.ReportNewInputDevice(
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
    public void Devices_HIDsWithoutProductName_AreNamedByTheirVendorAndProductIDAndUsageName()
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

        runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());
        InputSystem.Update();

        var device = (HID)InputSystem.devices.First(x => x is HID);
        Assert.That(device.name, Is.EqualTo("1234-5678 MultiAxisController"));
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

        runtime.ReportNewInputDevice(
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

        runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());

        InputSystem.Update();
        var hid = (HID)InputSystem.devices.First(x => x is HID);

        Assert.That(hid["hat"], Is.TypeOf<DpadControl>());

        // Assert that default state is set correctly.
        Assert.That(hid["hat/up"].ReadValueAsObject(), Is.EqualTo(0).Within(0.00001));
        Assert.That(hid["hat/down"].ReadValueAsObject(), Is.EqualTo(0).Within(0.00001));
        Assert.That(hid["hat/left"].ReadValueAsObject(), Is.EqualTo(0).Within(0.00001));
        Assert.That(hid["hat/right"].ReadValueAsObject(), Is.EqualTo(0).Within(0.00001));

        using (StateEvent.From(hid, out var eventPtr))
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

            Assert.That(hid["hat"].ReadValueAsObject(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kUp;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["hat"].ReadValueAsObject(), Is.EqualTo(Vector2.up).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kUpRight;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["hat"].ReadValueAsObject(), Is.EqualTo((Vector2.up + Vector2.right).normalized).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kRight;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["hat"].ReadValueAsObject(), Is.EqualTo(Vector2.right).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kRightDown;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["hat"].ReadValueAsObject(), Is.EqualTo((Vector2.right + Vector2.down).normalized).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kDown;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["hat"].ReadValueAsObject(), Is.EqualTo(Vector2.down).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kDownLeft;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["hat"].ReadValueAsObject(), Is.EqualTo((Vector2.down + Vector2.left).normalized).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kLeft;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["hat"].ReadValueAsObject(), Is.EqualTo(Vector2.left).Using(Vector2EqualityComparer.Instance));

            stateData[0] = kLeftUp;

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(hid["hat"].ReadValueAsObject(), Is.EqualTo((Vector2.left + Vector2.up).normalized).Using(Vector2EqualityComparer.Instance));
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

        runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());

        InputSystem.Update();
        var hid = (HID)InputSystem.devices.First(x => x is HID);

        Assert.That(hid["hat"], Is.TypeOf<DpadControl>());
        Assert.That(hid["hat1"], Is.TypeOf<DpadControl>());

        Assert.That(hid["hat"].stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(hid["hat"].stateBlock.bitOffset, Is.EqualTo(0));
        Assert.That(hid["hat1"].stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(hid["hat1"].stateBlock.bitOffset, Is.EqualTo(4));
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
    public void Devices_GenericHIDJoystickIsTurnedIntoJoystick()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            usage = (int)HID.GenericDesktop.Joystick,
            usagePage = HID.UsagePage.GenericDesktop,
            vendorId = 0x1234,
            productId = 0x5678,
            inputReportSize = 4,
            elements = new[]
            {
                // 16bit X and Y axes.
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 0, reportSizeInBits = 16 },
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.Y, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 16, reportSizeInBits = 16 },
            }
        };

        runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));

        var device = InputSystem.devices[0];
        Assert.That(device, Is.TypeOf<Joystick>());
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
    public void Devices_GenericHIDGamepadIsTurnedIntoJoystick()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            usage = (int)HID.GenericDesktop.Gamepad,
            usagePage = HID.UsagePage.GenericDesktop,
            vendorId = 0x1234,
            productId = 0x5678,
            inputReportSize = 4,
            elements = new[]
            {
                // 16bit X and Y axes.
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 0, reportSizeInBits = 16 },
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.Y, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 16, reportSizeInBits = 16 },
            }
        };

        runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));

        var device = InputSystem.devices[0];
        Assert.That(device, Is.TypeOf<Joystick>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_GenericHIDConvertsXAndYUsagesToStickControl()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            usage = (int)HID.GenericDesktop.Joystick,
            usagePage = HID.UsagePage.GenericDesktop,
            vendorId = 0x1234,
            productId = 0x5678,
            inputReportSize = 4,
            elements = new[]
            {
                // 16bit X and Y axes.
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 0, reportSizeInBits = 16 },
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.Y, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 16, reportSizeInBits = 16 },
            }
        };

        runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));

        var device = InputSystem.devices[0];
        Assert.That(device, Is.TypeOf<Joystick>());
        Assert.That(device["Stick"], Is.TypeOf<StickControl>());
    }

    [StructLayout(LayoutKind.Explicit)]
    struct SimpleJoystickLayout : IInputStateTypeInfo
    {
        [FieldOffset(0)] public byte reportId;
        [FieldOffset(1)] public ushort x;
        [FieldOffset(3)] public ushort y;

        public FourCC format => new FourCC('H', 'I', 'D');
    }

    [Test]
    [Category("Devices")]
    public void Devices_GenericHIDXAndYDrivesStickControl()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            usage = (int)HID.GenericDesktop.Joystick,
            usagePage = HID.UsagePage.GenericDesktop,
            vendorId = 0x1234,
            productId = 0x5678,
            inputReportSize = 9,
            elements = new[]
            {
                // 16bit X and Y axes.
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 0, reportSizeInBits = 16 },
                new HID.HIDElementDescriptor { usage = (int)HID.GenericDesktop.Y, usagePage = HID.UsagePage.GenericDesktop, reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 16, reportSizeInBits = 16 },
            }
        };

        runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));

        var device = InputSystem.devices[0];
        Assert.That(device, Is.TypeOf<Joystick>());
        Assert.That(device["Stick"], Is.TypeOf<StickControl>());

        InputSystem.QueueStateEvent(device, new SimpleJoystickLayout { reportId = 1, x = ushort.MaxValue, y = ushort.MinValue });
        InputSystem.Update();

        Assert.That(device["stick"].ReadValueAsObject(),
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(1, 1)))
                .Using(new Vector2EqualityComparer(0.01f)));
    }

    ////FIXME: The G25 racing wheel actually has the X and Y control separated such that other
    ////       controls (like Rz) are slotted in in-between the two. We can't handle this correctly
    ////       ATM as parent controls are required to span the entire memory range of child controls.
    ////       This means that the stick will implicitly cover the memory of controls that do not
    ////       belong to the stick.

    [StructLayout(LayoutKind.Explicit, Size = 13)]
    struct G25RacingWheelState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC("HID");

        [FieldOffset(0)] private int __padding1; // Work around il2cpp bug.
        [FieldOffset(4)] public ushort xAxis;
        [FieldOffset(6)] private short __padding2; // Work around il2cpp bug.
        [FieldOffset(8)] public byte yAxis;
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_GenericHIDSupportsStickWithAsymmetricXYSetup()
    {
        // This is the part of the actual setup from the Logitech G25 Racing Wheel. It has a 14-bit
        // X control and an 8-bit Y control with a gap in-between the two.
        // NOTE: Both the X and Y axis are unsigned so HID needs to correctly place the midpoint
        //       in-between the max and min.
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            usage = (int)HID.GenericDesktop.Joystick,
            usagePage = HID.UsagePage.GenericDesktop,
            vendorId = 0x1234,
            productId = 0x5678,
            inputReportSize = 13,
            elements = new[]
            {
                // 14-bit X axis at offset 34.
                new HID.HIDElementDescriptor
                {
                    usage = (int)HID.GenericDesktop.X,
                    usagePage = HID.UsagePage.GenericDesktop,
                    reportType = HID.HIDReportType.Input,
                    reportId = 0,
                    reportOffsetInBits = 34,
                    reportSizeInBits = 14,
                    logicalMin = 0,
                    logicalMax = 16383,
                    physicalMin = 0,
                    physicalMax = 16383
                },
                // 8-bit Y axis at offset 64.
                new HID.HIDElementDescriptor
                {
                    usage = (int)HID.GenericDesktop.Y,
                    usagePage = HID.UsagePage.GenericDesktop,
                    reportType = HID.HIDReportType.Input,
                    reportId = 0,
                    reportOffsetInBits = 64,
                    reportSizeInBits = 8,
                    logicalMin = 0,
                    logicalMax = 255,
                    physicalMin = 0,
                    physicalMax = 255
                },
            }
        };

        var device = InputSystem.AddDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            });

        Assert.That(device, Is.Not.Null);

        Assert.That(device["stick"].stateBlock.byteOffset, Is.EqualTo(4));
        Assert.That(device["stick"].stateBlock.bitOffset, Is.EqualTo(2));
        Assert.That(device["stick"].stateBlock.sizeInBits, Is.EqualTo((9 - 4) * 8 - 2));

        Assert.That(device["stick/x"].stateBlock.byteOffset, Is.EqualTo(4));
        Assert.That(device["stick/left"].stateBlock.byteOffset, Is.EqualTo(4));
        Assert.That(device["stick/right"].stateBlock.byteOffset, Is.EqualTo(4));
        Assert.That(device["stick/y"].stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device["stick/up"].stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device["stick/down"].stateBlock.byteOffset, Is.EqualTo(8));

        Assert.That(device["stick/x"].stateBlock.bitOffset, Is.EqualTo(2));
        Assert.That(device["stick/left"].stateBlock.bitOffset, Is.EqualTo(2));
        Assert.That(device["stick/right"].stateBlock.bitOffset, Is.EqualTo(2));
        Assert.That(device["stick/y"].stateBlock.bitOffset, Is.EqualTo(0));
        Assert.That(device["stick/up"].stateBlock.bitOffset, Is.EqualTo(0));
        Assert.That(device["stick/down"].stateBlock.bitOffset, Is.EqualTo(0));

        Assert.That(device["stick/x"].stateBlock.sizeInBits, Is.EqualTo(14));
        Assert.That(device["stick/left"].stateBlock.sizeInBits, Is.EqualTo(14));
        Assert.That(device["stick/right"].stateBlock.sizeInBits, Is.EqualTo(14));
        Assert.That(device["stick/y"].stateBlock.sizeInBits, Is.EqualTo(8));
        Assert.That(device["stick/up"].stateBlock.sizeInBits, Is.EqualTo(8));
        Assert.That(device["stick/down"].stateBlock.sizeInBits, Is.EqualTo(8));

        // Test default state.
        Assert.That(device["stick"].ReadValueAsObject(),
            Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));
        Assert.That(device["stick/x"].ReadValueAsObject(),
            Is.EqualTo(0).Within(0.00001));
        Assert.That(device["stick/y"].ReadValueAsObject(),
            Is.EqualTo(0).Within(0.00001));
        Assert.That(device["stick/up"].ReadValueAsObject(),
            Is.EqualTo(0).Within(0.00001));
        Assert.That(device["stick/down"].ReadValueAsObject(),
            Is.EqualTo(0).Within(0.00001));
        Assert.That(device["stick/left"].ReadValueAsObject(),
            Is.EqualTo(0).Within(0.00001));
        Assert.That(device["stick/right"].ReadValueAsObject(),
            Is.EqualTo(0).Within(0.00001));

        // Test lower limit.
        InputSystem.QueueStateEvent(device, new G25RacingWheelState());
        InputSystem.Update();

        Assert.That(device["stick"].ReadValueAsObject(),
            Is.EqualTo(new Vector2(-1, 1).normalized).Using(Vector2EqualityComparer.Instance));
        Assert.That(device["stick/x"].ReadValueAsObject(),
            Is.EqualTo(-1).Within(0.00001));
        Assert.That(device["stick/y"].ReadValueAsObject(),
            Is.EqualTo(1).Within(0.00001));
        Assert.That(device["stick/up"].ReadValueAsObject(),
            Is.EqualTo(1).Within(0.00001));
        Assert.That(device["stick/down"].ReadValueAsObject(),
            Is.EqualTo(0).Within(0.00001));
        Assert.That(device["stick/left"].ReadValueAsObject(),
            Is.EqualTo(1).Within(0.00001));
        Assert.That(device["stick/right"].ReadValueAsObject(),
            Is.EqualTo(0).Within(0.00001));

        // Test upper limit.
        InputSystem.QueueStateEvent(device, new G25RacingWheelState {xAxis = ((1 << 14) - 1) << 2, yAxis = 0xff});
        InputSystem.Update();

        Assert.That(device["stick"].ReadValueAsObject(),
            Is.EqualTo(new Vector2(1, -1).normalized).Using(new Vector2EqualityComparer(0.01f)));
        Assert.That(device["stick/x"].ReadValueAsObject(),
            Is.EqualTo(1).Within(0.00001));
        Assert.That(device["stick/y"].ReadValueAsObject(),
            Is.EqualTo(-1).Within(0.00001));
        Assert.That(device["stick/up"].ReadValueAsObject(),
            Is.EqualTo(0).Within(0.00001));
        Assert.That(device["stick/down"].ReadValueAsObject(),
            Is.EqualTo(1).Within(0.00001));
        Assert.That(device["stick/left"].ReadValueAsObject(),
            Is.EqualTo(0).Within(0.00001));
        Assert.That(device["stick/right"].ReadValueAsObject(),
            Is.EqualTo(1).Within(0.00001));
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

    // This is an integration-style test that just throws a complex HID descriptor from a real-world
    // joystick at the HID fallback code and ensures that what comes out of it at least looks like it makes sense.
    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetUpComplexJoystickHID()
    {
        // This is the JSON version of the InputDeviceDescription we get for a Mad Catz Saitek Pro Flight Stick X-56.
        const string jsonHID =
            "{\n    \"interface\": \"HID\",\n    \"type\": \"\",\n    \"product\": \"Saitek Pro Flight X-56 Rhino Stick\",\n    \"serial\": \"k0008627\",\n    \"version\": \"256\",\n    \"manufacturer\": \"Mad Catz\",\n    \"capabilities\": \"{\\n    \\\"vendorId\\\": 1848,\\n    \\\"productId\\\": 8737,\\n    \\\"usage\\\": 4,\\n    \\\"usagePage\\\": 1,\\n    \\\"inputReportSize\\\": 11,\\n    \\\"outputReportSize\\\": 0,\\n    \\\"featureReportSize\\\": 0,\\n    \\\"elements\\\": [\\n        {\\n            \\\"usage\\\": 48,\\n            \\\"usagePage\\\": 1,\\n            \\\"unit\\\": 0,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 65535,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 65535,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 16,\\n            \\\"reportOffsetInBits\\\": 0,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 49,\\n            \\\"usagePage\\\": 1,\\n            \\\"unit\\\": 0,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 65535,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 65535,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 16,\\n            \\\"reportOffsetInBits\\\": 16,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 53,\\n            \\\"usagePage\\\": 1,\\n            \\\"unit\\\": 0,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 4095,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 4095,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 12,\\n            \\\"reportOffsetInBits\\\": 32,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 57,\\n            \\\"usagePage\\\": 1,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 1,\\n            \\\"logicalMax\\\": 8,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 4,\\n            \\\"reportOffsetInBits\\\": 44,\\n            \\\"flags\\\": 66\\n        },\\n        {\\n            \\\"usage\\\": 1,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 48,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 2,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 49,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 3,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 50,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 4,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 51,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 5,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 52,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 6,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 53,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 7,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 54,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 8,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 55,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 9,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 56,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 10,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 57,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 11,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 58,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 12,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 59,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 13,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 60,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 14,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 61,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 15,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 62,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 16,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 63,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 17,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 64,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 0,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 7,\\n            \\\"reportOffsetInBits\\\": 65,\\n            \\\"flags\\\": 1\\n        },\\n        {\\n            \\\"usage\\\": 51,\\n            \\\"usagePage\\\": 1,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 255,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 8,\\n            \\\"reportOffsetInBits\\\": 72,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 52,\\n            \\\"usagePage\\\": 1,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 255,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 8,\\n            \\\"reportOffsetInBits\\\": 80,\\n            \\\"flags\\\": 2\\n        }\\n    ],\\n    \\\"collections\\\": [\\n        {\\n            \\\"type\\\": 1,\\n            \\\"usage\\\": 4,\\n            \\\"usagePage\\\": 1,\\n            \\\"parent\\\": -1,\\n            \\\"childCount\\\": 24,\\n            \\\"firstChild\\\": 0\\n        },\\n        {\\n            \\\"type\\\": 0,\\n            \\\"usage\\\": 1,\\n            \\\"usagePage\\\": 1,\\n            \\\"parent\\\": 0,\\n            \\\"childCount\\\": 24,\\n            \\\"firstChild\\\": 0\\n        }\\n    ]\\n}\"\n}";

        var device = InputSystem.AddDevice(InputDeviceDescription.FromJson(jsonHID));

        // Make sure it was indeed the HID fallback path picking up the device, not
        // a custom layout. Once we add that, it should be bypassed here in this test.
        Assert.That(device.layout, Does.StartWith("HID::"));

        Assert.That(device.TryGetChildControl("stick"), Is.TypeOf<StickControl>());
        Assert.That(device.TryGetChildControl("rx"), Is.TypeOf<AxisControl>());
        Assert.That(device.TryGetChildControl("ry"), Is.TypeOf<AxisControl>());
        Assert.That(device.TryGetChildControl("rz"), Is.TypeOf<AxisControl>());
        Assert.That(device.TryGetChildControl("hat"), Is.TypeOf<DpadControl>());

        Assert.That(device["rx"].ReadValueAsObject(), Is.EqualTo(0).Within(0.001));
        Assert.That(device["ry"].ReadValueAsObject(), Is.EqualTo(0).Within(0.001));
        Assert.That(device["rz"].ReadValueAsObject(), Is.EqualTo(0).Within(0.001));

        Assert.That(device["hat"].ReadValueAsObject(), Is.EqualTo(Vector2.zero));
        Assert.That(device["hat/up"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["hat/down"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["hat/left"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["hat/right"].ReadValueAsObject(), Is.Zero);

        Assert.That(device["stick"].ReadValueAsObject(), Is.EqualTo(Vector2.zero));
        Assert.That(device["stick/up"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["stick/down"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["stick/left"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["stick/right"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["stick/x"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["stick/y"].ReadValueAsObject(), Is.Zero);

        // Button 1 must be trigger.
        // NOTE: Funnily enough the HID adds a button0 which, however, I couldn't find on the device. There's
        //       also a button15 which seems to just eternally be stuck on 1 and not correspond to a control on the device.
        Assert.That(device.TryGetChildControl("button1"), Is.Null);
        Assert.That(device.TryGetChildControl("trigger"), Is.TypeOf<ButtonControl>());
        Assert.That(device["trigger"].stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device["trigger"].stateBlock.bitOffset, Is.EqualTo(0));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_GenericHIDSetsDisplayNameFromProduct()
    {
        const string jsonHID =
            "{\n    \"interface\": \"HID\",\n    \"type\": \"\",\n    \"product\": \"Saitek Pro Flight X-56 Rhino Stick\",\n    \"serial\": \"k0008627\",\n    \"version\": \"256\",\n    \"manufacturer\": \"Mad Catz\",\n    \"capabilities\": \"{\\n    \\\"vendorId\\\": 1848,\\n    \\\"productId\\\": 8737,\\n    \\\"usage\\\": 4,\\n    \\\"usagePage\\\": 1,\\n    \\\"inputReportSize\\\": 11,\\n    \\\"outputReportSize\\\": 0,\\n    \\\"featureReportSize\\\": 0,\\n    \\\"elements\\\": [\\n        {\\n            \\\"usage\\\": 48,\\n            \\\"usagePage\\\": 1,\\n            \\\"unit\\\": 0,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 65535,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 65535,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 16,\\n            \\\"reportOffsetInBits\\\": 0,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 49,\\n            \\\"usagePage\\\": 1,\\n            \\\"unit\\\": 0,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 65535,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 65535,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 16,\\n            \\\"reportOffsetInBits\\\": 16,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 53,\\n            \\\"usagePage\\\": 1,\\n            \\\"unit\\\": 0,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 4095,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 4095,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 12,\\n            \\\"reportOffsetInBits\\\": 32,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 57,\\n            \\\"usagePage\\\": 1,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 1,\\n            \\\"logicalMax\\\": 8,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 4,\\n            \\\"reportOffsetInBits\\\": 44,\\n            \\\"flags\\\": 66\\n        },\\n        {\\n            \\\"usage\\\": 1,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 48,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 2,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 49,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 3,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 50,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 4,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 51,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 5,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 52,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 6,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 53,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 7,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 54,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 8,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 55,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 9,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 56,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 10,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 57,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 11,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 58,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 12,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 59,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 13,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 60,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 14,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 61,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 15,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 62,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 16,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 63,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 17,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 1,\\n            \\\"reportOffsetInBits\\\": 64,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 0,\\n            \\\"usagePage\\\": 9,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 1,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 7,\\n            \\\"reportOffsetInBits\\\": 65,\\n            \\\"flags\\\": 1\\n        },\\n        {\\n            \\\"usage\\\": 51,\\n            \\\"usagePage\\\": 1,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 255,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 8,\\n            \\\"reportOffsetInBits\\\": 72,\\n            \\\"flags\\\": 2\\n        },\\n        {\\n            \\\"usage\\\": 52,\\n            \\\"usagePage\\\": 1,\\n            \\\"unit\\\": 20,\\n            \\\"unitExponent\\\": 0,\\n            \\\"logicalMin\\\": 0,\\n            \\\"logicalMax\\\": 255,\\n            \\\"physicalMin\\\": 0,\\n            \\\"physicalMax\\\": 315,\\n            \\\"reportType\\\": 1,\\n            \\\"collectionIndex\\\": 0,\\n            \\\"reportId\\\": 1,\\n            \\\"reportSizeInBits\\\": 8,\\n            \\\"reportOffsetInBits\\\": 80,\\n            \\\"flags\\\": 2\\n        }\\n    ],\\n    \\\"collections\\\": [\\n        {\\n            \\\"type\\\": 1,\\n            \\\"usage\\\": 4,\\n            \\\"usagePage\\\": 1,\\n            \\\"parent\\\": -1,\\n            \\\"childCount\\\": 24,\\n            \\\"firstChild\\\": 0\\n        },\\n        {\\n            \\\"type\\\": 0,\\n            \\\"usage\\\": 1,\\n            \\\"usagePage\\\": 1,\\n            \\\"parent\\\": 0,\\n            \\\"childCount\\\": 24,\\n            \\\"firstChild\\\": 0\\n        }\\n    ]\\n}\"\n}";

        var device = InputSystem.AddDevice(InputDeviceDescription.FromJson(jsonHID));
        var layout = InputSystem.LoadLayout(device.layout);

        Assert.That(layout.displayName, Is.EqualTo("Saitek Pro Flight X-56 Rhino Stick"));
    }

    [Test]
    [Category("Utilities")]
    public void Utilities_CanRecognizeVendorDefinedUsages()
    {
        Assert.That(HID.UsagePageToString((HID.UsagePage) 0xff01), Is.EqualTo("Vendor-Defined"));
        Assert.That(HID.UsageToString((HID.UsagePage) 0xff01, 0x33), Is.Null);
    }
}
#endif
