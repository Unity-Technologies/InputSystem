# Controls

* [Hierarchies](#control-hierarchies)
* [Types](#control-types)
* [Usages](#control-usages)
* [Paths](#control-paths)
* [State](#control-state)
* [Actuation](#control-actuation)
* [Noisy Controls](#noisy-controls)
* [Synthetic Controls](#synthetic-controls)

An Input Control represents a source of values. These values can be of any structured or primitive type. The only requirement is that the type is [blittable](https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types).

>__Note__: Controls are for input only. Output and configuration items on Input Devices are not represented as Controls.

Each Control is identified by a name ([`InputControl.name`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_name)) and can optionally have a display name ([`InputControl.displayName`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_displayName)) that differs from the Control name. For example, the right-hand face button closest to the touchpad on a PlayStation DualShock 4 controller has the control name "buttonWest" and the display name "Square".

Additionally, a Control might have one or more aliases which provide alternative names for the Control. You can access the aliases for a specific Control through its [`InputControl.aliases`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_aliases) property.

Finally, a Control might also have a short display name which can be accessed through the [`InputControl.shortDisplayName`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_shortDisplayName) property. For example, the short display name for the left mouse button is "LMB".

## Control hierarchies

Controls can form hierarchies. The root of a Control hierarchy is always a [Device](Devices.md).

The setup of hierarchies is exclusively controlled through [layouts](Layouts.md).

You can access the parent of a Control using [`InputControl.parent`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_parent), and its children using [`InputControl.children`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_children). To access the flattened hierarchy of all Controls on a Device, use [`InputDevice.allControls`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_allControls).

## Control types

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

## Control usages

A Control can have one or more associated usages. A usage is a string that denotes the Control's intended use. An example of a Control usage is `Submit`, which labels a Control that is commonly used to confirm a selection in the UI. On a gamepad, this usage is commonly found on the `buttonSouth` Control.

You can access a Control's usages using the [`InputControl.usages`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_usages) property.

Usages can be arbitrary strings. However, a certain set of usages is very commonly used and comes predefined in the API in the form of the [`CommonUsages`](../api/UnityEngine.InputSystem.CommonUsages.html) static class. Check out the [`CommonUsages` scripting API page](../api/UnityEngine.InputSystem.CommonUsages.html) for an overview.

## Control paths

>Example: `<Gamepad>/leftStick/x` means "X Control on left stick of gamepad".

The Input System can look up Controls using textual paths. [Bindings](ActionBindings.md) on Input Actions rely on this feature to identify the Control(s) they read input from. However, you can also use them for lookup directly on Controls and Devices, or to let the Input System search for Controls among all devices using [`InputSystem.FindControls`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_FindControls_System_String_).

```CSharp
var gamepad = Gamepad.all[0];
var leftStickX = gamepad["leftStick/x"];
var submitButton = gamepad["{Submit}"];
var allSubmitButtons = InputSystem.FindControls("*/{Submit}");
```

Control paths resemble file system paths. Each path consists of one or more components separated by a forward slash:

    component/component...

Each component uses a similar syntax made up of multiple fields. Each field is optional, but at least one field must be present. All fields are case-insensitive.

    <layoutName>{usageName}controlName#(displayName)

The following table explains the use of each field:

|Field|Description|Example|
|-----|-----------|-------|
|`<layoutName>`|Requires the Control at the current level to be based on the given layout. The actual layout of the Control may be the same or a layout *based* on the given layout.|`<Gamepad>/buttonSouth`|
|`{usageName}`|Works differently for Controls and Devices.<br><br>When used on a Device (the first component of a path), it requires the device to have the given usage. See [Device usages](Devices.md#device-usages) for more details.<br><br>For looking up a Control, the usage field is currently restricted to the path component immediately following the Device (the second component in the path). It finds the Control on the Device that has the given usage. The Control can be anywhere in the Control hierarchy of the Device.|Device:<br><br>`<XRController>{LeftHand}/trigger`<br><br>Control:<br><br>`<Gamepad>/{Submit}`|
|`controlName`|Requires the Control at the current level to have the given name. Takes both "proper" names ([`InputControl.name`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_name)) and aliases ([`InputControl.aliases`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_aliases)) into account.<br><br>This field can also be a wildcard (`*`) to match any name.|`MyGamepad/buttonSouth`<br><br>`*/{PrimaryAction}` (match `PrimaryAction` usage on Devices with any name)|
|`#(displayName)`|Requires the Control at the current level to have the given display name (i.e. [`InputControl.displayName`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_displayName)). The display name may contain whitespace and symbols.|`<Keyboard>/#(a)` (matches the key that generates the "a" character, if any, according to the current keyboard layout).<br><br>`<Gamepad>/#(Cross)`|

You can access the literal path of a given control via its [`InputControl.path`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_path) property.

## Control state

Each Control is connected to a block of memory that is considered the Control's "state". You can query the size, format, and location of this block of memory from a Control through the [`InputControl.stateBlock`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_stateBlock) property.

The state of Controls is stored in unmanaged memory that the Input System handles internally. All Devices added to the system share one block of unmanaged memory that contains the state of all the Controls on the Devices.

A Control's state might not be stored in the natural format for that Control. For example, the system often represents buttons as bitfields, and axis controls as 8-bit or 16-bit integer values. This format is determined by the combination of platform, hardware, and drivers. Each Control knows the format of its storage and how to translate the values as needed. The Input System uses [layouts](Layouts.md) to understand this representation.

You can access the current state of a Control through its [`ReadValue`](../api/UnityEngine.InputSystem.InputControl-1.html#UnityEngine_InputSystem_InputControl_1_ReadValue) method.

```CSharp
Gamepad.current.leftStick.x.ReadValue();
```

Each type of Control has a specific type of values that it returns, regardless of how many different types of formats it supports for its state. You can access this value type through the [`InputControl.valueType`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_valueType) property.

Reading a value from a Control might apply one or more value Processors. See documentation on [Processors](Processors.md) for more information.

[//]: # (#### Default State - TODO)

[//]: # (#### Reading State vs Reading Values - TODO)

#### Recording state history

You might want to access the history of value changes on a Control (for example, in order to compute exit velocity on a touch release).

To record state changes over time, you can use [`InputStateHistory`](../api/UnityEngine.InputSystem.LowLevel.InputStateHistory.html) or [`InputStateHistory<TValue>`](../api/UnityEngine.InputSystem.LowLevel.InputStateHistory-1.html). The latter restricts Controls to those of a specific value type, which in turn simplifies some of the API.

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

// You can also record state changes manually, which allows
// storing arbitrary histories in InputStateHistory.
// NOTE: This records a value change that didn't actually happen on the control.
history.RecordStateChange(Touchscreen.current.primaryTouch.position,
    new Vector2(0.123f, 0.234f));

// State histories allocate unmanaged memory and need to be disposed.
history.Dispose();
```

For example, if you want to have the last 100 samples of the left stick on the gamepad available, you can use this code:

```CSharp
var history = new InputStateHistory<Vector2>(Gamepad.current.leftStick);
history.historyDepth = 100;
history.StartRecording();
```

## Control actuation

A Control is considered actuated when it has moved away from its default state in such a way that it affects the actual value of the Control. You can query whether a Control is currently actuated using [`IsActuated`](../api/UnityEngine.InputSystem.InputControlExtensions.html#UnityEngine_InputSystem_InputControlExtensions_IsActuated_UnityEngine_InputSystem_InputControl_System_Single_).

```CSharp
// Check if leftStick is currently actuated.
if (Gamepad.current.leftStick.IsActuated())
    Debug.Log("Left Stick is actuated");
```

It can be useful to determine not just whether a Control is actuated at all, but also the amount by which it is actuated (that is, its magnitude). For example, for a [`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html) this would be the length of the vector, whereas for a button it is the raw, absolute floating-point value.

In general, the current magnitude of a Control is always >= 0. However, a Control might not have a meaningful magnitude, in which case it returns -1. Any negative value should be considered an invalid magnitude.

You can query the current amount of actuation using [`EvaluateMagnitude`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_EvaluateMagnitude).

```CSharp
// Check if left stick is actuated more than a quarter of its motion range.
if (Gamepad.current.leftStick.EvaluateMagnitude() > 0.25f)
    Debug.Log("Left Stick actuated past 25%");
```

There are two mechanisms that most notably make use of Control actuation:

- [Interactive rebinding](ActionBindings.md#runtime-rebinding) (`InputActionRebindingExceptions.RebindOperation`) uses it to select between multiple suitable Controls to find the one that is actuated the most.
- [Disambiguation](ActionBindings.md#disambiguation) between multiple Controls that are bound to the same action uses it to decide which Control gets to drive the action.

## Noisy Controls

The Input System can label a Control as "noisy". You can query this using the  [`InputControl.noisy`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_noisy) property.

Noisy Controls are those that can change value without any actual or intentional user interaction required. A good example of this is a gravity sensor in a cellphone. Even if the cellphone is perfectly still, there are usually fluctuations in gravity readings. Another example are orientation readings from an HMD.

If a Control is marked as noisy, it means that:

1. The Control is not considered for [interactive rebinding](ActionBindings.md#runtime-rebinding). [`InputActionRebindingExceptions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) ignores the Control by default (you can bypass this using [`WithoutIgnoringNoisyControls`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithoutIgnoringNoisyControls)).
2. If enabled in the Project Settings, the system performs additional event filtering, then calls [`InputDevice.MakeCurrent`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_MakeCurrent). If an input event for a Device contains no state change on a Control that is not marked noisy, then the Device will not be made current based on the event. This avoids, for example, a plugged in PS4 controller constantly making itself the current gamepad ([`Gamepad.current`](../api/UnityEngine.InputSystem.Gamepad.html#UnityEngine_InputSystem_Gamepad_current)) due to its sensors constantly feeding data into the system.
3. When the application loses focus and Devices are [reset](Devices.md#device-resets) as a result, the state of noisy Controls will be preserved as is. This ensures that sensor readinds will remain at their last value rather than being reset to default values.

>**Note**: If any Control on a Device is noisy, the Device itself is flagged as noisy.

Parallel to the [`input state`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_currentStatePtr) and the [`default state`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_defaultStatePtr) that the Input System keeps for all Devices currently present, it also maintains a [`noise mask`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_noiseMaskPtr) in which only bits for state that is __not__ noise are set. This can be used to very efficiently mask out noise in input.

## Synthetic Controls

A synthetic Control is a Control that doesn't correspond to an actual physical control on a device (for example the `left`, `right`, `up`, and `down` child Controls on a [`StickControl`](../api/UnityEngine.InputSystem.Controls.StickControl.html)). These Controls synthesize input from other, actual physical Controls and present it in a different way (in this example, they allow you to treat the individual directions of a stick as buttons).

Whether a given Control is synthetic is indicated by its [`InputControl.synthetic`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_synthetic) property.

The system considers synthetic Controls for [interactive rebinding](ActionBindings.md#runtime-rebinding) but always favors non-synthetic Controls. If both a synthetic and a non-synthetic Control that are a potential match exist, the non-synthetic Control wins by default. This makes it possible to interactively bind to `<Gamepad>/leftStick/left`, for example, but also makes it possible to bind to `<Gamepad>/leftStickPress` without getting interference from the synthetic buttons on the stick.
