---
uid: input-system-supported-devices
---

# Supported devices

A Project usually supports a known set of input methods. For example, a mobile app might support only touch, and a console application might support only gamepads. A cross-platform application might support gamepads, mouse, and keyboard, but might not require XR Device support.

To narrow the options that the Editor UI presents to you, and to avoid creating input Devices and consuming input that your application won't use, you can restrict the set of supported Devices on a per-project basis.

If __Supported Devices__ is empty, no restrictions apply, which means that the Input System adds any Device that Unity recognizes and processes input for it. However, if __Support Devices__ contains one or more entries, the Input System only adds Devices that are of one of the listed types.

> [!NOTE]
> When the __Support Devices__ list changes, the system removes or re-adds Devices as needed. The system always keeps information about what Devices are available for potential, which means that no Device is permanently lost as long as it stays connected.

To add Devices to the list, click the Add (+) icon and choose a Device from the menu that appears.

## Override in Editor

In the Editor, you might want to use input Devices that the application doesn't support. For example, you might want to use a tablet in the Editor even if your application only supports gamepads.

To force the Editor to add all locally available Devices, even if they're not in the list of __Supported Devices__, open the [Input Debugger](debugging.md) (menu: __Window > Analysis > Input Debugger__), and select __Options > Add Devices Not Listed in 'Supported Devices'__.

![Add Devices Not Listed In Supported Devices](Images/AddDevicesNotListedInSupportedDevices.png)

> [!NOTE]
> This setting is stored as a user setting, not a project setting. This means other users who open the project in their own Editor do not share the setting.

## Improved Shortcut Support

Under **Improved Shortcut Support**, two independent options control how the Input System resolves overlapping bindings on the same control (for example, a plain **B** key action as opposed to **Shift**+**B**):

| UI / API | Description |
| -------- | ----------- |
| __Complexity-Based Shortcut Resolution__ ([`shortcutKeysConsumeInput`](xref:UnityEngine.InputSystem.InputSettings.shortcutKeysConsumeInput)) | When enabled and Action Priority Shortcut Resolution is off, composite bindings are ordered by automatic **complexity** (depth of the composite). The first action that performs can consume the input so simpler bindings on the same control are skipped. Modifier composites use **ordered** modifier evaluation while this mode is active. |
| __Action Priority Shortcut Resolution__ ([`shortcutKeysUseActionPriority`](xref:UnityEngine.InputSystem.InputSettings.shortcutKeysUseActionPriority)) | When enabled, resolution uses each action's [`InputAction.Priority`](xref:UnityEngine.InputSystem.InputAction.Priority). Higher priority is handled first and can consume input for lower-priority actions. A performed action with priority 0 does not mark the input event as handled for suppressing lower-priority overlaps; any priority above zero can. The **Priority** field appears in the Input Actions editor when this is on. Serialized priority values are always kept on the asset even when this option is off. |

Both options default to **off**. If **Action Priority Shortcut Resolution** is on, it **takes precedence** over complexity-based resolution even if **Complexity-Based Shortcut Resolution** is also enabled.

For more detail on complexity ordering, see [Multiple input sequences (such as keyboard shortcuts)](xref:input-system-binding-conflicts#multiple-input-sequences-such-as-keyboard-shortcuts).
