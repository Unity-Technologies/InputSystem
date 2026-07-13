---
uid: input-system-layout-builder
---

# Add a layout using Layout Builder

Finally, the Input System can also build layouts on the fly in code. This is useful for Device interfaces such as [HID](hid-specification.md) that supply descriptive information for each Device.

To build layouts dynamically in code, you can use the [`InputControlLayout.Builder`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout.Builder) API.

Here's the same layout from the previous examples constructed programmatically:

```
var builder = new InputControlLayout.Builder()
    .WithName("MyDevice")
    .WithFormat("MDEV");

builder.AddControl("button1")
    .WithLayout("Button")
    .WithByteOffset(0)
    .WithBitOffset(0);

builder.AddControl("button2")
    .WithLayout("Button")
    .WithByteOffset(0)
    .WithBitOffset(1);

builder.AddControl("dpad")
    .WithLayout("Dpad")
    .WithByteOffset(0)
    .WithBitOffset(2)
    .WithSizeInBits(4);

builder.AddControl("dpad/up")
    .WithByteOffset(-1)
    .WithBitOffset(2);

builder.AddControl("dpad/down")
    .WithByteOffset(-1)
    .WithBitOffset(3);

builder.AddControl("dpad/left")
    .WithByteOffset(-1)
    .WithBitOffset(4);

builder.AddControl("dpad/right")
    .WithByteOffset(-1)
    .WithBitOffset(5);

builder.AddControl("stick")
    .WithLayout("Stick")
    .WithByteOffset(4)
    .WithFormat("VEC2");

builder.AddControl("trigger")
    .WithLayout("Axis")
    .WithByteOffset(12)
    .WithFormat("BYTE");

var layout = builder.Build();
```
