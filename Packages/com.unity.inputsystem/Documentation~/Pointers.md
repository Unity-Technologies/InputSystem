---
uid: input-system-pointers
---
# Pointers

[`Pointer`](xref:UnityEngine.InputSystem.Pointer) Devices are defined as [`InputDevices`](xref:UnityEngine.InputSystem.InputDevice) that track positions on a 2D surface. The Input System supports three types of pointers:

* [Touch](xref:input-system-touch)
* [Mouse](xref:input-system-mouse)
* [Pen](xref:input-system-pen)

## Controls

Each of these types implements a common set of Controls. For a more detailed descriptions of these Controls, refer to their [scripting reference](xref:UnityEngine.InputSystem.Pointer).

|Control|Type|Description|
|-------|----|-----------|
|[`position`](xref:UnityEngine.InputSystem.Pointer.position)|[`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|The current pointer coordinates in window space.|
|[`delta`](xref:UnityEngine.InputSystem.Pointer.delta)|[`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|Provides motion delta in pixels accumulated (summed) over the duration of the current frame/update. Resets to `(0,0)` each frame.<br><br>Note that the resolution of deltas depends on the specific hardware and/or platform.|
|[`press`](xref:UnityEngine.InputSystem.Pointer.press)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|Whether the pointer or its primary button is pressed down.|
|[`pressure`](xref:UnityEngine.InputSystem.Pointer.pressure)|[`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl)| The pressure applied with the pointer while in contact with the pointer surface. This value is normalized. This is only relevant for pressure-sensitive devices, such as tablets and some touch screens.|
|[`radius`](xref:UnityEngine.InputSystem.Pointer.radius)|[`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|The size of the area where the finger touches the surface. This is only relevant for touch input.|

## Window space

The coordinates within Player code are in the coordinate space of the Player window.

Within Editor code, the coordinates are in the coordinate space of the current [`EditorWindow`](https://docs.unity3d.com/ScriptReference/EditorWindow.html). If you query [`Pointer.current.position`](xref:UnityEngine.InputSystem.Pointer.position) in [`UnityEditor.EditorWindow.OnGUI`](https://docs.unity3d.com/ScriptReference/EditorWindow.OnGUI.html), for example, the returned 2D vector will be in the coordinate space of your local GUI (same as [`UnityEngine.Event.mousePosition`](https://docs.unity3d.com/ScriptReference/Event-mousePosition.html)).
