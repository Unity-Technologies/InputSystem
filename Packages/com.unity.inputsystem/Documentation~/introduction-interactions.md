---
uid: input-system-interactions-intro
---

# Introduction to interactions

An Interaction represents a specific pattern of [control actuation](control-actuation.md) that determines how an action is started, performed, or canceled.

An action with no explicit interaction applied behaves according the [default interaction](default-interactions.md) for its [action type](about-action-control-types.md). For example, a button action's default interaction is to immediately perform the action when the button is pressed.

When you apply an interaction to an action, it overrides the default interaction behavior and changes how the action is performed. This allows you, for example, to implement a [hold](built-in-interactions.md#hold) interaction that requires a control to be held for a minimum amount of time, or a [multi-tap](built-in-interactions.md#multitap) interaction that requires the control to be quickly tapped multiple times to perform the action.

Interactions trigger responses on actions. You can place them on individual bindings, or on actions, in which case they apply to every binding on the action. At runtime, when a particular interaction completes, this triggers the action.

![The Binding Path displays the buttonSouth [Gamepad] value set on the Interaction Properties window.](Images/InteractionProperties.png)

## How interactions work

An Interaction has a set of distinct phases it can go through in response to receiving input.

|Phase|Description|
|-----|-----------|
|`Waiting`|The Interaction is waiting for input.|
|`Started`|The Interaction has started (that is, it received some of its expected input), but is not complete yet.|
|`Performed`|The Interaction is complete.|
|`Canceled`|The Interaction was interrupted and aborted. For example, the user pressed and then released a button before the minimum time required for a Hold Interaction to complete.|

Not every Interaction triggers every phase, and the pattern in which specific Interactions trigger phases depends on the Interaction type.

While `Performed` is typically the phase that triggers the actual response to an Interaction, `Started` and `Canceled` can be  useful for providing UI feedback while the Interaction is in progress. For example, when a [hold](built-in-interactions.md#hold) is `Started`, the app can display a progress bar that fills up until the hold time has been reached. If, however, the hold is `Canceled` before it completes, the app can reset the progress bar to the beginning.


The following example demonstrates this using a [Slow Tap interaction](./built-in-interactions.md#slowtap) on a `Jump` action so that the user can tap to jump immediately, or hold down the jump button to charge up a higher powered jump, displaying a UI to show the amount charged:

```CSharp
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class ExampleScript : MonoBehaviour
{
    InputAction jumpAction;

    private void Start()
    {
        jumpAction = InputSystem.actions.FindAction("Jump");


        jumpAction.started += context =>
        {
            if (context.interaction is SlowTapInteraction)
            {
                // Show "charging" UI
            }
        };

        jumpAction.performed += context =>
        {
            if (context.interaction is SlowTapInteraction)
            {
                // call "charged jump" code
            }
            else
            {
                // call "regular jump" code
            };
        };

        jumpAction.canceled += context =>
        {
            // Hide "charging" UI
        };

    }
}
```

## Multiple Controls on an Action

If you have multiple Controls bound to a binding or an Action which has an Interaction, then the Input System first applies the [Control conflict resolution](binding-conflicts.md) logic to get a single value for the Action, which it then feeds to the Interaction logic. Any of the bound Controls can perform the Interaction.

## Multiple Interactions on a binding

If multiple Interactions are present on a single binding or Action, then the Input System checks the Interactions in the order they are present on the binding. The code example above illustrates this example. The binding on the `fireAction` Action has two Interactions: `WithInteractions("tap;slowTap")`. The [tap](built-in-interactions.md#tap) Interaction gets a first chance at interpreting the input from the Action. If the button is pressed, the Action calls the `Started` callback on the tap Interaction. If the user keeps holding the button, the tap Interaction times out, and the Action calls the [`Canceled`](xref:UnityEngine.InputSystem.InputAction) callback for the tap Interaction and starts processing the [slow tap](built-in-interactions.md#slowtap) Interaction (which now receives a `Started` callback).

At any one time, only one Interaction can be "driving" the action (that is, it gets to determine the action's current [`phase`](xref:UnityEngine.InputSystem.InputAction)). If an Interaction higher up in the stack cancels, Interactions lower down in the stack can take over.

Note that the order of interactions can affect which interaction is passed to your callback function. For example, an action with [Tap](built-in-interactions.md#tap), [MultiTap](built-in-interactions.md#multitap) and [Hold](built-in-interactions.md#hold) interactions will have different behaviour when the interactions are in a different order, such as [Hold](built-in-interactions.md#hold), [MultiTap](built-in-interactions.md#multitap) and [Tap](built-in-interactions.md#tap). If you get unexpected behaviour, you may need to experiment with a different ordering.

## Timeouts

Interactions might need to wait a certain time for a specific input to occur or to not occur. An example of this is the [Hold](built-in-interactions.md#hold) interaction which, after a button is pressed, has to wait for a set [duration](xref:UnityEngine.InputSystem.Interactions.HoldInteraction) until the "hold" is complete. To do this, an interaction installs a timeout using [`SetTimeout`](xref:UnityEngine.InputSystem.InputInteractionContext).

It can be useful to know how much of a timeout is left for an interaction to complete. For example, you might want to display a bar in the UI that is charging up while the interaction is waiting to complete. To query the percentage to which a timeout has completed, use [`GetTimeoutCompletionPercentage`](xref:UnityEngine.InputSystem.InputAction).

```CSharp
// Returns a value between 0 (inclusive) and 1 (inclusive).
var warpActionCompletion = playerInput.actions["warp"].GetTimeoutCompletionPercentage();
```

Note that each Interaction can have its own separate timeout (but only a single one at any one time). If [multiple interactions](#multiple-interactions-on-a-binding) are in effect, then [`GetTimeoutCompletionPercentage`](xref:UnityEngine.InputSystem.InputAction) will only use the timeout of the one interaction that is currently driving the action.

Some Interactions might involve multiple timeouts in succession. In this case, knowing only the completion of the currently running timeout (if any) is often not useful. An example is [`MultiTapInteraction`](xref:UnityEngine.InputSystem.Interactions.MultiTapInteraction), which involves a timeout on each individual tap, as well as a timeout in-between taps. The Interaction is complete only after a full tap sequence has been performed.

An Interaction can use [`SetTotalTimeoutCompletionTime`](xref:UnityEngine.InputSystem.InputInteractionContext) to inform the Input System of the total time it will run timeouts for.
