# Configuring multiplayer UI input

The Input System can handle multiple separate UI instances on the screen controlled separately by different [Input Bindings](ActionBindings.md). This is useful if you want to have multiple local players share a single screen with different controllers, so that every player can control their own UI instance. 

To allow this, you need to replace the [Event System](https://docs.unity3d.com/Manual/script-EventSystem.html) component from Unity with the Input System's [Multiplayer Event System](../api/UnityEngine.InputSystem.UI.MultiplayerEventSystem.html) component.

![MultiplayerEventSystem](Images/MultiplayerEventSystem.png)

You can have multiple Multiplayer Event Systems active in the Scene at the same time. This means you can have multiple players, each with their own [UI Input Module](LINK) and Multiplayer Event System components, and each player can have their own set of Actions driving their own UI instance. If you are using the [Player Input](player-input-component.md) component, you can also set it to automatically configure the player's UI Input Module to use the player's Actions. See the documentation on [Player Input](player-input-component.md#ui-input) to learn how.

The properties of the Multiplayer Event System component are identical to those from the Event System component. However, the Multiplayer Event System component also has a [Player Root](../api/UnityEngine.InputSystem.UI.MultiplayerEventSystem.html#UnityEngine_InputSystem_UI_MultiplayerEventSystem_playerRoot) property, which you can set to a GameObject that contains all the UI [selectables](https://docs.unity3d.com/Manual/script-Selectable.html) this event system should handle in its hierarchy. Mouse input that this event system processes then ignores any UI selectables which are not on any GameObject in the Hierarchy under [Player Root](../api/UnityEngine.InputSystem.UI.MultiplayerEventSystem.html#UnityEngine_InputSystem_UI_MultiplayerEventSystem_playerRoot).