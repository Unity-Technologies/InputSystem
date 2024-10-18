---
uid: input-system-actions
---
# Actions

**Actions** allow you to separate the purpose of an input from the device controls which perform that input. Actions allow you to associate the purpose and device controls together in a flexible way.

For example, the purpose of an input in a game might be to make the player's character move. The device control associated with that action might be the left gamepad stick.

The association between an Action and the device controls which perform that input is a **binding**, and you can set up bindings in the [Input Actions editor](ActionsEditor.md). When you use Actions in your code, you do not need to refer to specific devices because the binding defines which device's controls are used to perform the action.

To use actions in your code, you must use the [Input Actions editor](ActionsEditor.md) to configure the mapping between the Action and one or more device controls. For example in this screenshot, the "Move" action is displayed, showing its bindings the left gamepad stick, and the keyboard's arrow keys.

![Actions Bindings](Images/ActionsBinding.png)<br/>
*The Actions panel of the Input Actions Editor in Project Settings*

You can then get a reference to this action in your code, and check its value, or attach a callback method to be notified when it is performed. See the [Actions Workflow page](Workflow-Actions.md) for a simple example script demonstrating this.

Actions also make it simpler to create a system that lets your players [customize their bindings at runtime](ActionBindings.md#interactive-rebinding), which is a common requirement for games.

>**Notes:**
> - Actions are a runtime only feature. You can't use them in [Editor window code](https://docs.unity3d.com/ScriptReference/EditorWindow.html).
>
> - You can read input without using Actions and Bindings by directly reading specific device controls. This is less flexible, but can be quicker to implement for certain situations. Read more about [directly reading devices from script](Workflow-Direct.md).
>
