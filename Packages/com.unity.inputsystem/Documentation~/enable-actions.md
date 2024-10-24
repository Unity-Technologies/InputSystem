# Enabling actions

Actions have an **enabled** state, meaning you can enable or disable them to suit different situations.

If you have an Action Asset assigned as [project-wide](./ProjectWideActions.md), the actions it contains are enabled by default and ready to use.

For actions defined elsewhere, such as in an Action Asset not assigned as project-wide, or defined your own code, they begin in a disabled state, and you must enable them before they will respond to input.

You can enable actions individually, or as a group by enabling the Action Map which contains them.

```CSharp
// Enable a single action.
lookAction.Enable();

// Enable an en entire action map.
gameplayActions.Enable();
```

When you enable an Action, the Input System resolves its bindings, unless it has done so already, or if the set of devices that the Action can use has not changed. For more details about this process, see the documentation on [binding resolution](ActionBindings.md#binding-resolution).

You can't change certain aspects of the configuration, such Action Bindings, while an Action is enabled. To stop Actions or Action Maps from responding to input, call  [`Disable`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_Disable).

While enabled, an Action actively monitors the [Control(s)](Controls.md) it's bound to. If a bound Control changes state, the Action processes the change. If the Control's change represents an [Interaction](Interactions.md) change, the Action creates a response. All of this happens during the Input System update logic. Depending on the [update mode](Settings.md#update-mode) selected in the input settings, this happens once every frame, once every fixed update, or manually if updates are set to manual.




