# Default Interactions

Different Action types have different default Interactions. For example, the following diagram shows the behavior of the built-in Interactions for a simple button press.

![Interaction Diagram](./Images/InteractionsDiagram.png)

The following table provides an at-a-glance overview of how Interaction phases change for each action type's default Interaction. 

|__Callback__|[`InputActionType.Value`](RespondingToActions.md#value)|[`InputActionType.Button`](RespondingToActions.md#button)|[`InputActionType.PassThrough`](RespondingToActions.md#pass-through)|
|-----------|-------------|------------|-----------------|
|[`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started)|Control(s) changed value away from the default value.|Button started being pressed but has not necessarily crossed the press threshold yet.|not used|
|[`performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)|Control(s) changed value.|Button was pressed to at least the button [press threshold](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint).|Control changed value.|
|[`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)|Control(s) are no longer actuated.|Button was released. If the button was pressed above the press threshold, the button has now fallen to or below the [release threshold](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_buttonReleaseThreshold). If the button was never fully pressed, the button is now back to completely unpressed.|Action is disabled.|

## Value Action default Interaction

[`Value`](RespondingToActions.md#value)-type Actions have a default Interaction with the following behavior:

1. As soon as a bound Control becomes [actuated](Controls.md#control-actuation), the Action's Interaction phase goes from `Waiting` to `Started`, and then immediately to `Performed` and back to `Started`. One callback occurs on [`InputAction.started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started), followed by one callback on [`InputAction.performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed).
2. For as long as the bound Control remains actuated, the Action's Interaction phase stays in `Started` and triggers `Performed` whenever the value of the Control changes (that is, one call occurs to [`InputAction.performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)).
3. When the bound Control stops being actuated, the Action's Interaction phase goes to `Canceled` and then back to `Waiting`. One call occurs to [`InputAction.canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled).

## Button Action default Interaction

[`Button`](RespondingToActions.md#button)-type Actions have a default Interaction with the following behavior:

1. As soon as a bound Control becomes [actuated](Controls.md#control-actuation), the Action's Interaction phase goes from `Waiting` to `Started`. One callback occurs on [`InputAction.started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started).
2. If a Control then reaches or exceeds the button press threshold, the Action's Interaction phase goes from `Started` to `Performed`. One callback occurs on [`InputAction.performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed). The default value of the button press threshold is defined in the [input settings](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint). However, an individual control can [override](../api/UnityEngine.InputSystem.Controls.ButtonControl.html#UnityEngine_InputSystem_Controls_ButtonControl_pressPoint) this value.
3. Once the Action has `Performed`, if all Controls then go back to a level of actuation at or below the [release threshold](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_buttonReleaseThreshold), the Action's Interaction phase goes from `Performed` to `Canceled`. One call occurs to [`InputAction.canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled).
4. If the Action's Interaction phase never went to `Performed`, it will go to `Canceled` as soon as all Controls are released. One call occurs to [`InputAction.canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled).

## PassThrough Action default Interaction

[`PassThrough`](RespondingToActions.md#pass-through)-type Actions have a default Interaction with a simpler behavior. The Input System doesn't try to track bound Controls as a single source of input. Instead, it triggers a `Performed` callback for each value change.