---
uid: input-system-controls
---
# Controls

An input control represents a source of values. These values can be of any structured or primitive type. The only requirement is that the type is [blittable](https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types).

> [!NOTE]
> Controls are for input only. Output and configuration items on input devices are not represented as controls.

## Identification

Each control is identified by its [name](xref:UnityEngine.InputSystem.InputControl.name). Optionally, it can also have a different [display name](xref:UnityEngine.InputSystem.InputControl.displayName). For example, the right-hand face button closest to the touchpad on a PlayStation DualShock 4 controller has the control name "buttonWest" and the display name "Square".

Additionally, a control might have one or more aliases which provide alternative names for the control. You can access the aliases for a specific control through its [`aliases`](xref:UnityEngine.InputSystem.InputControl.aliases) property.

Finally, a control might also have a short display name which can be accessed through the [`shortDisplayName`](xref:UnityEngine.InputSystem.InputControl.shortDisplayName) property. For example, the short display name for the left mouse button is "LMB".

## Control hierarchies

Controls can form hierarchies. The root of a control hierarchy is always a [device](xref:input-system-devices).

The setup of hierarchies is exclusively controlled through [layouts](xref:input-system-layouts).

You can access the parent of a control using its [`parent`](xref:UnityEngine.InputSystem.InputControl.parent) property, and its children using [`children`](xref:UnityEngine.InputSystem.InputControl.children). To access the flattened hierarchy of all controls on a device, use [`allControls`](xref:UnityEngine.InputSystem.InputDevice.allControls).

## Control types

