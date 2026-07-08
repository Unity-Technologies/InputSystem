---
uid: input-system-poll-actions
---

# Polling actions

Polling actions is one of the two main [ways to respond to actions](about-responding-to-input.md) using the recommended workflow.

Polling refers to the act of repeatedly checking the status or value of an action. This is in contrast to using callbacks which are only triggered when an action is performed.

The code required to poll an action depends on the [action type](action-type-reference.md), and whether you want to read the action's values or use the [interaction state](Interactions.md) instead.

## Poll value-type actions

To poll an action whose type is **Value** or **Pass-through**, use:

- [`ReadValue<>()`](xref:UnityEngine.InputSystem.InputAction)

You must use the corresponding type that matches the action's [control type's](action-and-control-types.md) value. For example, `ReadValue<Vector2>()` for a 2D axis.

## Poll button-type actions

To poll an action whose type is **Button**, use:

- [`IsPressed()`](xref:UnityEngine.InputSystem.InputAction)
- [`WasPressedThisFrame()`](xref:UnityEngine.InputSystem.InputAction)
- [`WasReleasedThisFrame()`](xref:UnityEngine.InputSystem.InputAction)

Buttons have no applicable value other than whether they are pressed or released.

## Poll using interaction phases

Actions have [interaction phases](introduction-interactions.md) which can be either `Waiting`, `Started`, `Performed` or `Canceled`. The interaction phase changes depending on the action's [interaction type](built-in-interactions.md), if one is assigned, or the [default interaction](default-interactions.md) otherwise. You can poll an action's interaction phase using the following methods:

- [`phase`](xref:UnityEngine.InputSystem.InputAction)
- [`WasPerformedThisFrame()`](xref:UnityEngine.InputSystem.InputAction)
- [`WasCompletedThisFrame()`](xref:UnityEngine.InputSystem.InputAction)

## Examples

### Polling actions example with a 2D axis and a button action
This example shows polling two actions in `Update`.

The `Move` action is a value-type, and the `Jump` action is a button-type.

```CSharp
using UnityEngine;
using UnityEngine.InputSystem;

public class Example : MonoBehaviour
{
    // These variables are to hold the Action references
    InputAction moveAction;
    InputAction jumpAction;

    private void Start()
    {
        // Find the references to the "Move" and "Jump" actions
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    void Update()
    {
        // Read the "Move" action value, which is a 2D vector
        // and the "Jump" action state, which is a button

        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        // your movement code here

        if (jumpAction.IsPressed())
        {
            // your jump code here
        }
    }
}
```

### Polling actions example using interaction phase


This example uses the Interact action from the [default actions](default-actions.md), which has a [Hold](built-in-interactions.md#hold) interaction to make it perform only after the bound control is held for a period of time (for example, 0.4s):

```CSharp
using UnityEngine;
using UnityEngine.InputSystem;

public class Example : MonoBehaviour
{
    InputAction interactAction;

    private void Start()
    {
        interactAction = InputSystem.actions.FindAction("Interact");
    }

    void Update()
    {
        if (interactAction.WasPerformedThisFrame())
        {
            // your code to respond to the first frame that the Interact action is held for enough time
        }

        if (interactAction.WasCompletedThisFrame())
        {
            // your code to respond to the frame that the Interact action is released after being held for enough time
        }
    }
}
```


### Polling button actions example

This example uses three actions called Shield, Teleport, and Submit (which are not included in the [default actions](default-actions.md)):

```CSharp
using UnityEngine;
using UnityEngine.InputSystem;

public class Example : MonoBehaviour
{
    InputAction shieldAction;
    InputAction teleportAction;
    InputAction submitAction;

    private void Start()
    {
        shieldAction = InputSystem.actions.FindAction("Shield");
        teleportAction = InputSystem.actions.FindAction("Teleport");
        submitAction = InputSystem.actions.FindAction("Submit");
    }

    void Update()
    {
        if (shieldAction.IsPressed())
        {
            // shield is active for every frame that the shield action is pressed
        }

        if (teleportAction.WasPressedThisFrame())
        {
            // teleport occurs on the first frame that the action is pressed, and not again until the button is released
        }

            if (submitAction.WasReleasedThisFrame())
        {
            // submit occurs on the frame that the action is released, a common technique for buttons relating to UI controls.
        }
    }
}
```
