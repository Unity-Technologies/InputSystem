using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA
using UnityEngine.InputSystem.DualShock.LowLevel;
#endif
using UnityEngine.InputSystem.Processors;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.HID;
using UnityEngine.TestTools.Utils;

#if UNITY_WSA
using UnityEngine.InputSystem.HID;
#endif

internal class DualShockTests : CoreTestsFixture
{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_WSA
    public DualShockGamepad Devices_SupportsDualShockAsHID<TDevice, TState>(TState state)
        where TDevice : DualShockGamepad
        where TState : struct, IInputStateTypeInfo
    {
        var gamepad = InputSystem.AddDevice<TDevice>();
        // Dpad has default state value so make sure that one is coming through.
        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.zero).Using(Vector2EqualityComparer.Instance));

        InputSystem.QueueStateEvent(gamepad, state);

        InputSystem.Update();

        var leftStickDeadzone = gamepad.leftStick.TryGetProcessor<StickDeadzoneProcessor>();
        var rightStickDeadzone = gamepad.leftStick.TryGetProcessor<StickDeadzoneProcessor>();

        Assert.That(gamepad.leftStick.ReadValue(),
            Is.EqualTo(leftStickDeadzone.Process(
                new Vector2(NormalizeProcessor.Normalize(32 / 255.0f, 0f, 1f, 0.5f),
                    -NormalizeProcessor.Normalize(64 / 255.0f, 0f, 1f, 0.5f)))));

        Assert.That(gamepad.rightStick.ReadValue(), Is.EqualTo(rightStickDeadzone.Process(
            new Vector2(NormalizeProcessor.Normalize(128 / 255.0f, 0f, 1f, 0.5f),
                -NormalizeProcessor.Normalize(255 / 255.0f, 0f, 1f, 0.5f)))));

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

        ////REVIEW: Should we just kill these buttons? Do they provide any value?
        // PS controller adds buttons for the left and right trigger. Make sure these are marked as
        // synthetic so they don't get picked up as double input.
        // https://fogbugz.unity3d.com/f/cases/1293734
        Assert.That(gamepad["leftTriggerButton"].synthetic, Is.True);
        Assert.That(gamepad["rightTriggerButton"].synthetic, Is.True);

        return gamepad;
        // Sensors not (yet?) supported. Needs figuring out how to interpret the HID data.
    }

    [Test]
    [Category("Devices")]
    [TestCase(true)]
    [TestCase(false)]
    public void Devices_SupportsDualShock4AsHID(bool precompiled)
    {
        if (!precompiled)
            InputControlLayout.s_Layouts.precompiledLayouts.Clear();

        var gamepad = Devices_SupportsDualShockAsHID<DualShock4GamepadHID, DualShock4HIDInputReport>(
            new DualShock4HIDInputReport
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
            }
        );

        Assert.That(gamepad.touchpadButton.isPressed);
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsDualShock3AsHID()
    {
        Devices_SupportsDualShockAsHID<DualShock3GamepadHID, DualShock3HIDInputReport>(
            new DualShock3HIDInputReport
            {
                leftStickX = 32,
                leftStickY = 64,
                rightStickX = 128,
                rightStickY = 255,
                leftTrigger = 20,
                rightTrigger = 40,
                buttons1 = 0x9f, // High order 4 bits is Dpad
                buttons2 = 0xff,
                buttons3 = 0xff
            }
        );
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsDualShockAsHID_WithProductAndManufacturerName()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            product = "Wireless Controller",
            manufacturer = "Sony Interactive Entertainment",
            interfaceName = "HID",
        });

        Assert.That(device, Is.AssignableTo<DualShockGamepad>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsDualShockAsHID_WithProductAndAlternateManufacturerName()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            product = "Wireless Controller",
            manufacturer = "Sony Computer Entertainment",
            interfaceName = "HID",
        });

        Assert.That(device, Is.AssignableTo<DualShockGamepad>());
    }

    [Test]
    [Category("Devices")]
    [TestCase(0x54C, 0x9CC)]
    [TestCase(0x54C, 0x5C4)]
    public void Devices_SupportsDualShockAsHID_WithJustPIDAndVID(int vendorId, int productId)
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "HID",
            capabilities = new HID.HIDDeviceDescriptor
            {
                vendorId = vendorId,
                productId = productId,
            }.ToJson()
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

        var gamepad = InputSystem.AddDevice<DualShock4GamepadHID>();

        Assert.That(gamepad.dpad.up.isPressed, Is.False);
        Assert.That(gamepad.dpad.down.isPressed, Is.False);
        Assert.That(gamepad.dpad.left.isPressed, Is.False);
        Assert.That(gamepad.dpad.right.isPressed, Is.False);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanSetLightBarColorAndMotorSpeedsOnDualShockHID()
    {
        var gamepad = InputSystem.AddDevice<DualShock4GamepadHID>();

        DualShockHIDOutputReport? receivedCommand = null;
        unsafe
        {
            runtime.SetDeviceCommandCallback(gamepad.deviceId,
                (id, commandPtr) =>
                {
                    if (commandPtr->type == DualShockHIDOutputReport.Type)
                    {
                        Assert.That(receivedCommand.HasValue, Is.False);
                        receivedCommand = *((DualShockHIDOutputReport*)commandPtr);
                        return 1;
                    }

                    Assert.Fail("Received wrong type of command");
                    return InputDeviceCommand.GenericFailure;
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

        receivedCommand = null;
        gamepad.SetMotorSpeedsAndLightBarColor(0.5f, 0.5f, new Color(0.5f, 0.5f, 0.5f));

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.EqualTo((byte)(0.5 * 255)));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.EqualTo((byte)(0.5 * 255)));
        Assert.That(receivedCommand.Value.redColor, Is.EqualTo((byte)(0.5f * 255)));
        Assert.That(receivedCommand.Value.greenColor, Is.EqualTo((byte)(0.5f * 255)));
        Assert.That(receivedCommand.Value.blueColor, Is.EqualTo((byte)(0.5f * 255)));
    }

#endif
}
