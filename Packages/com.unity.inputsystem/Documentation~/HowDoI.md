---
uid: input-system-how-do-i
---
# How do I…?

A collection of frequently asked questions, and where to find their answers in the documentation.

> **Note:**
>
> If you're new to the Input System and have landed on this page looking for documentation, it's best to read the [QuickStart Guide](quick-start-guide.md), and the [Concepts](understanding-input.md) and [Workflows](workflows.md) pages, so that you can make sure you're choosing the best workflow for your project's input requirements.
>
> This is because there are a number of different ways to read input using the Input System, and many of the answers on this page give you the quickest but least flexible solution, and may not be suitable for a project with more complex requirements.


How do I...?

- [check if a specific key or button was pressed this frame?](xref:UnityEngine.InputSystem.Controls.ButtonControl)

- [check if any key or button was pressed](xref:UnityEngine.InputSystem.InputSystem)

- [find all connected gamepads?](xref:UnityEngine.InputSystem.Gamepad)

- [find the gamepad that the player is currently using?](xref:UnityEngine.InputSystem.Gamepad)

- [know when a new device was plugged in?](monitor-devices.md)

- [create my own custom devices?](hid-create-custom-layout.md)

- create a simple "Fire" type action?</br>
Use the same techniques shown for the "Jump" action in the [Workflows section](workflows.md)

- [require a button to be held down for some duration before triggering an action?](built-in-interactions.md#hold)

- [use a "positive" and a "negative" button to drive an axis?](configure-bindings-from-code.md#1d-axis)

- [create a UI to rebind input in my game?](xref:UnityEngine.InputSystem.InputActionRebindingExtensions)

- [set up an Action to specifically target the left-hand XR controller?](xref:UnityEngine.InputSystem.XR.XRController)

- [make my left-hand XR controller my right-hand one?](xref:UnityEngine.InputSystem.XR.XRController)

- [get all current touches from the touchscreen?](touch-polling.md#read-all-touches)

- [deal with my gamepad data arriving in a format different from `GamepadState`?](xref:UnityEngine.InputSystem.LowLevel.GamepadState)

- [force the Input System to use my own layout when the native backend discovers a specific Device?](native-devices.md)

- [add deadzoning to my gamepad sticks?](query-gamepads.md#add-a-deadzone-to-a-gamepad)

- [give my head tracking an extra update before rendering?](xref:UnityEngine.InputSystem.XR.XRHMD)

- [record events flowing through the system?](see-record-input-event-flow.md)

- [see events as they're processed?](see-record-input-event-flow.md)

- [see what Devices I have and what state they're in?](debug-device.md)
