---
uid: input-system-how-do-i
---
# How do I…?

A collection of frequently asked questions, and where to find their answers in the documentation.

> **Note:**
>
> If you're new to the Input System and have landed on this page looking for documentation, it's best to read the [QuickStart Guide](QuickStartGuide.md), and the [Concepts](Concepts.md) and [Workflows](Workflows.md) pages, so that you can make sure you're choosing the best workflow for your project's input requirements.
>
> This is because there are a number of different ways to read input using the Input System, and many of the answers on this page give you the quickest but least flexible solution, and may not be suitable for a project with more complex requirements.


How do I...?

- [check if a specific key or button was pressed this frame?](../api/UnityEngine.InputSystem.Controls.ButtonControl.html#UnityEngine_InputSystem_Controls_ButtonControl_wasPressedThisFrame)

- [check if any key or button was pressed](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onAnyButtonPress)

- [find all connected gamepads?](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_all)

- [find the gamepad that the player is currently using?](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_current)

- [know when a new device was plugged in?](Devices.md#monitoring-devices)

- [create my own custom devices?](HID.md#creating-a-custom-device-layout)

- create a simple "Fire" type action?</br>
Use the same techniques shown for the "Jump" action in the [Workflows section](Workflows.md)

- [require a button to be held down for some duration before triggering an action?](Interactions.html#hold)

- [use a "positive" and a "negative" button to drive an axis?](ActionBindings.html#1d-axis)

- [create a UI to rebind input in my game?](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html)

- [set up an Action to specifically target the left-hand XR controller?](../api/UnityEngine.InputSystem.XR.XRController.html#UnityEngine_InputSystem_XR_XRController_leftHand)

- [make my left-hand XR controller my right-hand one?](../api/UnityEngine.InputSystem.XR.XRController.html#UnityEngine_InputSystem_XR_XRController_leftHand)

- [get all current touches from the touchscreen?](Touch.md#reading-all-touches)

- [deal with my gamepad data arriving in a format different from `GamepadState`?](../api/UnityEngine.InputSystem.LowLevel.GamepadState.html)

- [force the Input System to use my own layout when the native backend discovers a specific Device?](Devices.md#native-devices)

- [add deadzoning to my gamepad sticks?](Gamepad.md#deadzones)

- [give my head tracking an extra update before rendering?](../api/UnityEngine.InputSystem.XR.XRHMD.html)

- [record events flowing through the system?](Debugging.md#other-tips)

- [see events as they're processed?](Debugging.md#other-tips)

- [see what Devices I have and what state they're in?](Debugging.html#debugging-devices)
