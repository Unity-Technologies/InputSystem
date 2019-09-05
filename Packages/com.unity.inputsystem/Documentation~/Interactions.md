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

![Interaction Properties](Images/InteractionProperties.png)

## Operation

An interaction has a set of dictinct phases it can go through in response to receiving input.

|Phase|Description|
|-----|-----------|
|`Waiting`|The interaction is waiting for input.|
|`Started`|The interaction has been started (i.e. some input has been received) but has not been completed yet.|
|`Performed`|The interaction has been completed.|
|`Canceled`|The interaction has been interrupted and aborted. For example, this can happen with a "Hold" if a button is released before a full "Hold" is achieved.|

Note that not every interaction supports every phase and that the pattern in which the phases are triggered from a specific interaction depends on the interaction.

While `Performed` will generally be the phase that triggers the actual response to an interaction, `Started` and `Canceled` can be very useful for providing UI feedback while the interation is in progress. For example, when a "Hold" is `Started`, a radial progress bar can be shown that fills up until the hold time has been reached. If, however, the "Hold" is `Canceled` before it is complete, the progress bar can be reset to the beginning.

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

fireAction.canceled +=
    _ => HideChargingUI();
```

### Multiple Controls on an Action

If you have multiple controls bound to a binding or an action which has an interaction then the input system will first apply the [control disambiguation](ActionBindings.md#disambiguation) logic to get a single value for the action, which is then fed to the interaction logic. Any of the bound controls can perform the interaction.

### Multiple Interactions on a Binding

If multiple interactions are present on a single binding or action, then the interactions will be checked in the order they are present on the binding. We have such a case in the code example [above](#operation) - the binding on the `fireAction` action has two interactions: `WithInteractions("tap;slowTap")`. Now, the Tap interaction gets a first chance at interpreting the input from the action. If the button is pressed, the  action will call  the `started` callback on the Tap interaction. Now, if we keep holding the button, the Tap interaction will time out, and the action will call the [`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled) callback for the Tap interaction, and then start processing the SlowTap interaction (which will get a `started` callback now).

## Using Interactions

Interactions can be installed on [bindings](ActionBindings.md) or [actions](Actions.md).

### Interactions on Bindings

When you create bindings for your [actions](Actions.md), you can choose to add interactions to the bindings.

