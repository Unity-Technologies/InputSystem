---
uid: input-system-bindings-from-code
---

# Configure bindings from code


[//]: # (TODO: Most of these examples should be moved to API docs and this page should provide an overview linking to those API pages.)


Each Binding has the following properties:

|Property|Description|
|--------|-----------|
|[`path`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_path)|[Control path](controls.md#control-paths) that identifies the control(s) from which the Action should receive input.<br><br>Example: `"<Gamepad>/leftStick"`|
|[`overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath)|[Control path](controls.md#control-paths) that overrides `path`. Unlike `path`, `overridePath` is not persistent, so you can use it to non-destructively override the path on a Binding. If it is set to something other than null, it takes effect and overrides `path`.  To get the path which is currently in effect (that is, either `path` or `overridePath`), you can query the [`effectivePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_effectivePath) property.|
|[`action`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_action)|The name or ID of the Action that the Binding should trigger. Note that this can be null or empty (for instance, for  [composites](#composite-bindings)). Not case-sensitive.<br><br>Example: `"fire"`|
|[`groups`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_groups)|A semicolon-separated list of Binding groups that the Binding belongs to. Can be null or empty. Binding groups can be anything, but are mostly used for [Control Schemes](#control-schemes). Not case-sensitive.<br><br>Example: `"Keyboard&Mouse;Gamepad"`|
|[`interactions`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_interactions)|A semicolon-separated list of [Interactions](Interactions.md) to apply to input on this Binding. Note that Unity appends Interactions applied to the [Action](actions.md) itself (if any) to this list. Not case-sensitive.<br><br>Example: `"slowTap;hold(duration=0.75)"`|
|[`processors`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_processors)|A semicolon-separated list of [Processors](processors.md) to apply to input on this Binding. Note that Unity appends Processors applied to the [Action](actions.md) itself (if any) to this list. Not case-sensitive.<br><br>Processors on Bindings apply in addition to Processors on Controls that are providing values. For example, if you put a `stickDeadzone` Processor on a Binding and then bind it to `<Gamepad>/leftStick`, you get deadzones applied twice: once from the deadzone Processor sitting on the `leftStick` Control, and once from the Binding.<br><br>Example: `"invert;axisDeadzone(min=0.1,max=0.95)"`|
|[`id`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_id)|Unique ID of the Binding. You can use it to identify the Binding when storing Binding overrides in user settings, for example.|
|[`name`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_name)|Optional name of the Binding. Identifies part names inside [Composites](#composite-bindings).<br><br>Example: `"Positive"`|
|[`isComposite`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isComposite)|Whether the Binding acts as a [Composite](#composite-bindings).|
|[`isPartOfComposite`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isPartOfComposite)|Whether the Binding is part of a [Composite](#composite-bindings).|

To query the Bindings to a particular Action, you can use [`InputAction.bindings`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_bindings). To query a flat list of Bindings for all Actions in an Action Map, you can use [`InputActionMap.bindings`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_bindings).


## Erasing Bindings

You can erase a binding by calling [`Erase`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.html#UnityEngine_InputSystem_InputActionSetupExtensions_BindingSyntax_Erase_) on the [binding accessor](../api/UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.html).

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

## Adding Bindings

New bindings can be added to an Action using [`AddBinding`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.html#UnityEngine_InputSystem_InputActionSetupExtensions_AddBinding_UnityEngine_InputSystem_InputAction_System_String_System_String_System_String_System_String_) or [`AddCompositeBinding`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.html#UnityEngine_InputSystem_InputActionSetupExtensions_AddCompositeBinding_UnityEngine_InputSystem_InputAction_System_String_System_String_System_String_).

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

A Binding may, either through itself or through its associated Action, lead to [processor](processors.md), [interaction](Interactions.md), and/or [composite](#composite-bindings) objects being created. These objects can have parameters you can configure through in the [Binding properties view](actions-editor.md#bindings) of the Action editor or through the API. This configuration will give parameters their default value.

```CSharp
// Create an action with a "Hold" interaction on it.
// Set the "duration" parameter to 4 seconds.
var action = new InputAction(interactions: "hold(duration=4)");
```

You can query the current value of any such parameter using the [`GetParameterValue`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_GetParameterValue_UnityEngine_InputSystem_InputAction_System_String_UnityEngine_InputSystem_InputBinding_) API.

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

To alter the current value of a parameter, you can use what is referred to as a "parameter override". You can apply these at the level of an individual [`InputAction`](../api/UnityEngine.InputSystem.InputAction.html), or at the level of an entire [`InputActionMap`](../api/UnityEngine.InputSystem.InputActionMap.html), or even at the level of an entire [`InputActionAsset`](../api/UnityEngine.InputSystem.InputActionAsset.html). Such overrides are stored internally and applied automatically even on bindings added later.

To add an override, use the [`ApplyParameterOverride`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_ApplyParameterOverride_UnityEngine_InputSystem_InputAction_System_String_UnityEngine_InputSystem_Utilities_PrimitiveValue_UnityEngine_InputSystem_InputBinding_) API or any of its overloads.

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

## Composite Bindings

To create composites in code, you can use the [`AddCompositeBinding`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.html#UnityEngine_InputSystem_InputActionSetupExtensions_AddCompositeBinding_UnityEngine_InputSystem_InputAction_System_String_System_String_System_String_) syntax.

```CSharp
myAction.AddCompositeBinding("Axis")
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

Each Composite consists of one Binding that has [`InputBinding.isComposite`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isComposite) set to true, followed by one or more Bindings that have [`InputBinding.isPartOfComposite`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isPartOfComposite) set to true. In other words, several consecutive entries in [`InputActionMap.bindings`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_bindings) or [`InputAction.bindings`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_bindings) together form a Composite.

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

There are currently five Composite types that come with the system out of the box: [1D-Axis](#1d-axis), [2D-Vector](#2d-vector), [3D-Vector](#3d-vector), [One Modifier](#one-modifier) and [Two Modifiers](#two-modifiers). Additionally, you can [add your own](#writing-custom-composites) types of Composites.

### 1D axis

A Composite made of two buttons: one that pulls a 1D axis in its negative direction, and another that pulls it in its positive direction. Implemented in the [`AxisComposite`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html) class. The result is a `float`.

```CSharp
myAction.AddCompositeBinding("1DAxis") // Or just "Axis"
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

The axis Composite has two part bindings.

|Part|Type|Description|
|----|----|-----------|
|[`positive`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_positive)|`Button`|Controls pulling in the positive direction (towards [`maxValue`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_maxValue)).|
|[`negative`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_negative)|`Button`|Controls pulling in the negative direction, (towards [`minValue`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_minValue)).|

You can set the following parameters on an axis Composite:

|Parameter|Description|
|---------|-----------|
|[`whichSideWins`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_whichSideWins)|What happens if both [`positive`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_positive) and [`negative`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_negative) are actuated. See table below.|
|[`minValue`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_minValue)|The value returned if the [`negative`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_negative) side is actuated. Default is -1.|
|[`maxValue`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_maxValue)|The value returned if the [`positive`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_positive) side is actuated. Default is 1.|

If Controls from both the `positive` and the `negative` side are actuated, then the resulting value of the axis Composite depends on the `whichSideWin` parameter setting.

|[`WhichSideWins`](../api/UnityEngine.InputSystem.Composites.AxisComposite.WhichSideWins.html)|Description|
|---------------|-----------|
|(0) `Neither`|Neither side has precedence. The Composite returns the midpoint between `minValue` and `maxValue` as a result. At their default settings, this is 0.<br><br>This is the default value for this setting.|
|(1) `Positive`|The positive side has precedence and the Composite returns `maxValue`.|
|(2) `Negative`|The negative side has precedence and the Composite returns `minValue`.|

>__Note__: There is no support yet for interpolating between the positive and negative over time.

### 2D vector

A Composite that represents a 4-way button setup like the D-pad on gamepads. Each button represents a cardinal direction. Implemented in the [`Vector2Composite`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html) class. The result is a `Vector2`.

This Composite is most useful for representing up-down-left-right controls, such as WASD keyboard input.

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

The 2D vector Composite has four part Bindings.

|Part|Type|Description|
|----|----|-----------|
|[`up`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_up)|`Button`|Controls representing `(0,1)` (+Y).|
|[`down`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_down)|`Button`|Controls representing `(0,-1)` (-Y).|
|[`left`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_left)|`Button`|Controls representing `(-1,0)` (-X).|
|[`right`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_right)|`Button`|Controls representing `(1,0)` (+X).|

In addition, you can set the following parameters on a 2D vector Composite:

|Parameter|Description|
|---------|-----------|
|[`mode`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_mode)|Whether to treat the inputs as digital or as analog controls.<br><br>If this is set to [`Mode.DigitalNormalized`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.Mode.html#UnityEngine_InputSystem_Composites_Vector2Composite_Mode_DigitalNormalized), inputs are treated as buttons (off if below [`defaultButtonPressPoint`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint) and on if equal to or greater). Each input is 0 or 1 depending on whether the button is pressed or not. The vector resulting from the up/down/left/right parts is normalized. The result is a diamond-shaped 2D input range.<br><br>If this is set to [`Mode.Digital`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.Mode.html#UnityEngine_InputSystem_Composites_Vector2Composite_Mode_Digital), the behavior is essentially the same as [`Mode.DigitalNormalized`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.Mode.html#UnityEngine_InputSystem_Composites_Vector2Composite_Mode_DigitalNormalized) except that the resulting vector is not normalized.<br><br>Finally, if this is set to [`Mode.Analog`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.Mode.html#UnityEngine_InputSystem_Composites_Vector2Composite_Mode_Analog), inputs are treated as analog (i.e. full floating-point values) and, other than [`down`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_down) and [`left`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_left) being inverted, values will be passed through as is.<br><br>The default is [`Mode.DigitalNormalized`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.Mode.html#UnityEngine_InputSystem_Composites_Vector2Composite_Mode_DigitalNormalized).|

>__Note__: There is no support yet for interpolating between the up/down/left/right over time.

### 3D vector

A Composite that represents a 6-way button where two combinations each control one axis of a 3D vector. Implemented in the [`Vector3Composite`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.html) class. The result is a `Vector3`.

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

The 3D vector Composite has four part Bindings.

|Part|Type|Description|
|----|----|-----------|
|[`up`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.html#UnityEngine_InputSystem_Composites_Vector3Composite_up)|`Button`|Controls representing `(0,1,0)` (+Y).|
|[`down`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.html#UnityEngine_InputSystem_Composites_Vector3Composite_down)|`Button`|Controls representing `(0,-1,0)` (-Y).|
|[`left`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.html#UnityEngine_InputSystem_Composites_Vector3Composite_left)|`Button`|Controls representing `(-1,0,0)` (-X).|
|[`right`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.html#UnityEngine_InputSystem_Composites_Vector3Composite_right)|`Button`|Controls representing `(1,0,0)` (+X).|
|[`forward`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.html#UnityEngine_InputSystem_Composites_Vector3Composite_forward)|`Button`|Controls representing `(0,0,1)` (+Z).|
|[`backward`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.html#UnityEngine_InputSystem_Composites_Vector3Composite_backward)|`Button`|Controls representing `(0,0,-1)` (-Z).|

In addition, you can set the following parameters on a 3D vector Composite:

|Parameter|Description|
|---------|-----------|
|[`mode`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.html#UnityEngine_InputSystem_Composites_Vector3Composite_mode)|Whether to treat the inputs as digital or as analog controls.<br><br>If this is set to [`Mode.DigitalNormalized`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.Mode.html#UnityEngine_InputSystem_Composites_Vector3Composite_Mode_DigitalNormalized), inputs are treated as buttons (off if below [`defaultButtonPressPoint`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint) and on if equal to or greater). Each input is 0 or 1 depending on whether the button is pressed or not. The vector resulting from the up/down/left/right/forward/backward parts is normalized.<br><br>If this is set to [`Mode.Digital`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.Mode.html#UnityEngine_InputSystem_Composites_Vector3Composite_Mode_Digital), the behavior is essentially the same as [`Mode.DigitalNormalized`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.Mode.html#UnityEngine_InputSystem_Composites_Vector3Composite_Mode_DigitalNormalized) except that the resulting vector is not normalized.<br><br>Finally, if this is set to [`Mode.Analog`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.Mode.html#UnityEngine_InputSystem_Composites_Vector3Composite_Mode_Analog), inputs are treated as analog (that is, full floating-point values) and, other than [`down`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.html#UnityEngine_InputSystem_Composites_Vector3Composite_down), [`left`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.html#UnityEngine_InputSystem_Composites_Vector3Composite_left), and [`backward`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.html#UnityEngine_InputSystem_Composites_Vector3Composite_backward) being inverted, values will be passed through as they are.<br><br>The default is [`Analog`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.Mode.html#UnityEngine_InputSystem_Composites_Vector3Composite_Mode_Analog).|

### One Modifier

A Composite that requires the user to hold down a "modifier" button in addition to another control from which the actual value of the Binding is determined. This can be used, for example, for Bindings such as "SHIFT+1". Implemented in the [`OneModifierComposite`](../api/UnityEngine.InputSystem.Composites.OneModifierComposite.html) class. The buttons can be on any Device, and can be toggle buttons or full-range buttons such as gamepad triggers.

The result is a value of the same type as the controls bound to the [`binding`](../api/UnityEngine.InputSystem.Composites.OneModifierComposite.html#UnityEngine_InputSystem_Composites_OneModifierComposite_binding) part.

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

The button with one modifier Composite has two part Bindings.

|Part|Type|Description|
|----|----|-----------|
|[`modifier`](../api/UnityEngine.InputSystem.Composites.OneModifierComposite.html#UnityEngine_InputSystem_Composites_OneModifierComposite_modifier)|`Button`|Modifier that has to be held for `binding` to come through. If the user holds any of the buttons bound to the `modifier` at the same time as the button that triggers the action, the Composite assumes the value of the `modifier` Binding. If the user does not press any button bound to the `modifier`, the Composite remains at default value.|
|[`binding`](../api/UnityEngine.InputSystem.Composites.OneModifierComposite.html#UnityEngine_InputSystem_Composites_OneModifierComposite_binding)|Any|The control(s) whose value the Composite assumes while the user holds down the `modifier` button.|

This Composite has no parameters.

### Two Modifiers

A Composite that requires the user to hold down two "modifier" buttons in addition to another control from which the actual value of the Binding is determined. This can be used, for example, for Bindings such as "SHIFT+CTRL+1". Implemented in the [`TwoModifiersComposite`](../api/UnityEngine.InputSystem.Composites.TwoModifiersComposite.html) class. The buttons can be on any Device, and can be toggle buttons or full-range buttons such as gamepad triggers.

The result is a value of the same type as the controls bound to the [`binding`](../api/UnityEngine.InputSystem.Composites.TwoModifiersComposite.html#UnityEngine_InputSystem_Composites_TwoModifiersComposite_binding) part.

```CSharp
myAction.AddCompositeBinding("TwoModifiers")
    .With("Button", "<Keyboard>/1")
    .With("Modifier1", "<Keyboard>/leftCtrl")
    .With("Modifier1", "<Keyboard>/rightCtrl")
    .With("Modifier2", "<Keyboard>/leftShift")
    .With("Modifier2", "<Keyboard>/rightShift");
```

The button with two modifiers Composite has three part Bindings.

|Part|Type|Description|
|----|----|-----------|
|[`modifier1`](../api/UnityEngine.InputSystem.Composites.TwoModifiersComposite.html#UnityEngine_InputSystem_Composites_TwoModifiersComposite_modifier1)|`Button`|The first modifier the user must hold alongside `modifier2`, for `binding` to come through. If the user does not press any button bound to the `modifier1`, the Composite remains at default value.|
|[`modifier2`](../api/UnityEngine.InputSystem.Composites.TwoModifiersComposite.html#UnityEngine_InputSystem_Composites_TwoModifiersComposite_modifier2)|`Button`|The second modifier the user must hold alongside `modifier1`, for `binding` to come through. If the user does not press any button bound to the `modifier2`, the Composite remains at default value.|
|[`binding`](../api/UnityEngine.InputSystem.Composites.TwoModifiersComposite.html#UnityEngine_InputSystem_Composites_TwoModifiersComposite_binding)|Any|The control(s) whose value the Composite assumes while the user presses both `modifier1` and `modifier2` at the same time.|

This Composite has no parameters.

### Writing custom Composites

You can define new types of Composites, and register them with the API. Unity treats these the same as predefined types, which the Input System internally defines and registers in the same way.

To define a new type of Composite, create a class based on [`InputBindingComposite<TValue>`](../api/UnityEngine.InputSystem.InputBindingComposite-1.html).

> __IMPORTANT__: Composites must be __stateless__. This means that you cannot store local state that changes depending on the input being processed. For __stateful__ processing on Bindings, see [interactions](write-custom-interactions.md).

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
    // in the data (e.g. "custom(floatParameter=2.0)").
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

The Composite should now appear in the editor UI when you add a Binding, and you can now use it in scripts.

```CSharp
    myAction.AddCompositeBinding("custom(floatParameter=2.0)")
        .With("firstpart", "<Gamepad>/buttonSouth")
        .With("secondpart", "<Gamepad>/buttonNorth");
```

To define a custom parameter editor for the Composite, you can derive from  [`InputParameterEditor<TObject>`](../api/UnityEngine.InputSystem.Editor.InputParameterEditor-1.html).

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


## Changing Bindings

In general, you can change existing bindings via the [`InputActionSetupExtensions.ChangeBinding`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.html#UnityEngine_InputSystem_InputActionSetupExtensions_ChangeBinding_UnityEngine_InputSystem_InputAction_System_Int32_) method. This returns an accessor that can be used to modify the properties of the targeted [`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html). Note that most of the write operations of the accessor are destructive. For non-destructive changes to bindings, see [Applying Overrides](#applying-overrides).

```CSharp
// Get write access to the second binding of the 'fire' action.
var accessor = playerInput.actions['fire'].ChangeBinding(1);

// You can also gain access through the InputActionMap. Each
// map contains an array of all its bindings (see InputActionMap.bindings).
// Here we gain access to the third binding in the map.
accessor = playerInput.actions.FindActionMap("gameplay").ChangeBinding(2);
```

You can use the resulting accessor to modify properties through methods such as [`WithPath`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.html#UnityEngine_InputSystem_InputActionSetupExtensions_BindingSyntax_WithPath_) or [`WithProcessors`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.html#UnityEngine_InputSystem_InputActionSetupExtensions_BindingSyntax_WithProcessors_).

```CSharp
playerInput.actions["fire"].ChangeBinding(1)
    // Change path to space key.
    .WithPath("<Keyboard>/space");
```

You can also use the accessor to iterate through bindings using [`PreviousBinding`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.html#UnityEngine_InputSystem_InputActionSetupExtensions_BindingSyntax_PreviousBinding_) and [`NextBinding`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.html#UnityEngine_InputSystem_InputActionSetupExtensions_BindingSyntax_NextBinding_).

```CSharp
// Move accessor to previous binding.
accessor = accessor.PreviousBinding();

// Move accessor to next binding.
accessor = accessor.NextBinding();
```

If the given binding is a [composite](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isComposite), you can address it by its name rather than by index.

```CSharp
// Change the 2DVector composite of the "move" action.
playerInput.actions["move"].ChangeCompositeBinding("2DVector")


//
playerInput.actions["move"].ChangeBinding("WASD")
```



## Set a binding's control scheme from code

Control schemes allow you to group types of bindings together according to their control type, so that you can enable or disable groups of bindings. For example, you might want to enable all keyboard and mouse bindings if the user presses a keyboard button or moves the mouse.


Unity stores these on the [`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html) class as a semicolon-separated string in the  [`InputBinding.groups`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_groups) property, and you can use them for any arbitrary grouping of bindings. To enable different sets of binding groups for an [`InputActionMap`](../api/UnityEngine.InputSystem.InputActionMap.html) or [`InputActionAsset`](../api/UnityEngine.InputSystem.InputActionAsset.html), you can use the [`InputActionMap.bindingMask`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_bindingMask)/[`InputActionAsset.bindingMask`](../api/UnityEngine.InputSystem.InputActionAsset.html#UnityEngine_InputSystem_InputActionAsset_bindingMask) property. The Input System uses this to implement the concept of grouping Bindings into different  [`InputControlSchemes`](../api/UnityEngine.InputSystem.InputControlScheme.html).

Control Schemes use Binding groups to map Bindings in an [`InputActionMap`](../api/UnityEngine.InputSystem.InputActionMap.html) or [`InputActionAsset`](../api/UnityEngine.InputSystem.InputActionAsset.html) to different types of Devices. The [`PlayerInput`](player-input-component.md) class uses these to enable a matching Control Scheme for a new [user](user-management.md) joining the game, based on the Device they are playing on.



### Apply binding overrides

You can override aspects of any Binding at run-time non-destructively. Specific properties of [`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html) have an `override` variant that, if set, will take precedent over the property that they shadow.  All `override` properties are of type `String`.

|Property|Override|Description|
|--------|--------|-----------|
|[`path`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_path)|[`overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath)|Replaces the [Control path](./controls.md#control-paths) that determines which Control(s) are referenced in the binding. If [`overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath) is set to an empty string, the binding is effectively disabled.<br><br>Example: `"<Gamepad>/leftStick"`|
|[`processors`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_processors)|[`overrideProcessors`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overrideProcessors)|Replaces the [processors](processors.md) applied to the binding.<br><br>Example: `"invert,normalize(min=0,max=10)"`|
|[`interactions`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_interactions)|[`overrideInteractions`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overrideInteractions)|Replaces the [interactions](Interactions.md) applied to the binding.<br><br>Example: `"tap(duration=0.5)"`|

>NOTE: The `override` property values will not be saved along with the Actions (for example, when calling [`InputActionAsset.ToJson()`](../api/UnityEngine.InputSystem.InputActionAsset.html#UnityEngine_InputSystem_InputActionAsset_ToJson)). See [Saving and loading rebinds](#saving-and-loading-rebinds) for details about how to persist user rebinds.

To set the various `override` properties, you can use the [`ApplyBindingOverride`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_ApplyBindingOverride_UnityEngine_InputSystem_InputAction_UnityEngine_InputSystem_InputBinding_) APIs.

```CSharp
// Rebind the "fire" action to the left trigger on the gamepad.
playerInput.actions["fire"].ApplyBindingOverride("<Gamepad>/leftTrigger");
```

In most cases, it is best to locate specific bindings using APIs such as [`GetBindingIndexForControl`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_GetBindingIndexForControl_) and to then apply the override to that specific binding.

```CSharp
// Find the "Jump" binding for the space key.
var jumpAction = playerInput.actions["Jump"];
var bindingIndex = jumpAction.GetBindingIndexForControl(Keyboard.current.spaceKey);

// And change it to the enter key.
jumpAction.ApplyBindingOverride(bindingIndex, "<Keyboard>/enter");
```
