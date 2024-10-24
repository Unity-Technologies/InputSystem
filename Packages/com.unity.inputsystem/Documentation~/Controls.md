---
uid: input-system-controls
---
# Controls

- [Control paths](#control-paths)
- [Control state](#control-state)
- [Control actuation](#control-actuation)
- [Noisy Controls](#noisy-controls)
- [Synthetic Controls](#synthetic-controls)
- [Performance Optimization](#performance-optimization)
  - [Avoiding defensive copies](#avoiding-defensive-copies)
  - [Control Value Caching](#control-value-caching)
  - [Optimized control read value](#optimized-control-read-value)

An Input Control represents a source of values. These values can be of any structured or primitive type. The only requirement is that the type is [blittable](https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types).

>__Note__: Controls are for input only. Output and configuration items on Input Devices are not represented as Controls.

Each Control is identified by a name ([`InputControl.name`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_name)) and can optionally have a display name ([`InputControl.displayName`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_displayName)) that differs from the Control name. For example, the right-hand face button closest to the touchpad on a PlayStation DualShock 4 controller has the control name "buttonWest" and the display name "Square".

Additionally, a Control might have one or more aliases which provide alternative names for the Control. You can access the aliases for a specific Control through its [`InputControl.aliases`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_aliases) property.

Finally, a Control might also have a short display name which can be accessed through the [`InputControl.shortDisplayName`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_shortDisplayName) property. For example, the short display name for the left mouse button is "LMB".




