using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.DualShock;
using UnityEngine.Experimental.Input.Plugins.DualShock.LowLevel;
using UnityEngine.Experimental.Input.Processors;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.TestTools.Utils;

#if UNITY_WSA
using UnityEngine.Experimental.Input.Plugins.HID;
#endif

public class DualShockTests : InputTestFixture
{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WSA
    [Test]
    [Category("Devices")]
    public void Devices_SupportsDualShockAsHID()
    {
#if !UNITY_WSA
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            product = "Wireless Controller",
            manufacturer = "Sony Interactive Entertainment",
            interfaceName = "HID",
        });
#else // UWP requires different query logic (manufacture not available)
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            product = "Wireless Controller",
            interfaceName = "HID",
            capabilities = new HID.HIDDeviceDescriptor
            {
                vendorId = 0x054C, // Sony
            }.ToJson()
        });
#endif

        Assert.That(device, Is.AssignableTo<DualShockGamepad>());
        var gamepad = (DualShockGamepad)device;

        // Dpad has default state value so make sure that one is coming through.
        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

        InputSystem.QueueStateEvent(gamepad,
            new DualShockHIDInputReport
            {
                leftStickX = 32,
                leftStickY = 64,
                rightStickX = 128,
                rightStickY = 255,
                leftTrigger = 20,
                rightTrigger = 40,
                buttons1 = 0xf7, // Low order 4 bits is Dpad but effectively uses only 3 bits.
                buttons2 = 0xff,
                buttons3 = 0xff
            });
        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.ReadValue(), Is.EqualTo(NormalizeProcessor.Normalize(32 / 255.0f, 0f, 1f, 0.5f)).Within(0.00001));
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(-NormalizeProcessor.Normalize(64 / 255.0f, 0f, 1f, 0.5f)).Within(0.00001));
        Assert.That(gamepad.rightStick.x.ReadValue(), Is.EqualTo(NormalizeProcessor.Normalize(128 / 255.0f, 0f, 1f, 0.5f)).Within(0.00001));
        Assert.That(gamepad.rightStick.y.ReadValue(), Is.EqualTo(-NormalizeProcessor.Normalize(255 / 255.0f, 0f, 1f, 0.5f)).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(NormalizeProcessor.Normalize(20 / 255.0f, 0f, 1f, 0f)).Within(0.00001));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(NormalizeProcessor.Normalize(40 / 255.0f, 0f, 1f, 0f)).Within(0.00001));
        ////TODO: test button presses individually
        Assert.That(gamepad.buttonSouth.isPressed);
        Assert.That(gamepad.buttonEast.isPressed);
        Assert.That(gamepad.buttonWest.isPressed);
        Assert.That(gamepad.buttonNorth.isPressed);
        Assert.That(gamepad.squareButton.isPressed);
        Assert.That(gamepad.triangleButton.isPressed);
        Assert.That(gamepad.circleButton.isPressed);
        Assert.That(gamepad.crossButton.isPressed);
        Assert.That(gamepad.startButton.isPressed);
        Assert.That(gamepad.selectButton.isPressed);
        Assert.That(gamepad.dpad.up.isPressed); // Dpad uses enumerated values, not a bitfield; 0x7 is up left.
        Assert.That(gamepad.dpad.down.isPressed, Is.False);
        Assert.That(gamepad.dpad.left.isPressed);
        Assert.That(gamepad.dpad.right.isPressed, Is.False);
        Assert.That(gamepad.leftShoulder.isPressed);
        Assert.That(gamepad.rightShoulder.isPressed);
        Assert.That(gamepad.leftStickButton.isPressed);
        Assert.That(gamepad.rightStickButton.isPressed);
        Assert.That(gamepad.touchpadButton.isPressed);

        // Sensors not (yet?) supported. Needs figuring out how to interpret the HID data.
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsDualShockAsHID_WithAlternateManufacturerName()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            product = "Wireless Controller",
            manufacturer = "Sony Computer Entertainment",
            interfaceName = "HID"
        });

        Assert.That(device, Is.AssignableTo<DualShockGamepad>());
    }

#if UNITY_WSA
    [Test]
    [Category("Devices")]
    public void Devices_SupportsDualShockAsHIDOnUWP()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            product = "Wireless Controller",
            capabilities = new HID.HIDDeviceDescriptor
            {
                vendorId = 0x054C, // Sony
            }.ToJson()
        });

        Assert.That(device, Is.AssignableTo<DualShockGamepad>());
    }

