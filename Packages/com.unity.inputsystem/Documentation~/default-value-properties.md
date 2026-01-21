---
uid: input-system-default-value-properties
---

# Default value properties 

|Property|Description|
|----|-----------|
|Default Deadzone Min|The default minimum value for [Stick Deadzone](Processors.md#stick-deadzone) or [Axis Deadzone](Processors.md#axis-deadzone) processors when no `min` value is explicitly set on the processor.|
|Default Deadzone Max|The default maximum value for [Stick Deadzone](Processors.md#stick-deadzone) or [Axis Deadzone](Processors.md#axis-deadzone) processors when no `max` value is explicitly set on the processor.|
|Default Button Press Point|The default [press point](../api/UnityEngine.InputSystem.Controls.ButtonControl.html#UnityEngine_InputSystem_Controls_ButtonControl_pressPointOrDefault) for [Button Controls](../api/UnityEngine.InputSystem.Controls.ButtonControl.html), and for various [Interactions](Interactions.md). For button Controls which have analog physics inputs (such as triggers on a gamepad), this configures how far they need to be held down for the system to consider them pressed.|
|Default Tap Time|Default duration for [Tap](Interactions.md#tap) and [MultiTap](Interactions.md#multitap) Interactions. Also used by by touchscreen Devices to distinguish taps from to new touches.|
|Default Slow Tap Time|Default duration for [SlowTap](Interactions.md#tap) Interactions.|
|Default Hold Time|Default duration for [Hold](Interactions.md#hold) Interactions.|
|Tap Radius|Maximum distance between two finger taps on a touchscreen Device for the system to consider this a tap of the same touch (as opposed to a new touch).|
|Multi Tap Delay Time|Default delay between taps for [MultiTap](Interactions.md#multitap) Interactions. Also used by touchscreen Devices to count multi-taps (See [`TouchControl.tapCount`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_tapCount)).|
