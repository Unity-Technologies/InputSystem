# Create an on-screen button control

To create an on-screen button:

1. Add a UI `Button` object.
2. Add the [`OnScreenButton`](../api/UnityEngine.InputSystem.OnScreen.OnScreenButton.html) component to it.
3. Set the [controlPath](../api/UnityEngine.InputSystem.OnScreen.OnScreenControl.html#UnityEngine_InputSystem_OnScreen_OnScreenControl_controlPath) to refer to a [`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html) (for example, `<Gamepad>/buttonSouth`). The type of device referenced by the control path determines the type of virtual device created by the component.

![OnScreenButton](Images/OnScreenButton.png)

The [`OnScreenButton`](../api/UnityEngine.InputSystem.OnScreen.OnScreenButton.html) component requires the target control to be a `Button` control. [`OnScreenButton`](../api/UnityEngine.InputSystem.OnScreen.OnScreenButton.html) sets the target control value to 1 when it receives a pointer-down (`IPointerDownHandler.OnPointerDown`) event, or 0 when it receives a pointer-up (`IPointerUpHandler.OnPointerUp`) event.