All controls are based on the [`InputControl`](xref:UnityEngine.InputSystem.InputControl) base class. Most concrete implementations are based on [InputControl<TValue>](xref:UnityEngine.InputSystem.InputControl`1).

The Input System provides the following types of controls out of the box:

|Control Type|Description|Example|
|------------|-----------|-------|
|[`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl)|A 1D floating-point axis.|[`Gamepad.leftStick.x`](xref:UnityEngine.InputSystem.Controls.Vector2Control.x)|
|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|A button expressed as a floating-point value. Whether the button can have a value other than 0 or 1 depends on the underlying representation. For example, gamepad trigger buttons can have values other than 0 and 1, but gamepad face buttons generally can't.|[`Mouse.leftButton`](xref:UnityEngine.InputSystem.Mouse.leftButton)|
|[`KeyControl`](xref:UnityEngine.InputSystem.Controls.KeyControl)|A specialized button that represents a key on a [`Keyboard`](xref:UnityEngine.InputSystem.Keyboard). Keys have an associated [`keyCode`](xref:UnityEngine.InputSystem.Controls.KeyControl.keyCode) and, unlike other types of controls, change their display name in accordance to the currently active system-wide keyboard layout. See the [Keyboard](xref:input-system-keyboard) documentation for details.|[`Keyboard.aKey`](xref:UnityEngine.InputSystem.Keyboard.aKey)|
|[`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|A 2D floating-point vector.|[`Pointer.position`](xref:UnityEngine.InputSystem.Pointer.position)|
|[`Vector3Control`](xref:UnityEngine.InputSystem.Controls.Vector3Control)|A 3D floating-point vector.|[`Accelerometer.acceleration`](xref:UnityEngine.InputSystem.Accelerometer.acceleration)|
|[`QuaternionControl`](xref:UnityEngine.InputSystem.Controls.QuaternionControl)|A 3D rotation.|[`AttitudeSensor.attitude`](xref:UnityEngine.InputSystem.AttitudeSensor.attitude)|
|[`IntegerControl`](xref:UnityEngine.InputSystem.Controls.IntegerControl)|An integer value.|[`Touchscreen.primaryTouch.touchId`](xref:UnityEngine.InputSystem.Controls.TouchControl.touchId)|
|[`StickControl`](xref:UnityEngine.InputSystem.Controls.StickControl)|A 2D stick control like the thumbsticks on gamepads or the stick control of a joystick.|[`Gamepad.rightStick`](xref:UnityEngine.InputSystem.Gamepad.rightStick)|
|[`DpadControl`](xref:UnityEngine.InputSystem.Controls.DpadControl)|A 4-way button control like the D-pad on gamepads or hatswitches on joysticks.|[`Gamepad.dpad`](xref:UnityEngine.InputSystem.Gamepad.dpad)|
|[`TouchControl`](xref:UnityEngine.InputSystem.Controls.TouchControl)|A control that represents all the properties of a touch on a [touch screen](xref:input-system-touch).|[`Touchscreen.primaryTouch`](xref:UnityEngine.InputSystem.Touchscreen.primaryTouch)|

You can browse the set of all registered control layouts in the [input debugger](xref:input-system-debugging#debugging-layouts).

## Control usages

A control can have one or more associated usages. A usage is a string that denotes the control's intended use. An example of a control usage is `Submit`, which labels a control that is commonly used to confirm a selection in the UI. On a gamepad, this usage is commonly found on the `buttonSouth` control.

You can access a control's usages using the [`InputControl.usages`](xref:UnityEngine.InputSystem.InputControl.usages) property.

Usages can be arbitrary strings. However, a certain set of usages is very commonly used and comes predefined in the API in the [`CommonUsages`](xref:UnityEngine.InputSystem.CommonUsages) static class.

## Control paths

The Input System can look up controls using textual paths. [Bindings](xref:input-system-action-bindings) on Input Actions rely on this feature to identify the control(s) they read input from. For example, `<Gamepad>/leftStick/x` means "X control on left stick of gamepad". However, you can also use them for lookup directly on controls and devices, or to let the Input System search for controls among all devices using [`InputSystem.FindControls`](xref:UnityEngine.InputSystem.InputSystem.FindControls(System.String)):

```CSharp
var gamepad = Gamepad.all[0];
var leftStickX = gamepad["leftStick/x"];
var submitButton = gamepad["{Submit}"];
var allSubmitButtons = InputSystem.FindControls("*/{Submit}");
```

Control paths resemble file system paths: they contain components separated by a forward slash (`/`):

    component/component...

Each component itself contains a set of [fields](#component-fields) with its own syntax. Each field is individually optional, provided that at least one of the fields is present as either a name or a wildcard:

```structured text
<layoutName>{usageName}#(displayName)controlName
```

You can access the literal path of a given control via its [`InputControl.path`](xref:UnityEngine.InputSystem.InputControl.path) property. If you need to, you can manually parse a control path into its components using the [`InputControlPath.Parse(path)`](xref:UnityEngine.InputSystem.InputControlPath.Parse(System.String)) API:

```CSharp
var parsed = InputControlPath.Parse("<XRController>{LeftHand}/trigger").ToArray();

Debug.Log(parsed.Length); // Prints 2.
Debug.Log(parsed[0].layout); // Prints "XRController".
Debug.Log(parsed[0].name); // Prints an empty string.
Debug.Log(parsed[0].usages.First()); // Prints "LeftHand".
Debug.Log(parsed[1].layout); // Prints null.
Debug.Log(parsed[1].name); // Prints "trigger".
```

### Component fields

All fields are case-insensitive.

The following table explains the use of each field:

|Field|Description|Related links|
|-----|-----|------------------|
|`layoutName`|The name of the layout that the control must be based on. The actual layout of the control may be the same or a layout *based* on the given layout. For example, `<Gamepad>`.|The [Layouts](xref:input-system-layouts) user manual topic<br/><br/>The [InputControlLayout](xref:UnityEngine.InputSystem.Layouts.InputControlLayout) class|
|`usageName`|Works differently for controls and devices:<ul><li>When used on a device (the first component of a path), it requires the device to have the given usage.  For example, `<XRController>{LeftHand}/trigger`.</li><li>For looking up a control, the usage field is currently restricted to the path component immediately following the device (the second component in the path). It finds the control on the device that has the given usage. The control can be anywhere in the control hierarchy of the device. For example, `<Gamepad>/{Submit}`.</li></ul>|The [Device usages](xref:input-system-devices#device-usages) user manual topic<br/><br/>The [Control usages](#control-usages) topic on this page<<br/><br/>The [InputControl.usages](xref:UnityEngine.InputSystem.Layouts.InputControlLayout) property|
|`displayName`|Requires the control at the current level to have the given display name. The display name may contain whitespace and symbols. For example:<ul><li>`<Keyboard>/#(a)` matches the key that generates the "a" character, if any, according to the current keyboard layout. </li><li>`<Gamepad>/#(Cross)` matches the button named "Cross" on the Gamepad.</li></ul>|The [Identification](#identification) topic on this page<br/><br/>The [InputControl.displayName](xref:UnityEngine.InputSystem.InputControl.displayName) property|
|`controlName`|Requires the control at the current level to have the given name. Takes both "proper" names such as `MyGamepad/buttonSouth`, and aliases such as `MyGamepad/South` into account.<br><br>This field can also be a wildcard (`*`) to match any name. For example, `*/{PrimaryAction}` matches any `PrimaryAction` usage on devices with any name.|The [Identification](#identification) topic on this page<br/><br/>The [InputControl.name](xref:UnityEngine.InputSystem.InputControl.name) property for "proper" names<br/><br/>The [InputControl.aliases](xref:UnityEngine.InputSystem.InputControl.aliases) property for aliases|

Here are several examples of control paths:

```csharp
// Matches all gamepads (also gamepads *based* on the Gamepad layout):
"<Gamepad>"
// Matches the "Submit" control on all devices:
"*/"
// Matches the key that prints the "a" character on the current keyboard layout:
"<Keyboard>/#(a)"
// Matches the X axis of the left stick on a gamepad.
"<Gamepad>/leftStick/x"
// Matches the orientation control of the right-hand XR controller:
"<XRController>/orientation"
// Matches all buttons on a gamepad.
"<Gamepad>/<Button>"
```

## Control state

Each control is connected to a block of memory that is considered the control's "state". You can query the size, format, and location of this block of memory from a control through the [`InputControl.stateBlock`](xref:UnityEngine.InputSystem.InputControl.stateBlock) property.

The Input System stores the state of controls in unmanaged memory that it handles internally. All devices added to the system share one block of unmanaged memory that contains the state of all the controls on the devices.

A control's state might not be stored in the natural format for that control. For example, the system often represents buttons as bitfields, and axis controls as 8-bit or 16-bit integer values. This format is determined by the combination of platform, hardware, and drivers. Each control knows the format of its storage and how to translate the values as needed. The Input System uses [layouts](xref:input-system-layouts) to understand this representation.

You can access the current state of a control through its [`ReadValue`](xref:UnityEngine.InputSystem.InputControl`1.ReadValue) method.

```CSharp
Gamepad.current.leftStick.x.ReadValue();
```

Each type of control has a specific type of values that it returns, regardless of how many different types of formats it supports for its state. You can access this value type through the [`InputControl.valueType`](xref:UnityEngine.InputSystem.InputControl.valueType) property.

Reading a value from a control might apply one or more value Processors. Refer to the documentation on [Processors](UsingProcessors.md) for more information.

[//]: # (#### Default State - TODO)

[//]: # (#### Reading State vs Reading Values - TODO)

#### Recording state history

If you want to access the history of value changes on a control (for example, in order to compute exit velocity on a touch release), you can record state changes over time with [`InputStateHistory`](xref:UnityEngine.InputSystem.LowLevel.InputStateHistory) or [`InputStateHistory<TValue>`](xref:UnityEngine.InputSystem.LowLevel.InputStateHistory`1). The latter restricts controls to those of a specific value type, which in turn simplifies some of the API.

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

A control is considered "actuated" when it has moved away from its default state in such a way that it affects the actual value of the control. Use [`IsActuated`](xref:UnityEngine.InputSystem.InputControlExtensions.IsActuated(UnityEngine.InputSystem.InputControl,System.Single)) to query whether a control is currently actuated.

```CSharp
// Check if leftStick is currently actuated.
if (Gamepad.current.leftStick.IsActuated())
    Debug.Log("Left Stick is actuated");
```

It can be useful to determine not just whether a control is actuated at all, but also the amount by which it is actuated (that is, its magnitude). For example, for a [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) this would be the length of the vector, whereas for a button it is the raw, absolute floating-point value.

In general, the current magnitude of a control is always >= 0. However, a control might not have a meaningful magnitude, in which case it returns -1. Any negative value should be considered an invalid magnitude.

You can query the current amount of actuation using [`EvaluateMagnitude`](xref:UnityEngine.InputSystem.InputControl.EvaluateMagnitude).

```CSharp
// Check if left stick is actuated more than a quarter of its motion range.
if (Gamepad.current.leftStick.EvaluateMagnitude() > 0.25f)
    Debug.Log("Left Stick actuated past 25%");
```

These two mechanisms use control actuation:

- [Interactive rebinding](xref:input-system-action-bindings#interactive-rebinding) (`InputActionRebindingExceptions.RebindOperation`) uses it to select between multiple suitable controls to find the one that is actuated the most.
- [Conflict resolution](xref:input-system-action-bindings#conflicting-inputs) between multiple controls that are bound to the same action uses it to decide which control gets to drive the action.

## Noisy controls

The Input System can label a control as "noisy", meaning that they can change value without needing any actual or intentional user interaction, such as a gravity [sensor](xref:UnityEngine.InputSystem.Sensor) in a cellphone, or taking orientation readings from an [XR head-mounted display](xref:UnityEngine.InputSystem.XR.XRHMD).

For example, the PS4 controller has a gyroscope sensor built into the device which constantly feeds data about the angular velocity of the device, even if the device just sits there without user interaction. Conversely, the controller's sticks and buttons require user interaction to produce non-default values.

If a control is marked as noisy, it means that:

- The control is not considered for [interactive rebinding](xref:input-system-action-bindings#interactive-rebinding). The [`InputActionRebindingExceptions.RebindingOperation`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation) ignores the control by default (although you can bypass this using [`WithoutIgnoringNoisyControls`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.WithoutIgnoringNoisyControls)).

- If enabled in the Project Settings, the system performs additional event filtering, then calls [`InputDevice.MakeCurrent`](xref:UnityEngine.InputSystem.InputDevice.MakeCurrent). If an input event for a device contains no state change on a control that is not marked noisy, then the device will not be made current based on the event. This avoids, for example, a plugged in PS4 controller constantly making itself the current gamepad ([`Gamepad.current`](xref:UnityEngine.InputSystem.Gamepad.current)) due to its sensors constantly feeding data into the system.

- When the application loses focus and devices are [reset](xref:input-system-devices#device-resets) as a result, the state of noisy controls will be preserved as is. This ensures that sensor readings will remain at their last value rather than being reset to default values. However, while other controls are reset to their default value, noisy controls will not be reset but rather remain at their current value (unless the device is [running in the background](xref:input-system-devices#background-and-focus-change-behavior)). This is based on the assumption that noisy controls most often represent sensor values and snapping the last sampling value back to default will usually have undesirable effects on an application's simulation logic.

> [!NOTE]
> To query whether a control is noisy, use the [`InputControl.noisy`](xref:UnityEngine.InputSystem.InputControl.noisy) property.
>
> If any control on a device is noisy, the device itself is flagged as noisy.

### Noise masks

Parallel to the [`input state`](xref:UnityEngine.InputSystem.InputControl.currentStatePtr) and the [`default state`](xref:UnityEngine.InputSystem.InputControl.defaultStatePtr) that the Input System keeps for all devices currently present, it also maintains a [`noise mask`](xref:UnityEngine.InputSystem.InputControl.noiseMaskPtr) in which only bits for state that is __not__ noise are set. This can be used to very efficiently mask out noise in input.

## Synthetic controls

A synthetic control is an input control that doesn't correspond to an actual physical control on a device (for example the `left`, `right`, `up`, and `down` child controls on a [`StickControl`](xref:UnityEngine.InputSystem.Controls.StickControl)). These controls synthesize input from other, actual physical controls and present it in a different way (in this example, they allow you to treat the individual directions of a stick as buttons).

The system considers synthetic controls for [interactive rebinding](xref:input-system-action-bindings#interactive-rebinding) but always favors non-synthetic controls. If both a synthetic and a non-synthetic control that are a potential match exist (for example, `<Gamepad>/leftStick/x` and `<Gamepad>/leftStick/left`), the non-synthetic control (`<Gamepad>/leftStick/x`) wins by default. This makes it possible to interactively bind to `<Gamepad>/leftStick/left`, for example, but also makes it possible to bind to `<Gamepad>/leftStickPress` without getting interference from the synthetic buttons on the stick.

> [!NOTE]
> To query whether a control is synthetic, use the [`InputControl.synthetic`](xref:UnityEngine.InputSystem.InputControl.synthetic) property.


## Performance Optimization

### Avoiding defensive copies

Use [`InputControl<T>.value`](xref:UnityEngine.InputSystem.InputControl`1.value) instead of [`InputControl<T>.ReadValue`](xref:UnityEngine.InputSystem.InputControl`1.ReadValue) to avoid creating a copy of the control state on every call. This is because `InputControl<T>.value` returns the value as `ref readonly` while `InputControl<T>.ReadValue` always makes a copy. Note that this optimization only applies if the call site assigns the return value to a variable that has been declared `ref readonly`. Otherwise, a copy is made as before.

Additionally, be aware of defensive copies that the compiler can allocate when it is unable to determine that it can safely use the read-only reference. This means that if the compiler can't determine that the reference won't be changed, it will create a defensive copy. For more details, refer to the [.NET guidance on reducing memory allocations](https://learn.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code#use-ref-readonly-return-statements).


### Control Value Caching

Enable the `USE_READ_VALUE_CACHING` internal feature flag to get the Input System to switch to an optimized path for reading control values. This path efficiently marks controls as "stale" when they have been actuated. Subsequent calls to [`InputControl<T>.ReadValue`](xref:UnityEngine.InputSystem.InputControl`1.ReadValue) will only apply control processing when there have been changes to that control or in the case of any [hard-coded processing](xref:input-system-processors#processors-on-controls) that might exist on the control. For example, [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) has built-in inversion, normalization, scaling, and any other processors added to the controls' [processor stack](xref:input-system-processors#processors-on-controls).

> [!NOTE]
> Performance improvements **are currently not guaranteed** for all use cases. Even though this performance path marks controls as "stale" in an efficient way, it still has an overhead which can degrade performance in some cases.

Positive performance impact can occur when:
- Reading from controls that don't change frequently.
- If the controls change every frame, are being read and have actions bound to them as well. For example, reading `leftStick`, `leftStick.x` and `leftStick.left` when there's an action with composite bindings on a Gamepad.

Negative performance impact can occur when:
- Reading from controls that change frequently but which have no bound actions.
- No readings from controls that change frequently.

`USE_READ_VALUE_CACHING` is not enabled by default because it can result in the following minor behavioral changes:
- For control processors that use a global state with cached value optimization, changing the global state of a control processor will have no effect. Reading the control value will only ever return a new value if the physical control has been actuated.

    This behavior differs from using global states without cached value optimizations, in which you can read the control value, change the global state, read the control value again, and get a new value due to the fact that the control processor runs on every call.
- Writing to device state using low-level APIs like [`InputControl<T>.WriteValueIntoState`](xref:UnityEngine.InputSystem.InputControl`1.WriteValueIntoState(`0,System.Void*)) doesn't set the stale flag and subsequent calls to [`InputControl<T>.value`](xref:UnityEngine.InputSystem.InputControl`1.value) won't reflect those changes.
- After changing properties on [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) the [`ApplyParameterChanges`](xref:UnityEngine.InputSystem.InputControl.ApplyParameterChanges) method has to be called to invalidate cached values.

Processors that must run on every read can set their caching policy to [EvaluateOnEveryRead](xref:UnityEngine.InputSystem.InputProcessor.CachingPolicy.EvaluateOnEveryRead), which disables caching on controls that are using such processors.

You can enable the `PARANOID_READ_VALUE_CACHING_CHECKS` internal feature flag to compare cached and uncached values on every read. If they don't match, the check logs an error.

### Optimized control read value

Enable the `USE_OPTIMIZED_CONTROLS` internal feature flag to get the Input System to access state memory faster for some control instances. This is very specific optimization and should be used with caution.

> [!NOTE]
> This optimization has a performance impact on `PlayMode` because the Input System performs extra checks in order to ensure that the controls have the correct memory representation during development. If you see a performance drop in `PlayMode` when using this optimization, that is expected at this stage.

Most controls are flexible with regards to memory representation. For example, [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) can be one bit, multiple bits, a float, or in [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) where `x` and `y` can have different memory representation. However, most controls use common memory representation patterns, such as [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl), which uses floats or single bytes. Another example is [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) which consists of two consecutive floats in memory.

If a control matches a common representation, you can bypass reading its children's controls and cast the memory directly to the common representation. For example if [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) has two consecutive floats in memory, you can bypass reading `x` and `y` separately and just cast the state memory to `Vector2`.

> [!NOTE]
> This optimization only works if the controls don't need any processing applied, such as `invert`, `clamp`, `normalize`, `scale` or any other processor. If any of these are applied to the control, the control will be read as usual without any optimization.

It is important to explicitly call [`InputControl.ApplyParameterChanges()`](xref:UnityEngine.InputSystem.InputControl.ApplyParameterChanges) to ensure [`InputControl.optimizedControlDataType`](xref:UnityEngine.InputSystem.InputControl.optimizedControlDataType) is updated to the correct memory representation for these specific changes:
- Configuration changes after calling [`InputControl.FinishSetup()`](xref:UnityEngine.InputSystem.InputControl.FinishSetup*).
- Changing parameters such as [`AxisControl.invert`](xref:UnityEngine.InputSystem.Controls.AxisControl.invert), [`AxisControl.clamp`](xref:UnityEngine.InputSystem.Controls.AxisControl.clamp), [`AxisControl.normalize`](xref:UnityEngine.InputSystem.Controls.AxisControl.normalize), [`AxisControl.scale`](xref:UnityEngine.InputSystem.Controls.AxisControl.scale) or changing processors. The memory representation needs to be recalculated after these changes to ensure that the control is no longer optimized. Otherwise, the control will be read with wrong values.

The optimized controls work as follows:
- A potential memory representation is set using [`InputControl.CalculateOptimizedControlDataType()`](xref:UnityEngine.InputSystem.InputControl.CalculateOptimizedControlDataType)
- Its memory representation is stored in [`InputControl.optimizedControlDataType`](xref:UnityEngine.InputSystem.InputControl.optimizedControlDataType)
- Finally,  [`ReadUnprocessedValueFromState`](xref:UnityEngine.InputSystem.InputControl`1.ReadUnprocessedValueFromState*) uses the optimized memory representation to decide if it should cast to memory directly instead of reading every children control on its own to reconstruct the controls state.
