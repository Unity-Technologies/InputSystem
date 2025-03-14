---
uid: input-system-tracked-input-devices
---
# The Player Input Manager component

>NOTE: The Input System package comes with a sample called `Simple Multiplayer` which you can install from the package manager UI in the Unity editor. The sample demonstrates how to use [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) to set up a simple local multiplayer scenario.

The [`Player Input`](PlayerInput.md) system facilitates setting up local multiplayer games, where multiple players share a single screen and multiple controllers. You can set this up using the [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) component, which automatically manages the creation and lifetime of `PlayerInput` instances as players join and leave the game.

![PlayerInputManager](Images/PlayerInputManager.png)

|Property|Description|
|--------|-----------|
|[`Notification Behavior`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_notificationBehavior)|How the [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInput.html) component notifies game code about changes to the connected players. [This works the same way as for the `PlayerInput` component](PlayerInput.md#notification-behaviors).|
|[`Join Behavior`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_joinBehavior)|Determines the mechanism by which players can join when joining is enabled. See documentation on [join behaviors](#join-behaviors).|
|[`Player Prefab`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_playerPrefab)|A prefab that represents a player in the game. The [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) component creates an instance of this prefab whenever a new player joins. This prefab must have one [`PlayerInput`](PlayerInput.md) component in its hierarchy.|
|[`Joining Enabled By Default`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_joiningEnabled)|While this is enabled, new players can join via the mechanism determined by [`Join Behavior`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_joinBehavior).|
|[`Limit Number of Players`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_maxPlayerCount)|Enable this if you want to limit the number of players who can join the game.|
|[`Max Player Count`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_maxPlayerCount)(Only shown when `Limit number of Players` is enabled.)|The maximum number of players allowed to join the game.|
|[`Enable Split-Screen`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_splitScreen)|If enabled, each player is automatically assigned a portion of the available screen area. See documentation on [split-screen](#split-screen) multiplayer.|

### Join behaviors

Test