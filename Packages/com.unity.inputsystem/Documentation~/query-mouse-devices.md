# Query and control mouse devices in code

You can use the Mouse class to control how mouse devices work with your application

## Query the last mouse

To query the last used or last added mouse, use [Mouse.current](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_current):

```c

   var mouse = Mouse.current;
```

## Move the mouse cursor

On desktop platforms (Windows, macOS, Linux, and UWP), you can move the mouse cursor via code with the [WarpCursorPosition](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_WarpCursorPosition_UnityEngine_Vector2_) method. 

[WarpCursorPosition](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_WarpCursorPosition_UnityEngine_Vector2_) moves the system's actual mouse cursor, not just Unity's internally-stored mouse position. This means that the user sees the cursor jumping to a different position, which is generally considered to be bad UX practice. It's best practice to only do this if the cursor is hidden. For information on how to hide a cursor, refer to the [Cursor API documentation](https://docs.unity3d.com/ScriptReference/Cursor.html).

To move the cursor to a different position, use [Mouse.WarpCursorPosition](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_WarpCursorPosition_UnityEngine_Vector2_). The coordinates are expressed as Unity screen coordinates, just like [Mouse.position](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_position).

```c

   Mouse.current.WarpCursorPosition(new Vector2(123, 234));

```

Note: If the cursor is locked, warping the mouse position is only temporary and Unity resets the cursor to the center of the window every frame.