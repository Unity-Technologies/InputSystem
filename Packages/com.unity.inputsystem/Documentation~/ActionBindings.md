# Input Bindings

An [`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html) represents a connection between an action and one or more controls identified by a [control path](Controls.md#control-paths). An action can have arbitrary many bindings pointed at it and the same control may be referenced by multiple bindings.

Each binding has the following properties:

|Property|Description|
|--------|-----------|
|[`path`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_path)|[Control path](Controls.md#control-paths) that identifies the control(s) from which input should be received.<br><br>Example: `"<Gamepad>/leftStick"`|
|[`overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath)|[Control path](Controls.md#control-paths) that overrides `path`. Unlike `path`, `overridePath` is not persistent, meaning that it can be used to non-destructively override the path on a binding. If it is set to something other than null, it will take effect and override `path`.  If you want to get the path which is currently being used (ie, either `path` or `overridePath`), you can query the [`effectivePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_effectivePath) property.|
|[`action`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_action)|The name or ID of the action that should be triggered from the binding. Note that this can be null or empty (e.g. for [composites](#composite-bindings)). Case-insensitive.<br><br>Example: `"fire"`|
|[`groups`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_groups)|A semicolon-separated list of binding groups that the binding belongs to. Can be null or empty. Binding groups can be anything but are mostly used for [control schemes](#control-schemes). Case-insensitive.<br><br>Example: `"Keyboard&Mouse;Gamepad"`|
|[`interactions`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_interactions)|A semicolon-separated list of [interactions](Interactions.md) to apply to input on this binding. Note that interactions applied to the [action](Actions.md) itself (if any) will get appended to this list. Case-insensitive.<br><br>Example: `"slowTap;hold(duration=0.75)"`|
|[`processors`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_processors)|A semicolon-separated list of [processors](Processors.md) to apply to input on this binding. Note that processors applied to the [action](Actions.md) itself (if any) will get appended to this list. Case-insensitive.<br><br>Note that processors applied to bindings apply __in addition__ to processors applied to controls that are providing values. If, for example, you put a `stickDeadzone` processor on a binding and then bind it to `<Gamepad>/leftStick`, you will get deadzones applied twice, once from the deadzone processor sitting on the `leftStick` control and once from the binding.<br><br>Example: `"invert;axisDeadzone(min=0.1,max=0.95)"`|
|[`id`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_id)|Unique ID of the binding. Can be used, for example, to identify the binding when storing binding overrides in user settings.|
|[`name`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_name)|Optional name of the binding. Most importantly used to identify part names inside [composites](#composite-bindings).<br><br>Example: `"Positive"`|
|[`isComposite`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isComposite)|Whether the binding acts as a [composite](#composite-bindings).|
|[`isPartOfComposite`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isPartOfComposite)|Whether the binding is part of a [composite](#composite-bindings).|

The bindings to a particular action can be queried from the action using [`InputAction.bindings`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_bindings). A flat list of bindings for all actions in a map can be queried from an action map using [`InputActionMap.bindings`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_bindings).

## Composite Bindings

Sometimes it is desirable to have several controls act in unison to mimick a different type of control. The most common example of this is using the W, A, S, and D keys on the keyboard to form a 2D vector control equivalent to mouse deltas or gamepad sticks. Another example is using two keys to form a 1D axis equivalent to a mouse scroll axis.

The problem is that with "normal" bindings, this cannot be solved easily. It is possible to bind a [`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html) to an action expecting a `Vector2` but doing so will result in an exception at runtime when trying to read a `Vector2` from a control that can deliver only a `float`.

This problem is solved by "composite bindings", i.e. bindings that are made up of other bindings. Composites themselves do not bind directly to controls but rather source values from other bindings that do and synthesize input on the fly from those values.

To see how to create composites in the editor UI, see [here](ActionAssets.md#editing-composite-bindings).

In code, composites can be created using the [`AddCompositeBinding`](../api/UnityEngine.InputSystem.InputActionSetupExtensions.html#UnityEngine_InputSystem_InputActionSetupExtensions_AddCompositeBinding_UnityEngine_InputSystem_InputAction_System_String_System_String_) syntax.

```CSharp
myAction.AddCompositeBinding("Axis")
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

Each composite is comprised of one binding with set to true and then one or more bindings immediately following it that have [`InputBinding.isPartOfComposiste`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_isPartOfComposite) set to true. This means that several consecutive entries in [`InputActionMap.bindings`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_bindings) or [`InputAction.bindings`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_bindings) together form a composite.

Composites can have parameters, just like [interactions](Interactions.md) and [processors](Processors.md).

```CSharp
myAction.AddCompositeBinding("Axis(wichSideWins=1)");
```

There are currently four composite types that come with the system out of the box: [1D-Axis](#1d-axis), [2D-Vector](#2d-vector), [Button With One Modifier](#button-with-one-modifier) and [Button With Two Modifiers](#button-with-two-modifiers).

### 1D Axis

A composite made up of two buttons, one pulling a 1D axis in its negative direction and one pulling it in its positive direction. Implemented in the [`AxisComposite`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html) class. The result is a `float`.

```CSharp
myAction.AddCompositeBinding("1DAxis") // Or just "Axis"
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

The axis composite has two part bindings.

|Part|Type|Description|
|----|----|-----------|
|[`positive`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_positive)|`Button`|Controls pulling in the positive direction, i.e. towards [`maxValue`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_maxValue).|
|[`negative`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_negative)|`Button`|Controls pulling in the negative direction, i.e. towards [`minValue`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_minValue).|

You can set the following parameters on an axis composite:

|Parameter|Description|
|---------|-----------|
|[`whichSideWins`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_whichSideWins)|What happens if both [`positive`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_positive) and [`negative`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_negative) are actuated. See table below.|
|[`minValue`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_minValue)|The value returned if the [`negative`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_negative) side is actuated. Default is -1.|
|[`maxValue`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_maxValue)|The value returned if the [`positive`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html#UnityEngine_InputSystem_Composites_AxisComposite_positive) side is actuated. Default is 1.|

If controls from both the `positive` and the `negative` side are actuated, then the resulting value of the axis composite depends on the `whichSideWin` parameter setting.

|[`WhichSideWins`](../api/UnityEngine.InputSystem.Composites.AxisComposite.WhichSideWins.html)|Description|
|---------------|-----------|
|(0) `Neither`|Neither side has precedence. The composite returns the midpoint between `minValue` and `maxValue` as a result. At their default settings, this is 0.<br><br>This is the default.|
|(1) `Positive`|The positive side has precedence and the composite returns `maxValue`.|
|(2) `Negative`|The negative side has precedence and the composite returns `minValue`.|

### 2D Vector

A composite representing a 4-way button setup akin to the d-pad on gamepads with each button representing a cardinal direction. Implemented in the [`Vector2Composite`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html) class. The result is a `Vector2`.

This composite is most useful for representing controls such as WASD.

```CSharp
myAction.AddCompositeBinding("2DVector") // Or "Dpad"
    .With("Up", "<Keyboard>/w")
    .With("Down", "<Keyboard>/s")
    .With("Left", "<Keyboard>/a")
    .With("Right", "<Keyboard>/d");
```

The 2D vector composite has four part bindings.

|Part|Type|Description|
|----|----|-----------|
|[`up`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_up)|`Button`|Controls representing `(0,1)`, i.e. +Y.|
|[`down`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_down)|`Button`|Controls representing `(0,-1)`, i.e. -Y.|
|[`left`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_left)|`Button`|Controls representing `(-1,0)`, i.e. -X.|
|[`right`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_right)|`Button`|Controls representing `(1,0)`, i.e. X.|

In addition, you can set the following parameters on a 2D vector composite:

|Parameter|Description|
|---------|-----------|
|[`normalize`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_normalize)|Whether the resulting vector should be normalized or not. If this is disabled, then, for example, pressing both [`up`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_up) and [`right`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.html#UnityEngine_InputSystem_Composites_Vector2Composite_right) will yield a vector `(1,1)` which has a length greater than one. This can be undesirable in situations where the vector's magnitude matterse. E.g. when scaling running speed by the length of the input vector.<br><br>This is true by default.|

### Button With One Modifier

A composite that requires another button to be held when pressing the button that triggers the action. Implemented in the [`ButtonWithOneModifier`](../api/UnityEngine.InputSystem.Composites.ButtonWithOneModifier.html) class. This is useful, for example, to represent keyboard shortcuts such as "CTRL+1" but is not restricted to keyboard controls, i.e. the buttons can be on any device and may be toggle buttons or full-range buttons like the gamepad triggers.

The result is a `float`.

```CSharp
myAction.AddCompositeBinding("ButtonWithOneModifier")
    .With("Button", "<Keyboard>/1")
    .With("Modifier", "<Keyboard>/leftCtrl")
    .With("Modifier", "<Keyboard>/rightCtrl");
```

The Button With One Modifier composite has two part bindings.

|Part|Type|Description|
|----|----|-----------|
|[`modifier`](../api/UnityEngine.InputSystem.Composites.ButtonWithOneModifier.html#UnityEngine_InputSystem_Composites_ButtonWithOneModifier_modifier)|`Button`|Modifier that has to be held for `button` to come through. If any of the buttons bound to the `modifier` part is pressed, the composite will assume the value of the `button` binding. If none of the buttons bound to the `modifier` part is pressed, the composite has a zero value.|
|[`button`](../api/UnityEngine.InputSystem.Composites.ButtonWithOneModifier.html#UnityEngine_InputSystem_Composites_ButtonWithOneModifier_button)|`Button`|The button whose value the composite will assume while `modifier` is pressed.|

The Button With One Modifier composite does not have parameters.

### Button With Two Modifiers

A composite that requires two other buttons to be held when pressing the button that triggers the action. Implemented in the [`ButtonWithTwoModifiers`](../api/UnityEngine.InputSystem.Composites.ButtonWithTwoModifiers.html) class. This is useful, for example, to represent keyboard shortcuts such as "CTRL+SHIFT+1" but is not restricted to keyboard controls, i.e. the buttons can be on any device and may be toggle buttons or full-range buttons like the gamepad triggers.

The result is a `float`.

```CSharp
myAction.AddCompositeBinding("ButtonWithTwoModifiers")
    .With("Button", "<Keyboard>/1")
    .With("Modifier1", "<Keyboard>/leftCtrl")
    .With("Modifier1", "<Keyboard>/rightCtrl")
    .With("Modifier2", "<Keyboard>/leftShift")
    .With("Modifier2", "<Keyboard>/rightShift");
```

The Button With Two Modifiers composite has three part bindings.

|Part|Type|Description|
|----|----|-----------|
|[`modifier1`](../api/UnityEngine.InputSystem.Composites.ButtonWithTwoModifiers.html#UnityEngine_InputSystem_Composites_ButtonWithTwoModifiers_modifier1)|`Button`|First modifier that has to be held for `button` to come through. If none of the buttons bound to the `modifier1` part is pressed, the composite has a zero value.|
|[`modifier2`](../api/UnityEngine.InputSystem.Composites.ButtonWithTwoModifiers.html#UnityEngine_InputSystem_Composites_ButtonWithTwoModifiers_modifier2)|`Button`|Second modifier that has to be held for `button` to come through. If none of the buttons bound to the `modifier2` part is pressed, the composite has a zero value.|
|[`button`](../api/UnityEngine.InputSystem.Composites.ButtonWithTwoModifiers.html#UnityEngine_InputSystem_Composites_ButtonWithTwoModifiers_button)|`Button`|The button whose value the composite will assume while both `modifier1` and `modifier2` are pressed.|

The Button With Two Modifiers composite does not have parameters.

### Writing Custom Composites

New types of composites can be defined and registered with the API. They are treated the same as predefined types &mdash; which are internally defined and registered the same way.

To define a new type of composite, create a class based on [`InputBindingComposite<TValue>`](../api/UnityEngine.InputSystem.InputBindingComposite-1.html).

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

The composite should now show up in the editor UI when adding a binding and it can now be used in scripts.

```CSharp
    myAction.AddCompositeBinding("custom(floatParameter=2.0)")
        .With("firstpart", "<Gamepad>/buttonSouth")
        .With("secondpart", "<Gamepad>/buttonNorth");
```

It is also possible to define a custom parameter editor for the composite by deriving from [`InputParameterEditor<TObject>`](../api/UnityEngine.InputSystem.Editor.InputParameterEditor-1.html).

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

## Binding Resolution

When the input system accesses the [controls](Controls.md) bound to an action for the first time, the action will "resolve" the action's bindings to match them to existing controls on existing devices. In this process, the action will call [`InputSystem.FindControls<>()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_FindControls__1_System_String_UnityEngine_InputSystem_InputControlList___0___) (filtering for devices assigned to the InputActionMap, if there are any) for the binding path of each of the action's bindings. This will then result in a list of resolved controls now bound to the action. Note that a single [binding path](Controls.md#control-paths) can match multiple controls:

* A specific device path such as `<DualShockGamepad>/buttonEast` will match the "circle button" on a [PlayStation controllers](Gamepad.md#playstation-controllers). If you have multiple PlayStation controllers connected, it will resolve to the "circle button" on each of these controllers.

* An abstract device path such as `<Gamepad>/buttonEast` will match the right action button on any connected gamepad. If you have a PlayStation controller and an [Xbox controller](Gamepad.md#xbox) connected, it will resolve to the "circle button" on the PlayStation controller, and to the "B Button" on the Xbox controller.

* A binding path can also contain wildcards, such as `<Gamepad>/button*`. This will match any control on any gamepad with a name starting with "button", which will match all the four action buttons on any connected gamepad. A different example: `*/{Submit}` will match any control tagged with the "Submit" [usage](Controls.md#control-usages) on any device.

The controls that an action has resolved to can be queried from the action using [`InputAction.controls`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_controls). This can also be done when the action has not yet been enabled.

### Choosing Which Devices to Use

By default, actions will resolve their bindings against all devices present in the system (i.e. [`InputSystem.devices`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_devices)). This means that, for example, if there are two gamepads present in the system, a binding to `<Gamepad>/buttonSouth` will pick up __both__ gamepads and alows the action to be used from either.

This behavior can be overridden by restricting [`InputActionAssets`](../api/UnityEngine.InputSystem.InputActionAsset.html) or individual [`InputActionMaps`](../api/UnityEngine.InputSystem.InputActionMap.html) to a specific set of devices. If this is done, binding resolution will take only the controls of the given devices into account.

```
    var actionMap = new InputActionMap();

    // Restrict the action map to just the first gamepad.
    actionMap.devices = new[] { Gamepad.all[0] };
```

>NOTE: [`InputUser`](UserManagement.md) and [`PlayerInput`](Components.md) make use of this facility automatically. I.e. they will set [`InputActionMap.devices`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_devices) automatically based on the devices that are paired to the user/player.

## Disambiguation

If multiple controls are bound to an action, the input system will monitor input from each bound control to feed the action. But then, how does the system decide which of the bound controls to use for the value of the action? For instance if you have a binding to `<Gamepad>/leftStick`, and you have multiple connected gamepads, which gamepad's stick will provide the input value for the action? We call this control the control which is "driving the action". Which control is currently driving the action is decided in a process called "disambiguation". During the disambiguation process, the input system looks at the value of each control bound to an action. If the [magnitude](Controls.md#control-actuation) of the input from any control is higher then the magnitude of the control currently driving the action, then the control with the higher magnitude becomes the new control driving the action. So in the above example of `<Gamepad>/leftStick` binding to multiple gamepads, the control driving the action will always be the left stick which is actuated the furthest of all the gamepads. You can query which control is currently driving the action by checking the [`InputAction.CallbackContext.control`](../api/UnityEngine.InputSystem.InputAction.CallbackContext.html#UnityEngine_InputSystem_InputAction_CallbackContext_control) property in an [action callback](Actions.md#started-performed-and-canceled-callbacks).

If yo don't want your action to perform disambiguation, you can set your action type to [Pass-Through](Actions.md#pass-through). Pass-Through actions skip disambiguation, and are trigged by changes to any bound control. The value of a Pass-Through action will always be the value of whichever bound control changed the last.

## Initial State Check

Actions with the type set to [Value](Actions.md#value) will perform an initial state check when they are first enabled to check the current state of any bound control, and set the action's value to the highest value of any bound control.

Actions with the type set to [Button](Actions.md#button) will not perform any initial state check, so that only buttons pressed after the action was enabled will have any effect on the action.

## Runtime Rebinding

It can be desirable to allow users to choose their own bindings to let them map the controls to match personal preferences. This can be done using the [`InputActionRebindingExtensions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) class. Call the [`PerformInteractiveRebinding()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_PerformInteractiveRebinding_UnityEngine_InputSystem_InputAction_) method on an action to create a rebinding operation, which will wait for the input system to register any input to assign to the action as a new binding. Once it detects any control being actuated on any device which matches the action's expected control type, it will then assign the control path for that control to the action's bindings using [`InputBinding.overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath). If multiple controls are actuated, it will choose the control with the highest [magnitude](Controls.md#control-actuation).

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

* Choose expected control types ([`WithExpectedControlType()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithExpectedControlType_System_Type_)).

* Exclude certain controls ([`WithControlsExcluding()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithControlsExcluding_System_String_))

* Set a control to cancel the operation ([`WithCancelingThrough()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithCancelingThrough_UnityEngine_InputSystem_InputControl_))

* Choose which bindings to apply the operation on if the action has multiple bindings ([`WithTargetBinding()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithTargetBinding_System_Int32_), [`WithBindingGroup()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithBindingGroup_System_String_), [`WithBindingMask()`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html#UnityEngine_InputSystem_InputActionRebindingExtensions_RebindingOperation_WithBindingMask_System_Nullable_UnityEngine_InputSystem_InputBinding__)).

Refer to the [scripting API reference for `InputActionRebindingExtensions.RebindingOperation`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.html) for a full overview.

### Showing Current Bindings

Both in rebinding UIs as well for on-screen hints during gameplay, it can be useful to know what an action is currently bound to (taking any potentially active rebindings into account). You can use [`InputBinding.effectivePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_effectivePath) to get the currently active path for a binding (ie, either [`path`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_path) or [`overridePath`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_overridePath) if applicable). Then you can use [`InputControlPath.ToHumanReadableString`](../api/UnityEngine.InputSystem.InputControlPath.html#UnityEngine_InputSystem_InputControlPath_ToHumanReadableString_System_String_UnityEngine_InputSystem_InputControlPath_HumanReadableStringOptions_) to turn that into a meaningful control name.

```CSharp
    m_RebindButtonName.text = InputControlPath.ToHumanReadableString(m_Action.bindings[0].effectivePath);
```

## Control Schemes

A binding can belong to any number of binding "groups". This is stored on the binding class as a semicolon-separated string, in the [`InputBinding.groups`](../api/UnityEngine.InputSystem.InputBinding.html#UnityEngine_InputSystem_InputBinding_groups) property. This can be used for any arbitrary grouping of bindings. You can enable different sets of binding groups for an [`InputActionMap`](../api/UnityEngine.InputSystem.InputActionMap.html) or [`InputActionAsset`](../api/UnityEngine.InputSystem.InputActionAsset.html) using the [`InputActionMap.bindingMask`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_bindingMask)/[`InputActionAsset.bindingMask`](../api/UnityEngine.InputSystem.InputActionAsset.html#UnityEngine_InputSystem_InputActionAsset_bindingMask) property.

This is used by the input system to implement the concept of grouping bindings into different [`InputControlSchemes`](../api/UnityEngine.InputSystem.InputControlScheme.html). Control schemes use binding groups to map bindings in an [`InputActionMap`](../api/UnityEngine.InputSystem.InputActionMap.html) or [`InputActionAsset`](../api/UnityEngine.InputSystem.InputActionAsset.html) to different types of devices. This is used by the [`PlayerInput`](Components.md) class to enable a matching control scheme for a new [user](UserManagement.md) joining the game, based on the device they are playing on.
