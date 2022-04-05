using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

partial class CoreTests
{
    private static IEnumerable<KeyCode> keys
    {
        get
        {
            foreach (var value in System.Enum.GetValues(typeof(KeyCode)))
            {
                var keyCode = (KeyCode)value;
                if (keyCode.ToKey() != null)
                    yield return (KeyCode)value;
            }
        }
    }

    private static IEnumerable<KeyCode> mouseButtons
    {
        get
        {
            foreach (var value in System.Enum.GetValues(typeof(KeyCode)))
            {
                var keyCode = (KeyCode)value;
                if (keyCode.IsMouseButton())
                    yield return keyCode;
            }
        }
    }

    private static IEnumerable<KeyCode> joystickButtons
    {
        get
        {
            foreach (var value in System.Enum.GetValues(typeof(KeyCode)))
            {
                var keyCode = (KeyCode)value;
                if (keyCode.IsJoystickButton())
                    yield return keyCode;
            }
        }
    }

    [UnityTest]
    [Category("API")]
    public IEnumerator API_CanReadKeyboardThroughGetKeyAPI([ValueSource(nameof(keys))] KeyCode keyCode)
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.AddDevice<Mouse>();
        InputSystem.AddDevice<Joystick>();

        Assert.That(Input.GetKey(keyCode), Is.False);
        Assert.That(Input.GetKeyDown(keyCode), Is.False);
        Assert.That(Input.GetKeyUp(keyCode), Is.False);

        var key = keyCode.ToKey().Value;

        Press(keyboard[key]);
        yield return null;

        Assert.That(Input.GetKey(keyCode), Is.True);
        Assert.That(Input.GetKeyDown(keyCode), Is.True);
        Assert.That(Input.GetKeyUp(keyCode), Is.False);

        foreach (var value in System.Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>())
        {
            if (value != keyCode)
            {
                Assert.That(Input.GetKey(value), Is.False,
                    () => $"Expecting key {value} to be in unpressed state");
                Assert.That(Input.GetKeyDown(value), Is.False,
                    () => $"Expecting key {value} to not be marked as pressed-this-frame");
                Assert.That(Input.GetKeyUp(value), Is.False,
                    () => $"Expecting key {value} to not be marked as released-this-frame");
            }
        }

        yield return null;

        Assert.That(Input.GetKey(keyCode), Is.True);
        Assert.That(Input.GetKeyDown(keyCode), Is.False);
        Assert.That(Input.GetKeyUp(keyCode), Is.False);

        foreach (var value in System.Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>())
        {
            if (value != keyCode)
            {
                Assert.That(Input.GetKey(value), Is.False,
                    () => $"Expecting key {value} to be in unpressed state");
                Assert.That(Input.GetKeyDown(value), Is.False,
                    () => $"Expecting key {value} to not be marked as pressed-this-frame");
                Assert.That(Input.GetKeyUp(value), Is.False,
                    () => $"Expecting key {value} to not be marked as released-this-frame");
            }
        }

        Release(keyboard[key]);
        yield return null;

        Assert.That(Input.GetKey(keyCode), Is.False);
        Assert.That(Input.GetKeyDown(keyCode), Is.False);
        Assert.That(Input.GetKeyUp(keyCode), Is.True);

        foreach (var value in System.Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>())
        {
            if (value != keyCode)
            {
                Assert.That(Input.GetKey(value), Is.False,
                    () => $"Expecting key {value} to be in unpressed state");
                Assert.That(Input.GetKeyDown(value), Is.False,
                    () => $"Expecting key {value} to not be marked as pressed-this-frame");
                Assert.That(Input.GetKeyUp(value), Is.False,
                    () => $"Expecting key {value} to not be marked as released-this-frame");
            }
        }

        yield return null;

        Assert.That(Input.GetKey(keyCode), Is.False);
        Assert.That(Input.GetKeyDown(keyCode), Is.False);
        Assert.That(Input.GetKeyUp(keyCode), Is.False);

