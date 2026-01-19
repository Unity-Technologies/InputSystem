---
uid: input-system-device-capabilities
---

# Device capabilities 

Aside from a number of standardized fields, such as `product` and `manufacturer`, a Device description can contain a [`capabilities`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_capabilities) string in JSON format. This string describes characteristics which help the Input System to interpret the data from a Device, and map it to Control representations. Not all Device interfaces report Device capabilities. Examples of interface-specific Device capabilities are [HID descriptors](hid-specification-introduction.md). WebGL, Android, and Linux use similar mechanisms to report available Controls on connected gamepads.