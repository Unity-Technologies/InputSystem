---
uid: input-system-interactions
---
# Interactions

An Interaction represents a specific input pattern. For example, a [hold](#hold) is an Interaction that requires a Control to be held for at least a minimum amount of time.

Interactions drive responses on Actions. You can place them on individual Bindings or an Action as a whole, in which case they apply to every Binding on the Action. At runtime, when a particular interaction completes, this triggers the Action.

![The Binding Path displays the buttonSouth [Gamepad] value set on the Interaction Properties window.](Images/InteractionProperties.png){width="486" height="585"}

## Operation

An Interaction has a set of distinct phases it can go through in response to receiving input.

|Phase|Description|
|-----|-----------|
|`Waiting`|The Interaction is waiting for input.|
|`Started`|The Interaction has been started (that is, it received some of its expected input), but is not complete yet.|
|`Performed`|The Interaction is complete.|
|`Canceled`|The Interaction was interrupted and aborted. For example, the user pressed and then released a button before the minimum time required for a [hold Interaction](#hold) to complete.|

Not every Interaction triggers every phase, and the pattern in which specific Interactions trigger phases depends on the Interaction type.

While `Performed` is typically the phase that triggers the actual response to an Interaction, `Started` and `Canceled` can be  useful for providing UI feedback while the Interaction is in progress. For example, when a [hold](#hold) is `Started`, the app can display a progress bar that fills up until the hold time has been reached. If, however, the hold is `Canceled` before it completes, the app can reset the progress bar to the beginning.

The following example demonstrates this kind of setup with a fire Action that the user can tap to fire immediately, or hold to charge:

```CSharp
var fireAction = new InputAction("fire");
fireAction.AddBinding("<Gamepad>/buttonSouth")
    // Tap fires, slow tap charges. Both act on release.
    .WithInteractions("tap,slowTap");

fireAction.started +=
    context =>
    {
        if (context.interaction is SlowTapInteraction)
            ShowChargingUI();
    };

fireAction.performed +=
    context =>
    {
        if (context.interaction is SlowTapInteraction)
            ChargedFire();
        else
            Fire();
    };

fireAction.canceled +=
    _ => HideChargingUI();
fireAction.Enable();
```

### Multiple Controls on an Action

If you have multiple Controls bound to a Binding or an Action which has an Interaction, then the Input System first applies the [Control conflict resolution](xref:input-system-action-bindings#conflicting-inputs) logic to get a single value for the Action, which it then feeds to the Interaction logic. Any of the bound Controls can perform the Interaction.

### Multiple Interactions on a Binding

If multiple Interactions are present on a single Binding or Action, then the Input System checks the Interactions in the order they are present on the Binding. The code example [above](#operation) illustrates this example. The Binding on the `fireAction` Action has two Interactions: `WithInteractions("tap;slowTap")`. The [tap](#tap) Interaction gets a first chance at interpreting the input from the Action. If the button is pressed, the Action calls the `Started` callback on the tap Interaction. If the user keeps holding the button, the tap Interaction times out, and the Action calls the [`Canceled`](xref:UnityEngine.InputSystem.InputAction.canceled) callback for the tap Interaction and starts processing the [slow tap](#slowtap) Interaction (which now receives a `Started` callback).

At any one time, only one Interaction can be "driving" the action (that is, it gets to determine the action's current [`phase`](xref:UnityEngine.InputSystem.InputAction.phase)). If an Interaction higher up in the stack cancels, Interactions lower down in the stack can take over.

Note that the order of interactions can affect which interaction is passed to your callback function. For example, an action with [Tap](#tap), [MultiTap](#multitap) and [Hold](#hold) interactions will have different behaviour when the interactions are in a different order, such as [Hold](#hold), [MultiTap](#multitap) and [Tap](#tap). If you get unexpected behaviour, you may need to experiment with a different ordering.

### Timeouts

Interactions might need to wait a certain time for a specific input to occur or to not occur. An example of this is the [Hold](#hold) interaction which, after a button is pressed, has to wait for a set [duration](xref:UnityEngine.InputSystem.Interactions.HoldInteraction.duration) until the "hold" is complete. To do this, an interaction installs a timeout using [`SetTimeout`](xref:UnityEngine.InputSystem.InputInteractionContext.SetTimeout(System.Single)).

It can be useful to know how much of a timeout is left for an interaction to complete. For example, you might want to display a bar in the UI that is charging up while the interaction is waiting to complete. To query the percentage to which a timeout has completed, use [`GetTimeoutCompletionPercentage`](xref:UnityEngine.InputSystem.InputAction.GetTimeoutCompletionPercentage*).

```CSharp
// Returns a value between 0 (inclusive) and 1 (inclusive).
var warpActionCompletion = playerInput.actions["warp"].GetTimeoutCompletionPercentage();
```

Note that each Interaction can have its own separate timeout (but only a single one at any one time). If [multiple interactions](#multiple-interactions-on-a-binding) are in effect, then [`GetTimeoutCompletionPercentage`](xref:UnityEngine.InputSystem.InputAction.GetTimeoutCompletionPercentage*) will only use the timeout of the one interaction that is currently driving the action.

Some Interactions might involve multiple timeouts in succession. In this case, knowing only the completion of the currently running timeout (if any) is often not useful. An example is [`MultiTapInteraction`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction), which involves a timeout on each individual tap, as well as a timeout in-between taps. The Interaction is complete only after a full tap sequence has been performed.

An Interaction can use [`SetTotalTimeoutCompletionTime`](xref:UnityEngine.InputSystem.InputInteractionContext.SetTotalTimeoutCompletionTime(System.Single)) to inform the Input System of the total time it will run timeouts for.

## Using Interactions

You can install Interactions on [Bindings](xref:input-system-action-bindings) or [Actions](xref:input-system-actions).

### Interactions applied to Bindings

When you create Bindings for your [Actions](xref:input-system-actions), you can choose to add Interactions to the Bindings.

If you're using [project-wide actions](xref:input-system-configuring-input), or [Input Action Assets](xref:input-system-action-assets), you can add any Interaction to your Bindings in the Input Action editor.

To add an Interaction:

1. [Create some Bindings](xref:input-system-configuring-input#bindings).

2. Select the Binding you want to add Interactions to.

    The right pane of the window shows the properties for that Binding.

3. Click on the plus icon on the __Interactions__ foldout to open a list of all available Interactions types.

4. Choose an Interaction type to add an Interaction instance of that type.

    The Interaction now appears in the __Interactions__ foldout. If the Interaction has any parameters, you can edit them, as well.

To remove an Interaction, click the minus button next to it.

To change the [order of Interactions](#multiple-interactions-on-a-binding), click the up and down arrows.

If you create your Bindings in code, you can add Interactions like this:

```CSharp
var Action = new InputAction();
action.AddBinding("<Gamepad>/leftStick")
    .WithInteractions("tap(duration=0.8)");
```

### Interactions applied to Actions

Applying Interactions directly to an Action is equivalent to applying them to all Bindings for the Action. It is thus more or less a shortcut that avoids manually adding the same Interaction(s) to each of the Bindings.

If Interactions are applied __both__ to an Action and to its Bindings, then the effect is the same as if the Action's Interactions are appended to the list of Interactions on each of the Bindings. This means that the Binding's Interactions are applied first, and then the Action's Interactions are applied after.

You can add and edit Interactions on Actions in the [Input Action Assets](xref:input-system-action-assets) editor window the [same way](#interactions-applied-to-bindings) as you would do for Bindings: select an Action to Edit, then add the Interactions in the right window pane.

If you create your Actions in code, you can add Interactions like this:

```CSharp
var Action = new InputAction(Interactions: "tap(duration=0.8)");
```

## Predefined Interactions

The Input System package comes with a set of basic Interactions you can use. If an Action has no Interactions set, the system uses its [default Interaction](#default-interaction).

> [!NOTE]
> The built-in Interactions operate on Control actuation and don't use Control values directly. The Input System evaluates the `pressPoint` parameters against the magnitude of the Control actuation. This means you can use these Interactions on any Control which has a magnitude, such as sticks, and not just on buttons.

The following diagram shows the behavior of the built-in Interactions for a simple button press.

![Interaction Diagram](./Images/InteractionsDiagram.png)

### Default Interaction

If you haven't specifically added an Interaction to a Binding or its Action, the default Interaction applies to the Binding.

[`Value`](xref:input-system-responding#value) type Actions have the following behavior:

1. As soon as a bound Control becomes [actuated](xref:input-system-controls#control-actuation), the Action goes from `Waiting` to `Started`, and then immediately to `Performed` and back to `Started`. One callback occurs on [`InputAction.started`](xref:UnityEngine.InputSystem.InputAction.started), followed by one callback on [`InputAction.performed`](xref:UnityEngine.InputSystem.InputAction.performed).
2. For as long as the bound Control remains actuated, the Action stays in `Started` and triggers `Performed` whenever the value of the Control changes (that is, one call occurs to [`InputAction.performed`](xref:UnityEngine.InputSystem.InputAction.performed)).
3. When the bound Control stops being actuated, the Action goes to `Canceled` and then back to `Waiting`. One call occurs to [`InputAction.canceled`](xref:UnityEngine.InputSystem.InputAction.canceled).

[`Button`](xref:input-system-responding#button) type Actions have the following behavior:

1. As soon as a bound Control becomes [actuated](xref:input-system-controls#control-actuation), the Action goes from `Waiting` to `Started`. One callback occurs on [`InputAction.started`](xref:UnityEngine.InputSystem.InputAction.started).
2. If a Control then reaches or exceeds the button press threshold, the Action goes from `Started` to `Performed`. One callback occurs on [`InputAction.performed`](xref:UnityEngine.InputSystem.InputAction.performed). The default value of the button press threshold is defined in the [input settings](xref:UnityEngine.InputSystem.InputSettings.defaultButtonPressPoint). However, an individual control can [override](xref:UnityEngine.InputSystem.Controls.ButtonControl.pressPoint) this value.
3. Once the Action has `Performed`, if all Controls then go back to a level of actuation at or below the [release threshold](xref:UnityEngine.InputSystem.InputSettings.buttonReleaseThreshold), the Action goes from `Performed` to `Canceled`. One call occurs to [`InputAction.canceled`](xref:UnityEngine.InputSystem.InputAction.canceled).
4. If the Action never went to `Performed`, it will go to `Canceled` as soon as all Controls are released. One call occurs to [`InputAction.canceled`](xref:UnityEngine.InputSystem.InputAction.canceled).

[`PassThrough`](xref:input-system-responding#pass-through) type Actions have a simpler behavior. The Input System doesn't try to track bound Controls as a single source of input. Instead, it triggers a `Performed` callback for each value change.

|__Callback__|[`InputActionType.Value`](xref:input-system-responding#value)|[`InputActionType.Button`](xref:input-system-responding#button)|[`InputActionType.PassThrough`](xref:input-system-responding#pass-through)|
|-----------|-------------|------------|-----------------|
|[`started`](xref:UnityEngine.InputSystem.InputAction.started)|Control(s) changed value away from the default value.|Button started being pressed but has not necessarily crossed the press threshold yet.|not used|
|[`performed`](xref:UnityEngine.InputSystem.InputAction.performed)|Control(s) changed value.|Button was pressed to at least the button [press threshold](xref:UnityEngine.InputSystem.InputSettings.defaultButtonPressPoint).|Control changed value.|
|[`canceled`](xref:UnityEngine.InputSystem.InputAction.canceled)|Control(s) are no longer actuated.|Button was released. If the button was pressed above the press threshold, the button has now fallen to or below the [release threshold](xref:UnityEngine.InputSystem.InputSettings.buttonReleaseThreshold). If the button was never fully pressed, the button is now back to completely unpressed.|Action is disabled.|

### Press

You can use a [`PressInteraction`](xref:UnityEngine.InputSystem.Interactions.PressInteraction) to explicitly force button-like interactions. Use the [`behavior`](xref:UnityEngine.InputSystem.Interactions.PressInteraction.behavior) parameter to select if the Interaction should trigger on button press, release, or both.

|__Parameters__|Type|Default value|
|---|---|---|
|[`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction.pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings.defaultButtonPressPoint)|
|[`behavior`](xref:UnityEngine.InputSystem.Interactions.PressInteraction.behavior)|[`PressBehavior`](xref:UnityEngine.InputSystem.Interactions.PressBehavior)|`PressOnly`|


|__Callbacks__/[`behavior`](xref:UnityEngine.InputSystem.Interactions.PressInteraction.behavior)|`PressOnly`|`ReleaseOnly`|`PressAndRelease`|
|---|-----------|-------------|-----------------|
|[`started`](xref:UnityEngine.InputSystem.InputAction.started)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction.pressPoint)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction.pressPoint)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction.pressPoint)|
|[`performed`](xref:UnityEngine.InputSystem.InputAction.performed)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction.pressPoint)|Control magnitude goes back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction.pressPoint)|- Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction.pressPoint)<br>or<br>- Control magnitude goes back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.PressInteraction.pressPoint)|
|[`canceled`](xref:UnityEngine.InputSystem.InputAction.canceled)|not used|not used|not used|

### Hold

A [`HoldInteraction`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction) requires the user to hold a Control for [`duration`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction.duration) seconds before the Input System triggers the Action.

|__Parameters__|Type|Default value|
|---|---|---|
|[`duration`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction.duration)|`float`|[`InputSettings.defaultHoldTime`](xref:UnityEngine.InputSystem.InputSettings.defaultHoldTime)|
|[`pressPoint`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction.pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings.defaultButtonPressPoint)|


To display UI feedback when a button starts being held, use the [`started`](xref:UnityEngine.InputSystem.InputAction.started) callback.

```C#

    action.started += _ => ShowGunChargeUI();
    action.performed += _ => FinishGunChargingAndHideChargeUI();
    action.cancelled += _ => HideChargeUI();

```

|__Callbacks__||
|---|---|
|[`started`](xref:UnityEngine.InputSystem.InputAction.started)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction.pressPoint).|
|[`performed`](xref:UnityEngine.InputSystem.InputAction.performed)|Control magnitude held above [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction.pressPoint) for >= [`duration`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction.duration).|
|[`canceled`](xref:UnityEngine.InputSystem.InputAction.canceled)|Control magnitude goes back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction.pressPoint) before [`duration`](xref:UnityEngine.InputSystem.Interactions.HoldInteraction.duration) (that is, the button was not held long enough).|

### Tap

A [`TapInteraction`](xref:UnityEngine.InputSystem.Interactions.TapInteraction) requires the user to press and release a Control within [`duration`](xref:UnityEngine.InputSystem.Interactions.TapInteraction.duration) seconds to trigger the Action.

|__Parameters__|Type|Default value|
|---|---|---|
|[`duration`](xref:UnityEngine.InputSystem.Interactions.TapInteraction.duration)|`float`|[`InputSettings.defaultTapTime`](xref:UnityEngine.InputSystem.InputSettings.defaultTapTime)|
|[`pressPoint`](xref:UnityEngine.InputSystem.Interactions.TapInteraction.pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings.defaultButtonPressPoint)|

|__Callbacks__||
|---|---|
|[`started`](xref:UnityEngine.InputSystem.InputAction.started)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.TapInteraction.pressPoint).|
|[`performed`](xref:UnityEngine.InputSystem.InputAction.performed)|Control magnitude goes back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.TapInteraction.pressPoint) before [`duration`](xref:UnityEngine.InputSystem.Interactions.TapInteraction.duration).|
|[`canceled`](xref:UnityEngine.InputSystem.InputAction.canceled)|Control magnitude held above [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.TapInteraction.pressPoint) for >= [`duration`](xref:UnityEngine.InputSystem.Interactions.TapInteraction.duration) (that is, the tap was too slow).|

### SlowTap

A [`SlowTapInteraction`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction) requires the user to press and hold a Control for a minimum duration of [`duration`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction.duration) seconds, and then release it, to trigger the Action.

|__Parameters__|Type|Default value|
|---|---|---|
|[`duration`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction.duration)|`float`|[`InputSettings.defaultSlowTapTime`](xref:UnityEngine.InputSystem.InputSettings.defaultSlowTapTime)|
|[`pressPoint`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction.pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings.defaultButtonPressPoint)|

|__Callbacks__||
|---|---|
|[`started`](xref:UnityEngine.InputSystem.InputAction.started)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction.pressPoint).|
|[`performed`](xref:UnityEngine.InputSystem.InputAction.performed)|Control magnitude goes back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction.pressPoint) after [`duration`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction.duration).|
|[`canceled`](xref:UnityEngine.InputSystem.InputAction.canceled)|Control magnitude goes back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction.pressPoint) before [`duration`](xref:UnityEngine.InputSystem.Interactions.SlowTapInteraction.duration) (that is, the tap was too fast).|

### MultiTap

A [`MultiTapInteraction`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction) requires the user to press and release a Control within [`tapTime`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.tapTime) seconds [`tapCount`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.tapCount) times, with no more then [`tapDelay`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.tapDelay) seconds passing between taps, for the Interaction to trigger. You can use this to detect double-click or multi-click gestures.

|__Parameters__|Type|Default value|
|---|---|---|
|[`tapTime`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.tapTime)|`float`|[`InputSettings.defaultTapTime`](xref:UnityEngine.InputSystem.InputSettings.defaultTapTime)|
|[`tapDelay`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.tapDelay)|`float`|2 * [`tapTime`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.tapTime)|
|[`tapCount`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.tapCount)|`int`|2|
|[`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](xref:UnityEngine.InputSystem.InputSettings.defaultButtonPressPoint)|

|__Callbacks__||
|---|---|
|[`started`](xref:UnityEngine.InputSystem.InputAction.started)|Control magnitude crosses [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.pressPoint).|
|[`performed`](xref:UnityEngine.InputSystem.InputAction.performed)|Control magnitude went back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.pressPoint) and back up above it repeatedly for [`tapCount`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.tapCount) times.|
|[`canceled`](xref:UnityEngine.InputSystem.InputAction.canceled)|- After going back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.pressPoint), Control magnitude did not go back above [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.pressPoint) within [`tapDelay`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.tapDelay) time (that is, taps were spaced out too far apart).<br>or<br>- After going back above [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.pressPoint), Control magnitude did not go back below [`pressPoint`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.pressPoint) within [`tapTime`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction.tapTime) time (that is, taps were too long).|

## Writing custom Interactions

You can also write a custom Interaction to use in your project. You can use custom Interactions in the UI and code the same way you use built-in Interactions.

Add a class implementing the [`IInputInteraction`](xref:UnityEngine.InputSystem.IInputInteraction) interface, like this:

```CSharp
// Interaction which performs when you quickly move an
// axis all the way from extreme to the other.
public class MyWiggleInteraction : IInputInteraction
{
    public float duration = 0.2;

    void Process(ref InputInteractionContext context)
    {
        if (context.timerHasExpired)
        {
            context.Canceled();
            return;
        }

        switch (context.phase)
        {
            case InputActionPhase.Waiting:
                if (context.Control.ReadValue<float>() == 1)
                {
                    context.Started();
                    context.SetTimeout(duration);
                }
                break;

            case InputActionPhase.Started:
                if (context.Control.ReadValue<float>() == -1)
                    context.Performed();
                break;
        }
    }

    // Unlike processors, Interactions can be stateful, meaning that you can keep a
    // local state that mutates over time as input is received. The system might
    // invoke the Reset() method to ask Interactions to reset to the local state
    // at certain points.
    void Reset()
    {
    }
}
```

Register your interaction with the Input System in your initialization code:

```CSharp
InputSystem.RegisterInteraction<MyWiggleInteraction>();
```

Your new Interaction is now available in the [Input Action Asset Editor window](xref:input-system-action-assets).

You can also add it in code using this call:

```CSharp
var Action = new InputAction(Interactions: "MyWiggle(duration=0.5)");
```
