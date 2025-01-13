# Control types

All controls are based on the [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html) base class. Most concrete implementations are based on [`InputControl<TValue>`](../api/UnityEngine.InputSystem.InputControl-1.html).

The Input System provides the following types of controls out of the box:

|Control Type|Description|Example|
|------------|-----------|-------|
|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|A 1D floating-point axis.|[`Gamepad.leftStick.x`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html#UnityEngine_InputSystem_Controls_Vector2Control_x)|
|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|A button expressed as a floating-point value. Whether the button can have a value other than 0 or 1 depends on the underlying representation. For example, gamepad trigger buttons can have values other than 0 and 1, but gamepad face buttons generally can't.|[`Mouse.leftButton`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_leftButton)|
|[`KeyControl`](../api/UnityEngine.InputSystem.Controls.KeyControl.html)|A specialized button that represents a key on a [`Keyboard`](../api/UnityEngine.InputSystem.Keyboard.html). Keys have an associated [`keyCode`](../api/UnityEngine.InputSystem.Controls.KeyControl.html#UnityEngine_InputSystem_Controls_KeyControl_keyCode) and, unlike other types of Controls, change their display name in accordance to the currently active system-wide keyboard layout. See the [Keyboard](Keyboard.md) documentation for details.|[`Keyboard.aKey`](../api/UnityEngine.InputSystem.Keyboard.html#UnityEngine_InputSystem_Keyboard_aKey)|
|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|A 2D floating-point vector.|[`Pointer.position`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_position)|
|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|A 3D floating-point vector.|[`Accelerometer.acceleration`](../api/UnityEngine.InputSystem.Accelerometer.html#UnityEngine_InputSystem_Accelerometer_acceleration)|
|[`QuaternionControl`](../api/UnityEngine.InputSystem.Controls.QuaternionControl.html)|A 3D rotation.|[`AttitudeSensor.attitude`](../api/UnityEngine.InputSystem.AttitudeSensor.html#UnityEngine_InputSystem_AttitudeSensor_attitude)|
|[`IntegerControl`](../api/UnityEngine.InputSystem.Controls.IntegerControl.html)|An integer value.|[`Touchscreen.primaryTouch.touchId`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_touchId)|
|[`StickControl`](../api/UnityEngine.InputSystem.Controls.StickControl.html)|A 2D stick control like the thumbsticks on gamepads or the stick control of a joystick.|[`Gamepad.rightStick`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_rightStick)|
|[`DpadControl`](../api/UnityEngine.InputSystem.Controls.DpadControl.html)|A 4-way button control like the D-pad on gamepads or hatswitches on joysticks.|[`Gamepad.dpad`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_dpad)|
|[`TouchControl`](../api/UnityEngine.InputSystem.Controls.TouchControl.html)|A control that represents all the properties of a touch on a [touch screen](Touch.md).|[`Touchscreen.primaryTouch`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_primaryTouch)|

You can browse the set of all registered control layouts in the [input debugger](Debugging.md#debugging-layouts).
