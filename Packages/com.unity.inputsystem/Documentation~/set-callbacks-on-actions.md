---
uid: input-system-set-callbacks-actions
---

# Set callbacks on actions

Setting callbacks on actions is one of the two main [ways to respond to actions](about-responding-to-input.md) using the recommended workflow.

When you set up callbacks for your Action, the Action informs your code that a certain type of input has occurred, and your code can then respond accordingly.

The Input System offers the following ways to set up input callbacks:

- The [PlayerInput component](using-playerinput-workflow.md).
- [Action callbacks](#action-callbacks).
- [Action map callbacks](#action-map-callbacks)
- The global [On Action Change callback](#global-input-callback).


You can also use [`InputActionTrace`](trace-actions.md) to record all changes happening on actions, which is useful for [debugging](debugging.md).

## The Player Input component

With the Player Input component, you can set up callbacks using an interface in the inspector without requiring intermediate code. Refer to [the PlayerInput component](using-playerinput-workflow.md) for further information.


## Action callbacks

Every Action has a set of distinct phases it can go through in response to receiving input.

|Phase|Description|
|-----|-----------|
|`Disabled`|The Action is disabled and can't receive input.|
|`Waiting`|The Action is enabled and is actively waiting for input.|
|`Started`|The Input System has received input that started an Interaction with the Action.|
|`Performed`|An Interaction with the Action has been completed.|
|`Canceled`|An Interaction with the Action has been canceled.|

You can read the current phase of an action using [`InputAction.phase`](xref:UnityEngine.InputSystem.InputAction).

The `Started`, `Performed`, and `Canceled` phases each have a callback associated with them:

```CSharp
jumpAction = InputSystem.actions.FindAction("Jump");

jumpAction.started += context => /* Action was started */;
jumpAction.performed += context => /* Action was performed */;
jumpAction.canceled += context => /* Action was canceled */;
```

Each callback receives an [`InputAction.CallbackContext`](xref:UnityEngine.InputSystem.InputAction.CallbackContext) structure, which holds context information that you can use to query the current state of the Action and to read out values from Controls that triggered the Action ([`InputAction.CallbackContext.ReadValue`](xref:UnityEngine.InputSystem.InputAction.CallbackContext)).

The contents of the callback context structure are only valid during the callback. In particular, it's not safe to store the received context and later access its properties from outside the callback.

When and how the callbacks are triggered depends on the [interactions](Interactions.md) present on the respective bindings. If the bindings have no interactions that apply to them, the [default interaction](default-interactions.md) applies.

## Action map callbacks

Instead of listening to individual actions, you can listen to an entire action map for state changes on any action it contains.

```CSharp
playerActionMap = InputSystem.actions.FindActionMap("Player");


playerActionMap.actionTriggered +=
    context => { ... };
```

The argument received is the same `InputAction.CallbackContext` structure that you receive through the [`started`, `performed`, and `canceled` callbacks](#action-callbacks).

> [!NOTE]
> The Input System calls `InputActionMap.actionTriggered` for all three of the individual callbacks on Actions. That is, you get `started`, `performed`, and `canceled` all on a single callback.

## Global input callback

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
