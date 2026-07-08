---
uid: input-system-precompiled-layouts
---

# Precompiled layouts

Building a device at runtime from an [`InputControlLayout`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout) is a slow process. The layout instance itself has to be built (which might involve reflection) and then interpreted in order to put the final [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) instance together. This process usually involves the loading of multiple [`InputControlLayout`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout) instances, each of which might be the result of merging multiple layouts together (if the layout involves [inheritance](layout-inheritance.md) or [overrides](override-layout-definitions.md)).

You can speed up this process up by "baking" the final form of a layout into a "precompiled layout". A precompiled layout is generated C# code that, when run, will build the corresponding device without relying on loading and interpreting an [`InputControlLayout`](xref:UnityEngine.InputSystem.Layouts.InputControlLayout). Aside from running faster, this will also create far less garbage and will not involve C# reflection (which generally causes runtime overhead by inflating the number of objects internally kept by the C# runtime).

> [!NOTE]
> Precompiled layouts must be device layouts. It is not possible to precompile the layout for an [`InputControl`](xref:UnityEngine.InputSystem.InputControl).
