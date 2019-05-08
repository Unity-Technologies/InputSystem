# Controls

* [Hierarchies](#control-hierarchies)
* [Types](#control-types)
* [Usages](#control-usages)
* [Paths](#control-paths)
* [State](#control-state)
* [Actuation](#control-actuation)
* [Noisy Controls](#noisy-controls)
* [Synthetic Controls](#synthetic-controls)

An input control represents a source of values. These values can be of any structured or primitive type. The only requirement is that the type is "blittable" (i.e. `unmanaged`).

>NOTE: Controls are used for __input__ only. Output and configuration items on input devices are not represented as controls.

Each control is identified by a name (`InputControl.name`) and can optionally have a display name (`InputControl.displayName`) that differs from the control name. For example, the face button closest to the touchpad on the PS4 controller has the control name `buttonSouth` and the display name `Square`.

Additionally, a control may be identified by one or more aliases which provide alternative names for the control. The aliases for a specific control can be accessed through its `InputControl.aliases` property.

Finally, a control may have a "short" display name which can be accessed through the `InputControl.shortDisplayName` property. An example of such a name is "LMB" for the left mouse button.

## Control Hierarchies

Controls can form hierarchies. The root of a control hierarchy is always a [device](Devices.md).

The setup of hierarchies is exclusively controlled through [layouts](Layouts.md) and invariably put together by `InputDeviceBuilder`.

The parent of a control can be accessed using `InputControl.parent`, the children through `InputControl.children`. The flattened hierarchy of all controls on a device can be accessed through `InputDevice.allControls`.

## Control Types

All controls are based on the `InputControl` base class. Most concrete implementations are based on `InputControl<TValue>`.

The following types of controls are provided by the input system out of the box:

|Control Type|Description|Example|
|------------|-----------|-------|
|Axis|A 1D floating-point axis.|`Gamepad.leftStick.x`|
|Button|A button expressed as a floating-point value. It depends on the underlying representation whether the button can have a value other than 0 and 1. The gamepad trigger buttons can do so, for example, whereas the gamepad face buttons generally can not.|`Mouse.leftButton`|
|Key|A specialized button representing a key on a `Keyboard`. Keys have an associated `keyCode` and, unlike other types of controls, will change their display name in accordance to the currently active system-wide keyboard layout. See the [Keyboard](Keyboard.md) documentation for details.|`Keyboard.aKey`|
|Vector2|A 2D floating-point vector.|`Mouse.position`|
|Vector3|A 3D floating-point vector.|`Accelerometer.acceleration`|
|Quaternion|A 3D rotation.|`AttitudeSensor.attitude`|
|Integer||
|Stick|A 2D stick control like the thumbsticks on gamepads or the stick control of a joystick.|`Gamepad.rightStick`|
|Dpad|A 4-way button control like the dpad on gamepads or hatswitches on joysticks.|`Gamepad.dpad`|
|Touch||`Touchscreen.primaryTouch`|

The set of all registered control layouts can be browsed using the [input debugger](Debugging.md).

## Control Usages

A control may have one or more associated usages. A usage denotes the semantics of a control, i.e. the way the control is intended to be used. An example of a control usage is `Submit` which denotes a control that is commonly used to confirm selections in UIs and such. On a gamepad, this usage is commonly found on the `buttonSouth` control.

The usages of a control can be accessed via the `InputControl.usages` property.

Usages can be arbitrary strings. However, a certain set of usages is very commonly used and comes predefined in the API in the form of the `CommonUsages` static class. The following table lists a number of common control usages and their meanings.

|Usage|Description|
|-----|-----------|
|Submit||
|Cancel||
|Forward||
|Back||
|PrimaryAction||
|SecondaryAction||
|PrimaryTrigger||
|SecondaryTrigger||
|Primary2DMotion||
|Secondary2DMotion||

## Control Paths

>Example: `<Gamepad>/leftStick/x` means "X control on left stick of gamepad".

Controls can be looked up using textual paths. This feature is heavily used by [bindings](Actions.md#bindings) on input actions in order to identify the control(s) from which input is to be read. However, they can also be used for lookup directly on controls and devices or to

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
|`controlName`|Requires the control at the current level to have the given name. Both "proper" names (`InputControl.name`) and aliases (`InputControl.aliases`) are taken into account.<br><br>This field can also be `*` to match any name.|`MyGamepad/buttonSouth`<br><br>`*/{PrimaryAction}` (match `PrimaryAction` usage on device with any name)|
|`#(displayName)`|Requires the control at the current level to have the given display name (i.e. `InputControl.displayName`). Note that the display name may contain whitespace and symbols.|`<Keyboard>/#(a)` (matches the key that generates the "a" character, if any, according to the current keyboard layout).<br><br>`<Gamepad>/#(Cross)`|

The "literal" path of a given control can be accessed via its `InputControl.path` property.

## Control State

Each control is connected to a block of memory that is considered the control's "state". The size, format, and location of this block of memory can be queried from a control through the `InputControl.stateBlock` property.

The state of controls is stored in unmanaged memory that is handled internally by the input system. All devices added to the system share one block of unmanaged memory that contains the state of all the controls on the devices.

State might not be stored in the "natural" format for a control. For example, buttons are often represented as bitfields and axis controls are often represented as 8-bit or 16-bit integer values. The format is determined by how the platform, hardware, and driver combination feeds input into Unity. Each control knows the format of its storage and how to translate the values as needed. This process is set up through [layouts](Layouts.md).

The current state of a control can be accessed through its `ReadValue` method.

```CSharp
Gamepad.current.leftStick.x.ReadValue();
```

Each type of control has a specific type of values that it returns &mdash; regardless of how many different types of formats that it supports for its state. This value type can be accessed through the `InputControl.valueType` property.

Reading a value from a control may apply one or more value processors. Details on how processors operate can be found [here](Processors.md).

#### Default State

    ////TODO

#### Reading State vs Reading Values

    ////TODO

## Control Actuation

A control is considered "actuated" when it has moved away from its default state in such a way that it affects the actual value of the control. Whether a control is currently actuated can be queried through `IsActuated`.

```CSharp
// Check if leftStick is currently actuated.
if (Gamepad.current.leftStick.IsActuated())
    Debug.Log("Left Stick is actuated");
```

It can be useful to determine not just whether a control is actuated at all but the amount by which it is actuated. This is termed its magnitude. For a `Vector2` control, for example, this would be the length of the vector whereas for a button, it is simply the raw, absolute floating-point value.

In general, the current magnitude of a control is always >= 0. However, a control may not have a meaningful magnitude, in which case it will return -1 (any negative value should be considered an invalid magnitude).

The current amount of actuation can be queried using `EvaluateMagnitude`.

```CSharp
// Check if left stick is actuated more than a quarter of its motion range.
if (Gamepad.current.leftStick.EvaluateMagnitude() > 0.25f)
    Debug.Log("Left Stick actuated past 25%");
```

There are two mechanisms that most notably make use of control actuation:

- [Interactive rebinding](ActionBindings.md#runtime-rebinding) (`InputActionRebindingExceptions.RebindOperation`) uses it to select between multiple suitable controls to find the one that is actuated  the most.
- [Disambiguation](ActionBindings.md#disambiguation) between multiple controls that are bound to the same action uses it to decide which control gets to drive the action.

## Noisy Controls

A control may be identified as being "noisy" in nature. This is indicated by the `InputControl.noisy` property being true. The value of the property is initialized by `InputDeviceBuilder` from the `noisy` property in the control's [layout](Layouts.md).

Being noisy has two primary effects:

1. The control is not considered for [interactive rebinding](ActionBindings.md#runtime-rebinding). I.e. `InputActionRebindingExceptions.RebindOperation` will ignore the control by default (this can be bypassed using `WithoutIgnoringNoisyControls`).
2. The system will perform additional event filtering before calling `InputDevice.MakeCurrent`. If an input event for a device contains no state change on a control that is __not__ marked noisy, then the device will not be made current based on the event. This avoids, for example, a plugged in PS4 controller constantly making itself the current gamepad (`Gamepad.current`) due to its sensors constantly feeding data into the system.

>NOTE: If __any__ control on a device is noisy, the device itself is flagged as noisy (i.e. `InputDevice.noisy` is true).

## Synthetic Controls

A "synthetic" control is a control that does not correspond to an actual physical control on a device. An example are the `left`, `right`, `up`, and `down` direction buttons on a stick. These controls synthesize input from actual physical controls for the sake of presenting it in a different way (in this case, for allowing to treat the individual directions of a stick as buttons).

Whether a given control is synthetic is indicated by its `InputControl.synthetic` property. The value of the property is initialized by `InputDeviceBuilder` from the `synthetic` property in the control's [layout](Layouts.md).

Synthetic controls will be considering for [interactive rebinding](ActionBindings.md#runtime-rebinding) but non-synthetic controls will always be favored over synethetic ones. I.e. if there is both a synthetic and a non-synthetic control that are a potential match, the non-synthetic control will win out by default. This makes it possible to interactively bind to `<Gamepad>/leftStick/left`, for example, but at the same time makes it possible to bind to `<Gamepad>/leftStickPress` without getting interference from the synthetic buttons on the stick.
