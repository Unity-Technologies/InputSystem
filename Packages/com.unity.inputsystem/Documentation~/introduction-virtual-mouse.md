# Introduction to Virtual Mouse for UI cursor control

If your application uses gamepads and joysticks as an input, you can use [navigation input actions](supported-ui-input-types-ui-input-module.md#navigation-input) to operate the UI. However, it usually involves extra work to make the UI work well with navigation. 

An alternative way to operate the UI is to allow gamepads and joysticks to drive the cursor from a virtual mouse. The Input System package provides a **Virtual Mouse** component for this purpose.

At runtime, the component adds a virtual [Mouse](../api/UnityEngine.InputSystem.Mouse.html) device which the [InputSystemUIInputModule](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html) component picks up. The controls of the `Mouse` are fed input based on the actions configured on the [VirtualMouseInput](../api/UnityEngine.InputSystem.UI.VirtualMouseInput.html) component.

Note that the resulting [Mouse](../api/UnityEngine.InputSystem.Mouse.html) input is visible in all code that picks up input from the mouse device. You can therefore use the component for mouse simulation elsewhere, not just with [InputSystemUIInputModule](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html).

To see an example of the Virtual Mouse in a project, see the [Gamepad Mouse Cursor sample](Installation.md#installing-samples) included with the Input System package.