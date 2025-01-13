
### Dealing with binding conflicts


There are two situations where a given input may lead to ambiguity:

1. Several Controls are bound to the same Action and more than one is feeding input into the Action at the same time. Example: an Action that is bound to both the left and right trigger on a Gamepad and both triggers are pressed.
2. The input is part of a sequence of inputs and there are several possible such sequences. Example: one Action is bound to the `B` key and another Action is bound to `Shift-B`.

#### Multiple, concurrently used Controls

>__Note:__ This section does not apply to [`PassThrough`](RespondingToActions.md#pass-through) Actions as they are by design meant to allow multiple concurrent inputs.

For a [`Button`](RespondingToActions.md#button) or [`Value`](RespondingToActions.md#value) Action, there can only be one Control at any time that is "driving" the Action. This Control is considered the [`activeControl`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_activeControl).

When an Action is bound to multiple Controls, the [`activeControl`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_activeControl) at any point is the one with the greatest level of ["actuation"](Controls.md#control-actuation), that is, the largest value returned from [`EvaluateMagnitude`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_EvaluateMagnitude_). If a Control exceeds the actuation level of the current [`activeControl`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_activeControl), it will itself become the active Control.

The following example demonstrates this mechanism with a [`Button`](RespondingToActions.md#button) Action and also demonstrates the difference to a [`PassThrough`](RespondingToActions.md#pass-through) Action.

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

For [composite bindings](#composite-bindings), magnitudes of the composite as a whole rather than for individual Controls are tracked. However, [`activeControl`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_activeControl) will stick track individual Controls from the composite.

##### Disabling Conflict Resolution

Conflict resolution is always applied to [Button](RespondingToActions.md#button) and [Value](RespondingToActions.md#value) type Actions. However, it can be undesirable in situations when an Action is simply used to gather any and all inputs from bound Controls. For example, the following Action would monitor the A button of all available gamepads:

```CSharp
var action = new InputAction(type: InputActionType.PassThrough, binding: "<Gamepad>/buttonSouth");
action.Enable();
```

By using the [Pass-Through](RespondingToActions.md#pass-through) Action type, conflict resolution is bypassed and thus, pressing the A button on one gamepad will not result in a press on a different gamepad being ignored.

#### Multiple input sequences (such as keyboard shortcuts)

>__Note__: The mechanism described here only applies to Actions that are part of the same [`InputActionMap`](../api/UnityEngine.InputSystem.InputActionMap.html) or [`InputActionAsset`](../api/UnityEngine.InputSystem.InputActionAsset.html).

Inputs that are used in combinations with other inputs may also lead to ambiguities. If, for example, the `b` key on the Keyboard is bound both on its own as well as in combination with the `shift` key, then if you first press `shift` and then `b`, the latter key press would be a valid input for either of the Actions.

The way this is handled is that Bindings will be processed in the order of decreasing "complexity". This metric is derived automatically from the Binding:

* A binding that is *not* part of a [composite](#composite-bindings) is assigned a complexity of 1.
* A binding that *is* part of a [composite](#composite-bindings) is assigned a complexity equal to the number of part bindings in the composite.

In our example, this means that a [`OneModifier`](#one-modifier) composite Binding to `Shift+B` has a higher "complexity" than a Binding to `B` and thus is processed first.

Additionally, the first Binding that results in the Action changing [phase](RespondingToActions.md#action-callbacks) will "consume" the input. This consuming will result in other Bindings to the same input not being processed. So in our example, when `Shift+B` "consumes" the `B` input, the Binding to `B` will be skipped.

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