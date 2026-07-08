---
uid: input-system-query-mouse-devices
---

# Query and control mouse devices in code

You can use the `Mouse` class to control how mouse devices work with your application.

## Query the last mouse

To query the last used or last added mouse, use [`Mouse.current`](xref:UnityEngine.InputSystem.Mouse.current):

```c#

   var mouse = Mouse.current;

```

## Move the mouse cursor

On desktop platforms (Windows, macOS, Linux, and Universal Windows Platform), you can move the mouse cursor with code with the [`WarpCursorPosition`](xref:UnityEngine.InputSystem.Mouse.WarpCursorPosition(UnityEngine.Vector2)) method.

`WarpCursorPosition` moves the system's actual mouse cursor, not just Unity's internally stored mouse position. This means that the user sees the cursor jumping to a different position, which is generally considered to be bad practice. It's best practice to only do this if the cursor is hidden. For information on how to hide a cursor, refer to the [`Cursor` API documentation](xref:UnityEngine.Cursor).

To move the cursor to a different position, use [`Mouse.WarpCursorPosition`](xref:UnityEngine.InputSystem.Mouse.WarpCursorPosition(UnityEngine.Vector2)). The coordinates are expressed as Unity screen coordinates, just like [`Mouse.position`](xref:UnityEngine.InputSystem.Pointer.position).

```c#

   Mouse.current.WarpCursorPosition(new Vector2(123, 234));

```

> [!NOTE]
> If the cursor is locked, warping the mouse position is only temporary and Unity resets the cursor to the center of the window every frame.
