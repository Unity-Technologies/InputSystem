---
uid: input-system-action-bindings
---
# Action Bindings

- [Composite Bindings](#composite-bindings)
  - [1D axis](#1d-axis)
  - [2D vector](#2d-vector)
  - [3D vector](#3d-vector)
  - [One Modifier](#one-modifier)
  - [Two Modifiers](#two-modifiers)
  - [Writing custom Composites](#writing-custom-composites)
- [Working with Bindings](#working-with-bindings)
- [Looking up Bindings](#looking-up-bindings)
- [Changing Bindings](#changing-bindings)
  - [Applying overrides](#applying-overrides)
  - [Erasing Bindings](#erasing-bindings)
  - [Adding Bindings](#adding-bindings)
  - [Setting parameters](#setting-parameters)
- [Interactive rebinding](#interactive-rebinding)
- [Saving and loading rebinds](#saving-and-loading-rebinds)
  - [Restoring original Bindings](#restoring-original-bindings)
  - [Displaying Bindings](#displaying-bindings)
- [Control Schemes](#control-schemes)
- [Details](#details)
  - [Binding resolution](#binding-resolution)
  - [Conflicting inputs](#conflicting-inputs)
  - [Initial state check](#initial-state-check)


### Bindings

* To add a new Binding, select the Add (+) icon on the action you want to add it to, and select the binding type from the menu that appears.
* To delete an existing Binding, either right-click it and select __Delete__ from the context menu.
* To duplicate an existing Binding, either right-click it and select __Duplicate__ from the context menu.

You can add multiple bindings to an action, which is generally useful for supporting multiple types of input device. For example, in the default set of actions, the "Move" action has a binding to the left gamepad stick and the WSAD keys, which means input through any of these bindings will perform the action.

![The default "move" action with its multiple bindings highlighted](./Images/ActionWithMultipleBindings.png)<br/>
_The default "Move" action in the Actions Editor window, displaying the multiple bindings associated with it._

If you select a Binding, you can edit its properties in the right-hand pane of the window:

![Binding Properties](Images/BindingProperties.png)





An [`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html) represents a connection between an [Action](Actions.md) and one or more [Controls](Controls.md) identified by a [Control path](Controls.md#control-paths). For example, the **right trigger** of a gamepad (a control) might be bound to an an action named "accelerate", so that pulling the right trigger causes a car to accelerate in your game.

You can add multiple bindings to an action, which is generally useful for supporting multiple types of input device. For example, in the default set of actions, the "Move" action has a binding to the left gamepad stick and the WSAD keys, which means input through any of these bindings will perform the action.

You can also bind multiple controls from the same device to an action. For example, both the left and right trigger of a gamepad could be mapped to the same action, so that pulling either trigger has the same result in your game.

![The default "move" action with its multiple bindings highlighted](./Images/ActionWithMultipleBindings.png)<br/>
_The default "Move" action in the Actions Editor window, displaying the multiple bindings associated with it._


Each Binding has the following properties:

|Property|Description|
|--------|-----------|
|[`path`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_path)|[Control path](Controls.md#control-paths) that identifies the control(s) from which the Action should receive input.<br><br>Example: `"<Gamepad>/leftStick"`|
|[`overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath)|[Control path](Controls.md#control-paths) that overrides `path`. Unlike `path`, `overridePath` is not persistent, so you can use it to non-destructively override the path on a Binding. If it is set to something other than null, it takes effect and overrides `path`.  To get the path which is currently in effect (that is, either `path` or `overridePath`), you can query the [`effectivePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_effectivePath) property.|
|[`action`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_action)|The name or ID of the Action that the Binding should trigger. Note that this can be null or empty (for instance, for  [composites](#composite-bindings)). Not case-sensitive.<br><br>Example: `"fire"`|
|[`groups`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_groups)|A semicolon-separated list of Binding groups that the Binding belongs to. Can be null or empty. Binding groups can be anything, but are mostly used for [Control Schemes](#control-schemes). Not case-sensitive.<br><br>Example: `"Keyboard&Mouse;Gamepad"`|
|[`interactions`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_interactions)|A semicolon-separated list of [Interactions](Interactions.md) to apply to input on this Binding. Note that Unity appends Interactions applied to the [Action](Actions.md) itself (if any) to this list. Not case-sensitive.<br><br>Example: `"slowTap;hold(duration=0.75)"`|
|[`processors`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_processors)|A semicolon-separated list of [Processors](Processors.md) to apply to input on this Binding. Note that Unity appends Processors applied to the [Action](Actions.md) itself (if any) to this list. Not case-sensitive.<br><br>Processors on Bindings apply in addition to Processors on Controls that are providing values. For example, if you put a `stickDeadzone` Processor on a Binding and then bind it to `<Gamepad>/leftStick`, you get deadzones applied twice: once from the deadzone Processor sitting on the `leftStick` Control, and once from the Binding.<br><br>Example: `"invert;axisDeadzone(min=0.1,max=0.95)"`|
|[`id`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_id)|Unique ID of the Binding. You can use it to identify the Binding when storing Binding overrides in user settings, for example.|
|[`name`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_name)|Optional name of the Binding. Identifies part names inside [Composites](#composite-bindings).<br><br>Example: `"Positive"`|
|[`isComposite`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isComposite)|Whether the Binding acts as a [Composite](#composite-bindings).|
|[`isPartOfComposite`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isPartOfComposite)|Whether the Binding is part of a [Composite](#composite-bindings).|

To query the Bindings to a particular Action, you can use [`InputAction.bindings`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_bindings). To query a flat list of Bindings for all Actions in an Action Map, you can use [`InputActionMap.bindings`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_bindings).


### Erasing Bindings

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

### Adding Bindings

New bindings can be added to an Action using [`AddAction`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.html#UnityEngine_InputSystem_InputActionSetupExtensions_AddBinding_UnityEngine_InputSystem_InputAction_System_String_System_String_System_String_System_String_) or [`AddCompositeBinding`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.html#UnityEngine_InputSystem_InputActionSetupExtensions_AddCompositeBinding_UnityEngine_InputSystem_InputAction_System_String_System_String_System_String_).

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

### Setting parameters

A Binding may, either through itself or through its associated Action, lead to [processor](Processors.md), [interaction](Interactions.md), and/or [composite](#composite-bindings) objects being created. These objects can have parameters you can configure through in the [Binding properties view](ActionsEditor.md#bindings) of the Action editor or through the API. This configuration will give parameters their default value.

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





