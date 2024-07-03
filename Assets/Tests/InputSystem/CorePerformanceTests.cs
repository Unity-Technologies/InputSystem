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
using UnityEngine.InputSystem.Utilities;

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

    [Test, Performance]
    [Category("Performance")]
    public void Performance_MakeCircles()
    {
        Measure.Method(() =>
        {
            SpriteUtilities.CreateCircleSprite(16, new Color32(255, 255, 255, 255));
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
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

    internal enum OptimizationTestType
    {
        NoOptimization,
        OptimizedControls,
        ReadValueCaching,
        OptimizedControlsAndReadValueCaching
    }

    public void SetInternalFeatureFlagsFromTestType(OptimizationTestType testType)
    {
        var useOptimizedControls = testType == OptimizationTestType.OptimizedControls
            || testType == OptimizationTestType.OptimizedControlsAndReadValueCaching;
        var useReadValueCaching = testType == OptimizationTestType.ReadValueCaching
            || testType == OptimizationTestType.OptimizedControlsAndReadValueCaching;

        InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kUseOptimizedControls, useOptimizedControls);
        InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kUseReadValueCaching, useReadValueCaching);
        InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kParanoidReadValueCachingChecks, false);
    }

    [Test, Performance]
    [Category("Performance")]
    [TestCase(OptimizationTestType.NoOptimization)]
    [TestCase(OptimizationTestType.OptimizedControls)]
    [TestCase(OptimizationTestType.ReadValueCaching)]
    [TestCase(OptimizationTestType.OptimizedControlsAndReadValueCaching)]
    // Isolated tests for reading from Mouse device to evaluate the performance of the optimizations.
    // Does not take into account the performance of the InputSystem.Update() call.
    public void Performance_OptimizedControls_ReadingMousePosition100kTimes(OptimizationTestType testType)
    {
        SetInternalFeatureFlagsFromTestType(testType);

        var mouse = InputSystem.AddDevice<Mouse>();
        var useOptimizedControls = testType == OptimizationTestType.OptimizedControls
            || testType == OptimizationTestType.OptimizedControlsAndReadValueCaching;
        Assert.That(mouse.position.x.optimizedControlDataType, Is.EqualTo(useOptimizedControls ? InputStateBlock.FormatFloat : InputStateBlock.FormatInvalid));
        Assert.That(mouse.position.y.optimizedControlDataType, Is.EqualTo(useOptimizedControls ? InputStateBlock.FormatFloat : InputStateBlock.FormatInvalid));
        Assert.That(mouse.position.optimizedControlDataType, Is.EqualTo(useOptimizedControls ? InputStateBlock.FormatVector2 : InputStateBlock.FormatInvalid));

        Measure.Method(() =>
        {
            var pos = new Vector2();
            for (var i = 0; i < 100000; ++i)
                pos += mouse.position.ReadValue();
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    [TestCase(OptimizationTestType.NoOptimization)]
    [TestCase(OptimizationTestType.OptimizedControls)]
    [TestCase(OptimizationTestType.ReadValueCaching)]
    [TestCase(OptimizationTestType.OptimizedControlsAndReadValueCaching)]
    // Currently these tests shows that all the optimizations have a performance cost when reading from a Mouse device.
    // OptimizedControls option is slower because of an extra check that is only done in Editor and Development Builds.
    // ReadValueCaching option is slower because Mouse state (FastMouse) is changed every update, which means cached
    // values are always stale. And currently there is a cost when caching the value.
    public void Performance_OptimizedControls_ReadAndUpdateMousePosition1kTimes(OptimizationTestType testType)
    {
        SetInternalFeatureFlagsFromTestType(testType);

        var mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.Update();

        var useOptimizedControls = testType == OptimizationTestType.OptimizedControls
            || testType == OptimizationTestType.OptimizedControlsAndReadValueCaching;
        Assert.That(mouse.position.x.optimizedControlDataType, Is.EqualTo(useOptimizedControls ? InputStateBlock.FormatFloat : InputStateBlock.FormatInvalid));
        Assert.That(mouse.position.y.optimizedControlDataType, Is.EqualTo(useOptimizedControls ? InputStateBlock.FormatFloat : InputStateBlock.FormatInvalid));
        Assert.That(mouse.position.optimizedControlDataType, Is.EqualTo(useOptimizedControls ? InputStateBlock.FormatVector2 : InputStateBlock.FormatInvalid));

        Measure.Method(() =>
        {
            var pos = new Vector2();
            for (var i = 0; i < 1000; ++i)
            {
                pos += mouse.position.ReadValue();
                InputSystem.Update();

                if (i % 100 == 0)
                {
                    // Make sure there's a new different value every 100 frames.
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(i + 1, i + 2) });
                }
            }
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    [TestCase(OptimizationTestType.NoOptimization)]
    [TestCase(OptimizationTestType.ReadValueCaching)]
    // These tests shows a use case where ReadValueCaching optimization will perform better than without any
    // optimization.
    // It shows that there's a performance improvement when the control values being read are not changing every frame.
    public void Performance_OptimizedControls_ReadAndUpdateGamepad1kTimes(OptimizationTestType testType)
    {
        SetInternalFeatureFlagsFromTestType(testType);

        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.Update();

        Measure.Method(() =>
        {
            var pos = new Vector2();
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.3f, 0.1f) });
            InputSystem.Update();

            pos = gamepad.leftStick.value;
            Assert.That(gamepad.leftStick.m_CachedValueIsStale, Is.False);

            for (var i = 0; i < 1000; ++i)
            {
                InputSystem.Update();
                pos = gamepad.leftStick.value;
                Assert.That(gamepad.leftStick.m_CachedValueIsStale, Is.False);

                if (i % 100 == 0)
                {
                    // Make sure there's a new different value every 100 frames to mark the cached value as stale.
                    InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(i / 1000f, i / 1000f) });
                    InputSystem.Update();
                }
            }
        })
            .MeasurementCount(100)
            .WarmupCount(10)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    [TestCase(OptimizationTestType.NoOptimization)]
    [TestCase(OptimizationTestType.ReadValueCaching)]
    // This shows a use case where ReadValueCaching optimization will perform worse when controls have stale cached
    // values every frame. Meaning, when control values change in every frame.
    public void Performance_OptimizedControls_ReadAndUpdateGamepadNewValuesEveryFrame1kTimes(OptimizationTestType testType)
    {
        SetInternalFeatureFlagsFromTestType(testType);

        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.Update();

        Measure.Method(() =>
        {
            var pos = new Vector2();
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.1f, 0.1f) });
            InputSystem.Update();

            gamepad.leftStick.ReadValue();
            Assert.That(gamepad.leftStick.m_CachedValueIsStale, Is.False);

            for (var i = 0; i < 1000; ++i)
            {
                InputSystem.Update();
                pos = gamepad.leftStick.value;
                Assert.That(gamepad.leftStick.m_CachedValueIsStale, Is.False);
                // Make sure there's a new different value every frames to mark the cached value as stale.
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(i / 1000f, i / 1000f) });
            }
        })
            .MeasurementCount(100)
            .WarmupCount(10)
            .Run();
    }

    [Test, Performance]
    [Category("Performance")]
    [TestCase(OptimizationTestType.NoOptimization)]
    [TestCase(OptimizationTestType.OptimizedControls)]
    [TestCase(OptimizationTestType.ReadValueCaching)]
    [TestCase(OptimizationTestType.OptimizedControlsAndReadValueCaching)]
    // These tests evaluate the performance when there's no read value performed and only InputSystem.Update() is called.
    // Emulates a scenario where the controls are not being changed to evaluate the impact of the optimizations.
    public void Performance_OptimizedControls_UpdateOnly1kTimes(OptimizationTestType testType)
    {
        SetInternalFeatureFlagsFromTestType(testType);

        // This adds FastMouse, which updates state every frame and can lead to a performance cost
        // when using ReadValueCaching.
        var mouse = InputSystem.AddDevice<Mouse>();
        InputSystem.Update();

        Measure.Method(() =>
        {
            CallUpdate();
        })
            .MeasurementCount(100)
            .SampleGroup("Mouse Only")
            .WarmupCount(10)
            .Run();

        InputSystem.RemoveDevice(mouse);
        InputSystem.AddDevice<Gamepad>();
        InputSystem.Update();

        Measure.Method(() =>
        {
            CallUpdate();
        })
            .MeasurementCount(100)
            .SampleGroup("Gamepad Only")
            .WarmupCount(10)
            .Run();

        return;

        void CallUpdate()
        {
            for (var i = 0; i < 1000; ++i) InputSystem.Update();
        }
    }

    [Test, Performance]
    [Category("Performance")]
    [TestCase(OptimizationTestType.NoOptimization)]
    [TestCase(OptimizationTestType.ReadValueCaching)]
    // These tests show the performance of the ReadValueCaching optimization when there are state changes per frame on
    // gamepad controls and there are composite actions that read from controls.
    // Currently, there is a positive performance impact by using ReadValueCaching when reading from controls which have
    // composite bindings.
    public void Performance_OptimizedControls_EvaluateStaleControlReadsWhenGamepadStateChanges(OptimizationTestType testType)
    {
        SetInternalFeatureFlagsFromTestType(testType);

        var gamepad = InputSystem.AddDevice<Gamepad>();

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Disable the project wide actions actions to avoid performance impact.
        InputSystem.actions.Disable();
#endif

        Measure.Method(() =>
        {
            MethodToMeasure(gamepad);
        }).SampleGroup("ReadValueCaching Expected With WORSE Performance")
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();

        // Create composite actions to show the performance improvement when using ReadValueCaching.

        var leftStickCompositeAction = new InputAction("LeftStickComposite", InputActionType.Value);
        leftStickCompositeAction.AddCompositeBinding("2DVector")
            .With("Up", "<Gamepad>/leftStick/up")
            .With("Down", "<Gamepad>/leftStick/down")
            .With("Left", "<Gamepad>/leftStick/left")
            .With("Right", "<Gamepad>/leftStick/right");


        var rightStickCompositeAction = new InputAction("RightStickComposite", InputActionType.Value);
        rightStickCompositeAction.AddCompositeBinding("2DVector")
            .With("Up", "<Gamepad>/rightStick/up")
            .With("Down", "<Gamepad>/rightStick/down")
            .With("Left", "<Gamepad>/rightStick/left")
            .With("Right", "<Gamepad>/rightStick/right");

        leftStickCompositeAction.Enable();
        rightStickCompositeAction.Enable();

        Measure.Method(() =>
        {
            MethodToMeasure(gamepad);
        }).SampleGroup("ReadValueCaching Expected With BETTER Performance")
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();


#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Re-enable the project wide actions actions.
        InputSystem.actions.Enable();
#endif
        return;

        void MethodToMeasure(Gamepad g)
        {
            var value2d = Vector2.zero;

            for (var i = 0; i < 1000; ++i)
            {
                // Make sure state changes are different from previous state so that we mark the controls as
                // stale.
                InputSystem.QueueStateEvent(g,
                    new GamepadState
                    {
                        leftStick = new Vector2(i / 1000f, i / 1000f),
                        rightStick = new Vector2(i / 1000f, i / 1200f)
                    });
                InputSystem.Update();

                value2d = gamepad.leftStick.value;
            }
        }
    }

