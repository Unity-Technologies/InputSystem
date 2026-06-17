---
uid: input-system-apply-interactions-bindings
---

# Apply interactions to bindings

When you create bindings for your [Actions](actions.md), you can choose to add Interactions to the bindings with the Editor, or with code.

To apply Interactions to all bindings on an Action, refer to [Apply Interactions to Actions](apply-interactions-actions.md).

## Apply Interactions to bindings in the Editor

If you're using [project-wide actions](actions-editor.md), or [Input Action Assets](action-assets.md), you can add any Interaction to your bindings via the Input Action editor. 

1. Once you have [created some bindings](actions-editor.md#bindings), select the binding you want to add Interactions to, so that the right pane of the window displays the properties for that binding. 
1. Select the plus icon on the __Interactions__ foldout to open a list of all available Interactions types. 
1. Select an Interaction type to add an Interaction instance of that type. The Interaction now appears in the __Interactions__ foldout. 
1. If the Interaction has any parameters, you can now edit them at this stage.

![Binding Processors](Images/BindingProcessors.png)

To remove an Interaction, select the minus (-) button next to it. To change the [order of Interactions](introduction-interactions.md#multiple-interactions-on-a-binding), select the up and down arrows.

## Apply Interactions to bindings in code

To add Interactions to bindings that you created in code, you can use the following code sample as a template:

```CSharp
var Action = new InputAction();
action.AddBinding("<Gamepad>/leftStick")
    .WithInteractions("tap(duration=0.8)");
```