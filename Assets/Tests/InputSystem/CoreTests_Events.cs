using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.TestTools.Utils;
using Is = UnityEngine.TestTools.Constraints.Is;
using Random = UnityEngine.Random;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

#pragma warning disable CS0649
partial class CoreTests
{
    [Test]
    [Category("Events")]
    public void Events_CanListenForEvents()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var callMouseEventCount = 0;
        var callOnceOnButtonPressCount = 0;
        var firstButtonPress = new List<InputControl>();
        var allButtonPresses = new List<InputControl>();

        InputSystem.onEvent
            .Select(e => e.GetFirstButtonPressOrNull())
            .Call(c =>
            {
                if (c != null) firstButtonPress.Add(c);
            });

        InputSystem.onEvent
            .SelectMany(e => e.GetAllButtonPresses())
            .Call(c => allButtonPresses.Add(c));

        InputSystem.onEvent
            .Where(e => e.HasButtonPress())
            .CallOnce(e =>
            {
                Assert.That(e, Is.Not.EqualTo(default(InputEventPtr)));
                ++callOnceOnButtonPressCount;
            });

        InputSystem.onEvent
            .ForDevice(mouse)
            .Call(e => ++ callMouseEventCount);

        Assert.That(callOnceOnButtonPressCount, Is.Zero);
        Assert.That(callMouseEventCount, Is.Zero);
        Assert.That(firstButtonPress, Is.Empty);
        Assert.That(allButtonPresses, Is.Empty);

        Press(gamepad.buttonSouth);

        Assert.That(callOnceOnButtonPressCount, Is.EqualTo(1));
        Assert.That(callMouseEventCount, Is.Zero);
        Assert.That(firstButtonPress, Is.EquivalentTo(new[] { gamepad.buttonSouth }));
        Assert.That(allButtonPresses, Is.EquivalentTo(new[] { gamepad.buttonSouth }));

        firstButtonPress.Clear();
        allButtonPresses.Clear();

        Release(gamepad.buttonSouth);

        Assert.That(callOnceOnButtonPressCount, Is.EqualTo(1));
        Assert.That(callMouseEventCount, Is.Zero);
        Assert.That(firstButtonPress, Is.Empty);
        Assert.That(allButtonPresses, Is.Empty);

        Press(gamepad.buttonSouth);

        Assert.That(callOnceOnButtonPressCount, Is.EqualTo(1));
        Assert.That(callMouseEventCount, Is.Zero);
        Assert.That(firstButtonPress, Is.EquivalentTo(new[] { gamepad.buttonSouth }));
        Assert.That(allButtonPresses, Is.EquivalentTo(new[] { gamepad.buttonSouth }));

        Press(keyboard.spaceKey, queueEventOnly: true);
        Press(mouse.leftButton);

        Assert.That(callOnceOnButtonPressCount, Is.EqualTo(1));
        Assert.That(callMouseEventCount, Is.EqualTo(1));
        Assert.That(firstButtonPress, Is.EquivalentTo(new[] { gamepad.buttonSouth, keyboard.spaceKey, mouse.leftButton }));
        Assert.That(allButtonPresses, Is.EquivalentTo(new[] { gamepad.buttonSouth, keyboard.spaceKey, mouse.leftButton }));

        firstButtonPress.Clear();
        allButtonPresses.Clear();

