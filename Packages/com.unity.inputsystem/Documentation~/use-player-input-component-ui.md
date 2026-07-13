---
uid: input-system-use-player-input-component-ui
---

# Use the Player Input component with UI

The `PlayerInput` component can work together with an [`InputSystemUIInputModule`](supported-ui-systems.md) to drive the [UI system](ui-input.md).

To set this up, assign a reference to a `InputSystemUIInputModule` component in the [`UI Input Module`](xref:UnityEngine.InputSystem.PlayerInput) field of the `PlayerInput` component. The `PlayerInput` and `InputSystemUIInputModule` components should be configured to work with the same [`InputActionAsset`](actions.md) for this to work.

Once you've completed this setup, when the `PlayerInput` component configures the Actions for a specific player, it assigns the same Action configuration to the `InputSystemUIInputModule`. In other words, the same Action and Device configuration that controls the player now also controls the UI.

If you use [`MultiplayerEventSystem`](multiplayer-ui-input.md) components to dispatch UI events, you can also use this setup to simultaneously have multiple UI instances on the screen, each controlled by a separate player.

> [!NOTE]
> - As a general rule, if you are using the PlayerInput workflow, you should read input through callbacks as described above, however if you need to access the input actions asset directly while using the PlayerInput component, you should access the [PlayerInput component's copy of the actions](xref:UnityEngine.InputSystem.PlayerInput), not `InputSystem.actions`. This is because the PlayerInput component performs device filtering to automatically assign devices to multiple players, so each instance has its own copy of the actions filtered for each player. If you bypass this by reading `InputSystem.actions` directly, the automatic device assignment won't work.
>
> - This component is built on top of the public Input System API. As such, they don't do anything that you can't program yourself. They are meant primarily as an easy, out-of-the-box setup that eliminates much of the need for custom scripting.
> <br/>&nbsp;
