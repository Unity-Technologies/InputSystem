# Interactions

* [Operation](#operation)
    * [Multiple Controls on an Action](#multiple-controls-on-an-action)
    * [Multiple Interactios on a Binding](#multiple-interactions-on-a-binding)
* [Predefined Interactions](#predefined-interactions)
    * [Default Interaction](#default-interaction)
    * [Press](#press)
    * [Hold](#hold)
    * [Tap](#tap)
    * [SlowTap](#slowtap)
    * [MultiTap](#multitap)
* [Custom Interactions](#writing-custom-interactions)

An interaction represents a specific input pattern. For example, a ["hold"](#hold) is an interaction that requires a control to be held for at least a minimum amount of time.

Interactions drive responses on actions. They are placed on individual bindings but can also be placed on an action as a whole, in which case they are applied to every binding on the action. At runtime, when a particular interaction is completed, it triggers the action.

    ////TODO: Update screenshot
![Interaction Properties](Images/InteractionProperties.png)

## Operation

An interaction has a set of dictinct phases it can go through in response to receiving input.

|Phase|Description|
|-----|-----------|
|`Waiting`|The interaction is waiting for input.|
|`Started`|The interaction has been started (i.e. some input has been received) but has not been completed yet.|
|`Performed`|The interaction has been completed.|
|`Cancelled`|The interaction has been interrupted and aborted. For example, this can happen with a "Hold" if a button is released before a full "Hold" is achieved.|

Note that not every interaction supports every phase and that the pattern in which the phases are triggered from a specific interaction depends on the interaction.

While `Performed` will generally be the phase that triggers the actual response to an interaction, `Started` and `Cancelled` can be very useful for providing UI feedback while the interation is in progress. For example, when a "Hold" is `Started`, a radial progress bar can be shown that fills up until the hold time has been reached. If, however, the "Hold" is `Cancelled` before it is complete, the progress bar can be reset to the beginning.

The following example demonstrates this kind of setup with a fire action that can be tapped to fire immediately and held to charge.

```CSharp
var fireAction = new InputAction("fire");
fireAction.AddBinding("<Gamepad>/buttonSouth")
    // Tap fires, slow tap charges. Both act on release.
    .WithInteractions("tap;slowTap");

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

fireAction.cancelled +=
    _ => HideChargingUI();
```

### Multiple Controls on an Action

### Multiple Interactions on a Binding

## Predefined Interactions

The following table shows all the interactions that are registerd by default. Additional interactions can be added to the system using `InputSystem.RegisterInteraction<T>()`. See ["Writing Custom Interactions"](#writing-custom-interactions) for details.

Some of the interactions behave differently when the action they are associated with through the binding is set to "continuous" mode (see `InputAction.continuous`). This is indicated in the table by a separate "... (continuous)" entry.

|Interaction|Started|Performed|Cancelled|
|-----------|-------|---------|---------|
|[*Default*](#default-interaction)|Control is actuated|Controls changes actuation (also first time, i.e. when `Started` is triggered, too)|Control is no longer actuated|
|[*Press*](#press) (`PressOnly`)|<br>|Control crosses button press threshold; then will not perform again until button is first released again|<br>|
|[*Press*](#press) (`ReleaseOnly`)|Control crosses button press threshold|Control goes back below button press threshold|<br>|
|[*Press*](#press) (`PressAndRelease`)|Control crosses button press threshold|Control crosses button press threshold|Control goes back below button threshold|
|[*Press*](#press) (`PressOnly`; continuous)|<br>|Control crosses button press threshold; then any time the control changes value or at least every frame|<br>|
|[*Press*](#press) (`ReleaseOnly`; continuous)|<br>|No difference to non-continuous mode| |
|[*Press*](#press) (`PressAndRelease`; continuous)|<br>|Control crosses button press threshold; then any time the control changes value or at least every frame|Control goes back below button threshold|
|[*Hold*](#hold)|Control actuated beyond button press point|Held for >= `duration`|Control goes back below button press point before `duration`|
|[*Hold*](#hold) (continuous)|Control actuated beyond button press point|Held for >= `duration`; after that, every frame regardless of whether the bound control receives input in the frame or not.|Released before `duration`|
|[*Tap*](#tap)|Control actuated beyond button press point|Control released below button press point within `duration` (defaults to `InputSettings.defaultTapTime`) seconds|Control held for longer than `duration` seconds|
|[*SlowTap*](#slowtap)|Control actuated beyond button press point|Control released below button press point within `duration` (defaults to `InputSettings.defaultSlowTapTime`) seconds|Control released before `duration` seconds|
|[*MultiTap*](#multitap)|Control actuated beyond button press point (first tap counting against `tapCount`)|Control went back below button press point and back up above it repeatedly until `tapCount` had been reached|After going back below button press point, control did not go back above button press point within `tapDelay` time (i.e. taps were spaced out too far apart)|

### Default Interaction

If no interaction has specifically been added to a binding or its action, then the default interaction applies to the binding. It is designed to represent a "generic" interaction with an input control.

If [pass-through](Actions.md#passthrough-actions) is __disabled__, the behavior is as follows:

1. As soon as a bound control becomes [actuated](Controls.md#control-actuated), the action goes from `Waiting` to `Started` and then immediately to `Performed` and back to `Started` (i.e. you will see on callback on `InputAction.started` followed by one callback on `InputAction.performed`).
2. For as long as the bound control remains actuated, the action stays in `Started` and will trigger `Performed` whenever the value of the control changes (i.e. you will see one call to `InputAction.performed`).
3. When the bound control stops being actuated, the action goes to `Cancelled` and then back to `Waiting` (i.e. you will see one call to `InputAction.cancelled`).

If [pass-through](Actions.md#passthrough-actions) is __enabled__, the TODO

### `Press`

A `Press` interaction can be used to explicitly force button-like interaction.

Note that the `Press` interaction operates on control actuation, not on control values directly. This means that the press threshold will be evaluated against the magnitude of the control actuation. This means that it is possible to use

### `Hold`

A `Hold` requires a control to be held for a set duration before the action is triggered. The duration can either be set explicitly on the action or be left at default (`0`) in which case the default hold time setting applies (`InputSettings.defaultHoldTime`).

```
    // Create an action with a .3 second hold on the A button of the gamepad.
    var action = new InputAction();
    action.AddBinding("<Gamepad>/buttonSouth").WithInteraction("Hold(duration=0.3");
```

### `Tap`

### `SlowTap`

### `MultiTap`


## Writing Custom Interactions

The set of interactions is freely extensible. Newly added interactions are usable in the UI and data the same way that built-in interations are.

To implement

>NOTE: Interactions cannot currently orchestrate input between several actions and/or bindings. They are at this point restricted to operating on a single binding and the data that flows in through it.

Unlike processors, interations can be stateful, meaning that it is permissible to keep local state that mutates over time as input is received. The system may ask interactions to reset such state at certain points by invoking the `Reset()` method.

### Interaction Parameters
