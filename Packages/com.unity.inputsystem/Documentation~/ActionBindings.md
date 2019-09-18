# Input Bindings

An [`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html) represents a connection between an [Action](Actions.md) and one or more [Controls](Controls.md) identified by a [Control path](Controls.md#control-paths). An Action have an arbitrary number of Bindings pointed at it. Multiple Bindings can reference the same Control.

Each Binding has the following properties:

|Property|Description|
|--------|-----------|
|[`path`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_path)|[Control path](Controls.md#control-paths) that identifies the control(s) from which the Action should receive input.<br><br>Example: `"<Gamepad>/leftStick"`|
|[`overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath)|[Control path](Controls.md#control-paths) that overrides `path`. Unlike `path`, `overridePath` is not persistent, meaning that it can be used to non-destructively override the path on a Binding. If it is set to something other than null, it will take effect and override `path`.  If you want to get the path which is currently being used (that is either `path` or `overridePath`), you can query the [`effectivePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_effectivePath) property.|
|[`action`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_action)|The name or ID of the Action that the Binding should trigger. Note that this can be null or empty (for instance, for  [composites](#composite-bindings)). Case-insensitive.<br><br>Example: `"fire"`|
|[`groups`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_groups)|A semicolon-separated list of Binding groups that the Binding belongs to. Can be null or empty. Binding groups can be anything, but are mostly used for [Control Schemes](#control-schemes). Case-insensitive.<br><br>Example: `"Keyboard&Mouse;Gamepad"`|
|[`interactions`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_interactions)|A semicolon-separated list of [Interactions](Interactions.md) to apply to input on this Binding. Note that Unity appends Interactions applied to the [Action](Actions.md) itself (if any) to this list. Case-insensitive.<br><br>Example: `"slowTap;hold(duration=0.75)"`|
|[`processors`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_processors)|A semicolon-separated list of [Processors](Processors.md) to apply to input on this Binding. Note that Unity appends Processors applied to the [Action](Actions.md) itself (if any) to this list. Case-insensitive.<br><br>Processors applied to Bindings apply in addition to Processors applied to Controls that are providing values. If, for example, you put a `stickDeadzone` Processor on a Binding and then bind it to `<Gamepad>/leftStick`, you will get deadzones applied twice: once from the deadzone Processor sitting on the `leftStick` Control and once from the Binding.<br><br>Example: `"invert;axisDeadzone(min=0.1,max=0.95)"`|
|[`id`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_id)|Unique ID of the Binding. You can use it to identify the Binding when storing Binding overrides in user settings, for example.|
|[`name`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_name)|Optional name of the Binding. Most importantly used to identify part names inside [Composites](#composite-bindings).<br><br>Example: `"Positive"`|
|[`isComposite`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isComposite)|Whether the Binding acts as a [Composite](#composite-bindings).|
|[`isPartOfComposite`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isPartOfComposite)|Whether the Binding is part of a [Composite](#composite-bindings).|

You can query the Bindings to a particular Action using [`InputAction.bindings`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_bindings). You can also query a flat list of Nindings for all Actions in an Action Map using [`InputActionMap.bindings`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_bindings).

## Composite Bindings

Sometimes, you might want to have several Controls act in unison to mimic a different type of Control. The most common example of this is using the W, A, S, and D keys on the keyboard to form a 2D vector Control equivalent to mouse deltas or gamepad sticks. Another example is to use two keys to form a 1D axis equivalent to a mouse scroll axis.

This is difficult to implement with normal Bindings. You can bind a  [`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html) to an action expecting a `Vector2`, but doing so will result in an exception at runtime when trying to read a `Vector2` from a Control that can deliver only a `float`.

Composite Bindings (that is, Bindings that are made up of other Bindings) solve this problem. Composites themselves don't bind directly to Controls, but rather source values from other Bindings that do, and then synthesize input on the fly from those values.

To see how to create Composites in the editor UI, see documentation on [editing Composite Bindings](ActionAssets.md#editing-composite-bindings).

In code, you can create Composites using the [`AddCompositeBinding`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.html#UnityEngine_InputSystem_InputActionSetupExtensions_AddCompositeBinding_UnityEngine_InputSystem_InputAction_System_String_System_String_) syntax.

```CSharp
myAction.AddCompositeBinding("Axis")
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

Each Composite consists of one Binding that has [`InputBinding.isComposiste`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isComposite) set to true, followed by one or more Bindings that have [`InputBinding.isPartOfComposiste`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isPartOfComposite) set to true. In other words, several consecutive entries in [`InputActionMap.bindings`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_bindings) or [`InputAction.bindings`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_bindings) together form a Composite.

Composites can have parameters, just like [Interactions](Interactions.md) and [Processors](Processors.md).

```CSharp
myAction.AddCompositeBinding("Axis(wichSideWins=1)");
```

There are currently four Composite types that come with the system out of the box: [1D-Axis](#1d-axis), [2D-Vector](#2d-vector), [Button With One Modifier](#button-with-one-modifier) and [Button With Two Modifiers](#button-with-two-modifiers).

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
|[`positive`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_positive)|`Button`|Controls pulling in the positive direction, that is, towards [`maxValue`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_maxValue).|
|[`negative`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_negative)|`Button`|Controls pulling in the negative direction, that is, towards [`minValue`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_minValue).|

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

### 2D vector

A Composite that represents a 4-way button setup like the D-pad on gamepads, with each button representing a cardinal direction. Implemented in the [`Vector2Composite`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html) class. The result is a `Vector2`.

This Composite is most useful for representing controls such as WASD keyboard input.

```CSharp
myAction.AddCompositeBinding("2DVector") // Or "Dpad"
    .With("Up", "<Keyboard>/w")
    .With("Down", "<Keyboard>/s")
    .With("Left", "<Keyboard>/a")
    .With("Right", "<Keyboard>/d");
```

The 2D vector Composite has four part Bindings.

|Part|Type|Description|
|----|----|-----------|
|[`up`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_up)|`Button`|Controls representing `(0,1)`, that is, +Y.|
|[`down`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_down)|`Button`|Controls representing `(0,-1)`, that is, -Y.|
|[`left`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_left)|`Button`|Controls representing `(-1,0)`, that is, -X.|
|[`right`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_right)|`Button`|Controls representing `(1,0)`, that is, X.|

In addition, you can set the following parameters on a 2D vector Composite:

|Parameter|Description|
|---------|-----------|
|[`normalize`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_normalize)|Whether the resulting vector should be normalized or not. If this is disabled, then, for example, pressing both [`up`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_up) and [`right`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_right) will yield a vector `(1,1)` which has a length greater than 1. This can be undesirable in situations where the vector's magnitude matters (for example, when scaling running speed by the length of the input vector).<br><br>This is true by default.|

### Button with one modifier

A Composite that requires another button to be held when pressing the button that triggers the Action. Implemented in the [`ButtonWithOneModifier`](../api/UnityEngine.InputSystem.Composites.ButtonWithOneModifier.html) class. This is useful, for example, to represent keyboard shortcuts such as Ctrl+1 but isn't restricted to keyboard controls. The buttons can be on any Device, and can be toggle buttons or full-range buttons such as gamepad triggers.

The result is a `float`.

```CSharp
myAction.AddCompositeBinding("ButtonWithOneModifier")
    .With("Button", "<Keyboard>/1")
    .With("Modifier", "<Keyboard>/leftCtrl")
    .With("Modifier", "<Keyboard>/rightCtrl");
```

The button with one modifier Composite has two part Bindings.

|Part|Type|Description|
|----|----|-----------|
|[`modifier`](../api/UnityEngine.InputSystem.Composites.ButtonWithOneModifier.html#UnityEngine_InputSystem_Composites_ButtonWithOneModifier_modifier)|`Button`|Modifier that has to be held for `button` to come through. If any of the buttons bound to the `modifier` part is pressed, the Composite assumes the value of the `button` Binding. If none of the buttons bound to the `modifier` part are pressed, the Composite has a value of 0.|
|[`button`](../api/UnityEngine.InputSystem.Composites.ButtonWithOneModifier.html#UnityEngine_InputSystem_Composites_ButtonWithOneModifier_button)|`Button`|The button whose value the Composite will assume while `modifier` is pressed.|

This Composite has no parameters.

### Button with two modifiers

A Composite that requires two other buttons to be held when pressing the button that triggers the Action. Implemented in the [`ButtonWithTwoModifiers`](../api/UnityEngine.InputSystem.Composites.ButtonWithTwoModifiers.html) class. This is useful, for example, to represent keyboard shortcuts such as "Ctrl+Shift+1" but isn't restricted to keyboard controls. The buttons can be on any Device, and can be toggle buttons or full-range buttons such as gamepad triggers.

The result is a `float`.

```CSharp
myAction.AddCompositeBinding("ButtonWithTwoModifiers")
    .With("Button", "<Keyboard>/1")
    .With("Modifier1", "<Keyboard>/leftCtrl")
    .With("Modifier1", "<Keyboard>/rightCtrl")
    .With("Modifier2", "<Keyboard>/leftShift")
    .With("Modifier2", "<Keyboard>/rightShift");
```

The button with two modifiers Composite has three part Bindings.

|Part|Type|Description|
|----|----|-----------|
|[`modifier1`](../api/UnityEngine.InputSystem.Composites.ButtonWithTwoModifiers.html#UnityEngine_InputSystem_Composites_ButtonWithTwoModifiers_modifier1)|`Button`|First modifier that has to be held for `button` to come through. If none of the buttons bound to the `modifier1` part is pressed, the composite has a value of 0.|
|[`modifier2`](../api/UnityEngine.InputSystem.Composites.ButtonWithTwoModifiers.html#UnityEngine_InputSystem_Composites_ButtonWithTwoModifiers_modifier2)|`Button`|Second modifier that has to be held for `button` to come through. If none of the buttons bound to the `modifier2` part is pressed, the composite has a value of 0.|
|[`button`](../api/UnityEngine.InputSystem.Composites.ButtonWithTwoModifiers.html#UnityEngine_InputSystem_Composites_ButtonWithTwoModifiers_button)|`Button`|The button whose value the Composite assumes while both `modifier1` and `modifier2` are pressed.|

This Composite has no parameters.

### Writing custom Composites

You can define new types of Composites, and register them with the API. Unity treats these the same as predefined types, which are internally defined and registered the same way.

To define a new type of Composite, create a class based on [`InputBindingComposite<TValue>`](../api/UnityEngine.InputSystem.InputBindingComposite-1.html).

```CSharp
// Use InputBindingComposite<TValue> as a base class for a composite that returns
// values of type TValue.
// NOTE: It is possible to define a composite that returns different kinds of values
//       but doing so requires deriving directly from InputBindingComposite.
#if UNITY_EDITOR
[InitializeOnLoad] // Automatically register in editor.
#endif
public class CustomComposite : InputBindingComposite<float>
{
    // Each part binding is represented as a field of type int and annotated with
    // InputControlAttribute. Setting "layout" allows to restrict the controls that
    // are made available for picking in the UI.
    //
    // On creation, the int value will be set to an integer identifier for the binding
    // part. This identifier can be used to read values from InputBindingCompositeContext.
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

    [RuntimeInitializeOnLoadMethod]
    static void Init() {} // Trigger static constructor.
}
```

The Composite should now appear in the editor UI when adding a Binding, and you can now use it in scripts.

```CSharp
    myAction.AddCompositeBinding("custom(floatParameter=2.0)")
        .With("firstpart", "<Gamepad>/buttonSouth")
        .With("secondpart", "<Gamepad>/buttonNorth");
```

You can also define a custom parameter editor for the Composite by deriving from  [`InputParameterEditor<TObject>`](../api/UnityEngine.InputSystem.Editor.InputParameterEditor-1.html).

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

## Binding resolution

When the Input System accesses the [Controls](Controls.md) bound to an Action for the first time, the Action resolves its Bindings to match them to existing Controls on existing Devices. In this process, the Action calls [`InputSystem.FindControls<>()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_FindControls__1_System_String_UnityEngine_InputSystem_InputControlList___0___) (filtering for devices assigned to the InputActionMap, if there are any) for the Binding path of each of the Action's bindings. This creates a list of resolved Controls that are now bound to the Action.

Note that a single [Binding path](Controls.md#control-paths) can match multiple Controls:

* A specific Device path such as `<DualShockGamepad>/buttonEast` matches the "circle button" on a [PlayStation controllers](Gamepad.md#playstation-controllers). If you have multiple PlayStation controllers connected, it resolves to the "circle" button on each of these controllers.

* An abstract Device path such as `<Gamepad>/buttonEast` matches the right action button on any connected gamepad. If you have a PlayStation controller and an [Xbox controller](Gamepad.md#xbox) connected, it resolves the "circle button" on the PlayStation controller, and to the "B Button" on the Xbox controller.

* A Binding path can also contain wildcards, such as `<Gamepad>/button*`. This matches any Control on any gamepad with a name starting with "button", which matches all the four action buttons on any connected gamepad. A different example: `*/{Submit}` matches any Control tagged with the "Submit" [usage](Controls.md#control-usages) on any Device.

You can query the Controls that an Action resolves to using [`InputAction.controls`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_controls). You can also run this query if the Action is disabled.

### Choosing which Devices to use

By default, Actions will resolve their Bindings against all Devices present in the Input  System (that is, [`InputSystem.devices`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_devices)). For example, if there are two gamepads present in the system, a Binding to `<Gamepad>/buttonSouth` picks up both gamepads and allows the Action to be used from either.

You can override this behavior by restricting [`InputActionAssets`](../api/UnityEngine.InputSystem.InputActionAsset.html) or individual [`InputActionMaps`](../api/UnityEngine.InputSystem.InputActionMap.html) to a specific set of Devices. If you do this, Binding resolution only takes the Controls of the given Devices into account.

```
    var actionMap = new InputActionMap();

    // Restrict the action map to just the first gamepad.
    actionMap.devices = new[] { Gamepad.all[0] };
```

>__Note__: [`InputUser`](UserManagement.md) and [`PlayerInput`](Components.md) make use of this facility automatically. They set [`InputActionMap.devices`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_devices) automatically based on the Devices that are paired to the user.

## Disambiguation

If multiple Controls are bound to an Action, the Input System monitors input from each bound Control to feed the Action. But then, how does the system decide which of the bound Controls to use for the value of the Action? For instance, if you have a Binding to `<Gamepad>/leftStick`, and you have multiple connected gamepads, which gamepad's stick will provide the input value for the Action? We call this Control the Control which is driving the Action.

Unity decides which Control is currently driving the Action in a process called disambiguation. During the disambiguation process, the Input System looks at the value of each Control bound to an Action. If the [magnitude](Controls.md#control-actuation) of the input from any Control is higher then the magnitude of the Control currently driving the Action, then the Control with the higher magnitude becomes the new Control driving the Action. In the above example of `<Gamepad>/leftStick` binding to multiple gamepads, the Control driving the Action is the left stick which is actuated the furthest of all the gamepads. You can query which Control is currently driving the Action by checking the [`InputAction.CallbackContext.control`](../api/UnityEngine.InputSystem.InputAction.CallbackContext.html#UnityEngine_InputSystem_InputAction_CallbackContext_control) property in an [Action callback](Actions.md#started-performed-and-canceled-callbacks).

If you don't want your Action to perform disambiguation, you can set your Action type to [Pass-Through](Actions.md#pass-through). Pass-Through Actions skip disambiguation, changes to any bound Control trigger them. The value of a Pass-Through Action is the value of whichever bound Control changed most recently.

## Initial state check

Actions with the type set to [Value](Actions.md#value) perform an initial state check when they are first enabled to check the current state of any bound Control, and set the Action's value to the highest value of any bound Control.

Actions with the type set to [Button](Actions.md#button) don't perform any initial state check, so that only buttons pressed after the Action was enabled have any effect on the Action.

## Run time rebinding

To allow users to choose their own Bindings, use the  [`InputActionRebindingExtensions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) class. Call the [`PerformInteractiveRebinding()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_PerformInteractiveRebinding_UnityEngine_InputSystem_InputAction_) method on an Action to create a rebinding operation, which waits for the Input System to register any input to assign to the Action as a new Binding. Once it detects any Control being actuated on any Device which matches the Action's expected Control type, it then assigns the Control path for that Control to the Action's Bindings using [`InputBinding.overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath). If multiple Controls are actuated, the operation chooses the Control with the highest [magnitude](Controls.md#control-actuation).

```C#
    void RemapButtonClicked(InputAction actionToRebind)
    {
        var rebindOperation = actionToRebind.PerformInteractiveRebinding()
                    // To avoid accidental input from mouse motion
                    .WithControlsExcluding("Mouse")
                    .OnMatchWaitForAnother(0.1f)
                    .Start();
    }
```

The [`InputActionRebindingExtensions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) API is highly configurable to match your needs. Among other things, you can:

* Choose expected Control types ([`WithExpectedControlType()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithExpectedControlType_System_Type_)).

* Exclude certain Controls ([`WithControlsExcluding()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithControlsExcluding_System_String_)).

* Set a Control to cancel the operation ([`WithCancelingThrough()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithCancelingThrough_UnityEngine_InputSystem_InputControl_)).

* Choose which Bindings to apply the operation on if the Action has multiple Bindings ([`WithTargetBinding()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithTargetBinding_System_Int32_), [`WithBindingGroup()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithBindingGroup_System_String_), [`WithBindingMask()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithBindingMask_System_Nullable_UnityEngine_InputSystem_InputBinding__)).

Refer to the [scripting API reference for `InputActionRebindingExtensions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) for a full overview.

### Showing current Bindings

It can be useful for the user to know what an Action is currently bound to (taking any potentially active rebindings into account), both in rebinding UIs as well as for on-screen hints while the app is running. You can use [`InputBinding.effectivePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_effectivePath) to get the currently active path for a Binding (either [`path`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_path) or [`overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath), if applicable). You can then use [`InputControlPath.ToHumanReadableString`](../api/UnityEngine.InputSystem.InputControlPath.html#UnityEngine_InputSystem_InputControlPath_ToHumanReadableString_System_String_UnityEngine_InputSystem_InputControlPath_HumanReadableStringOptions_) to turn that into a meaningful control name.

```CSharp
    m_RebindButtonName.text = InputControlPath.ToHumanReadableString(m_Action.bindings[0].effectivePath);
```

## Control Schemes

A Binding can belong to any number of Binding groups. Unity stores these on the [`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html) class as a semicolon-separated string in the  [`InputBinding.groups`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_groups) property, and you can use them for any arbitrary grouping of bindings. You can enable different sets of binding groups for an [`InputActionMap`](../api/UnityEngine.InputSystem.InputActionMap.html) or [`InputActionAsset`](../api/UnityEngine.InputSystem.InputActionAsset.html) using the [`InputActionMap.bindingMask`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_bindingMask)/[`InputActionAsset.bindingMask`](../api/UnityEngine.InputSystem.InputActionAsset.html#UnityEngine_InputSystem_InputActionAsset_bindingMask) property. The Input System uses this to implement the concept of grouping Bindings into different  [`InputControlSchemes`](../api/UnityEngine.InputSystem.InputControlScheme.html).

Control Schemes use Binding groups to map Bindings in an [`InputActionMap`](../api/UnityEngine.InputSystem.InputActionMap.html) or [`InputActionAsset`](../api/UnityEngine.InputSystem.InputActionAsset.html) to different types of Devices. The [`PlayerInput`](Components.md) class uses these to enable a matching Control Scheme for a new [user](UserManagement.md) joining the game, based on the Device they are playing on.
