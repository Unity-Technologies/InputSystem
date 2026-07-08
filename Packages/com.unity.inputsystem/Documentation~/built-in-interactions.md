---
uid: input-system-built-in-interactions
---

# Built-in interactions

The Input System package comes with a set of built-in interactions, which you can use on actions and bindings:

* [`PressInteraction`](xref:UnityEngine.InputSystem.Interactions.PressInteraction)
* [`HoldInteraction`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction)
* [`TapInteraction`](xref:UnityEngine.InputSystem.Interactions.TapInteraction)
* [`SlowTapInteraction`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction)
* [`MultiTapInteraction`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction)

Each built-in Interaction has its own parameters, and responds differently to Interaction callbacks.

> [!NOTE]
> The built-in Interactions operate on Control actuation and don't use Control values directly. The Input System evaluates the `pressPoint` parameters against the magnitude of the Control actuation. This means you can use these Interactions on any Control which has a magnitude, such as sticks, and not just on buttons.

If an action or binding has no interaction set, the system uses its [default Interaction](default-interactions.md).

## Press

You can use a [`PressInteraction`](xref:UnityEngine.InputSystem.Interactions.PressInteraction) to explicitly force button-like interactions. Use the [`behavior`](xref:UnityEngine.InputSystem.Interactions.PressInteraction) parameter to select if the Interaction should trigger on button press, release, or both.

|__Parameters__|Type|Default value|
|---|---|---|
|[`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction)|`float`|[`InputSettings.defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings)|
|[`behavior`](xref:UnityEngine.InputSystem.Interactions.PressInteraction)|[`PressBehavior`](xref:UnityEngine.InputSystem.Interactions.PressBehavior)|`PressOnly`|


|__Callbacks__/[`behavior`](xref:UnityEngine.InputSystem.Interactions.PressInteraction)|`PressOnly`|`ReleaseOnly`|`PressAndRelease`|
|---|-----------|-------------|-----------------|
|[`started`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction)|
|[`performed`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction)|Control magnitude goes back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction)|- Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction)<br>or<br>- Control magnitude goes back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction)|
|[`canceled`](xref:UnityEngine.InputSystem.InputAction)|not used|not used|not used|

## Hold

A [`HoldInteraction`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction) requires the user to hold a Control for [`duration`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction) seconds before the Input System triggers the Action.

|__Parameters__|Type|Default value|
|---|---|---|
|[`duration`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction)|`float`|[`InputSettings.defaultHoldTime`](xref:UnityEngine.InputSystem.InputSettings)|
|[`pressPoint`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction)|`float`|[`InputSettings.defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings)|


To display UI feedback when a button starts being held, use the [`started`](xref:UnityEngine.InputSystem.InputAction) callback.

```C#

    action.started += _ => ShowGunChargeUI();
    action.performed += _ => FinishGunChargingAndHideChargeUI();
    action.cancelled += _ => HideChargeUI();

```

|__Callbacks__||
|---|---|
|[`started`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction).|
|[`performed`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude held above [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction) for >= [`duration`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction).|
|[`canceled`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude goes back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction) before [`duration`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction) (that is, the button was not held long enough).|

## Tap

A [`TapInteraction`](xref:UnityEngine.InputSystem.Interactions.TapInteraction) requires the user to press and release a Control within [`duration`](xref:UnityEngine.InputSystem.Interactions.TapInteraction) seconds to trigger the Action.

|__Parameters__|Type|Default value|
|---|---|---|
|[`duration`](xref:UnityEngine.InputSystem.Interactions.TapInteraction)|`float`|[`InputSettings.defaultTapTime`](xref:UnityEngine.InputSystem.InputSettings)|
|[`pressPoint`](xref:UnityEngine.InputSystem.Interactions.TapInteraction)|`float`|[`InputSettings.defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings)|

|__Callbacks__||
|---|---|
|[`started`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.TapInteraction).|
|[`performed`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude goes back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.TapInteraction) before [`duration`](xref:UnityEngine.InputSystem.Interactions.TapInteraction).|
|[`canceled`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude held above [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.TapInteraction) for >= [`duration`](xref:UnityEngine.InputSystem.Interactions.TapInteraction) (that is, the tap was too slow).|

## SlowTap

A [`SlowTapInteraction`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction) requires the user to press and hold a Control for a minimum duration of [`duration`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction) seconds, and then release it, to trigger the Action.

|__Parameters__|Type|Default value|
|---|---|---|
|[`duration`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction)|`float`|[`InputSettings.defaultSlowTapTime`](xref:UnityEngine.InputSystem.InputSettings)|
|[`pressPoint`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction)|`float`|[`InputSettings.defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings)|

|__Callbacks__||
|---|---|
|[`started`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction).|
|[`performed`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude goes back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction) after [`duration`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction).|
|[`canceled`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude goes back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction) before [`duration`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction) (that is, the tap was too fast).|

## MultiTap

A [`MultiTapInteraction`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction) requires the user to press and release a Control within [`tapTime`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction) seconds [`tapCount`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction) times, with no more then [`tapDelay`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction) seconds passing between taps, for the Interaction to trigger. You can use this to detect double-click or multi-click gestures.

|__Parameters__|Type|Default value|
|---|---|---|
|[`tapTime`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction)|`float`|[`InputSettings.defaultTapTime`](xref:UnityEngine.InputSystem.InputSettings)|
|[`tapDelay`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction)|`float`|2 * [`tapTime`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction)|
|[`tapCount`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction)|`int`|2|
|[`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction)|`float`|[`InputSettings.defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings)|

|__Callbacks__||
|---|---|
|[`started`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction).|
|[`performed`](xref:UnityEngine.InputSystem.InputAction)|Control magnitude went back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction) and back up above it repeatedly for [`tapCount`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction) times.|
|[`canceled`](xref:UnityEngine.InputSystem.InputAction)|- After going back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction), Control magnitude did not go back above [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction) within [`tapDelay`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction) time (that is, taps were spaced out too far apart).<br>or<br>- After going back above [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction), Control magnitude did not go back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction) within [`tapTime`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction) time (that is, taps were too long).|