#if ENABLE_VR
    [Test, Performance]
    [Category("Performance")]
    [TestCase(OptimizationTestType.NoOptimization)]
    [TestCase(OptimizationTestType.OptimizedControls)]
    [TestCase(OptimizationTestType.ReadValueCaching)]
    [TestCase(OptimizationTestType.OptimizedControlsAndReadValueCaching)]
    // Isolated tests for reading from XR Pose device to evaluate the performance of the optimizations.
    // Does not take into account the performance of the InputSystem.Update() call.
    public void Performance_OptimizedControls_ReadingPose4kTimes(OptimizationTestType testType)
    {
        SetInternalFeatureFlagsFromTestType(testType);

        runtime.ReportNewInputDevice(XRTests.PoseDeviceState.CreateDeviceDescription().ToJson());

        InputSystem.Update();

        var device = InputSystem.devices[0];

        var poseControl = device["posecontrol"] as UnityEngine.InputSystem.XR.PoseControl;
        var useOptimizedControls = testType == OptimizationTestType.OptimizedControls
            || testType == OptimizationTestType.OptimizedControlsAndReadValueCaching;
        Assert.That(poseControl.optimizedControlDataType, Is.EqualTo(useOptimizedControls ? InputStateBlock.FormatPose : InputStateBlock.FormatInvalid));

        Measure.Method(() =>
        {
            for (var i = 0; i < 4000; ++i)
                poseControl.ReadValue();
        })
            .MeasurementCount(100)
            .WarmupCount(5)
            .Run();
    }

#endif
}
