---
uid: input-system-how-do-i
---
# How do I…?

A collection of frequently asked questions, and where to find their answers in the documentation.

> [!NOTE]
> If you're new to the Input System and have landed on this page looking for documentation, it's best to read the [QuickStart Guide](xref:input-system-quickstart), and the [Concepts](xref:basic-concepts) and [Workflows](xref:input-system-workflows) pages, so that you can make sure you're choosing the best workflow for your project's input requirements.
>
> This is because there are a number of different ways to read input using the Input System, and many of the answers on this page give you the quickest but least flexible solution, and may not be suitable for a project with more complex requirements.


How do I...?

- [check if a specific key or button was pressed this frame?](xref:UnityEngine.InputSystem.Controls.ButtonControl.wasPressedThisFrame)

- [check if any key or button was pressed](xref:UnityEngine.InputSystem.InputSystem.onAnyButtonPress)

- [find all connected gamepads?](xref:UnityEngine.InputSystem.Gamepad.all)

- [find the gamepad that the player is currently using?](xref:UnityEngine.InputSystem.Gamepad.current)

- [know when a new device was plugged in?](xref:input-system-devices#monitoring-devices)

- [create my own custom devices?](xref:input-system-hid#creating-a-custom-device-layout)

- create a simple "Fire" type action?</br>
Use the same techniques shown for the "Jump" action in the [Workflows section](xref:input-system-workflows)

- [require a button to be held down for some duration before triggering an action?](Interactions.html#hold)

- [use a "positive" and a "negative" button to drive an axis?](ActionBindings.html#1d-axis)

- [create a UI to rebind input in my game?](xref:UnityEngine.InputSystem.InputActionRebindingExtensions)

- [set up an Action to specifically target the left-hand XR controller?](xref:UnityEngine.InputSystem.XR.XRController.leftHand)

- [make my left-hand XR controller my right-hand one?](xref:UnityEngine.InputSystem.XR.XRController.leftHand)

- [get all current touches from the touchscreen?](xref:input-system-touch#reading-all-touches)

- [deal with my gamepad data arriving in a format different from `GamepadState`?](xref:UnityEngine.InputSystem.LowLevel.GamepadState)

- [force the Input System to use my own layout when the native backend discovers a specific Device?](xref:input-system-devices#native-devices)

- [add deadzoning to my gamepad sticks?](xref:input-system-gamepad#deadzones)

- [give my head tracking an extra update before rendering?](xref:UnityEngine.InputSystem.XR.XRHMD)

- [record events flowing through the system?](xref:input-system-debugging#other-tips)

- [see events as they're processed?](xref:input-system-debugging#other-tips)

- [see what Devices I have and what state they're in?](Debugging.html#debugging-devices)
