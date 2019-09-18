>__Note____: Events are an advanced, mostly internal feature of the Input System. You don't need to understand them to use the Input System. Knowledge of the event system is mostly useful if you want to support custom Devices or change the behavior of existing Devices.

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

Input events are a low-level mechanism. Usually, you don't need to deal with events if all you want to do is receive input for your app. Events are stored in unmanaged memory buffers and not converted to C# heap objects. The Input System provides wrapper APIs, but unsafe code is required for more involved event manipulations.

Note that there is no routing mechanism. Events are delivered from the runtime straight to the Input System, where they are incorporated directly into Device state.

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

A state event contains input state for a Device. The Input System uses these events to feed new input to Devices.

There are two types of state events:

* [`StateEvent`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html) (`'STAT'`)
* [`DeltaStateEvent`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html) (`'DLTA'`)

[`StateEvent`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html) contains a full snapshot of the entire state of a Device in the format specific to that Device. The [`stateFormat`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html#UnityEngine_InputSystem_LowLevel_StateEvent_stateFormat) field identifies the type of the  data in the event. You can access the raw data using the [`state`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html#UnityEngine_InputSystem_LowLevel_StateEvent_state) pointer and [`stateSizeInBytes`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html#UnityEngine_InputSystem_LowLevel_StateEvent_stateSizeInBytes).

A [`DeltaStateEvent`](../api/UnityEngine.InputSystem.LowLevel.DeltaStateEvent.html) is like a [`StateEvent`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html), but only contains a partial snapshot of the state of a Device. The backend usually sends this for Devices requiring a large state record to reduce the amount of memory which needs to be updated if only some of the Controls change their state. You can access the raw data using the [`deltaState`](../api/UnityEngine.InputSystem.LowLevel.DeltaStateEvent.html#UnityEngine_InputSystem_LowLevel_DeltaStateEvent_deltaState) pointer and [`deltaStateSizeInBytes`](../api/UnityEngine.InputSystem.LowLevel.DeltaStateEvent.html#UnityEngine_InputSystem_LowLevel_DeltaStateEvent_deltaStateSizeInBytes). The data should be applied to the Device's state at the offset at [`stateOffset`](../api/UnityEngine.InputSystem.LowLevel.DeltaStateEvent.html#UnityEngine_InputSystem_LowLevel_DeltaStateEvent_stateOffset).

### Device events

Device events indicate a change that is relevant to a Device as a whole. If you're interested in these events, it is usually more convenient to subscribe to the higher-level [`InputSystem.onDeviceChange`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceChange) event rather then processing [`InputEvents`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html) yourself.

There are two types of Device events:

* [`DeviceRemoveEvent`](../api/UnityEngine.InputSystem.LowLevel.DeviceRemoveEvent.html) (`'DREM'`)
* [`DeviceConfigurationEvent`](../api/UnityEngine.InputSystem.LowLevel.DeviceConfigurationEvent.html) (`'DCFG'`)

`DeviceRemovedEvent` indicates that a Device has been removed or disconnected. You can query the device which has been removed from the common [`deviceId`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_deviceId) field. This event doesn't have any additional data.

`DeviceConfigurationEvent` indicates that the configuration of a Device has changed. The meaning of this is Device-specific. This may signal, for example, that the layout used by the keyboard has changed or that, on a console, a gamepad has changed which player ID(s) it is assigned to. You can query the changed device from the common [`deviceId`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_deviceId) field. This event does not have any additional data.

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

The easiest way to access state contained in a state event is to rely on the Device the state is meant for. You can ask any Control to read its value from a given event rather than from its own internally stored state.

For example, the following code demonstrates how to read a value for [`Gamepad.leftStick`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_leftStick) from a state event targeted at a [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html).

```
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

>__Note__: Memory allocated to events coming from background threads is limited. If too many events are produced by background threads, queueing an event from a thread will block the thread until the main thread has flushed out the background event queue.

Note that queuing an event will not immediately consume the event. Processing of events happens on the next update (depending [`InputSettings.updateMode`](Settings.md#update-mode), it can be either manually triggered via [`InputSystem.Update`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_Update) or triggered automatically as part of the Player loop).

#### Sending state events

The easiest way to create a state event is directly from the Device.

```
// `StateEvent.From` creates a temporary buffer in unmanaged memory holding
// a state event large enough for the given device and containing a memory
// copy of the device's current state.
InputEventPtr eventPtr;
using (StateEvent.From(myDevice, out eventPtr))
{
    ((AxisControl) myDevice["myControl"]).WriteValueIntoEvent(0.5f, eventPtr);
    InputSystem.QueueEvent(eventPtr);
}
```

Alternatively, you can send events for individual Controls.

```
// Send event to update leftStick on the gamepad.
InputSystem.QueueDeltaStateEvent(Gamepad.current.leftStick,
    new Vector2(0.123f, 0.234f);
```

Note that delta state events only work for Controls that are both byte-aligned and a multiple of 8 bits in size in memory. You can't send a delta state event for a button Control that is stored as a single bit, for example.

### Capturing Events

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
