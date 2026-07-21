---
uid: input-system-default-value-properties
---

# Default value properties

|Property|Description|
|----|-----------|
|Default Deadzone Min|The default minimum value for [Stick Deadzone](built-in-processors.md) or [Axis Deadzone](built-in-processors.md) processors when no `min` value is explicitly set on the processor.|
|Default Deadzone Max|The default maximum value for [Stick Deadzone](built-in-processors.md) or [Axis Deadzone](built-in-processors.md) processors when no `max` value is explicitly set on the processor.|
|Default Button Press Point|The default [press point](xref:UnityEngine.InputSystem.Controls.ButtonControl) for [Button Controls](xref:UnityEngine.InputSystem.Controls.ButtonControl), and for various [Interactions](Interactions.md). For button Controls which have analog physics inputs (such as triggers on a gamepad), this configures how far they need to be held down for the system to consider them pressed.|
|Default Tap Time|Default duration for [Tap](built-in-interactions.md#tap) and [MultiTap](built-in-interactions.md#multitap) Interactions. Also used by by touchscreen Devices to distinguish taps from to new touches.|
|Default Slow Tap Time|Default duration for [SlowTap](built-in-interactions.md#tap) Interactions.|
|Default Hold Time|Default duration for [Hold](built-in-interactions.md#hold) Interactions.|
|Tap Radius|Maximum distance between two finger taps on a touchscreen Device for the system to consider this a tap of the same touch (as opposed to a new touch).|
|Multi Tap Delay Time|Default delay between taps for [MultiTap](built-in-interactions.md#multitap) Interactions. Also used by touchscreen Devices to count multi-taps (Refer to [`TouchControl.tapCount`](xref:UnityEngine.InputSystem.Controls.TouchControl)).|
