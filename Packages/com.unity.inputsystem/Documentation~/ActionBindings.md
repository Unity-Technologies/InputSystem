---
uid: input-system-action-bindings
---
# Input bindings

An [`InputBinding`](xref:UnityEngine.InputSystem.InputBinding) represents a connection between an [action](xref:input-system-actions) and one or more [controls](xref:input-system-controls) identified by a [control path](xref:input-system-controls#control-paths). For example, the right trigger of a gamepad (a control) might be bound to an an action named "accelerate", so that pulling the right trigger causes a car to accelerate in your game.

You can add multiple bindings to an action, which is generally useful for supporting multiple types of input device. For example, in the default set of actions, the "Move" action has a binding to the left gamepad stick and the WASD keys, which means input through any of these bindings will perform the action.

You can also bind multiple controls from the same device to an action. For example, both the left and right trigger of a gamepad could be mapped to the same action, so that pulling either trigger has the same result in your game.

You can also set up [Composite](#composite-bindings) bindings, which don't bind to the controls themselves, but receive their input from **Part Bindings** and then return a value representing a composition of those inputs. For example, the right trigger on the gamepad can act as a strength multiplier on the value of the left stick.

## InputBinding API access

Each `InputBinding` has the following properties:

|Property|Description|
|--------|-----------|
|[`path`](xref:UnityEngine.InputSystem.InputBinding.path)|[Control path](xref:input-system-controls#control-paths) that identifies the control(s) from which the action should receive input.<br><br>Example: `"<Gamepad>/leftStick"`|
|[`overridePath`](xref:UnityEngine.InputSystem.InputBinding.overridePath)|[Control path](xref:input-system-controls#control-paths) that overrides `path`. Unlike `path`, `overridePath` is not persistent, so you can use it to non-destructively override the path on a binding. If it is set to something other than null, it takes effect and overrides `path`.  To get the path which is currently in effect (that is, either `path` or `overridePath`), you can query the [`effectivePath`](xref:UnityEngine.InputSystem.InputBinding.effectivePath) property.|
|[`action`](xref:UnityEngine.InputSystem.InputBinding.action)|The name or ID of the action that the binding should trigger. Note that this can be null or empty (for instance, for  [composites](#composite-bindings)). Not case-sensitive.<br><br>Example: `"fire"`|
|[`groups`](xref:UnityEngine.InputSystem.InputBinding.groups)|A semicolon-separated list of binding groups that the binding belongs to. Can be null or empty. Binding groups can be anything, but are mostly used for [control schemes](#control-schemes). Not case-sensitive.<br><br>Example: `"Keyboard&Mouse;Gamepad"`|
|[`interactions`](xref:UnityEngine.InputSystem.InputBinding.interactions)|A semicolon-separated list of [Interactions](xref:input-system-interactions) to apply to input on this binding. Note that Unity appends Interactions applied to the [action](xref:input-system-actions) itself (if any) to this list. Not case-sensitive.<br><br>Example: `"slowTap;hold(duration=0.75)"`|
|[`processors`](xref:UnityEngine.InputSystem.InputBinding.processors)|A semicolon-separated list of [Processors](UsingProcessors.md) to apply to input on this binding. Note that Unity appends Processors applied to the [action](xref:input-system-actions) itself (if any) to this list. Not case-sensitive.<br><br>Processors on bindings apply in addition to Processors on controls that are providing values. For example, if you put a `stickDeadzone` Processor on a binding and then bind it to `<Gamepad>/leftStick`, you get deadzones applied twice: once from the deadzone Processor sitting on the `leftStick` control, and once from the binding.<br><br>Example: `"invert;axisDeadzone(min=0.1,max=0.95)"`|
|[`id`](xref:UnityEngine.InputSystem.InputBinding.id)|Unique ID of the binding. You can use it to identify the binding when storing binding overrides in user settings, for example.|
|[`name`](xref:UnityEngine.InputSystem.InputBinding.name)|Optional name of the binding. Identifies part names inside [Composites](#composite-bindings).<br><br>Example: `"Positive"`|
|[`isComposite`](xref:UnityEngine.InputSystem.InputBinding.isComposite)|Whether the binding acts as a [Composite](#composite-bindings).|
|[`isPartOfComposite`](xref:UnityEngine.InputSystem.InputBinding.isPartOfComposite)|Whether the binding is part of a [Composite](#composite-bindings).|

To query the bindings for a specific action, use [`InputAction.bindings`](xref:UnityEngine.InputSystem.InputAction.bindings).

To query a flat list of bindings for all actions in an action map, use [`InputActionMap.bindings`](xref:UnityEngine.InputSystem.InputActionMap.bindings).

## Composite bindings

You might want to have several controls act in unison to mimic a different type of control. The most common example of this is using the W, A, S, and D keys on the keyboard to form a 2D vector control equivalent to mouse deltas or gamepad sticks. Another example is to use two keys to form a 1D axis equivalent to a mouse scroll axis.

This is difficult to implement with normal bindings. You can bind a  [`ButtonControl`](xref:UnityEngine.InputSystem.Controls.ButtonControl) to an action expecting a `Vector2`, but doing so results in an exception at runtime when the Input System tries to read a `Vector2` from a control that can deliver only a `float`.

Composite bindings (that is, bindings that are made up of other bindings) solve this problem. Composites themselves don't bind directly to controls; instead, they source values from other bindings that do, and then synthesize input on the fly from those values.

To see how to create Composites in the editor UI, refer to [Editing Composite Bindings](xref:input-system-configuring-input#edit-composite-bindings).

To create composites in code, use the [`AddCompositeBinding`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.AddCompositeBinding(UnityEngine.InputSystem.InputAction,System.String,System.String,System.String)) method:

```CSharp
myAction.AddCompositeBinding("Axis")
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

Each Composite consists of one binding that has [`InputBinding.isComposite`](xref:UnityEngine.InputSystem.InputBinding.isComposite) set to true, followed by one or more bindings that have [`InputBinding.isPartOfComposite`](xref:UnityEngine.InputSystem.InputBinding.isPartOfComposite) set to true. In other words, several consecutive entries in [`InputActionMap.bindings`](xref:UnityEngine.InputSystem.InputActionMap.bindings) or [`InputAction.bindings`](xref:UnityEngine.InputSystem.InputAction.bindings) together form a Composite.

Note that each composite part can be bound arbitrary many times.

```CSharp
// Make both shoulders and triggers pull on the axis.
myAction.AddCompositeBinding("Axis")
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Positive", "<Gamepad>/rightShoulder")
    .With("Negative", "<Gamepad>/leftTrigger");
    .With("Negative", "<Gamepad>/leftShoulder");
```

Composites can have parameters, just like [Interactions](xref:input-system-interactions) and [Processors](UsingProcessors.md).

```CSharp
myAction.AddCompositeBinding("Axis(whichSideWins=1)");
```

There are currently five Composite types that come with the system out of the box:

- [1D-Axis](#1d-axis): two buttons that pull a 1D axis in the negative and positive direction.
- [2D-Vector](#2d-vector): represents a 4-way button setup where each button represents a cardinal direction, for example a WASD keyboard input (up-down-left-right controls).
- [3D-Vector](#3d-vector): represents a 6-way button where two combinations each control one axis of a 3D Vector.
- [One Modifier](#one-modifier): requires the user to hold down a "modifier" button in addition to another control, for example, "SHIFT+1".
- [Two Modifiers](#two-modifiers): requires the user to hold down two "modifier" buttons in addition to another control, for example, "SHIFT+CTRL+1".

You can also [add your own](#writing-custom-composites) types of Composites.

### 1D Axis

![The Add Positive/Negative binding property is selected for the "fire" action on the Actions panel.](Images/Add1DAxisComposite.png){width="486" height="133"}

![The 1D Axis Composite binding appears under the "fire" action on the Actions panel.](Images/1DAxisComposite.png){width="486" height="142"}


The 1D Axis Composite is made of two buttons: one that pulls a 1D axis in its negative direction, and another that pulls it in its positive direction, using the [`AxisComposite`](xref:UnityEngine.InputSystem.Composites.AxisComposite) class to compute a `float`.

```CSharp
myAction.AddCompositeBinding("1DAxis") // Or just "Axis"
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

The axis Composite has two Part Bindings:

|Part Binding|Type|Description|
|----|----|-----------|
|[`positive`](xref:UnityEngine.InputSystem.Composites.AxisComposite.positive)|`Button`|Controls pulling in the positive direction (towards [`maxValue`](xref:UnityEngine.InputSystem.Composites.AxisComposite.maxValue)).|
|[`negative`](xref:UnityEngine.InputSystem.Composites.AxisComposite.negative)|`Button`|Controls pulling in the negative direction, (towards [`minValue`](xref:UnityEngine.InputSystem.Composites.AxisComposite.minValue)).|

You can set the following parameters on an axis Composite:

|Parameter|Description|
|---------|-----------|
|[`whichSideWins`](xref:UnityEngine.InputSystem.Composites.AxisComposite.whichSideWins)|What happens if both [`positive`](xref:UnityEngine.InputSystem.Composites.AxisComposite.positive) and [`negative`](xref:UnityEngine.InputSystem.Composites.AxisComposite.negative) are actuated. See table below.|
|[`minValue`](xref:UnityEngine.InputSystem.Composites.AxisComposite.minValue)|The value returned if the [`negative`](xref:UnityEngine.InputSystem.Composites.AxisComposite.negative) side is actuated. Default is -1.|
|[`maxValue`](xref:UnityEngine.InputSystem.Composites.AxisComposite.maxValue)|The value returned if the [`positive`](xref:UnityEngine.InputSystem.Composites.AxisComposite.positive) side is actuated. Default is 1.|

If controls from both the `positive` and the `negative` side are actuated, then the resulting value of the axis Composite depends on the `whichSideWin` parameter setting.

| [`WhichSideWins`](xref:UnityEngine.InputSystem.Composites.AxisComposite.WhichSideWins) | Description                                                  |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| (0) `Neither`                                                | Neither side has precedence. The Composite returns the [`midpoint`](xref:UnityEngine.InputSystem.Composites.AxisComposite.midPoint) between `minValue` and `maxValue` as a result. At their default settings, this is 0.<br><br>This is the default value for this setting. |
| (1) `Positive`                                               | The positive side has precedence and the Composite returns `maxValue`. |
| (2) `Negative`                                               | The negative side has precedence and the Composite returns `minValue`. |

> [!NOTE]
> There is no support yet for interpolating between the positive and negative over time.

### 2D Vector

![The Add Up/Down/Left/Right Composite binding is selected for the "Move" action on the Actions panel.](Images/Add2DVectorComposite.png){width="486" height="199"}

![The WASD part bindings appear under the "Move" action on the Actions panel.](Images/2DVectorComposite.png){width="486" height="178"}

A 2D Vector Composite represents a 4-way button setup like the D-pad on gamepads, where each button represents a cardinal direction. This type of Composite binding uses the [`Vector2Composite`](xref:UnityEngine.InputSystem.Composites.Vector2Composite) class to compute a `Vector2`.

This is very useful for representing up-down-left-right controls, such as WASD keyboard input.

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

The 2D Vector Composite has four Part Bindings.

|Part Binding|Type|Description|
|----|----|-----------|
|[`up`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.up)|`Button`|Controls representing `(0,1)` (+Y).|
|[`down`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.down)|`Button`|Controls representing `(0,-1)` (-Y).|
|[`left`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.left)|`Button`|Controls representing `(-1,0)` (-X).|
|[`right`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.right)|`Button`|Controls representing `(1,0)` (+X).|

In addition, you can set this parameter on a 2D Vector Composite:

|Parameter|Description|
|---------|-----------|
|[`mode`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.mode)|Whether to treat the inputs as digital or as analog controls.<br><br>If this is set to [`Mode.DigitalNormalized`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.Mode.DigitalNormalized), inputs are treated as buttons (off if below [`defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings.defaultButtonPressPoint) and on if equal to or greater). Each input is 0 or 1 depending on whether the button is pressed or not. The vector resulting from the up/down/left/right parts is normalized. The result is a diamond-shaped 2D input range.<br><br>If this is set to [`Mode.Digital`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.Mode.Digital), the behavior is essentially the same as [`Mode.DigitalNormalized`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.Mode.DigitalNormalized) except that the resulting vector is not normalized.<br><br>Finally, if this is set to [`Mode.Analog`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.Mode.Analog), inputs are treated as analog (i.e. full floating-point values) and, other than [`down`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.down) and [`left`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.left) being inverted, values will be passed through as is.<br><br>The default is [`Mode.DigitalNormalized`](xref:UnityEngine.InputSystem.Composites.Vector2Composite.Mode.DigitalNormalized).|

> [!NOTE]
> There is no support yet for interpolating between the up/down/left/right over time.

### 3D Vector


![The Add Up/Down/Left/Right/Forward/Backward Composite binding is selected for the "position" action on the Actions panel.](Images/Add3DVectorComposite.png){width="486" height="150"}

![The 3D Vector part bindings appear under the "position" action on the Actions panel.](Images/3DVectorComposite.png){width="486" height="259"
}

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

The 3D Vector Composite has four Part Bindings.

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
|[`mode`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.mode)|Whether to treat the inputs as digital or as analog controls.<br><br>If this is set to [`Mode.DigitalNormalized`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.Mode.DigitalNormalized), inputs are treated as buttons (off if below [`defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings.defaultButtonPressPoint) and on if equal to or greater). Each input is 0 or 1 depending on whether the button is pressed or not. The vector resulting from the up/down/left/right/forward/backward parts is normalized.<br><br>If this is set to [`Mode.Digital`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.Mode.Digital), the behavior is essentially the same as [`Mode.DigitalNormalized`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.Mode.DigitalNormalized) except that the resulting vector is not normalized.<br><br>Finally, if this is set to [`Mode.Analog`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.Mode.Analog), inputs are treated as analog (that is, full floating-point values) and, other than [`down`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.down), [`left`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.left), and [`backward`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.backward) being inverted, values will be passed through as they are.<br><br>The default is [`Analog`](xref:UnityEngine.InputSystem.Composites.Vector3Composite.Mode.Analog).|

### One Modifier


![The Add Binding With One Modifier Composite binding is selected for the "fire" action on the Actions panel.](Images/AddBindingWithOneModifier.png){width="486" height="129"}

![The One Modifier part bindings appear under the "fire" action on the Actions panel.](Images/OneModifierComposite.png){width="486" height="147"}

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

The button with One Modifier Composite has two Part Bindings.

|Part|Type|Description|
|----|----|-----------|
|[`modifier`](xref:UnityEngine.InputSystem.Composites.OneModifierComposite.modifier)|`Button`|Modifier that has to be held for `binding` to come through. If the user holds any of the buttons bound to the `modifier` at the same time as the button that triggers the action, the Composite assumes the value of the `modifier` binding. If the user does not press any button bound to the `modifier`, the Composite remains at default value.|
|[`binding`](xref:UnityEngine.InputSystem.Composites.OneModifierComposite.binding)|Any|The control(s) whose value the Composite assumes while the user holds down the `modifier` button.|

This Composite has no parameters.

### Two Modifiers


![The bindings With Two Modifiers Composite binding is selected for the "fire" action on the Actions panel.](Images/AddBindingWithTwoModifiers.png){width="486" height="119"}

![The Two Modifiers part bindings appear under the "fire" action on the Actions panel.](Images/TwoModifiersComposite.png){width="486" height="149"}

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

The button with Two Modifiers Composite has three Part Bindings.

|Part|Type|Description|
|----|----|-----------|
|[`modifier1`](xref:UnityEngine.InputSystem.Composites.TwoModifiersComposite.modifier1)|`Button`|The first modifier the user must hold alongside `modifier2`, for `binding` to come through. If the user does not press any button bound to the `modifier1`, the Composite remains at default value.|
|[`modifier2`](xref:UnityEngine.InputSystem.Composites.TwoModifiersComposite.modifier2)|`Button`|The second modifier the user must hold alongside `modifier1`, for `binding` to come through. If the user does not press any button bound to the `modifier2`, the Composite remains at default value.|
|[`binding`](xref:UnityEngine.InputSystem.Composites.TwoModifiersComposite.binding)|Any|The control(s) whose value the Composite assumes while the user presses both `modifier1` and `modifier2` at the same time.|

This Composite has no parameters.

### Writing custom Composites

You can define new types of Composites, and register them with the API. Unity treats these the same as predefined types, which the Input System internally defines and registers in the same way.

To define a new type of Composite, create a class based on [`InputBindingComposite<TValue>`](xref:UnityEngine.InputSystem.InputBindingComposite`1).

> [!IMPORTANT]
> Composites must be __stateless__. This means that you cannot store local state that changes depending on the input being processed. For __stateful__ processing on bindings, see [interactions](xref:input-system-interactions#writing-custom-interactions).

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

## Working with bindings

### Look up bindings

You can retrieve the bindings of an action using its [`InputAction.bindings`](xref:UnityEngine.InputSystem.InputAction.bindings) property which returns a read-only array of [`InputBinding`](xref:UnityEngine.InputSystem.InputBinding) structs.

```CSharp
    // Get bindings of "fire" action.
    var fireBindings = playerInput.actions["fire"].bindings;
```

Also, all the bindings for all actions in an [`InputActionMap`](xref:UnityEngine.InputSystem.InputActionMap) are made available through the [`InputActionMap.bindings`](xref:UnityEngine.InputSystem.InputActionMap.bindings) property. The bindings are associated with actions through an [action ID](xref:UnityEngine.InputSystem.InputAction.id) or [action name](xref:UnityEngine.InputSystem.InputAction.name) stored in the [`InputBinding.action`](xref:UnityEngine.InputSystem.InputBinding.action) property.

```CSharp
    // Get all bindings in "gameplay" action map.
    var gameplayBindings = playerInput.actions.FindActionMap("gameplay").bindings;
```

You can also look up specific the indices of specific bindings in [`InputAction.bindings`](xref:UnityEngine.InputSystem.InputAction.bindings) using the [`InputActionRebindingExtensions.GetBindingIndex`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.GetBindingIndex(UnityEngine.InputSystem.InputAction,UnityEngine.InputSystem.InputBinding)) method.

```CSharp
    // Find the binding in the "Keyboard" control scheme.
    playerInput.actions["fire"].GetBindingIndex(group: "Keyboard");

    // Find the first binding to the space key in the "gameplay" action map.
    playerInput.FindActionMap("gameplay").GetBindingIndex(
        new InputBinding { path = "<Keyboard>/space" });
```

Finally, you can look up the binding that corresponds to a specific control through [`GetBindingIndexForControl`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.GetBindingIndexForControl*). This way, you can, for example, map a control found in the [`controls`](xref:UnityEngine.InputSystem.InputAction.controls) array of an [`InputAction`](xref:UnityEngine.InputSystem.InputAction) back to an [`InputBinding`](xref:UnityEngine.InputSystem.InputBinding).

```CSharp
    // Find the binding that binds LMB to "fire". If there is no such binding,
    // bindingIndex will be -1.
    var fireAction = playerInput.actions["fire"];
    var bindingIndex = fireAction.GetBindingIndexForControl(Mouse.current.leftButton);
    if (binding == -1)
        Debug.Log("Fire is not bound to LMB of the current mouse.");
```

### Change bindings

In general, you can change existing bindings via the [`InputActionSetupExtensions.ChangeBinding`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.ChangeBinding(UnityEngine.InputSystem.InputAction,System.Int32)) method. This returns an accessor that can be used to modify the properties of the targeted [`InputBinding`](xref:UnityEngine.InputSystem.InputBinding). Note that most of the write operations of the accessor are destructive. For non-destructive changes to bindings, refer to [Apply overrides](#apply-overrides).

```CSharp
// Get write access to the second binding of the 'fire' action.
var accessor = playerInput.actions['fire'].ChangeBinding(1);

// You can also gain access through the InputActionMap. Each
// map contains an array of all its bindings (see InputActionMap.bindings).
// Here we gain access to the third binding in the map.
accessor = playerInput.actions.FindActionMap("gameplay").ChangeBinding(2);
```

You can use the resulting accessor to modify properties through methods such as [`WithPath`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.WithPath*) or [`WithProcessors`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.WithProcessors*).

```CSharp
playerInput.actions["fire"].ChangeBinding(1)
    // Change path to space key.
    .WithPath("<Keyboard>/space");
```

You can also use the accessor to iterate through bindings using [`PreviousBinding`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.PreviousBinding*) and [`NextBinding`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.NextBinding*).

```CSharp
// Move accessor to previous binding.
accessor = accessor.PreviousBinding();

// Move accessor to next binding.
accessor = accessor.NextBinding();
```

If the given binding is a [composite](xref:UnityEngine.InputSystem.InputBinding.isComposite), you can address it by its name rather than by index.

```CSharp
// Change the 2DVector composite of the "move" action.
playerInput.actions["move"].ChangeCompositeBinding("2DVector")


//
playerInput.actions["move"].ChangeBinding("WASD")
```

#### Apply overrides

You can override aspects of any binding at run-time non-destructively. Specific properties of [`InputBinding`](xref:UnityEngine.InputSystem.InputBinding) have an `override` variant that, if set, will take precedent over the property that they shadow.  All `override` properties are of type `String`.

|Property|Override|Description|
|--------|--------|-----------|
|[`path`](xref:UnityEngine.InputSystem.InputBinding.path)|[`overridePath`](xref:UnityEngine.InputSystem.InputBinding.overridePath)|Replaces the [control path](xref:input-system-controls#control-paths) that determines which control(s) are referenced in the binding. If [`overridePath`](xref:UnityEngine.InputSystem.InputBinding.overridePath) is set to an empty string, the binding is effectively disabled.<br><br>Example: `"<Gamepad>/leftStick"`|
|[`processors`](xref:UnityEngine.InputSystem.InputBinding.processors)|[`overrideProcessors`](xref:UnityEngine.InputSystem.InputBinding.overrideProcessors)|Replaces the [processors](./UsingProcessors.md) applied to the binding.<br><br>Example: `"invert,normalize(min=0,max=10)"`|
|[`interactions`](xref:UnityEngine.InputSystem.InputBinding.interactions)|[`overrideInteractions`](xref:UnityEngine.InputSystem.InputBinding.overrideInteractions)|Replaces the [interactions](xref:input-system-interactions) applied to the binding.<br><br>Example: `"tap(duration=0.5)"`|

> [!NOTE]
> The `override` property values are not saved with the actions, for example, when calling [`InputActionAsset.ToJson()`](xref:UnityEngine.InputSystem.InputActionAsset.ToJson)). Refer to [Saving and loading rebinds](#save-and-load-rebinds) for details about how to persist user rebinds.

To set the various `override` properties, you can use the [`ApplyBindingOverride`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.ApplyBindingOverride(UnityEngine.InputSystem.InputAction,UnityEngine.InputSystem.InputBinding)) APIs.

```CSharp
// Rebind the "fire" action to the left trigger on the gamepad.
playerInput.actions["fire"].ApplyBindingOverride("<Gamepad>/leftTrigger");
```

In most cases, it is best to locate specific bindings using APIs such as [`GetBindingIndexForControl`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.GetBindingIndexForControl*) and to then apply the override to that specific binding.

```CSharp
// Find the "Jump" binding for the space key.
var jumpAction = playerInput.actions["Jump"];
var bindingIndex = jumpAction.GetBindingIndexForControl(Keyboard.current.spaceKey);

// And change it to the enter key.
jumpAction.ApplyBindingOverride(bindingIndex, "<Keyboard>/enter");
```

#### Erase bindings

You can erase a binding by calling [`Erase`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax.Erase*) on the [binding accessor](xref:UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax).

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

#### Add bindings

New bindings can be added to an action using [`AddBinding`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.AddBinding(UnityEngine.InputSystem.InputAction,System.String,System.String,System.String,System.String)) or [`AddCompositeBinding`](xref:UnityEngine.InputSystem.InputActionSetupExtensions.AddCompositeBinding(UnityEngine.InputSystem.InputAction,System.String,System.String,System.String)).

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

#### Set parameters

A binding may, either through itself or through its associated action, lead to [processor](UsingProcessors.md), [interaction](xref:input-system-interactions), and/or [composite](#composite-bindings) objects being created. These objects can have parameters you can configure through in the [Binding properties view](xref:input-system-configuring-input#bindings) of the [Input Actions Editor](xref:input-system-configuring-input) or through the API. This configuration will give parameters their default value.

```CSharp
// Create an action with a "Hold" interaction on it.
// Set the "duration" parameter to 4 seconds.
var action = new InputAction(interactions: "hold(duration=4)");
```

You can query the current value of any such parameter using the [`GetParameterValue`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.GetParameterValue(UnityEngine.InputSystem.InputAction,System.String,UnityEngine.InputSystem.InputBinding)) API.

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

To add an override, use the [`ApplyParameterOverride`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.ApplyParameterOverride(UnityEngine.InputSystem.InputAction,System.String,UnityEngine.InputSystem.Utilities.PrimitiveValue,UnityEngine.InputSystem.InputBinding)) API or any of its overloads.

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

> [!NOTE]
> Parameter overrides are *not* persisted along with an asset.

### Interactive rebinding

> [!NOTE]
> To download a sample project which demonstrates how to set up a rebinding user interface with Input System APIs, open the Package Manager, select the Input System Package, and choose the sample project "Rebinding UI" to download.

Runtime rebinding allows users of your application to set their own bindings.

To allow users to choose their own bindings interactively, use the  [`InputActionRebindingExtensions.RebindingOperation`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation) class. Call the [`PerformInteractiveRebinding()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.PerformInteractiveRebinding(UnityEngine.InputSystem.InputAction,System.Int32)) method on an action to create a rebinding operation. This operation waits for the Input System to register any input from any device which matches the action's expected control type, then uses [`InputBinding.overridePath`](xref:UnityEngine.InputSystem.InputBinding.overridePath) to assign the control path for that control to the action's bindings. If the user actuates multiple controls, the rebinding operation chooses the control with the highest [magnitude](xref:input-system-controls#control-actuation).

> [!IMPORTANT]
> You must dispose of [`InputActionRebindingExtensions.RebindingOperation`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation) instances via `Dispose()`, so that they don't leak memory on the unmanaged memory heap.

```C#
    void RemapButtonClicked(InputAction actionToRebind)
    {
        var rebindOperation = actionToRebind
            .PerformInteractiveRebinding().Start();
    }
```

The [`InputActionRebindingExtensions.RebindingOperation`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation) API is highly configurable to match your needs. For example, you can:

* Choose expected control types ([`WithExpectedControlType()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.WithExpectedControlType(System.Type))).

* Exclude certain controls ([`WithControlsExcluding()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.WithControlsExcluding(System.String))).

* Set a control to cancel the operation ([`WithCancelingThrough()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.WithCancelingThrough(UnityEngine.InputSystem.InputControl))).

* Choose which bindings to apply the operation on if the action has multiple bindings ([`WithTargetBinding()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.WithTargetBinding(System.Int32)), [`WithBindingGroup()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.WithBindingGroup(System.String)), [`WithBindingMask()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation.WithBindingMask(System.Nullable{UnityEngine.InputSystem.InputBinding}))).

Refer to the scripting API reference for [`InputActionRebindingExtensions.RebindingOperation`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation) for a full overview.

Note that [`PerformInteractiveRebinding()`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.PerformInteractiveRebinding(UnityEngine.InputSystem.InputAction,System.Int32)) automatically applies a set of default configurations based on the given action and targeted binding.

### Save and load rebinds

You can serialize override properties of [bindings](xref:UnityEngine.InputSystem.InputBinding) by serializing them as JSON strings and restoring them from these. Use [`SaveBindingOverridesAsJson`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.SaveBindingOverridesAsJson(UnityEngine.InputSystem.IInputActionCollection2)) to create these strings and [`LoadBindingOverridesFromJson`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.LoadBindingOverridesFromJson(UnityEngine.InputSystem.IInputActionCollection2,System.String,System.Boolean)) to restore overrides from them.

```CSharp
// Store player rebinds in PlayerPrefs.
var rebinds = playerInput.actions.SaveBindingOverridesAsJson();
PlayerPrefs.SetString("rebinds", rebinds);

// Restore player rebinds from PlayerPrefs (removes all existing
// overrides on the actions; pass `false` for second argument
// in case you want to prevent that).
var rebinds = PlayerPrefs.GetString("rebinds");
playerInput.actions.LoadBindingOverridesFromJson(rebinds);
```

#### Restore original bindings

You can remove binding overrides and thus restore defaults by using [`RemoveBindingOverride`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RemoveBindingOverride(UnityEngine.InputSystem.InputAction,System.Int32)) or [`RemoveAllBindingOverrides`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.RemoveAllBindingOverrides(UnityEngine.InputSystem.IInputActionCollection2)).

```CSharp
// Remove binding overrides from the first binding of the "fire" action.
playerInput.actions["fire"].RemoveBindingOverride(0);

// Remove all binding overrides from the "fire" action.
playerInput.actions["fire"].RemoveAllBindingOverrides();

// Remove all binding overrides from a player's actions.
playerInput.actions.RemoveAllBindingOverrides();
```

#### Display bindings

It can be useful for the user to know what an action is currently bound to (taking any potentially active rebindings into account) while rebinding UIs, and for on-screen hints while the app is running. You can use [`InputBinding.effectivePath`](xref:UnityEngine.InputSystem.InputBinding.effectivePath) to get the currently active path for a binding (which returns [`overridePath`](xref:UnityEngine.InputSystem.InputBinding.overridePath) if set, or otherwise returns [`path`](xref:UnityEngine.InputSystem.InputBinding.path)).

The easiest way to retrieve a display string for an action is to call [`InputActionRebindingExtensions.GetBindingDisplayString`](xref:UnityEngine.InputSystem.InputActionRebindingExtensions.GetBindingDisplayString*) which is an extension method for [`InputAction`](xref:UnityEngine.InputSystem.InputAction).

```CSharp
    // Get a binding string for the action as a whole. This takes into account which
    // bindings are currently active and the actual controls bound to the action.
    m_RebindButton.GetComponentInChildren<Text>().text = action.GetBindingDisplayString();

    // Get a binding string for a specific binding on an action by index.
    m_RebindButton.GetComponentInChildren<Text>().text = action.GetBindingDisplayString(1);

    // Look up binding indices with GetBindingIndex.
    var bindingIndex = action.GetBindingIndex(InputBinding.MaskByGroup("Gamepad"));
    m_RebindButton.GetComponentInChildren<Text>().text =
        action.GetBindingDisplayString(bindingIndex);
```

You can also use this method to replace the text string with images.

```CSharp
    // Call GetBindingDisplayString() such that it also returns information about the
    // name of the device layout and path of the control on the device. This information
    // is useful for reliably associating imagery with individual controls.
    // NOTE: The first argument is the index of the binding within InputAction.bindings.
    var bindingString = action.GetBindingDisplayString(0, out deviceLayout, out controlPath);

    // If it's a gamepad, look up an icon for the control.
    Sprite icon = null;
    if (!string.IsNullOrEmpty(deviceLayout)
        && !string.IsNullOrEmpty(controlPath)
        && InputSystem.IsFirstLayoutBasedOnSecond(deviceLayout, "Gamepad"))
    {
        switch (controlPath)
        {
            case "buttonSouth": icon = aButtonIcon; break;
            case "dpad/up": icon = dpadUpIcon; break;
            //...
        }
    }

    // If you have an icon, display it instead of the text.
    var text = m_RebindButton.GetComponentInChildren<Text>();
    var image = m_RebindButton.GetComponentInChildren<Image>();
    if (icon != null)
    {
        // Display icon.
        text.gameObject.SetActive(false);
        image.gameObject.SetActive(true);
        image.sprite = icon;
    }
    else
    {
        // Display text.
        text.gameObject.SetActive(true);
        image.gameObject.SetActive(false);
        text.text = bindingString;
    }
```

Additionally, each binding has a [`ToDisplayString`](xref:UnityEngine.InputSystem.InputBinding.ToDisplayString(UnityEngine.InputSystem.InputBinding.DisplayStringOptions,UnityEngine.InputSystem.InputControl)) method, which you can use to turn individual bindings into display strings. There is also a generic formatting method for control paths, [`InputControlPath.ToHumanReadableString`](xref:UnityEngine.InputSystem.InputControlPath.ToHumanReadableString(System.String,UnityEngine.InputSystem.InputControlPath.HumanReadableStringOptions,UnityEngine.InputSystem.InputControl)), which you can use with arbitrary control path strings.

Note that the controls a binding resolves to can change at any time, and the display strings for controls might change dynamically. For example, if the user switches the currently active keyboard layout, the display string for each individual key on the [`Keyboard`](xref:UnityEngine.InputSystem.Keyboard) might change.

## Control schemes

A binding can belong to any number of binding groups. Unity stores these on the [`InputBinding`](xref:UnityEngine.InputSystem.InputBinding) class as a semicolon-separated string in the  [`InputBinding.groups`](xref:UnityEngine.InputSystem.InputBinding.groups) property, and you can use them for any arbitrary grouping of bindings. To enable different sets of binding groups for an [`InputActionMap`](xref:UnityEngine.InputSystem.InputActionMap) or [`InputActionAsset`](xref:UnityEngine.InputSystem.InputActionAsset), you can use the [`InputActionMap.bindingMask`](xref:UnityEngine.InputSystem.InputActionMap.bindingMask)/[`InputActionAsset.bindingMask`](xref:UnityEngine.InputSystem.InputActionAsset.bindingMask) property. The Input System uses this to implement the concept of grouping bindings into different  [`InputControlSchemes`](xref:UnityEngine.InputSystem.InputControlScheme).

Control Schemes use binding groups to map bindings in an [`InputActionMap`](xref:UnityEngine.InputSystem.InputActionMap) or [`InputActionAsset`](xref:UnityEngine.InputSystem.InputActionAsset) to different types of devices. The [`PlayerInput`](xref:input-system-player-input) class uses these to enable a matching control scheme for a new [user](xref:input-system-user-management) joining the game, based on the device they are playing on.

## Details

### Binding resolution

When the Input System accesses the [controls](xref:input-system-controls) bound to an action for the first time, the action resolves its bindings to match them to existing controls on existing devices. In this process, the action calls [`InputSystem.FindControls<>()`](xref:UnityEngine.InputSystem.InputSystem.FindControls``1(System.String,UnityEngine.InputSystem.InputControlList{``0}@)) (filtering for devices assigned to the InputActionMap, if there are any) for the binding path of each of the action's bindings. This creates a list of resolved controls that are now bound to the action.

Note that a single [binding path](xref:input-system-controls#control-paths) can match multiple controls:

* A specific device path such as `<DualShockGamepad>/buttonEast` matches the "Circle" button on a [PlayStation controller](xref:input-system-gamepad#playstation-controllers). If you have multiple PlayStation controllers connected, it resolves to the "Circle" button on each of these controllers.

* An abstract device path such as `<Gamepad>/buttonEast` matches the right action button on any connected gamepad. If you have a PlayStation controller and an [Xbox controller](xref:input-system-gamepad#xbox-controllers) connected, it resolves to the "Circle" button on the PlayStation controller, and to the "B" button on the Xbox controller.

* A binding path can also contain wildcards, such as `<Gamepad>/button*`. This matches any control on any gamepad with a name starting with "button", which matches all the four action buttons on any connected gamepad. A different example: `*/{Submit}` matches any control tagged with the "Submit" [usage](xref:input-system-controls#control-usages) on any device.

If there are multiple bindings on the same action that all reference the same control(s), the control will effectively feed into the action multiple times. This is to allow, for example, a single control to produce different input on the same action by virtue of being bound in a different fashion (composites, processors, interactions, etc). However, regardless of how many times a control is bound on any given action, it will only be mentioned once in the action's [array of `controls`](xref:UnityEngine.InputSystem.InputAction.controls).

To query the controls that an action resolves to, you can use [`InputAction.controls`](xref:UnityEngine.InputSystem.InputAction.controls). You can also run this query if the action is disabled.

To be notified when binding resolution happens, you can listen to [`InputSystem.onActionChange`](xref:UnityEngine.InputSystem.InputSystem.onActionChange) which triggers [`InputActionChange.BoundControlsAboutToChange`](xref:UnityEngine.InputSystem.InputActionChange.BoundControlsAboutToChange) before modifying control lists and triggers [`InputActionChange.BoundControlsChanged`](xref:UnityEngine.InputSystem.InputActionChange.BoundControlsChanged) after having updated them.

#### Binding resolution while actions are enabled

In certain situations, the [controls](xref:UnityEngine.InputSystem.InputAction.controls) bound to an action have to be updated more than once. For example, if a new [device](xref:input-system-devices) becomes usable with an action, the action may now pick up input from additional controls. Also, if bindings are added, removed, or modified, control lists will need to be updated.

This updating of controls usually happens transparently in the background. However, when an action is [enabled](xref:UnityEngine.InputSystem.InputAction.enabled) and especially when it is [in progress](xref:UnityEngine.InputSystem.InputAction.IsInProgress*), there may be a noticeable effect on the action.

Adding or removing a device &ndash; either [globally](xref:UnityEngine.InputSystem.InputSystem.devices) or to/from the [device list](xref:UnityEngine.InputSystem.InputActionAsset.devices) of an action &ndash; will remain transparent __except__ if an action is in progress and it is the device of its [active control](xref:UnityEngine.InputSystem.InputAction.activeControl) that is being removed. In this case, the action will automatically be [cancelled](xref:UnityEngine.InputSystem.InputAction.canceled).

Modifying the [binding mask](xref:UnityEngine.InputSystem.InputActionAsset.bindingMask) or modifying any of the bindings (such as through [rebinding](#interactive-rebinding) or by adding or removing bindings) will, however, lead to all enabled actions being temporarily disabled and then re-enabled and resumed.

#### Choose which devices to use

> [!NOTE]
> [`InputUser`](xref:input-system-user-management) and [`PlayerInput`](xref:input-system-player-input) make use of this facility automatically. They set [`InputActionMap.devices`](xref:UnityEngine.InputSystem.InputActionMap.devices) automatically based on the devices that are paired to the user.

By default, actions resolve their bindings against all devices present in the Input System (that is, [`InputSystem.devices`](xref:UnityEngine.InputSystem.InputSystem.devices)). For example, if there are two gamepads present in the system, a binding to `<Gamepad>/buttonSouth` picks up both gamepads and allows the action to be used from either.

You can override this behavior by restricting [`InputActionAssets`](xref:UnityEngine.InputSystem.InputActionAsset) or individual [`InputActionMaps`](xref:UnityEngine.InputSystem.InputActionMap) to a specific set of devices. If you do this, binding resolution only takes the controls of the given devices into account.

```
    var actionMap = new InputActionMap();

    // Restrict the action map to just the first gamepad.
    actionMap.devices = new[] { Gamepad.all[0] };
```

### Conflicting inputs

There are two situations where a given input may lead to ambiguity:

1. Several controls are bound to the same action and more than one is feeding input into the action at the same time. Example: an action that is bound to both the left and right trigger on a Gamepad and both triggers are pressed.
2. The input is part of a sequence of inputs and there are several possible such sequences. Example: one action is bound to the `B` key and another action is bound to `Shift-B`.

#### Multiple, concurrently used controls

> [!NOTE]
> This section does not apply to [`PassThrough`](xref:input-system-responding#pass-through) actions as they are by design meant to allow multiple concurrent inputs.

For a [`Button`](xref:input-system-responding#button) or [`Value`](xref:input-system-responding#value) action, there can only be one control at any time that is "driving" the action. This control is considered the [`activeControl`](xref:UnityEngine.InputSystem.InputAction.activeControl).

When an action is bound to multiple controls, the [`activeControl`](xref:UnityEngine.InputSystem.InputAction.activeControl) at any point is the one with the greatest level of ["actuation"](xref:input-system-controls#control-actuation), that is, the largest value returned from [`EvaluateMagnitude`](xref:UnityEngine.InputSystem.InputControl.EvaluateMagnitude*). If a control exceeds the actuation level of the current [`activeControl`](xref:UnityEngine.InputSystem.InputAction.activeControl), it will itself become the active control.

The following example demonstrates this mechanism with a [`Button`](xref:input-system-responding#button) action and also demonstrates the difference to a [`PassThrough`](xref:input-system-responding#pass-through) action.

```CSharp
// Create a button and a pass-through action and bind each of them
// to both triggers on the gamepad.
var buttonAction = new InputAction(type: InputActionType.Button,
    binding: "<Gamepad>/*Trigger");
var passThroughAction = new InputAction(type: InputActionType.PassThrough,
    binding: "<Gamepad>/*Trigger");

buttonAction.performed += c => Debug.Log("${c.control.name} pressed (Button)");
passThroughAction.performed += c => Debug.Log("${c.control.name} changed (Pass-Through)");

buttonAction.Enable();
passThroughAction.Enable();

// Press the left trigger all the way down.
// This will trigger both buttonAction and passThroughAction. Both will
// see leftTrigger becoming the activeControl.
Set(gamepad.leftTrigger, 1f);

// Will log
//   "leftTrigger pressed (Button)" and
//   "leftTrigger changed (Pass-Through)"

// Press the right trigger halfway down.
// This will *not* trigger or otherwise change buttonAction as the right trigger
// is actuated *less* than the left one that is already driving action.
// However, passThrough action is not performing such tracking and will thus respond
// directly to the value change. It will perform and make rightTrigger its activeControl.
Set(gamepad.rightTrigger, 0.5f);

// Will log
//   "rightTrigger changed (Pass-Through)"

// Release the left trigger.
// For buttonAction, this will mean that now all controls feeding into the action have
// been released and thus the button releases. activeControl will go back to null.
// For passThrough action, this is just another value change. So, the action performs
// and its active control changes to leftTrigger.
Set(gamepad.leftTrigger,  0f);

// Will log
//   "leftTrigger changed (Pass-Through)"
```

For [composite bindings](#composite-bindings), magnitudes of the composite as a whole rather than for individual controls are tracked. However, [`activeControl`](xref:UnityEngine.InputSystem.InputAction.activeControl) will stick track individual controls from the composite.

##### Disable conflict resolution

Conflict resolution is always applied to [Button](xref:input-system-responding#button) and [Value](xref:input-system-responding#value) type actions. However, it can be undesirable in situations when an action is simply used to gather any and all inputs from bound controls. For example, the following action would monitor the A button of all available gamepads:

```CSharp
var action = new InputAction(type: InputActionType.PassThrough, binding: "<Gamepad>/buttonSouth");
action.Enable();
```

By using the [Pass-Through](xref:input-system-responding#pass-through) action type, conflict resolution is bypassed and thus, pressing the A button on one gamepad will not result in a press on a different gamepad being ignored.

#### Multiple input sequences (such as keyboard shortcuts)

> [!NOTE]
> The mechanism described here only applies to actions that are part of the same [`InputActionMap`](xref:UnityEngine.InputSystem.InputActionMap) or [`InputActionAsset`](xref:UnityEngine.InputSystem.InputActionAsset).

Inputs that are used in combinations with other inputs may also lead to ambiguities. If, for example, the `b` key on the Keyboard is bound both on its own as well as in combination with the `shift` key, then if you first press `shift` and then `b`, the latter key press would be a valid input for either of the actions.

The way this is handled is that bindings will be processed in the order of decreasing "complexity". This metric is derived automatically from the binding:

* A binding that is *not* part of a [composite](#composite-bindings) is assigned a complexity of 1.
* A binding that *is* part of a [composite](#composite-bindings) is assigned a complexity equal to the number of part bindings in the composite.

In our example, this means that a [`OneModifier`](#one-modifier) composite binding to `Shift+B` has a higher "complexity" than a binding to `B` and thus is processed first.

Additionally, the first binding that results in the action changing [phase](xref:input-system-responding#action-callbacks) will "consume" the input. This consuming will result in other bindings to the same input not being processed. So in our example, when `Shift+B` "consumes" the `B` input, the binding to `B` will be skipped.

The following example illustrates how this works at the API level.

```CSharp
// Create two actions in the same map.
var map = new InputActionMap();
var bAction = map.AddAction("B");
var shiftbAction = map.AddAction("ShiftB");

// Bind one of the actions to 'B' and the other to 'SHIFT+B'.
bAction.AddBinding("<Keyboard>/b");
shiftbAction.AddCompositeBinding("OneModifier")
    .With("Modifier", "<Keyboard>/shift")
    .With("Binding", "<Keyboard>/b");

// Print something to the console when the actions are triggered.
bAction.performed += _ => Debug.Log("B action performed");
shiftbAction.performed += _ => Debug.Log("SHIFT+B action performed");

// Start listening to input.
map.Enable();

// Now, let's assume the left shift key on the keyboard is pressed (here, we manually
// press it with the InputTestFixture API).
Press(Keyboard.current.leftShiftKey);

// And then the B is pressed. This is a valid input for both
// bAction as well as shiftbAction.
//
// What will happen now is that shiftbAction will do its processing first. In response,
// it will *perform* the action (i.e. we see the `performed` callback being invoked) and
// thus "consume" the input. bAction will stay silent as it will in turn be skipped over.
Press(keyboard.bKey);
```

### Initial state check

After an action is [enabled](xref:UnityEngine.InputSystem.InputAction.enabled), it will start reacting to input as it comes in. However, at the time the action is enabled, one or more of the controls that are [bound](xref:UnityEngine.InputSystem.InputAction.controls) to an action may already have a non-default state at that point.

Using what is referred to as an "initial state check", an action can be made to respond to such a non-default state as if the state change happened *after* the action was enabled. The way this works is that in the first input [update](xref:UnityEngine.InputSystem.InputSystem.Update*) after the action was enabled, all its bound controls are checked in turn. If any of them has a non-default state, the action responds right away.

This check is implicitly enabled for [Value](xref:input-system-responding#value) actions. If, for example, you have a `Move` action bound to the left stick on the gamepad and the stick is already pushed in a direction when `Move` is enabled, the character will immediately start walking.

By default, [Button](xref:input-system-responding#button) and [Pass-Through](xref:input-system-responding#pass-through) type actions, do not perform this check. A button that is pressed when its respective action is enabled first needs to be released and then pressed again for it to trigger the action.

However, you can manually enable initial state checks on these types of actions using the checkbox in the Editor:

![The Initial State Check setting appears with a checkmark under the Pass Through action on the Actions panel.](./Images/InitialStateCheck.png){width="486" height="116"}
