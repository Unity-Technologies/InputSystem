# Controls

* [Hierarchies](#control-hierarchies)
* [Types](#control-types)
* [Usages](#control-usages)
* [Paths](#control-paths)
* [State](#control-state)
* [Actuation](#control-actuation)
* [Noisy Controls](#noisy-controls)
* [Synthetic Controls](#synthetic-controls)

An input control represents a source of values. These values can be of any structured or primitive type. The only requirement is that the type is [blittable](https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types).

>NOTE: Controls are used for __input__ only. Output and configuration items on input devices are not represented as controls.

Each control is identified by a name ([`InputControl.name`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_name)) and can optionally have a display name ([`InputControl.displayName`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_displayName)) that differs from the control name. For example, the face button closest to the touchpad on the PS4 controller has the control name "buttonSouth" and the display name "Square".

Additionally, a control may be identified by one or more aliases which provide alternative names for the control. The aliases for a specific control can be accessed through its [`InputControl.aliases`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_aliases) property.

Finally, a control may have a "short" display name which can be accessed through the [`InputControl.shortDisplayName`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_shortDisplayName) property. An example of such a name is "LMB" for the left mouse button.

## Control Hierarchies

Controls can form hierarchies. The root of a control hierarchy is always a [device](Devices.md).

The setup of hierarchies is exclusively controlled through [layouts](Layouts.md).

The parent of a control can be accessed using [`InputControl.parent`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_parent), the children through [`InputControl.children`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_children). The flattened hierarchy of all controls on a device can be accessed through [`InputDevice.allControls`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_allControls).

## Control Types

All controls are based on the [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html) base class. Most concrete implementations are based on [`InputControl<TValue>`](../api/UnityEngine.InputSystem.InputControl-1.html).

The following types of controls are provided by the input system out of the box:

|Control Type|Description|Example|
|------------|-----------|-------|
|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|A 1D floating-point axis.|[`Gamepad.leftStick.x`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html#UnityEngine_InputSystem_Controls_Vector2Control_x)|
|[`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html)|A button expressed as a floating-point value. It depends on the underlying representation whether the button can have a value other than 0 and 1. The gamepad trigger buttons can do so, for example, whereas the gamepad face buttons generally can not.|[`Mouse.leftButton`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_leftButton)|
|[`KeyControl`](../api/UnityEngine.InputSystem.Controls.KeyControl.html)|A specialized button representing a key on a [`Keyboard`](../api/UnityEngine.InputSystem.Keyboard.html). Keys have an associated [`keyCode`](../api/UnityEngine.InputSystem.Controls.KeyControl.html#UnityEngine_InputSystem_Controls_KeyControl_keyCode) and, unlike other types of controls, will change their display name in accordance to the currently active system-wide keyboard layout. See the [Keyboard](Keyboard.md) documentation for details.|[`Keyboard.aKey`](../api/UnityEngine.InputSystem.Keyboard.html#UnityEngine_InputSystem_Keyboard_aKey)|
|[`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html)|A 2D floating-point vector.|[`Pointer.position`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_position)|
|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|A 3D floating-point vector.|[`Accelerometer.acceleration`](../api/UnityEngine.InputSystem.Accelerometer.html#UnityEngine_InputSystem_Accelerometer_acceleration)|
|[`QuaternionControl`](../api/UnityEngine.InputSystem.Controls.QuaternionControl.html)|A 3D rotation.|[`AttitudeSensor.attitude`](../api/UnityEngine.InputSystem.AttitudeSensor.html#UnityEngine_InputSystem_AttitudeSensor_attitude)|
|[`IntegerControl`](../api/UnityEngine.InputSystem.Controls.IntegerControl.html)|An integer value.|[`Touchscreen.primaryTouch.touchId`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_touchId)|
|[`StickControl`](../api/UnityEngine.InputSystem.Controls.StickControl.html)|A 2D stick control like the thumbsticks on gamepads or the stick control of a joystick.|[`Gamepad.rightStick`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_rightStick)|
|[`DpadControl`](../api/UnityEngine.InputSystem.Controls.DpadControl.html)|A 4-way button control like the dpad on gamepads or hatswitches on joysticks.|[`Gamepad.dpad`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_dpad)|
|[`TouchControl`](../api/UnityEngine.InputSystem.Controls.TouchControl.html)|A control representing all the properties of a touch on a [touch screen](Touch.md).|[`Touchscreen.primaryTouch`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_primaryTouch)|

The set of all registered control layouts can be browsed using the [input debugger](Debugging.md#debugging-layouts).

## Control Usages

A control may have one or more associated usages. A usage is a string denoting the semantics of a control, i.e. the way the control is intended to be used. An example of a control usage is `Submit` which denotes a control that is commonly used to confirm selections in UIs and such. On a gamepad, this usage is commonly found on the `buttonSouth` control.

The usages of a control can be accessed via the [`InputControl.usages`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_usages) property.

Usages can be arbitrary strings. However, a certain set of usages is very commonly used and comes predefined in the API in the form of the [`CommonUsages`](../api/UnityEngine.InputSystem.CommonUsages.html) static class. Check out the [`CommonUsages` scripting API page](../api/UnityEngine.InputSystem.CommonUsages.html) for an overview.

## Control Paths

>Example: `<Gamepad>/leftStick/x` means "X control on left stick of gamepad".

Controls can be looked up using textual paths. This feature is heavily used by [bindings](ActionBindings.md) on input actions in order to identify the control(s) from which input is to be read. However, they can also be used for lookup directly on controls and devices or to let the input system search for controls among all devices using [`InputSystem.FindControls`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_FindControls_System_String_).

```CSharp
var gamepad = Gamepad.all[0];
var leftStickX = gamepad["leftStick/x"];
var submitButton = gamepad["{Submit}"];
var allSubmitButtons = InputSystem.FindControls("*/{Submit}");
```

Control paths resemble file system paths. Each path is comprised of one or more components each separated by a forward slash:

    component/component...

Each component has the same syntax made up of multiple fields. Each field is optional but at least one field must be present. All fields are case-insensitive.

    <layoutName>{usageName}controlName#(displayName)

The following table explains the use of each field:

|Field|Description|Example|
|-----|-----------|-------|
|`<layoutName>`|Requires the control at the current level to be based on the given layout. The actual layout of the control may be the same or a layout *based* on the given layout.|`<Gamepad>/buttonSouth`|
|`{usageName}`|Works differently for controls and devices.<br><br>When used on a device (i.e. in the first component of a path), it requires the device to have the given usage. See [Device Usages](Devices.md#device-usages) for more details.<br><br>For looking up a control, the usage field is currently restricted to the path component immediately following the device (i.e. the second component in the path). It will find the control on the device that has the given usage. The control can be anywhere in the control hierarchy of the device.|*Device:*<br><br>`<XRController>{LeftHand}/trigger`<br><br>*Control:*<br><br>`<Gamepad>/{Submit}`|
|`controlName`|Requires the control at the current level to have the given name. Both "proper" names ([`InputControl.name`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_name)) and aliases ([`InputControl.aliases`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_aliases)) are taken into account.<br><br>This field can also be `*` to match any name.|`MyGamepad/buttonSouth`<br><br>`*/{PrimaryAction}` (match `PrimaryAction` usage on device with any name)|
|`#(displayName)`|Requires the control at the current level to have the given display name (i.e. [`InputControl.displayName`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_displayName)). Note that the display name may contain whitespace and symbols.|`<Keyboard>/#(a)` (matches the key that generates the "a" character, if any, according to the current keyboard layout).<br><br>`<Gamepad>/#(Cross)`|

The "literal" path of a given control can be accessed via its [`InputControl.path`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_path) property.

## Control State

Each control is connected to a block of memory that is considered the control's "state". The size, format, and location of this block of memory can be queried from a control through the [`InputControl.stateBlock`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_stateBlock) property.

The state of controls is stored in unmanaged memory that is handled internally by the input system. All devices added to the system share one block of unmanaged memory that contains the state of all the controls on the devices.

State might not be stored in the "natural" format for a control. For example, buttons are often represented as bitfields and axis controls are often represented as 8-bit or 16-bit integer values. The format is determined by how the platform, hardware, and driver combination feeds input into Unity. Each control knows the format of its storage and how to translate the values as needed. This process is set up through [layouts](Layouts.md).

The current state of a control can be accessed through its [`ReadValue`](../api/UnityEngine.InputSystem.InputControl-1.html#UnityEngine_InputSystem_InputControl_1_ReadValue) method.

```CSharp
Gamepad.current.leftStick.x.ReadValue();
```

Each type of control has a specific type of values that it returns &mdash; regardless of how many different types of formats that it supports for its state. This value type can be accessed through the [`InputControl.valueType`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_valueType) property.

Reading a value from a control may apply one or more value processors. Details on how processors operate can be found [here](Processors.md).

[//]: # (#### Default State - TODO)

[//]: # (#### Reading State vs Reading Values - TODO)

#### Recording State History

It can be useful to be able to access the history of value changes on a control. For example, in order to compute exit velocity on a touch release.

Recording state changes over time can be easily achieved using [`InputStateHistory`](../api/UnityEngine.InputSystem.LowLevel.InputStateHistory.html) or [`InputStateHistory<TValue>`](../api/UnityEngine.InputSystem.LowLevel.InputStateHistory-1.html) (the latter restricts controls to those of a specific value type which in turn simplifies some of the API).

```CSharp
// Create history that records Vector2 control value changes.
// NOTE: You can also pass controls directly or use paths that match multiple
//       controls (e.g. "<Gamepad>/<Button>").
// NOTE: The unconstrained InputStateHistory class can record changes on controls
//        of different value types.
var history = new InputStateHistory<Vector2>("<Touchscreen>/primaryTouch/position");

// To start recording state changes of the controls to which the history
// is attached, call StartRecording.
history.StartRecording();

// To stop recording state changes, call StopRecording.
history.StopRecording();

// Recorded history can be accessed like an array.
for (var i = 0; i < history.Count; ++i)
{
    // Each recorded value provides information about which control changed
    // value (in cases state from multiple controls is recorded concurrently
    // by the same InputStateHistory) and when it did so.

    var time = history[i].time;
    var control = history[i].control;
    var value = history[i].ReadValue();
}

// Recorded history can also be iterated over.
foreach (var record in history)
    Debug.Log(record.ReadValue());
Debug.Log(string.Join(",\n", history));

// You can also record state changes manually which essentially allows
// storing arbitrary histories in InputStateHistory.
// NOTE: This records a value change that did not actually happen on the control.
history.RecordStateChange(Touchscreen.current.primaryTouch.position,
    new Vector2(0.123f, 0.234f));

// State histories allocate unmanaged memory and need to be disposed.
history.Dispose();
```

Say you want to have the last 100 samples of the left stick on the gamepad available.

```
var history = new InputStateHistory<Vector2>(Gamepad.current.leftStick);
history.historyDepth = 100;
history.StartRecording();
```

## Control Actuation

A control is considered "actuated" when it has moved away from its default state in such a way that it affects the actual value of the control. Whether a control is currently actuated can be queried through [`IsActuated`](../api/UnityEngine.InputSystem.InputControlExtensions.html#UnityEngine_InputSystem_InputControlExtensions_IsActuated_UnityEngine_InputSystem_InputControl_System_Single_).

```CSharp
// Check if leftStick is currently actuated.
if (Gamepad.current.leftStick.IsActuated())
    Debug.Log("Left Stick is actuated");
```

It can be useful to determine not just whether a control is actuated at all but the amount by which it is actuated. This is termed its magnitude. For a [`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html), for example, this would be the length of the vector whereas for a button, it is simply the raw, absolute floating-point value.

In general, the current magnitude of a control is always >= 0. However, a control may not have a meaningful magnitude, in which case it will return -1 (any negative value should be considered an invalid magnitude).

The current amount of actuation can be queried using [`EvaluateMagnitude`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_EvaluateMagnitude).

```CSharp
// Check if left stick is actuated more than a quarter of its motion range.
if (Gamepad.current.leftStick.EvaluateMagnitude() > 0.25f)
    Debug.Log("Left Stick actuated past 25%");
```

There are two mechanisms that most notably make use of control actuation:

- [Interactive rebinding](ActionBindings.md#runtime-rebinding) (`InputActionRebindingExceptions.RebindOperation`) uses it to select between multiple suitable controls to find the one that is actuated  the most.
- [Disambiguation](ActionBindings.md#disambiguation) between multiple controls that are bound to the same action uses it to decide which control gets to drive the action.

## Noisy Controls

A control may be identified as being "noisy" in nature. This is indicated by the [`InputControl.noisy`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_noisy) property being true.

Being noisy has two primary effects:

1. The control is not considered for [interactive rebinding](ActionBindings.md#runtime-rebinding). I.e. [`InputActionRebindingExceptions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) will ignore the control by default (this can be bypassed using [`WithoutIgnoringNoisyControls`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithoutIgnoringNoisyControls)).
2. The system will perform additional event filtering before calling [`InputDevice.MakeCurrent`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_MakeCurrent). If an input event for a device contains no state change on a control that is __not__ marked noisy, then the device will not be made current based on the event. This avoids, for example, a plugged in PS4 controller constantly making itself the current gamepad ([`Gamepad.current`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_current)) due to its sensors constantly feeding data into the system.

>NOTE: If __any__ control on a device is noisy, the device itself is flagged as noisy.

## Synthetic Controls

A "synthetic" control is a control that does not correspond to an actual physical control on a device. An example are the `left`, `right`, `up`, and `down` direction buttons on a stick. These controls synthesize input from actual physical controls for the sake of presenting it in a different way (in this case, for allowing to treat the individual directions of a stick as buttons).

Whether a given control is synthetic is indicated by its [`InputControl.synthetic`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_synthetic) property.

Synthetic controls will be considering for [interactive rebinding](ActionBindings.md#runtime-rebinding) but non-synthetic controls will always be favored over synethetic ones. I.e. if there is both a synthetic and a non-synthetic control that are a potential match, the non-synthetic control will win out by default. This makes it possible to interactively bind to `<Gamepad>/leftStick/left`, for example, but at the same time makes it possible to bind to `<Gamepad>/leftStickPress` without getting interference from the synthetic buttons on the stick.
