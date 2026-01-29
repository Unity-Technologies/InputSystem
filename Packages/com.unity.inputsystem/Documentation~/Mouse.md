---
uid: input-system-mouse
---
# Mouse support

The Input System represents mouse input with the [`Mouse`](xref:UnityEngine.InputSystem.Mouse) Device layout that the [`Mouse`](xref:UnityEngine.InputSystem.Mouse) class implements. Mice are based on the [`Pointer`](xref:input-system-pointers) layout.

To query the last used or last added mouse, use [`Mouse.current`](xref:UnityEngine.InputSystem.Mouse.current).

```
    var mouse = Mouse.current;
```

> [!NOTE]
> The Input System does not currently support:
>* Input from multiple mice at the platform level.
>* Identifying the current display a mouse is on.

## Controls

In addition to the [Controls inherited from `Pointer`](xref:input-system-pointers#controls), Mouse devices implement the following Controls:

|Control|Type|Description|
|-------|----|-----------|
|[`leftButton`](xref:UnityEngine.InputSystem.Mouse.leftButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The left mouse button. Same as the inherited [`Pointer.press`](xref:UnityEngine.InputSystem.Pointer.press).|
|[`rightButton`](xref:UnityEngine.InputSystem.Mouse.rightButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The right mouse button.|
|[`middleButton`](xref:UnityEngine.InputSystem.Mouse.middleButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|The middle mouse button.|
|[`forwardButton`](xref:UnityEngine.InputSystem.Mouse.forwardButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|Used for other mouse buttons where applicable.|
|[`backButton`](xref:UnityEngine.InputSystem.Mouse.backButton)|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|Used for other mouse buttons where applicable.|
|[`clickCount`](xref:UnityEngine.InputSystem.Mouse.clickCount)|[`IntegerControl`](xref:UnityEngine.InputSystem.Controls.IntegerControl)|A Control which lets you read the number of consecutive clicks the last mouse click belonged to, as reported by the OS. Use this to distinguish double- or multi-clicks.|
|[`scroll`](xref:UnityEngine.InputSystem.Mouse.scroll)|[`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|The input from the mouse scrolling control expressed as a delta in pixels since the last frame. Can come from a physical scroll wheel, or from touchpad gestures.|

## Cursor warping

On desktop platforms (Windows, Mac, Linux, and UWP), you can move the mouse cursor via code. Note that this moves the system's actual mouse cursor, not just Unity's internally-stored mouse position. This means that the user sees the cursor jumping to a different position, which is generally considered to be bad UX practice. It's advisable to only do this if the cursor is hidden (see the [`Cursor` API documentation](https://docs.unity3d.com/ScriptReference/Cursor.html) for more information).

To move the cursor to a different position, use [`Mouse.WarpCursorPosition`](xref:UnityEngine.InputSystem.Mouse.WarpCursorPosition(UnityEngine.Vector2)). The coordinates are expressed as Unity screen coordinates, just like [`Mouse.position`](xref:UnityEngine.InputSystem.Pointer.position).

```
    Mouse.current.WarpCursorPosition(new Vector2(123, 234));
```

> [!NOTE]
> If the cursor is locked, warping the mouse position is only temporary and Unity resets the cursor to the center of the window every frame.
