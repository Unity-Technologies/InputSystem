using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.XInput;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Utilities;
using System.Runtime.InteropServices;

#if UNITY_EDITOR || UNITY_XBOXONE
using UnityEngine.Experimental.Input.Plugins.XInput.LowLevel;
#endif

internal class XInputTests : InputTestFixture
{
    ////TODO: refactor this into two tests that send actual state and test the wiring
    ////TODO: enable everything in the editor always and test
    [Test]
    [Category("Devices")]
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    [TestCase("Xbox One Wired Controller", "Microsoft", "HID", "XInputControllerOSX")]
#endif
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA
    [TestCase(null, null, "XInput", "XInputControllerWindows")]
#endif
    public void Devices_SupportsXInputDevicesOnPlatform(string product, string manufacturer, string interfaceName, string layoutName)
    {
        var description = new InputDeviceDescription
        {
            interfaceName = interfaceName,
            product = product,
            manufacturer = manufacturer
        };

        InputDevice device = null;
        Assert.That(() => device = InputSystem.AddDevice(description), Throws.Nothing);

        using (var matches = InputSystem.FindControls(string.Format("/<{0}>", layoutName)))
            Assert.That(matches, Has.Exactly(1).SameAs(device));

        Assert.That(device.name, Is.EqualTo(layoutName));
        Assert.That(device.description.manufacturer, Is.EqualTo(manufacturer));
        Assert.That(device.description.interfaceName, Is.EqualTo(interfaceName));
        Assert.That(device.description.product, Is.EqualTo(product));
    }

    ////FIXME: we should not have tests that only run in players
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA
    [Test]
    [Category("Devices")]
    public void Devices_CanGetSubTypeOfXInputDevice()
    {
        var capabilities = new XInputController.Capabilities
        {
            subType = XInputController.DeviceSubType.ArcadePad
        };
        var description = new InputDeviceDescription
        {
            interfaceName = "XInput",
            capabilities = JsonUtility.ToJson(capabilities)
        };

        var device = (XInputController)InputSystem.AddDevice(description);

        Assert.That(device.subType, Is.EqualTo(XInputController.DeviceSubType.ArcadePad));
    }

#endif

#if UNITY_EDITOR || UNITY_XBOXONE
    [Test]
    [Category("Devices")]
    public void Devices_SupportsControllerOnXbox()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            deviceClass = "XboxOneGamepad", ////REVIEW: this should be the product name instead
            interfaceName = "Xbox"
        });

        Assert.That(device, Is.AssignableTo<XInputController>());
        Assert.That(device, Is.AssignableTo<XboxOneGamepad>());
        var gamepad = (XboxOneGamepad)device;

        InputSystem.QueueStateEvent(gamepad,
            new XboxOneGamepadState
            {
                leftStick = new Vector2(0.123f, 0.456f),
                rightStick = new Vector2(0.789f, 0.234f),
                leftTrigger = 0.567f,
                rightTrigger = 0.891f,
            });
        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.ReadValue(), Is.EqualTo(0.123).Within(0.00001));
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.456).Within(0.00001));
        Assert.That(gamepad.rightStick.x.ReadValue(), Is.EqualTo(0.789).Within(0.00001));
        Assert.That(gamepad.rightStick.y.ReadValue(), Is.EqualTo(0.234).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.567).Within(0.00001));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(0.891).Within(0.00001));

        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.A), gamepad.aButton);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.A), gamepad.buttonSouth);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.B), gamepad.bButton);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.B), gamepad.buttonEast);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.X), gamepad.xButton);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.X), gamepad.buttonWest);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.Y), gamepad.yButton);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.Y), gamepad.buttonNorth);

        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.DPadDown), gamepad.dpad.down);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.DPadUp), gamepad.dpad.up);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.DPadLeft), gamepad.dpad.left);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.DPadRight), gamepad.dpad.right);

        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.LeftThumbstick), gamepad.leftStickButton);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.RightThumbstick), gamepad.rightStickButton);

        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.LeftShoulder), gamepad.leftShoulder);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.RightShoulder), gamepad.rightShoulder);

        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.Menu), gamepad.menu);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.Menu), gamepad.startButton);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.View), gamepad.view);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.View), gamepad.selectButton);

        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.Paddle1), gamepad.paddle1);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.Paddle2), gamepad.paddle2);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.Paddle3), gamepad.paddle3);
        AssertButtonPress(gamepad, new XboxOneGamepadState().WithButton(XboxOneGamepadState.Button.Paddle4), gamepad.paddle4);
    }

