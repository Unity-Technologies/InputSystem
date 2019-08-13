using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Profiling;
using UnityEngine.TestTools.Constraints;
using UnityEngine.TestTools.Utils;
using Is = UnityEngine.TestTools.Constraints.Is;
using Property = NUnit.Framework.PropertyAttribute;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

#pragma warning disable CS0649
partial class CoreTests
{
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
    public unsafe void Events_AreTimeslicedByDefault()
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

        Assert.That(InputUpdate.s_LastUpdateRetainedEventCount, Is.Zero);

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

        Assert.That(InputUpdate.s_LastUpdateRetainedEventCount, Is.EqualTo(2));

        receivedEvents.Clear();

        runtime.currentTimeForFixedUpdate += 1 / 60.0f;

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.EqualTo(1));
        Assert.That(receivedEvents[0].time, Is.EqualTo(3 + 1.0 / 60 + 0.001).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.3456).Within(0.00001));

        Assert.That(InputUpdate.s_LastUpdateRetainedEventCount, Is.EqualTo(1));

        receivedEvents.Clear();

        runtime.currentTimeForFixedUpdate += 1 / 60.0f;

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.EqualTo(1));
        Assert.That(receivedEvents[0].time, Is.EqualTo(3 + 2 * (1.0 / 60) + 0.001).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.4567).Within(0.00001));

        Assert.That(InputUpdate.s_LastUpdateRetainedEventCount, Is.Zero);

        receivedEvents.Clear();

        runtime.currentTimeForFixedUpdate += 1 / 60.0f;

        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedEvents, Has.Count.Zero);
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.4567).Within(0.00001));

        Assert.That(InputUpdate.s_LastUpdateRetainedEventCount, Is.Zero);
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

            Assert.That(stateEventPtr->baseEvent.deviceId, Is.EqualTo(mouse.id));
            Assert.That(stateEventPtr->baseEvent.time, Is.EqualTo(runtime.currentTime));
            Assert.That(stateEventPtr->baseEvent.sizeInBytes, Is.EqualTo(buffer.Length));
            Assert.That(stateEventPtr->baseEvent.sizeInBytes,
                Is.EqualTo(InputEvent.kBaseEventSize + sizeof(FourCC) + mouse.stateBlock.alignedSizeInBytes));
            Assert.That(stateEventPtr->stateSizeInBytes, Is.EqualTo(mouse.stateBlock.alignedSizeInBytes));
            Assert.That(stateEventPtr->stateFormat, Is.EqualTo(mouse.stateBlock.format));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanCreateDeltaStateEventFromControl()
    {
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
            Assert.That(inputEvent.deviceId, Is.EqualTo(device.id));
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
    public void Events_CanQueueAndReceiveEventsAgainstNonExistingDevices()
    {
        // Device IDs are looked up only *after* the system shows the event to us.

        var receivedCalls = 0;
        var receivedDeviceId = InputDevice.InvalidDeviceId;
        InputSystem.onEvent +=
            (eventPtr, _) =>
        {
            ++receivedCalls;
            receivedDeviceId = eventPtr.deviceId;
        };

        var inputEvent = DeviceConfigurationEvent.Create(4, 1.0);
        InputSystem.QueueEvent(ref inputEvent);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDeviceId, Is.EqualTo(4));
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

        var inputEvent = DeviceConfigurationEvent.Create(4, 1.0);

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

        using (var trace = new InputEventTrace {deviceId = device.id})
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
            Assert.That(events[0].deviceId, Is.EqualTo(device.id));
            Assert.That(events[0].time, Is.EqualTo(0.5).Within(0.000001));
            Assert.That(events[0].sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<GamepadState>()));
            Assert.That(UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref firstState),
                StateEvent.From(events[0])->state, UnsafeUtility.SizeOf<GamepadState>()), Is.Zero);

            Assert.That(events[1].type, Is.EqualTo((FourCC)StateEvent.Type));
            Assert.That(events[1].deviceId, Is.EqualTo(device.id));
            Assert.That(events[1].time, Is.EqualTo(1.5).Within(0.000001));
            Assert.That(events[1].sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<GamepadState>()));
            Assert.That(UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref secondState),
                StateEvent.From(events[1])->state, UnsafeUtility.SizeOf<GamepadState>()), Is.Zero);
        }
    }

    [Test]
    [Category("Events")]
    public void Events_WhenTraceIsFull_WillStartOverwritingOldEvents()
    {
        var device = InputSystem.AddDevice<Gamepad>();
        using (var trace =
                   new InputEventTrace(StateEvent.GetEventSizeWithPayload<GamepadState>() * 2) {deviceId = device.id})
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

            Assert.That(trace.ToList(), Has.Count.EqualTo(2));

            trace.Clear();

            Assert.That(trace.ToList(), Has.Count.EqualTo(0));
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
        var firstId = InputEvent.InvalidId;
        var secondId = InputEvent.InvalidId;

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
    public unsafe void Events_CanDetectWhetherControlIsPartOfEvent()
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
                Assert.That(mouse.tilt.GetStatePtrFromStateEvent(eventPtr) == null);
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

    [Test]
    [Category("Events")]
    public void Events_CanListenForWhenAllEventsHaveBeenProcessed()
    {
        var receivedCalls = 0;
        Action callback = () => ++ receivedCalls;

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
                Assert.That(events[0].deviceId, Is.EqualTo(gamepad.id));
                Assert.That(events[1].deviceId, Is.EqualTo(gamepad.id));
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
                buffer.AppendEvent(DeviceConfigurationEvent.Create(gamepad.id, 123).ToEventPtr());
                buffer.AppendEvent(DeviceConfigurationEvent.Create(gamepad.id, 234).ToEventPtr());

                var events = buffer.ToArray();
                Assert.That(events, Has.Length.EqualTo(2));
                Assert.That(events[0].type, Is.EqualTo(new FourCC(DeviceConfigurationEvent.Type)));
                Assert.That(events[1].type, Is.EqualTo(new FourCC(DeviceConfigurationEvent.Type)));

                buffer.Reset();

                Assert.That(buffer.eventCount, Is.Zero);

                buffer.AppendEvent(DeviceRemoveEvent.Create(gamepad.id, 432).ToEventPtr());

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
}
