---
uid: input-system-configure-player-input-manager
---

# Configure the Player Input Manager component

![PlayerInputManager](Images/PlayerInputManager.png)

|Property|Description|
|--------|-----------|
|[`Notification Behavior`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_notificationBehavior)|How the [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInput.html) component notifies game code about changes to the connected players. [This works the same way as for the `PlayerInput` component](player-input-component.md#notification-behaviors).|
|[`Join Behavior`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_joinBehavior)|Determines the mechanism by which players can join when joining is enabled. See documentation on [join behaviors](#join-behaviors).|
|[`Player Prefab`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_playerPrefab)|A prefab that represents a player in the game. The [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) component creates an instance of this prefab whenever a new player joins. This prefab must have one [`PlayerInput`](player-input-component.md) component in its hierarchy.|
|[`Joining Enabled By Default`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_joiningEnabled)|While this is enabled, new players can join via the mechanism determined by [`Join Behavior`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_joinBehavior).|
|[`Limit Number of Players`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_maxPlayerCount)|Enable this if you want to limit the number of players who can join the game.|
|[`Max Player Count`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_maxPlayerCount)(Only shown when `Limit number of Players` is enabled.)|The maximum number of players allowed to join the game.|
|[`Enable Split-Screen`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_splitScreen)|If enabled, each player is automatically assigned a portion of the available screen area. See documentation on [split-screen](#split-screen) multiplayer.|

### Join behaviors

You can use the [`Join Behavior`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_joinBehavior) property in the Inspector to determine how a [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) component decides when to add new players to the game. The following options are available to choose the specific mechanism that [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) employs.

|Behavior|Description|
|--------|-----------|
|[`Join Players When Button IsPressed`](../api/UnityEngine.InputSystem.PlayerJoinBehavior.html)|Listen for button presses on Devices that are not paired to any player. If a player presses a button and joining is allowed, join the new player using the Device they pressed the button on.|
|[`Join Players When Join Action Is Triggered`](../api/UnityEngine.InputSystem.PlayerJoinBehavior.html)|Similar to `Join Players When Button IsPressed`, but this only joins a player if the control they triggered matches a specific action you define. For example, you can set up players to join when pressing a specific gamepad button.|
|[`Join Players Manually`](../api/UnityEngine.InputSystem.PlayerJoinBehavior.html)|Don't join players automatically. Call [`JoinPlayer`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_JoinPlayer_System_Int32_System_Int32_System_String_UnityEngine_InputSystem_InputDevice_) explicitly to join new players. Alternatively, create GameObjects with [`PlayerInput`](player-input-component.md) components directly and the Input System will automatically join them.|

### `PlayerInputManager` notifications

`PlayerInputManager` sends notifications when something notable happens with the current player setup. These notifications are delivered according to the `Notification Behavior` property, in the [same way as for `PlayerInput`](player-input-component.md#notification-behaviors).

Your game can listen to the following notifications:

|Notification|Description|
|------------|-----------|
|[`PlayerJoinedMessage`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_PlayerJoinedMessage)|A new player joined the game. Passes the [`PlayerInput`](PlayerInput.md`PlayerInputManager` sends a `Player Joined` notification for each of these.|
|[`PlayerLeftMessage`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_PlayerLeftMessage)|A player left the game. Passes the [`PlayerInput`](player-input-component.md) instance of the player who left.|
