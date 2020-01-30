# Mouse support

The Input System represents mouse input with the [`Mouse`](../api/UnityEngine.InputSystem.Mouse.html) Device layout that the [`Mouse`](../api/UnityEngine.InputSystem.Mouse.html) class implements. Mice are based on the [`Pointer`](Pointers.md) layout.

To query the last used or last added mouse, use [`Mouse.current`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_current).

```
    var mouse = Mouse.current;
```

>__Note__: The Input System does not currently support:
>* Input from multiple mice at the platform level.
>* Identifying the current display a mouse is on.

## Controls

In addition to the [Controls inherited from `Pointer`](Pointers.md#controls), Mouse devices implement the following Controls:

|Control|Type|Description|
|-------|----|-----------|
|[`leftButton`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_leftButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The left mouse button. Same as the inherited [`Pointer.press`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_press).|
|[`rightButton`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_rightButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The right mouse button.|
|[`middleButton`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_middleButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|The middle mouse button.|
|[`forwardButton`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_forwardButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Used for other mouse buttons where applicable.|
|[`backButton`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_backButton)|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|Used for other mouse buttons where applicable.|
|[`clickCount`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_clickCount)|[`IntegerControl`](../api/UnityEngine.InputSystem.Controls.IntegerControl.html)|A Control which lets you read the number of consecutive clicks the last mouse click belonged to, as reported by the OS. Use this to distinguish double- or multi-clicks.|
|[`scroll`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_scroll)|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|The input from the mouse scrolling control expressed as a delta in pixels since the last frame. Can come from a physical scroll wheel, or from touchpad gestures.|

## Cursor warping

On desktop platforms (Windows, Mac, Linux, and UWP), you can move the mouse cursor via code. Note that this moves the system's actual mouse cursor, not just Unity's internally-stored mouse position. This means that the user sees the cursor jumping to a different position, which is generally considered to be bad UX practice. It's advisable to only do this if the cursor is hidden (see the [`Cursor` API documentation](https://docs.unity3d.com/ScriptReference/Cursor.html) for more information).

To move the cursor to a different position, use [`Mouse.WarpCursorPosition`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_WarpCursorPosition_Vector2_). The coordinates are expressed as Unity screen coordinates, just like [`Mouse.position`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_position).

```
    Mouse.current.WarpCursorPosition(new Vector2(123, 234));
```

>__Note__: If the cursor is locked, warping the mouse position is only temporary and Unity resets the cursor to the center of the window every frame.
