---
uid: input-system-on-screen
---
# Introduction to on-screen controls

You can use on-screen controls to simulate Input Devices with UI widgets that the user interacts with on the screen. The most prominent example is the use of stick and button widgets on touchscreens to emulate a joystick or gamepad.

There are two built-in on-screen control types:

* [On-screen buttons](create-on-screen-button-control.md)
* [On-screen sticks](create-on-screen-stick-control.md)

You can implement custom controls by extending the base [`OnScreenControl`](xref:UnityEngine.InputSystem.OnScreen.OnScreenControl) class (see documentation on [writing custom on screen controls](create-custom-on-screen-control.md) to learn more).

> [!NOTE]
> On-screen controls don't have a predefined visual representation. It's up to you to set up the visual aspect of a control (for example, by adding a sprite or UI component to the GameObject). On-screen controls take care of the interaction logic and of setting up and generating input from interactions.

Each on-screen control uses a [control path](control-paths.md) to reference the control that it should report input as. For example, the following on-screen button reports input as the right shoulder button of a gamepad:

![The OnScreenButton component displays the Control Path value as `rightShoulder [Gamepad]`.](Images/OnScreenButton.png)

The collection of on-screen controls present in a Scene forms one or more [input devices](devices.md). The Input System creates one input Device for each distinct type of device the controls reference. For example, if one on-screen button references `<Gamepad>/buttonSouth` and another on-screen button references `<Keyboard>/a`, the Input System creates both a `Gamepad` and a `Keyboard`. This happens automatically when the components are enabled. When disabled, the Input System automatically removes the devices again.

To query the control (and, implicitly, the device) that an on-screen control feeds into, you can use the [`OnScreenControl.control`](xref:UnityEngine.InputSystem.OnScreen.OnScreenControl) property.

> [!NOTE]
> This design allows you to use on-screen controls to create input for arbitrary input devices, in addition to joysticks and gamepads.