#endif

    [Test]
    [Category("Devices")]
    public void Devices_DualShockHID_HasDpadInNullStateByDefault()
    {
        // The DualShock's dpad has a default state of 8 (indicating dpad isn't pressed in any direction),
        // not of 0 (which actually means "up" is pressed). Make sure this is set up correctly.

        var gamepad = InputSystem.AddDevice<DualShockGamepadHID>();

        Assert.That(gamepad.dpad.up.isPressed, Is.False);
        Assert.That(gamepad.dpad.down.isPressed, Is.False);
        Assert.That(gamepad.dpad.left.isPressed, Is.False);
        Assert.That(gamepad.dpad.right.isPressed, Is.False);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanSetLightBarColorAndMotorSpeedsOnDualShockHID()
    {
        var gamepad = InputSystem.AddDevice<DualShockGamepadHID>();

        DualShockHIDOutputReport? receivedCommand = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(gamepad.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == DualShockHIDOutputReport.Type)
                    {
                        Assert.That(receivedCommand.HasValue, Is.False);
                        receivedCommand = *((DualShockHIDOutputReport*)commandPtr);
                        return 1;
                    }

                    Assert.Fail("Received wrong type of command");
                    return InputDeviceCommand.kGenericFailure;
                });
        }
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

#if UNITY_EDITOR || UNITY_PS4

    [Test]
    [Category("Devices")]
    public void Devices_SupportsDualShockOnPS4()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            deviceClass = "PS4DualShockGamepad", ////REVIEW: this should be the product name instead
            interfaceName = "PS4"
        });

        Assert.That(device, Is.AssignableTo<DualShockGamepad>());
        var gamepad = (DualShockGamepad)device;

        InputSystem.QueueStateEvent(gamepad,
            new DualShockGamepadStatePS4
            {
                buttons = 0xffffffff,
                leftStick = new Vector2(0.123f, 0.456f),
                rightStick = new Vector2(0.789f, 0.234f),
                leftTrigger = 0.567f,
                rightTrigger = 0.891f,
                acceleration = new Vector3(0.987f, 0.654f, 0.321f),
                orientation = new Quaternion(0.111f, 0.222f, 0.333f, 0.444f),
                angularVelocity = new Vector3(0.444f, 0.555f, 0.666f),
                touch0 = new PS4Touch
                {
                    touchId = 123,
                    position = new Vector2(0.231f, 0.342f)
                },
                touch1 = new PS4Touch
                {
                    touchId = 456,
                    position = new Vector2(0.453f, 0.564f)
                },
            });
        InputSystem.Update();

        Assert.That(gamepad.squareButton.isPressed);
        Assert.That(gamepad.triangleButton.isPressed);
        Assert.That(gamepad.circleButton.isPressed);
        Assert.That(gamepad.crossButton.isPressed);
        Assert.That(gamepad.buttonSouth.isPressed);
        Assert.That(gamepad.buttonNorth.isPressed);
        Assert.That(gamepad.buttonEast.isPressed);
        Assert.That(gamepad.buttonWest.isPressed);
        Assert.That(gamepad.leftStickButton.isPressed);
        Assert.That(gamepad.rightStickButton.isPressed);
        Assert.That(gamepad.L3.isPressed);
        Assert.That(gamepad.R3.isPressed);

        Assert.That(gamepad.leftStick.x.ReadValue(), Is.EqualTo(0.123).Within(0.00001));
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.456).Within(0.00001));
        Assert.That(gamepad.rightStick.x.ReadValue(), Is.EqualTo(0.789).Within(0.00001));
        Assert.That(gamepad.rightStick.y.ReadValue(), Is.EqualTo(0.234).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.567).Within(0.00001));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(0.891).Within(0.00001));

        Assert.That(gamepad.acceleration.x.ReadValue(), Is.EqualTo(0.987).Within(0.00001));
        Assert.That(gamepad.acceleration.y.ReadValue(), Is.EqualTo(0.654).Within(0.00001));
        Assert.That(gamepad.acceleration.z.ReadValue(), Is.EqualTo(0.321).Within(0.00001));

        Quaternion orientation = gamepad.orientation.ReadValue();

        Assert.That(orientation.x, Is.EqualTo(0.111).Within(0.00001));
        Assert.That(orientation.y, Is.EqualTo(0.222).Within(0.00001));
        Assert.That(orientation.z, Is.EqualTo(0.333).Within(0.00001));
        Assert.That(orientation.w, Is.EqualTo(0.444).Within(0.00001));

        Assert.That(gamepad.angularVelocity.x.ReadValue(), Is.EqualTo(0.444).Within(0.00001));
        Assert.That(gamepad.angularVelocity.y.ReadValue(), Is.EqualTo(0.555).Within(0.00001));
        Assert.That(gamepad.angularVelocity.z.ReadValue(), Is.EqualTo(0.666).Within(0.00001));

        ////TODO: touch
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanSetLightBarColorAndMotorSpeedsOnDualShockPS4()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            deviceClass = "PS4DualShockGamepad",
            interfaceName = "PS4"
        });

        Assert.That(device, Is.AssignableTo<DualShockGamepadPS4>());
        var gamepad = (DualShockGamepadPS4)device;

        DualShockPS4OuputCommand? receivedCommand = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(gamepad.id,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == DualShockPS4OuputCommand.Type)
                    {
                        Assert.That(receivedCommand.HasValue, Is.False);
                        receivedCommand = *((DualShockPS4OuputCommand*)commandPtr);
                        return 1;
                    }

                    Assert.Fail("Received wrong type of command");
                    return InputDeviceCommand.kGenericFailure;
                });
        }
        gamepad.SetMotorSpeeds(0.1234f, 0.5678f);

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.largeMotorSpeed, Is.EqualTo((byte)(0.1234 * 255)));
        Assert.That(receivedCommand.Value.smallMotorSpeed, Is.EqualTo((byte)(0.56787 * 255)));

        receivedCommand = null;
        gamepad.SetLightBarColor(new Color(0.123f, 0.456f, 0.789f));

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.redColor, Is.EqualTo((byte)(0.123f * 255)));
        Assert.That(receivedCommand.Value.greenColor, Is.EqualTo((byte)(0.456f * 255)));
        Assert.That(receivedCommand.Value.blueColor, Is.EqualTo((byte)(0.789f * 255)));
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanReadSlotIndexAndGetDualShockPS4BySlotIndex()
    {
        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            deviceClass = "PS4DualShockGamepad",
            interfaceName = "PS4"
        }.ToJson(), 1);
        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            deviceClass = "PS4DualShockGamepad",
            interfaceName = "PS4"
        }.ToJson(), 2);
        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            deviceClass = "PS4DualShockGamepad",
            interfaceName = "PS4"
        }.ToJson(), 3);
        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            deviceClass = "PS4DualShockGamepad",
            interfaceName = "PS4"
        }.ToJson(), 4);

        testRuntime.SetDeviceCommandCallback(1, QueryPS4ControllerInfo.Create().WithSlotIndex(0));
        testRuntime.SetDeviceCommandCallback(2, QueryPS4ControllerInfo.Create().WithSlotIndex(1));
        testRuntime.SetDeviceCommandCallback(3, QueryPS4ControllerInfo.Create().WithSlotIndex(2));
        testRuntime.SetDeviceCommandCallback(4, QueryPS4ControllerInfo.Create().WithSlotIndex(3));

        InputSystem.Update();

        var gamepad1 = (DualShockGamepadPS4)InputSystem.devices[0];
        var gamepad2 = (DualShockGamepadPS4)InputSystem.devices[1];
        var gamepad3 = (DualShockGamepadPS4)InputSystem.devices[2];
        var gamepad4 = (DualShockGamepadPS4)InputSystem.devices[3];

        Assert.That(gamepad1.slotIndex, Is.EqualTo(0));
        Assert.That(gamepad2.slotIndex, Is.EqualTo(1));
        Assert.That(gamepad3.slotIndex, Is.EqualTo(2));
        Assert.That(gamepad4.slotIndex, Is.EqualTo(3));

        Assert.That(DualShockGamepadPS4.GetBySlotIndex(0), Is.SameAs(gamepad1));
        Assert.That(DualShockGamepadPS4.GetBySlotIndex(1), Is.SameAs(gamepad2));
        Assert.That(DualShockGamepadPS4.GetBySlotIndex(2), Is.SameAs(gamepad3));
        Assert.That(DualShockGamepadPS4.GetBySlotIndex(3), Is.SameAs(gamepad4));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanQueryPS4UserIdFromDualShockPS4()
    {
        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            deviceClass = "PS4DualShockGamepad",
            interfaceName = "PS4"
        }.ToJson(), 1);

        bool? receivedCommand = null;
        unsafe
        {
            testRuntime.SetDeviceCommandCallback(1,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == QueryPS4ControllerInfo.Type)
                    {
                        Assert.That(receivedCommand.HasValue, Is.False);
                        receivedCommand = true;
                        ((QueryPS4ControllerInfo*)commandPtr)->slotIndex = 1;  // Otherwise we query over and over again.
                        ((QueryPS4ControllerInfo*)commandPtr)->userId = 1234;
                        return 1;
                    }

                    Assert.Fail("Received wrong type of command");
                    return InputDeviceCommand.kGenericFailure;
                });
        }
        InputSystem.Update();
        var gamepad = (DualShockGamepadPS4)InputSystem.devices[0];

        Assert.That(gamepad.ps4UserId, Is.EqualTo(1234));
    }

#endif
}
