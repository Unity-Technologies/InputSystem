---
uid: input-system-configure-unity-events
---

# Configure Unity events

You can use the following properties to configure `PlayerInput`:

|Property|Description|
|--------|-----------|
|[`Actions`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_actions)|The set of [Input Actions](actions.md) associated with the player. Typically you would set this to Project-Wide Actions, however you can assign an [ActionAsset](ActionAssets.md) reference here). To receive input, each player must have an associated set of Actions. See documentation on [Actions](#actions) for details.|
|[`Default Control Scheme`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_defaultControlScheme)|Which [Control Scheme](ActionBindings.md#control-schemes) (from what is defined in [`Actions`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_actions)) to enable by default.|
|[`Default Action Map`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_defaultActionMap)|Which [Action Map](actions.md#overview) in [`Actions`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_actions) to enable by default. If set to `None`, then the player starts with no Actions being enabled.|
|[`Camera`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_camera)|The individual camera associated with the player. This is only required when employing [split-screen](PlayerInputManager.md#split-screen) setups and has no effect otherwise.|
|[`Behavior`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_notificationBehavior)|How the `PlayerInput` component notifies game code about things that happen with the player. See documentation on [notification behaviors](#notification-behaviors).|

## Actions

To receive input, each player must have an associated set of Input Actions.

### Specifying the Actions to use

The simplest workflow is to use the project-wide actions defined in the [Input Actions editor](ActionsEditor.md). However, the Player Input component also allows you to use an [Actions Asset](ActionAssets.md) to specify the actions that should be used by any instance of the component. If you set the **Actions** field to **Actions Asset**, the inspector displays a field into which you can assign an actions asset, and a **Create Actions** button which allows you to create a new actions asset. When you create these via the Player Input inspector's __Create Actions__ button, the Input System creates a default set of Actions. However, the Player Input component places no restrictions on the arrangement of Actions.

### Enabling and disabling Actions

The Player Input component automatically handles enabling and disabling Actions, and also handles installing [callbacks](RespondingToActions.md#responding-to-actions-using-callbacks) on the Actions. When multiple Player Input components use the same Actions, the components automatically create [private copies of the Actions](RespondingToActions.md#using-actions-with-multiple-players). This is why, when writing input code that works with the PlayerInput component, you should not use `InputSystem.actions` because this references the "singleton" copy of the actions rather than the specific private copy associated with the PlayerInput instance you are coding for.

When first enabled, the Player Input component enables all Actions from the the [`Default Action Map`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_defaultActionMap). If no default Action Map exists, the Player Input component does not enable any Actions. To manually enable Actions, you can call [`Enable`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_Enable) and [`Disable`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_Disable) on the Action Maps or Actions, like you would do [without `PlayerInput`](actions.md). To check which Action Map is currently enabled, or to switch to a different one, use the  [`PlayerInput.currentActionMap`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_currentActionMap) property. To switch Action Maps with an Action Map name, you can also call [`PlayerInput.SwitchCurrentActionMap`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_SwitchCurrentActionMap_System_String_).

To disable a player's input, call [`PlayerInput.DeactivateInput`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_DeactivateInput). To re-enable it, call [`PlayerInput.ActivateInput`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_ActivateInput). The latter enables the default Action Map, if it exists.

When `PlayerInput` is disabled, it automatically disables the currently active Action Map ([`PlayerInput.currentActionMap`](../api/UnityEngine.InputSystem.PlayerInput.html#UnityEngine_InputSystem_PlayerInput_currentActionMap)) and disassociate any Devices paired to the player.

See the [notification behaviors](#notification-behaviors) section below for how to be notified when player triggers an Action.

## When using **Send Messages** or **Broadcast Messages**

When the [notification behavior](#notification-behaviors) of `PlayerInput` is set to **Send Messages** or **Broadcast Messages**, you can set your app to respond to Actions by defining methods in components like so:

```CSharp
public class MyPlayerScript : MonoBehaviour
{
    // "jump" action becomes "OnJump" method.

    // If you're not interested in the value from the control that triggers the action, use a method without arguments.
    public void OnJump()
    {
        // your Jump code here
    }

    // If you are interested in the value from the control that triggers an action, you can declare a parameter of type InputValue.
    public void OnMove(InputValue value)
    {
        // Read value from control. The type depends on what type of controls.
        // the action is bound to.
        var v = value.Get<Vector2>();

        // IMPORTANT:
        // The given InputValue is only valid for the duration of the callback. Storing the InputValue references somewhere and calling Get<T>() later does not work correctly.
    }
}
```

The component must be on the same `GameObject` if you are using `Send Messages`, or on the same or any child `GameObject` if you are using `Broadcast Messages`.

## When using **Invoke Unity Events**

When the [notification behavior](#notification-behaviors) of `PlayerInput` is set to `Invoke Unity Events`, each Action has to be routed to a target method. The methods have the same format as the [`started`, `performed`, and `canceled` callbacks](RespondingToActions.md#action-callbacks) on [`InputAction`](../api/UnityEngine.InputSystem.InputAction.html).

```CSharp
public class MyPlayerScript : MonoBehaviour
{
    public void OnFire(InputAction.CallbackContext context)
    {
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        var value = context.ReadValue<Vector2>();
    }
}
```