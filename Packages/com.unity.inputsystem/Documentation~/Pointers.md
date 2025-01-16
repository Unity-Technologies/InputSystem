---
uid: input-system-pointers
---
# Pointer device overview

[`Pointer`](xref:UnityEngine.InputSystem.Pointer) devices are defined as an [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) that tracks positions on a 2D surface. The Input System package supports three types of pointers:

* [Touch](Touch.md)
* [Mouse](Mouse.md)
* [Pen](Pen.md)

Each `Pointer` type implements a common set of control properties. For detailed descriptions of these properties, refer to the [`Pointer` API documentation](xref:UnityEngine.InputSystem.Pointer).

## Window space

The coordinates within Player code are in the coordinate space of the Player window.

In Unity Editor code, the coordinates are in the coordinate space of the current [`EditorWindow`](xref:UnityEditor.EditorWindow). For example, if you query [`Pointer.current.position`](xref:UnityEngine.InputSystem.Pointer.position) in [`UnityEditor.EditorWindow.OnGUI`](xref:UnityEditor.EditorWindow.OnGUI),  the returned 2D vector is in the coordinate space of your local GUI (same as [`UnityEngine.Event.mousePosition`](xref:UnityEngine.Event-mousePosition)).
