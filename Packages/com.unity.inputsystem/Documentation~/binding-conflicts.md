
# Binding conflicts

A binding conflict is when an [action](./actions.md) with more than one [control](./controls.md) [bound](./bindings.md) to it recieves values from multiple controls. 

For example, if the left and right triggers of a gamepad are both bound to an "accelerate" action in your game, each trigger could be pressed in to different amounts. This conflict of recieving two different values for the same action is resolved by the Input System.

## Conflict Resolution

For [value type actions](./action-and-control-types.md), the Input System continuously monitors all the controls bound to the action, and then chooses the one which is the **most actuated** to be the control driving the action. Most actuated means the largest absolute value is being reported, whether positive or negative in the case of a 1D axis, and regardless of direction in the case of 2D and 3D axes. The value of the most actuated control is reported in callbacks, and triggered whenever the value changes.

If a different bound control is actuated more, that control becomes the control driving the action. This process is called **conflict resolution**. This is useful if you want to allow different Controls to control an Action in the game, but only take input from one Control at the same time.

## Ambiguous situations

There are two situations where a given input might lead to ambiguity:

1. Several controls are bound to the same action and more than one is feeding input into the Action at the same time. Example: an Action that is bound to both the left and right trigger on a Gamepad and both triggers are pressed.
2. The input is part of a sequence of inputs and there are several possible such sequences. Example: one Action is bound to the `B` key and another Action is bound to `Shift-B`.

## Multiple concurrently used controls

>__Note:__ This section does not apply to [pass-through](./configure-action-type.md) actions, which do not perform conflict resolution, and are intended allow multiple concurrent inputs.

For a **Button** or **Value** [action type](./configure-action-type.md), there can only be one control at any time that is "driving" the action. This control is considered the [`activeControl`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_activeControl).

When an action is bound to multiple controls, the active control at any point is the one with the greatest level of ["actuation"](./control-actuation.md) (the one with the largest value returned from [`EvaluateMagnitude`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_EvaluateMagnitude_)). If a different control exceeds the actuation level of the current active control, it becomes the active control.

For [composite bindings](./composite-bindings.md), magnitudes of the composite as a whole rather than for individual controls are tracked. However, [`activeControl`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_activeControl) will still track individual Controls from the composite.

## Disabling Conflict Resolution

Conflict resolution is always applied to **Button** or **Value** [action types](./configure-action-type.md). However, it can be undesirable in situations when an action is simply used to gather all inputs from bound Controls. For example, if you have a **Button** type action bound to the **A** button on all gamepads, a user holding down **A** on one gamepad means that the **A** button on other gamepads is ignored.

By using the **Pass Through** action type, conflict resolution is bypassed, which means pressing the **A** button on one gamepad will not result in presses on other gamepads being ignored.

## Multiple input sequences (such as keyboard shortcuts)

>__Note__: The mechanism described here only applies to Actions that are part of the same [action map](./action-maps-panel.md) or [action assets](./action-assets.md).

Inputs used in combinations with other inputs can also lead to ambiguities. If, for example, the **B** key on the Keyboard is bound both on its own as well as in combination with the **Shift** key, then if you first press **Shift** and then **B**, the latter key press would be a valid input for either of the Actions.

The way the Input System handles this, is that Bindings are processed in the order of decreasing complexity. This metric is derived automatically from the Binding:

* A binding that is *not* part of a [composite](composite-bindings.md) is assigned a complexity of 1.
* A binding that *is* part of a [composite](composite-bindings.md) is assigned a complexity equal to the number of part bindings in the composite.

In our example, this means that a **one-modifier composite** binding to **Shift** + **B** has a higher complexity than a Binding to  **B** and gets processed first.

Additionally, the first Binding that results in the Action changing [phase](./set-callbacks-on-actions.md) will consume the input. This results in other Bindings to the same input not being processed. This means in our example, when the **Shift** + **B** binding consumes the **B** input, the Binding to **B** is skipped.

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