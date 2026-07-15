---
uid: input-system-bindings-from-code
---

# Configure bindings from code

Each `InputBinding` has the following properties:

|Property|Description|
|--------|-----------|
|[`path`](xref:UnityEngine.InputSystem.InputBinding)|[Control path](control-paths.md) that identifies the control(s) from which the Action should receive input.<br><br>Example: `"<Gamepad>/leftStick"`|
|[`overridePath`](xref:UnityEngine.InputSystem.InputBinding)|[Control path](control-paths.md) that overrides `path`. Unlike `path`, `overridePath` is not persistent, so you can use it to non-destructively override the path on a binding. If it is set to something other than null, it takes effect and overrides `path`.  To get the path which is currently in effect (that is, either `path` or `overridePath`), you can query the [`effectivePath`](xref:UnityEngine.InputSystem.InputBinding) property.|
|[`action`](xref:UnityEngine.InputSystem.InputBinding)|The name or ID of the action that the binding should trigger. Note that this can be null or empty (for instance, for  [composites](#composite-bindings)). Not case-sensitive.<br><br>Example: `"fire"`|
|[`groups`](xref:UnityEngine.InputSystem.InputBinding)|A semicolon-separated list of binding groups that the binding belongs to. Can be null or empty. Binding groups can be anything, but are mostly used for [Control Schemes](control-schemes.md). Not case-sensitive.<br><br>Example: `"Keyboard&Mouse;Gamepad"`|
|[`interactions`](xref:UnityEngine.InputSystem.InputBinding)|A semicolon-separated list of [Interactions](Interactions.md) to apply to input on this binding. Note that Unity appends Interactions applied to the [Action](actions.md) itself (if any) to this list. Not case-sensitive.<br><br>Example: `"slowTap;hold(duration=0.75)"`|
|[`processors`](xref:UnityEngine.InputSystem.InputBinding)|A semicolon-separated list of [Processors](processors.md) to apply to input on this binding. Note that Unity appends Processors applied to the [Action](actions.md) itself (if any) to this list. Not case-sensitive.<br><br>Processors on bindings apply in addition to Processors on Controls that are providing values. For example, if you put a `stickDeadzone` Processor on a binding and then bind it to `<Gamepad>/leftStick`, you get deadzones applied twice: once from the deadzone Processor sitting on the `leftStick` Control, and once from the binding.<br><br>Example: `"invert;axisDeadzone(min=0.1,max=0.95)"`|
|[`id`](xref:UnityEngine.InputSystem.InputBinding)|Unique ID of the binding. You can use it to identify the binding when storing binding overrides in user settings, for example.|
|[`name`](xref:UnityEngine.InputSystem.InputBinding)|Optional name of the binding. Identifies part names inside [Composites](#composite-bindings).<br><br>Example: `"Positive"`|
|[`isComposite`](xref:UnityEngine.InputSystem.InputBinding)|Whether the binding acts as a [Composite](#composite-bindings).|
|[`isPartOfComposite`](xref:UnityEngine.InputSystem.InputBinding)|Whether the binding is part of a [Composite](#composite-bindings).|

To query a flat list of bindings for all actions in an action map, use [`InputActionMap.bindings`](xref:UnityEngine.InputSystem.InputActionMap.bindings).


## Erasing bindings

You can erase a binding by calling [`Erase`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax) on the [binding accessor](xref:UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax).

```CSharp
// Erase first binding on "fire" action.
playerInput.actions["fire"].ChangeBinding(0).Erase();

// Erase "2DVector" composite. This will also erase the part
// bindings of the composite.
playerInput.actions["move"].ChangeCompositeBinding("2DVector").Erase();

// Can also do this by using the name given to the composite binding.
playerInput.actions["move"].ChangeCompositeBinding("WASD").Erase();

// Erase first binding in "gameplay" action map.
playerInput.actions.FindActionMap("gameplay").ChangeBinding(0).Erase();
```

## Adding bindings

New bindings can be added to an Action using [`AddBinding`](xref:UnityEngine.InputSystem.InputActionSetupExtensions) or [`AddCompositeBinding`](xref:UnityEngine.InputSystem.InputActionSetupExtensions).

```CSharp
// Add a binding for the left mouse button to the "fire" action.
playerInput.actions["fire"].AddBinding("<Mouse>/leftButton");

// Add a WASD composite binding to the "move" action.
playerInput.actions["move"]
    .AddCompositeBinding("2DVector")
        .With("Up", "<Keyboard>/w")
        .With("Left", "<Keyboard>/a")
        .With("Down", "<Keyboard>/s")
        .With("Right", "<Keyboard>/d");
```

## Setting parameters

A binding might, either through itself or through its associated action, lead to [processor](processors.md), [interaction](Interactions.md), and/or [composite](#composite-bindings) objects being created. These objects can have parameters you can configure through in the [Binding properties view](binding-properties-panel-reference.md) of the Action editor or through the API. This configuration will give parameters their default value.

```CSharp
// Create an action with a "Hold" interaction on it.
// Set the "duration" parameter to 4 seconds.
var action = new InputAction(interactions: "hold(duration=4)");
```

You can query the current value of any such parameter using the [`GetParameterValue`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions) API.

```CSharp
// This returns a PrimitiveValue?. It will be null if the
// parameter is not found. Otherwise, it is a PrimitiveValue
// which can be converted to a number or boolean.
var p = action.GetParameterValue("duration");
Debug.Log("'duration' is set to: " + p.Value);
```

The above looks for the parameter on any object found on any of the bindings on the action. You can restrict either or both to a more narrow set.

```CSharp
// Retrieve the value of the "duration" parameter specifically of a
// "Hold" interaction and only look on bindings in the "Gamepad" group.
action.GetParameterValue("hold:duration", InputBinding.MaskByGroup("Gamepad"));
```

Alternatively, you can use an expression parameter to encapsulate both the type and the name of the parameter you want to get the value of. This has the advantage of not needing a string parameter but rather references both the type and the name of the parameter in a typesafe way.

```CSharp
// Retrieve the value of the "duration" parameter of TapInteraction.
// This version returns a float? instead of a PrimitiveValue? as it
// sees the type of "duration" at compile-time.
action.GetParameterValue((TapInteraction x) => x.duration);
```

To alter the current value of a parameter, you can use what is referred to as a "parameter override". You can apply these at the level of an individual [`InputAction`](xref:UnityEngine.InputSystem.InputAction), or at the level of an entire [`InputActionMap`](xref:UnityEngine.InputSystem.InputActionMap), or even at the level of an entire [`InputActionAsset`](xref:UnityEngine.InputSystem.InputActionAsset). Such overrides are stored internally and applied automatically even on bindings added later.

To add an override, use the [`ApplyParameterOverride`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions) API or any of its overloads.

```CSharp
// Set the "duration" parameter on all bindings of the action to 4.
action.ApplyParameterOverride("duration", 4f);

// Set the "duration" parameter specifically for "tap" interactions only.
action.ApplyParameterOverride("tap:duration", 0.5f);

// Set the "duration" parameter on tap interactions but only for bindings
// in the "Gamepad" group.
action.ApplyParameterOverride("tap:duration", 0.5f, InputBinding.MaskByGroup("Gamepad");

// Set tap duration for all bindings in an action map.
map.ApplyParameterOverride("tap:duration", 0.5f);

// Set tap duration for all bindings in an entire asset.
asset.ApplyParameterOverride("tap:duration", 0.5f);

// Like for GetParameterValue, overloads are available that take
// an expression instead.
action.ApplyParameterOverride((TapInteraction x) => x.duration, 0.4f);
map.ApplyParameterOverride((TapInteraction x) => x.duration, 0.4f);
asset.ApplyParameterOverride((TapInteraction x) => x.duration, 0.4f);
```

The new value will be applied immediately and affect all composites, processors, and interactions already in use and targeted by the override.

Note that if multiple parameter overrides are applied &ndash; especially when applying some directly to actions and some to maps or assets &ndash;, there may be conflicts between which override to apply. In this case, an attempt is made to chose the "most specific" override to apply.

```CSharp
// Let's say you have an InputAction `action` that is part of an InputActionAsset asset.
var map = action.actionMap;
var asset = map.asset;

// And you apply a "tap:duration" override to the action.
action.ApplyParameterOverride("tap:duration", 0.6f);

// But also apply a "tap:duration" override to the action specifically
// for bindings in the "Gamepad" group.
action.ApplyParameterOverride("tap:duration", 1f, InputBinding.MaskByGroup("Gamepad"));

// And finally also apply a "tap:duration" override to the entire asset.
asset.ApplyParameterOverride("tap:duration", 0.3f);

// Now, bindings on `action` in the "Gamepad" group will use a value of 1 for tap durations,
// other bindings on `action` will use 0.6, and every other binding in the asset will use 0.3.
```

You can use parameter overrides, for example, to scale mouse delta values on a "Look" action.

```CSharp
// Set up an example "Look" action.
var look = new InputAction("look", type: InputActionType.Value);
look.AddBinding("<Mouse>/delta", groups: "KeyboardMouse", processors: "scaleVector2");
look.AddBinding("<Gamepad>/rightStick", groups: "Gamepad", processors: "scaleVector2");

// Now you can adjust stick sensitivity separately from mouse sensitivity.
look.ApplyParameterOverride("scaleVector2:x", 0.5f, InputBinding.MaskByGroup("KeyboardMouse"));
look.ApplyParameterOverride("scaleVector2:y", 0.5f, InputBinding.MaskByGroup("KeyboardMouse"));

look.ApplyParameterOverride("scaleVector2:x", 2f, InputBinding.MaskByGroup("Gamepad"));
look.ApplyParameterOverride("scaleVector2:y", 2f, InputBinding.MaskByGroup("Gamepad"));

// Alternative to using groups, you can also apply overrides directly to specific binding paths.
look.ApplyParameterOverride("scaleVector2:x", 0.5f, new InputBinding("<Mouse>/delta"));
look.ApplyParameterOverride("scaleVector2:y", 0.5f, new InputBinding("<Mouse>/delta"));
```

>NOTE: Parameter overrides are *not* persisted along with an asset.

## Composite bindings

To create composites in code, use the [`AddCompositeBinding`](xref:UnityEngine.InputSystem.InputActionSetupExtensions) method.

```CSharp
myAction.AddCompositeBinding("Axis")
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

Each composite consists of one binding that has [`InputBinding.isComposite`](xref:UnityEngine.InputSystem.InputBinding) set to true, followed by one or more bindings that have [`InputBinding.isPartOfComposite`](xref:UnityEngine.InputSystem.InputBinding) set to true. In other words, several consecutive entries in [`InputActionMap.bindings`](xref:UnityEngine.InputSystem.InputActionMap) or [`InputAction.bindings`](xref:UnityEngine.InputSystem.InputAction) together form a composite.

Note that each composite part can be bound arbitrary many times.

```CSharp
// Make both shoulders and triggers pull on the axis.
myAction.AddCompositeBinding("Axis")
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Positive", "<Gamepad>/rightShoulder")
    .With("Negative", "<Gamepad>/leftTrigger");
    .With("Negative", "<Gamepad>/leftShoulder");
```

Composites can have parameters, just like [Interactions](Interactions.md) and [Processors](processors.md).

```CSharp
myAction.AddCompositeBinding("Axis(whichSideWins=1)");
```

There are currently five Composite types that come with the system out of the box:

- [1D-Axis](#1d-axis): two buttons that pull a 1D axis in the negative and positive direction.
- [2D-Vector](#2d-vector): represents a 4-way button setup where each button represents a cardinal direction, for example a WASD keyboard input (up-down-left-right controls).
- [3D-Vector](#3d-vector): represents a 6-way button where two combinations each control one axis of a 3D Vector.
- [One Modifier](#one-modifier): requires the user to hold down a "modifier" button in addition to another control, for example, "SHIFT+1".
- [Two Modifiers](#two-modifiers): requires the user to hold down two "modifier" buttons in addition to another control, for example, "SHIFT+CTRL+1".


### 1D axis

The 1D Axis Composite is made of two buttons: one that pulls a 1D axis in its negative direction, and another that pulls it in its positive direction, using the [`AxisComposite`](xref:UnityEngine.InputSystem.Composites.AxisComposite) class to compute a `float`.

```CSharp
myAction.AddCompositeBinding("1DAxis") // Or just "Axis"
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

The axis Composite has two Part Bindings.

|Part Binding|Type|Description|
|----|----|-----------|
|[`positive`](xref:UnityEngine.InputSystem.Composites.AxisComposite.positive)|`Button`|Controls pulling in the positive direction (towards [`maxValue`](xref:UnityEngine.InputSystem.Composites.AxisComposite.maxValue)).|
|[`negative`](xref:UnityEngine.InputSystem.Composites.AxisComposite.negative)|`Button`|Controls pulling in the negative direction, (towards [`minValue`](xref:UnityEngine.InputSystem.Composites.AxisComposite.minValue)).|

You can set the following parameters on an axis Composite:

|Parameter|Description|
|---------|-----------|
|[`whichSideWins`](xref:UnityEngine.InputSystem.Composites.AxisComposite)|What happens if both [`positive`](xref:UnityEngine.InputSystem.Composites.AxisComposite) and [`negative`](xref:UnityEngine.InputSystem.Composites.AxisComposite) are actuated. See table below.|
|[`minValue`](xref:UnityEngine.InputSystem.Composites.AxisComposite)|The value returned if the [`negative`](xref:UnityEngine.InputSystem.Composites.AxisComposite) side is actuated. Default is -1.|
|[`maxValue`](xref:UnityEngine.InputSystem.Composites.AxisComposite)|The value returned if the [`positive`](xref:UnityEngine.InputSystem.Composites.AxisComposite) side is actuated. Default is 1.|

If controls from both the `positive` and the `negative` side are actuated, then the resulting value of the axis Composite depends on the `whichSideWin` parameter setting.

|[`WhichSideWins`](xref:UnityEngine.InputSystem.Composites.AxisComposite.WhichSideWins)|Description|
|---------------|-----------|
|(0) `Neither`|Neither side has precedence. The Composite returns the midpoint between `minValue` and `maxValue` as a result. At their default settings, this is 0.<br><br>This is the default value for this setting.|
|(1) `Positive`|The positive side has precedence and the Composite returns `maxValue`.|
|(2) `Negative`|The negative side has precedence and the Composite returns `minValue`.|

> [!NOTE]
> There is no support yet for interpolating between the positive and negative over time.

### 2D vector

A 2D Vector Composite represents a 4-way button setup like the D-pad on gamepads, where each button represents a cardinal direction. This type of Composite binding uses the [`Vector2Composite`](xref:UnityEngine.InputSystem.Composites.Vector2Composite) class to compute a `Vector2`.

Use this to represent up-down-left-right controls, such as WASD keyboard input.

```CSharp
myAction.AddCompositeBinding("2DVector") // Or "Dpad"
    .With("Up", "<Keyboard>/w")
    .With("Down", "<Keyboard>/s")
    .With("Left", "<Keyboard>/a")
    .With("Right", "<Keyboard>/d");

// To set mode (2=analog, 1=digital, 0=digitalNormalized):
myAction.AddCompositeBinding("2DVector(mode=2)")
    .With("Up", "<Gamepad>/leftStick/up")
    .With("Down", "<Gamepad>/leftStick/down")
    .With("Left", "<Gamepad>/leftStick/left")
    .With("Right", "<Gamepad>/leftStick/right");
```

The 2D vector Composite has four Part Bindings.

|Part Binding|Type|Description|
|----|----|-----------|
|[`up`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.up)|`Button`|Controls representing `(0,1)` (+Y).|
|[`down`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.down)|`Button`|Controls representing `(0,-1)` (-Y).|
|[`left`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.left)|`Button`|Controls representing `(-1,0)` (-X).|
|[`right`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.right)|`Button`|Controls representing `(1,0)` (+X).|

In addition, you can set these parameters on a 2D Vector Composite:

|Parameter|Description|
|---------|-----------|
|[`mode`](xref:UnityEngine.InputSystem.Composites.Vector2Composite)|Whether to treat the inputs as digital or as analog controls.<br><br>If this is set to [`Mode.DigitalNormalized`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.Mode), inputs are treated as buttons (off if below [`defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings) and on if equal to or greater). Each input is 0 or 1 depending on whether the button is pressed or not. The vector resulting from the up/down/left/right parts is normalized. The result is a diamond-shaped 2D input range.<br><br>If this is set to [`Mode.Digital`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.Mode), the behavior is essentially the same as [`Mode.DigitalNormalized`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.Mode) except that the resulting vector is not normalized.<br><br>Finally, if this is set to [`Mode.Analog`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.Mode), inputs are treated as analog (That is, full floating-point values) and, other than [`down`](xref:UnityEngine.InputSystem.Composites.Vector2Composite) and [`left`](xref:UnityEngine.InputSystem.Composites.Vector2Composite) being inverted, values will be passed through as is.<br><br>The default is [`Mode.DigitalNormalized`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.Mode).|

> [!NOTE]
> There is no support yet for interpolating between the up/down/left/right over time.

### 3D vector

A 3D Vector Composite that represents a 6-way button where two combinations each control one axis of a 3D Vector. This type of Composite binding uses the the [`Vector3Composite`](xref:UnityEngine.InputSystem.Composites.Vector3Composite) class to compute a `Vector3`.

```CSharp
myAction.AddCompositeBinding("3DVector")
    .With("Up", "<Keyboard>/w")
    .With("Down", "<Keyboard>/s")
    .With("Left", "<Keyboard>/a")
    .With("Right", "<Keyboard>/d");

// To set mode (2=analog, 1=digital, 0=digitalNormalized):
myAction.AddCompositeBinding("3DVector(mode=2)")
    .With("Up", "<Gamepad>/leftStick/up")
    .With("Down", "<Gamepad>/leftStick/down")
    .With("Left", "<Gamepad>/leftStick/left")
    .With("Right", "<Gamepad>/leftStick/right");
```

The 3D vector Composite has six Part Bindings.

|Part Binding|Type|Description|
|----|----|-----------|
|[`up`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.up)|`Button`|Controls representing `(0,1,0)` (+Y).|
|[`down`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.down)|`Button`|Controls representing `(0,-1,0)` (-Y).|
|[`left`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.left)|`Button`|Controls representing `(-1,0,0)` (-X).|
|[`right`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.right)|`Button`|Controls representing `(1,0,0)` (+X).|
|[`forward`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.forward)|`Button`|Controls representing `(0,0,1)` (+Z).|
|[`backward`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.backward)|`Button`|Controls representing `(0,0,-1)` (-Z).|

In addition, you can set the following parameters on a 3D vector Composite:

|Parameter|Description|
|---------|-----------|
|[`mode`](xref:UnityEngine.InputSystem.Composites.Vector3Composite)|Whether to treat the inputs as digital or as analog controls.<br><br>If this is set to [`Mode.DigitalNormalized`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.Mode), inputs are treated as buttons (off if below [`defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings) and on if equal to or greater). Each input is 0 or 1 depending on whether the button is pressed or not. The vector resulting from the up/down/left/right/forward/backward parts is normalized.<br><br>If this is set to [`Mode.Digital`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.Mode), the behavior is essentially the same as [`Mode.DigitalNormalized`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.Mode) except that the resulting vector is not normalized.<br><br>Finally, if this is set to [`Mode.Analog`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.Mode), inputs are treated as analog (that is, full floating-point values) and, other than [`down`](xref:UnityEngine.InputSystem.Composites.Vector3Composite), [`left`](xref:UnityEngine.InputSystem.Composites.Vector3Composite), and [`backward`](xref:UnityEngine.InputSystem.Composites.Vector3Composite) being inverted, values will be passed through as they are.<br><br>The default is [`Analog`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.Mode).|

### One Modifier

A One Modifier Composite requires the user to hold down a "modifier" button in addition to another control from which the actual value of the binding is determined. This can be used, for example, for bindings such as "SHIFT+1". This type of Composite binding uses the [`OneModifierComposite`](xref:UnityEngine.InputSystem.Composites.OneModifierComposite) class. The buttons can be on any device, and can be toggle buttons or full-range buttons such as gamepad triggers.

The result is a value of the same type as the controls bound to the [`binding`](xref:UnityEngine.InputSystem.Composites.OneModifierComposite.binding) part.

```CSharp
// Add binding for "CTRL+1".
myAction.AddCompositeBinding("OneModifier")
    .With("Binding", "<Keyboard>/1")
    .With("Modifier", "<Keyboard>/ctrl")

// Add binding to mouse delta such that it only takes effect
// while the ALT key is down.
myAction.AddCompositeBinding("OneModifier")
    .With("Binding", "<Mouse>/delta")
    .With("Modifier", "<Keyboard>/alt");
```

The button with one Modifier Composite has two Part Bindings.

|Part Binding|Type|Description|
|----|----|-----------|
|[`modifier`](xref:UnityEngine.InputSystem.Composites.OneModifierComposite.modifier)|`Button`|Modifier that has to be held for `binding` to come through. If the user holds any of the buttons bound to the `modifier` at the same time as the button that triggers the action, the Composite assumes the value of the `modifier` binding. If the user does not press any button bound to the `modifier`, the Composite remains at default value.|
|[`binding`](xref:UnityEngine.InputSystem.Composites.OneModifierComposite.binding)|Any|The control(s) whose value the Composite assumes while the user holds down the `modifier` button.|

This Composite has no parameters.

### Two Modifiers

A Two Modifiers Composite requires the user to hold down two "modifier" buttons in addition to another control from which the actual value of the binding is determined. This can be used, for example, for bindings such as "SHIFT+CTRL+1". This type of Composite binding uses the [`TwoModifiersComposite`](xref:UnityEngine.InputSystem.Composites.TwoModifiersComposite) class. The buttons can be on any device, and can be toggle buttons or full-range buttons such as gamepad triggers.

The result is a value of the same type as the controls bound to the [`binding`](xref:UnityEngine.InputSystem.Composites.TwoModifiersComposite.binding) part.

```CSharp
myAction.AddCompositeBinding("TwoModifiers")
    .With("Button", "<Keyboard>/1")
    .With("Modifier1", "<Keyboard>/leftCtrl")
    .With("Modifier1", "<Keyboard>/rightCtrl")
    .With("Modifier2", "<Keyboard>/leftShift")
    .With("Modifier2", "<Keyboard>/rightShift");
```

The button with two Modifiers Composite has three Part Bindings.

|Part Binding|Type|Description|
|----|----|-----------|
|[`modifier1`](xref:UnityEngine.InputSystem.Composites.TwoModifiersComposite.modifier1)|`Button`|The first modifier the user must hold alongside `modifier2`, for `binding` to come through. If the user does not press any button bound to the `modifier1`, the Composite remains at default value.|
|[`modifier2`](xref:UnityEngine.InputSystem.Composites.TwoModifiersComposite.modifier2)|`Button`|The second modifier the user must hold alongside `modifier1`, for `binding` to come through. If the user does not press any button bound to the `modifier2`, the Composite remains at default value.|
|[`binding`](xref:UnityEngine.InputSystem.Composites.TwoModifiersComposite.binding)|Any|The control(s) whose value the Composite assumes while the user presses both `modifier1` and `modifier2` at the same time.|

This Composite has no parameters.

### Writing custom Composites

You can define new types of Composites, and register them with the API. Unity treats these the same as predefined types, which the Input System internally defines and registers in the same way.

To define a new type of Composite, create a class based on [`InputBindingComposite<TValue>`](xref:UnityEngine.InputSystem.InputBindingComposite`1).

> [!IMPORTANT]
> Composites must be __stateless__. This means that you cannot store local state that changes depending on the input being processed. For __stateful__ processing on bindings, refer to [interactions](xref:input-system-custom-interactions).

```CSharp
// Use InputBindingComposite<TValue> as a base class for a composite that returns
// values of type TValue.
// NOTE: It is possible to define a composite that returns different kinds of values
//       but doing so requires deriving directly from InputBindingComposite.
#if UNITY_EDITOR
[InitializeOnLoad] // Automatically register in editor.
#endif
// Determine how GetBindingDisplayString() formats the composite by applying
// the  DisplayStringFormat attribute.
[DisplayStringFormat("{firstPart}+{secondPart}")]
public class CustomComposite : InputBindingComposite<float>
{
    // Each part binding is represented as a field of type int and annotated with
    // InputControlAttribute. Setting "layout" restricts the controls that
    // are made available for picking in the UI.
    //
    // On creation, the int value is set to an integer identifier for the binding
    // part. This identifier can read values from InputBindingCompositeContext.
    // See ReadValue() below.
    [InputControl(layout = "Button")]
    public int firstPart;

    [InputControl(layout = "Button")]
    public int secondPart;

    // Any public field that is not annotated with InputControlAttribute is considered
    // a parameter of the composite. This can be set graphically in the UI and also
    // in the data (For example, "custom(floatParameter=2.0)").
    public float floatParameter;
    public bool boolParameter;

    // This method computes the resulting input value of the composite based
    // on the input from its part bindings.
    public override float ReadValue(ref InputBindingCompositeContext context)
    {
        var firstPartValue = context.ReadValue<float>(firstPart);
        var secondPartValue = context.ReadValue<float>(secondPart);

        //... do some processing and return value
    }

    // This method computes the current actuation of the binding as a whole.
    public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
    {
        // Compute normalized [0..1] magnitude value for current actuation level.
    }

    static CustomComposite()
    {
        // Can give custom name or use default (type name with "Composite" clipped off).
        // Same composite can be registered multiple times with different names to introduce
        // aliases.
        //
        // NOTE: Registering from the static constructor using InitializeOnLoad and
        //       RuntimeInitializeOnLoadMethod is only one way. You can register the
        //       composite from wherever it works best for you. Note, however, that
        //       the registration has to take place before the composite is first used
        //       in a binding. Also, for the composite to show in the editor, it has
        //       to be registered from code that runs in edit mode.
        InputSystem.RegisterBindingComposite<CustomComposite>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init() {} // Trigger static constructor.
}
```

The Composite should now appear in the editor UI when you add a binding, and you can now use it in scripts.

```CSharp
    myAction.AddCompositeBinding("custom(floatParameter=2.0)")
        .With("firstpart", "<Gamepad>/buttonSouth")
        .With("secondpart", "<Gamepad>/buttonNorth");
```

To define a custom parameter editor for the Composite, you can derive from  [`InputParameterEditor<TObject>`](xref:UnityEngine.InputSystem.Editor.InputParameterEditor`1).

```CSharp
#if UNITY_EDITOR
public class CustomParameterEditor : InputParameterEditor<CustomComposite>
{
    public override void OnGUI()
    {
        EditorGUILayout.Label("Custom stuff");
        target.floatParameter = EditorGUILayout.FloatField("Some Parameter", target.floatParameter);
    }
}
#endif
```


## Changing bindings

In general, you can change existing bindings with the [`InputActionSetupExtensions.ChangeBinding`](xref:UnityEngine.InputSystem.InputActionSetupExtensions) method. This returns an accessor that can be used to modify the properties of the targeted [`InputBinding`](xref:UnityEngine.InputSystem.InputBinding). Note that most of the write operations of the accessor are destructive. For non-destructive changes to bindings, refer to [Applying Overrides](#apply-binding-overrides).

```CSharp
// Get write access to the second binding of the 'fire' action.
var accessor = playerInput.actions['fire'].ChangeBinding(1);

// You can also gain access through the InputActionMap. Each
// map contains an array of all its bindings (see InputActionMap.bindings).
// Here we gain access to the third binding in the map.
accessor = playerInput.actions.FindActionMap("gameplay").ChangeBinding(2);
```

You can use the resulting accessor to modify properties through methods such as [`WithPath`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax) or [`WithProcessors`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax).

```CSharp
playerInput.actions["fire"].ChangeBinding(1)
    // Change path to space key.
    .WithPath("<Keyboard>/space");
```

You can also use the accessor to iterate through bindings using [`PreviousBinding`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax) and [`NextBinding`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax).

```CSharp
// Move accessor to previous binding.
accessor = accessor.PreviousBinding();

// Move accessor to next binding.
accessor = accessor.NextBinding();
```

If the given binding is a [composite](xref:UnityEngine.InputSystem.InputBinding), you can address it by its name rather than by index.

```CSharp
// Change the 2DVector composite of the "move" action.
playerInput.actions["move"].ChangeCompositeBinding("2DVector")


//
playerInput.actions["move"].ChangeBinding("WASD")
```



## Set a binding's control scheme from code

Control schemes allow you to group types of bindings together according to their control type, so that you can enable or disable groups of bindings. For example, you might want to enable all keyboard and mouse bindings if the user presses a keyboard button or moves the mouse.


Unity stores these on the [`InputBinding`](xref:UnityEngine.InputSystem.InputBinding) class as a semicolon-separated string in the  [`InputBinding.groups`](xref:UnityEngine.InputSystem.InputBinding) property, and you can use them for any arbitrary grouping of bindings. To enable different sets of binding groups for an [`InputActionMap`](xref:UnityEngine.InputSystem.InputActionMap) or [`InputActionAsset`](xref:UnityEngine.InputSystem.InputActionAsset), you can use the [`InputActionMap.bindingMask`](xref:UnityEngine.InputSystem.InputActionMap)/[`InputActionAsset.bindingMask`](xref:UnityEngine.InputSystem.InputActionAsset) property. The Input System uses this to implement the concept of grouping bindings into different  [`InputControlSchemes`](xref:UnityEngine.InputSystem.InputControlScheme).

Control Schemes use binding groups to map bindings in an [`InputActionMap`](xref:UnityEngine.InputSystem.InputActionMap) or [`InputActionAsset`](xref:UnityEngine.InputSystem.InputActionAsset) to different types of Devices. The [`PlayerInput`](player-input-component.md) class uses these to enable a matching Control Scheme for a new [user](user-management.md) joining the game, based on the Device they are playing on.



### Apply binding overrides

You can override aspects of any binding at run-time non-destructively. Specific properties of [`InputBinding`](xref:UnityEngine.InputSystem.InputBinding) have an `override` variant that, if set, will take precedent over the property that they shadow.  All `override` properties are of type `String`.

|Property|Override|Description|
|--------|--------|-----------|
|[`path`](xref:UnityEngine.InputSystem.InputBinding)|[`overridePath`](xref:UnityEngine.InputSystem.InputBinding)|Replaces the [Control path](control-paths.md) that determines which Control(s) are referenced in the binding. If [`overridePath`](xref:UnityEngine.InputSystem.InputBinding) is set to an empty string, the binding is effectively disabled.<br><br>Example: `"<Gamepad>/leftStick"`|
|[`processors`](xref:UnityEngine.InputSystem.InputBinding)|[`overrideProcessors`](xref:UnityEngine.InputSystem.InputBinding)|Replaces the [processors](processors.md) applied to the binding.<br><br>Example: `"invert,normalize(min=0,max=10)"`|
|[`interactions`](xref:UnityEngine.InputSystem.InputBinding)|[`overrideInteractions`](xref:UnityEngine.InputSystem.InputBinding)|Replaces the [interactions](Interactions.md) applied to the binding.<br><br>Example: `"tap(duration=0.5)"`|

>NOTE: The `override` property values will not be saved along with the Actions (for example, when calling [`InputActionAsset.ToJson()`](xref:UnityEngine.InputSystem.InputActionAsset)). Refer to [Saving and loading rebinds](save-load-rebinds.md) for details about how to persist user rebinds.

To set the various `override` properties, you can use the [`ApplyBindingOverride`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions) APIs.

```CSharp
// Rebind the "fire" action to the left trigger on the gamepad.
playerInput.actions["fire"].ApplyBindingOverride("<Gamepad>/leftTrigger");
```

In most cases, it is best to locate specific bindings using APIs such as [`GetBindingIndexForControl`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions) and to then apply the override to that specific binding.

```CSharp
// Find the "Jump" binding for the space key.
var jumpAction = playerInput.actions["Jump"];
var bindingIndex = jumpAction.GetBindingIndexForControl(Keyboard.current.spaceKey);

// And change it to the enter key.
jumpAction.ApplyBindingOverride(bindingIndex, "<Keyboard>/enter");
```
