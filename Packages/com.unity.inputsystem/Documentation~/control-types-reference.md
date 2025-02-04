# Control types reference

The Input System provides the following types of controls. These are available to select in the drop-down menu when you [configure the control type of an action](./configure-control-type.md).

|Name|Description|API|
|-|-|-|
|**Any**|A setting that allows the action to bind to a control of any type.|N/A|
|**Axis**|A 1D axis providing `float` values.|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|**Button**|A button expressed as a floating-point value. Whether the button can have a value other than 0 or 1 depends on the underlying representation. For example, gamepad trigger buttons can have values other than 0 and 1, but gamepad face buttons generally can't.|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|
|**Key**|A specialized button that represents a key on a [`Keyboard`](../api/UnityEngine.InputSystem.Keyboard.html). Keys have an associated [`keyCode`](../api/UnityEngine.InputSystem.Controls.KeyControl.html#UnityEngine_InputSystem_Controls_KeyControl_keyCode) and, unlike other types of Controls, change their display name in accordance to the currently active system-wide keyboard layout. See the [Keyboard](Keyboard.md) documentation for details.|[`KeyControl`](../api/UnityEngine.InputSystem.Controls.KeyControl.html)|
|**Vector 2**|A 2D axis providing values as a `Vector2`.|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|
|**Vector 3**|A 3D axis providing values as a `Vector3`.|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|**Quaternion**|A rotational orientation control providing values as a `Quaternion`.|[`QuaternionControl`](../api/UnityEngine.InputSystem.Controls.QuaternionControl.html)|
|**Integer**|An integer value.|[`IntegerControl`](../api/UnityEngine.InputSystem.Controls.IntegerControl.html)|
|**Stick**|A 2D stick control like the thumbsticks on gamepads or the stick control of a joystick.|[`StickControl`](../api/UnityEngine.InputSystem.Controls.StickControl.html)|
|**Dpad**|A 4-way button control like the D-pad on gamepads or hatswitches on joysticks.|[`DpadControl`](../api/UnityEngine.InputSystem.Controls.DpadControl.html)|
|**Touch**|A control that represents all the properties of a touch on a [touch screen](Touch.md).|[`TouchControl`](../api/UnityEngine.InputSystem.Controls.TouchControl.html)|

You can browse the set of all registered control layouts in the [input debugger](Debugging.md#debugging-layouts).

All controls are based on the [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html) base class. Most concrete implementations are based on [`InputControl<TValue>`](../api/UnityEngine.InputSystem.InputControl-1.html).
