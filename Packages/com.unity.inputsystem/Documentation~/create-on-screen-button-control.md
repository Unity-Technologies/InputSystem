---
uid: input-system-screen-button-control
---

# Create an on-screen button control

To create an on-screen button:

1. Add a UI `Button` object.
2. Add the [`OnScreenButton`](xref:UnityEngine.InputSystem.OnScreen.OnScreenButton) component to it.
3. Set the [controlPath](xref:UnityEngine.InputSystem.OnScreen.OnScreenControl) to refer to a [`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl) (for example, `<Gamepad>/buttonSouth`). The type of device referenced by the control path determines the type of virtual device created by the component.

![The OnScreenButton component displays the Control Path value as `rightShoulder [Gamepad]`.](Images/OnScreenButton.png)

The [`OnScreenButton`](xref:UnityEngine.InputSystem.OnScreen.OnScreenButton) component requires the target control to be a `Button` control. [`OnScreenButton`](xref:UnityEngine.InputSystem.OnScreen.OnScreenButton) sets the target control value to 1 when it receives a pointer-down (`IPointerDownHandler.OnPointerDown`) event, or 0 when it receives a pointer-up (`IPointerUpHandler.OnPointerUp`) event.
