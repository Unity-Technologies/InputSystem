---
uid: input-system-pointer-devices-intro
---

# Pointer devices introduction

A pointer device tracks positions on a 2D surface. The Input System package supports the following types of pointer device:

* [Touch](devices-touch.md)
* [Mouse](devices-mouse.md)
* [Pen](devices-pen.md)

Pointer devices are represented by the [`Pointer` class](xref:UnityEngine.InputSystem.Pointer) which inherits from the [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) class. They have a shared set of behaviors and controls, which are explained in the [`Pointer` API documentation](xref:UnityEngine.InputSystem.Pointer).

For a list of platforms that support pointers, refer to [Supported devices reference](supported-devices-reference.md).

## Pointer window space

The coordinates of pointers depend on whether you're working in a player or in the Unity Editor:

* In player code, the coordinates are in the coordinate space of the Player window.
* In Editor code, the coordinates are in the coordinate space of the current [`EditorWindow`](xref:UnityEditor.EditorWindow). For example, if you query [`Pointer.current.position`](xref:UnityEngine.InputSystem.Pointer.position) in [`UnityEditor.EditorWindow.OnGUI`](https://docs.unity3d.com/ScriptReference/EditorWindow.OnGUI.html), the returned 2D vector is in the coordinate space of your local GUI (same as [`UnityEngine.Event.mousePosition`](https://docs.unity3d.com/ScriptReference/Event-mousePosition.html)).
