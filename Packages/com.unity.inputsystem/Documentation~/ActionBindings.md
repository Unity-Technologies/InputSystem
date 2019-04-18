    ////TODO: mention/explain disambiguation on this page
    ////REVIEW: is "Action Bindings" really the best name for this?
    ////        (the respective API is called "InputBinding")

# Action Bindings

An `InputBinding` represents a connection between an action and one or more controls identified by a [control path](Controls.md#control-paths). An action can have arbitrary many bindings pointed at it and the same control may be referenced by multiple bindings.

Each binding has the following properties:

|Property|Description|
|--------|-----------|
|`path`|[Control path](Controls.md#control-paths) that identifies the control(s) from which input should be received.<br><br>Example: `"<Gamepad>/leftStick"`|
|`overridePath`|[Control path](Controls.md#control-paths) that overrides `path`. Unlike `path`, `overridePath` is not persistent, meaning that it can be used to non-destructively override the path on a binding. If it is set to something other than null, it will take effect and override `path`.|
|`action`|The name or ID of the action that should be triggered from the binding. Note that this can be null or empty (e.g. for [composites](#composite-bindings)). Case-insensitive.<br><br>Example: `"fire"`|
|`groups`|A semicolon-separated list of binding groups that the binding belongs to. Can be null or empty. Binding groups can be anything but are mostly used for [control schemes](#control-schemes). Case-insensitive.<br><br>Example: `"Keyboard&Mouse;Gamepad"`|
|`interactions`|A semicolon-separated list of [interactions](Interactions.md) to apply to input on this binding. Note that interactions applied to the [action](Actions.md) itself (if any) will get appended to this list. Case-insensitive.<br><br>Example: `"slowTap;hold(duration=0.75)"`|
|`processors`|A semicolon-separated list of [processors](Processors.md) to apply to input on this binding. Note that processors applied to the [action](Actions.md) itself (if any) will get appended to this list. Case-insensitive.<br><br>Note that processors applied to bindings apply __in addition__ to processors applied to controls that are providing values. If, for example, you put a `stickDeadzone` processor on a binding and then bind it to `<Gamepad>/leftStick`, you will get deadzones applied twice, once from the deadzone processor sitting on the `leftStick` control and once from the binding.<br><br>Example: `"invert;axisDeadzone(min=0.1,max=0.95)"`|
|`id`|Unique ID of the binding. Can be used, for example, to identify the binding when storing binding overrides in user settings.|
|`name`|Optional name of the binding. Most importantly used to identify part names inside [composites](#composite-bindings).<br><br>Example: `"Positive"`|
|`isComposite`|Whether the binding acts as a [composite](#composite-bindings).|
|`isPartOfComposite`|Whether the binding is part of a [composite](#composite-bindings).|

The bindings to a particular action can be queried from the action using `InputAction.bindings`. A flat list of bindings for all actions in a map can be queried from an action map using `InputActionMap.bindings`.

## Composite Bindings

Sometimes it is desirable to have several controls act in unison to mimick a different type of control. The most common example of this is using the W, A, S, and D keys on the keyboard to form a 2D vector control equivalent to mouse deltas or gamepad sticks. Another example is using two keys to form a 1D axis equivalent to a mouse scroll axis.

The problem is that with "normal" bindings, this cannot be solved easily. It is possible to bind a `Button` to an action expecting a `Vector2` but doing so will result in an exception at runtime when trying to read a `Vector2` from a control that can deliver only a `float`.

This problem is solved by "composite bindings", i.e. bindings that are made up of other bindings. Composites themselves do not bind directly to controls but rather source values from other bindings that do and synthesize input on the fly from those values.

>NOTE: Actions set on bindings that are part of composites are ignored. The composite as a whole can trigger an action. Individual parts of the composite cannot.

To see how to create composites in the editor UI, see [here](ActionEditor.md#editing-composite-bindings).

In code, composites can be created using the `AddCompositeBinding` syntax.

```CSharp
myAction.AddCompositeBinding("Axis")
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

Each composite is comprised of one binding with set to true and then one or more bindings immediately following it that have `InputBinding.isPartOfComposiste` set to true. This means that several consecutive entries in `InputActionMap.bindings` or `InputAction.bindings` together form a composite.

Composites can have parameters, just like [interactions](Interactions.md#interaction-parameters) and [processors](Processors.md#processor-parameters).

```CSharp
myAction.AddCompositeBinding("Axis(wichSideWins=1)");
```

There are currently two composite types that come with the system out of the box: [1D-Axis](#1d-axis) and [2D-Vector](#2d-vector).

### 1D Axis

A composite made up of two buttons, one pulling a 1D axis in its negative direction and one pulling it in its positive direction. The result is a `float`.

```CSharp
myAction.AddCompositeBinding("1DAxis") // Or just "Axis"
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

The axis composite has two part bindings.

|Part|Type|Description|
|----|----|-----------|
|`positive`|`Button`|Controls pulling in the positive direction, i.e. towards `maxValue`.|
|`negative`|`Button`|Controls pulling in the negative direction, i.e. towards `minValue`.|

You can set the following parameters on an axis composite:

|Parameter|Description|
|---------|-----------|
|`whichSideWins`|What happens if both `positive` and `negative` are actuated. See table below.|
|`minValue`|The value returned if the `negative` side is actuated. Default is -1.|
|`maxValue`|The value returned if the `positive` side is actuated. Default is 1.|

If controls from both the `positive` and the `negative` side are actuated, then the resulting value of the axis composite depends on the `whichSideWin` parameter setting.

|`WhichSideWins`|Description|
|---------------|-----------|
|(0) `Neither`|Neither side has precedence. The composite returns the midpoint between `minValue` and `maxValue` as a result. At their default settings, this is 0.<br><br>This is the default.|
|(1) `Positive`|The positive side has precedence and the composite returns `maxValue`.|
|(2) `Negative`|The negative side has precedence and the composite returns `minValue`.|

### 2D Vector

A composite representing a 4-way button setup akin to the d-pad on gamepads with each button representing a cardinal direction. The result is a `Vector2`.

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
|`up`|`Button`|Controls representing `(0,1)`, i.e. +Y.|
|`down`|`Button`|Controls representing `(0,-1)`, i.e. -Y.|
|`left`|`Button`|Controls representing `(-1,0)`, i.e. -X.|
|`right`|`Button`|Controls representing `(1,0)`, i.e. X.|

In addition, you can set the following parameters on a 2D vector composite:

|Parameter|Description|
|---------|-----------|
|`normalize`|Whether the resulting vector should be normalized or not. If this is disabled, then, for example, pressing both `up` and `right` will yield a vector `(1,1)` which has a length greater than one. This can be undesirable in situations where the vector's magnitude matterse. E.g. when scaling running speed by the length of the input vector.<br><br>This is true by default.|

### Writing Custom Composites

New types of composites can be defined and registered with the API. They are treated the same as predefined types &mdash; which are internally defined and registered the same way.

To define a new type of composite, create a class based on `InputBindingComposite<TValue>`.

```CSharp
// Use InputBindingComposite<TValue> as a base class for a composite that returns
// values of type TValue.
// NOTE: It is possible to define a composite that returns different kinds of values
//       but doing so requires deriving directly from InputBindingComposite.
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
}
```

To register the composite, call `InputSystem.RegisterBindingComposite<TComposite>()`. This is best done during from `InitializeOnLoad`/`RuntimeInitializeOnLoad` code.

```CSharp
// Can give custom name or use default (type name with "Composite" clipped off).
// Same composite can be registered multiple times with different names to introduce
// aliases.
InputSystem.RegisterBindingComposite<CustomComposite>();
```

The composite should now show up in the editor UI when adding a binding and it can now be used in scripts.

```CSharp
    myAction.AddCompositeBinding("custom(floatParameter=2.0)")
        .With("firstpart", "<Gamepad>/buttonSouth")
        .With("secondpart", "<Gamepad>/buttonNorth");
```

It is also possible to define a custom parameter editor for the composite by deriving from `InputParameterEditor<TObject>`.

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

...

The controls that an action has resolved to can be queried from the action using `InputAction.controls`. This can also be done when the action has not yet been enabled.

### Choosing Which Devices to Use

By default, actions will resolve their bindings against all devices present in the system (i.e. `InputSystem.devices`). This means that, for example, if there are two gamepads present in the system, a binding to `<Gamepad>/buttonSouth` will pick up __both__ gamepads and alows the action to be used from either.

This behavior can be overridden by restricting `InputActionAssets` or individual `InputActionMaps` to a specific set of devices. If this is done, binding resolution will take only the controls of the given devices into account.

```
    var actionMap = new InputActionMap();

    // Restrict the action map to just the first gamepad.
    actionMap.devices = new[] { Gamepad.all[0] };
```

>NOTE: `InputUser` and `PlayerInput` make use of this facility automatically. I.e. they will set `InputActionMap.devices` automatically based on the devices that are paired to the user/player.

## Runtime Rebinding

### Showing Current Bindings

Both in rebinding UIs as well for on-screen hints during gameplay, it can be useful to know what an action is currently bound to.

## Control Schemes
