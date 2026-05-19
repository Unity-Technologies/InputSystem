---
uid: input-system-input-events
---

# Input events 

The Input System is event-driven. All input is delivered as events, and you can generate custom input by injecting events. You can also observe all source input by listening in on the events flowing through the system.

>__Note__: Events are an advanced, mostly internal feature of the Input System. Knowledge of the event system is mostly useful if you want to support custom Devices, or change the behavior of existing Devices.

Input events are a low-level mechanism. Usually, you don't need to deal with events if all you want to do is receive input for your app. Events are stored in unmanaged memory buffers and not converted to C# heap objects. The Input System provides wrapper APIs, but unsafe code is required for more involved event manipulations.

Note that there are no routing mechanism. The runtime delivers events straight to the Input System, which then incorporates them directly into the Device state.

Input events are represented by the [`InputEvent`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html) struct. Each event has a set of common properties:

|Property|Description|
|--------|-----------|
|[`type`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_type)|[`FourCC`](../api/UnityEngine.InputSystem.Utilities.FourCC.html) code that indicates what type of event it is.|
|[`eventId`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_eventId)|Unique numeric ID of the event.|
|[`time`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_time)|Timestamp of when the event was generated. This is on the same timeline as [`Time.realtimeSinceStartup`](https://docs.unity3d.com/ScriptReference/Time-realtimeSinceStartup.html).|
|[`deviceId`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_deviceId)|ID of the Device that the event targets.|
|[`sizeInBytes`](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_sizeInBytes)|Total size of the event in bytes.|

You can observe the events received for a specific input device in the [input debugger](debug-device.md).
