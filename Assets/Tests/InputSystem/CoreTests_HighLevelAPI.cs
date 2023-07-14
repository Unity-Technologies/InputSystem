#if UNITY_INPUT_SYSTEM_ENABLE_GLOBAL_ACTIONS_API

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.TestTools.Utils;
using GamepadButton = UnityEngine.InputSystem.LowLevel.GamepadButton;
using Input = UnityEngine.InputSystem.Input;
using Is = NUnit.Framework.Is;
using Random = UnityEngine.Random;

internal partial class CoreTests
{
    // TODO rename HighLevelAPI to something that makes sense
    // one perspective is to see this API as an evolution of input manager API, aka input manager 2.0
    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_CanQueryControl()
    {
        // gamepad sticks can only be engaged in two directions
        // at a same time (either left+up or right+down or permutation of both)
        // hence we need to ignore some buttons
        var buttonsToIgnore = new HashSet<Inputs>
        {
            Inputs.Gamepad_LeftStickDown,
            Inputs.Gamepad_LeftStickRight,
            Inputs.Gamepad_RightStickUp,
            Inputs.Gamepad_RightStickLeft
        };

        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // add a joystick using the HID path that contains one stick and eight buttons
        var joystick = AddHidJoystick();

        // check that all controls are not actuated
        foreach (var value in typeof(Inputs).GetEnumValues())
        {
            var input = (Inputs)value;
            Assert.That(Input.IsPressed(input), Is.False, $"Input '{input}' should be 'not pressed'");
            Assert.That(Input.WasPressedThisFrame(input), Is.False, $"Input '{input}' should be 'not down'");
            Assert.That(Input.WasReleasedThisFrame(input), Is.False, $"Input '{input}' should be 'not up'");
        }

        // press all buttons
        var keyboardState = new KeyboardState();
        foreach (var value in typeof(Key).GetEnumValues())
            keyboardState.Press((Key)value);
        InputSystem.QueueStateEvent(keyboard, keyboardState);

        var mouseState = new MouseState()
            .WithButton(MouseButton.Left)
            .WithButton(MouseButton.Right)
            .WithButton(MouseButton.Middle)
            .WithButton(MouseButton.Forward)
            .WithButton(MouseButton.Back);
        InputSystem.QueueStateEvent(mouse, mouseState);

        var gamepadState = new GamepadState()
            .WithButton(GamepadButton.DpadUp)
            .WithButton(GamepadButton.DpadDown)
            .WithButton(GamepadButton.DpadLeft)
            .WithButton(GamepadButton.DpadRight)
            .WithButton(GamepadButton.North)
            .WithButton(GamepadButton.East)
            .WithButton(GamepadButton.South)
            .WithButton(GamepadButton.West)
            .WithButton(GamepadButton.LeftStick)
            .WithButton(GamepadButton.RightStick)
            .WithButton(GamepadButton.LeftShoulder)
            .WithButton(GamepadButton.RightShoulder)
            .WithButton(GamepadButton.Start)
            .WithButton(GamepadButton.Select);
        gamepadState.leftStick = new Vector2(-1, 1);
        gamepadState.rightStick = new Vector2(1, -1);
        gamepadState.leftTrigger = 1.0f;
        gamepadState.rightTrigger = 1.0f;
        InputSystem.QueueStateEvent(gamepad, gamepadState);
        InputSystem.QueueStateEvent(joystick, new HidJoystickState
        {
            reportId = 1,
            x = ushort.MaxValue,
            y = ushort.MaxValue,
            buttons = 255 // all 8 buttons pressed
        });

        // check that all buttons are pressed, and control down is true for the first frame
        InputSystem.Update();
        foreach (var value in typeof(Inputs).GetEnumValues())
        {
            var input = (Inputs)value;
            if (buttonsToIgnore.Contains(input))
                continue;

            Assert.That(Input.IsPressed(input), Is.True, $"Input '{input}' should be 'pressed'");
            Assert.That(Input.WasPressedThisFrame(input), Is.True, $"Input '{input}' should be 'down'");
            Assert.That(Input.WasReleasedThisFrame(input), Is.False, $"Input '{input}' should be 'not up'");
        }

        // check that WasPressedThisFrame became false after one frame
        InputSystem.Update();
        foreach (var value in typeof(Inputs).GetEnumValues())
        {
            var input = (Inputs)value;
            if (buttonsToIgnore.Contains(input))
                continue;

            Assert.That(Input.IsPressed(input), Is.True, $"Input '{input}' should be 'pressed'");
            Assert.That(Input.WasPressedThisFrame(input), Is.False, $"Input '{input}' should be 'not down'");
            Assert.That(Input.WasReleasedThisFrame(input), Is.False, $"Input '{input}' should be 'not up'");
        }

        // release everything
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.QueueStateEvent(joystick, new HidJoystickState
        {
            reportId = 1,
            x = 0,
            y = 0,
            buttons = 0 // all 8 buttons released
        });

        // check that all controls are not pressed, and control up became true
        InputSystem.Update();
        foreach (var value in typeof(Inputs).GetEnumValues())
        {
            var input = (Inputs)value;
            if (buttonsToIgnore.Contains(input))
                continue;

            Assert.That(Input.IsPressed(input), Is.False, $"Input '{input}' should be 'not pressed'");
            Assert.That(Input.WasPressedThisFrame(input), Is.False, $"Input '{input}' should be 'not down'");
            Assert.That(Input.WasReleasedThisFrame(input), Is.True, $"Input '{input}' should be 'up'");
        }

        // check control up became false after one frame
        InputSystem.Update();
        foreach (var value in typeof(Inputs).GetEnumValues())
        {
            var input = (Inputs)value;
            Assert.That(Input.IsPressed(input), Is.False, $"Input '{input}' should be 'not pressed'");
            Assert.That(Input.WasPressedThisFrame(input), Is.False, $"Input '{input}' should be 'not down'");
            Assert.That(Input.WasReleasedThisFrame(input), Is.False, $"Input '{input}' should be 'not up'");
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct HidJoystickState : IInputStateTypeInfo
    {
        [FieldOffset(0)] public byte reportId;
        [FieldOffset(1)] public ushort x;
        [FieldOffset(3)] public ushort y;
        [FieldOffset(5)] public int buttons;

        public FourCC format => new FourCC('H', 'I', 'D');
    }

    [Test]
    [Category("HighLevelAPI")]
    [TestCase(InputSlot.Slot1)]
    [TestCase(InputSlot.Slot2)]
    public void HighLevelAPI_CanQueryGamepadControl(InputSlot slot)
    {
        var gamepadOne = InputSystem.AddDevice<Gamepad>();
        var gamepadTwo = InputSystem.AddDevice<Gamepad>();
        var expectedUnusedSlot = slot == InputSlot.Slot2 ? InputSlot.Slot1 : InputSlot.Slot2;
        var gamepadButtons = Enum.GetValues(typeof(UnityEngine.InputSystem.InputGamepadButton))
            .Cast<UnityEngine.InputSystem.InputGamepadButton>()
            .ToList();

        var gamepadState = new GamepadState(
            GamepadButton.DpadUp,
            GamepadButton.DpadDown,
            GamepadButton.DpadLeft,
            GamepadButton.DpadRight,
            GamepadButton.North,
            GamepadButton.East,
            GamepadButton.South,
            GamepadButton.West,
            GamepadButton.LeftStick,
            GamepadButton.RightStick,
            GamepadButton.LeftShoulder,
            GamepadButton.RightShoulder,
            GamepadButton.Start,
            GamepadButton.Select)
        {
            leftStick = new Vector2(-1, 1),
            rightStick = new Vector2(1, -1),
            leftTrigger = 1.0f,
            rightTrigger = 1.0f
        };
        InputSystem.QueueStateEvent(slot == InputSlot.Slot1 ? gamepadOne : gamepadTwo, gamepadState);
        InputSystem.Update();

        foreach (var buttonValue in gamepadButtons)
        {
            AssertControlStates(buttonValue, true, true, false, slot);
            AssertControlStates(buttonValue, false, false, false, expectedUnusedSlot);
        }

        InputSystem.Update();

        foreach (var buttonValue in gamepadButtons)
        {
            AssertControlStates(buttonValue, false, true, false, slot);
            AssertControlStates(buttonValue, false, false, false, expectedUnusedSlot);
        }

        InputSystem.QueueStateEvent(slot == InputSlot.Slot1 ? gamepadOne : gamepadTwo, new GamepadState());
        InputSystem.Update();

        foreach (var buttonValue in gamepadButtons)
        {
            AssertControlStates(buttonValue, false, false, true, slot);
            AssertControlStates(buttonValue, false, false, false, expectedUnusedSlot);
        }

        void AssertControlStates(UnityEngine.InputSystem.InputGamepadButton gamepadButton,
            bool controlDown, bool controlPressed, bool controlUp, InputSlot gamepadSlot)
        {
            Assert.That(Input.WasPressedThisFrame(gamepadButton, gamepadSlot), Is.EqualTo(controlDown));
            Assert.That(Input.IsPressed(gamepadButton, gamepadSlot), Is.EqualTo(controlPressed));
            Assert.That(Input.WasReleasedThisFrame(gamepadButton, gamepadSlot), Is.EqualTo(controlUp));
        }
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_CanQuerySpecificJoystick()
    {
        var joystickOne = InputSystem.AddDevice<Joystick>();
        var joystickTwo = AddHidJoystick();

        Set((ButtonControl)joystickTwo["button2"], 1);

        Assert.That(Input.WasPressedThisFrame(JoystickButton.Button2, InputSlot.Slot1), Is.False);
        Assert.That(Input.WasPressedThisFrame(JoystickButton.Button2, InputSlot.Slot2), Is.True);

        InputSystem.Update();

        Assert.That(Input.WasPressedThisFrame(JoystickButton.Button2, InputSlot.Slot2), Is.False);
        Assert.That(Input.IsPressed(JoystickButton.Button2, InputSlot.Slot2), Is.True);

        Set((ButtonControl)joystickTwo["button2"], 0);

        Assert.That(Input.IsPressed(JoystickButton.Button2, InputSlot.Slot2), Is.False);
        Assert.That(Input.WasReleasedThisFrame(JoystickButton.Button2, InputSlot.Slot2), Is.True);

        InputSystem.Update();

        Assert.That(Input.WasReleasedThisFrame(JoystickButton.Button2, InputSlot.Slot2), Is.False);
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_CanQueryGetAxis()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var keyboardState = new KeyboardState();
        keyboardState.Press(Key.W);
        keyboardState.Press(Key.A);
        InputSystem.QueueStateEvent(keyboard, keyboardState);

        var gamepadState = new GamepadState()
            .WithButton(GamepadButton.North)
            .WithButton(GamepadButton.West);
        gamepadState.leftStick = new Vector2(-1, -1);
        gamepadState.rightStick = new Vector2(1, 1);
        gamepadState.leftTrigger = 0.7f;
        InputSystem.QueueStateEvent(gamepad, gamepadState);
        InputSystem.Update();

        // normal buttons should return 0.0f or 1.0f
        Assert.That(Input.GetAxis(Inputs.Key_W), Is.EqualTo(1.0f));
        Assert.That(Input.GetAxis(Inputs.Key_S), Is.EqualTo(0.0f));
        Assert.That(Input.GetAxis(Inputs.Gamepad_North), Is.EqualTo(1.0f));
        Assert.That(Input.GetAxis(Inputs.Gamepad_South), Is.EqualTo(0.0f));

        Assert.That(Input.GetAxis(Inputs.Key_A, Inputs.Key_D), Is.EqualTo(-1.0f));
        Assert.That(Input.GetAxis(Inputs.Key_S, Inputs.Key_W), Is.EqualTo(1.0f));
        Assert.That(Input.GetAxis(Inputs.Key_A, Inputs.Key_W), Is.EqualTo(0.0f));
        Assert.That(Input.GetAxis(Inputs.Gamepad_North, Inputs.Gamepad_East), Is.EqualTo(-1.0f));
        Assert.That(Input.GetAxis(Inputs.Gamepad_East, Inputs.Gamepad_West), Is.EqualTo(1.0f));
        Assert.That(Input.GetAxis(Inputs.Gamepad_North, Inputs.Gamepad_West), Is.EqualTo(0.0f));

        // triggers should return [0.0f, 1.0f]
        Assert.That(Input.GetAxis(Inputs.Gamepad_LeftTrigger), Is.EqualTo(gamepadState.leftTrigger));
        Assert.That(Input.GetAxis(Inputs.Gamepad_RightTrigger), Is.EqualTo(0.0f));

        // check normalization
        Assert.That(Input.GetAxisRaw(Inputs.Key_A, Inputs.Key_D, Inputs.Key_W, Inputs.Key_S), Is.EqualTo(new Vector2(-1, 1)));
        Assert.That(Input.GetAxis(Inputs.Key_A, Inputs.Key_D, Inputs.Key_W, Inputs.Key_S),
            Is.EqualTo(new Vector2(-0.71f, 0.71f)).Using(new Vector2EqualityComparer(0.01f)));
        Assert.That(Input.GetAxisRaw(Inputs.Gamepad_West, Inputs.Gamepad_East, Inputs.Gamepad_North, Inputs.Gamepad_East), Is.EqualTo(new Vector2(-1, 1)));
        Assert.That(Input.GetAxis(Inputs.Gamepad_West, Inputs.Gamepad_East, Inputs.Gamepad_North, Inputs.Gamepad_East),
            Is.EqualTo(new Vector2(-0.71f, 0.71f)).Using(new Vector2EqualityComparer(0.01f)));

        // sticks go via different path
        Assert.That(Input.GetAxis(GamepadAxis.LeftStick), Is.EqualTo(new Vector2(-0.71f, -0.71f)).Using(new Vector2EqualityComparer(0.01f)));
        Assert.That(Input.GetAxis(GamepadAxis.RightStick), Is.EqualTo(new Vector2(0.71f, 0.71f)).Using(new Vector2EqualityComparer(0.01f)));
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_GamepadsCollectionIsInitializedToMaxSlots()
    {
        Assert.That(Input.gamepads.Count, Is.EqualTo(Input.maxGamepadSlots));
        Assert.That(Input.gamepads, Is.EquivalentTo(Enumerable.Repeat<InputDevice>(null, Input.maxGamepadSlots)));
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_JoysticksCollectionIsInitializedToMaxSlots()
    {
        Assert.That(Input.joysticks.Count, Is.EqualTo((int)InputSlot.Joystick_Max));
        Assert.That(Input.joysticks, Is.EquivalentTo(Enumerable.Repeat<InputDevice>(null, (int)InputSlot.Joystick_Max)));
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_GamepadsOccupyFirstFreeSlotOnConnectOrReconnect()
    {
        var gamepadOne = InputSystem.AddDevice<Gamepad>();
        var gamepadTwo = InputSystem.AddDevice<Gamepad>();
        var gamepadThree = InputSystem.AddDevice<Gamepad>();

        Assert.That(Input.gamepads[0], Is.EqualTo(gamepadOne));
        Assert.That(Input.gamepads[1], Is.EqualTo(gamepadTwo));
        Assert.That(Input.gamepads[2], Is.EqualTo(gamepadThree));

        InputSystem.RemoveDevice(gamepadTwo);

        var gamepadFour = InputSystem.AddDevice<Gamepad>();

        Assert.That(Input.gamepads[1], Is.EqualTo(gamepadFour));
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_IsGamepadConnected_ReturnsTrueForOccupiedSlots(
        [NUnit.Framework.Range(0, 11)] int disconnectedSlot)
    {
        for (var i = 0; i < Input.maxGamepadSlots; i++)
        {
            InputSystem.AddDevice<Gamepad>();

            Assert.That(Input.IsGamepadConnected((InputSlot)i), Is.True);
        }

        InputSystem.RemoveDevice(Gamepad.all[disconnectedSlot]);

        Assert.That(Input.IsGamepadConnected((InputSlot)disconnectedSlot), Is.False);
    }

    [UnityTest]
    [Category("HighLevelAPI")]
    public IEnumerator HighLevelAPI_DidGamepadConnectThisFrame_IsTrueInTheFrameTheGamepadConnectedEventIsProcessed()
    {
        // can't use InputSystem.AddDevice here because that immediately raises the onDeviceChanged event, so the
        // frame count will be one too early. We need to wait for one full Unity player loop update to run
        runtime.ReportNewInputDevice<Gamepad>();
        yield return null;

        Assert.That(Input.DidGamepadConnectThisFrame(InputSlot.Slot1), Is.True);

        yield return null;
        Assert.That(Input.DidGamepadConnectThisFrame(InputSlot.Slot1), Is.False);
    }

    [UnityTest]
    [Category("HighLevelAPI")]
    public IEnumerator HighLevelAPI_DidAllGamepadsConnectOrDisconnectThisFrame()
    {
        for (var i = 0; i < Input.maxGamepadSlots; i++)
        {
            runtime.ReportNewInputDevice<Gamepad>();
        }

        yield return null;

        Assert.That(Input.DidGamepadConnectThisFrame(InputSlot.All), Is.True);

        foreach (var inputDevice in InputSystem.devices)
        {
            runtime.ReportInputDeviceRemoved(inputDevice);
        }

        yield return null;

        Assert.That(Input.DidGamepadDisconnectThisFrame(InputSlot.All), Is.True);
    }

    [UnityTest]
    [Category("HighLevelAPI")]
    public IEnumerator HighLevelAPI_DidGamepadDisconnectThisFrame_IsTrueInTheFrameTheGamepadDisconnectedEventIsProcessed()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        yield return null;
        Assert.That(Input.DidGamepadDisconnectThisFrame(InputSlot.Slot1), Is.False);

        // can't use RemoveDevice here because that immediately raises the onDeviceChanged event, so the
        // frame count will be one too early.
        runtime.ReportInputDeviceRemoved(gamepad);
        yield return null;
        Assert.That(Input.DidGamepadDisconnectThisFrame(InputSlot.Slot1), Is.True);

        yield return null;
        Assert.That(Input.DidGamepadDisconnectThisFrame(InputSlot.Slot1), Is.False);
    }

    #if UNITY_EDITOR
    [UnityTest]
    [Category("HighLevelAPI")]
    public IEnumerator HighLevelAPI_DidGamepadConnectThisFrame_WorksWhenEventArrivesBeforeEditorUpdate()
    {
        // this test simulates the editor behaviour where a device connected (or disconnected) event can occur
        // before the editor loop update. In that case, the event gets sent through to managed code before the
        // Time.frameCount property has been updated for this frame.
        var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
        playerLoop.InsertSystemAsSubSystemOf<TestAsyncDeviceChangedEventPlayerLoop, TimeUpdate>(
            () =>
            {
                // this code will run before the frameCount has been updated for the frame, and calling AddDevice
                // will make the onDeviceChanged handler run in the Input class.
                InputSystem.AddDevice<Gamepad>();
            }, true);
        PlayerLoop.SetPlayerLoop(playerLoop);

        yield return null;
        try
        {
            Assert.That(Input.DidGamepadConnectThisFrame(InputSlot.Slot1), Is.True);
        }
        finally
        {
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());
        }
    }

    private struct TestAsyncDeviceChangedEventPlayerLoop {}
    #endif

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_CanSetGamepadTriggerPoint()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var gamepadState = new GamepadState();
        gamepadState.leftTrigger = 0.3f;
        InputSystem.QueueStateEvent(gamepad, gamepadState);
        InputSystem.Update();

        Input.SetGamepadTriggerPressPoint(0.5f);
        Assert.That(Input.IsPressed(Inputs.Gamepad_LeftTrigger), Is.False);

        Input.SetGamepadTriggerPressPoint(0.3f);
        Assert.That(Input.IsPressed(Inputs.Gamepad_LeftTrigger), Is.True);

        gamepadState.leftTrigger = 0.1f;
        InputSystem.QueueStateEvent(gamepad, gamepadState);
        InputSystem.Update();
        Assert.That(Input.IsPressed(Inputs.Gamepad_LeftTrigger), Is.False);
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_CanSetGamepadDeadZone()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var gamepadState = new GamepadState();
        gamepadState.leftStick = new Vector2(-0.3f, 0.3f);
        gamepadState.rightStick = new Vector2(0.7f, -0.7f);
        InputSystem.QueueStateEvent(gamepad, gamepadState);
        InputSystem.Update();

        Input.SetGamepadStickDeadzone(0.0f);
        Assert.That(Input.GetAxis(GamepadAxis.LeftStick), Is.EqualTo(new Vector2(-0.3f, 0.3f)).Using(new Vector2EqualityComparer(0.01f)));

        Input.SetGamepadStickDeadzone(0.3f);
        Assert.That(Input.GetAxis(GamepadAxis.LeftStick), Is.EqualTo(new Vector2(-0.13f, 0.13f)).Using(new Vector2EqualityComparer(0.01f)));
        Assert.That(Input.GetAxis(GamepadAxis.RightStick), Is.EqualTo(new Vector2(0.70f, -0.70f)).Using(new Vector2EqualityComparer(0.01f)));
    }

    [Test]
    [TestCase("Mouse")]
    [TestCase("Pen")]
    [TestCase("Touchscreen")]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_PointerPresentReturnsCorrectValue(string deviceName)
    {
        var pointer = InputSystem.AddDevice(deviceName);

        Assert.That(Input.pointerPresent, Is.True);

        InputSystem.RemoveDevice(pointer);

        Assert.That(Input.pointerPresent, Is.False);
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_PointerPositionReturnsZeroWhenNoPointerIsAttached()
    {
        Assert.That(Input.pointerPosition, Is.EqualTo(Vector2.zero));
    }

    [Test]
    [TestCase("Mouse")]
    [TestCase("Pen")]
    [TestCase("Touchscreen")]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_PointerPositionReturnsThePointerPositionWhenAnyPointerDeviceIsAttached(string deviceName)
    {
        var device = (Pointer)InputSystem.AddDevice(deviceName);

        if (device is Touchscreen)
            BeginTouch(4, new Vector2(123, 234));
        else
            Set(device.position, new Vector2(123, 234));

        Assert.That(Input.pointerPosition, Is.EqualTo(new Vector2(123, 234)));
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_ScrollDeltaReturnsScrollWheelDelta()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        Set(mouse.scroll, new Vector2(123, 456));

        Assert.That(Input.scrollDelta, Is.EqualTo(new Vector2(123, 456)));
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_JoysticksAreAddedAndRemovedFromCollection()
    {
        var joysticks = new List<Joystick>();
        for (var i = 0; i < (int)InputSlot.Joystick_Max; i++)
        {
            joysticks.Add(InputSystem.AddDevice<Joystick>());
            Assert.That(Input.joysticks[0], Is.Not.Null);
            Assert.That(Input.joysticks[i], Is.EqualTo(joysticks[i]));
        }

        // remove a joystick from the middle of the collection and make sure the joysticks array doesn't rearrange
        InputSystem.RemoveDevice(joysticks[2]);

        Assert.That(Input.joysticks[0], Is.EqualTo(joysticks[0]));
        Assert.That(Input.joysticks[1], Is.EqualTo(joysticks[1]));
        Assert.That(Input.joysticks[2], Is.Null);
        Assert.That(Input.joysticks[3], Is.EqualTo(joysticks[3]));

        // now just clear everything
        foreach (var joystick in joysticks)
        {
            InputSystem.RemoveDevice(joystick);
        }

        Assert.That(Input.joysticks.All(j => j == null), Is.True);
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_CanQueryJoystickMainAxisValueOnAnyJoystick()
    {
        Random.InitState((int)DateTime.Now.Ticks);

        var joysticks = new List<Joystick>();
        for (var i = 0; i < 3; i++)
        {
            joysticks.Add(InputSystem.AddDevice<Joystick>());
            Set(joysticks[i].stick, Random.insideUnitCircle, queueEventOnly: true);
        }

        // make sure this works for joysticks added through the HID path
        joysticks.Add(AddHidJoystick());
        Set(joysticks[3].stick, Random.insideUnitCircle, queueEventOnly: true);

        InputSystem.Update();

        for (var i = 0; i < joysticks.Count; i++)
        {
            var joystick = joysticks[i];
            var value = joystick.stick.ReadUnprocessedValue();
            value = Input.NormalizeAxis(value, Input.kDefaultJoystickDeadzone);
            Assert.That(value, Is.EqualTo(Input.GetAxis((InputSlot)i)));
        }
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_IsGamepadConnectedReturnsTrueWhenAllIsSpecifiedAndAllSlotsAreFilled()
    {
        foreach (var gamepadSlotEnum in Input.gamepadSlotEnums)
        {
            InputSystem.AddDevice<Gamepad>();
        }

        Assert.That(Input.IsGamepadConnected(InputSlot.All), Is.True);

        InputSystem.RemoveDevice(InputSystem.devices[0]);

        Assert.That(Input.IsGamepadConnected(InputSlot.All), Is.False);
    }

    [Test]
    [Category("HighLevelAPI")]
    public void HighLevelAPI_BasicAPIMethodsDoNotAllocate()
    {
        // Warm up all the methods so we don't log jitting and generic type generation as allocations
        ExerciseInputMethods();

        Assert.That(ExerciseInputMethods, Is.Not.AllocatingGCMemory());
    }

    private static void ExerciseInputMethods()
    {
        Input.WasPressedThisFrame(Inputs.Key_A);
        Input.WasPressedThisFrame(Inputs.Mouse_Left);
        Input.WasPressedThisFrame(Inputs.Gamepad_A);
        Input.WasPressedThisFrame(Inputs.Joystick_Trigger);

        Input.WasReleasedThisFrame(Inputs.Key_A);
        Input.WasReleasedThisFrame(Inputs.Mouse_Left);
        Input.WasReleasedThisFrame(Inputs.Gamepad_A);
        Input.WasReleasedThisFrame(Inputs.Joystick_Trigger);

        Input.IsPressed(Inputs.Key_A);
        Input.IsPressed(Inputs.Mouse_Left);
        Input.IsPressed(Inputs.Gamepad_A);
        Input.IsPressed(Inputs.Joystick_Trigger);

        Input.IsGamepadConnected(InputSlot.All);
        Input.IsGamepadConnected(InputSlot.Slot1);

        Input.DidGamepadConnectThisFrame(InputSlot.Slot1);
        Input.DidGamepadConnectThisFrame(InputSlot.All);
        Input.DidGamepadDisconnectThisFrame(InputSlot.Slot1);
        Input.DidGamepadDisconnectThisFrame(InputSlot.All);

        Input.GetAxis(GamepadAxis.LeftStick, InputSlot.All);
        Input.GetAxis(GamepadAxis.LeftStick, InputSlot.Slot1);

        Input.GetAxis(Inputs.Key_A);
        Input.GetAxis(Inputs.Mouse_Left);
        Input.GetAxis(Inputs.Gamepad_A);
        Input.GetAxis(Inputs.Joystick_Trigger);

        Input.GetAxis(InputSlot.All);
    }

    private Joystick AddHidJoystick()
    {
        var hidDescriptor = new HID.HIDDeviceDescriptor
        {
            usage = (int)HID.GenericDesktop.Joystick,
            usagePage = HID.UsagePage.GenericDesktop,
            vendorId = 0x1234,
            productId = 0x5678,
            inputReportSize = 40,
            elements = new[]
            {
                // 16bit X and Y axes.
                new HID.HIDElementDescriptor
                {
                    usage = (int)HID.GenericDesktop.X, usagePage = HID.UsagePage.GenericDesktop,
                    reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 0, reportSizeInBits = 16
                },
                new HID.HIDElementDescriptor
                {
                    usage = (int)HID.GenericDesktop.Y, usagePage = HID.UsagePage.GenericDesktop,
                    reportType = HID.HIDReportType.Input, reportId = 1, reportOffsetInBits = 16, reportSizeInBits = 16
                },
                new HID.HIDElementDescriptor
                {
                    usage = 1, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1,
                    reportOffsetInBits = 32, reportSizeInBits = 1
                },
                new HID.HIDElementDescriptor
                {
                    usage = 2, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1,
                    reportOffsetInBits = 33, reportSizeInBits = 1
                },
                new HID.HIDElementDescriptor
                {
                    usage = 3, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1,
                    reportOffsetInBits = 34, reportSizeInBits = 1
                },
                new HID.HIDElementDescriptor
                {
                    usage = 4, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1,
                    reportOffsetInBits = 35, reportSizeInBits = 1
                },
                new HID.HIDElementDescriptor
                {
                    usage = 5, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1,
                    reportOffsetInBits = 36, reportSizeInBits = 1
                },
                new HID.HIDElementDescriptor
                {
                    usage = 6, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1,
                    reportOffsetInBits = 37, reportSizeInBits = 1
                },
                new HID.HIDElementDescriptor
                {
                    usage = 7, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1,
                    reportOffsetInBits = 38, reportSizeInBits = 1
                },
                new HID.HIDElementDescriptor
                {
                    usage = 8, usagePage = HID.UsagePage.Button, reportType = HID.HIDReportType.Input, reportId = 1,
                    reportOffsetInBits = 39, reportSizeInBits = 1
                },
            }
        };

        var deviceId = runtime.ReportNewInputDevice(
            new InputDeviceDescription
            {
                interfaceName = HID.kHIDInterface,
                capabilities = hidDescriptor.ToJson()
            }.ToJson());

        InputSystem.Update();

        return InputSystem.GetDeviceById(deviceId) as Joystick;
    }
}
#endif
