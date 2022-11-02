# The Player Input Manager component

>NOTE: The Input System package comes with a sample called `Simple Multiplayer` which you can install from the package manager UI in the Unity editor. The sample demonstrates how to use [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) to set up a simple local multiplayer scenario.

The [`Player Input`](PlayerInput.md) system facilitates setting up local multiplayer games, where multiple players share a single device with a single screen and multiple controllers. Set this up using the [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) component, which automatically manages the creation and lifetime of `PlayerInput` instances as players join and leave the game.

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

You can use the [`Join Behavior`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_joinBehavior) property in the Inspector to determine how a [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) component decides when to add new players to the game. The following options are available to choose the specific mechanism that [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) employs.

|Behavior|Description|
|--------|-----------|
|[`Join Players When Button IsPressed`](../api/UnityEngine.InputSystem.PlayerJoinBehavior.html)|Listen for button presses on Devices that are not paired to any player. If a player presses a button and joining is allowed, join the new player using the Device they pressed the button on.|
|[`Join Players When Join Action Is Triggered`](../api/UnityEngine.InputSystem.PlayerJoinBehavior.html)|Similar to `Join Players When Button IsPressed`, but this only joins a player if the control they triggered matches a specific action you define. For example, you can set up players to join when pressing a specific gamepad button.|
|[`Join Players Manually`](../api/UnityEngine.InputSystem.PlayerJoinBehavior.html)|Don't join players automatically. Call [`JoinPlayer`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_JoinPlayer_System_Int32_System_Int32_System_String_UnityEngine_InputSystem_InputDevice_) explicitly to join new players. Alternatively, create GameObjects with [`PlayerInput`](PlayerInput.md) components directly and the Input System will automatically join them.|

### Split-screen

If you enable the [`Split-Screen`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_splitScreen) option, the [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) automatically splits the available screen space between the active players. For this to work, you must set the [`Camera`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_camera) property on the `PlayerInput` prefab. The [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) then automatically resizes and repositions each camera instance to let each player have their own part of the screen.

If you enable the [`Split-Screen`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_splitScreen) option, you can configure the following additional properties in the Inspector:

|Property|Description|
|--------|-----------|
|[`Maintain Aspect Ratio`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_maintainAspectRatioInSplitScreen)|A `false` value enables the game to produce screen areas that have an aspect ratio different from the screen resolution when subdividing the screen.|
|[`Set Fixed Number`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_fixedNumberOfSplitScreens)|If this value is greater than zero, the [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) always splits the screen into a fixed number of rectangles, regardless of the actual number of players.|
|[`Screen Rectangle`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_splitScreenArea)|The normalized screen rectangle available for allocating player split-screens into.|

By default, any player in the game can interact with any UI elements. However, in split-screen setups, your game can have screen-space UIs that are restricted to just one specific camera. See the [UI Input](PlayerInput.md#ui-input) section on the Player Input component page on how to set this up using the Player Input component, [`InputSystemUIInputModule`](UISupport.md#setting-up-ui-input) and [`MultiplayerEventSystem`](UISupport.md#multiplayer-uis) components.

### `PlayerInputManager` notifications

`PlayerInputManager` sends notifications when something notable happens with the current player setup. These notifications are delivered according to the `Notification Behavior` property, in the [same way as for `PlayerInput`](PlayerInput.md#notification-behaviors).

Your game can listen to the following notifications:

|Notification|Description|
|------------|-----------|
|[`PlayerJoinedMessage`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_PlayerJoinedMessage)|A new player joined the game. Passes the [`PlayerInput`](PlayerInput.md`PlayerInputManager` sends a `Player Joined` notification for each of these.|
|[`PlayerLeftMessage`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_PlayerLeftMessage)|A player left the game. Passes the [`PlayerInput`](PlayerInput.md) instance of the player who left.|
