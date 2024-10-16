# Scripting With Actions

Scripting with Actions can be divided into three main tasks. These are to:

- Respond to actions at runtime
- Interactively rebind actions at runtime
- Dynamically create and configure actions at runtime

These tasks are listed in increasing complexity.

### Respond to actions at runtime

The simplest scenario is to create a game or app where your actions are all pre-configured before publishing, and are therefore fixed once published. In this scenario, you define all actions and bindings in the editor, and the only input scripting required in your project is to detect and respond to those actions. Users of your game or app in this scenario cannot reconfigure their input bindings, and are limited to using the keys, joypad buttons, or axes that you have defined.

The simplest ways to script responses to actions at runtime are to:

- [Poll project-wide actions](./RespondingToActions.md#polling-actions)
- [Set up callbacks from project-wide actions](./RespondingToActions.md#responding-to-actions-using-callbacks)
- [Set up callbacks using the PlayerInput component](./PlayerInput.md#connecting-actions-to-methods-or-callbacks)

### Interactively rebind actions at runtime

Many games offer players the opportunity to rebind actions to suit their own preference. This means players can specify which key, gamepad button, or axis to use for each of the actions in your game. In this scenario, you define all actions and their default bindings in the editor. Then, in addition to scripting responses to those actions, you must also create a user interface and corresponding code that allows your users to reconfigure the bindings to values that differ from the defaults, and save their new configuration.

Get started with implementing [Interactive Rebinding](InteractiveRebinding.md).

### Dynamically create and configure actions at runtime

While most common scenarios do not require dynamic creation and configuration of actions themselves at runtime, the API of the input system package is fully open, and allows you the ability to write code that does anything the input system itself does, including creating actions, configuring axes, setting up bindings, defining new types of custom interactions and processors. 

Get started with [dynamically creating actions](CreatingActionsAPI.md).
