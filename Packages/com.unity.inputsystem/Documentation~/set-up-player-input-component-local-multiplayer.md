---
uid: input-system-component-local-multiplayer
---

# Set up the Player Input component for local multiplayer

In these local multiplayer scenarios, the Player Input component should be on a prefab that represents a player in your game, which the [**Player Input Manager**](PlayerInputManager.md) has a reference to. The **Player Input Manager** then instantiates players as they join the game and pairs each player instance to a unique device that the player uses exclusively (for example, one gamepad for each player). You can also manually pair devices in a way that enables two or more players to share a Device (for example, left/right keyboard splits or hot seat use).

Each `PlayerInput` corresponds to one [`InputUser`](UserManagement.md). You can use [`PlayerInput.user`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_user) to query the `InputUser` from the component.