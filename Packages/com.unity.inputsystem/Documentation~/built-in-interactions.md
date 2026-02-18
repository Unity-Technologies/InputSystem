---
uid: input-system-built-in-interactions
---

# Built-in interactions

The Input System package comes with a set of built-in Interactions, which you can use on Actions and Bindings:

* [`PressInteraction`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html)
* [`HoldInteraction`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html)
* [`TapInteraction`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html)
* [`SlowTapInteraction`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html)
* [`MultiTapInteraction`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html)

Each built-in Interaction has its own parameters, and responds differently to Interaction callbacks.

>[!Note]
>The built-in Interactions operate on Control actuation and don't use Control values directly. The Input System evaluates the `pressPoint` parameters against the magnitude of the Control actuation. This means you can use these Interactions on any Control which has a magnitude, such as sticks, and not just on buttons.

If an Action or Binding has no Interaction set, the system uses its [default Interaction](default-interactions.md).

## Press

You can use a [`PressInteraction`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html) to explicitly force button-like interactions. Use the [`behavior`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_behavior) parameter to select if the Interaction should trigger on button press, release, or both.

|__Parameters__|Type|Default value|
|---|---|---|
|[`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint)|
|[`behavior`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_behavior)|[`PressBehavior`](../api/UnityEngine.InputSystem.Interactions.PressBehavior.html)|`PressOnly`| 


|__Callbacks__/[`behavior`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_behavior)|`PressOnly`|`ReleaseOnly`|`PressAndRelease`|
|---|-----------|-------------|-----------------|
|[`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|
|[`performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|Control magnitude goes back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|- Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)<br>or<br>- Control magnitude goes back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|
|[`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)|not used|not used|not used|

## Hold

A [`HoldInteraction`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html) requires the user to hold a Control for [`duration`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_duration) seconds before the Input System triggers the Action.

|__Parameters__|Type|Default value|
|---|---|---|
|[`duration`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_duration)|`float`|[`InputSettings.defaultHoldTime`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultHoldTime)|
|[`pressPoint`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint)|


To display UI feedback when a button starts being held, use the [`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started) callback.

```C#

    action.started += _ => ShowGunChargeUI();
    action.performed += _ => FinishGunChargingAndHideChargeUI();
    action.cancelled += _ => HideChargeUI();

```

|__Callbacks__||
|---|---|
|[`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_pressPoint).|
|[`performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)|Control magnitude held above [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_pressPoint) for >= [`duration`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_duration).|
|[`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)|Control magnitude goes back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_pressPoint) before [`duration`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_duration) (that is, the button was not held long enough).|

## Tap

A [`TapInteraction`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html) requires the user to press and release a Control within [`duration`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_duration) seconds to trigger the Action.

|__Parameters__|Type|Default value|
|---|---|---|
|[`duration`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_duration)|`float`|[`InputSettings.defaultTapTime`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultTapTime)|
|[`pressPoint`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint)|

|__Callbacks__||
|---|---|
|[`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_pressPoint).|
|[`performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)|Control magnitude goes back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_pressPoint) before [`duration`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_duration).|
|[`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)|Control magnitude held above [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_pressPoint) for >= [`duration`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_duration) (that is, the tap was too slow).|

## SlowTap

A [`SlowTapInteraction`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html) requires the user to press and hold a Control for a minimum duration of [`duration`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_duration) seconds, and then release it, to trigger the Action.

|__Parameters__|Type|Default value|
|---|---|---|
|[`duration`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_duration)|`float`|[`InputSettings.defaultSlowTapTime`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultSlowTapTime)|
|[`pressPoint`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint)|

|__Callbacks__||
|---|---|
|[`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_pressPoint).|
|[`performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)|Control magnitude goes back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_pressPoint) after [`duration`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_duration).|
|[`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)|Control magnitude goes back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_pressPoint) before [`duration`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_duration) (that is, the tap was too fast).|

## MultiTap

A [`MultiTapInteraction`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html) requires the user to press and release a Control within [`tapTime`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapTime) seconds [`tapCount`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapCount) times, with no more then [`tapDelay`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapDelay) seconds passing between taps, for the Interaction to trigger. You can use this to detect double-click or multi-click gestures.

|__Parameters__|Type|Default value|
|---|---|---|
|[`tapTime`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapTime)|`float`|[`InputSettings.defaultTapTime`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultTapTime)|
|[`tapDelay`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapDelay)|`float`|2 * [`tapTime`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapTime)|
|[`tapCount`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapCount)|`int`|2|
|[`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint)|

|__Callbacks__||
|---|---|
|[`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint).|
|[`performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)|Control magnitude went back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint) and back up above it repeatedly for [`tapCount`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapCount) times.|
|[`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)|- After going back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint), Control magnitude did not go back above [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint) within [`tapDelay`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapDelay) time (that is, taps were spaced out too far apart).<br>or<br>- After going back above [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint), Control magnitude did not go back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint) within [`tapTime`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapTime) time (that is, taps were too long).|