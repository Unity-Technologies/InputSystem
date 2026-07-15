---
uid: input-system-platform-specific-settings
---

# Platform-specific settings

## iOS/tvOS

__Motion Usage__
  Governs access to the [pedometer](supported-sensors-reference.md) on the device. If you enable this setting, the __Description__ field becomes editable. The text you enter into the Description field is added to your application's `Info.plist`.

## Editor

### Play Mode Input Behavior

__Play Mode Input Behavior__ determines how input is handled in the Editor when in play mode. Unlike in built players, in the Unity Editor the input back-ends keep running for as long as the Editor is active, regardless of whether a Game View window is focused or not. This setting determines how input should behave when focus is __not__ on any Game View &ndash; and thus [`Application.isFocused`](https://docs.unity3d.com/ScriptReference/Application-isFocused.html) is false and the player considered to be running in the background.

|Setting|Description|
|-------|-----------|
|[`Pointers And Keyboards Respect Game View Focus`](xref:UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)|Only [Pointer](devices-pointers.md) and [Keyboard](devices-keyboard.md) Devices require the Game View to be focused. Other Devices will route their input into the application regardless of Game View focus.<br><br>This setting essentially routes any input into the game that is, by default, not used to operate the Editor UI. So, Devices such as [gamepads](devices-gamepads.md) will go to the application at all times when in play mode whereas keyboard input, for example, will require explicitly giving focus to a Game View window.<br><br>This setting is the default.|
|[`All Devices Respect Game View Focus`](xref:UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)|Focus on a Game View is required for all Devices. When no Game View window is focused, all input goes to the editor and not to the application. This allows other EditorWindows to receive these inputs (from gamepads, for example).|
|[`All Device Input Always Goes To Game View`](xref:UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)|All editor input is disabled and input is considered to be exclusive to Game Views. Also, [`Background Behavior`](background-behavior.md) is to be taken literally and executed like in players. Meaning, if in a certain situation, a Device is disabled in the player, it will get disabled in the editor as well.<br><br>This setting most closely aligns player behavior with editor behavior. Be aware, however, that no EditorWindows will be able to see input from Devices (this does not effect IMGUI and UITK input in the Editor in general as they do not consume input from the Input System).|

### Input Action Property Drawer Mode

Determines how the Inspector window displays [`InputActionProperty`](xref:UnityEngine.InputSystem.InputActionProperty) fields.

This setting is not shown in the **Edit** &gt; **Project Settings** window, it is instead only available in the Debug mode of the Inspector window of an Input Settings asset. See the Unity Manual page for [working in the Inspector](https://docs.unity3d.com/Manual/InspectorOptions.html) under section Toggle Debug Mode.

|Setting|Description|
|-------|-----------|
|[`Compact`](xref:UnityEngine.InputSystem.InputSettings.InputActionPropertyDrawerMode)|Display the property in a compact format, using a minimal number of lines. Toggling between a reference to an input action in an asset and a directly serialized input action is done using a dropdown menu.|
|[`Multiline Effective`](xref:UnityEngine.InputSystem.InputSettings.InputActionPropertyDrawerMode)|Display the effective action underlying the property, using multiple lines. Toggling between a reference to an input action in an asset and a directly serialized input action is done using a property that is always visible. This mode could be useful if you want to see or revert prefab overrides and hide the field that is ignored.|
|[`Multiline Both`](xref:UnityEngine.InputSystem.InputSettings.InputActionPropertyDrawerMode)|Display both the input action and external reference underlying the property. Toggling between a reference to an input action in an asset and a directly serialized input action is done using a property that is always visible. This mode could be useful if you want to see both values of the property without needing to toggle Use Reference.|
