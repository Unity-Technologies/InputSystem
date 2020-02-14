# Input events

* [Types of events](#types-of-events)
    * [State events](#state-events)
    * [Device events](#device-events)
    * [Text events](#text-events)
* [Working with events](#working-with-events)
    * [Monitoring events](#monitoring-events)
    * [Reading state events](#reading-state-events)
    * [Creating events](#creating-events)
    * [Capturing events](#capturing-events)

The Input System is event-driven. All input is delivered as events, and you can generate custom input by injecting events. You can also observe all source input by listening in on the events flowing through the system.

>__Note__: Events are an advanced, mostly internal feature of the Input System. Knowledge of the event system is mostly useful if you want to support custom Devices, or change the behavior of existing Devices.

Input events are a low-level mechanism. Usually, you don't need to deal with events if all you want to do is receive input for your app. Events are stored in unmanaged memory buffers and not converted to C# heap objects. The Input System provides wrapper APIs, but unsafe code is required for more involved event manipulations.

Note that there are no routing mechanism. The runtime delivers events straight to the Input System, which then incorporates them directly into the Device state.

Input events are represented by the [`InputEvent`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html) struct. Each event has a set of common properties:

|Property|Description|
|--------|-----------|
|[`type`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_type)|[`FourCC`](../api/UnityEngine.InputSystem.Utilities.FourCC.html) code that indicates what type of event it is.|
|[`eventId`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_eventId)|Unique numeric ID of the event.|
|[`time`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_time)|Timestamp of when the event was generated.|
|[`deviceId`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_deviceId)|ID of the Device that the event targets.|
|[`sizeInBytes`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_sizeInBytes)|Total size of the event in bytes.|

You can observe the events received for a specific input device in the [input debugger](Debugging.md#debugging-devices).

## Types of events

### State events

A state event contains the input state for a Device. The Input System uses these events to feed new input to Devices.

There are two types of state events:

* [`StateEvent`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html) (`'STAT'`)
* [`DeltaStateEvent`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html) (`'DLTA'`)

[`StateEvent`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html) contains a full snapshot of the entire state of a Device in the format specific to that Device. The [`stateFormat`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html#UnityEngine_InputSystem_LowLevel_StateEvent_stateFormat) field identifies the type of the data in the event. You can access the raw data using the [`state`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html#UnityEngine_InputSystem_LowLevel_StateEvent_state) pointer and [`stateSizeInBytes`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html#UnityEngine_InputSystem_LowLevel_StateEvent_stateSizeInBytes).

A [`DeltaStateEvent`](../api/UnityEngine.InputSystem.LowLevel.DeltaStateEvent.html) is like a [`StateEvent`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html), but only contains a partial snapshot of the state of a Device. The Input System usually sends this for Devices that require a large state record, to reduce the amount of memory it needs to update if only some of the Controls change their state. To access the raw data, you can use the [`deltaState`](../api/UnityEngine.InputSystem.LowLevel.DeltaStateEvent.html#UnityEngine_InputSystem_LowLevel_DeltaStateEvent_deltaState) pointer and [`deltaStateSizeInBytes`](../api/UnityEngine.InputSystem.LowLevel.DeltaStateEvent.html#UnityEngine_InputSystem_LowLevel_DeltaStateEvent_deltaStateSizeInBytes). The Input System should apply the data to the Device's state at the offset defined by [`stateOffset`](../api/UnityEngine.InputSystem.LowLevel.DeltaStateEvent.html#UnityEngine_InputSystem_LowLevel_DeltaStateEvent_stateOffset).

### Device events

Device events indicate a change that is relevant to a Device as a whole. If you're interested in these events, it is usually more convenient to subscribe to the higher-level [`InputSystem.onDeviceChange`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceChange) event rather then processing [`InputEvents`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html) yourself.

There are two types of Device events:

* [`DeviceRemoveEvent`](../api/UnityEngine.InputSystem.LowLevel.DeviceRemoveEvent.html) (`'DREM'`)
* [`DeviceConfigurationEvent`](../api/UnityEngine.InputSystem.LowLevel.DeviceConfigurationEvent.html) (`'DCFG'`)

`DeviceRemovedEvent` indicates that a Device has been removed or disconnected. To query the device that has been removed, you can use the common [`deviceId`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_deviceId) field. This event doesn't have any additional data.

`DeviceConfigurationEvent` indicates that the configuration of a Device has changed. The meaning of this is Device-specific. This might signal, for example, that the layout used by the keyboard has changed or that, on a console, a gamepad has changed which player ID(s) it is assigned to. You can query the changed device from the common [`deviceId`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_deviceId) field. This event doesn't have any additional data.

### Text events

[Keyboard](Keyboard.md) devices send these events to handle text input. If you're interested in these events, it's usually more convenient to subscribe to the higher-level [callbacks on the Keyboard class](Keyboard.md#text-input) rather than processing [`InputEvents`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html) yourself.

There are two types of text events:

* [`TextEvent`](../api/UnityEngine.InputSystem.LowLevel.TextEvent.html) (`'TEXT'`)
* [`IMECompositionEvent`](../api/UnityEngine.InputSystem.LowLevel.IMECompositionEvent.html) (`'IMES'`)

## Working with events

### Monitoring events

If you want to do any monitoring or processing on incoming events yourself, subscribe to the [`InputSystem.onEvent`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onEvent) callback.

### Reading state events

State events contain raw memory snapshots for Devices. As such, interpreting the data in the event requires knowledge about where and how individual state is stored for a given Device.

The easiest way to access state contained in a state event is to rely on the Device that the state is meant for. You can ask any Control to read its value from a given event rather than from its own internally stored state.

For example, the following code demonstrates how to read a value for [`Gamepad.leftStick`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_leftStick) from a state event targeted at a [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html).

```CSharp
InputSystem.onEvent +=
    (eventPtr, device) =>
    {
        // Ignore anything that isn't a state event.
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        var gamepad = device as Gamepad;
        if (gamepad == null)
        {
            // Event isn't for a gamepad or device ID is no longer valid.
            return;
        }

        var leftStickValue = gamepad.leftStick.ReadValueFromEvent(eventPtr);
    };
```

### Creating events

Anyone can create and queue new input events against any existing Device. Queueing an input event is thread-safe, which means that event generation can happen in background threads.

>__Note__: Unity allocates limited memory to events that come from background threads. If background threads produce too many events, queueing an event from a thread blocks the thread until the main thread flushes out the background event queue.

Note that queuing an event doesn't immediately consume the event. Event processing happens on the next update (depending on [`InputSettings.updateMode`](Settings.md#update-mode), it is triggered either manually via [`InputSystem.Update`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_Update), or automatically as part of the Player loop).

#### Sending state events

The easiest way to create a state event is directly from the Device.

```CSharp
// `StateEvent.From` creates a temporary buffer in unmanaged memory that holds
// a state event large enough for the given device and contains a memory
// copy of the device's current state.
InputEventPtr eventPtr;
using (StateEvent.From(myDevice, out eventPtr))
{
    ((AxisControl) myDevice["myControl"]).WriteValueIntoEvent(0.5f, eventPtr);
    InputSystem.QueueEvent(eventPtr);
}
```

Alternatively, you can send events for individual Controls.

```CSharp
// Send event to update leftStick on the gamepad.
InputSystem.QueueDeltaStateEvent(Gamepad.current.leftStick,
    new Vector2(0.123f, 0.234f);
```

Note that delta state events only work for Controls that are both byte-aligned and a multiple of 8 bits in size in memory. You can't send a delta state event for a button Control that is stored as a single bit, for example.

### Capturing Events

>NOTE: To download a sample project which contains a reusable MonoBehaviour called `InputRecorder`, which can capture and replay input from arbitrary devices, open the Package Manager, select the Input System Package, and choose the sample project "Input Recorder" to download.

You can use the [`InputEventTrace`](../api/UnityEngine.InputSystem.LowLevel.InputEventTrace.html) class to record input events for later processing:

```CSharp
var trace = new InputEventTrace(); // Can also give device ID to only
                                   // trace events for a specific device.

trace.Enable();

//... run stuff

var current = new InputEventPtr();
while (trace.GetNextEvent(ref current))
{
    Debug.Log("Got some event: " + current);
}

// Also supports IEnumerable.
foreach (var eventPtr in trace)
    Debug.Log("Got some event: " + eventPtr);

// Trace consumes unmanaged resources. Make sure to dispose.
trace.Dispose();
```

Dispose event traces after use, so that they do not leak memory on the unmanaged (C++) memory heap.

You can also write event traces out to files/streams, load them back in, and replay recorded streams.

```CSharp
// Set up a trace with such that it automatically grows in size as needed.
var trace = new InputEventTrace(growBuffer: true);
trace.Enable();

// ... capture some input ...

// Write trace to file.
trace.WriteTo("mytrace.inputtrace.");

// Load trace from same file.
var loadedTrace = InputEventTrace.LoadFrom("mytrace.inputtrace");
```

You can replay captured traces directly from [`InputEventTrace`](../api/UnityEngine.InputSystem.LowLevel.InputEventTrace.html) instances using the [`Replay`](../api/UnityEngine.InputSystem.LowLevel.InputEventTrace.html#UnityEngine_InputSystem_LowLevel_InputEventTrace_Replay_) method.

```CSharp
// The Replay method returns a ReplayController that can be used to
// configure and control playback.
var controller = trace.Replay();

// For example, to not replay the events as is but rather create new devices and send
// the events to them, call WithAllDevicesMappedToNewInstances.
controller.WithAllDevicessMappedToNewInstances();

// Replay all frames one by one.
controller.PlayAllFramesOnyByOne();

// Replay events in a way that tries to simulate original event timing.
controller.PlayAllEventsAccordingToTimestamps();
```
