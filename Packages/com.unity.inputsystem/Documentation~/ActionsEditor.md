---
uid: input-system-configuring-input
---
# Configuring input

The **Input Actions Editor** allows you to edit [action assets](xref:input-system-action-assets), which contain a saved configuration of [input actions](xref:input-system-actions) and their associated [bindings](xref:input-system-action-bindings).

It allows you to group collections of actions into [action maps](ActionsEditor.html#configure-action-maps), which represent different input scenarios in your project (such as UI navigation, gameplay, etc.)

It also allows you to define [control schemes](xref:input-system-action-bindings#control-schemes) which are a way to enable or disable a set of devices, or respond to which type of device is being used. This is often useful if you want to customize your UI based on whether your users are using a mouse, keyboard, or gamepad as their chosen input.

## Action assets and project-wide actions

The typical workflow for most projects is to have a single action asset, which is assigned as the **project-wide actions**. Refer to [Project-Wide Actions](xref:project-wide-actions) to create and assign an actions asset as your project-wide action if you haven't already done this.

## The Input Actions Editor window and panels

The **Input Actions Editor** appears when you double-click an action asset to open it.

It also appears in the Project Settings window under **Edit** > **Project Settings** > **Input System Package** if you have an action asset assigned as project-wide.

![The Input Actions Editor displays the three panels and the default actions](./Images/ActionsEditorCallout.png)

The Input Actions Editor is divided into three panels (marked A, B & C in the image above).

|Panel name|Description|
|-|-|
|**(A)&nbsp;Action Maps**|Displays the list of currently defined action maps. Each action map is a collection of actions that you can enable or disable together as a group.|
|**(B)&nbsp;Actions**|Displays all the actions defined in the currently selected action map, and the bindings associated with each action.|
|**(C)&nbsp;Properties**|Displays the properties of the currently selected action or binding from the Actions panel. The title of this panel changes depending on whether you have an action or a binding selected in the Actions panel.|

## Configure action maps

* To add a new action map, select the Add (+) icon in the header of the __Action Maps__ panel.
* To rename an existing action map, either long-click the name, or right-click the action map and select __Rename__ from the context menu. Note that action map names can't contain slashes  (`/`).
* To delete an existing action map, right-click it and select __Delete__ from the context menu.
* To duplicate an existing action map, right-click it and select __Duplicate__ from the context menu.

## Configure actions

* To add a new action, select the Add (+) icon in the header of the __Action__ column.
* To rename an existing action, either long-click the name, or right-click the action map and select __Rename__ from the context menu.
* To delete an existing action, either right-click it and select __Delete__ from the context menu.
* To duplicate an existing action, either right-click it and select __Duplicate__ from the context menu.

## Edit action properties

If you select an action, you can edit its properties in the __Action Properties__ panel on the right:

![The Action Properties panel of the Input Actions Editor displays the Action, Interactions, and Processors groups expanded.](Images/ActionProperties.png)

### Action Type

Use the __Action Type__ setting to select **Button**, **Value** or **PassThrough**.

These options relate to whether this action should represent a discrete on/off button-style interaction or a value that can change over time while the control is being used.

For device controls such as keyboard keys, mouse clicks, or gamepad buttons, select **Button**. For device controls such as mouse movement, a joystick or gamepad stick, or device orientation that provide continuously changing input over a period of time, select **Value**.

The Button and Value types of action also provides data about the action such as whether it has started and stopped, and conflict resolution in situations where multiple bindings are mapped to the same action.

The third option, **PassThrough**, is also a value type, and as such is suitable for the same types of device controls as value. The difference is that actions set to PassThrough only provide basic information about the values incoming from the device controls bound to it, and does not provide the extra data relating to the phase of the action, nor does it perform conflict resolution in the case of multiple controls mapped to the same action.

For details about how these types work, refer to [Action types](xref:input-system-responding#action-types) and [Default Interaction](xref:input-system-interactions#default-interaction).

### Control Type

The __Control Type__ setting allows you to select the type of control expected by the action. This limits the controls shown when setting up bindings in the UI and also limits which contols can be bound interactively to the action.

For example, if you select **2D axis**, only those controls that can supply a 2D vector as value are available as options for the binding control path.

There are more specific control types available which further filter the available bindings, such as "Stick", "Dpad" or "Touch". If you select one of these control types, the list of available controls is further limited to only those controls of those specific types when you select a binding for your action (see directly below).

## Bindings

* To add a new binding, select the Add (+) icon on the action you want to add it to, and select the binding type from the menu that appears.
* To delete an existing binding, either right-click it and select __Delete__ from the context menu.
* To duplicate an existing binding, either right-click it and select __Duplicate__ from the context menu.

You can add multiple bindings to an action, which is generally useful for supporting multiple types of input device. For example, in the default set of actions, the "Move" action has a binding to the left gamepad stick and the WSAD keys, which means input through any of these bindings will perform the action.

![](./Images/ActionWithMultipleBindings.png)<br/>
_The default Move action in the Input Actions Editor, displaying the multiple bindings associated with it._

If you select a binding, you can edit its properties in the __Binding Properties__ panel on the right:

![The Binding Properties panel displays the Path value as Left Stick [Gamepad].](Images/BindingProperties.png)

### Set control paths

The most important property of any binding is the [control path](xref:input-system-controls#control-paths) it's bound to. To edit it, open the __Path__ dropdown menu. This displays a control picker window.

![The Binding Properties panel displays the control picker window available from the Path dropdown menu.](Images/InputControlPicker.png)

In the control picker window, you can explore a tree of input devices and controls that the Input System recognizes, and bind to these controls. Unity filters this list by the action's [`expectedControlType`](xref:UnityEngine.InputSystem.InputAction.expectedControlType) property. For example, if the control type is `Vector2`, you can only select a control that generates two-dimensional values, like a stick.

The device and control tree is organized hierarchically from generic to specific. For example, the __Gamepad__ control path `<Gamepad>/buttonSouth` matches the lower action button on any gamepad. Alternatively, if you navigate to __Gamepad__ > __More Specific Gamepads__ and select __PS4 Controller__, and then choose the control path `<DualShockGamepad>/buttonSouth`, this only matches the "Cross" button on PlayStation gamepads, and doesn't match any other gamepads.

Instead of browsing the tree to find the control you want, it's easier to let the Input System listen for input. To do that, select the __Listen__ button. At first, the list of Controls is empty. Once you start pressing buttons or actuating Controls on the Devices you want to bind to, the control picker window starts listing any bindings that match the controls you pressed. Select any of these bindings to view them.

Finally, you can choose to manually edit the binding path, instead of using the control picker. To do that, select the __T__ button next to the control path popup. This changes the popup to a text field, where you can enter any binding string. This also allows you to use wildcard (`*`) characters in your bindings. For example, you can use a binding path such as `<Touchscreen>/touch*/press` to bind to any finger being pressed on the touchscreen, instead of manually binding to `<Touchscreen>/touch0/press`, `<Touchscreen>/touch1/press` and so on.

### Edit composite bindings

Composite bindings are bindings consisting of multiple parts, which form a control together. For instance, a [2D Vector Composite](xref:input-system-action-bindings#2d-vector) uses four buttons (left, right, up, down) to simulate a 2D stick input. Refer to [Composite bindings](xref:input-system-action-bindings#composite-bindings) to learn more.

![The WASD setting appears under the Move property on the Actions panel.](Images/2DVectorComposite.png){width="486" height="178"}


To create a composite binding, in the Input Actions Editor, select the Add (+) icon on the action you want to add it to, and select the composite binding type from the popup menu.

![The Add Up/Down/Left/Right Composite binding is selected for the Move property on the Actions panel.](Images/Add2DVectorComposite.png){width="486" height="199"}

This creates multiple binding entries for the action: one for the Composite as a whole, and then, one level below that, one for each Composite part. The Composite itself doesn't have a binding path property, but its individual parts do, and you can edit these parts like any other binding. Once you bind all the Composite's parts, the Composite can work together as if you bound a single control to the action.

> [!NOTE]
> The set of Composites displayed in the menu is depends on the value type of the action. This means that, for example, if the action is set to type "Button", then only Composites able to return values of type `float` will be shown.

To change the type of a Composite retroactively, select the Composite, then select the new type from the **Composite Type** drop-down in the **Properties** pane.

![The Composite Type binding is set to 2D Vector binding on the Actions panel.](./Images/CompositeType.png){width="486" height="184"}

To change the part of the Composite to which a particular binding is assigned, use the **Composite Part** drop-down in the binding's properties.

![The Composite Part binding is set to Up under the Path binding property.](./Images/CompositePart.png){width="486" height="161"}

You can assign multiple bindings to the same part. You can also duplicate individual part bindings: right-click the binding, then select **Duplicate** to create new part bindings for the Composite. This can be used, for example, to create a single Composite for both "WASD" style controls and arrow keys.

![The Keyboard setting under Move on the Actions panel displays duplicated part bindings.](./Images/DuplicatedPartBindings.png){width="486" height="214"}

## Edit control schemes

Input action assets can have multiple [control schemes](xref:input-system-action-bindings#control-schemes), which let you enable or disable different sets of bindings for your actions for different types of Devices.

![Gamepad appears as the Scheme Name value on the Add Control Scheme window.](Images/ControlSchemeProperties.png)

To see the control schemes in the Input Actions Editor, open the control scheme drop-down list in the top left of the window. This menu lets you add or remove control schemes to your actions asset. If the actions asset contains any control schemes, you can select a control scheme, and then the window only shows bindings that are associated with that scheme. If you select a binding, you can pick the control schemes for which this binding is active in the __Properties__ panel on the left.

When you add a new control scheme, or select an existing control scheme, and then select __Edit Control Scheme__, you can edit the name of the control scheme and which devices the scheme should be active for. When you add a new control scheme, the "Device Type" list is empty by default (as shown above). You must add at least one type of device to this list for the control scheme to be functional.
