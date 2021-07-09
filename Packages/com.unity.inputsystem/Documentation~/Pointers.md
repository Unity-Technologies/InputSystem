# Pointers

[`Pointer`](../api/UnityEngine.InputSystem.Pointer.html) Devices are defined as [`InputDevices`](../api/UnityEngine.InputSystem.InputDevice.html) that track positions on a 2D surface. The Input System supports three types of pointers:

* [Touch](Touch.md)
* [Mouse](Mouse.md)
* [Pen](Pen.md)

## Controls

Each of these types implements a common set of Controls. For a more detailed descriptions of these Controls, refer to their [scripting reference](../api/UnityEngine.InputSystem.Pointer.html).

|Control|Type|Description|
|-------|----|-----------|
|[`position`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_position)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|The current pointer coordinates in window space.|
|[`delta`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_delta)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|Provides motion delta in pixels accumulated (summed) over the duration of the current frame/update. Resets to `(0,0)` each frame.<br><br>Note that the resolution of deltas depends on the specific hardware and/or platform.|
|[`press`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_press)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Whether the pointer or its primary button is pressed down.|
|[`pressure`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_pressure)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)| The pressure applied with the pointer while in contact with the pointer surface. This value is normalized. This is only relevant for pressure-sensitive devices, such as tablets and some touch screens.|
|[`radius`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_radius)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|The size of the area where the finger touches the surface. This is only relevant for touch input.|

## Window space

The coordinates within Player code are in the coordinate space of the Player window.

Within Editor code, the coordinates are in the coordinate space of the current [`EditorWindow`](https://docs.unity3d.com/ScriptReference/EditorWindow.html). If you query [`Pointer.current.position`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_position) in [`UnityEditor.EditorWindow.OnGUI`](https://docs.unity3d.com/ScriptReference/EditorWindow.OnGUI.html), for example, the returned 2D vector will be in the coordinate space of your local GUI (same as [`UnityEngine.Event.mousePosition`](https://docs.unity3d.com/ScriptReference/Event-mousePosition.html)).
