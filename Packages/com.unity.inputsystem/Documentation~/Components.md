# GameObject Components for Input

Two `MonoBehaviour` components are available to simplify setting up and working with input. They are generally the quickest and easiest way to get started with input in Unity.

|Component|Description|
|---------|-----------|
|[`PlayerInput`](#playerinput-component)|Represents a single player along with the player's associated [input actions](Actions.md).|
|[`PlayerInputManager`](#playerinputmanager-component)|Handles setups that allow for several players including scenarios such as player lobbies and split-screen gameplay.|

>NOTE: These components are built on top of the public input system API. As such, they don't do anything that you cannot program yourself. They are meant primarily as an easy out-of-the-box setup to obsolete much of the need for custom scripting.

## `PlayerInput` Component

![PlayerInput](Images/PlayerInput.png)

Each [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) represents a separate player in the game. Multiple [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) instances may coexist at the same time (though not on the same `GameObject`) to represent local multiplayer setups. Each player will be paired to a unique set of devices for exclusive use by the player, although it is possible to manually pair devices such that players share a device (e.g. for left/right keyboard splits or hotseat use).

Each [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) corresponds to one [`InputUser`](UserManagement.md). The [`InputUser`](UserManagement.md) can be queried from the component using [`PlayerInput.user`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_user).

You can use the following properties to configure [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html).

|Property|Description|
|--------|-----------|
|[`Actions`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_actions)|The set of [input actions](Actions.md) associated with the player. To receive input, each player must have an associated set of actions. See [here](#actions) for details.|
|[`Default Control Scheme`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_defaultControlScheme)|Which [control scheme](ActionBindings.md#control-schemes) to use from what is defined in [`Actions`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_actions) by default.|
|[`Default Action Map`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_defaultActionMap)|Which [action map](Actions.md#overview) in [`Actions`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_actions) to enable by default. If set to `None`, then the player will start out with no actions being enabled.|
|[`Camera`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_camera)|The individual camera associated with the player.<br><br>This is __only required__ when employing [split-screen](#split-screen) setups. It has no effect otherwise.|
|[`Behavior`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_notificationBehavior)|How the [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) component notifies game code about things that happen with the player. See [here](#notification-behaviors).|

### Actions

To receive input, each player requires an associated set of input actions. When creating these through the "Create Actions..." button in the [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) inspector, a default set of actions will be created. However, the [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) component places no restrictions on the arrangement of actions.

[`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) will handle [enabling and disabling](Actions.md#using-actions) automatically and will also take care of installing [callbacks](Actions.md#responding-to-actions) on the actions. Also, when multiple [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) components use the same actions, the components will automatically take care of creating [private copies of the actions](Actions.md#using-actions-with-multiple-players).

When first enabled, [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) will enable all actions from the action map identified by [`Default Action Map`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_defaultActionMap). If no default action map has been set, no actions will be enabled by default. To manually enable actions, you can simply call [`Enable`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_Enable) and [`Disable`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_Disable) on the action maps or actions like you would do [without `PlayerInput`](Actions.md#using-actions). You can check or switch which action map is currently enabled using the [`PlayerInput.currentActionMap`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_currentActionMap) property. To switch action maps by action map name, you can also call [`PlayerInput.SwitchCurrentActionMap`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_SwitchCurrentActionMap_System_String_).

To disable input on a player, call [`PlayerInput.PassivateInput`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_PassivateInput). To re-enable it, call [`PlayerInput.ActivateInput`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_ActivateInput). The latter will enable the default action map, if set.

When [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) is disabled, it will automatically disable the currently active action map ([`PlayerInput.currentActionMap`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_currentActionMap)) and disassociate any devices paired to the player.

See [the following section](#notification-behaviors) for how to be notified when an action is triggered by a player.

#### `SendMessage`/`BroadcastMessage` Actions

When the [notification behavior](#notification-behaviors) of [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) is set to `Send Messages` or `Broadcast Messages`, actions can be responded to by defining methods in components like so:

```CSharp
public class MyPlayerScript : MonoBehaviour
{
    // "fire" action becomes "OnFire" method. If you're not interested in the
    // value from the control that triggers the action, simply have a method
    // without arguments.
    public void OnFire()
    {
    }

    // If you are interested in the value from the control that triggers an action,
    // you can declare a parameter of type InputValue.
    public void OnMove(InputValue value)
    {
        // Read value from control. The type depends on what type of controls
        // the action is bound to.
        var v = value.Get<Vector2>();

        // IMPORTANT: The given InputValue is only valid for the duration of the callback.
        //            Storing the InputValue references somewhere and calling Get<T>()
        //            later will not work correctly.
    }
}
```

The component must sit on the same `GameObject` if `Send Messages` is chosen or on the same or any child `GameObject` if `Broadcast Messages` is chosen.

#### `UnityEvent` Actions

When the [notification behavior](#notification-behaviors) of [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) is set to `Invoke Unity Events`, each action has to individually be routed to a target method. The methods have the same format as the [`started`, `performed`, and `canceled` callbacks](Actions.md#started-performed-and-canceled-callbacks) on [`InputAction`](../api/UnityEngine.InputSystem.InputAction.html).

```CSharp
public class MyPlayerScript : MonoBehaviour
{
    public void OnFire(InputAction.CallbackContext context)
    {
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        var value = context.ReadValue<Vector2>();
    }
}
```

### Notification Behaviors

You can use the [`Behavior`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_notificationBehavior) property in the inspector to determine how a [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) component notifies game code when something related to the player has occurred. The following options are available to choose the specific mechanism that [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) employs.

You can listen to the following notifications:

|Behavior|Description|
|--------|-----------|
|[`Send Messages`](../api/UnityEngine.InputSystem.PlayerNotifications.html)|Uses [`GameObject.SendMessage`](https://docs.unity3d.com/ScriptReference/GameObject.SendMessage.html) on the `GameObject` that the [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) component belongs to. The messages that will be sent by the component are shown in the UI.|
|[`Broadcast Messages`](../api/UnityEngine.InputSystem.PlayerNotifications.html)|Like `Send Message` but instead of [`GameObject.SendMessage`](https://docs.unity3d.com/ScriptReference/GameObject.SendMessage.html) uses [`GameObject.BroadcastMessage`](https://docs.unity3d.com/ScriptReference/GameObject.BroadcastMessage.html). This will broadcast the message down the `GameObject` hierarchy.|
|[`Invoke Unity Events`](../api/UnityEngine.InputSystem.PlayerNotifications.html)|Uses a separate [`UnityEvent`](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html) for each individual type of message. When this is selected, the events that are available on the given [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) are accessible from the "Events" foldout. The argument received by events triggered for actions is the same as the one received by [`started`, `performed`, and `canceled` callbacks](Actions.md#started-performed-and-canceled-callbacks).<br><br>![PlayerInput UnityEvents](Images/MyPlayerActionEvents.png)|
|[`Invoke CSharp Events`](../api/UnityEngine.InputSystem.PlayerNotifications.html)|Similar to `Invoke Unity Events` except that the events are plain C# events available on the [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) API. These cannot be initialized from the inspector but have to be manually registered callbacks for in script.<br><br>The following events are available:<br><br><ul><li>[`onActionTriggered`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_onActionTriggered) (collective event for all actions on the player)</li><li>[`onDeviceLost`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_onDeviceLost)</li><li>[`onDeviceRegained`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_onDeviceRegained)</li></ul>|

In addition to per-action notifications, the following general notifications are employed by [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html).

|Notification|Description|
|------------|-----------|
|[`DeviceLostMessage`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_DeviceLostMessage)|The player has lost one of the devices assigned to it. This can happen, for example, if a wireless device runs out of battery.|
|[`DeviceRegainedMessage`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_DeviceRegainedMessage)|Notification that is triggered when the player recovers from device loss and is good to go again.|

### Control Schemes

### Device Assignments

Each [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) can be assigned one or more devices. By default, no two [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) components will be assigned the same devices &mdash; although this can be forced explicitly by manually assigning devices to a player when calling [`PlayerInput.Instantiate`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_Instantiate_GameObject_System_Int32_System_String_System_Int32_UnityEngine_InputSystem_InputDevice_) or by calling [`InputUser.PerformPairingWithDevice`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_PerformPairingWithDevice_UnityEngine_InputSystem_InputDevice_UnityEngine_InputSystem_Users_InputUser_UnityEngine_InputSystem_Users_InputUserPairingOptions_) on the [`InputUser`](UserManagement.md) of a [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html).

The devices assigned .

### UI Input

The [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) component can work together with a [`InputSystemUIInputModule`](UISupport.md#inputsystemuiinputmodule-component) to drive the [UI system](UISupport.md).

To set this up, assign a reference to a [`InputSystemUIInputModule`](UISupport.md#inputsystemuiinputmodule-component) component in the [`UI Input Module`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_uiInputModule) field of the [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) component. The [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) and [`InputSystemUIInputModule`](UISupport.md#inputsystemuiinputmodule-component) components should be configured to work with the same [`InputActionAsset`](Actions.md) for this to work.

Now, when the [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) component configures the actions for a specific player, it will assign the same action configuration to the [`InputSystemUIInputModule`](UISupport.md#inputsystemuiinputmodule-component). So the same device used to control the player will now be set up to control the UI.

If you use [`MultiplayerEventSystem`](UISupport.md#multiplayereventsystem-component) components to dispatch UI events, you can also use this setup to simultaneously have multiple UI instances on the screen, controlled by separate players.

## `PlayerInputManager` Component

The [`PlayerInput`](#playerinput-component) system has been designed to facilitate setting up local multiplayer games, with multiple players sharing a single device with a single screen and multiple controllers. This is set up using the [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) component, which automatically manages the creation and livetime of [`PlayerInput`](#playerinput-component) instances as players join and leave the game.

![PlayerInputManager](Images/PlayerInputManager.png)

|Property|Description|
|--------|-----------|
|[`Notification Behavior`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_notificationBehavior)|How the [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInput.html) component notifies game code about things that happen with the. [This works the same way as for the `PlayerInput` component](#notification-behaviors).|
|[`Join Behavior`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_joinBehavior)|Determines the mechanism by which players can join when joining is enabled. See [Join Behaviors](#join-behaviors).|
|[`Player Prefab`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_playerPrefab)|A prefab representing a player in the game. The [`PlayerInputManager`]((../api/UnityEngine.InputSystem.PlayerInputManager.html) component will create an instance of this prefab whenever a new player joins. This prefab must have one [`PlayerInput`](#playerinput-component) component in it's hierarchy.|
|[`Joining Enabled By Default`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_joiningEnabled)|While this is enabled, new players can join via the mechanism determined by [`Join Behavior`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_joinBehavior).|
|[`Limit Number of Players`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_maxPlayerCount)|Enable this if you want to limit the number of players who can join the game.|
|[`Max Player Cou nt`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_maxPlayerCount)(Only shown when `Limit number of Players` is enabled.)|The maximum number of players allowed to join the game.|
|[`Enable Split-Screen`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_splitScreen)|If enabled, each player will automatically be assigned a portion of the available screen area. See [Split-Screen](#split-screen)|

### Join Behaviors

You can use the [`Join Behavior`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_joinBehavior) property in the inspector to determine how a [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) component decides when to add new players to the game. The following options are available to choose the specific mechanism that [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) employs.

|Behavior|Description|
|--------|-----------|
|[`Join Players When Button IsPressed`](../api/UnityEngine.InputSystem.PlayerJoinBehavior.html)|Listen for button presses on devices that are not paired to any player. If they occur and joining is allowed, join a new player using the device the button was pressed on.|
|[`Join Players When Join Action Is Triggered`](../api/UnityEngine.InputSystem.PlayerJoinBehavior.html)|Similar to `Join Players When Button IsPressed`, but this will only join a player if the control which was triggered matches a specific action you can define. That way, you can set up players to join when pressing a specific gamepad button for instance.|
|[`Join Players Manually`](../api/UnityEngine.InputSystem.PlayerJoinBehavior.html)|Do not join players automatically. Call [`JoinPlayerFromUI`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_JoinPlayerFromUI) or [`JoinPlayerFromAction`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_JoinPlayerFromAction_UnityEngine_InputSystem_InputAction_CallbackContext_) explicitly in order to join new players. Alternatively, just create GameObjects with [`PlayerInput`](#playerinput-component) components directly and they will be joined automatically.|

### Split-Screen

If you enable the [`Split-Screen`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_splitScreen) option, then the [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) will automatically split the available screen space between the active players. For this to work, the [`Camera`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_camera) property must be configured on the [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html) prefab. The [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) will then automatically resize and reposition each camera instance to let each player have it's own part of the screen.

If you enable the [`Split-Screen`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_splitScreen) option, the following additional properties will be shown in the inspector:

|Property|Description|
|--------|-----------|
|[`Maintain Aspect Ratio`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_maintainAspectRatioInSplitScreen)|Determines whether subdividing the screen is allowed to produce screen areas that have an aspect ratio different from the screen resolution.|
|[`Set Fixed Number`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_fixedNumberOfSplitScreens)|If this value is larger then zero, then the [`PlayerInputManager`](../api/UnityEngine.InputSystem.PlayerInputManager.html) will always split the screen into a fixed number of rectangles, regardless of the actual number of players.|
|[`Screen Rectangle`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_splitScreenArea)|The normalized screen rectangle available for allocating player split-screens into.|

By default, any UI elements can be interacted with by any player in the game. However, in split-screen setups, it is possible to have screen-space UIs that are restricted to just one specific camera. See the (UI Input)[#ui-input] section above on how to set this up using the [`PlayerInput`](../api/UnityEngine.InputSystem.PlayerInput.html), [`InputSystemUIInputModule`](UISupport.md#inputsystemuiinputmodule-component) and [`MultiplayerEventSystem`](UISupport.md#multiplayereventsystem-component) components.

### `PlayerInputManager` Notifications

`PlayerInputManager` sends notifications when something notable happens with the current player setup. How these notifications are delivered through the `Notification Behavior` property the [same way as for `PlayerInput`](#notification-behaviors).

You can listen to the following notifications:

|Notification|Description|
|------------|-----------|
|[`PlayerJoinedMessage`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_PlayerJoinedMessage)|A new player has joined the game. Passes the [`PlayerInput`](#playerinput-component) instance of the player who has joined.<br><br>Note that if there are already active [`PlayerInput`](#playerinput-component) components present when `PlayerInputManager` is enabled, it will send a `Player Joined` notification for each of these.|
|[`PlayerLeftMessage`](../api/UnityEngine.InputSystem.PlayerInputManager.html#UnityEngine_InputSystem_PlayerInputManager_PlayerLeftMessage)|A player left the game. Passes the [`PlayerInput`](#playerinput-component) instance of the player who has left.|
