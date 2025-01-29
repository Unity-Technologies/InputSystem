# Apply Interactions to Bindings

When you create Bindings for your [Actions](actions.md), you can choose to add Interactions to the Bindings via the Editor, or via code.

To apply Interactions to all Bindings on an Action, refer to [Apply Interactions to Actions](apply-interactions-sctions).

## Apply Interactions to Bindings via the Editor

If you're using [project-wide actions](ActionsEditor.md), or [Input Action Assets](ActionAssets.md), you can add any Interaction to your Bindings via the Input Action editor. 

1. Once you have [created some Bindings](ActionsEditor.md#bindings), select the Binding you want to add Interactions to, so that the right pane of the window displays the properties for that Binding. 
1. Select the plus icon on the __Interactions__ foldout to open a list of all available Interactions types. 
1. Select an Interaction type to add an Interaction instance of that type. The Interaction now appears in the __Interactions__ foldout. 
1. If the Interaction has any parameters, you can now edit them at this stage.

![Binding Processors](Images/BindingProcessors.png)

To remove an Interaction, select the minus (-) button next to it. To change the [order of Interactions](#multiple-interactions-on-a-binding), select the up and down arrows.

## Apply Interactions to Bindings via code

To add Interactions to Bindings that you created in code, you can use the following code sample as a template:

```CSharp
var Action = new InputAction();
action.AddBinding("<Gamepad>/leftStick")
    .WithInteractions("tap(duration=0.8)");
```