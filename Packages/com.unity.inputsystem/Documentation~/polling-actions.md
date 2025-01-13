# Polling Actions

You can poll the current value of an Action using [`InputAction.ReadValue<>()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_ReadValue__1):

```CSharp
using UnityEngine;
using UnityEngine.InputSystem;

public class Example : MonoBehaviour
{
    InputAction moveAction;

    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
    }

    void Update()
    {
        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        // your code would then use moveValue to apply movement
        // to your GameObject
    }
}
```

Note that the value type has to correspond to the value type of the control that the value is being read from.

There are two methods you can use to poll for `performed` [action callbacks](#action-callbacks) to determine whether an action was performed or stopped performing in the current frame.

These methods differ from [`InputAction.WasPressedThisFrame()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasPressedThisFrame) and [`InputAction.WasReleasedThisFrame()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasReleasedThisFrame) in that these depend directly on the [Interactions](Interactions.md) driving the action (including the [default Interaction](Interactions.md#default-interaction) if no specific interaction has been added to the action or binding).

|Method|Description|
|------|-----------|
|[`InputAction.WasPerformedThisFrame()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasPerformedThisFrame)|True if the [`InputAction.phase`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_phase) of the action has, at any point during the current frame, changed to [`Performed`](../api/UnityEngine.InputSystem.InputActionPhase.html#UnityEngine_InputSystem_InputActionPhase_Performed).|
|[`InputAction.WasCompletedThisFrame()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasCompletedThisFrame)|True if the [`InputAction.phase`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_phase) of the action has, at any point during the current frame, changed away from [`Performed`](../api/UnityEngine.InputSystem.InputActionPhase.html#UnityEngine_InputSystem_InputActionPhase_Performed) to any other phase. This can be useful for [Button](#button) actions or [Value](#value) actions with interactions like [Press](Interactions.md#press) or [Hold](Interactions.md#hold) when you want to know the frame the interaction stops being performed. For actions with the [default Interaction](Interactions.md#default-interaction), this method will always return false for [Value](#value) and [Pass-Through](#pass-through) actions (since the phase stays in [`Started`](../api/UnityEngine.InputSystem.InputActionPhase.html#UnityEngine_InputSystem_InputActionPhase_Started) for Value actions and stays in [`Performed`](../api/UnityEngine.InputSystem.InputActionPhase.html#UnityEngine_InputSystem_InputActionPhase_Performed) for Pass-Through).|

This example uses the Interact action from the [default actions](./about-project-wide-actions.md#the-default-actions), which has a [Hold](Interactions.md#hold) interaction to make it perform only after the bound control is held for a period of time (for example, 0.4 seconds):

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
