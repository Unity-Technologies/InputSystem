using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using NUnit.Framework;
using Unity.Collections;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Users;

////TODO: add test for domain reload logic

// IMPORTANT: When running in editor, make sure to turn off debugging (disable "Editor Attaching" in
//            editor preferences and restart editor) when running tests here. If debugging is enabled,
//            the code will run A LOT slower.

internal class CorePerformanceTests : CoreTestsFixture
{
    public override void Setup()
    {
        base.Setup();

        // InputTestFixture enables leak detection with stack traces for native collections. The
        // stack traces make each native container allocation extremely expensive. For our
        // performance tests, turn this off entirely.
        NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
    }

    ////TODO: same test but with several actions listening on each gamepad
    [Test, Performance]
    [Category("Performance")]
    public void Performance_Update10Gamepads()
    {
        const int kNumGamepads = 10;

        var gamepads = new Gamepad[kNumGamepads];
        for (var i = 0; i < kNumGamepads; ++i)
            gamepads[i] = InputSystem.AddDevice<Gamepad>();

        Measure.Method(() =>
        {
            for (var i = 0; i < kNumGamepads; ++i)
                InputSystem.QueueStateEvent(gamepads[i], default(GamepadState));
            InputSystem.Update();
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    public void Performance_UpdateMouse100TimesInFrame()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        Measure.Method(() =>
        {
            for (var i = 0; i < 100; ++i)
                InputSystem.QueueStateEvent(mouse, default(MouseState));
            InputSystem.Update();
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    [TestCase(true)]
    [TestCase(false)]
    public void Performance_TwoTouchesOverThreeFrames(bool enableEnhancedTouch)
    {
        InputSystem.AddDevice<Touchscreen>();

        if (enableEnhancedTouch)
            EnhancedTouchSupport.Enable();

        Measure.Method(() =>
        {
            BeginTouch(1, new Vector2(123, 234));

            BeginTouch(2, new Vector2(234, 345), queueEventOnly: true);
            MoveTouch(1, new Vector2(345, 456), queueEventOnly: true);
            MoveTouch(1, new Vector2(456, 567), queueEventOnly: true);
            MoveTouch(1, new Vector2(567, 678), queueEventOnly: true);
            MoveTouch(1, new Vector2(789, 890), queueEventOnly: true);
            InputSystem.Update();

            EndTouch(1, new Vector2(111, 222), queueEventOnly: true);
            EndTouch(2, new Vector2(111, 222), queueEventOnly: true);
            InputSystem.Update();
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    public void Performance_ReadEveryKey()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        Measure.Method(() =>
        {
            foreach (var key in keyboard.allKeys)
                key.ReadValue();
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    [TestCase("Gamepad", "leftStick")]
    [TestCase("Gamepad", "buttonSouth")]
    [TestCase("Gamepad", "leftTrigger")]
    public void Performance_ReadControl(string deviceLayout, string controlPath)
    {
        var device = InputSystem.AddDevice(deviceLayout);
        var control = device[controlPath];

        Action method;
        if (control is StickControl stick)
            method = () => stick.ReadValue();
        else if (control is AxisControl axis)
            method = () => axis.ReadValue();
        else
            throw new NotImplementedException();

        Measure.Method(method)
            .MeasurementCount(200)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    [TestCase("Mouse")]
    [TestCase("Touchscreen")]
    [TestCase("Keyboard")]
    [TestCase("Gamepad")]
    public void Performance_CreateDevice(string layoutName)
    {
        // Nuke builtin precompiled layouts.
        InputControlLayout.s_Layouts.precompiledLayouts.Clear();

        Measure.Method(() => InputDevice.Build<InputDevice>(layoutName))
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    // Layouts tested here must have precompiled versions for this test to be meaningful.
    [TestCase("Mouse")]
    [TestCase("Touchscreen")]
    [TestCase("Keyboard")]
    public void Performance_CreatePrecompiledDevice(string layoutName)
    {
        Measure.Method(() => InputDevice.Build<InputDevice>(layoutName))
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    public void Performance_TriggerAction()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        action.Enable();

        Measure.Method(() => PressAndRelease(gamepad.buttonSouth))
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    public void Performance_ReadActionValue_InCallback()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        action.Enable();

        action.performed += ctx => ctx.ReadValue<float>();

        Measure.Method(() => PressAndRelease(gamepad.buttonSouth))
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    public void Performance_ReadActionValue()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        action.Enable();

        Press(gamepad.buttonSouth);

        Measure.Method(() => action.ReadValue<float>())
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    public void Performance_ListenForUnpairedDeviceActivity()
    {
        // Use touchscreen as it is the most complicated device we have and thus
        // will likely incur the highest cost.
        InputSystem.AddDevice<Touchscreen>();
        ++InputUser.listenForUnpairedDeviceActivity;
        InputUser.onUnpairedDeviceUsed += (control, eventPtr) => {};

        Measure.Method(() =>
        {
            BeginTouch(1, new Vector2(123, 234));
            EndTouch(1, new Vector2(234, 345));
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    public unsafe void Performance_SearchForChangedButtonInEvent_Manually()
    {
        // Using Touchscreen as it is generally our worst-case device.
        var touchscreen = InputSystem.AddDevice<Touchscreen>();
        using (StateEvent.From(touchscreen, out var eventPtr))
        {
            touchscreen.touches[6].pressure.WriteValueIntoEvent(0.5f, eventPtr);

            Measure.Method(() =>
            {
                // Blindly find the control using brute force. Look for leaf controls only and
                // skip noisy and synthetic controls manually.
                var foundIt = false;
                foreach (var control in touchscreen.allControls)
                {
                    if (control.m_ChildCount > 0)
                        continue;
                    if (control.noisy || control.synthetic)
                        continue;

                    if (control is AxisControl axis)
                    {
                        var statePtr = axis.GetStatePtrFromStateEvent(eventPtr);
                        if (!axis.CheckStateIsAtDefaultIgnoringNoise(statePtr) && axis.ReadValueFromState(statePtr) > 0)
                        {
                            // Make sure we're actually finding the right control.
                            Assert.That(axis, Is.SameAs(touchscreen.touches[6].pressure));
                            foundIt = true;
                            break; // Found.
                        }
                    }
                }
                Assert.That(foundIt, Is.True);
            })
                .MeasurementCount(500)
                .WarmupCount(5)
                .Run();
        }
    }

    [Test, Performance]
    [Category("Performance")]
    public void Performance_SearchForChangedButtonInEvent_UsingEnumerateChangedControls()
    {
        // Using Touchscreen as it is generally our worst-case device. This setup
        // is somewhat artificial for the touchscreen, though, as we'd not normally
        // send a full state event for the entire device but rather only send
        // individual TouchState events.
        var touchscreen = InputSystem.AddDevice<Touchscreen>();
        using (StateEvent.From(touchscreen, out var eventPtr))
        {
            touchscreen.touches[6].pressure.WriteValueIntoEvent(0.5f, eventPtr);

            Measure.Method(() =>
            {
                var foundIt = false;
                foreach (var control in eventPtr.EnumerateChangedControls())
                {
                    if (control is AxisControl axis)
                    {
                        // Make sure we're actually finding the right control.
                        Assert.That(axis, Is.SameAs(touchscreen.touches[6].pressure));
                        foundIt = true;
                        break;     // Found.
                    }
                }
                Assert.That(foundIt, Is.True);
            })
                .MeasurementCount(500)
                .WarmupCount(5)
                .Run();
        }
    }

    // Auto-switching of control schemes in PlayerInput is performance-sensitive in that it has
    // to monitor all incoming input events and figure out whether they are or are not leading
    // to a control scheme change. The performance impact of this must be as low as we can
    // get it.
    [Test, Performance]
    [Category("Performance")]
    public void Performance_AutoSwitchingOfControlSchemesInPlayerInput_UnrelatedDeviceIsFeedingInput()
    {
        // In this test, we go through the case where we have one device that just constantly spams
        // the system without any relevant input. InputUser's job is to eliminate that as quickly
        // as possible.

        InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        actions.AddActionMap("map").AddAction("action", binding: "<Gamepad>/buttonSouth", groups: "Gamepad");
        actions.AddControlScheme("Gamepad")
            .WithRequiredDevice<Gamepad>();

        var player = new GameObject();
        var playerInput = player.AddComponent<PlayerInput>();
        playerInput.actions = actions;

        Measure.Method(() =>
        {
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(123, 234)});
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(234, 345)});
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(345, 456)});
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(456, 567)});
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(567, 678)});
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(678, 789)});
            InputSystem.Update();
        })
            .MeasurementCount(500)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    public void Performance_AutoSwitchingOfControlSchemesInPlayerInput_SwitchBackAndForth()
    {
        // In this test, we go through the case where we bounce back and forth between control
        // schemes, i.e. where we have input that leads to an actual switch.

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var action = actions.AddActionMap("map").AddAction("action");
        action.AddBinding("<Gamepad>/buttonSouth", groups: "Gamepad");
        action.AddBinding("<Keyboard>/space", groups: "Keyboard");
        actions.AddControlScheme("Gamepad")
            .WithRequiredDevice<Gamepad>();
        actions.AddControlScheme("Keyboard")
            .WithRequiredDevice<Keyboard>();

        var player = new GameObject();
        var playerInput = player.AddComponent<PlayerInput>();
        playerInput.defaultControlScheme = "Keyboard";
        playerInput.actions = actions;

        Measure.Method(() =>
        {
            PressAndRelease(gamepad.buttonSouth);
            PressAndRelease(keyboard.spaceKey);
            PressAndRelease(gamepad.buttonSouth);
            PressAndRelease(keyboard.spaceKey);
            PressAndRelease(gamepad.buttonSouth);
            PressAndRelease(keyboard.spaceKey);
        })
            .MeasurementCount(500)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    public void Performance_Rebinding_OneSuccessfulCycle()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var action = new InputAction(binding: "<Gamepad>/buttonSouth");

        Measure.Method(() =>
        {
            using (action.PerformInteractiveRebinding().Start())
            {
                Press(gamepad.buttonNorth);
                currentTime += 1;
                InputSystem.Update();
                Assert.That(action.controls[0], Is.SameAs(gamepad.buttonNorth));
            }
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    public enum LookupByName
    {
        CaseMatches,
        CaseDoesNotMatch
    }

    [Test, Performance]
    [Category("Performance")]
    [TestCase(LookupByName.CaseMatches)]
    [TestCase(LookupByName.CaseDoesNotMatch)]
    public void Performance_LookupActionByName(LookupByName lookup)
    {
        const int kActionCount = 100;

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        for (var n = 0; n < kActionCount; ++n)
            map.AddAction("action" + n);

        Measure.Method(() =>
        {
            var _ = asset[(lookup == LookupByName.CaseDoesNotMatch ? "ACTION" : "action") + (int)(kActionCount * 0.75f)];
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    public void Performance_LookupActionByGuid()
    {
        const int kActionCount = 100;

        InputAction actionToFind = null;

        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("map");
        for (var n = 0; n < kActionCount; ++n)
        {
            var action = map.AddAction("action" + n);
            action.GenerateId();

            if (n == (int)(kActionCount * 0.75f))
                actionToFind = action;
        }

        Measure.Method(() =>
        {
            Assert.That(asset[actionToFind.id.ToString()], Is.SameAs(actionToFind));
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    // We're hitting MatchesPrefix a lot from rebinding, so make sure it's performing reasonably well.
    [Test, Performance]
    [Category("Performance")]
    public void Performance_MatchControlPathPrefix()
    {
        var touchscreen = InputSystem.AddDevice<Touchscreen>();

        Measure.Method(() =>
        {
            var result1 = InputControlPath.MatchesPrefix("<Touchscreen>", touchscreen.touches[0].position.x);
            var result2 = InputControlPath.MatchesPrefix("<Touchscreen>/touch0", touchscreen.touches[0].position.x);
            var result3 = InputControlPath.MatchesPrefix("<Touchscreen>/touch0/position", touchscreen.touches[0].position.x);
            var result4 = InputControlPath.MatchesPrefix("<Gamepad>", touchscreen.touches[0].position.x);

            Assert.That(result1, Is.True);
            Assert.That(result2, Is.True);
            Assert.That(result3, Is.True);
            Assert.That(result4, Is.False);
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    [TestCase("Gamepad")]
    [TestCase("Touchscreen")]
    [TestCase("Keyboard", "Mouse")]
    [TestCase("Joystick")]
    [TestCase("Mouse", null, false)]
    public void Performance_FindControlScheme(string device1, string device2 = null, bool success = true)
    {
        var actions = new DefaultInputActions();

        var device1Instance = InputSystem.AddDevice(device1);
        var device2Instance = device2 != null ? InputSystem.AddDevice(device2) : null;

        InputDevice[] devices;
        if (device1Instance != null && device2Instance != null)
            devices = new[] { device1Instance, device2Instance };
        else
            devices = new[] { device1Instance };

        Measure.Method(() =>
        {
            var result = InputControlScheme.FindControlSchemeForDevices(devices, actions.controlSchemes, out _, out var match);
            match.Dispose();
            Assert.That(result, Is.EqualTo(success));
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    #if UNITY_EDITOR
    [Test]
    [Category("Performance")]
    [Ignore("TODO")]
    public void TODO_CanSaveAndRestoreSystemInLessThan10Milliseconds() // Currently it's >200ms!
    {
        Assert.Fail();
    }

    #endif
}
