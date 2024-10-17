
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

