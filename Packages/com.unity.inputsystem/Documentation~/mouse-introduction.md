---
uid: input-system-mouse-intro
---

# Mouse devices introduction

The Input System represents mouse input with the [`MouseState`](xref:UnityEngine.InputSystem.LowLevel.MouseState) [device layout](layouts.md) that the [`Mouse`](xref:UnityEngine.InputSystem.Mouse) class implements.

> [!IMPORTANT]
> The Input System doesn't support input from multiple mice at the platform level, or identifying the current display a mouse is on.

The `Mouse` class is based on the [pointer layout](devices-pointers.md) so inherits its controls. It also implements additional controls outlined in the [Mouse API documentation](xref:UnityEngine.InputSystem.Mouse).

For a list of platforms that support mice, refer to [Supported devices reference](supported-devices-reference.md).
