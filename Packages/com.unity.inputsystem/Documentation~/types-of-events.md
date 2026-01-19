---
uid: input-system-types-of-events
---

# Types of events 

There are three types of events: state, device, and text.

## State events

A state event contains the input state for a Device. The Input System uses these events to feed new input to Devices.

There are two types of state events:

* [`StateEvent`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html) (`'STAT'`)
* [`DeltaStateEvent`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html) (`'DLTA'`)

[`StateEvent`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html) contains a full snapshot of the entire state of a Device in the format specific to that Device. The [`stateFormat`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html#UnityEngine_InputSystem_LowLevel_StateEvent_stateFormat) field identifies the type of the data in the event. You can access the raw data using the [`state`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html#UnityEngine_InputSystem_LowLevel_StateEvent_state) pointer and [`stateSizeInBytes`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html#UnityEngine_InputSystem_LowLevel_StateEvent_stateSizeInBytes).

A [`DeltaStateEvent`](../api/UnityEngine.InputSystem.LowLevel.DeltaStateEvent.html) is like a [`StateEvent`](../api/UnityEngine.InputSystem.LowLevel.StateEvent.html), but only contains a partial snapshot of the state of a Device. The Input System usually sends this for Devices that require a large state record, to reduce the amount of memory it needs to update if only some of the Controls change their state. To access the raw data, you can use the [`deltaState`](../api/UnityEngine.InputSystem.LowLevel.DeltaStateEvent.html#UnityEngine_InputSystem_LowLevel_DeltaStateEvent_deltaState) pointer and [`deltaStateSizeInBytes`](../api/UnityEngine.InputSystem.LowLevel.DeltaStateEvent.html#UnityEngine_InputSystem_LowLevel_DeltaStateEvent_deltaStateSizeInBytes). The Input System should apply the data to the Device's state at the offset defined by [`stateOffset`](../api/UnityEngine.InputSystem.LowLevel.DeltaStateEvent.html#UnityEngine_InputSystem_LowLevel_DeltaStateEvent_stateOffset).

## Device events

Device events indicate a change that is relevant to a Device as a whole. If you're interested in these events, it is usually more convenient to subscribe to the higher-level [`InputSystem.onDeviceChange`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceChange) event rather then processing [`InputEvents`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html) yourself.

There are three types of Device events:

* [`DeviceRemoveEvent`](../api/UnityEngine.InputSystem.LowLevel.DeviceRemoveEvent.html) (`'DREM'`)
* [`DeviceConfigurationEvent`](../api/UnityEngine.InputSystem.LowLevel.DeviceConfigurationEvent.html) (`'DCFG'`)
* [`DeviceResetEvent`](../api/UnityEngine.InputSystem.LowLevel.DeviceResetEvent.html) (`'DRST'`)

`DeviceRemovedEvent` indicates that a Device has been removed or disconnected. To query the device that has been removed, you can use the common [`deviceId`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_deviceId) field. This event doesn't have any additional data.

`DeviceConfigurationEvent` indicates that the configuration of a Device has changed. The meaning of this is Device-specific. This might signal, for example, that the layout used by the keyboard has changed or that, on a console, a gamepad has changed which player ID(s) it is assigned to. You can query the changed device from the common [`deviceId`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_deviceId) field. This event doesn't have any additional data.

`DeviceResetEvent` indicates that a device should get reset. This will trigger [`InputSystem.ResetDevice`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_ResetDevice_UnityEngine_InputSystem_InputDevice_System_Boolean_) to be called on the Device.

## Text events

[Keyboard](devices-keyboard.md) devices send these events to handle text input. If you're interested in these events, it's usually more convenient to subscribe to the higher-level [callbacks on the Keyboard class](read-keyboard-text-input.md) rather than processing [`InputEvents`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html) yourself.

There are two types of text events:

* [`TextEvent`](../api/UnityEngine.InputSystem.LowLevel.TextEvent.html) (`'TEXT'`)
* [`IMECompositionEvent`](../api/UnityEngine.InputSystem.LowLevel.IMECompositionEvent.html) (`'IMES'`)