        Release(gamepad.buttonSouth);
        InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.A, GamepadButton.B));
        InputSystem.Update();

        Assert.That(callOnceOnButtonPressCount, Is.EqualTo(1));
        Assert.That(callMouseEventCount, Is.EqualTo(1));
        Assert.That(firstButtonPress, Is.EquivalentTo(new[] { gamepad.bButton })); // Comes first in layout.
        Assert.That(allButtonPresses, Is.EquivalentTo(new[] { gamepad.aButton, gamepad.bButton }));
    }

    [Test]
    [Category("Events")]
    public void Events_CanListenForButtonPresses()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.5f;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var callCount = 0;

        InputSystem.onAnyButtonPress
            .CallOnce(ctrl =>
            {
                Assert.That(ctrl, Is.SameAs(gamepad.buttonSouth));
                ++callCount;
            });

        Assert.That(callCount, Is.Zero);

        InputSystem.Update();

        Assert.That(callCount, Is.Zero);

        InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.A));
        InputSystem.Update();

        Assert.That(callCount, Is.EqualTo(1));

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();

        Assert.That(callCount, Is.EqualTo(1));

        InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.A));
        InputSystem.Update();

        Assert.That(callCount, Is.EqualTo(1));

        // Try trigger.
        callCount = 0;
        InputSystem.onAnyButtonPress
            .CallOnce(ctrl =>
            {
                Assert.That(ctrl, Is.SameAs(gamepad.leftTrigger));
                ++callCount;
            });

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.4f });
        InputSystem.Update();

        Assert.That(callCount, Is.Zero);

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.6f });
        InputSystem.Update();

        Assert.That(callCount, Is.EqualTo(1));

        // Try stick left "button".
        Observable.CallOnce<InputControl>(InputSystem.onAnyButtonPress, _ =>
        {
            Assert.Fail("Must not be called for leftStick movement");
        });

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(-1, 0) });
        InputSystem.Update();
    }

    [Test]
    [Category("Events")]
    public void Events_CanGetAllButtonPressesInEvent()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var controls = new List<InputControl>();
        InputSystem.onEvent += (eventPtr, device) =>
        {
            controls = eventPtr.GetAllButtonPresses().ToList();
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.A, GamepadButton.B));
        InputSystem.Update();

        Assert.That(controls, Is.EquivalentTo(new[] { gamepad.aButton, gamepad.bButton }));
    }

    // This is one of the most central tests. If this one breaks, it most often
    // hints at the state layouting or state updating machinery being borked.
    [Test]
    [Category("Events")]
    public void Events_CanUpdateStateOfDeviceWithEvent()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var newState = new GamepadState {leftTrigger = 0.234f};

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.234f).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanUpdateStateOfDeviceWithEvent_SentFromUpdateCallback()
    {
        var device = InputSystem.AddDevice<CustomDeviceWithUpdate>();

        InputSystem.Update();

        Assert.That(device.onUpdateCallCount, Is.EqualTo(1));
        Assert.That(device.axis.ReadValue(), Is.EqualTo(0.234).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanChangeStateOfDeviceDirectlyUsingEvent()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        using (StateEvent.From(mouse, out var eventPtr))
        {
            var stateChangeMonitorTriggered = false;
            InputState.AddChangeMonitor(mouse.delta,
                (c, t, e, i) => stateChangeMonitorTriggered = true);

            mouse.delta.WriteValueIntoEvent(new Vector2(123, 234), eventPtr);

            InputState.Change(mouse, eventPtr);

            Assert.That(stateChangeMonitorTriggered, Is.True);
            Assert.That(mouse.delta.ReadValue(), Is.EqualTo(new Vector2(123, 234)).Using(Vector2EqualityComparer.Instance));
        }
    }

    [Test]
    [Category("Events")]
    [Ignore("TODO")]
    public void TODO_Events_CanUpdateStateOfDeviceWithBatchEvent()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Events")]
    public void Events_CanResetDeviceWithEvent()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        Set(device.leftTrigger, 0.5f);

        var sawReset = false;
        InputSystem.onDeviceChange += (inputDevice, change) =>
        {
            if (change == InputDeviceChange.SoftReset || change == InputDeviceChange.HardReset)
            {
                Assert.That(sawReset, Is.False);
                sawReset = true;
            }
        };

        var resetEvent = DeviceResetEvent.Create(device.deviceId);
        InputSystem.QueueEvent(ref resetEvent);
        InputSystem.Update();

        Assert.That(sawReset, Is.True);
        Assert.That(device.leftTrigger.ReadValue(), Is.Zero);
    }

    [Test]
    [Category("Events")]
    public void Events_CanUpdatePartialStateOfDeviceWithEvent()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Full state update to make sure we won't be overwriting other
        // controls with state. Also, make sure we actually carry over
        // those values on buffer flips.
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState
            {
                buttons = 0xffffffff,
                rightStick = Vector2.one,
                leftTrigger = 0.123f,
                rightTrigger = 0.456f
            });
        InputSystem.Update();

        // Update just left stick.
        InputSystem.QueueDeltaStateEvent(gamepad.leftStick, new Vector2(0.5f, 0.5f));
        InputSystem.Update();

        Assert.That(gamepad.leftStick.ReadValue(),
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.5f, 0.5f))));
        Assert.That(gamepad.rightStick.ReadValue(),
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(1, 1))));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public void Events_QueuingAndProcessingStateEvent_DoesNotAllocateMemory()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Warm up JIT and get rid of GC noise from initial input system update.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.one });
        InputSystem.Update();

        // Make sure we don't get an allocation from the string literal.
        var kProfilerRegion = "Events_ProcessingStateEvent_DoesNotAllocateMemory";

        Assert.That(() =>
        {
            Profiler.BeginSample(kProfilerRegion);
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = Vector2.one });
            InputSystem.Update();
            Profiler.EndSample();
        }, Is.Not.AllocatingGCMemory());
    }

    [Test]
    [Category("Events")]
    public void Events_TakeDeviceOffsetsIntoAccount()
    {
        InputSystem.AddDevice<Gamepad>();
        var secondGamepad = InputSystem.AddDevice<Gamepad>();

        // Full state updates to make sure we won't be overwriting other
        // controls with state. Also, make sure we actually carry over
        // those values on buffer flips.
        InputSystem.QueueStateEvent(secondGamepad,
            new GamepadState
            {
                buttons = 0xffffffff,
                rightStick = Vector2.one,
                leftTrigger = 0.123f,
                rightTrigger = 0.456f
            });
        InputSystem.Update();

        // Update just left stick.
        InputSystem.QueueDeltaStateEvent(secondGamepad.leftStick, new Vector2(0.5f, 0.5f));
        InputSystem.Update();

        Assert.That(secondGamepad.leftStick.ReadValue(),
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.5f, 0.5f))));
    }

    [Test]
    [Category("Events")]
    public void Events_UseCurrentTimeByDefault()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        runtime.currentTime = 1234;
        runtime.currentTimeOffsetToRealtimeSinceStartup = 1123;

        double? receivedTime = null;
        double? receivedInternalTime = null;
        InputSystem.onEvent +=
            (eventPtr, _) =>
        {
            receivedTime = eventPtr.time;
            receivedInternalTime = eventPtr.internalTime;
        };

        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();

        Assert.That(receivedTime.HasValue, Is.True);
        Assert.That(receivedTime.Value, Is.EqualTo(111).Within(0.00001));
        Assert.That(receivedInternalTime.Value, Is.EqualTo(1234).Within(0.00001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanSwitchToFullyManualUpdates()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var receivedOnChange = true;
        InputSystem.onSettingsChange += () => receivedOnChange = true;

        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;

        Assert.That(InputSystem.settings.updateMode, Is.EqualTo(InputSettings.UpdateMode.ProcessEventsManually));
        Assert.That(receivedOnChange, Is.True);

        #if UNITY_EDITOR
        // Edit mode updates shouldn't have been disabled in editor.
        Assert.That(InputSystem.s_Manager.updateMask & InputUpdateType.Editor, Is.Not.Zero);
        #endif

        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.Update(InputUpdateType.Manual);

        Assert.That(mouse.leftButton.isPressed, Is.True);

        Assert.That(() => InputSystem.Update(InputUpdateType.Fixed), Throws.InvalidOperationException);
        Assert.That(() => InputSystem.Update(InputUpdateType.Dynamic), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Events")]
    public void Events_CanSwitchToProcessingInFixedUpdates()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        var receivedOnChange = true;
        InputSystem.onSettingsChange += () => receivedOnChange = true;

        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInFixedUpdate;

        Assert.That(InputSystem.settings.updateMode, Is.EqualTo(InputSettings.UpdateMode.ProcessEventsInFixedUpdate));
        Assert.That(receivedOnChange, Is.True);
        Assert.That(InputSystem.s_Manager.updateMask & InputUpdateType.Fixed, Is.EqualTo(InputUpdateType.Fixed));
        Assert.That(InputSystem.s_Manager.updateMask & InputUpdateType.Dynamic, Is.EqualTo(InputUpdateType.None));

        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        runtime.currentTimeForFixedUpdate += Time.fixedDeltaTime;
        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(mouse.leftButton.isPressed, Is.True);

        Assert.That(() => InputSystem.Update(InputUpdateType.Dynamic), Throws.InvalidOperationException);
        Assert.That(() => InputSystem.Update(InputUpdateType.Manual), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Events")]
    public void Events_ShouldRunUpdate_AppliesUpdateMask()
    {
        InputSystem.s_Manager.updateMask = InputUpdateType.Dynamic;

        Assert.That(runtime.onShouldRunUpdate.Invoke(InputUpdateType.Dynamic));
        Assert.That(!runtime.onShouldRunUpdate.Invoke(InputUpdateType.Fixed));
        Assert.That(!runtime.onShouldRunUpdate.Invoke(InputUpdateType.Manual));

        InputSystem.s_Manager.updateMask = InputUpdateType.Manual;

        Assert.That(!runtime.onShouldRunUpdate.Invoke(InputUpdateType.Dynamic));
        Assert.That(!runtime.onShouldRunUpdate.Invoke(InputUpdateType.Fixed));
        Assert.That(runtime.onShouldRunUpdate.Invoke(InputUpdateType.Manual));

        InputSystem.s_Manager.updateMask = InputUpdateType.Default;

        Assert.That(runtime.onShouldRunUpdate.Invoke(InputUpdateType.Dynamic));
        Assert.That(runtime.onShouldRunUpdate.Invoke(InputUpdateType.Fixed));
        Assert.That(!runtime.onShouldRunUpdate.Invoke(InputUpdateType.Manual));
    }

    [Test]
    [Category("Events")]
    public unsafe void Events_InFixedUpdate_AreTimesliced()
    {
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInFixedUpdate;

        runtime.currentTimeForFixedUpdate = 1;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedEvents = new List<InputEvent>();
        InputSystem.onEvent +=
            (eventPtr, _) => receivedEvents.Add(*eventPtr.data);

        // First fixed update should just take everything.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.1234f}, 1);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.2345f}, 2);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.3456f}, 2.9);

        runtime.currentTimeForFixedUpdate = 3;

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.EqualTo(3));
        Assert.That(receivedEvents[0].time, Is.EqualTo(1).Within(0.00001));
        Assert.That(receivedEvents[1].time, Is.EqualTo(2).Within(0.00001));
        Assert.That(receivedEvents[2].time, Is.EqualTo(2.9).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.3456).Within(0.00001));

        Assert.That(runtime.eventCount, Is.Zero);

        receivedEvents.Clear();

        runtime.currentTimeForFixedUpdate += 1 / 60.0f;

        // From now on, fixed updates should only take what falls in their slice.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.1234f}, 3 + 0.001);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.2345f}, 3 + 0.002);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.3456f}, 3 + 1.0 / 60 + 0.001);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.4567f}, 3 + 2 * (1.0 / 60) + 0.001);

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.EqualTo(2));
        Assert.That(receivedEvents[0].time, Is.EqualTo(3 + 0.001).Within(0.00001));
        Assert.That(receivedEvents[1].time, Is.EqualTo(3 + 0.002).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.2345).Within(0.00001));

        Assert.That(runtime.eventCount, Is.EqualTo(2));

        receivedEvents.Clear();

        runtime.currentTimeForFixedUpdate += 1 / 60.0f;

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.EqualTo(1));
        Assert.That(receivedEvents[0].time, Is.EqualTo(3 + 1.0 / 60 + 0.001).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.3456).Within(0.00001));

        Assert.That(runtime.eventCount, Is.EqualTo(1));

        receivedEvents.Clear();

        runtime.currentTimeForFixedUpdate += 1 / 60.0f;

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.EqualTo(1));
        Assert.That(receivedEvents[0].time, Is.EqualTo(3 + 2 * (1.0 / 60) + 0.001).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.4567).Within(0.00001));

        Assert.That(runtime.eventCount, Is.Zero);

        receivedEvents.Clear();

        runtime.currentTimeForFixedUpdate += 1 / 60.0f;

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.Zero);
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.4567).Within(0.00001));

        Assert.That(runtime.eventCount, Is.Zero);
    }

    [Test]
    [Category("Events")]
    public void Events_CanGetAverageEventLag()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        runtime.advanceTimeEachDynamicUpdate = 0;
        runtime.currentTime = 10;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A), 6);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.123f, 0.234f)}, 1);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A), 10);
        InputSystem.Update();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.234f, 0.345f)}, 3);
        InputSystem.Update();

        var metrics = InputSystem.metrics;

        Assert.That(metrics.averageLagTimePerEvent, Is.EqualTo((9 + 7 + 4 + 0) / 4.0).Within(0.0001));
    }

    [Test]
    [Category("Events")]
    public unsafe void Events_CanCreateStateEventFromDevice()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        InputSystem.QueueStateEvent(mouse, new MouseState {delta = Vector2.one});
        InputSystem.Update();

        using (var buffer = StateEvent.From(mouse, out var eventPtr))
        {
            Assert.That(mouse.delta.x.ReadValueFromEvent(eventPtr, out var xVal), Is.True);
            Assert.That(xVal, Is.EqualTo(1).Within(0.00001));

            Assert.That(mouse.delta.y.ReadValueFromEvent(eventPtr, out var yVal), Is.True);
            Assert.That(yVal, Is.EqualTo(1).Within(0.00001));

            var stateEventPtr = StateEvent.From(eventPtr);

            Assert.That(stateEventPtr->baseEvent.deviceId, Is.EqualTo(mouse.deviceId));
            Assert.That(stateEventPtr->baseEvent.time, Is.EqualTo(runtime.currentTime));
            Assert.That(stateEventPtr->baseEvent.sizeInBytes.AlignToMultipleOf(4), Is.EqualTo(buffer.Length));
            Assert.That(stateEventPtr->baseEvent.sizeInBytes,
                Is.EqualTo(InputEvent.kBaseEventSize + sizeof(FourCC) + mouse.stateBlock.alignedSizeInBytes));
            Assert.That(stateEventPtr->stateSizeInBytes, Is.EqualTo(mouse.stateBlock.alignedSizeInBytes));
            Assert.That(stateEventPtr->stateFormat, Is.EqualTo(mouse.stateBlock.format));
        }
    }

    [Test]
    [Category("Events")]
    public unsafe void Events_CanCreateDeltaStateEventFromControl()
    {
        InputSystem.AddDevice<Mouse>(); // Noise.
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Set(gamepad.buttonSouth, 1);
        Set(gamepad.buttonNorth, 1);
        Set(gamepad.leftTrigger, 0.123f);

        using (DeltaStateEvent.From(gamepad.buttonNorth, out var eventPtr))
        {
            Assert.That(gamepad.buttonNorth.ReadValueFromEvent(eventPtr, out var val), Is.True);
            Assert.That(val, Is.EqualTo(1).Within(0.00001));

            gamepad.buttonNorth.WriteValueIntoEvent(0f, eventPtr);

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(gamepad.buttonNorth.ReadValue(), Is.Zero);
        }

        // More in-depth check on whether we get the offsetting right.
        using (DeltaStateEvent.From(gamepad.leftStick, out var eventPtr))
        {
            var deltaEventPtr = (DeltaStateEvent*)eventPtr.data;
            var statePtr = (byte*)gamepad.leftStick.GetStatePtrFromStateEvent(eventPtr);

            Assert.That((ulong)(statePtr + gamepad.leftStick.stateBlock.byteOffset), Is.EqualTo((ulong)deltaEventPtr->deltaState));

            InputSystem.settings.defaultDeadzoneMin = 0;
            InputSystem.settings.defaultDeadzoneMax = 1;

            gamepad.leftStick.WriteValueIntoEvent(new Vector2(0.123f, 0.234f), eventPtr);

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(gamepad.leftStick.ReadValue(),
                Is.EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance));
        }
    }

    [Test]
    [Category("Events")]
    [TestCase(false, "Gamepad")] // No changed controls.
    [TestCase(false, "Gamepad", "buttonSouth", 0f)] // Default value.
    [TestCase(true, "Gamepad", "buttonSouth", 0f)] // Default value.
    [TestCase(false, "Gamepad", "buttonSouth", 1f)]
    [TestCase(true, "Gamepad", "buttonSouth", 1f)]
    [TestCase(false, "Gamepad", "buttonSouth", 1f, "buttonNorth", 1f)]
    [TestCase(true, "Gamepad", "leftTrigger", 1f, "rightTrigger", 1f)] // Using buttons here will lead to problems because delta events do not work at the level of individual bits.
    public void Events_CanIterateOverChangedControlsInEvent(bool deltaEvent, string layoutName, params object[] controlsAndValues)
    {
        Assert.That(controlsAndValues.Length % 2, Is.Zero, "Must have even pairing of controls and values");

        var device = InputSystem.AddDevice(layoutName);
        var controls = controlsAndValues.EveryNth(2, start: 0).Select(path => device[(string)path]).ToArray();
        var values = controlsAndValues.EveryNth(2, start: 1).ToArray();
        var controlsNotAtDefaultValue = controls
            .Zip(values, (control, value) => !control.ReadDefaultValueAsObject().Equals(value) ? control : null).Where(v => v != null).ToArray();

        var eventHandlerWasCalled = false;
        var numChangedControls = 0;
        InputSystem.onEvent +=
            (eventPtrReceived, deviceForEvent) =>
        {
            eventHandlerWasCalled = true;

            Assert.That(deviceForEvent, Is.SameAs(device));
            Assert.That(eventPtrReceived.IsA<StateEvent>() || deltaEvent);
            Assert.That(eventPtrReceived.IsA<DeltaStateEvent>() || !deltaEvent);

            var result = eventPtrReceived.EnumerateChangedControls().ToList();
            Assert.That(result, Has.Count.EqualTo(deltaEvent ? Math.Min(controlsNotAtDefaultValue.Length, 1) : controlsNotAtDefaultValue.Length));
            numChangedControls += result.Count;
        };

        NativeArray<byte> tempMemory = default;
        InputEventPtr stateEventPtr = default;

        if (!deltaEvent)
            tempMemory = StateEvent.From(device, out stateEventPtr);

        for (var i = 0; i < controls.Length; ++i)
        {
            var control = controls[i];
            var value = values[i];

            if (deltaEvent)
            {
                using (DeltaStateEvent.From(control, out var eventPtr))
                {
                    control.WriteValueFromObjectIntoEvent(eventPtr, value);
                    InputSystem.QueueEvent(eventPtr);
                }
            }
            else
            {
                control.WriteValueFromObjectIntoEvent(stateEventPtr, value);
            }
        }

        if (!deltaEvent)
            InputSystem.QueueEvent(stateEventPtr);
        if (tempMemory.IsCreated)
            tempMemory.Dispose();

        InputSystem.Update();

        Assert.That(eventHandlerWasCalled, Is.True);
        Assert.That(numChangedControls, Is.EqualTo(controlsNotAtDefaultValue.Length));
    }

    // Touchscreen, as usual, is a troublemaker. We send it TouchState events and not TouchscreenState events
    // and it has its own (involved) logic for incorporating them into state. From a control enumeration
    // perspective, though, this is still just a matter of narrowing down the set of controls to consider
    // and taking their relative offsets into account.
    [Test]
    [Category("Events")]
    public unsafe void Events_CanIterateOverChangedControlsInEvent_WithDeviceThatIsStateCallbackReceiver()
    {
        var touchscreen = InputSystem.AddDevice<Touchscreen>();

        var eventHandlerWasCalled = false;
        InputSystem.onEvent +=
            (eventPtrReceived, deviceForEvent) =>
        {
            eventHandlerWasCalled = true;

            Assert.That(deviceForEvent, Is.SameAs(touchscreen));
            Assert.That(eventPtrReceived.IsA<StateEvent>());
            Assert.That(StateEvent.From(eventPtrReceived)->stateFormat, Is.EqualTo(TouchState.Format));

            var result = eventPtrReceived.EnumerateChangedControls().ToList();
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Has.Exactly(1).SameAs(touchscreen.touches[0].position.x));
            Assert.That(result, Has.Exactly(1).SameAs(touchscreen.touches[0].position.y));
        };

        InputSystem.QueueStateEvent(touchscreen, new TouchState { position = new Vector2(123, 234) });
        InputSystem.Update();

        Assert.That(eventHandlerWasCalled, Is.True);
    }

    // A control may employ useStateFrom to point itself at the state of another control. The
    // control enumerator by default ignores useStateFrom controls and returns only the ones that have
    // their "own" state.
    [Test]
    [Category("Events")]
    public void Events_CanIterateOverChangedControlsInEvent_IgnoresControlsUsingStateFromOtherControls()
    {
        const string json = @"
            {
                ""name"" : ""TestDevice"",
                ""controls"" : [
                    { ""name"" : ""Button2"", ""layout"" : ""Button"", ""useStateFrom"" : ""Button1"" },
                    { ""name"" : ""Button1"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputSystem.AddDevice("TestDevice");

        var eventHandlerWasCalled = false;
        InputSystem.onEvent +=
            (eventPtrReceived, deviceForEvent) =>
        {
            eventHandlerWasCalled = true;

            Assert.That(deviceForEvent, Is.SameAs(device));
            Assert.That(eventPtrReceived.IsA<StateEvent>());

            var result = eventPtrReceived.EnumerateChangedControls().ToList();
            Assert.That(result, Is.EquivalentTo(new[] { device["Button1"] }));
        };

        using (StateEvent.From(device, out var eventPtr))
        {
            device["Button1"].WriteValueFromObjectIntoEvent(eventPtr, 1f);
            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();
        }

        InputSystem.Update();

        Assert.That(eventHandlerWasCalled, Is.True);
    }

    [Test]
    [Category("Events")]
    public void Events_CanIterateOverChangedControlsInEvent_IncludingParentControls()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        Set(gamepad.leftStick.x, 0.5f);

        using (StateEvent.From(gamepad, out var eventPtr))
        {
            Set(gamepad.leftStick.x, 0f);

            var result =
                eventPtr.EnumerateControls(InputControlExtensions.Enumerate.IgnoreControlsInCurrentState
                    | InputControlExtensions.Enumerate.IncludeNonLeafControls).ToList();

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Has.Exactly(1).SameAs(gamepad.leftStick.x));
            Assert.That(result, Has.Exactly(1).SameAs(gamepad.leftStick));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanIterateOverChangedControlsInEvent_IncludingSyntheticControls()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        Press(mouse.leftButton);

        using (StateEvent.From(mouse, out var eventPtr))
        {
            Release(mouse.leftButton);

            var result =
                eventPtr.EnumerateControls(InputControlExtensions.Enumerate.IgnoreControlsInCurrentState
                    | InputControlExtensions.Enumerate.IncludeSyntheticControls).ToList();

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Has.Exactly(1).SameAs(mouse.leftButton));
            Assert.That(result, Has.Exactly(1).SameAs(mouse.press));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanIterateOverControlsInEvent_AboveSpecifiedThreshold()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Set(gamepad.leftTrigger, 0.25f);
        Set(gamepad.rightTrigger, 0.75f);

        using (StateEvent.From(gamepad, out var eventPtr))
        {
            Set(gamepad.leftTrigger, 0f);
            Set(gamepad.rightTrigger, 0f);

            var result = eventPtr.EnumerateChangedControls(magnitudeThreshold: 0.5f);
            Assert.That(result, Is.EquivalentTo(new[] { gamepad.rightTrigger }));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanIterateOverControlsInEvent_WhenDeltaEventContainsOnlyPartialStateForControl()
    {
        // The keyboard's AnyKey control spans all keys on the keyboard. Thus, if we send
        // a DeltaStateEvent, it will have partial state for AnyKey. This should be treated
        // equivalent to the control *not* being in the event and thus not getting returned
        // by enumeration.
        var keyboard = InputSystem.AddDevice<Keyboard>();

        Assert.That(keyboard.spaceKey.stateBlock.byteOffset, Is.Zero);
        Assert.That(keyboard.spaceKey.stateBlock.bitOffset, Is.LessThan(8));
        var allKeysInFirstByte = keyboard.allControls.Where(x => x.stateBlock.byteOffset == 0 && x.stateBlock.bitOffset < 8 && !x.synthetic).ToList();

        using (DeltaStateEvent.From(keyboard.spaceKey, out var eventPtr))
        {
            var result = eventPtr.EnumerateControls(InputControlExtensions.Enumerate.IncludeSyntheticControls).ToList();

            Assert.That(result, Has.Count.EqualTo(allKeysInFirstByte.Count));
            Assert.That(result, Has.Exactly(0).SameAs(keyboard.anyKey));
            Assert.That(result, Has.Exactly(1).SameAs(keyboard.spaceKey));
        }
    }

    [Test]
    [Category("Events")]
    [Retry(2)] // Warm up JIT.
    public void Events_CanIterateOverControlsInEvent_WithoutAllocatingGC()
    {
        var touchscreen = InputSystem.AddDevice<Touchscreen>();

        using (StateEvent.From(touchscreen, out var eventPtr))
        {
            // Alter current state after we grabbed the default state above.
            BeginTouch(1, new Vector2(123, 234));

            var list = new List<InputControl>(512);
            Assert.That(() =>
            {
                foreach (var control in eventPtr.EnumerateChangedControls())
                    list.Add(control);
            }, Is.Not.AllocatingGCMemory());
            Assert.That(list, Is.Not.Empty);
        }
    }

    private struct DpadState : IInputStateTypeInfo
    {
        [InputControl(name = "dpad", format = "BIT", layout = "Dpad", sizeInBits = 4, defaultState = 8)]
        [InputControl(name = "dpad/up", format = "BIT", layout = "DiscreteButton", parameters = "minValue=7,maxValue=1,nullValue=8,wrapAtValue=7", bit = 0, sizeInBits = 4)]
        [InputControl(name = "dpad/right", format = "BIT", layout = "DiscreteButton", parameters = "minValue=1,maxValue=3", bit = 0, sizeInBits = 4)]
        [InputControl(name = "dpad/down", format = "BIT", layout = "DiscreteButton", parameters = "minValue=3,maxValue=5", bit = 0, sizeInBits = 4)]
        [InputControl(name = "dpad/left", format = "BIT", layout = "DiscreteButton", parameters = "minValue=5, maxValue=7", bit = 0, sizeInBits = 4)]
        public int dpad;

        [InputControl(layout = "Axis")]
        public float axis1;

        [InputControl(layout = "Axis")]
        public float axis2;

        public FourCC format => new FourCC("TEST");
    }

    [InputControlLayout(stateType = typeof(DpadState))]
    private class DpadDevice : InputDevice
    {
    }

    // https://fogbugz.unity3d.com/f/cases/1315107/
    [Test]
    [Category("Events")]
    public unsafe void Events_CanIterateOverChangedControlsInEvent_EvenWhenMultipleControlsShareTheSameState()
    {
        // Create a device that has a dpad setup  where all buttons share the same four bits.
        // To handle this correctly, the iteration code must not advance past a control until
        // after having tried all possible controls using the state.
        // (this setup is actually what's found on the PS4 controller)
        var device = InputSystem.AddDevice<DpadDevice>();

        using (StateEvent.From(device, out var eventPtr))
        {
            var stateEventPtr = StateEvent.From(eventPtr);
            var statePtr = (DpadState*)stateEventPtr->state;

            statePtr->dpad = 6; // Press dpad left.
            statePtr->axis2 = 1f;

            // The code is checking only state, not values. The state for *all* dpad buttons is shared and thus
            // has changed for all of them. So all the dpad buttons are in the list.
            Assert.That(eventPtr.EnumerateChangedControls(),
                Is.EquivalentTo(new[] { device["dpad/up"], device["dpad/right"], device["dpad/down"], device["dpad/left"], device["axis2"] }));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanResetControlToDefaultState()
    {
        const string json = @"
            {
                ""name"" : ""TestDevice"",
                ""controls"" : [
                    { ""name"" : ""Button"", ""layout"" : ""Button"" },
                    { ""name"" : ""ButtonWithDefaultState"", ""layout"" : ""Button"", ""defaultState"" : ""1"" },
                    { ""name"" : ""Axis"", ""layout"" : ""Axis"" },
                    { ""name"" : ""AxisWithDefaultState"", ""layout"" : ""Axis"", ""defaultState"" : ""0.5"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputSystem.AddDevice("TestDevice");

        Press((ButtonControl)device["Button"]);
        Release((ButtonControl)device["ButtonWithDefaultState"]);
        Set((AxisControl)device["Axis"], 0.123f);
        Set((AxisControl)device["AxisWithDefaultState"], 0.234f);

        using (StateEvent.From(device, out var eventPtr))
        {
            device["Button"].ResetToDefaultStateInEvent(eventPtr);
            device["ButtonWithDefaultState"].ResetToDefaultStateInEvent(eventPtr);
            device["Axis"].ResetToDefaultStateInEvent(eventPtr);
            device["AxisWithDefaultState"].ResetToDefaultStateInEvent(eventPtr);

            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(device["Button"].CheckStateIsAtDefault());
            Assert.That(device["ButtonWithDefaultState"].CheckStateIsAtDefault());
            Assert.That(device["Axis"].CheckStateIsAtDefault());
            Assert.That(device["AxisWithDefaultState"].CheckStateIsAtDefault());
        }
    }

    [Test]
    [Category("Events")]
    public void Events_SendingStateToDeviceWithoutBeforeRenderEnabled_DoesNothingInBeforeRenderUpdate()
    {
        // We need one device that has before-render updates enabled for the update to enable
        // at all.
        const string deviceJson = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";

        InputSystem.RegisterLayout(deviceJson);
        InputSystem.AddDevice("CustomGamepad");

        var gamepad = InputSystem.AddDevice<Gamepad>();
        var newState = new GamepadState {leftStick = new Vector2(0.123f, 0.456f)};

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update(InputUpdateType.BeforeRender);

        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo(default(Vector2)));
    }

    [Test]
    [Category("Events")]
    public void Events_SendingStateToDeviceWithBeforeRenderEnabled_UpdatesDeviceInBeforeRender()
    {
        const string deviceJson = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";

        InputSystem.RegisterLayout(deviceJson);

        var gamepad = (Gamepad)InputSystem.AddDevice("CustomGamepad");
        var newState = new GamepadState {leftTrigger = 0.123f};

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update(InputUpdateType.BeforeRender);

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123f).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanListenToEventStream()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        var receivedCalls = 0;
        InputSystem.onEvent += (inputEvent, _) =>
        {
            ++receivedCalls;
            Assert.That(inputEvent.IsA<StateEvent>(), Is.True);
            Assert.That(inputEvent.deviceId, Is.EqualTo(device.deviceId));
        };

        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
    }

    // Should be possible to have a pointer to a state event and from it, return
    // the list of controls that have non-default values.
    // Probably makes sense to also be able to return from it a list of changed
    // controls by comparing it to a device's current state.
    [Test]
    [Category("Events")]
    [Ignore("TODO")]
    public void TODO_Events_CanFindActiveControlsFromStateEvent()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Events")]
    public void Events_AreProcessedInOrderTheyAreQueuedIn()
    {
        const double kFirstTime = 0.5;
        const double kSecondTime = 1.5;
        const double kThirdTime = 2.5;

        var receivedCalls = 0;
        var receivedFirstTime = 0.0;
        var receivedSecondTime = 0.0;
        var receivedThirdTime = 0.0;

        InputSystem.onEvent +=
            (inputEvent, _) =>
        {
            ++receivedCalls;
            if (receivedCalls == 1)
                receivedFirstTime = inputEvent.time;
            else if (receivedCalls == 2)
                receivedSecondTime = inputEvent.time;
            else
                receivedThirdTime = inputEvent.time;
        };

        var device = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(device, new GamepadState(), kSecondTime);
        InputSystem.QueueStateEvent(device, new GamepadState(), kFirstTime);
        InputSystem.QueueStateEvent(device, new GamepadState(), kThirdTime);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(3));
        Assert.That(receivedFirstTime, Is.EqualTo(kSecondTime).Within(0.00001));
        Assert.That(receivedSecondTime, Is.EqualTo(kFirstTime).Within(0.00001));
        Assert.That(receivedThirdTime, Is.EqualTo(kThirdTime).Within(0.00001));
    }

    [Test]
    [Category("Events")]
    public void Events_WillNotReceiveEventsAgainstNonExistingDevices()
    {
        // Device IDs are looked up only *after* the system shows the event to us.

        var receivedCalls = 0;
        InputSystem.onEvent +=
            (eventPtr, _) =>
        {
            ++receivedCalls;
        };

        var inputEvent = DeviceConfigurationEvent.Create(4, 1.0);
        InputSystem.QueueEvent(ref inputEvent);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(0));
    }

    [Test]
    [Category("Events")]
    public void Events_HandledFlagIsResetWhenEventIsQueued()
    {
        var receivedCalls = 0;
        var wasHandled = true;

        InputSystem.onEvent +=
            (eventPtr, _) =>
        {
            ++receivedCalls;
            wasHandled = eventPtr.handled;
        };

        var device = InputSystem.AddDevice<Gamepad>();

        var inputEvent = DeviceConfigurationEvent.Create(device.deviceId, 1.0);

        // This should go back to false when we inputEvent goes on the queue.
        // The way the behavior is implemented is a side-effect of how we store
        // the handled flag as a bit on the event ID -- which will get set by
        // native on an event when it is queued.
        inputEvent.baseEvent.handled = true;

        InputSystem.QueueEvent(ref inputEvent);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(wasHandled, Is.False);
    }

    [Test]
    [Category("Events")]
    public void Events_CanPreventEventsFromBeingProcessed()
    {
        InputSystem.onEvent +=
            (inputEvent, _) =>
        {
            // If we mark the event handled, the system should skip it and not
            // let it go to the device.
            inputEvent.handled = true;
        };

        var device = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(device, new GamepadState {rightTrigger = 0.45f});
        InputSystem.Update();

        Assert.That(device.rightTrigger.ReadValue(), Is.EqualTo(0.0).Within(0.00001));
    }

    [StructLayout(LayoutKind.Explicit, Size = 2)]
    struct StateWith2Bytes : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis")]
        [FieldOffset(0)] public ushort value;

        public FourCC format => new FourCC('T', 'E', 'S', 'T');
    }

    [InputControlLayout(stateType = typeof(StateWith2Bytes))]
    class DeviceWith2ByteState : InputDevice
    {
    }

    // This test pertains mostly to how the input runtime handles events so it's of limited
    // use in our current test setup with InputTestRuntime. There's an equivalent native test
    // in the Unity runtime to ensure the constraint.
    //
    // Previously we used to actually modify event size to always be 4 byte aligned and thus potentially
    // added padding to events. This is a bad idea. The C# system can't tell between padding added to an
    // event and valid input data that's part of the state. This can cause the padding to actually overwrite
    // state of controls that happen to start at the end of an event. On top, we didn't clear out the
    // memory we added to an event and thus ended up with random garbage being written to unrelated controls.
    //
    // What we do now is to simply align event pointers to 4 byte boundaries as we read and write events.
    [Test]
    [Category("Events")]
    public unsafe void Events_CanHandleStateNotAlignedTo4ByteBoundary()
    {
        Debug.Assert(UnsafeUtility.SizeOf<StateWith2Bytes>() == 2);

        var device = InputSystem.AddDevice<DeviceWith2ByteState>();

        InputSystem.QueueStateEvent(device, new StateWith2Bytes());
        InputSystem.QueueStateEvent(device, new StateWith2Bytes());

        InputSystem.onEvent +=
            (eventPtr, _) =>
        {
            // Event addresses must be 4-byte aligned but sizeInBytes must not have been altered.
            Assert.That((Int64)eventPtr.data % 4, Is.EqualTo(0));
            Assert.That(eventPtr.sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<StateWith2Bytes>()));
        };

        InputSystem.Update();
    }

    [Test]
    [Category("Events")]
    public unsafe void Events_CanTraceEventsOfDevice()
    {
        var device = InputSystem.AddDevice<Gamepad>();
        var noise = InputSystem.AddDevice<Gamepad>();

        using (var trace = new InputEventTrace {deviceId = device.deviceId})
        {
            trace.Enable();
            Assert.That(trace.enabled, Is.True);

            var firstState = new GamepadState {rightTrigger = 0.35f};
            var secondState = new GamepadState {leftTrigger = 0.75f};

            InputSystem.QueueStateEvent(device, firstState, 0.5);
            InputSystem.QueueStateEvent(device, secondState, 1.5);
            InputSystem.QueueStateEvent(noise, new GamepadState()); // This one just to make sure we don't get it.

            InputSystem.Update();

            trace.Disable();

            var events = trace.ToList();

            Assert.That(events, Has.Count.EqualTo(2));

            Assert.That(events[0].type, Is.EqualTo((FourCC)StateEvent.Type));
            Assert.That(events[0].deviceId, Is.EqualTo(device.deviceId));
            Assert.That(events[0].time, Is.EqualTo(0.5).Within(0.000001));
            Assert.That(events[0].sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<GamepadState>()));
            Assert.That(UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref firstState),
                StateEvent.From(events[0])->state, UnsafeUtility.SizeOf<GamepadState>()), Is.Zero);

            Assert.That(events[1].type, Is.EqualTo((FourCC)StateEvent.Type));
            Assert.That(events[1].deviceId, Is.EqualTo(device.deviceId));
            Assert.That(events[1].time, Is.EqualTo(1.5).Within(0.000001));
            Assert.That(events[1].sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<GamepadState>()));
            Assert.That(UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref secondState),
                StateEvent.From(events[1])->state, UnsafeUtility.SizeOf<GamepadState>()), Is.Zero);
        }
    }

    [Test]
    [Category("Events")]
    public unsafe void Events_CanTraceEventsOfDevice_AndFilterEventsThroughCallback()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var trace = new InputEventTrace())
        {
            trace.onFilterEvent +=
                (eventPtr, device) => gamepad.buttonSouth.ReadValueFromEvent(eventPtr, out var value) && value > 0;

            trace.Enable();

            InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.A));
            InputSystem.QueueStateEvent(gamepad, default(GamepadState));
            InputSystem.Update();

            Assert.That(trace.eventCount, Is.EqualTo(1));
            Assert.That(*(GamepadState*)StateEvent.From(trace.ToArray()[0])->state, Is.EqualTo(new GamepadState(GamepadButton.South)));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanTraceEventsOfDevice_AndGrowBufferAsNeeded()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        using (var trace = new InputEventTrace(10, growBuffer: true, growIncrementSizeInBytes: 2048) {deviceId = device.deviceId})
        {
            trace.Enable();

            InputSystem.QueueStateEvent(device, new GamepadState().WithButton(GamepadButton.A));
            InputSystem.QueueStateEvent(device, new GamepadState().WithButton(GamepadButton.B));

            InputSystem.Update();

            Assert.That(trace.eventCount, Is.EqualTo(2));
            Assert.That(trace.allocatedSizeInBytes, Is.EqualTo(2048 + 10));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanTraceEventsOfDevice_AndRecordFrameBoundaries()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var trace = new InputEventTrace(gamepad) { recordFrameMarkers = true })
        {
            trace.Enable();

            InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.A));
            InputSystem.Update();

            Assert.That(trace.eventCount, Is.EqualTo(2));
            Assert.That(trace.ToArray()[0].type, Is.EqualTo(InputEventTrace.FrameMarkerEvent));
            Assert.That(trace.ToArray()[1].type, Is.EqualTo(new FourCC(StateEvent.Type)));

            InputSystem.Update();

            Assert.That(trace.eventCount, Is.EqualTo(3));
            Assert.That(trace.ToArray()[0].type, Is.EqualTo(InputEventTrace.FrameMarkerEvent));
            Assert.That(trace.ToArray()[1].type, Is.EqualTo(new FourCC(StateEvent.Type)));
            Assert.That(trace.ToArray()[2].type, Is.EqualTo(InputEventTrace.FrameMarkerEvent));

            InputSystem.QueueStateEvent(gamepad, default(GamepadState));
            InputSystem.Update();

            Assert.That(trace.eventCount, Is.EqualTo(5));
            Assert.That(trace.ToArray()[0].type, Is.EqualTo(InputEventTrace.FrameMarkerEvent));
            Assert.That(trace.ToArray()[1].type, Is.EqualTo(new FourCC(StateEvent.Type)));
            Assert.That(trace.ToArray()[2].type, Is.EqualTo(InputEventTrace.FrameMarkerEvent));
            Assert.That(trace.ToArray()[3].type, Is.EqualTo(InputEventTrace.FrameMarkerEvent));
            Assert.That(trace.ToArray()[4].type, Is.EqualTo(new FourCC(StateEvent.Type)));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_WhenTraceIsFull_WillStartOverwritingOldEvents()
    {
        var device = InputSystem.AddDevice<Gamepad>();
        using (var trace =
                   new InputEventTrace(StateEvent.GetEventSizeWithPayload<GamepadState>() * 2) {deviceId = device.deviceId})
        {
            trace.Enable();

            var firstState = new GamepadState {rightTrigger = 0.35f};
            var secondState = new GamepadState {leftTrigger = 0.75f};
            var thirdState = new GamepadState {leftTrigger = 0.95f};

            InputSystem.QueueStateEvent(device, firstState, 0.5);
            InputSystem.QueueStateEvent(device, secondState, 1.5);
            InputSystem.QueueStateEvent(device, thirdState, 2.5);

            InputSystem.Update();

            trace.Disable();

            var events = trace.ToList();

            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events, Has.Exactly(1).With.Property("time").EqualTo(1.5).Within(0.000001));
            Assert.That(events, Has.Exactly(1).With.Property("time").EqualTo(2.5).Within(0.000001));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanClearEventTrace()
    {
        using (var trace = new InputEventTrace())
        {
            trace.Enable();

            var device = InputSystem.AddDevice<Gamepad>();
            InputSystem.QueueStateEvent(device, new GamepadState());
            InputSystem.QueueStateEvent(device, new GamepadState());
            InputSystem.Update();

            Assert.That(trace.eventCount, Is.EqualTo(2));
            Assert.That(trace.totalEventSizeInBytes, Is.GreaterThan(0));
            Assert.That(trace.ToList(), Has.Count.EqualTo(2));

            trace.Clear();

            Assert.That(trace.eventCount, Is.EqualTo(0));
            Assert.That(trace.totalEventSizeInBytes, Is.EqualTo(0));
            Assert.That(trace.ToList(), Has.Count.EqualTo(0));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanPersistEventTracesInStream()
    {
        var pen = InputSystem.AddDevice<Pen>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var originalTrace = new InputEventTrace())
        {
            originalTrace.Enable();

            InputSystem.QueueStateEvent(pen, new PenState { position = new Vector2(123, 234) });
            InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.A));
            InputSystem.QueueStateEvent(pen, new PenState { position = new Vector2(234, 345) });

            InputSystem.Update();

            Assert.That(originalTrace.eventCount, Is.EqualTo(3));

            using (var memoryStream = new MemoryStream())
            {
                originalTrace.WriteTo(memoryStream);

                memoryStream.Seek(0, SeekOrigin.Begin);

                using (var loadedTrace = InputEventTrace.LoadFrom(memoryStream))
                {
                    Assert.That(loadedTrace, Is.EquivalentTo(originalTrace));
                    Assert.That(loadedTrace.totalEventSizeInBytes, Is.EqualTo(originalTrace.totalEventSizeInBytes));
                }
            }
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanReplayEventsFromEventTrace()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var trace = new InputEventTrace(gamepad))
        {
            trace.Enable();

            Press(gamepad.buttonSouth);
            Press(gamepad.buttonNorth);

            trace.Disable();

            InputSystem.QueueStateEvent(gamepad, default(GamepadState));
            InputSystem.Update();

            Assert.That(gamepad.buttonSouth.isPressed, Is.False);
            Assert.That(gamepad.buttonNorth.isPressed, Is.False);

            trace.Replay().PlayAllEvents();
            Assert.That(runtime.eventCount, Is.EqualTo(2));

            InputSystem.Update();

            Assert.That(gamepad.buttonSouth.isPressed, Is.True);
            Assert.That(gamepad.buttonNorth.isPressed, Is.True);
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanReplayEventsFromEventTrace_EventByEvent()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var trace = new InputEventTrace(gamepad))
        {
            trace.Enable();

            Press(gamepad.buttonSouth);
            Press(gamepad.buttonNorth);

            trace.Disable();

            InputSystem.QueueStateEvent(gamepad, default(GamepadState));
            InputSystem.Update();

            Assert.That(gamepad.buttonSouth.isPressed, Is.False);
            Assert.That(gamepad.buttonNorth.isPressed, Is.False);

            var replay = trace.Replay();

            replay.PlayOneEvent();
            InputSystem.Update();

            Assert.That(gamepad.buttonSouth.isPressed, Is.True);
            Assert.That(gamepad.buttonNorth.isPressed, Is.False);

            replay.PlayOneEvent();
            InputSystem.Update();

            Assert.That(gamepad.buttonSouth.isPressed, Is.True);
            Assert.That(gamepad.buttonNorth.isPressed, Is.True);

            Assert.That(() => replay.PlayOneEvent(), Throws.InvalidOperationException);
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanReplayEventsFromEventTrace_FrameByFrame()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var trace = new InputEventTrace(gamepad) { recordFrameMarkers = true })
        {
            Assert.That(trace.recordFrameMarkers, Is.True);

            trace.Enable();

            Press(gamepad.buttonSouth);
            InputSystem.Update(); // Record empty frame.
            InputSystem.Update(); // Record empty frame.
            Release(gamepad.buttonSouth);

            trace.Disable();

            var replay = trace.Replay().PlayAllFramesOneByOne();

            Assert.That(replay.finished, Is.False);
            Assert.That(gamepad.buttonSouth.isPressed, Is.False);

            InputSystem.Update();

            Assert.That(replay.finished, Is.False);
            Assert.That(gamepad.buttonSouth.isPressed, Is.True);

            InputSystem.Update();

            Assert.That(replay.finished, Is.False);
            Assert.That(gamepad.buttonSouth.isPressed, Is.True);

            InputSystem.Update();

            Assert.That(replay.finished, Is.False);
            Assert.That(gamepad.buttonSouth.isPressed, Is.True);

            InputSystem.Update();

            Assert.That(replay.finished, Is.True);
            Assert.That(gamepad.buttonSouth.isPressed, Is.False);
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanReplayEventsFromEventTrace_UsingGivenDevice()
    {
        // Capture events from one device and replay them on another.

        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        using (var trace = new InputEventTrace(gamepad1))
        {
            trace.Enable();

            Press(gamepad1.buttonSouth);
            Release(gamepad1.buttonSouth);

            var replay = trace.Replay()
                .WithDeviceMappedFromTo(gamepad1, gamepad2);

            replay.PlayOneEvent();
            InputSystem.Update();

            Assert.That(gamepad2.buttonSouth.isPressed, Is.True);

            replay.PlayOneEvent();
            InputSystem.Update();

            Assert.That(gamepad2.buttonSouth.isPressed, Is.False);
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanReplayEventsFromEventTrace_UsingSyntheticDevice()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var trace = new InputEventTrace(gamepad))
        {
            trace.Enable();

            Press(gamepad.buttonSouth);
            Release(gamepad.buttonSouth);

            trace.Disable();

            InputDevice addedDevice = null;
            InputDevice removedDevice = null;
            using (var trace2 = new InputEventTrace())
            {
                InputSystem.onDeviceChange +=
                    (device, change) =>
                {
                    if (change == InputDeviceChange.Added)
                    {
                        Assert.That(addedDevice, Is.Null);
                        addedDevice = device;
                        trace2.deviceId = device.deviceId;
                        trace2.Enable();
                    }
                    else if (change == InputDeviceChange.Removed)
                    {
                        Assert.That(removedDevice, Is.Null);
                        removedDevice = device;
                    }
                };

                var replay = trace.Replay().WithAllDevicesMappedToNewInstances().PlayAllEvents();
                InputSystem.Update();

                Assert.That(addedDevice, Is.Not.Null);
                Assert.That(addedDevice, Is.TypeOf<Gamepad>());
                Assert.That(addedDevice, Is.Not.SameAs(gamepad));
                Assert.That(trace2.eventCount, Is.EqualTo(trace.eventCount));
                Assert.That(trace2.deviceInfos, Has.Count.EqualTo(1));
                Assert.That(trace2.deviceInfos, Has.All.Property("deviceId").EqualTo(addedDevice.deviceId));
                Assert.That(((Gamepad)addedDevice).buttonSouth.ReadValueFromEvent(trace2.ToArray()[0]), Is.EqualTo(1));
                Assert.That(((Gamepad)addedDevice).buttonSouth.ReadValueFromEvent(trace2.ToArray()[1]), Is.EqualTo(0));
                Assert.That(removedDevice, Is.Null);
                Assert.That(replay.createdDevices, Is.EquivalentTo(new[] { addedDevice }));

                replay.Dispose();

                Assert.That(removedDevice, Is.SameAs(addedDevice));
                Assert.That(replay.createdDevices, Is.Empty);
            }
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanReplayEventsFromEventTrace_AndGetInfoOnRecordedDevices()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();

        using (var trace = new InputEventTrace())
        {
            trace.Enable();

            Press(gamepad.buttonSouth);
            Release(gamepad.buttonSouth);
            Set(mouse.position, Vector2.one);

            trace.Disable();

            Assert.That(trace.deviceInfos, Has.Count.EqualTo(2));
            Assert.That(trace.deviceInfos,
                Has.Exactly(1).With.Property("deviceId").EqualTo(gamepad.deviceId).And.Property("layout").EqualTo(gamepad.layout).And
                    .Property("stateFormat").EqualTo(gamepad.stateBlock.format).And.Property("stateSizeInBytes").EqualTo(gamepad.stateBlock.alignedSizeInBytes));
            Assert.That(trace.deviceInfos,
                Has.Exactly(1).With.Property("deviceId").EqualTo(mouse.deviceId).And.Property("layout").EqualTo(mouse.layout).And
                    .Property("stateFormat").EqualTo(mouse.stateBlock.format).And.Property("stateSizeInBytes").EqualTo(mouse.stateBlock.alignedSizeInBytes));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanReplayEventsFromEventTrace_AndRewindAndPlayAgain()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var trace = new InputEventTrace())
        {
            trace.Enable();

            Press(gamepad.buttonSouth);
            Release(gamepad.buttonSouth);

            trace.Disable();

            var action = new InputAction(binding: "<Gamepad>/buttonSouth");
            action.Enable();

            var replay = trace.Replay().PlayAllEvents();
            InputSystem.Update();

            Assert.That(action.triggered, Is.True);

            InputSystem.Update();
            Assert.That(action.triggered, Is.False);

            replay.Rewind();
            Assert.That(replay.position, Is.Zero);

            replay.PlayAllEvents();
            InputSystem.Update();

            Assert.That(action.triggered, Is.True);
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanReplayEventsFromEventTrace_AndUseOriginalEventTiming()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var trace = new InputEventTrace())
        {
            trace.Enable();

            currentTime = 1;
            Press(gamepad.buttonSouth);

            currentTime = 3;
            Press(gamepad.buttonNorth);
            Release(gamepad.buttonSouth);

            currentTime = 4;
            Release(gamepad.buttonNorth);

            var replay = trace.Replay();

            replay.PlayAllEventsAccordingToTimestamps();

            Assert.That(replay.position, Is.EqualTo(0));
            Assert.That(replay.finished, Is.False);

            // First update becomes starting point of replay, i.e. everything is
            // relative to time=1 now.
            InputSystem.Update();

            Assert.That(replay.finished, Is.False);
            Assert.That(replay.position, Is.EqualTo(1));
            Assert.That(gamepad.buttonSouth.isPressed, Is.True);
            Assert.That(gamepad.buttonNorth.isPressed, Is.False);

            currentTime += 1;

            InputSystem.Update();

            Assert.That(replay.finished, Is.False);
            Assert.That(replay.position, Is.EqualTo(1));
            Assert.That(gamepad.buttonSouth.isPressed, Is.True);
            Assert.That(gamepad.buttonNorth.isPressed, Is.False);

            currentTime += 1.5f;

            InputSystem.Update();

            Assert.That(replay.finished, Is.False);
            Assert.That(replay.position, Is.EqualTo(3));
            Assert.That(gamepad.buttonSouth.isPressed, Is.False);
            Assert.That(gamepad.buttonNorth.isPressed, Is.True);

            currentTime += 1;

            InputSystem.Update();

            Assert.That(replay.finished, Is.True);
            Assert.That(replay.position, Is.EqualTo(4));
            Assert.That(gamepad.buttonSouth.isPressed, Is.False);
            Assert.That(gamepad.buttonNorth.isPressed, Is.False);

            InputSystem.Update();

            Assert.That(replay.finished, Is.True);
            Assert.That(replay.position, Is.EqualTo(4));
            Assert.That(gamepad.buttonSouth.isPressed, Is.False);
            Assert.That(gamepad.buttonNorth.isPressed, Is.False);
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanResizeEventTrace()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Allocate a trace for 2.5 GamepadState events to get wrap-around in the buffer.
        using (var trace = new InputEventTrace((int)(2.5f * StateEvent.GetEventSizeWithPayload<GamepadState>())))
        {
            trace.Enable();

            InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.South));
            InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.North));
            InputSystem.QueueStateEvent(gamepad, new GamepadState(GamepadButton.East));
            InputSystem.Update();

            Assert.That(trace.eventCount, Is.EqualTo(2)); // Make sure we wrapped around.
            Assert.That(gamepad.buttonNorth.ReadValueFromEvent(trace.ToArray()[0]), Is.EqualTo(1).Within(0.00001));
            Assert.That(gamepad.buttonEast.ReadValueFromEvent(trace.ToArray()[1]), Is.EqualTo(1).Within(0.00001));

            trace.Resize(4096);

            // Contents should remain unchanged.
            Assert.That(trace.eventCount, Is.EqualTo(2));
            Assert.That(gamepad.buttonNorth.ReadValueFromEvent(trace.ToArray()[0]), Is.EqualTo(1).Within(0.00001));
            Assert.That(gamepad.buttonEast.ReadValueFromEvent(trace.ToArray()[1]), Is.EqualTo(1).Within(0.00001));

            // Recording new events should append as expected.
            Press(gamepad.buttonWest);

            Assert.That(trace.eventCount, Is.EqualTo(3));
            Assert.That(gamepad.buttonNorth.ReadValueFromEvent(trace.ToArray()[0]), Is.EqualTo(1).Within(0.00001));
            Assert.That(gamepad.buttonEast.ReadValueFromEvent(trace.ToArray()[1]), Is.EqualTo(1).Within(0.00001));
            Assert.That(gamepad.buttonWest.ReadValueFromEvent(trace.ToArray()[2]), Is.EqualTo(1).Within(0.00001));

            // Resize back down so we just barely fit one event.
            trace.Resize(StateEvent.GetEventSizeWithPayload<GamepadState>() + 4);

            Assert.That(trace.eventCount, Is.EqualTo(1));
            Assert.That(gamepad.buttonWest.ReadValueFromEvent(trace.ToArray()[0]), Is.EqualTo(1).Within(0.00001));
        }
    }

    #if UNITY_EDITOR
    [Test]
    [Category("Events")]
    public void Events_EventTraceCanSurviveDomainReload()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var trace = new InputEventTrace(gamepad))
        {
            trace.Enable();

            Press(gamepad.buttonSouth);

            // All that's necessary to survive the reload is for the serialized data to come through fine.
            // We use the JSON serializer to test that. Technically, it's not quite the same as Unity internally
            // serializes such that private, otherwise non-serialized data gets captured, too, but for our
            // purposes here it's close enough.

            var json = JsonUtility.ToJson(trace, prettyPrint: true); // Prettyprint for debugging.
            var traceFromJson = JsonUtility.FromJson<InputEventTrace>(json);

            Assert.That(traceFromJson.eventCount, Is.EqualTo(trace.eventCount));
            Assert.That(traceFromJson.ToArray(), Is.EquivalentTo(trace.ToArray()));
        }
    }

    #endif

    // To simplify matters, rather than supporting some kind of buffer modifications while not supporting others,
    // we just reject all of them. When playback is in progress, the buffer must not change. Period.
    [Test]
    [Category("Events")]
    public void Events_WhenReplayingEvents_ModifyingTraceInvalidatesReplayController()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var trace = new InputEventTrace(gamepad))
        {
            trace.Enable();

            Press(gamepad.buttonSouth);

            var replay = trace.Replay();

            replay.PlayOneEvent();

            Release(gamepad.buttonSouth);

            Assert.That(() => replay.PlayOneEvent(), Throws.InvalidOperationException);
        }
    }

    [Test]
    [Category("Events")]
    public void Events_GetUniqueIds()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.QueueStateEvent(device, new GamepadState());

        var receivedCalls = 0;
        var firstId = InputEvent.InvalidEventId;
        var secondId = InputEvent.InvalidEventId;

        InputSystem.onEvent +=
            (eventPtr, _) =>
        {
            ++receivedCalls;
            if (receivedCalls == 1)
                firstId = eventPtr.id;
            else if (receivedCalls == 2)
                secondId = eventPtr.id;
        };

        InputSystem.Update();

        Assert.That(firstId, Is.Not.EqualTo(secondId));
    }

    [Test]
    [Category("Events")]
    public void Events_IfOldStateEventIsSentToDevice_IsIgnored()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 0.5f}, 2.0);
        InputSystem.Update();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 0.75f}, 1.0);
        InputSystem.Update();

        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(0.5f).Within(0.000001));
    }

    // This is another case of IInputStateCallbackReceiver making everything more complicated by deviating from
    // the common, simple code path. Basically, what this test here is trying to ensure is that we can send
    // touch states to a Touchscreen and not have them rejected because of timestamps. It's easy to order the
    // events for a single touch correctly but ordering them for all touches would require backends to make
    // a sorting pass over all events before queueing them.
    [Test]
    [Category("Events")]
    public void Events_IfOldStateEventIsSentToDevice_IsIgnored_ExceptIfEventIsHandledByIInputStateCallbackReceiver()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        // Sanity check.
        Assert.That(device is IInputStateCallbackReceiver,
            "Test assumes that Touchscreen implements IInputStateCallbackReceiver");

        InputSystem.QueueStateEvent(device, new TouchState { touchId = 1, phase = TouchPhase.Began, position = new Vector2(0.123f, 0.234f) }, 2);
        InputSystem.QueueStateEvent(device, new TouchState { touchId = 1, phase = TouchPhase.Moved, position = new Vector2(0.234f, 0.345f) }, 1);// Goes back in time.
        InputSystem.Update();

        Assert.That(device.lastUpdateTime, Is.EqualTo(2).Within(0.00001));
        Assert.That(device.position.ReadValue(), Is.EqualTo(new Vector2(0.234f, 0.345f)).Using(Vector2EqualityComparer.Instance));
    }

    private struct CustomNestedDeviceState : IInputStateTypeInfo
    {
        [InputControl(name = "button1", layout = "Button")]
        public int buttons;
        [InputControl(layout = "Axis")] public float axis2;

        public FourCC format => new FourCC('N', 'S', 'T', 'D');
    }

    private struct CustomDeviceState : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis")] public float axis;

        public CustomNestedDeviceState nested;

        public FourCC format => new FourCC('C', 'U', 'S', 'T');
    }

    [InputControlLayout(stateType = typeof(CustomDeviceState))]
    private class CustomDevice : InputDevice
    {
        public AxisControl axis { get; private set; }

        protected override void FinishSetup()
        {
            axis = GetChildControl<AxisControl>("axis");
            base.FinishSetup();
        }
    }

    [InputControlLayout(stateType = typeof(CustomDeviceState))]
    private class CustomDeviceWithUpdate : CustomDevice, IInputUpdateCallbackReceiver
    {
        public int onUpdateCallCount;

        public void OnUpdate()
        {
            ++onUpdateCallCount;
            InputSystem.QueueStateEvent(this, new CustomDeviceState {axis = 0.234f});
        }
    }

    // We want devices to be able to "park" unused controls outside of the state
    // memory region that is being sent to the device in events.
    [Test]
    [Category("Events")]
    public void Events_CanSendSmallerStateToDeviceWithLargerState()
    {
        const string json = @"
            {
                ""name"" : ""TestLayout"",
                ""extend"" : ""CustomDevice"",
                ""controls"" : [
                    { ""name"" : ""extra"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayout<CustomDevice>();
        InputSystem.RegisterLayout(json);
        var device = (CustomDevice)InputSystem.AddDevice("TestLayout");

        InputSystem.QueueStateEvent(device, new CustomDeviceState {axis = 0.5f});
        InputSystem.Update();

        Assert.That(device.axis.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
    }

    private struct ExtendedCustomDeviceState : IInputStateTypeInfo
    {
        public CustomDeviceState baseState;
        public int extra;

        public FourCC format => baseState.format;
    }

    // HIDs rely on this behavior as we may only use a subset of a HID's set of
    // controls and thus get state events that are larger than the device state
    // that we store for the HID.
    [Test]
    [Category("Events")]
    public void Events_CanSendLargerStateToDeviceWithSmallerState()
    {
        var device = InputSystem.AddDevice<CustomDevice>();

        var state = new ExtendedCustomDeviceState {baseState = {axis = 0.5f}};
        InputSystem.QueueStateEvent(device, state);
        InputSystem.Update();

        Assert.That(device.axis.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public unsafe void Events_CanGetStatePointerFromEventThroughControl()
    {
        // We use a mouse here as it has several controls that are "parked" outside MouseState.
        var mouse = InputSystem.AddDevice<Mouse>();

        InputSystem.onEvent +=
            (eventPtr, _) =>
        {
            // For every control that isn't contained in a state event, GetStatePtrFromStateEvent() should
            // return IntPtr.Zero.
            if (eventPtr.IsA<StateEvent>())
            {
                Assert.That(mouse.position.GetStatePtrFromStateEvent(eventPtr) != null);
            }
            else if (eventPtr.IsA<DeltaStateEvent>())
            {
                Assert.That(mouse.position.GetStatePtrFromStateEvent(eventPtr) != null);
                Assert.That(mouse.leftButton.GetStatePtrFromStateEvent(eventPtr) == null);
            }
            else
            {
                Assert.Fail("Unexpected type of event");
            }
        };

        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.QueueDeltaStateEvent(mouse.position, new Vector2(0.5f, 0.5f));
        InputSystem.Update();
    }

    // For devices that implement IStateCallbackReceiver (such as Touchscreen), things get tricky. We may be looking
    // at an event in a state format different from that of the device and with only the device knowing how it
    // correlates to the state of an individual control.
    [Test]
    [Category("Events")]
    public unsafe void Events_CanGetStatePointerFromEventThroughControl_EvenIfDeviceIsStateCallbackReceiver()
    {
        var touchscreen = InputSystem.AddDevice<Touchscreen>();

        using (var trace = new InputEventTrace { deviceId = touchscreen.deviceId })
        {
            trace.Enable();

            BeginTouch(1, new Vector2(123, 234));

            var statePtr = touchscreen.primaryTouch.position.GetStatePtrFromStateEvent(trace.ToArray()[0]);
            Assert.That(statePtr != null);

            // Attempt reading the position value from the touch event.
            Assert.That(touchscreen.primaryTouch.position.ReadValueFromState(statePtr),
                Is.EqualTo(new Vector2(123, 234)).Using(Vector2EqualityComparer.Instance));

            // It only works with primaryTouch. See Touchscreen.GetStateOffsetForEvent for details.
            Assert.That(touchscreen.touches[1].position.GetStatePtrFromStateEvent(trace.ToArray()[0]) == null);
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanListenForWhenAllEventsHaveBeenProcessed()
    {
        var receivedCalls = 0;
        void callback() => ++ receivedCalls;

        InputSystem.onAfterUpdate += callback;

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));

        receivedCalls = 0;
        InputSystem.onAfterUpdate -= callback;

        InputSystem.Update();

        Assert.That(receivedCalls, Is.Zero);
    }

    [Test]
    [Category("Events")]
    public void Events_EventBuffer_CanIterateEvents()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        unsafe
        {
            using (StateEvent.From(gamepad, out var eventPtr))
            using (var buffer = new InputEventBuffer(eventPtr, 1))
            {
                Assert.That(buffer.eventCount, Is.EqualTo(1));
                Assert.That(buffer.sizeInBytes, Is.EqualTo(InputEventBuffer.BufferSizeUnknown));
                Assert.That(buffer.capacityInBytes, Is.Zero);
                Assert.That(buffer.bufferPtr, Is.EqualTo(eventPtr));

                var events = buffer.ToArray();
                Assert.That(events, Has.Length.EqualTo(1));
                Assert.That(events[0], Is.EqualTo(eventPtr));
            }
        }
    }

    [Test]
    [Category("Events")]
    public void Events_EventBuffer_CanAddEvents()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        unsafe
        {
            using (StateEvent.From(gamepad, out var eventPtr))
            using (var buffer = new InputEventBuffer())
            {
                // Write two events into buffer.
                gamepad.leftStick.WriteValueIntoEvent(Vector2.one, eventPtr);
                eventPtr.id = 111;
                eventPtr.time = 123;
                eventPtr.handled = false;
                buffer.AppendEvent(eventPtr);
                gamepad.leftStick.WriteValueIntoEvent(Vector2.zero, eventPtr);
                eventPtr.id = 222;
                eventPtr.time = 234;
                eventPtr.handled = true;
                buffer.AppendEvent(eventPtr);

                Assert.That(buffer.eventCount, Is.EqualTo(2));
                var events = buffer.ToArray();

                Assert.That(events, Has.Length.EqualTo(2));
                Assert.That(events[0].type, Is.EqualTo(new FourCC(StateEvent.Type)));
                Assert.That(events[1].type, Is.EqualTo(new FourCC(StateEvent.Type)));
                Assert.That(events[0].time, Is.EqualTo(123).Within(0.00001));
                Assert.That(events[1].time, Is.EqualTo(234).Within(0.00001));
                Assert.That(events[0].id, Is.EqualTo(111));
                Assert.That(events[1].id, Is.EqualTo(222));
                Assert.That(events[0].handled, Is.False);
                Assert.That(events[1].handled, Is.True);
                Assert.That(events[0].deviceId, Is.EqualTo(gamepad.deviceId));
                Assert.That(events[1].deviceId, Is.EqualTo(gamepad.deviceId));
                Assert.That(InputControlExtensions.ReadUnprocessedValueFromEvent(gamepad.leftStick, events[0]), Is.EqualTo(Vector2.one));
                Assert.That(InputControlExtensions.ReadUnprocessedValueFromEvent(gamepad.leftStick, events[1]), Is.EqualTo(Vector2.zero));
            }
        }
    }

    [Test]
    [Category("Events")]
    public void Events_EventBuffer_CanBeReset()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        unsafe
        {
            using (var buffer = new InputEventBuffer())
            {
                buffer.AppendEvent(DeviceConfigurationEvent.Create(gamepad.deviceId, 123).ToEventPtr());
                buffer.AppendEvent(DeviceConfigurationEvent.Create(gamepad.deviceId, 234).ToEventPtr());

                var events = buffer.ToArray();
                Assert.That(events, Has.Length.EqualTo(2));
                Assert.That(events[0].type, Is.EqualTo(new FourCC(DeviceConfigurationEvent.Type)));
                Assert.That(events[1].type, Is.EqualTo(new FourCC(DeviceConfigurationEvent.Type)));

                buffer.Reset();

                Assert.That(buffer.eventCount, Is.Zero);

                buffer.AppendEvent(DeviceRemoveEvent.Create(gamepad.deviceId, 432).ToEventPtr());

                events = buffer.ToArray();

                Assert.That(events.Length, Is.EqualTo(1));
                Assert.That(events[0].type, Is.EqualTo(new FourCC(DeviceRemoveEvent.Type)));
            }
        }
    }

    [Test]
    [Category("Events")]
    public void Events_EventBuffer_CanAllocateEvent()
    {
        unsafe
        {
            using (var buffer = new InputEventBuffer())
            {
                var eventPtr = buffer.AllocateEvent(1024);

                Assert.That(buffer.bufferPtr, Is.EqualTo(new InputEventPtr(eventPtr)));
                Assert.That(buffer.eventCount, Is.EqualTo(1));
                Assert.That(eventPtr->sizeInBytes, Is.EqualTo(1024));
                Assert.That(eventPtr->type, Is.EqualTo(new FourCC()));
            }
        }
    }

    [UnityTest]
    [Category("Events")]
    public IEnumerator Events_CanTestInputDistributedOverFrames()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Press(gamepad.buttonSouth, queueEventOnly: true);

        yield return null;

        Assert.That(gamepad.buttonSouth.isPressed, Is.True);
    }

    [Test]
    [Category("Events")]
    public void Events_MaximumEventLoadPerUpdateIsLimited()
    {
        // Default setting is 5MB.
        Assert.That(InputSystem.settings.maxEventBytesPerUpdate, Is.EqualTo(5 * 1024 * 1024));

        InputSystem.settings.maxEventBytesPerUpdate = StateEvent.GetEventSizeWithPayload<MouseState>() * 2;

        var mouse = InputSystem.AddDevice<Mouse>();

        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Middle));

        var eventCount = 0;
        InputSystem.onEvent += (eventPtr, device) => ++ eventCount;

        LogAssert.Expect(LogType.Error, "Exceeded budget for maximum input event throughput per InputSystem.Update(). Discarding remaining events. "
            + "Increase InputSystem.settings.maxEventBytesPerUpdate or set it to 0 to remove the limit.");

        InputSystem.Update();

        Assert.That(eventCount, Is.EqualTo(2));
        Assert.That(mouse.rightButton.isPressed, Is.True);
        Assert.That(mouse.middleButton.isPressed, Is.False);

        eventCount = 0;

        // Disable the limit.
        InputSystem.settings.maxEventBytesPerUpdate = 0;

        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Middle));

        InputSystem.Update();

        Assert.That(eventCount, Is.EqualTo(3));
        Assert.That(mouse.rightButton.isPressed, Is.False);
        Assert.That(mouse.middleButton.isPressed, Is.True);
    }

    [Test]
    [Category("Events")]
    [TestCase(false)]
    [TestCase(true)]
    public void Events_CanQueueEventsFromWithinEventProcessing_WithEventMergingSetTo(bool mergeRedundantEvents)
    {
        InputSystem.settings.disableRedundantEventsMerging = !mergeRedundantEvents;

        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var numMouseEventsQueued = InputTestRuntime.kDefaultEventBufferSize / StateEvent.GetEventSizeWithPayload<MouseState>() + 1;

        // allow all these events
        InputSystem.settings.maxQueuedEventsPerUpdate = numMouseEventsQueued;

        var numMouseEventsReceived = 0;
        InputSystem.onEvent +=
            (eventPtr, device) =>
        {
            if (device == mouse)
                ++numMouseEventsReceived;
        };

        var action = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/space");
        action.performed +=
            _ =>
        {
            // Queue enough events to make sure we overflow the current event buffer size.
            // This way we not only test whether the events make it into the queue at all but
            // also that they still make it if we have to re-allocate the buffer.
            for (var i = 0; i < numMouseEventsQueued; ++i)
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2((i + 1), (i + 1) * 2), delta = Vector2.one}.WithButton(MouseButton.Left));
        };
        action.Enable();

        Press(keyboard.spaceKey);

        Assert.That(mouse.leftButton.isPressed, Is.True);
        Assert.That(mouse.position.ReadValue(), Is.EqualTo(new Vector2(numMouseEventsQueued, numMouseEventsQueued * 2)));
        Assert.That(mouse.delta.ReadValue(), Is.EqualTo(new Vector2(numMouseEventsQueued, numMouseEventsQueued)));
        Assert.That(numMouseEventsReceived, Is.EqualTo(mergeRedundantEvents ? 1 : numMouseEventsQueued));
    }

    [Test]
    [Category("Events")]
    public void Events_QueuedToDifferentDevices_AreNotMergedTogether()
    {
        var mouse1 = InputSystem.AddDevice<Mouse>();
        var mouse2 = InputSystem.AddDevice<Mouse>();

        var numMouse1EventsReceived = 0;
        var numMouse2EventsReceived = 0;
        InputSystem.onEvent +=
            (eventPtr, device) =>
        {
            if (device == mouse1)
                ++numMouse1EventsReceived;
            if (device == mouse2)
                ++numMouse2EventsReceived;
        };

        InputSystem.QueueStateEvent(mouse1, new MouseState { position = new Vector2(1, 2)});
        InputSystem.QueueStateEvent(mouse1, new MouseState { position = new Vector2(2, 3)});
        InputSystem.QueueStateEvent(mouse2, new MouseState { position = new Vector2(3, 4)});
        InputSystem.QueueStateEvent(mouse2, new MouseState { position = new Vector2(4, 5)});
        InputSystem.QueueStateEvent(mouse2, new MouseState { position = new Vector2(5, 6)});

        InputSystem.Update();

        Assert.That(mouse1.position.ReadValue(), Is.EqualTo(new Vector2(2, 3)));
        Assert.That(mouse2.position.ReadValue(), Is.EqualTo(new Vector2(5, 6)));

        Assert.That(numMouse1EventsReceived, Is.EqualTo(1));
        Assert.That(numMouse2EventsReceived, Is.EqualTo(1));
    }

    [Test]
    [Category("Events")]
    public void Events_MaximumQueuedEventsDuringEventProcessingIsLimited()
    {
        // Default setting is 1000.
        Assert.That(InputSystem.settings.maxQueuedEventsPerUpdate, Is.EqualTo(1000));

        InputSystem.settings.maxQueuedEventsPerUpdate = 20;

        var mouse = InputSystem.AddDevice<Mouse>();

        var callbackCount = 0;
        var action = new InputAction(type: InputActionType.Value, binding: "<mouse>/position");
        action.performed +=
            _ =>
        {
            if (callbackCount > InputSystem.settings.maxQueuedEventsPerUpdate)
                Assert.Fail("Maximum queued event count exceeded");

            callbackCount++;
            Set(mouse.position, Random.insideUnitCircle * 100, queueEventOnly: true);
        };
        action.Enable();

        Set(mouse.position, Random.insideUnitCircle * 100);

        LogAssert.Expect(LogType.Error, $"Maximum number of queued events exceeded. Set the '{nameof(InputSettings.maxQueuedEventsPerUpdate)}' setting to a higher value if you " +
            $"need to queue more events than this. Current limit is '{InputSystem.settings.maxQueuedEventsPerUpdate}'.");
        Assert.That(callbackCount - 1, Is.EqualTo(InputSystem.settings.maxQueuedEventsPerUpdate));
    }

    // https://fogbugz.unity3d.com/f/cases/1316000/
    [Test]
    [Category("Events")]
    public void Events_CallingInputSystemUpdateDuringEventProcessingThrowsInvalidOperation()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "<Gamepad>/buttonSouth");
        action.performed += _ =>
        {
            InputSystem.Update();
        };
        action.Enable();

        LogAssert.Expect(LogType.Exception, "InvalidOperationException: Already have an event buffer set! Was OnUpdate() called recursively?");
        LogAssert.Expect(LogType.Error, "InvalidOperationException during event processing of Dynamic update; resetting event buffer");
        LogAssert.Expect(LogType.Exception, "InvalidOperationException: Already have an event buffer set! Was OnUpdate() called recursively?");
        LogAssert.Expect(LogType.Error, "InvalidOperationException while executing 'performed' callbacks of '<Unnamed>[/Gamepad/buttonSouth]'");

        Press(device.buttonSouth);
    }

    private struct ThrowingExceptionTestDeviceState : IInputStateTypeInfo
    {
        public static FourCC Format => new FourCC('T', 'E', 'T', 'D');
        public FourCC format => Format;
        [InputControl(layout = "Axis")] public float axis;
    }

    [InputControlLayout(stateType = typeof(ThrowingExceptionTestDeviceState))]
    private class ThrowingExceptionTestDevice : InputDevice, IInputStateCallbackReceiver
    {
        public class TestException : Exception
        {
        }

        public bool throwExceptionOnState = true;
        public AxisControl axis { get; private set; }

        protected override void FinishSetup()
        {
            axis = GetChildControl<AxisControl>("axis");
            base.FinishSetup();
        }

        void IInputStateCallbackReceiver.OnNextUpdate()
        {
        }

        unsafe void IInputStateCallbackReceiver.OnStateEvent(InputEventPtr eventPtr)
        {
            if (throwExceptionOnState)
                throw new TestException();

            if (eventPtr.type != StateEvent.Type)
                return;

            var stateEvent = StateEvent.FromUnchecked(eventPtr);
            if (stateEvent->stateFormat != ThrowingExceptionTestDeviceState.Format)
                return;

            var newState = *(ThrowingExceptionTestDeviceState*)stateEvent->state;
            InputState.Change(this, ref newState, InputState.currentUpdateType, eventPtr: eventPtr);
        }

        bool IInputStateCallbackReceiver.GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
        {
            return false;
        }
    };

    [Test]
    [Category("Events")]
    public void Events_ThrowingExceptionsInInternalCode_DoesntLookupLatterUpdates()
    {
        var device = InputSystem.AddDevice<ThrowingExceptionTestDevice>();

        InputSystem.QueueStateEvent(device, new ThrowingExceptionTestDeviceState {axis = 50.0f});
        LogAssert.Expect(LogType.Exception, "TestException: Exception of type 'CoreTests+ThrowingExceptionTestDevice+TestException' was thrown.");
        LogAssert.Expect(LogType.Error, "TestException during event processing of Dynamic update; resetting event buffer");
        Assert.That(() => InputSystem.Update(), Throws.TypeOf<ThrowingExceptionTestDevice.TestException>());
        Assert.That(device.axis.ReadValue(), Is.EqualTo(0.0f));

        // Event buffer should be reset so latter update calls should work without any errors
        InputSystem.Update();
        InputSystem.Update();

        // Everything should recover to a working state
        device.throwExceptionOnState = false;
        InputSystem.QueueStateEvent(device, new ThrowingExceptionTestDeviceState {axis = 100.0f});
        InputSystem.Update();
        Assert.That(device.axis.ReadValue(), Is.EqualTo(100.0f));
    }

    ////TODO: test thread-safe QueueEvent
}