#endif

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX

    [Test]
    [Category("Devices")]
    public void Devices_SupportXboxControllerOnOSX()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "HID",
            product = "Xbox One Wired Controller",
            manufacturer = "Microsoft"
        });

        Assert.That(device, Is.AssignableTo<XInputController>());
        Assert.That(device, Is.AssignableTo<XInputControllerOSX>());
        var gamepad = (XInputControllerOSX)device;

        InputSystem.QueueStateEvent(gamepad,
            new XInputControllerOSXState
            {
                leftStickX = 32767,
                leftStickY = -32767,
                rightStickX = 32767,
                rightStickY = -32767,
                leftTrigger = 255,
                rightTrigger = 255,
            });

        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.up.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.down.ReadValue(), Is.EqualTo(0.0).Within(0.001));
        Assert.That(gamepad.leftStick.right.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.left.ReadValue(), Is.EqualTo(0.0).Within(0.001));

        Assert.That(gamepad.rightStick.x.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.y.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.up.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.down.ReadValue(), Is.EqualTo(0.0).Within(0.001));
        Assert.That(gamepad.rightStick.right.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.left.ReadValue(), Is.EqualTo(0.0).Within(0.001));

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(1));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(1));

        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.A), gamepad.aButton);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.A), gamepad.buttonSouth);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.B), gamepad.bButton);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.B), gamepad.buttonEast);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.X), gamepad.xButton);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.X), gamepad.buttonWest);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.Y), gamepad.yButton);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.Y), gamepad.buttonNorth);

        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.DPadDown), gamepad.dpad.down);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.DPadUp), gamepad.dpad.up);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.DPadLeft), gamepad.dpad.left);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.DPadRight), gamepad.dpad.right);

        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.LeftThumbstickPress), gamepad.leftStickButton);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.RightThumbstickPress), gamepad.rightStickButton);

        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.LeftShoulder), gamepad.leftShoulder);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.RightShoulder), gamepad.rightShoulder);

        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.Start), gamepad.menu);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.Start), gamepad.startButton);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.Select), gamepad.view);
        AssertButtonPress(gamepad, new XInputControllerOSXState().WithButton(XInputControllerOSXState.Button.Select), gamepad.selectButton);
    }

#endif


#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA
    [Test]
    [Category("Devices")]
    public void Devices_SupportXboxControllerOnWindows()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "XInput"
        });

        Assert.That(device, Is.AssignableTo<XInputController>());
        Assert.That(device, Is.AssignableTo<XInputControllerWindows>());
        var gamepad = (XInputControllerWindows)device;

        InputSystem.QueueStateEvent(gamepad,
            new XInputControllerWindowsState
            {
                leftStickX = 32767,
                leftStickY = 32767,
                rightStickX = 32767,
                rightStickY = 32767,
                leftTrigger = 255,
                rightTrigger = 255,
            });

        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.x.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.rightStick.y.ReadValue(), Is.EqualTo(0.9999).Within(0.001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(1));
        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(1));

        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.A), gamepad.aButton);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.A), gamepad.buttonSouth);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.B), gamepad.bButton);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.B), gamepad.buttonEast);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.X), gamepad.xButton);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.X), gamepad.buttonWest);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.Y), gamepad.yButton);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.Y), gamepad.buttonNorth);

        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.DPadDown), gamepad.dpad.down);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.DPadUp), gamepad.dpad.up);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.DPadLeft), gamepad.dpad.left);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.DPadRight), gamepad.dpad.right);

        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.LeftThumbstickPress), gamepad.leftStickButton);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.RightThumbstickPress), gamepad.rightStickButton);

        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.LeftShoulder), gamepad.leftShoulder);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.RightShoulder), gamepad.rightShoulder);

        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.Start), gamepad.menu);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.Start), gamepad.startButton);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.Select), gamepad.view);
        AssertButtonPress(gamepad, new XInputControllerWindowsState().WithButton(XInputControllerWindowsState.Button.Select), gamepad.selectButton);
    }

#endif
}
