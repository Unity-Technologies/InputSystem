---
uid: input-system-add-processors-bindings-actions
---

# Add processors to bindings and actions

To add a processor to an [action](actions.md) or [binding](ActionBindings.md) via the Input Actions Editor:

1. Select the action or binding you want to add processors to. The Properties panel opens in the right pane of the window. 
1. In the Properties panel, navigate to the **Processors** foldout. Select the **Add (+)** icon on the header to open a list of all available processors that match your control type. 
1. From the drop-down list, select a processor type. A processor of that type appears in the __Processors__ foldout. 
1. In the __Processors__ foldout, edit any parameters of the processor.

<br/>![An example of the Processors foldout in the Properties panel, displaying processors called Stick Deadzone and Scale Vactor 2](Images/BindingProcessors.png)

To remove a processor, select the **Remove (-)** icon next to it. You can also use the up and down  arrows to change the order of processors. This affects the order in which the system processes values.

To add a processor to an action or binding via code, use the following code examples as templates:

**Actions:**

```CSharp
var action = new InputAction(processors: "invertVector2(invertX=false)");
```

**Bindings:**

```CSharp
var action = new InputAction();
action.AddBinding("<Gamepad>/leftStick")
    .WithProcessor("invertVector2(invertX=false)");
```

>[!NOTE]
>The received value and result value must be of the same type. To convert received input values into different types, see [composite Bindings](ActionBindings.md#composite-bindings).