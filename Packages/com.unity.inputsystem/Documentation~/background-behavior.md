---
uid: input-system-background-behavior
---

# Background behavior

Background Behaviour determines what happens when [application focus](https://docs.unity3d.com/ScriptReference/Application-isFocused.html) is lost or regained, and how input behaves while the application is not in the foreground.

This setting is only relevant when "Run In Background" is enabled in the [Player Settings](https://docs.unity3d.com/Manual/class-PlayerSettings.html) for the project. This setting is only supported on some platforms. On platforms such as Android and iOS, your app will not run when it is not in the foreground.

In the Editor, "Run In Background" is considered to always be enabled as the player loop is kept running regardless of whether a Game View is focused or not. Also, in development players on desktop platforms, the setting is force-enabled during the build process.

> [!NOTE]
> In the editor, `Background Behavior` is further influenced by [`Play Mode Input Behavior`](platform-specific-settings.md#play-mode-input-behavior). Refer to [Background and Focus Change Behavior](device-background-focus-changes.md) for a detailed breakdown. In particular, which devices are considered as [`canRunInBackground`](xref:UnityEngine.InputSystem.InputDevice) partly depends on the [`Play Mode Input Behavior`](platform-specific-settings.md#play-mode-input-behavior) setting.

|Setting|Description|
|----|-----------|
|[`Reset And Disable Non Background Devices`](xref:UnityEngine.InputSystem.InputSettings.BackgroundBehavior)|When focus is lost, perform a [soft reset](xref:UnityEngine.InputSystem.InputSystem) on all Devices that are not marked as [`canRunInBackground`](xref:UnityEngine.InputSystem.InputDevice) and also subsequently [disable](xref:UnityEngine.InputSystem.InputSystem) them. Does not affect Devices marked as being able to run in the background.<br><br>When focus is regained, [re-enable](xref:UnityEngine.InputSystem.InputSystem) any Device that has been disabled and also issue a [sync request](xref:UnityEngine.InputSystem.InputSystem) on these Devices in order to update their current state. If a Device is issued a sync request and does not respond to it, [soft-reset](reset-device.md) the Device.<br><br>This is the default setting.|
|[`Reset And Disable All Devices`](xref:UnityEngine.InputSystem.InputSettings.BackgroundBehavior)|When focus is lost, perform a [soft reset](xref:UnityEngine.InputSystem.InputSystem) on all Devices and also subsequently [disable](xref:UnityEngine.InputSystem.InputSystem) them.<br><br>When focus is regained, [re-enable](xref:UnityEngine.InputSystem.InputSystem) all Devices and also issue a [sync request](xref:UnityEngine.InputSystem.InputSystem) on each Device in order to update it to its current state. If a device does not respond to the sync request, [soft-reset](reset-device.md) it.|
|[`Ignore Focus`](xref:UnityEngine.InputSystem.InputSettings.BackgroundBehavior)|Do nothing when focus is lost. When focus is regained, issue a [sync request](sync-device.md) on all Devices.|

Focus behavior has implications for how [Actions](actions.md) behave on focus changes. When a Device is reset, Actions bound to Controls on the device will be cancelled. This ensures, for example, that a user-controlled character in your game doesn't continue to move when focus is lost while the user is pressing one of the W, A, S or D keys. The cancellation happens in such a way that Actions are guaranteed to not trigger. That is, even if an Action is set to trigger on button release, it will not get triggered when a button is down and gets reset by a [Device reset](reset-device.md).
