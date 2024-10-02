## How interactions work

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

If you have multiple Controls bound to a Binding or an Action which has an Interaction, then the Input System first applies the [Control conflict resolution](ActionBindings.md#conflicting-inputs) logic to get a single value for the Action, which it then feeds to the Interaction logic. Any of the bound Controls can perform the Interaction.

### Multiple Interactions on a Binding

If multiple Interactions are present on a single Binding or Action, then the Input System checks the Interactions in the order they are present on the Binding. The code example [above](#operation) illustrates this example. The Binding on the `fireAction` Action has two Interactions: `WithInteractions("tap;slowTap")`. The [tap](#tap) Interaction gets a first chance at interpreting the input from the Action. If the button is pressed, the Action calls the `Started` callback on the tap Interaction. If the user keeps holding the button, the tap Interaction times out, and the Action calls the [`Canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled) callback for the tap Interaction and starts processing the [slow tap](#slowtap) Interaction (which now receives a `Started` callback).

At any one time, only one Interaction can be "driving" the action (that is, it gets to determine the action's current [`phase`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_phase)). If an Interaction higher up in the stack cancels, Interactions lower down in the stack can take over.

Note that the order of interactions can affect which interaction is passed to your callback function. For example, an action with [Tap](#tap), [MultiTap](#multitap) and [Hold](#hold) interactions will have different behaviour when the interactions are in a different order, such as [Hold](#hold), [MultiTap](#multitap) and [Tap](#tap). If you get unexpected behaviour, you may need to experiment with a different ordering.

### Timeouts

Interactions might need to wait a certain time for a specific input to occur or to not occur. An example of this is the [Hold](#hold) interaction which, after a button is pressed, has to wait for a set [duration](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_duration) until the "hold" is complete. To do this, an interaction installs a timeout using [`SetTimeout`](../api/UnityEngine.InputSystem.InputInteractionContext.html#UnityEngine_InputSystem_InputInteractionContext_SetTimeout_System_Single_).

It can be useful to know how much of a timeout is left for an interaction to complete. For example, you might want to display a bar in the UI that is charging up while the interaction is waiting to complete. To query the percentage to which a timeout has completed, use [`GetTimeoutCompletionPercentage`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_GetTimeoutCompletionPercentage_).

```CSharp
// Returns a value between 0 (inclusive) and 1 (inclusive).
var warpActionCompletion = playerInput.actions["warp"].GetTimeoutCompletionPercentage();
```

Note that each Interaction can have its own separate timeout (but only a single one at any one time). If [multiple interactions](#multiple-interactions-on-a-binding) are in effect, then [`GetTimeoutCompletionPercentage`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_GetTimeoutCompletionPercentage_) will only use the timeout of the one interaction that is currently driving the action.

Some Interactions might involve multiple timeouts in succession. In this case, knowing only the completion of the currently running timeout (if any) is often not useful. An example is [`MultiTapInteraction`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html), which involves a timeout on each individual tap, as well as a timeout in-between taps. The Interaction is complete only after a full tap sequence has been performed.

An Interaction can use [`SetTotalTimeoutCompletionTime`](../api/UnityEngine.InputSystem.InputInteractionContext.html#UnityEngine_InputSystem_InputInteractionContext_SetTotalTimeoutCompletionTime_System_Single_) to inform the Input System of the total time it will run timeouts for.
