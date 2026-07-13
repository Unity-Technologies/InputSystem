---
uid: input-system-control-types-ref
---

# Control types reference

The Input System provides the following types of controls. These are available to select in the drop-down menu when you [configure the control type of an action](./configure-control-type.md).

|Control Type|Value Type|Description|Example|
|-|-|-|-|
|[`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl)|`float`|A 1D floating-point axis.|[`Gamepad.leftStick.x`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|
|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|`float`|A button expressed as a floating-point value. Whether the button can have a value other than 0 or 1 depends on the underlying representation. For example, gamepad trigger buttons can have values other than 0 and 1, but gamepad face buttons generally can't.|[`Mouse.leftButton`](xref:UnityEngine.InputSystem.Mouse)|
|[`KeyControl`](xref:UnityEngine.InputSystem.Controls.KeyControl)|N/A|A specialized button that represents a key on a [`Keyboard`](xref:UnityEngine.InputSystem.Keyboard). Keys have an associated [`keyCode`](xref:UnityEngine.InputSystem.Controls.KeyControl) and, unlike other types of Controls, change their display name in accordance to the currently active system-wide keyboard layout. Refer to the [Keyboard](keyboards-introduction.md) documentation for details.|[`Keyboard.aKey`](xref:UnityEngine.InputSystem.Keyboard)|
|[`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|`Vector2`|A 2D floating-point vector.|[`Pointer.position`](xref:UnityEngine.InputSystem.Pointer)|
|[`Vector3Control`](xref:UnityEngine.InputSystem.Controls.Vector3Control)|`Vector3`|A 3D floating-point vector.|[`Accelerometer.acceleration`](xref:UnityEngine.InputSystem.Accelerometer)|
|[`QuaternionControl`](xref:UnityEngine.InputSystem.Controls.QuaternionControl)|`Quaternion`|A 3D rotation.|[`AttitudeSensor.attitude`](xref:UnityEngine.InputSystem.AttitudeSensor)|
|[`IntegerControl`](xref:UnityEngine.InputSystem.Controls.IntegerControl)|`int`|An integer value.|[`Touchscreen.primaryTouch.touchId`](xref:UnityEngine.InputSystem.Controls.TouchControl)|
|[`StickControl`](xref:UnityEngine.InputSystem.Controls.StickControl)|`Vector2`|A 2D stick control like the thumbsticks on gamepads or the stick control of a joystick.|[`Gamepad.rightStick`](xref:UnityEngine.InputSystem.Gamepad)|
|[`DpadControl`](xref:UnityEngine.InputSystem.Controls.DpadControl)|`Vector2`|A 4-way button control like the D-pad on gamepads or hatswitches on joysticks.|[`Gamepad.dpad`](xref:UnityEngine.InputSystem.Gamepad)|
|[`TouchControl`](xref:UnityEngine.InputSystem.Controls.TouchControl)|`TouchState`|A control that represents all the properties of a touch on a [touch screen](devices-touch.md).|[`Touchscreen.primaryTouch`](xref:UnityEngine.InputSystem.Touchscreen)|

You can browse the set of all registered control layouts in the [input debugger](debug-layouts.md).

All controls are based on the [`InputControl`](xref:UnityEngine.InputSystem.InputControl) base class. Most concrete implementations are based on [`InputControl<TValue>`](xref:UnityEngine.InputSystem.InputControl`1).
