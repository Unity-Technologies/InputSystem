# Processors on Actions

Processors on Actions work in the same way as Processors on Bindings, but they affect all controls bound to an Action, rather than just the controls from a specific Binding. If there are Processors on both the Binding and the Action, the system processes the ones from the Binding first.

You can add and edit Processors on Actions in the [Input Actions Editor](ActionsEditor.md), or in an  [Action Asset](ActionAssets.md) the [same way](#processors-on-bindings) as you would for Bindings: select an Action to edit, then add one or more Processors in the right window pane.

If you create your Actions in code, you can add Processors like this:

```CSharp
var action = new InputAction(processors: "invertVector2(invertX=false)");
```
