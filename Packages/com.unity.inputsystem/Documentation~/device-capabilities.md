---
uid: input-system-device-capabilities
---

# Device capabilities 

Part of the Device description can be a [`capabilities`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_capabilities) string in JSON format. This string describes characteristics that help the Input System to interpret the data from a Device, and map it to Control representations. Not all Device interfaces report Device capabilities. Examples of interface-specific Device capabilities are [HID descriptors](hid-specification-introduction.md). WebGL, Android, and Linux use similar mechanisms to report available Controls on connected gamepads.