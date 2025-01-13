# Control actuation

A Control is considered actuated when it has moved away from its default state in such a way that it affects the actual value of the Control. You can query whether a Control is currently actuated using [`IsActuated`](../api/UnityEngine.InputSystem.InputControlExtensions.html#UnityEngine_InputSystem_InputControlExtensions_IsActuated_UnityEngine_InputSystem_InputControl_System_Single_).

```CSharp
// Check if leftStick is currently actuated.
if (Gamepad.current.leftStick.IsActuated())
    Debug.Log("Left Stick is actuated");
```

It can be useful to determine not just whether a Control is actuated at all, but also the amount by which it is actuated (that is, its magnitude). For example, for a [`Vector2Control`](../api/UnityEngine.InputSystem.Controls.Vector2Control.html) this would be the length of the vector, whereas for a button it is the raw, absolute floating-point value.

In general, the current magnitude of a Control is always >= 0. However, a Control might not have a meaningful magnitude, in which case it returns -1. Any negative value should be considered an invalid magnitude.

You can query the current amount of actuation using [`EvaluateMagnitude`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_EvaluateMagnitude).

```CSharp
// Check if left stick is actuated more than a quarter of its motion range.
if (Gamepad.current.leftStick.EvaluateMagnitude() > 0.25f)
    Debug.Log("Left Stick actuated past 25%");
```

There are two mechanisms that most notably make use of Control actuation:

- [Interactive rebinding](ActionBindings.md#interactive-rebinding) (`InputActionRebindingExceptions.RebindOperation`) uses it to select between multiple suitable Controls to find the one that is actuated the most.
- [Conflict resolution](ActionBindings.md#conflicting-inputs) between multiple Controls that are bound to the same action uses it to decide which Control gets to drive the action.