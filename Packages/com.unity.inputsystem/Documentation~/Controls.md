---
uid: input-system-controls
---
# Controls

An Input Control represents a source of values. These values can be of any structured or primitive type. The only requirement is that the type is [blittable](https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types).

> [!NOTE]
> Controls are for input only. Output and configuration items on Input Devices are not represented as Controls.

## Identification

Each Control is identified by a name ([`InputControl.name`](xref:UnityEngine.InputSystem.InputControl.name)) and can optionally have a display name ([`InputControl.displayName`](xref:UnityEngine.InputSystem.InputControl.displayName)) that differs from the Control name. For example, the right-hand face button closest to the touchpad on a PlayStation DualShock 4 controller has the control name "buttonWest" and the display name "Square".

Additionally, a Control might have one or more aliases which provide alternative names for the Control. You can access the aliases for a specific Control through its [`InputControl.aliases`](xref:UnityEngine.InputSystem.InputControl.aliases) property.

Finally, a Control might also have a short display name which can be accessed through the [`InputControl.shortDisplayName`](xref:UnityEngine.InputSystem.InputControl.shortDisplayName) property. For example, the short display name for the left mouse button is "LMB".

## Control hierarchies

Controls can form hierarchies. The root of a Control hierarchy is always a [Device](xref:input-system-devices).

The setup of hierarchies is exclusively controlled through [layouts](xref:input-system-layouts).

You can access the parent of a Control using [`InputControl.parent`](xref:UnityEngine.InputSystem.InputControl.parent), and its children using [`InputControl.children`](xref:UnityEngine.InputSystem.InputControl.children). To access the flattened hierarchy of all Controls on a Device, use [`InputDevice.allControls`](xref:UnityEngine.InputSystem.InputDevice.allControls).

## Control types

All controls are based on the [`InputControl`](xref:UnityEngine.InputSystem.InputControl) base class. Most concrete implementations are based on [`InputControl<TValue>`](xref:UnityEngine.InputSystem.InputControl`1).

The Input System provides the following types of controls out of the box:

|Control Type|Description|Example|
|------------|-----------|-------|
|[`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl)|A 1D floating-point axis.|[`Gamepad.leftStick.x`](xref:UnityEngine.InputSystem.Controls.Vector2Control.x)|
|[`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl)|A button expressed as a floating-point value. Whether the button can have a value other than 0 or 1 depends on the underlying representation. For example, gamepad trigger buttons can have values other than 0 and 1, but gamepad face buttons generally can't.|[`Mouse.leftButton`](xref:UnityEngine.InputSystem.Mouse.leftButton)|
|[`KeyControl`](xref:UnityEngine.InputSystem.Controls.KeyControl)|A specialized button that represents a key on a [`Keyboard`](xref:UnityEngine.InputSystem.Keyboard). Keys have an associated [`keyCode`](xref:UnityEngine.InputSystem.Controls.KeyControl.keyCode) and, unlike other types of Controls, change their display name in accordance to the currently active system-wide keyboard layout. See the [Keyboard](xref:input-system-keyboard) documentation for details.|[`Keyboard.aKey`](xref:UnityEngine.InputSystem.Keyboard.aKey)|
|[`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control)|A 2D floating-point vector.|[`Pointer.position`](xref:UnityEngine.InputSystem.Pointer.position)|
|[`Vector3Control`](xref:UnityEngine.InputSystem.Controls.Vector3Control)|A 3D floating-point vector.|[`Accelerometer.acceleration`](xref:UnityEngine.InputSystem.Accelerometer.acceleration)|
|[`QuaternionControl`](xref:UnityEngine.InputSystem.Controls.QuaternionControl)|A 3D rotation.|[`AttitudeSensor.attitude`](xref:UnityEngine.InputSystem.AttitudeSensor.attitude)|
|[`IntegerControl`](xref:UnityEngine.InputSystem.Controls.IntegerControl)|An integer value.|[`Touchscreen.primaryTouch.touchId`](xref:UnityEngine.InputSystem.Controls.TouchControl.touchId)|
|[`StickControl`](xref:UnityEngine.InputSystem.Controls.StickControl)|A 2D stick control like the thumbsticks on gamepads or the stick control of a joystick.|[`Gamepad.rightStick`](xref:UnityEngine.InputSystem.Gamepad.rightStick)|
|[`DpadControl`](xref:UnityEngine.InputSystem.Controls.DpadControl)|A 4-way button control like the D-pad on gamepads or hatswitches on joysticks.|[`Gamepad.dpad`](xref:UnityEngine.InputSystem.Gamepad.dpad)|
|[`TouchControl`](xref:UnityEngine.InputSystem.Controls.TouchControl)|A control that represents all the properties of a touch on a [touch screen](xref:input-system-touch).|[`Touchscreen.primaryTouch`](xref:UnityEngine.InputSystem.Touchscreen.primaryTouch)|

