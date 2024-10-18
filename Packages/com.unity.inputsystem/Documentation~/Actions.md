---
uid: input-system-actions
---
# Actions

**Actions** are an important concept in the Input System. They allow you to separate the purpose of an input from the device controls which perform that input. Actions allow you to associate the purpose and device controls together in a flexible way.

For example, the purpose of an input in a game might be to make the player's character move around. The device control associated with that action might be the motion of the left gamepad stick.

The association between an Action and the device controls which perform that input is a **binding**, and you can set up bindings in the [Input Actions editor](ActionsEditor.md). When you use Actions in your code, you do not need to refer to specific devices because the binding defines which device's controls are used to perform the action.

To use actions in your code, you must use the [Input Actions editor](ActionsEditor.md) to establish the mapping between the Action and one or more device controls. For example in this screenshot, the "Move" action is displayed, showing its bindings the left gamepad stick, and the keyboard's arrow keys.

![Actions Bindings](Images/ActionsBinding.png)<br/>
*The Actions panel of the Input Actions Editor in Project Settings*

You can then get a reference to this action in your code, and check its value, or attach a callback method to be notified when it is performed. See the [Actions Workflow page](Workflow-Actions.md) for a simple example script demonstrating this.

Actions also make it simpler to create a system that lets your players [customize their bindings at runtime](ActionBindings.md#interactive-rebinding), which is a common requirement for games.

**Notes:**

 - Actions are a runtime only feature. You can't use them in [Editor window code](https://docs.unity3d.com/ScriptReference/EditorWindow.html).

 - You can read input without using Actions and Bindings by directly reading specific device controls. This is less flexible, but can be quicker to implement for certain situations. Read more about [directly reading devices from script](Workflow-Direct.md).

 - Although you can reorder actions in this window, the ordering is for visual convenience only, and does not affect the order in which the actions are triggered in your code. If multiple actions are performed in the same frame, the order in which they are reported by the input system is undefined. To avoid problems, you should not write code that assumes they will be reported in a particular order.




## Creating Actions

The simplest way to create actions is to use the [Input Actions editor](ActionsEditor.md) in the Project Settings window. This is the primary recommended workflow and suitable for most scenarios.

However, because the input system API is very open, there are many other ways to create actions which may suit less common scenarios. For example, by loading actions from JSON data, or creating actions entirely in code.

### Creating Actions using the Action editor

For information on how to create and edit Input Actions in the editor, see the [Input Actions editor](ActionsEditor.md). This is the recommended workflow if you want to organise all your input actions and bindings in one place, which applies across the whole of your project. This often the case for most types of game or app.

![Action Editor Window](Images/ProjectSettingsInputActionsSimpleShot.png)
*The Input Actions Editor in the Project Settings window*


# Other ways to create Actions

The simplest way to create actions is to use the [Input Actions editor](ActionsEditor.md) to configure a set of actions in an asset, as described above. However, because the Input System package API is open and flexible, you can create actions using alternative techniques. These alternatives might be more suitable if you want to customize your project beyond the standard workflow.

Read more about [creating actions in code](CreatingActionsAPI).
