---
uid: input-system-select-notification-behavior
---

# Select a notification behavior

You can use the [`Behavior`](xref:UnityEngine.InputSystem.PlayerInput) property in the Inspector to determine how a `PlayerInput` component notifies game code when something related to the player has occurred.

The following options are available:

|Behavior|Description|
|--------|-----------|
|[`Send Messages`](xref:UnityEngine.InputSystem.PlayerNotifications)|Uses [`GameObject.SendMessage`](https://docs.unity3d.com/ScriptReference/GameObject.SendMessage.html) on the `GameObject` that the `PlayerInput` component belongs to.|
|[`Broadcast Messages`](xref:UnityEngine.InputSystem.PlayerNotifications)|Uses [`GameObject.BroadcastMessage`](https://docs.unity3d.com/ScriptReference/GameObject.BroadcastMessage.html) on the `GameObject` that the `PlayerInput` component belongs to. This broadcasts the message down the `GameObject` hierarchy.|
|[`Invoke Unity Events`](xref:UnityEngine.InputSystem.PlayerNotifications)|Uses a separate [`UnityEvent`](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html) for each individual type of message. When this is selected, the events available on the `PlayerInput` are accessible from the __Events__ foldout. The argument received by events triggered for Actions is the same as the one received by [`started`, `performed`, and `canceled` callbacks](respond-to-input.md#action-callbacks).<br><br>![PlayerInput UnityEvents](Images/MyPlayerActionEvents.png)|
|[`Invoke CSharp Events`](xref:UnityEngine.InputSystem.PlayerNotifications)|Similar to `Invoke Unity Events`, except that the events are plain C# events available on the `PlayerInput` API. You cannot configure these from the Inspector. Instead, you have to register callbacks for the events in your scripts.<br><br>The following events are available:<br><br><ul><li>[`onActionTriggered`](xref:UnityEngine.InputSystem.PlayerInput) (collective event for all actions on the player)</li><li>[`onDeviceLost`](xref:UnityEngine.InputSystem.PlayerInput)</li><li>[`onDeviceRegained`](xref:UnityEngine.InputSystem.PlayerInput)</li></ul>|

In addition to per-action notifications, `PlayerInput` sends the following general notifications:

|Notification|Description|
|------------|-----------|
|[`DeviceLostMessage`](xref:UnityEngine.InputSystem.PlayerInput)|The player has lost one of the Devices assigned to it. This can happen, for example, if a wireless device runs out of battery.|
|[`DeviceRegainedMessage`](xref:UnityEngine.InputSystem.PlayerInput)|Notification that triggers when the player recovers from Device loss and is good to go again.|