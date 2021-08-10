#if UNITY_WEBGL || UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.WebGL;
using UnityEngine.InputSystem.WebGL.LowLevel;

internal class WebGLTests : CoreTestsFixture
{
    [Test]
    [Category("Devices")]
    public void Devices_SupportsWebGLStandardGamepads()
    {
        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            interfaceName = "WebGL",
            deviceClass = "Gamepad",
            capabilities = new WebGLDeviceCapabilities
            {
                mapping = "standard"
            }.ToJson()
        });

        InputSystem.Update();

        var gamepad = InputSystem.GetDevice<WebGLGamepad>();
        Assert.That(gamepad, Is.Not.Null);

        // Test the sticks and triggers.
        InputSystem.QueueStateEvent(gamepad, new WebGLGamepadState
        {
            leftTrigger = 0.123f,
            rightTrigger = 0.234f,
            leftStick = new Vector2(0.345f, 0.456f),
            rightStick = new Vector2(0.567f, 0.678f),
        });
        InputSystem.Update();

        Assert.That(gamepad.leftTrigger.ReadUnprocessedValue(), Is.EqualTo(0.123).Within(0.0001));
        Assert.That(gamepad.rightTrigger.ReadUnprocessedValue(), Is.EqualTo(0.234).Within(0.0001));

        AssertStickValues(gamepad.leftStick, new Vector2(0.345f, -0.456f), -0.456f, 0, 0, 0.345f);
        AssertStickValues(gamepad.rightStick, new Vector2(0.567f, -0.678f), -0.678f, 0, 0, 0.567f);


        InputSystem.QueueStateEvent(gamepad, new WebGLGamepadState
        {
            leftStick = new Vector2(-0.345f, -0.456f),
            rightStick = new Vector2(-0.567f, -0.678f),
        });
        InputSystem.Update();

        AssertStickValues(gamepad.leftStick, new Vector2(-0.345f, 0.456f), 0, -0.456f, 0.345f, 0);
        AssertStickValues(gamepad.rightStick, new Vector2(-0.567f, 0.678f), 0, -0.678f, 0.567f, 0);


        // Test all buttons.
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.South), gamepad[GamepadButton.South]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.North), gamepad[GamepadButton.North]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.East), gamepad[GamepadButton.East]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.West), gamepad[GamepadButton.West]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.Select), gamepad[GamepadButton.Select]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.Start), gamepad[GamepadButton.Start]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.LeftStick), gamepad[GamepadButton.LeftStick]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.RightStick), gamepad[GamepadButton.RightStick]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.LeftShoulder), gamepad[GamepadButton.LeftShoulder]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.RightShoulder), gamepad[GamepadButton.RightShoulder]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.DpadUp), gamepad[GamepadButton.DpadUp]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.DpadDown), gamepad[GamepadButton.DpadDown]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.DpadLeft), gamepad[GamepadButton.DpadLeft]);
        AssertButtonPress(gamepad, new WebGLGamepadState().WithButton(GamepadButton.DpadRight), gamepad[GamepadButton.DpadRight]);
    }

    struct TestJoystickState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC("HTML");
        public float x, y, z;
        public float button1, button2, button3;
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsWebGLJoysticks()
    {
        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            interfaceName = "WebGL",
            deviceClass = "Gamepad",
            product = "WebGL test joystick",
            capabilities = new WebGLDeviceCapabilities
            {
                mapping = "",
                numAxes = 3,
                numButtons = 3
            }.ToJson()
        });

        InputSystem.Update();

        var joystick = InputSystem.GetDevice<WebGLJoystick>();
        Assert.That(joystick , Is.Not.Null);

        // Test the sticks and triggers.
        InputSystem.QueueStateEvent(joystick , new TestJoystickState {x = 0.1f, y = 0.2f, z = 0.3f});
        InputSystem.Update();

        Assert.That((joystick.TryGetChildControl("stick") as StickControl).ReadUnprocessedValue(), Is.EqualTo(new Vector2(0.1f, -0.2f))); // Y inverted on WebGL.
        Assert.That((joystick.TryGetChildControl("Axis 1") as AxisControl).ReadUnprocessedValue(), Is.EqualTo(0.3).Within(0.0001));

        // Test all buttons.
        AssertButtonPress(joystick, new TestJoystickState {button1 = 1.0f}, joystick.TryGetChildControl("Trigger") as ButtonControl, joystick.TryGetChildControl("Button 1") as ButtonControl);
        AssertButtonPress(joystick, new TestJoystickState {button2 = 1.0f}, joystick.TryGetChildControl("Trigger") as ButtonControl, joystick.TryGetChildControl("Button 2") as ButtonControl);
        AssertButtonPress(joystick, new TestJoystickState {button3 = 1.0f}, joystick.TryGetChildControl("Trigger") as ButtonControl, joystick.TryGetChildControl("Button 3") as ButtonControl);
        AssertButtonPress(joystick, new TestJoystickState());
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanHaveWebGLJoystickWithBadRegexInName()
    {
        runtime.ReportNewInputDevice(new InputDeviceDescription
        {
            interfaceName = "WebGL",
            deviceClass = "Gamepad",
            product = "Bad(Regex",
            capabilities = new WebGLDeviceCapabilities
            {
                mapping = "",
                numAxes = 3,
                numButtons = 3
            }.ToJson()
        });

        InputSystem.Update();

        var joystick = InputSystem.GetDevice<WebGLJoystick>();
        Assert.That(joystick , Is.Not.Null);
    }
}
#endif // UNITY_WEBGL || UNITY_EDITOR
