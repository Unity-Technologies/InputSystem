---
uid: input-system-on-screen
---
# Introduction to on-screen Controls

You can use on-screen Controls to simulate Input Devices with UI widgets that the user interacts with on the screen. The most prominent example is the use of stick and button widgets on touchscreens to emulate a joystick or gamepad.

There are currently two on-screen Control types implemented out of the box: [on-screen buttons](create-on-screen-button-control.md) and [on-screen sticks](create-on-screen-stick-control.md). You can implement custom Controls by extending the base [`OnScreenControl`](../api/UnityEngine.InputSystem.OnScreen.OnScreenControl.html) class (see documentation on [writing custom on screen Controls](create-custom-on-screen-control.md) to learn more).

>[!NOTE]
>On-screen Controls don't have a predefined visual representation. It's up to you to set up the visual aspect of a Control (for example, by adding a sprite or UI component to the GameObject). On-screen Controls take care of the interaction logic and of setting up and generating input from interactions.

Each on-screen Control uses a [Control path](Controls.md#control-paths) to reference the Control that it should report input as. For example, the following on-screen button reports input as the right shoulder button of a gamepad:

![OnScreenButton](Images/OnScreenButton.png)

The collection of on-screen Controls present in a Scene forms one or more [Input Devices](Devices.md). The Input System creates one Input Device for each distinct type of Device the Controls reference. For example, if one on-screen button references `<Gamepad>/buttonSouth` and another on-screen button references `<Keyboard>/a`, the Input System creates both a `Gamepad` and a `Keyboard`. This happens automatically when the components are enabled. When disabled, the Input System automatically removes the Devices again.

To query the Control (and, implicitly, the Device) that an on-screen Control feeds into, you can use the [`OnScreenControl.control`](../api/UnityEngine.InputSystem.OnScreen.OnScreenControl.html#UnityEngine_InputSystem_OnScreen_OnScreenControl_control) property.

>[!NOTE]
>This design allows you to use on-screen Controls to create input for arbitrary Input Devices, in addition to joysticks and gamepads.