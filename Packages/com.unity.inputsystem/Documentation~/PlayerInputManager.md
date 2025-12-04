---
uid: input-system-player-input-manager
---
# The Player Input Manager component

> [!NOTE]
> The Input System package comes with a sample called `Simple Multiplayer` which you can install from the package manager UI in the Unity editor. The sample demonstrates how to use [`PlayerInputManager`](xref:UnityEngine.InputSystem.PlayerInputManager) to set up a simple local multiplayer scenario.

The [`Player Input`](xref:input-system-player-input) system facilitates setting up local multiplayer games, where multiple players share a single screen and multiple controllers. You can set this up using the [`PlayerInputManager`](xref:UnityEngine.InputSystem.PlayerInputManager) component, which automatically manages the creation and lifetime of `PlayerInput` instances as players join and leave the game.

![On the PlayerInputManager component, the Notification Behavior value displays Send Messages, the Join Behavior value displays Join Players When Button Is Pressed, and the Joining Enabled By Default value is checked.](Images/PlayerInputManager.png){width="486" height="279"}

|Property|Description|
|--------|-----------|
|[`Notification Behavior`](xref:UnityEngine.InputSystem.PlayerInputManager.notificationBehavior)|How the [`PlayerInputManager`](xref:UnityEngine.InputSystem.PlayerInput) component notifies game code about changes to the connected players. [This works the same way as for the `PlayerInput` component](xref:input-system-player-input#notification-behaviors).|
|[`Join Behavior`](xref:UnityEngine.InputSystem.PlayerInputManager.joinBehavior)|Determines the mechanism by which players can join when joining is enabled. See documentation on [join behaviors](#join-behaviors).|
|[`Player Prefab`](xref:UnityEngine.InputSystem.PlayerInputManager.playerPrefab)|A prefab that represents a player in the game. The [`PlayerInputManager`](xref:UnityEngine.InputSystem.PlayerInputManager) component creates an instance of this prefab whenever a new player joins. This prefab must have one [`PlayerInput`](xref:input-system-player-input) component in its hierarchy.|
|[`Joining Enabled By Default`](xref:UnityEngine.InputSystem.PlayerInputManager.joiningEnabled)|While this is enabled, new players can join via the mechanism determined by [`Join Behavior`](xref:UnityEngine.InputSystem.PlayerInputManager.joinBehavior).|
|[`Limit Number of Players`](xref:UnityEngine.InputSystem.PlayerInputManager.maxPlayerCount)|Enable this if you want to limit the number of players who can join the game.|
|[`Max Player Count`](xref:UnityEngine.InputSystem.PlayerInputManager.maxPlayerCount)(Only shown when `Limit number of Players` is enabled.)|The maximum number of players allowed to join the game.|
|[`Enable Split-Screen`](xref:UnityEngine.InputSystem.PlayerInputManager.splitScreen)|If enabled, each player is automatically assigned a portion of the available screen area. See documentation on [split-screen](#split-screen) multiplayer.|

### Join behaviors

You can use the [`Join Behavior`](xref:UnityEngine.InputSystem.PlayerInputManager.joinBehavior) property in the Inspector to determine how a [`PlayerInputManager`](xref:UnityEngine.InputSystem.PlayerInputManager) component decides when to add new players to the game. The following options are available to choose the specific mechanism that [`PlayerInputManager`](xref:UnityEngine.InputSystem.PlayerInputManager) employs.

|Behavior|Description|
|--------|-----------|
|[`Join Players When Button IsPressed`](xref:UnityEngine.InputSystem.PlayerJoinBehavior)|Listen for button presses on Devices that are not paired to any player. If a player presses a button and joining is allowed, join the new player using the Device they pressed the button on.|
|[`Join Players When Join Action Is Triggered`](xref:UnityEngine.InputSystem.PlayerJoinBehavior)|Similar to `Join Players When Button IsPressed`, but this only joins a player if the control they triggered matches a specific action you define. For example, you can set up players to join when pressing a specific gamepad button.|
|[`Join Players Manually`](xref:UnityEngine.InputSystem.PlayerJoinBehavior)|Don't join players automatically. Call [`JoinPlayer`](xref:UnityEngine.InputSystem.PlayerInputManager.JoinPlayer(System.Int32,System.Int32,System.String,UnityEngine.InputSystem.InputDevice)) explicitly to join new players. Alternatively, create GameObjects with [`PlayerInput`](xref:input-system-player-input) components directly and the Input System will automatically join them.|

### Split-screen

If you enable the [`Split-Screen`](xref:UnityEngine.InputSystem.PlayerInputManager.splitScreen) option, the [`PlayerInputManager`](xref:UnityEngine.InputSystem.PlayerInputManager) automatically splits the available screen space between the active players. For this to work, you must set the [`Camera`](xref:UnityEngine.InputSystem.PlayerInput.camera) property on the `PlayerInput` prefab. The [`PlayerInputManager`](xref:UnityEngine.InputSystem.PlayerInputManager) then automatically resizes and repositions each camera instance to let each player have their own part of the screen.

If you enable the [`Split-Screen`](xref:UnityEngine.InputSystem.PlayerInputManager.splitScreen) option, you can configure the following additional properties in the Inspector:

|Property|Description|
|--------|-----------|
|[`Maintain Aspect Ratio`](xref:UnityEngine.InputSystem.PlayerInputManager.maintainAspectRatioInSplitScreen)|A `false` value enables the game to produce screen areas that have an aspect ratio different from the screen resolution when subdividing the screen.|
|[`Set Fixed Number`](xref:UnityEngine.InputSystem.PlayerInputManager.fixedNumberOfSplitScreens)|If this value is greater than zero, the [`PlayerInputManager`](xref:UnityEngine.InputSystem.PlayerInputManager) always splits the screen into a fixed number of rectangles, regardless of the actual number of players.|
|[`Screen Rectangle`](xref:UnityEngine.InputSystem.PlayerInputManager.splitScreenArea)|The normalized screen rectangle available for allocating player split-screens into.|

By default, any player in the game can interact with any UI elements. However, in split-screen setups, your game can have screen-space UIs that are restricted to just one specific camera. See the [UI Input](xref:input-system-player-input#ui-input) section on the Player Input component page on how to set this up using the Player Input component, [`InputSystemUIInputModule`](xref:input-system-ui-support#setting-up-ui-input) and [`MultiplayerEventSystem`](xref:input-system-ui-support#multiplayer-uis) components.

### `PlayerInputManager` notifications

`PlayerInputManager` sends notifications when something notable happens with the current player setup. These notifications are delivered according to the `Notification Behavior` property, in the [same way as for `PlayerInput`](xref:input-system-player-input#notification-behaviors).

Your game can listen to the following notifications:

|Notification|Description|
|------------|-----------|
|[`PlayerJoinedMessage`](xref:UnityEngine.InputSystem.PlayerInputManager.PlayerJoinedMessage)|A new player joined the game. Passes the [`PlayerInput`](PlayerInput.md`PlayerInputManager` sends a `Player Joined` notification for each of these).|
|[`PlayerLeftMessage`](xref:UnityEngine.InputSystem.PlayerInputManager.PlayerLeftMessage)|A player left the game. Passes the [`PlayerInput`](xref:input-system-player-input) instance of the player who left.|
