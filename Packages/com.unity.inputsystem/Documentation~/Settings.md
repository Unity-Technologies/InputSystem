---
uid: input-system-settings
---
# Input settings

* [Update Mode](#update-mode)
* [Background Behavior](#background-behavior)
* [Filter Noise on .current](#filter-noise-on-current)
* [Compensate orientation](#compensate-orientation)
* [Default value properties](#default-value-properties)
* [Supported Devices](#supported-devices)
* [Platform-specific Settings](#platform-specific-settings)
* [Play Mode Input Behavior](#play-mode-input-behavior)

To configure the Input System individually for each project, go to __Edit > Project Settings… > Input System Package__ from Unity's main menu.

![Input Settings](Images/InputSettings.png)

The Input System stores input settings in Assets. If your Project doesn't contain an input settings Asset, click __Create settings asset__ in the Settings window to create one. If your Project contains multiple settings Assets, use the gear menu in the top-right corner of the window to choose which one to use. You can also use this menu to create additional settings Assets.

>__Note__: Unity saves changes to these settings when you save the Project.

This page describes each input setting in detail.

## Update Mode

![Update Mode](Images/UpdateMode.png)

This is a fundamental setting that determines when the Input System processes input.

The Input System processes input in one of three distinct ways:

|Type|Description|
|----|-----------|
|[`Process Events In Dynamic Update`](../api/UnityEngine.InputSystem.InputSettings.UpdateMode.html)|The Input System processes events at irregular intervals determined by the current framerate.|
|[`Process Events In Fixed Update`](../api/UnityEngine.InputSystem.InputSettings.UpdateMode.html)|The Input System processes events at fixed-length intervals. This corresponds to how [`MonoBehaviour.FixedUpdate`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html) operates. The length of each interval is determined by [`Time.fixedDeltaTime`](https://docs.unity3d.com/ScriptReference/Time-fixedDeltaTime.html).|
|[`Process Events Manually`](../api/UnityEngine.InputSystem.InputSettings.UpdateMode.html)|The Input System does not process events automatically. Instead, it processes them whenever you call [`InputSystem.Update()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_Update).|

>__Note__: The system performs two additional types of updates in the form of  [`InputUpdateType.BeforeRender`](../api/UnityEngine.InputSystem.LowLevel.InputUpdateType.html) (late update for XR tracking Devices) and [`InputUpdateType.Editor`](../api/UnityEngine.InputSystem.LowLevel.InputUpdateType.html) (for EditorWindows). Neither of these update types change how the application consumes input.

## Background Behavior

![Background Behavior](Images/BackgroundBehavior.png)

Determines how [application focus](https://docs.unity3d.com/ScriptReference/Application-isFocused.html) is handled. That is, what happens when focus is lost or gained and how input behaves while the application is not in the foreground.

This setting is only relevant when "Run In Background" is enabled in the [Player Settings](https://docs.unity3d.com/Manual/class-PlayerSettings.html) for the project. This setting is only supported on some platforms. On platforms such as Android and iOS, the game will not be run while the application is not in the foreground.

Note that in the editor, "Run In Background" is considered to always be enabled as the player loop will be kept running regardless of whether a Game View is focused or not. Also, in development players on desktop platforms, the setting is force-enabled during the build process.

>__Note__: In the editor, `Background Behavior` is further influenced by [`Play Mode Input Behavior`](#play-mode-input-behavior). See [Background and Focus Change Behavior](Devices.md#background-and-focus-change-behavior) for a detailed breakdown. In particular, which devices are considered as [`canRunInBackground`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_canRunInBackground) partly depends on the [`Play Mode Input Behavior`](#play-mode-input-behavior) setting.

|Setting|Description|
|----|-----------|
|[`Reset And Disable Non Background Devices`](../api/UnityEngine.InputSystem.InputSettings.BackgroundBehavior.html#UnityEngine_InputSystem_InputSettings_BackgroundBehavior_ResetAndDisableNonBackgroundDevices)|When focus is lost, perform a [soft reset](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_ResetDevice_UnityEngine_InputSystem_InputDevice_System_Boolean_) on all Devices that are not marked as [`canRunInBackground`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_canRunInBackground) and also subsequently [disable](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_DisableDevice_UnityEngine_InputSystem_InputDevice_System_Boolean_) them. Does not affect Devices marked as being able to run in the background.<br><br>When focus is regained, [re-enable](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_EnableDevice_UnityEngine_InputSystem_InputDevice_) any Device that has been disabled and also issue a [sync request](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_TrySyncDevice_UnityEngine_InputSystem_InputDevice_) on these Devices in order to update their current state. If a Device is issued a sync request and does not respond to it, [soft-reset](Devices.md#device-resets) the Device.<br><br>This is the default setting.|
|[`Reset And Disable All Devices`](../api/UnityEngine.InputSystem.InputSettings.BackgroundBehavior.html#UnityEngine_InputSystem_InputSettings_BackgroundBehavior_ResetAndDisableAllDevices)|When focus is lost, perform a [soft reset](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_ResetDevice_UnityEngine_InputSystem_InputDevice_System_Boolean_) on all Devices and also subsequently [disable](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_DisableDevice_UnityEngine_InputSystem_InputDevice_System_Boolean_) them.<br><br>When focus is regained, [re-enable](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_EnableDevice_UnityEngine_InputSystem_InputDevice_) all Devices and also issue a [sync request](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_TrySyncDevice_UnityEngine_InputSystem_InputDevice_) on each Device in order to update it to its current state. If a device does not respond to the sync request, [soft-reset](Devices.md#device-resets) it.|
|[`Ignore Focus`](../api/UnityEngine.InputSystem.InputSettings.BackgroundBehavior.html#UnityEngine_InputSystem_InputSettings_BackgroundBehavior_IgnoreFocus)|Do nothing when focus is lost. When focus is regained, issue a [sync request](Devices.md#device-syncs) on all Devices.|

Focus behavior has implications for how [Actions](./Actions.md) behave on focus changes. When a Device is reset, Actions bound to Controls on the device will be cancelled. This ensures, for example, that a user-controlled character in your game doesn't continue to move when focus is lost while the user is pressing one of the W, A, S or D keys. The cancellation happens in such a way that Actions are guaranteed to not trigger. That is, even if an Action is set to trigger on button release, it will not get triggered when a button is down and gets reset by a [Device reset](Devices.md#device-resets).

## Filter Noise On Current

[//]: # (REVIEW: should this be enabled by default)

This setting is disabled by default, and it's only relevant for apps that use the `.current` properties (such as [`Gamepad.current`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_current)) in the API. If your app doesn't use these properties, leave this setting disabled. Otherwise, it adds needless overhead.

Whenever there is input on a Device, the system make the respective Device `.current`. For example, if a [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) receives new input, [`Gamepad.current`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_current) is assigned to that gamepad.

Some Devices have noise in their input, and receive input even if nothing is interacting with them. For example, the PS4 DualShock controller generates a constant stream of input because of its built-in gyro. This means that if both an Xbox and a PS4 controller are connected, and the user is using the Xbox controller, the PS4 controller still pushes itself to the front continuously and makes itself current.

To counteract this, enable noise filtering. When this setting is enabled and your application receives input, the system determines whether the input comes from a Device that has noisy Controls ([`InputControl.noisy`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_noisy)). If it does, the system also determines whether the given input contains any state changes on a Control that isn't flagged as noisy. If so, that Device becomes current. Otherwise, your application still consumes the input, which is also visible on the Device, but the Device doesn't become current.

>__Note__: The system doesn't currently detect most forms of noise, but does detect those on gamepad sticks. This means that if the sticks wiggle a small amount but are still within deadzone limits, the Device still becomes current. This doesn't require actuating the sticks themselves. On most gamepads, there's a small tolerance within which the sticks move when the entire device moves.

## Compensate Orientation

If this setting is enabled, rotation values reported by [sensors](Sensors.md) are rotated around the Z axis as follows:

|Screen orientation|Effect on rotation values|
|---|---|
|[`ScreenOrientation.Portrait`](https://docs.unity3d.com/ScriptReference/ScreenOrientation.html)|Values remain unchanged|
|[`ScreenOrientation.PortraitUpsideDown`](https://docs.unity3d.com/ScriptReference/ScreenOrientation.html)|Values rotate by 180 degrees.|
|[`ScreenOrientation.LandscapeLeft`](https://docs.unity3d.com/ScriptReference/ScreenOrientation.html)|Values rotate by 90 degrees.|
|[`ScreenOrientation.LandscapeRight`](https://docs.unity3d.com/ScriptReference/ScreenOrientation.html)|Values rotate by 270 degrees.|

This setting affects the following sensors:
* [`Gyroscope`](../api/UnityEngine.InputSystem.Gyroscope.html)
* [`GravitySensor`](../api/UnityEngine.InputSystem.GravitySensor.html)
* [`AttitudeSensor`](../api/UnityEngine.InputSystem.AttitudeSensor.html)
* [`Accelerometer`](../api/UnityEngine.InputSystem.Accelerometer.html)
* [`LinearAccelerationSensor`](../api/UnityEngine.InputSystem.LinearAccelerationSensor.html)

## Default value properties

|Property|Description|
|----|-----------|
|Default Deadzone Min|The default minimum value for [Stick Deadzone](Processors.md#stick-deadzone) or [Axis Deadzone](Processors.md#axis-deadzone) processors when no `min` value is explicitly set on the processor.|
|Default Deadzone Max|The default maximum value for [Stick Deadzone](Processors.md#stick-deadzone) or [Axis Deadzone](Processors.md#axis-deadzone) processors when no `max` value is explicitly set on the processor.|
|Default Button Press Point|The default [press point](../api/UnityEngine.InputSystem.Controls.ButtonControl.html#UnityEngine_InputSystem_Controls_ButtonControl_pressPointOrDefault) for [Button Controls](../api/UnityEngine.InputSystem.Controls.ButtonControl.html), and for various [Interactions](Interactions.md). For button Controls which have analog physics inputs (such as triggers on a gamepad), this configures how far they need to be held down for the system to consider them pressed.|
|Default Tap Time|Default duration for [Tap](Interactions.md#tap) and [MultiTap](Interactions.md#multitap) Interactions. Also used by by touchscreen Devices to distinguish taps from to new touches.|
|Default Slow Tap Time|Default duration for [SlowTap](Interactions.md#tap) Interactions.|
|Default Hold Time|Default duration for [Hold](Interactions.md#hold) Interactions.|
|Tap Radius|Maximum distance between two finger taps on a touchscreen Device for the system to consider this a tap of the same touch (as opposed to a new touch).|
|Multi Tap Delay Time|Default delay between taps for [MultiTap](Interactions.md#multitap) Interactions. Also used by touchscreen Devices to count multi-taps (See [`TouchControl.tapCount`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_tapCount)).|

## Supported Devices

![Supported Devices](Images/SupportedDevices.png)

A Project usually supports a known set of input methods. For example, a mobile app might support only touch, and a console application might support only gamepads. A cross-platform application might support gamepads, mouse, and keyboard, but might not require XR Device support.

To narrow the options that the Editor UI presents to you, and to avoid creating input Devices and consuming input that your application won't use, you can restrict the set of supported Devices on a per-project basis.

If __Supported Devices__ is empty, no restrictions apply, which means that the Input System adds any Device that Unity recognizes and processes input for it. However, if __Support Devices__ contains one or more entries, the Input System only adds Devices that are of one of the listed types.

>__Note__: When the __Support Devices__ list changes, the system removes or re-adds Devices as needed. The system always keeps information about what Devices are available for potential, which means that no Device is permanently lost as long as it stays connected.

To add Devices to the list, click the Add (+) icon and choose a Device from the menu that appears.

![Add Supported Device](Images/AddSupportedDevice.png)

__Abstract Devices__ contains common Device abstractions such as "Keyboard" and "Mouse". __Specific Devices__ contains specific hardware products.

### Override in Editor

In the Editor, you might want to use input Devices that the application doesn't support. For example, you might want to use a tablet in the Editor even if your application only supports gamepads.

To force the Editor to add all locally available Devices, even if they're not in the list of __Supported Devices__, open the [Input Debugger](Debugging.md) (menu: __Window > Analysis > Input Debugger__), and select __Options > Add Devices Not Listed in 'Supported Devices'__.

![Add Devices Not Listed In Supported Devices](Images/AddDevicesNotListedInSupportedDevices.png)

This setting is stored as a user setting (that is, other users who open the same Project can't see the setting).

## Platform-specific settings

### iOS/tvOS

![iOSSettings](Images/iOSSettings.png)

* __Motion Usage__<br>
  Governs access to the [pedometer](Sensors.md#stepcounter) on the device. If enabled, the __Description__ string supplied in the settings will be added to the application's Info.plist

### Editor

#### Play Mode Input Behavior

![Play Mode Input Behavior](Images/PlayModeInputBehavior.png)

Determines how input is handled in the Editor when in play mode. Unlike in players, in the Editor Unity (and thus its input backends) will keep running for as long as the Editor is active regardless of whether a Game View is focused or not. This setting determines how input should behave when focus is __not__ on any Game View &ndash; and thus [`Application.isFocused`](https://docs.unity3d.com/ScriptReference/Application-isFocused.html) is false and the player considered to be running in the background.

|Setting|Description|
|-------|-----------|
|[`Pointers And Keyboards Respect Game View Focus`](../api/UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.html#UnityEngine_InputSystem_InputSettings_EditorInputBehaviorInPlayMode_PointersAndKeyboardsRespectGameViewFocus)|Only [Pointer](Pointers.md) and [Keyboard](Keyboard.md) Devices require the Game View to be focused. Other Devices will route their input into the application regardless of Game View focus.<br><br>This setting essentially routes any input into the game that is, by default, not used to operate the Editor UI. So, Devices such as [gamepads](Gamepad.md) will go to the application at all times when in play mode whereas keyboard input, for example, will require explicitly giving focus to a Game View window.<br><br>This setting is the default.|
|[`All Devices Respect Game View Focus`](../api/UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.html#UnityEngine_InputSystem_InputSettings_EditorInputBehaviorInPlayMode_AllDevicesRespectGameViewFocus)|Focus on a Game View is required for all Devices. When no Game View window is focused, all input goes to the editor and not to the application. This allows other EditorWindows to receive these inputs (from gamepads, for example).|
|[`All Device Input Always Goes To Game View`](../api/UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.html#UnityEngine_InputSystem_InputSettings_EditorInputBehaviorInPlayMode_AllDeviceInputAlwaysGoesToGameView)|All editor input is disabled and input is considered to be exclusive to Game Views. Also, [`Background Behavior`](#background-behavior) is to be taken literally and executed like in players. Meaning, if in a certain situation, a Device is disabled in the player, it will get disabled in the editor as well.<br><br>This setting most closely aligns player behavior with editor behavior. Be aware, however, that no EditorWindows will be able to see input from Devices (this does not effect IMGUI and UITK input in the Editor in general as they do not consume input from the Input System).|
