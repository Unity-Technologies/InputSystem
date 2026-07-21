---
uid: input-system-intro-controls
---

# Introduction to controls

A **control** is a part of a [device](devices.md) that sends values to the Input System when [actuated](control-actuation.md). Devices usually have multiple controls integrated into a single physical object. On a gamepad device, each of the buttons and sticks are controls. On a keyboard device, each of the individual keys are controls. Controls can take many other forms unique to certain types of device such as the pressure and radius of a pen, or the three-dimensional orientation of an XR input device.

In most scenarios, to set up input for your app, use the [Actions Editor window](actions-editor.md) to [bind](./bindings.md) controls to [actions](actions.md), so that when a user presses a button or moves a stick, the Input System sends the values from that control to the action which you can then [write code to respond](./respond-to-input.md).

## Control names

Each control is identified by a name ([`InputControl.name`](xref:UnityEngine.InputSystem.InputControl)) and can optionally have a display name ([`InputControl.displayName`](xref:UnityEngine.InputSystem.InputControl)) that differs from the control name. For example, the right-hand face button closest to the touchpad on a PlayStation DualShock 4 controller has the control name `buttonWest` and the display name `Square`.

Additionally, a control might have one or more aliases which provide alternative names for the control. You can access the aliases for a specific control through its [`InputControl.aliases`](xref:UnityEngine.InputSystem.InputControl) property.

Finally, a control might also have a short display name which can be accessed through the [`InputControl.shortDisplayName`](xref:UnityEngine.InputSystem.InputControl) property. For example, the short display name for the left mouse button is "LMB".

## Control value types

A control represents a source of values. These values can be of any structured or primitive type, such as `float`, `Vector2`, or `Quaternion`. The only requirement is that the type is [blittable](https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types).

## Parts of devices that are not controls

Devices sometimes have other functional parts that are not considered controls. For example, output and configuration items such as haptic feedback, player number indicator lights, and on/off switches. The Input System only considers a part to be a control if it sends values for the purposes of input.
