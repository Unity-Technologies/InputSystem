using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.XInput;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input.Layouts;

#if UNITY_EDITOR || UNITY_XBOXONE
using UnityEngine.Experimental.Input.Plugins.XInput.LowLevel;
#endif

public class XInputTests : InputTestFixture
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
}
