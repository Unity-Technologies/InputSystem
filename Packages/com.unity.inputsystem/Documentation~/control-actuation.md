---
uid: input-system-control-actuation
---

# Control actuation

Control actuation refers to whether or not a [control](controls.md) is currently being used by the user.

A control is considered actuated when it has moved away from its default state in such a way that it affects the value of the control.

The recommended workflow is to [bind controls to actions](add-duplicate-delete-binding.md), and then [respond to input at runtime](./respond-to-input.md) by polling or recieving callbacks from those actions. For this reason, it is not typically necessary to directly check whether a control is actuated. Instead, actuation of a control bound to an action causes the action to be performed (according to its [interaction pattern](Interactions.md), if an interaction has been assigned).

However in some scenarios you might want to directly read the actuation of a control.

## Directly read the actuation of a control

You can query whether a control is currently actuated using [`IsActuated`](../api/UnityEngine.InputSystem.InputControlExtensions.html#UnityEngine_InputSystem_InputControlExtensions_IsActuated_UnityEngine_InputSystem_InputControl_System_Single_).

```CSharp
// Check if leftStick is currently actuated.
if (Gamepad.current.leftStick.IsActuated())
    Debug.Log("Left Stick is actuated");
```

It can be useful to determine not just whether a control is actuated at all, but also the amount by which it is actuated (that is, its magnitude). For example, for a [`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html) this is the length of the vector, whereas for a button it is the raw, absolute floating-point value.

In general, the current magnitude of a control is always greater than or equal to zero. However, a control might not have a meaningful magnitude, in which case it returns -1. Any negative value should be considered an invalid magnitude.

You can query the current amount of actuation using [`EvaluateMagnitude`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_EvaluateMagnitude).

```CSharp
// Check if left stick is actuated more than a quarter of its motion range.
if (Gamepad.current.leftStick.EvaluateMagnitude() > 0.25f)
    Debug.Log("Left Stick actuated past 25%");
```

There are two mechanisms within the Input System that most notably make use of control actuation:

- [Interactive rebinding](interactive-rebinding.md) (`InputActionRebindingExceptions.RebindOperation`) uses it to select between multiple suitable controls to find the one that is actuated the most.
- [Conflict resolution](binding-conflicts.md) between multiple controls that are bound to the same action uses it to decide which control gets to drive the action.