#if UNITY_WEBGL || UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.WebGL;
using UnityEngine.Experimental.Input.Plugins.WebGL.LowLevel;

internal class WebGLTests : InputTestFixture
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

        Assert.That(gamepad.leftStick.ReadUnprocessedValue(), Is.EqualTo(new Vector2(0.345f, -0.456f))); // Y inverted on WebGL.
        Assert.That(gamepad.rightStick.ReadUnprocessedValue(), Is.EqualTo(new Vector2(0.567f, -0.678f))); // Y inverted on WebGL.
        Assert.That(gamepad.leftTrigger.ReadUnprocessedValue(), Is.EqualTo(0.123).Within(0.0001));
        Assert.That(gamepad.rightTrigger.ReadUnprocessedValue(), Is.EqualTo(0.234).Within(0.0001));

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

    [Test]
    [Category("Devices")]
    [Ignore("TODO")]
    public void TODO_Devices_SupportsWebGLJoysticks()
    {
        Assert.Fail();
    }
}
#endif // UNITY_WEBGL || UNITY_EDITOR
