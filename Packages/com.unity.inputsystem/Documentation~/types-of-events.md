---
uid: input-system-types-of-events
---

# Types of events

There are three types of events: state, device, and text.

## State events

A state event contains the input state for a Device. The Input System uses these events to feed new input to Devices.

There are two types of state events:

* [`StateEvent`](xref:UnityEngine.InputSystem.LowLevel.StateEvent) (`'STAT'`)
* [`DeltaStateEvent`](xref:UnityEngine.InputSystem.LowLevel.StateEvent) (`'DLTA'`)

[`StateEvent`](xref:UnityEngine.InputSystem.LowLevel.StateEvent) contains a full snapshot of the entire state of a Device in the format specific to that Device. The [`stateFormat`](xref:UnityEngine.InputSystem.LowLevel.StateEvent) field identifies the type of the data in the event. You can access the raw data using the [`state`](xref:UnityEngine.InputSystem.LowLevel.StateEvent) pointer and [`stateSizeInBytes`](xref:UnityEngine.InputSystem.LowLevel.StateEvent).

A [`DeltaStateEvent`](xref:UnityEngine.InputSystem.LowLevel.DeltaStateEvent) is like a [`StateEvent`](xref:UnityEngine.InputSystem.LowLevel.StateEvent), but only contains a partial snapshot of the state of a Device. The Input System usually sends this for Devices that require a large state record, to reduce the amount of memory it needs to update if only some of the Controls change their state. To access the raw data, you can use the [`deltaState`](xref:UnityEngine.InputSystem.LowLevel.DeltaStateEvent) pointer and [`deltaStateSizeInBytes`](xref:UnityEngine.InputSystem.LowLevel.DeltaStateEvent). The Input System should apply the data to the Device's state at the offset defined by [`stateOffset`](xref:UnityEngine.InputSystem.LowLevel.DeltaStateEvent).

## Device events

Device events indicate a change that is relevant to a Device as a whole. If you're interested in these events, it is usually more convenient to subscribe to the higher-level [`InputSystem.onDeviceChange`](xref:UnityEngine.InputSystem.InputSystem) event rather then processing [`InputEvents`](xref:UnityEngine.InputSystem.LowLevel.InputEvent) yourself.

There are three types of Device events:

* [`DeviceRemoveEvent`](xref:UnityEngine.InputSystem.LowLevel.DeviceRemoveEvent) (`'DREM'`)
* [`DeviceConfigurationEvent`](xref:UnityEngine.InputSystem.LowLevel.DeviceConfigurationEvent) (`'DCFG'`)
* [`DeviceResetEvent`](xref:UnityEngine.InputSystem.LowLevel.DeviceResetEvent) (`'DRST'`)

`DeviceRemovedEvent` indicates that a Device has been removed or disconnected. To query the device that has been removed, you can use the common [`deviceId`](xref:UnityEngine.InputSystem.LowLevel.InputEvent) field. This event doesn't have any additional data.

`DeviceConfigurationEvent` indicates that the configuration of a Device has changed. The meaning of this is Device-specific. This might signal, for example, that the layout used by the keyboard has changed or that, on a console, a gamepad has changed which player ID(s) it is assigned to. You can query the changed device from the common [`deviceId`](xref:UnityEngine.InputSystem.LowLevel.InputEvent) field. This event doesn't have any additional data.

`DeviceResetEvent` indicates that a device should get reset. This will trigger [`InputSystem.ResetDevice`](xref:UnityEngine.InputSystem.InputSystem) to be called on the Device.

## Text events

[Keyboard](devices-keyboard.md) devices send these events to handle text input. If you're interested in these events, it's usually more convenient to subscribe to the higher-level [callbacks on the Keyboard class](read-keyboard-text-input.md) rather than processing [`InputEvents`](xref:UnityEngine.InputSystem.LowLevel.InputEvent) yourself.

There are two types of text events:

* [`TextEvent`](xref:UnityEngine.InputSystem.LowLevel.TextEvent) (`'TEXT'`)
* [`IMECompositionEvent`](xref:UnityEngine.InputSystem.LowLevel.IMECompositionEvent) (`'IMES'`)