If you are using [Input Action Assets](ActionAssets.md), you can simply add any interaction to your bindings in the input action editor. Once you [created some bindings](ActionAssets.md#editing-bindings), just select the binding you want to add interactions to (so that the right pane of the window shows the properties for that binding). Now, click on the "Plus" icon on the "Interactions" foldout, which will show a popup of all available interactions. Choose an interaction type to add an interaction instance of that type. The interaction will now be shown under the interactions foldout. If the interaction has any parameters you can now edit them here as well:

![Binding Processors](Images/BindingProcessors.png)

You can click the "Minus" button next to a interaction to remove it. You can click the "up" and "down" arrows to change the [order of interactions](#multiple-interactions-on-a-binding).

If you create your bindings in code, you can add interaction like this:

```CSharp
var action = new InputAction();
action.AddBinding("<Gamepad>/leftStick")
    .WithInteractions("tap(duration=0.8)");
```

### Interactions on Actions

Interactions on actions work very similar to interactions on bindings, but they affect all controls bound to an action, not just the ones coming from a specific binding. If there are interactions on both the binding and the action, the ones from the binding will be processes first.

You can add and edit interactions on actions in the [Input Action Assets](ActionAssets.md) the [same way](#interactions-on-bindings) as you would do for bindings - just add them in the right window pane when you have selected an action to edit.

If you create your actions in code, you can add interactions like this:

```CSharp
var action = new InputAction(interactions: "tap(duration=0.8)");
```

## Predefined Interactions

The input system package comes with a set of useful interactions you can use. If no interaction is set an an action, the system will use the [default interaction](#default-interaction).

Note that the built-in interaction operate on control actuation, not on control values directly. This means that the `pressPoint` parameters will be evaluated against the magnitude of the control actuation. This means that it is possible to use these interactions on any control which has a magnitude (like sticks) and not just on buttons.

### Default Interaction

If no interaction has specifically been added to a binding or its action, then the default interaction applies to the binding. It is designed to represent a "generic" interaction with an input control.

For [`Value`](Actions.md#value) or [`Button`](Actions.md#button) type actions, the behavior is as follows:

1. As soon as a bound control becomes [actuated](Controls.md#control-actuation), the action goes from `Waiting` to `Started` and then immediately to `Performed` and back to `Started` (i.e. you will see on callback on [`InputAction.started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started) followed by one callback on [`InputAction.performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)).
2. For as long as the bound control remains actuated, the action stays in `Started` and will trigger `Performed` whenever the value of the control changes (i.e. you will see one call to [`InputAction.performed`])(../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed).
3. When the bound control stops being actuated, the action goes to `Canceled` and then back to `Waiting` (i.e. you will see one call to [`InputAction.canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)).

For [`PassThrough`](Actions.md#pass-through) type actions, the behavior is simpler. The input system will not try to track any interaction state (which would be meaningless if tracking several controls separately), but simply trigger a `Performed` callback for each value change.

|__Callbacks__/[`InputAction.type`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_type)|[`Value`](Actions.md#value) or [`Button`](Actions.md#button)|[`PassThrough`](Actions.md#pass-through)|
|---|-----------|-------------|-----------------|
|[`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started)|Control is actuated|not used|
|[`performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)|Controls changes actuation (also first time, i.e. when [`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started) is triggered, too)|Control changes value|
|[`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)|Control is no longer actuated|not used|

### Press

A [`PressInteraction`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html) can be used to explicitly force button-like interaction. The [`behavior`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_behavior) parameter let's you select if the interaction should trigger on button press, release or both.

|__Parameters__|Type|Default value|
|---|---|---|
|[`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint)|
|[`behavior`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_behavior)|[`PressBehavior`](../api/UnityEngine.InputSystem.Interactions.PressBehavior.html)|`PressOnly`|


|__Callbacks__/[`behavior`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_behavior)|`PressOnly`|`ReleaseOnly`|`PressAndRelease`|
|---|-----------|-------------|-----------------|
|[`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|
|[`performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|Control magnitude goes back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|* Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)<br>or<br>* Control magnitude goes back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.PressInteraction.html#UnityEngine_InputSystem_Interactions_PressInteraction_pressPoint)|
|[`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)|not used|not used|not used|

### Hold

A [`HoldInteraction`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html) requires a control to be held for [`duration`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_duration) seconds before the action is triggered.

|__Parameters__|Type|Default value|
|---|---|---|
|[`duration`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_duration)|`float`|[`InputSettings.defaultHoldTime`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultHoldTime)|
|[`pressPoint`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint)|

|__Callbacks__||
|---|---|
|[`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_pressPoint)|
|[`performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)|Control magnitude held above [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_pressPoint) for >= [`duration`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_duration)|
|[`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)|Control magnitude goes back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_pressPoint) before [`duration`](../api/UnityEngine.InputSystem.Interactions.HoldInteraction.html#UnityEngine_InputSystem_Interactions_HoldInteraction_duration) (i.e. button was not held long enough)|

### Tap

A [`TapInteraction`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html) requires a control to be pressed and released within [`duration`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_duration) seconds to trigger the action.

|__Parameters__|Type|Default value|
|---|---|---|
|[`duration`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_duration)|`float`|[`InputSettings.defaultTapTime`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultTapTime)|
|[`pressPoint`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint)|

|__Callbacks__||
|---|---|
|[`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_pressPoint)|
|[`performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)|Control magnitude goes back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_pressPoint) before [`duration`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_duration)|
|[`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)|Control magnitude held above [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_pressPoint) for >= [`duration`](../api/UnityEngine.InputSystem.Interactions.TapInteraction.html#UnityEngine_InputSystem_Interactions_TapInteraction_duration) (i.e. tap was too slow)|

### SlowTap

A [`SlowTapInteraction`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html) requires a control to be pressed and, held for a minimum duration of [`duration`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_duration) seconds, and then released to trigger the action.

|__Parameters__|Type|Default value|
|---|---|---|
|[`duration`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_duration)|`float`|[`InputSettings.defaultSlowTapTime`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultSlowTapTime)|
|[`pressPoint`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint)|

|__Callbacks__||
|---|---|
|[`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_pressPoint)|
|[`performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)|Control magnitude goes back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_pressPoint) after [`duration`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_duration)|
|[`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)|Control magnitude goes back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_pressPoint) before [`duration`](../api/UnityEngine.InputSystem.Interactions.SlowTapInteraction.html#UnityEngine_InputSystem_Interactions_SlowTapInteraction_duration) (i.e. tap was too fast)|

### MultiTap

A [`MultiTapInteraction`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html) requires a control to be pressed and released within [`tapTime`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapTime) seconds [`tapCount`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapCount) times, with no more then [`tapDelay`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapDelay) seconds passing between taps for the interaction to trigger. This can be used to detect double-click or multi-click gestures for instance.

|__Parameters__|Type|Default value|
|---|---|---|
|[`tapTime`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapTime)|`float`|[`InputSettings.defaultTapTime`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultTapTime)|
|[`tapDelay`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapDelay)|`float`|2 * [`tapTime`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapTime)|
|[`tapCount`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapCount)|`int`|2|
|[`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint)|`float`|[`InputSettings.defaultButtonPressPoint`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultButtonPressPoint)|

|__Callbacks__||
|---|---|
|[`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started)|Control magnitude crosses [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint)|
|[`performed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_performed)|Control magnitude went back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint) and back up above it repeatedly for [`tapCount`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapCount) times|
|[`canceled`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_canceled)|* After going back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint), control magnitude did not go back above [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint) within [`tapDelay`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapDelay) time (i.e. taps were spaced out too far apart)<br>or<br>* After going back above [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint), control magnitude did not go back below [`pressPoint`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_pressPoint) within [`tapTime`](../api/UnityEngine.InputSystem.Interactions.MultiTapInteraction.html#UnityEngine_InputSystem_Interactions_MultiTapInteraction_tapTime) time (i.e. taps were too long)|

## Writing Custom Interactions

You can also write a custom interaction to use in your project. Newly added interactions are usable in the UI and data the same way that built-in interactions are. Simply add a class implementing the [`IInputInteraction`](../api/UnityEngine.InputSystem.IInputInteraction.html), interface [`Process`]() and [`Reset`]() methods:

```CSharp
// In the spirit of classic joystick-destroying C64 sport simulation games
// like "Winter Games", here's an interaction which performs when you move an
// axis all the way from end to end fast enough.
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
                if (context.control.ReadValue<float>() == 1)
                {
                    context.Started();
                    context.SetTimeout(duration);
                }
                break;

            case InputActionPhase.Started:
                if (context.control.ReadValue<float>() == -1)
                    context.Performed();
                break;
        }
    }

    // Unlike processors, interations can be stateful, meaning that it is permissible
    // to keep local state that mutates over time as input is received. The system may
    // ask interactions to reset such state at certain points by invoking the `Reset()`
    // method.
    void Reset()
    {
    }
}
```

Now, you need to tell the Input System about your interaction. So, somewhere in your initialization code, you should call:

```CSharp
InputSystem.RegisterInteraction<MyWiggleInteraction>();
```

Now, your new interaction will become available up in the [Input Action Asset Editor window](ActionAssets.md), and you can also add it in code like this:

```CSharp
var action = new InputAction(interactions: "MyWiggle(duration=0.5)");
```
