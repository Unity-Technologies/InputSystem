### Processors on Bindings

When you create Bindings for your [actions](Actions.md), you can choose to add Processors to the Bindings. These process the values from the controls they bind to, before the system applies them to the Action value. For instance, you might want to invert the `Vector2` values from the controls along the Y axis before passing these values to the Action that drives the input logic for your application. To do this, you can add an [Invert Vector2](#invert-vector-2) Processor to your Binding.

If you're using Actions defined in the [Input Actions Editor](ActionsEditor.md), or in an  [Action Asset](ActionAssets.md), you can add any Processor to your Bindings in the Input Action editor. Select the Binding you want to add Processors to so that the right pane of the window displays the properties for that Binding. Select the Add (+) icon on the __Processors__ foldout to open a list of all available Processors that match your control type, then choose a Processor type to add a Processor instance of that type. The Processor now appears under the __Processors__ foldout. If the Processor has any parameters, you can edit them in the __Processors__ foldout.

![Binding Processors](Images/BindingProcessors.png)

To remove a Processor, click the Remove (-) icon next to it. You can also use the up and down arrows to change the order of Processors. This affects the order in which the system processes values.

If you create your Bindings in code, you can add Processors like this:

```CSharp
var action = new InputAction();
action.AddBinding("<Gamepad>/leftStick")
    .WithProcessor("invertVector2(invertX=false)");
```
