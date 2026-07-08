---
uid: input-system-gamepads-intro
---

# Gamepads introduction

In the Input System, a **gamepad** is a controller that matches the [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) layout: two thumbsticks, a D-pad, four face buttons, shoulder and trigger buttons, and usually two center buttons. Use [Gamepads](devices-gamepads.md) to browse related setup and reference topics in this manual.

A gamepad can expose extra controls (for example a gyroscope). Every recognized gamepad still implements at least the controls defined on the [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) type.

The Input System also provides device-specific APIs for:

* PlayStation DualShock and DualSense hardware in the [`DualShock`](xref:UnityEngine.InputSystem.DualShock) namespace.
* Xbox controllers that use XInput in the [`XInput`](xref:UnityEngine.InputSystem.XInput) namespace.
* Nintendo Switch Pro controllers in the [`Switch`](xref:UnityEngine.InputSystem) namespace.

For platform availability, refer to [Supported devices reference](supported-devices-reference.md).

When the Input System recognizes a device as a [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad), control placement stays consistent across platforms and hardware. For example, a PlayStation 4 DualShock maps to the same logical layout whether it is connected on Windows or macOS, and the south face button is always the bottom button in the diamond.

> [!IMPORTANT]
> Generic [HID](hid-specification.md) gamepads are not surfaced as [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) instances; they appear as generic [joysticks](devices-joysticks.md). The Input System cannot rely on HID descriptors alone to map every axis and button correctly. Only HID devices that ship with explicit [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) support (for example the PlayStation 4 controller) use the gamepad layout. To add comparable support for another HID controller, refer to [Create a custom device layout](hid-create-custom-layout.md).