You can browse the set of all registered control layouts in the [input debugger](xref:input-system-debugging#debugging-layouts).

## Control usages

A Control can have one or more associated usages. A usage is a string that denotes the Control's intended use. An example of a Control usage is `Submit`, which labels a Control that is commonly used to confirm a selection in the UI. On a gamepad, this usage is commonly found on the `buttonSouth` Control.

You can access a Control's usages using the [`InputControl.usages`](xref:UnityEngine.InputSystem.InputControl.usages) property.

Usages can be arbitrary strings. However, a certain set of usages is very commonly used and comes predefined in the API in the form of the [`CommonUsages`](xref:UnityEngine.InputSystem.CommonUsages) static class. Check out the [`CommonUsages` scripting API page](xref:UnityEngine.InputSystem.CommonUsages) for an overview.

## Control paths

The Input System can look up Controls using textual paths. [Bindings](xref:input-system-action-bindings) on Input Actions rely on this feature to identify the Control(s) they read input from. For example, `<Gamepad>/leftStick/x` means "X Control on left stick of gamepad". However, you can also use them for lookup directly on Controls and Devices, or to let the Input System search for Controls among all devices using [`InputSystem.FindControls`](xref:UnityEngine.InputSystem.InputSystem.FindControls(System.String)):

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

|Field|Description|Related&nbsp;links|
|-----|-----|------------------|
|`layoutName`|The name of the layout that the control must be based on. The actual layout of the control may be the same or a layout *based* on the given layout. For example, `<Gamepad>`.|The [Layouts](xref:input-system-layouts) user manual topic<br/><br/>The [InputControlLayout](xref:UnityEngine.InputSystem.Layouts.InputControlLayout) class|
|`usageName`|Works differently for Controls and Devices:<ul><li>When used on a Device (the first component of a path), it requires the device to have the given usage.  For example, `<XRController>{LeftHand}/trigger`.</li><li>For looking up a Control, the usage field is currently restricted to the path component immediately following the Device (the second component in the path). It finds the Control on the Device that has the given usage. The Control can be anywhere in the Control hierarchy of the Device. For example, `<Gamepad>/{Submit}`.</li></ul>|The [Device usages](xref:input-system-devices#device-usages) user manual topic<br/><br/>The [Control usages](#control-usages) topic on this page<<br/><br/>The [InputControl.usages](xref:UnityEngine.InputSystem.Layouts.InputControlLayout) property|
|`displayName`|Requires the Control at the current level to have the given display name. The display name may contain whitespace and symbols. For example:<ul><li>`<Keyboard>/#(a)` matches the key that generates the "a" character, if any, according to the current keyboard layout. </li><li>`<Gamepad>/#(Cross)` matches the button named "Cross" on the Gamepad.</li></ul>|The [Identification](#identification) topic on this page<br/><br/>The [InputControl.displayName](xref:UnityEngine.InputSystem.InputControl.displayName) property|
|`controlName`|Requires the Control at the current level to have the given name. Takes both "proper" names such as `MyGamepad/buttonSouth`, and aliases such as `MyGamepad/South` into account.<br><br>This field can also be a wildcard (`*`) to match any name. For example, `*/{PrimaryAction}` matches any `PrimaryAction` usage on Devices with any name.|The [Identification](#identification) topic on this page<br/><br/>The [InputControl.name](xref:UnityEngine.InputSystem.InputControl.name) property for "proper" names<br/><br/>The [InputControl.aliases](xref:UnityEngine.InputSystem.InputControl.aliases) property for aliases|

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

Each Control is connected to a block of memory that is considered the Control's "state". You can query the size, format, and location of this block of memory from a Control through the [`InputControl.stateBlock`](xref:UnityEngine.InputSystem.InputControl.stateBlock) property.

The state of Controls is stored in unmanaged memory that the Input System handles internally. All Devices added to the system share one block of unmanaged memory that contains the state of all the Controls on the Devices.

A Control's state might not be stored in the natural format for that Control. For example, the system often represents buttons as bitfields, and axis controls as 8-bit or 16-bit integer values. This format is determined by the combination of platform, hardware, and drivers. Each Control knows the format of its storage and how to translate the values as needed. The Input System uses [layouts](xref:input-system-layouts) to understand this representation.

You can access the current state of a Control through its [`ReadValue`](xref:UnityEngine.InputSystem.InputControl`1.ReadValue) method.

```CSharp
Gamepad.current.leftStick.x.ReadValue();
```

Each type of Control has a specific type of values that it returns, regardless of how many different types of formats it supports for its state. You can access this value type through the [`InputControl.valueType`](xref:UnityEngine.InputSystem.InputControl.valueType) property.

Reading a value from a Control might apply one or more value Processors. Refer to the documentation on [Processors](UsingProcessors.md) for more information.

[//]: # (#### Default State - TODO)

[//]: # (#### Reading State vs Reading Values - TODO)

#### Recording state history

If you want to access the history of value changes on a Control (for example, in order to compute exit velocity on a touch release), you can record state changes over time with [`InputStateHistory`](xref:UnityEngine.InputSystem.LowLevel.InputStateHistory) or [`InputStateHistory<TValue>`](xref:UnityEngine.InputSystem.LowLevel.InputStateHistory`1). The latter restricts Controls to those of a specific value type, which in turn simplifies some of the API.

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

A Control is considered actuated when it has moved away from its default state in such a way that it affects the actual value of the Control. You can query whether a Control is currently actuated using [`IsActuated`](xref:UnityEngine.InputSystem.InputControlExtensions.IsActuated(UnityEngine.InputSystem.InputControl,System.Single)).

```CSharp
// Check if leftStick is currently actuated.
if (Gamepad.current.leftStick.IsActuated())
    Debug.Log("Left Stick is actuated");
```

It can be useful to determine not just whether a Control is actuated at all, but also the amount by which it is actuated (that is, its magnitude). For example, for a [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) this would be the length of the vector, whereas for a button it is the raw, absolute floating-point value.

In general, the current magnitude of a Control is always >= 0. However, a Control might not have a meaningful magnitude, in which case it returns -1. Any negative value should be considered an invalid magnitude.

You can query the current amount of actuation using [`EvaluateMagnitude`](xref:UnityEngine.InputSystem.InputControl.EvaluateMagnitude).

```CSharp
// Check if left stick is actuated more than a quarter of its motion range.
if (Gamepad.current.leftStick.EvaluateMagnitude() > 0.25f)
    Debug.Log("Left Stick actuated past 25%");
```

These two mechanisms use Control actuation:

- [Interactive rebinding](xref:input-system-action-bindings#interactive-rebinding) (`InputActionRebindingExceptions.RebindOperation`) uses it to select between multiple suitable Controls to find the one that is actuated the most.
- [Conflict resolution](xref:input-system-action-bindings#conflicting-inputs) between multiple Controls that are bound to the same action uses it to decide which Control gets to drive the action.

## Noisy Controls

The Input System can label a Control as "noisy", meaning that they can change value without needing any actual or intentional user interaction. A good example of this is a gravity sensor in a cellphone. Even if the cellphone is perfectly still, there are usually fluctuations in gravity readings. Another example is taking orientation readings from an HMD.

You can query whether a control is noisy with the  [`InputControl.noisy`](xref:UnityEngine.InputSystem.InputControl.noisy) property.

If a Control is marked as noisy, it means that:

1. The Control is not considered for [interactive rebinding](xref:input-system-action-bindings#interactive-rebinding). [`InputActionRebindingExceptions.RebindingOperation`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation) ignores the Control by default (you can bypass this using [`WithoutIgnoringNoisyControls`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.WithoutIgnoringNoisyControls)).
2. If enabled in the Project Settings, the system performs additional event filtering, then calls [`InputDevice.MakeCurrent`](xref:UnityEngine.InputSystem.InputDevice.MakeCurrent). If an input event for a Device contains no state change on a Control that is not marked noisy, then the Device will not be made current based on the event. This avoids, for example, a plugged in PS4 controller constantly making itself the current gamepad ([`Gamepad.current`](xref:UnityEngine.InputSystem.Gamepad.current)) due to its sensors constantly feeding data into the system.
3. When the application loses focus and Devices are [reset](xref:input-system-devices#device-resets) as a result, the state of noisy Controls will be preserved as is. This ensures that sensor readinds will remain at their last value rather than being reset to default values.

> [!NOTE]
> If any Control on a Device is noisy, the Device itself is flagged as noisy.

Parallel to the [`input state`](xref:UnityEngine.InputSystem.InputControl.currentStatePtr) and the [`default state`](xref:UnityEngine.InputSystem.InputControl.defaultStatePtr) that the Input System keeps for all Devices currently present, it also maintains a [`noise mask`](xref:UnityEngine.InputSystem.InputControl.noiseMaskPtr) in which only bits for state that is __not__ noise are set. This can be used to very efficiently mask out noise in input.

## Synthetic Controls

A synthetic Control is a Control that doesn't correspond to an actual physical control on a device (for example the `left`, `right`, `up`, and `down` child Controls on a [`StickControl`](xref:UnityEngine.InputSystem.Controls.StickControl)). These Controls synthesize input from other, actual physical Controls and present it in a different way (in this example, they allow you to treat the individual directions of a stick as buttons).

Whether a given Control is synthetic is indicated by its [`InputControl.synthetic`](xref:UnityEngine.InputSystem.InputControl.synthetic) property.

The system considers synthetic Controls for [interactive rebinding](xref:input-system-action-bindings#interactive-rebinding) but always favors non-synthetic Controls. If both a synthetic and a non-synthetic Control that are a potential match exist, the non-synthetic Control wins by default. This makes it possible to interactively bind to `<Gamepad>/leftStick/left`, for example, but also makes it possible to bind to `<Gamepad>/leftStickPress` without getting interference from the synthetic buttons on the stick.

## Performance Optimization

### Avoiding defensive copies

Use [`InputControl<T>.value`](xref:UnityEngine.InputSystem.InputControl`1.value) instead of [`InputControl<T>.ReadValue`](xref:UnityEngine.InputSystem.InputControl`1.ReadValue) to avoid creating a copy of the control state on every call, as the former returns the value as `ref readonly` while the latter always makes a copy. Note that this optimization only applies if the call site assigns the return value to a variable that has been declared 'ref readonly'. Otherwise a copy will be made as before. Additionally, be aware of defensive copies that can be allocated by the compiler when it is unable to determine that it can safely use the readonly reference i.e. if it can't determine that the reference won't be changed, it will create a defensive copy for you. For more details, see https://learn.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code#use-ref-readonly-return-statements.


### Control Value Caching

When the `'USE_READ_VALUE_CACHING'` internal feature flag is set, the Input System will switch to an optimized path for reading control values. This path efficiently marks controls as 'stale' when they have been actuated. Subsequent calls to [`InputControl<T>.ReadValue`](xref:UnityEngine.InputSystem.InputControl`1.ReadValue) will only apply control processing when there have been changes to that control or in case of control processing. Control processing in this case can mean any hard-coded processing that might exist on the control, such as with [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) which has built-in inversion, normalisation, scaling etc, or any processors that have been applied to the controls' [processor stack](xref:input-system-processors#processors-on-controls).

> [!NOTE]
> Performance improvements **are currently not guaranteed** for all use cases. Even though this performance path marks controls as "stale" in an efficient way, it still has an overhead which can degrade performance in some cases.

A positive performance impact has been seen when:
- Reading from controls that do not change frequently.
- In case the controls change every frame, are being read and have actions bound to them as well, e.g. on a Gamepad, reading `leftStick`, `leftStick.x` and `leftStick.left` for example when there's a action with composite bindings setup.

On the other hand, it is likely to have a negative performance impact when:
- No control reads are performed for a control, and there are a lot of changes for that particular control.
- Reading from controls that change frequently that have no actions bound to those controls.

Moreover, this feature is not enabled by default as it can result in the following minor behavioural changes:
 * Some control processors use global state. Without cached value optimizations, it is possible to read the control value, change the global state, read the control value again, and get a new value due to the fact that the control processor runs on every call. With cached value optimizations, reading the control value will only ever return a new value if the physical control has been actuated. Changing the global state of a control processor will have no effect otherwise.
 * Writing to device state using low-level APIs like [`InputControl<T>.WriteValueIntoState`](xref:UnityEngine.InputSystem.InputControl`1.WriteValueIntoState(`0,System.Void*)) does not set the stale flag and subsequent calls to [`InputControl<T>.value`](xref:UnityEngine.InputSystem.InputControl`1.value) will not reflect those changes.
 * After changing properties on [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) the [`ApplyParameterChanges`](xref:UnityEngine.InputSystem.InputControl.ApplyParameterChanges) has to be called to invalidate cached value.

Processors that need to run on every read can set their respective caching policy to EvaluateOnEveryRead. That will disable caching on controls that are using such processor.

If there are any non-obvious inconsistencies, 'PARANOID_READ_VALUE_CACHING_CHECKS' internal feature flag can be enabled to compare cached and uncached value on every read and log an error if they don't match.

### Optimized control read value

When the `'USE_OPTIMIZED_CONTROLS'` internal feature flag is set, the Input System will use faster way to use state memory for some controls instances. This is very specific optimization and should be used with caution.

> [!NOTE]
> This optimization has a performance impact on `PlayMode` as we do extra checks to ensure that the controls have the correct memory representation during development. Don't be alarmed if you see a performance drop in `PlayMode` when using this optimization as it's expected at this stage.

Most controls are flexible with regards to memory representation, like [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) can be one bit, multiple bits, a float, etc, or in [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) where x and y can have different memory representation.
Yet for most controls there are common memory representation patterns, for example [`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl) are floats or single bytes. Or some [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) are two consequitive floats in memory.
If a control matches a common representation we can bypass reading its children control and cast the memory directly to the common representation. For example if [`Vector2Control`](xref:UnityEngine.InputSystem.Controls.Vector2Control) is two consecutive floats in memory we can bypass reading `x` and `y` separately and just cast the state memory to `Vector2`.

> [!NOTE]
> This optimization only works if the controls don't need any processing applied to them, such as `invert`, `clamp`, `normalize`, `scale` or any other processor. If any of these are applied to the control, **there won't be any optimization applied** and the control will be read as usual.

Also, [`InputControl.ApplyParameterChanges()`](xref:UnityEngine.InputSystem.InputControl.ApplyParameterChanges) **must be explicitly called** in specific changes to ensure [`InputControl.optimizedControlDataType`](xref:UnityEngine.InputSystem.InputControl.optimizedControlDataType) is updated to the correct memory representation. Make sure to call it when:
* Configuration changes after [`InputControl.FinishSetup()`](xref:UnityEngine.InputSystem.InputControl.FinishSetup*) is called.
* Changing parameters such [`AxisControl.invert`](xref:UnityEngine.InputSystem.Controls.AxisControl.invert), [`AxisControl.clamp`](xref:UnityEngine.InputSystem.Controls.AxisControl.clamp), [`AxisControl.normalize`](xref:UnityEngine.InputSystem.Controls.AxisControl.normalize), [`AxisControl.scale`](xref:UnityEngine.InputSystem.Controls.AxisControl.scale) or changing processors. The memory representation needs to be recalculated after these changes so that we know that the control is not optimized anymore. Otherwise, the control will be read with wrong values.

The optimized controls work as follows:
* A potential memory representation is set using [`InputControl.CalculateOptimizedControlDataType()`](xref:UnityEngine.InputSystem.InputControl.CalculateOptimizedControlDataType)
* Its memory representation is stored in [`InputControl.optimizedControlDataType`](xref:UnityEngine.InputSystem.InputControl.optimizedControlDataType)
* Finally,  [`ReadUnprocessedValueFromState`](xref:UnityEngine.InputSystem.InputControl`1.ReadUnprocessedValueFromState*) uses the optimized memory representation to decide if it should cast to memory directly instead of reading every children control on it's own to reconstruct the controls state.
