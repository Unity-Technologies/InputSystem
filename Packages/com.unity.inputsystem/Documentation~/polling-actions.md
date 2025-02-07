# Polling actions

Polling actions is one of the two main [ways to respond to actions](about-responding-to-input.md) using the recommended workflow.

Polling refers to the act of repeatedly checking the status or value of an action. This is in contrast to using callbacks which are only triggered when an action is performed.

The code required to poll an action depends on the [action type](action-type-reference.md)

## Poll value-type actions

To poll an action whose type is **Value** or **Pass-through**, use:

- [`ReadValue<>()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_ReadValue__1) with the corresponding type that matches the action's [control type's](control-types.md) value.

## Poll button-type actions

To poll an action whose type is **Button**, use:

- [`IsPressed()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasPressedThisFrame) 
- [`WasPressedThisFrame()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasPressedThisFrame) 
-  [`WasReleasedThisFrame()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasReleasedThisFrame)



## Poll using interaction phases

Actions have interaction phases which can be either `Waiting`, `Started`, `Performed` or `Canceled`. When the interaction phase changes depends on the action's [interaction type](built-in-interactions.md), if one is assigned, or the [default interaction](default-interactions.md) otherwise. You can poll an action's interaction phase using the following methods:

- [`phase`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_phase)
- [`WasPerformedThisFrame()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasPerformedThisFrame)
- [`WasCompletedThisFrame()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasCompletedThisFrame)

## Examples

### Polling value and button actions example
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

### Polling interaction phase example 


This example uses the Interact action from the [default actions](default-actions.md), which has a [Hold](Interactions.md#hold) interaction to make it perform only after the bound control is held for a period of time (for example, 0.4s):

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






Note that the value type has to correspond to the value type of the control that the value is being read from.

There are two methods you can use to poll for `performed` [action callbacks](#action-callbacks) to determine whether an action was performed or stopped performing in the current frame.




Finally, there are three methods you can use to poll for button presses and releases:

|Method|Description|
|------|-----------|
|[`InputAction.IsPressed()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_IsPressed)|True if the level of [actuation](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_EvaluateMagnitude) on the action has crossed the [press point](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint) and did not yet fall to or below the [release threshold](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_buttonReleaseThreshold).|
|[`InputAction.WasPressedThisFrame()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasPressedThisFrame)|True if the level of [actuation](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_EvaluateMagnitude) on the action has, at any point during the current frame, reached or gone above the [press point](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint).|
|[`InputAction.WasReleasedThisFrame()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasReleasedThisFrame)|True if the level of [actuation](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_EvaluateMagnitude) on the action has, at any point during the current frame, gone from being at or above the [press point](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint) to at or below the [release threshold](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_buttonReleaseThreshold).|

This example uses three actions called Shield, Teleport and Submit (which are not included in the [default actions](./about-project-wide-actions.md#the-default-actions)):

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

        if (submit.WasReleasedThisFrame())
        {
            // submit occurs on the frame that the action is released, a common technique for buttons relating to UI controls.
        }
    }
}
```
