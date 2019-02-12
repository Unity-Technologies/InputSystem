    ////WIP

# Mouse Support

Mice are represented by the `Mouse` device layout which is implemented by the `Mouse` class. Mice are based on the `Pointer` layout.

The last used or last added mouse can be queried with `Mouse.current`.

```
    var mouse = Mouse.current;
```

## Controls

|Control|Type|Description|
|-------|----|-----------|
|`leftButton`|`Button`|The left mouse button.|
|`rightButton`|`Button`|The right mouse button.|
|`middleButton`|`Button`|The middle mouse button.|
|`forwardButton`|`Button`||
|`backButton`|`Button`||
|`position`|`Vector2`||
|`delta`|`Vector2`||
|`scroll`|`Vector2`||
|`button`|`Button`|Same as `leftButton`. Inherited from `Pointer`.|
|`pressure`|`Vector2`||
|`tilt`|`Vector2`|Inherited from `Pointer`. Always `(0,0)` for a mouse.|
|`twist`|`Axis`||
|`phase`|`Phase`||

## Cursor Warping

On desktop platforms (Windows, Mac, Linux, and UWP), the mouse cursor can be moved from code. Note that this will move the system's actual mouse cursor, not just Unity's internally stored mouse position. This means that the user will visibly see the cursor jumping to a different position.

To move the cursor to a different position, use `Mouse.WarpCursorPosition`. The coordinates are in Unity screen coordinates just like `Mouse.position`.

```
    Mouse.current.WarpCursorPosition(new Vector2(123, 234));
```

>NOTE: If the cursor is locked, warping the mouse position will only temporarily take effect. Unity will reset the cursor to the center of the window every frame.

## Multi-Mouse Support

>We do not yet support separating input from multiple mice at the platform level.

## Multi-Display Support

>We do not yet support identifying the current display a mouse is on.
