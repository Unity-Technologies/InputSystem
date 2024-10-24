# Using Interactions

You can install Interactions on [Bindings](ActionBindings.md) or [Actions](Actions.md).

### Interactions applied to Bindings

When you create Bindings for your [Actions](Actions.md), you can choose to add Interactions to the Bindings.

If you're using [project-wide actions](ActionsEditor.md), or [Input Action Assets](ActionAssets.md), you can add any Interaction to your Bindings in the Input Action editor. Once you [created some Bindings](ActionsEditor.md#bindings), select the Binding you want to add Interactions to, so that the right pane of the window shows the properties for that Binding. Next, click on the plus icon on the __Interactions__ foldout to open a list of all available Interactions types. Choose an Interaction type to add an Interaction instance of that type. The Interaction now appears in the __Interactions__ foldout. If the Interaction has any parameters, you can now edit them here as well:

![Binding Processors](Images/BindingProcessors.png)

To remove an Interaction, click the minus button next to it. To change the [order of Interactions](#multiple-interactions-on-a-binding), click the up and down arrows.

If you create your Bindings in code, you can add Interactions like this:

```CSharp
var Action = new InputAction();
action.AddBinding("<Gamepad>/leftStick")
    .WithInteractions("tap(duration=0.8)");
```

### Interactions applied to Actions

Applying Interactions directly to an Action is equivalent to applying them to all Bindings for the Action. It is thus more or less a shortcut that avoids manually adding the same Interaction(s) to each of the Bindings.

If Interactions are applied __both__ to an Action and to its Bindings, then the effect is the same as if the Action's Interactions are *appended* to the list of Interactions on each of the Bindings. This means that the Binding's Interactions are applied *first*, and then the Action's Interactions are applied *after*.

You can add and edit Interactions on Actions in the [Input Action Assets](ActionAssets.md) editor window the [same way](#interactions-applied-to-bindings) as you would do for Bindings: select an Action to Edit, then add the Interactions in the right window pane.

If you create your Actions in code, you can add Interactions like this:

```CSharp
var Action = new InputAction(Interactions: "tap(duration=0.8)");
```