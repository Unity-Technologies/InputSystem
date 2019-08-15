# Mouse Support

Mice are represented by the [`Mouse`](../api/UnityEngine.InputSystem.Mouse.html) device layout which is implemented by the [`Mouse`](../api/UnityEngine.InputSystem.Mouse.html) class. Mice are based on the [`Pointer`](Pointers.md) layout.

The last used or last added mouse can be queried with [`Mouse.current`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_current).

```
    var mouse = Mouse.current;
```

>NOTES:
>* We do not yet support separating input from multiple mice at the platform level.
>* We do not yet support identifying the current display a mouse is on.

## Controls

Additional to the [controls inherited from `Pointer`](Pointers.md#controls), Mouse devices implement the following controls:

|Control|Type|Description|
|-------|----|-----------|
|[`leftButton`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_leftButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The left mouse button. Same as the inherited [`Pointer.press`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_press).|
|[`rightButton`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_rightButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The right mouse button.|
|[`middleButton`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_middleButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The middle mouse button.|
|[`forwardButton`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_forwardButton)
|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Used for further mouse buttons where applicable.|
|[`backButton`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_backButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Used for further mouse buttons where applicable.|
|[`clickCount`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_clickCount)|[`IntegerControl`](../api/UnityEngine.InputSystem.Controls.IntegerControl.html)|A control which lets you read the number of consecutive clicks the last mouse click belonged to, as reported by the OS. This can be used to distinguish double- or multi-clicks.|
|[`scroll`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_scroll)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|The input from the mouse scrolling control expressed as a delta in pixels since the last frame. Can come from a physics scroll wheel, or from gestures on a touch pad.|


## Cursor Warping

On desktop platforms (Windows, Mac, Linux, and UWP), the mouse cursor can be moved from code. Note that this will move the system's actual mouse cursor, not just Unity's internally stored mouse position. This means that the user will visibly see the cursor jumping to a different position. This is usually considered a bad UI practice, and it is advisable to only do this when the cursor is hidden (see the [`Cursor` API](https://docs.unity3d.com/ScriptReference/Cursor.html)).

To move the cursor to a different position, use [`Mouse.WarpCursorPosition`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_WarpCursorPosition_Vector2_). The coordinates are in Unity screen coordinates just like [`Mouse.position`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_position).

```
    Mouse.current.WarpCursorPosition(new Vector2(123, 234));
```

>NOTE: If the cursor is locked, warping the mouse position will only temporarily take effect. Unity will reset the cursor to the center of the window every frame.