        foreach (var value in System.Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>())
        {
            if (value != keyCode)
            {
                Assert.That(Input.GetKey(keyCode), Is.False,
                    () => $"Expecting key {value} to be in unpressed state");
                Assert.That(Input.GetKeyDown(keyCode), Is.False,
                    () => $"Expecting key {value} to not be marked as pressed-this-frame");
                Assert.That(Input.GetKeyUp(keyCode), Is.False,
                    () => $"Expecting key {value} to not be marked as released-this-frame");
            }
        }
    }

    [UnityTest]
    [Category("API")]
    public IEnumerator API_CanReadMouseButtonThroughGetKeyAPI([ValueSource(nameof(mouseButtons))] KeyCode keyCode)
    {
        yield return null;
        Assert.Fail();
    }

    [Test]
    [Category("API")]
    public void API_ProcessingKeyPressDoesNotAllocateGCMemory()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        PressAndRelease(keyboard.commaKey);
        var kProfilerRegion = "API_ProcessingKeyPressDoesNotAllocateGCMemory";

        Assert.That(() =>
        {
            Profiler.BeginSample(kProfilerRegion);
            PressAndRelease(keyboard.commaKey);
            Profiler.EndSample();
        }, Is.Not.AllocatingGCMemory());
    }

    // Test mapping between Key and KeyCode enums. We need to support the KeyCode enum because of the
    // legacy API we support and because IMGUI still uses it. However, going forward, we want to steer
    // away from it as it mixes codes for actual keys with those for mouse and joystick buttons.
    [Test]
    [Category("API")]
    [TestCase(KeyCode.A, Key.A)]
    [TestCase(KeyCode.B, Key.B)]
    [TestCase(KeyCode.C, Key.C)]
    [TestCase(KeyCode.D, Key.D)]
    [TestCase(KeyCode.E, Key.E)]
    [TestCase(KeyCode.F, Key.F)]
    [TestCase(KeyCode.G, Key.G)]
    [TestCase(KeyCode.H, Key.H)]
    [TestCase(KeyCode.I, Key.I)]
    [TestCase(KeyCode.J, Key.J)]
    [TestCase(KeyCode.K, Key.K)]
    [TestCase(KeyCode.L, Key.L)]
    [TestCase(KeyCode.M, Key.M)]
    [TestCase(KeyCode.N, Key.N)]
    [TestCase(KeyCode.O, Key.O)]
    [TestCase(KeyCode.P, Key.P)]
    [TestCase(KeyCode.Q, Key.Q)]
    [TestCase(KeyCode.R, Key.R)]
    [TestCase(KeyCode.S, Key.S)]
    [TestCase(KeyCode.T, Key.T)]
    [TestCase(KeyCode.U, Key.U)]
    [TestCase(KeyCode.V, Key.V)]
    [TestCase(KeyCode.W, Key.W)]
    [TestCase(KeyCode.X, Key.X)]
    [TestCase(KeyCode.Y, Key.Y)]
    [TestCase(KeyCode.Z, Key.Z)]
    [TestCase(KeyCode.Space, Key.Space)]
    [TestCase(KeyCode.Backspace, Key.Backspace)]
    [TestCase(KeyCode.Escape, Key.Escape)]
    [TestCase(KeyCode.Return, Key.Enter)]
    [TestCase(KeyCode.Alpha0, Key.Digit0)]
    [TestCase(KeyCode.Alpha1, Key.Digit1)]
    [TestCase(KeyCode.Alpha2, Key.Digit2)]
    [TestCase(KeyCode.Alpha3, Key.Digit3)]
    [TestCase(KeyCode.Alpha4, Key.Digit4)]
    [TestCase(KeyCode.Alpha5, Key.Digit5)]
    [TestCase(KeyCode.Alpha6, Key.Digit6)]
    [TestCase(KeyCode.Alpha7, Key.Digit7)]
    [TestCase(KeyCode.Alpha8, Key.Digit8)]
    [TestCase(KeyCode.Alpha9, Key.Digit9)]
    [TestCase(KeyCode.F1, Key.F1)]
    [TestCase(KeyCode.F2, Key.F2)]
    [TestCase(KeyCode.F3, Key.F3)]
    [TestCase(KeyCode.F4, Key.F4)]
    [TestCase(KeyCode.F5, Key.F5)]
    [TestCase(KeyCode.F6, Key.F6)]
    [TestCase(KeyCode.F7, Key.F7)]
    [TestCase(KeyCode.F8, Key.F8)]
    [TestCase(KeyCode.F9, Key.F9)]
    [TestCase(KeyCode.F10, Key.F10)]
    [TestCase(KeyCode.F11, Key.F11)]
    [TestCase(KeyCode.F12, Key.F12)]
    [TestCase(KeyCode.F13, null)] // These weren't *actually* supported by the old system anyway.
    [TestCase(KeyCode.F14, null)]
    [TestCase(KeyCode.F15, null)]
    [TestCase(KeyCode.Keypad0, Key.Numpad0)]
    [TestCase(KeyCode.Keypad1, Key.Numpad1)]
    [TestCase(KeyCode.Keypad2, Key.Numpad2)]
    [TestCase(KeyCode.Keypad3, Key.Numpad3)]
    [TestCase(KeyCode.Keypad4, Key.Numpad4)]
    [TestCase(KeyCode.Keypad5, Key.Numpad5)]
    [TestCase(KeyCode.Keypad6, Key.Numpad6)]
    [TestCase(KeyCode.Keypad7, Key.Numpad7)]
    [TestCase(KeyCode.Keypad8, Key.Numpad8)]
    [TestCase(KeyCode.Keypad9, Key.Numpad9)]
    [TestCase(KeyCode.KeypadEnter, Key.NumpadEnter)]
    [TestCase(KeyCode.KeypadEquals, Key.NumpadEquals)]
    [TestCase(KeyCode.KeypadMinus, Key.NumpadMinus)]
    [TestCase(KeyCode.KeypadPlus, Key.NumpadPlus)]
    [TestCase(KeyCode.KeypadMultiply, Key.NumpadMultiply)]
    [TestCase(KeyCode.KeypadDivide, Key.NumpadDivide)]
    [TestCase(KeyCode.LeftArrow, Key.LeftArrow)]
    [TestCase(KeyCode.RightArrow, Key.RightArrow)]
    [TestCase(KeyCode.DownArrow, Key.DownArrow)]
    [TestCase(KeyCode.UpArrow, Key.UpArrow)]
    [TestCase(KeyCode.Mouse0, null)]
    [TestCase(KeyCode.Mouse1, null)]
    [TestCase(KeyCode.Mouse2, null)]
    [TestCase(KeyCode.Mouse3, null)]
    [TestCase(KeyCode.Mouse4, null)]
    [TestCase(KeyCode.Joystick1Button0, null)]
    public void API_CanMapKeyCodeEnumToKeyEnum(KeyCode keyCode, Key? key)
    {
        Assert.That(keyCode.ToKey(), Is.EqualTo(key));
    }
}
