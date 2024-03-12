
# Responding to Actions

There are two main techniques you can use to respond to Actions in your project. These are to either use **polling** or an **event-driven** approach.

- The **Polling** approach refers to the technique of repeatedly checking the current state of the Actions you are interested in. Typically you would do this in the `Update()` method of a `MonoBehaviour` script.
- The **Event-driven** approach involves creating your own methods in code that are automatically called when an action is performed.

For most common scenarios, especially action games where the user's input should have a continuous effect on an in-game character, **Polling** is usually simpler and easier to implement.

For other situations where input is less frequent and directed to various different GameObjects in your scene, an event-driven approach might be more suitable.


## Polling Actions

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

This example uses the Interact action from the [default actions](./ProjectWideActions.md#the-default-actions), which has a [Hold](Interactions.md#hold) interaction to make it perform only after the bound control is held for a period of time (for example, 0.4 seconds):

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

This example uses three actions called Shield, Teleport and Submit (which are not included in the [default actions](./ProjectWideActions.md#the-default-actions)):

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



## Responding to Actions using callbacks

When you set up callbacks for your Action, the Action informs your code that a certain type of input has occurred, and your code can then respond accordingly.

There are several ways to do this:

1. You can use the [PlayerInput component](Workflow-PlayerInput.md) to set up callbacks in the inspector.
1. Each Action has a [`started`, `performed`, and `canceled` callback](#action-callbacks).
1. Each Action Map has an [`actionTriggered` callback](#inputactionmapactiontriggered-callback).
1. The Input System has a global [`InputSystem.onActionChange` callback](#inputsystemonactionchange-callback).
2. [`InputActionTrace`](#inputactiontrace) can record changes happening on Actions.

#### The PlayerInput component

The PlayerInput component is the simplest way to set up Action callbacks. It provides an interface in the inspector that allows you set up callbacks directly to your methods without requiring intermediate code. [Read more about the PlayerInput component](Workflow-PlayerInput.md).

Alternatively, you can implement callbacks entirely from your own code using the following workflow:


#### Action callbacks

Every Action has a set of distinct phases it can go through in response to receiving input.

|Phase|Description|
|-----|-----------|
|`Disabled`|The Action is disabled and can't receive input.|
|`Waiting`|The Action is enabled and is actively waiting for input.|
|`Started`|The Input System has received input that started an Interaction with the Action.|
|`Performed`|An Interaction with the Action has been completed.|
|`Canceled`|An Interaction with the Action has been canceled.|

You can read the current phase of an action using [`InputAction.phase`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_phase).

The `Started`, `Performed`, and `Canceled` phases each have a callback associated with them:

```CSharp
    var action = new InputAction();

    action.started += context => /* Action was started */;
    action.performed += context => /* Action was performed */;
    action.canceled += context => /* Action was canceled */;
```

Each callback receives an [`InputAction.CallbackContext`](../api/UnityEngine.InputSystem.InputAction.CallbackContext.html) structure, which holds context information that you can use to query the current state of the Action and to read out values from Controls that triggered the Action ([`InputAction.CallbackContext.ReadValue`](../api/UnityEngine.InputSystem.InputAction.CallbackContext.html#UnityEngine_InputSystem_InputAction_CallbackContext_ReadValue__1)).

>__Note__: The contents of the structure are only valid for the duration of the callback. In particular, it isn't safe to store the received context and later access its properties from outside the callback.

When and how the callbacks are triggered depends on the [Interactions](Interactions.md) present on the respective Bindings. If the Bindings have no Interactions that apply to them, the [default Interaction](Interactions.md#default-interaction) applies.

##### `InputActionMap.actionTriggered` callback

Instead of listening to individual actions, you can listen on an entire Action Map for state changes on any of the Actions in the Action Map.

```CSharp
var actionMap = new InputActionMap();
actionMap.AddAction("action1", "<Gamepad>/buttonSouth");
actionMap.AddAction("action2", "<Gamepad>/buttonNorth");

actionMap.actionTriggered +=
    context => { ... };
```

The argument received is the same `InputAction.CallbackContext` structure that you receive through the [`started`, `performed`, and `canceled` callbacks](#action-callbacks).

>__Note__: The Input System calls `InputActionMap.actionTriggered` for all three of the individual callbacks on Actions. That is, you get `started`, `performed`, and `canceled` all on a single callback.

##### `InputSystem.onActionChange` callback

Similar to `InputSystem.onDeviceChange`, your app can listen for any action-related change globally.

```CSharp
InputSystem.onActionChange +=
    (obj, change) =>
    {
        // obj can be either an InputAction or an InputActionMap
        // depending on the specific change.
        switch (change)
        {
            case InputActionChange.ActionStarted:
            case InputActionChange.ActionPerformed:
            case InputActionChange.ActionCanceled:
                Debug.Log($"{((InputAction)obj).name} {change}");
                break;
        }
    }
```


#### `InputActionTrace`

You can trace Actions to generate a log of all activity that happened on a particular set of Actions. To do so, use [`InputActionTrace`](../api/UnityEngine.InputSystem.Utilities.InputActionTrace.html). This behaves in a similar way to [`InputEventTrace`](../api/UnityEngine.InputSystem.LowLevel.InputEventTrace.html) for events.

>__Note__: `InputActionTrace` allocates unmanaged memory and needs to be disposed of so that it doesn't create memory leaks.

```CSharp
var trace = new InputActionTrace();

// Subscribe trace to single Action.
// (Use UnsubscribeFrom to unsubscribe)
trace.SubscribeTo(myAction);

// Subscribe trace to entire Action Map.
// (Use UnsubscribeFrom to unsubscribe)
trace.SubscribeTo(myActionMap);

// Subscribe trace to all Actions in the system.
trace.SubscribeToAll();

// Record a single triggering of an Action.
myAction.performed +=
    ctx =>
    {
        if (ctx.ReadValue<float>() > 0.5f)
            trace.RecordAction(ctx);
    };

// Output trace to console.
Debug.Log(string.Join(",\n", trace));

// Walk through all recorded Actions and then clear trace.
foreach (var record in trace)
{
    Debug.Log($"{record.action} was {record.phase} by control {record.control}");

    // To read out the value, you either have to know the value type or read the
    // value out as a generic byte buffer. Here, we assume that the value type is
    // float.

    Debug.Log("Value: " + record.ReadValue<float>());

    // If it's okay to accept a GC hit, you can also read out values as objects.
    // In this case, you don't have to know the value type.

    Debug.Log("Value: " + record.ReadValueAsObject());
}
trace.Clear();

// Unsubscribe trace from everything.
trace.UnsubscribeFromAll();

// Release memory held by trace.
trace.Dispose();
```

Once recorded, a trace can be safely read from multiple threads as long as it is not concurrently being written to and as long as the Action setup (that is, the configuration data accessed by the trace) is not concurrently being changed on the main thread.

### Action types

Each Action can be one of three different [Action types](../api/UnityEngine.InputSystem.InputActionType.html). You can select the Action type in the Input Action editor window, or by specifying the `type` parameter when calling the [`InputAction()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction__ctor_System_String_UnityEngine_InputSystem_InputActionType_System_String_System_String_System_String_System_String_) constructor. The Action type influences how the Input System processes state changes for the Action. The default Action type is `Value`.

#### Value

This is the default Action type. Use this for any inputs which should track continuous changes to the state of a Control.

 [`Value`](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_Value) type actions continuously monitor all the Controls which are bound to the Action, and then choose the one which is the most actuated to be the Control driving the Action, and report the values from that Control in callbacks, triggered whenever the value changes. If a different bound Control actuated more, then that Control becomes the Control driving the Action, and the Action starts reporting values from that Control. This process is called [conflict resolution](ActionBindings.md#conflicting-inputs). This is useful if you want to allow different Controls to control an Action in the game, but only take input from one Control at the same time.

When the Action initially enables, it performs an [initial state check](ActionBindings.md#initial-state-check) of all bound Controls. If any of them is actuated, the Action then triggers a callback with the current value.

#### Button

This is very similar to [`Value`](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_Value), but [`Button`](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_Button) type Actions can only be bound to [`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html) Controls, and don't perform an initial state check like [`Value`](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_Value) Actions do (see the Value section above). Use this for inputs that trigger an Action once every time they are pressed. The initial state check is usually not useful in such cases, because it can trigger actions if the button is still held down from a previous press when the Action was enabled.

#### Pass-Through

 [`Pass-Through`](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_PassThrough) Actions bypass the [conflict resolution](ActionBindings.md#conflicting-inputs) process described above for `Value` Actions and don't use the concept of a specific Control driving the Action. Instead, any change to any bound Control triggers a callback with that Control's value. This is useful if you want to process all input from a set of Controls.

### Debugging Actions

To see currently enabled Actions and their bound Controls, use the [Input Debugger](Debugging.md#debugging-actions).

You can also use the [`InputActionVisualizer`](Debugging.md#inputactionvisualizer) component from the Visualizers sample to get an on-screen visualization of an Action's value and Interaction state in real-time.

### Using Actions with multiple players

You can use the same Action definitions for multiple local players (for example, in a local co-op game). For more information, see documentation on the [Player Input Manager](PlayerInputManager.md) component.
