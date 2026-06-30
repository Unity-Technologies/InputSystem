---
uid: input-system-split-screen-multiplayer
---

# Set up split-screen local multiplayer

If you enable the [`Split-Screen`](xref:UnityEngine.InputSystem.PlayerInputManager) option, the [`PlayerInputManager`](xref:UnityEngine.InputSystem.PlayerInputManager) automatically splits the available screen space between the active players. For this to work, you must set the [`Camera`](xref:UnityEngine.InputSystem.PlayerInput) property on the `PlayerInput` prefab. The [`PlayerInputManager`](xref:UnityEngine.InputSystem.PlayerInputManager) then automatically resizes and repositions each camera instance to let each player have their own part of the screen.

If you enable the [`Split-Screen`](xref:UnityEngine.InputSystem.PlayerInputManager) option, you can configure the following additional properties in the Inspector:

|Property|Description|
|--------|-----------|
|[`Maintain Aspect Ratio`](xref:UnityEngine.InputSystem.PlayerInputManager)|A `false` value enables the game to produce screen areas that have an aspect ratio different from the screen resolution when subdividing the screen.|
|[`Set Fixed Number`](xref:UnityEngine.InputSystem.PlayerInputManager)|If this value is greater than zero, the [`PlayerInputManager`](xref:UnityEngine.InputSystem.PlayerInputManager) always splits the screen into a fixed number of rectangles, regardless of the actual number of players.|
|[`Screen Rectangle`](xref:UnityEngine.InputSystem.PlayerInputManager)|The normalized screen rectangle available for allocating player split-screens into.|

By default, any player in the game can interact with any UI elements. However, in split-screen setups, your game can have screen-space UIs that are restricted to just one specific camera. See the [UI Input](use-player-input-component-ui.md) section on the Player Input component page on how to set this up using the Player Input component, [`InputSystemUIInputModule`](supported-ui-systems.md) and [`MultiplayerEventSystem`](multiplayer-ui-input.md) components.
