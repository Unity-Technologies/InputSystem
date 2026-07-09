---
uid: input-system-configure-unity-events
---

# Configure Unity events

You can use the following properties to configure `PlayerInput`:

| Component (UI) property | Description | Matching property |
| -- | -- | -- |
| **Actions** | The set of [Input Actions](xref:input-system-actions) associated with the player. Typically you would set this to Project-Wide Actions, however you can also assign an [ActionAsset](xref:input-system-action-assets) reference instead. To receive input, each player must have an associated set of [Actions](#actions). | [`actions`](xref:UnityEngine.InputSystem.PlayerInput.actions) |
| **Default Scheme** | The [Control Scheme](xref:input-system-control-schemes) to enable by default, as defined in the [`PlayerInput.actions`](xref:UnityEngine.InputSystem.PlayerInput.actions) property. | [`defaultControlScheme`](xref:UnityEngine.InputSystem.PlayerInput.defaultControlScheme) |
| **Default Map** | The [Action Map](xref:input-system-actions) in the [`PlayerInput.actions`](xref:UnityEngine.InputSystem.PlayerInput.actions) property to enable by default. If set to `None`, then the player starts with no Actions being enabled. | [`defaultActionMap`](xref:UnityEngine.InputSystem.PlayerInput.defaultActionMap) |
| **Camera** | The individual camera associated with the player. This is only required when employing [split-screen](xref:input-system-split-screen-multiplayer) setups and has no effect otherwise. | [`camera`](xref:UnityEngine.InputSystem.PlayerInput.camera) |
| **Behavior** | How the `PlayerInput` component [notifies](select-notification-behavior.md) game code about input actions and other input-related events happening to the player or that the player initiates. | [`notificationBehavior`](xref:UnityEngine.InputSystem.PlayerInput.notificationBehavior) |

## Actions

To receive input, each player must have an associated set of Input Actions.

### Specifying the Actions to use

The simplest workflow is to use the project-wide actions defined in the [Input Actions editor](actions-editor.md). However, the Player Input component also allows you to use an [Actions Asset](action-assets.md) to specify the actions that should be used by any instance of the component. If you set the **Actions** field to **Actions Asset**, the inspector displays a field into which you can assign an actions asset, and a **Create Actions** button which allows you to create a new actions asset. When you create these with the Player Input inspector's __Create Actions__ button, the Input System creates a default set of Actions. However, the Player Input component places no restrictions on the arrangement of Actions.

### Enabling and disabling Actions

The Player Input component automatically handles enabling and disabling Actions, and also handles installing [callbacks](set-callbacks-on-actions.md) on the Actions. When multiple Player Input components use the same Actions, the components automatically create [private copies of the Actions](local-multiplayer-scenarios.md). This is why, when writing input code that works with the PlayerInput component, you should not use `InputSystem.actions` because this references the "singleton" copy of the actions rather than the specific private copy associated with the PlayerInput instance you are coding for.

While we advise against using it, if you **really need or want** to use `InputSystem.actions` for single player use cases, it is advisible to manually disable them and manually enable the default map that **Player Input** sets, during `Start()`, like so:

```csharp
public class MyPlayerScript : MonoBehaviour
{
    PlayerInput playerInput;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        InputSystem.actions.Disable();
        playerInput.currentActionMap?.Enable();
    }
}

```

When first enabled, the Player Input component enables all Actions from the the [`Default Action Map`](xref:UnityEngine.InputSystem.PlayerInput). If no default Action Map exists, the Player Input component does not enable any Actions. To manually enable Actions, you can call [`Enable`](xref:UnityEngine.InputSystem.InputActionMap) and [`Disable`](xref:UnityEngine.InputSystem.InputActionMap) on the action maps or Actions, like you would do [without `PlayerInput`](actions.md). To check which Action Map is currently enabled, or to switch to a different one, use the  [`PlayerInput.currentActionMap`](xref:UnityEngine.InputSystem.PlayerInput) property. To switch actions maps with an action map name, you can also call [`PlayerInput.SwitchCurrentActionMap`](xref:UnityEngine.InputSystem.PlayerInput).

To disable a player's input, call [`PlayerInput.DeactivateInput`](xref:UnityEngine.InputSystem.PlayerInput). To re-enable it, call [`PlayerInput.ActivateInput`](xref:UnityEngine.InputSystem.PlayerInput). The latter enables the default Action Map, if it exists.

When `PlayerInput` is disabled, it automatically disables the currently active Action Map ([`PlayerInput.currentActionMap`](xref:UnityEngine.InputSystem.PlayerInput)) and disassociate any Devices paired to the player.

See the [notification behaviors](select-notification-behavior.md) section below for how to be notified when player triggers an Action.

## When using **Send Messages** or **Broadcast Messages**

When the [notification behavior](select-notification-behavior.md) of `PlayerInput` is set to **Send Messages** or **Broadcast Messages**, you can set your app to respond to Actions by defining methods in components like so:

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

When the [notification behavior](select-notification-behavior.md) of `PlayerInput` is set to `Invoke Unity Events`, each Action has to be routed to a target method. The methods have the same format as the [`started`, `performed`, and `canceled` callbacks](set-callbacks-on-actions.md#action-callbacks) on [`InputAction`](xref:UnityEngine.InputSystem.InputAction).

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
