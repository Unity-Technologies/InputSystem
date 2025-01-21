---
uid: input-system-configuring-input
---
# Configuring Input with the Actions Editor

The **Input Actions Editor** allows you to edit [Action Assets](ActionAssets.md), which contain a saved configuration of [Input Actions](actions.md) and their associated [Bindings](ActionBindings.md).

It allows you to group collections of Actions into [Action Maps](ActionsEditor.html#configure-action-maps), which represent different input scenarios in your project (such as UI navigation, gameplay, etc.)

It also allows you to define [Control Schemes](ActionBindings.md#control-schemes) which are a way to enable or disable a set of devices, or respond to which type of device is being used. This is often useful if you want to customise your UI based on whether your users are using mouse, keyboard, or gamepad as their chosen input.

### Action Assets and Project-Wide Actions

The typical workflow for most projects is to have a single Action Asset, which is assigned as the **project-wide actions**. If you have not yet created and assigned an Actions Asset as the project-wide actions, the recommended workflow is to do this first. Read more about [project-wide actions](about-project-wide-actions.md).

### Opening the Actions Editor

The **Input Actions Editor** is an editor window displayed when you open an Action Asset by double-clicking it.

It is also displayed in the Project Settings window under **Edit** > **Project Settings** > **Input System Package** if you have an Action Asset assigned as project-wide.

![image alt text](./Images/ActionsEditorCallout.png)
*The Input Actions editor, displaying the default actions*

### The Actions Editor panels

The Input Actions editor is divided into three panels (marked A, B & C above).

|Name|Description|
|-|-|
|**(A)&nbsp;Action Maps**|Displays the list of currently defined Action Maps. Each Action Map is a collection of Actions that you can enable or disable together as a group.|
|**(B)&nbsp;Actions**|Displays all the actions defined in the currently selected Action Map, and the bindings associated with each Action.|
|**(C)&nbsp;Properties**|Displays the properties of the currently selected Action or Binding from the Actions panel. The title of this panel changes depending on whether you have an Action or a Binding selected in the Actions panel.|


### Configure Actions

* To add a new Action, select the Add (+) icon in the header of the Action column.
* To rename an existing Action, either long-click the name, or right-click the Action Map and select __Rename__ from the context menu.
* To delete an existing Action, either right-click it and select __Delete__ from the context menu.
* To duplicate an existing Action, either right-click it and select __Duplicate__ from the context menu.


> **Note:**
> Although you can reorder actions in this window, the ordering is for visual convenience only, and does not affect the order in which the actions are triggered in your code. If multiple actions are performed in the same frame, the order in which they are reported by the input system is undefined. To avoid problems, you should not write code that assumes they will be reported in a particular order.


### Editing Control Schemes

Input Action Assets can have multiple [Control Schemes](ActionBindings.md#control-schemes), which let you enable or disable different sets of Bindings for your Actions for different types of Devices.

![Control Scheme Properties](Images/ControlSchemeProperties.png)

To see the Control Schemes in the Input Action Asset editor window, open the Control Scheme drop-down list in the top left of the window. This menu lets you add or remove Control Schemes to your Actions Asset. If the Actions Asset contains any Control Schemes, you can select a Control Scheme, and then the window only shows bindings that are associated with that Scheme. If you select a binding, you can now pick the Control Schemes for which this binding should be active in the __Properties__ view to the left of the window.

When you add a new Control Scheme, or select an existing Control Scheme, and then select __Edit Control Scheme__, you can edit the name of the Control Scheme and which devices the Scheme should be active for. When you add a new Control Scheme, the "Device Type" list is empty by default (as shown above). You must add at least one type of device to this list for the Control Scheme to be functional.
