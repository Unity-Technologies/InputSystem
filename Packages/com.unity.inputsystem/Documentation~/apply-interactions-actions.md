# Apply Interactions to Actions

Applying Interactions directly to an Action is equivalent to applying them to all Bindings for the Action. You can use this instead of manually adding the same Interaction(s) to multiple Bindings.

To apply Interactions to individual Bindings, refer to [Apply Interactions to Bindings](apply-interactions-bindings).

If you apply Interactions to both an Action and its Bindings, then the effect is the same as if the Action's Interactions are on the list of Interactions on each of the Bindings. This means that the Input System applies the Binding's Interactions first, and then the Action's Interactions.

## Apply Interactions to Actions via the Editor

To apply interactions via the Input Action Editor: 

1. Select an Action to edit, so that the right pane of the window displays the properties for that Action. 
1. Select the plus icon on the __Interactions__ foldout to open a list of all available Interactions types. 
1. Select an Interaction type to add an Interaction instance of that type. The Interaction now appears in the __Interactions__ foldout. 
1. If the Interaction has any parameters, you can now edit them at this stage.

## Apply Interactions to Actions via code

If you create your Actions in code, you can add Interactions like this:

```CSharp
var Action = new InputAction(Interactions: "tap(duration=0.8)");
```