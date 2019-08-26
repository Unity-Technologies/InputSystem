# Input Action Assets

* [Creating action assets](#creating-input-action-assets)
* [Editing action assets](#editing-input-action-assets)
* [Using action assets](#using-input-action-assets)

An "input action asset" is an asset containing [input actions](Actions.md) as well as their associated [bindings](ActionBindings.md) and [control schemes](ActionBindings.md#control-schemes). These assets are distinguished by the `.inputactions` file extension and are stored in a plain JSON format.

## Creating Input Action Assets

To create an asset containing [input actions](Actions.md) in Unity, right-click in the Project window or open the `Assets` entry in Unity's main menu. From there, select `Create >> Input Actions`.

![Create Input Actions](Images/CreateInputActions.png)

## Editing Input Action Assets

To bring up the action editor, double-click an `.inputactions` asset in the Project Browser or click the "Edit asset" button in the inspector for the asset. Multiple action editor windows can be open concurrently (not on the same asset, though).

![Action Editor Window](Images/MyGameActions.png)

The action editor comes up as a separate window which can optionally be docked into Unity's main UI.

>NOTE: For details about how action maps, actions, and bindings work, see the documentation on [actions](Actions.md).

Edits made in the action asset window are not saved automatically with the project by default. To save your changes, click `Save Asset` in the window's toolbar. To discard your changes, close the window and choose `Don't Save`. Alternatively, auto-saving can be toggled on by ticking the `Auto-Save` checkbox in the toolbar. This will cause any change to the asset to automatically be persisted back to disk. This setting will take effect for all `.inputactions` assets and will persist across editor sessions.

The editor window is divided into three panes.

1. The left pane lists the "action maps" in the asset. Each map is a collection of actions that can be enabled and disabled in bulk.
2. The middle pane contains the actions in the currently selection action map and the bindings associated with each action.
3. The right pane contains the properties of the currently selected action or binding.

Multiple shorts are available to provide quick access to certain common operations.

|Shortcut (Mac)|Shortcut (Windows)|Description|
|--------------|------------------|-----------|
|⌘X, ⌘C, ⌘V|Ctrl-X, Ctrl-C, Ctrl-V|Cut, Copy and Paste. Can be used on actions, action maps and binding.|
|⌘D|Ctrl-D|Duplicate. Can be used on actions, action maps and bindings.|
|⌘⌫|Del|Delete. Can be used on actions, action maps and bindings.|
|⌥S|Alt-S|Save.|
|⌥M|Alt-M|Add Action Map.|
|⌥A|Alt-A|Add Action.|
|⌥B|Alt-B|Add Binding.|


>Pro Tip: You can search quickly by devices and/or control schemes directly from the search box. "d:gamepad" filters for bindings to gamepad devices whereas "g:gamepad" filters for bindings in the gamepad control scheme. Matching is case-insensitive and will match any partial name.

### Editing Action Maps

![Action Maps Column](Images/ActionMapsColumn.png)

>NOTE: Action map names should not contain slashes (`/`).

* To add a new action map, click the plus icon in the header of the action map column.
* To rename an existing map, either long-click the name or right-click the action map and select "Rename" from the context menu.
* To delete an existing map, either right-click it and select "Delete" from the context menu or use the Delete key (Windows) / ⌘⌫ (Mac).
* To duplicate an existing map, either right-click it and select "Duplicate" or use Ctrl-D (Windows) / ⌘D (Mac).

### Editing Actions

![Action Column](Images/ActionColumn.png)

* To add a new action, click the plus icon in the header of the action column.
* To rename an existing action, either long-click the name or right-click the action map and select "Rename" from the context menu.
* To delete an existing action, either right-click it and select "Delete" from the context menu or use the Delete key (Windows) / ⌘⌫ (Mac).
* To duplicate an existing action, either right-click it and select "Duplicate" or use Ctrl-D (Windows) / ⌘D (Mac).

If you select an action you can edit it's properties in the right hand pane of the window:

![Action Properties](Images/ActionProperties.png)

### Editing Bindings

* To add a new binding, click the plus icon on the action you want to add it to, and select the binding type fron the popup menu.
* To delete an existing binding, either right-click it and select "Delete" from the context menu or use the Delete key (Windows) / ⌘⌫ (Mac).
* To duplicate an existing binding, either right-click it and select "Duplicate" or use Ctrl-D (Windows) / ⌘D (Mac).

If you select an action you can edit it's properties in the right hand pane of the window:

![Binding Properties](Images/BindingProperties.png)

#### Picking Controls

The most important property of any binding is the [control path](Controls.md#control-paths) it is bound to. To edit it, click on the popup button for the `Path` property. This will pop up a Control Picker window.

![Control Picker](Images/InputControlPicker.png)

In the control picker window, you can explore a tree of input devices and controls known to the input system to bind to. This list will be filtered by the action's [`Control Type`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_expectedControlType) property, so if the control type is `Vector2`, you will only be able to select any control which generates two-dimensional values (like a stick). The device and control tree is organized hierarchically from generic to specific. So, you can for instance navigate to "Gamepad", and choose the control path `<Gamepad>/buttonSouth`. This will then match the lower action button on any gamepad. But you can also navigate to "Gamepad", and from there scroll down to "More specific Gamepads" and select "PS4 Controller", and then choose the control path `<DualShockGamepad>/buttonSouth`. This will then only match the "cross" button on PlayStation gamepads, and not match any other gamepads.

Instead of browsing the tree to find the control you want, it is often easier to simply let the input system listen for input. To do that, click the "Listen" button. The list of controls will first be empty. Now you can start clicking buttons or actuating controls on the devices you want to bind to. The control picker window will list any bindings which would match the controls you pressed, and you can click on one to choose it.

Finally, you can choose to manually edit the binding path, instead of using the control picker. To do that, you can click the "T" button next to the control path popup. This will switch the popup to a text field, where you can enter any binding string. The advantage of doing this is that you are allowed to use wildcard (`*`) characters in your bindings. So you can for instance use a binding path such as `<Touchscreen>/touch*/press` to bind to any finger being pressed on the touchscreen (instead of manually binding to `<Touchscreen>/touch0/press`, `<Touchscreen>/touch1/press`, etc..).

#### Editing Composite Bindings

Composite bindings are bindings consisting of multiple parts, which form a control together. For instance a [2D Vector composite](ActionBindings.md#2d-vector) uses four buttons (left, right, up, down) to simulate a 2D stick input. You can learn more about how composites works and which types of composites there are [here](ActionBindings.md#composite-bindings).

To create a composite binding in the input action asset editor window, click the plus icon on the action you want to add it to, and select the composite binding type fron the popup menu. This will create multiple bindings entries for the action - one for the composite as a whole, and then, one level below that, one for each composite part. The composite  itself does not have a binding path property, but it's individual parts do - and those can be edited like any other binding. Once you have bound all the composite's parts, the composite can work together as if you bound a single control to the action.

### Editing Control Schemes

Input Action Assets can have multiple [control schemes](ActionBindings.md#control-schemes) which let you enable or disable different sets of bindings for your actions for different types of devices.

![Control Scheme Properties](Images/ControlSchemeProperties.png)

To see the control schemes in the input action asset editor window, click the control scheme popup button in the top left of the window. This popup menu lets you add or remove control schemes to your asset. If your asset contains any control schemes, you can select a control scheme, and then the window will only show bindings belong to that scheme. If you select a binding, you will now be able to pick the control schemes for which this binding should be active in the properties view to the left of the window. When you add a new control scheme (or select an existing control scheme, and then select "Edit Control Scheme…" from the popup), a little window will open which lets you edit the name of the control scheme and which devices the scheme should be active for.

## Using Input Action Assets

### Auto-Generating Script Code for Actions

One of the most convenient ways to work with `.inputactions` assets in script is to generate a C# wrapper class for them automatically. This obviates the need for manually looking up actions and maps using their names and also provides easier ways for setting up callbacks.

To enable this, tick the `Generate C# Class` in the importer properties in the inspector of the `.inputactions` asset when selected in Unity and hit "Apply".

![MyPlayerControls Importer Settings](Images/FireActionInputAssetInspector.png)

(You can optionally choose a path name, and a class name and namespace for the generated script, or you can keep the defaults.)

This will generate a C# script that makes working with the asset a lot simpler.

```CSharp
using UnityEngine;
using UnityEngine.Experimental.Input;

// IGameplayActions is an interface generated from the "gameplay" action map
// we added (note that if you called the action map differently, the name of
// the interface will be different). This was triggered by the "Generate Interfaces"
// checkbox.
public class MyPlayerScript : MonoBehaviour, IGameplayActions
{
    // MyPlayerControls is the C# class that has been generated for us.
    // It encapsulates the data  from the .inputactions asset we created
    // and automatically looks up all the maps and actions for us.
    MyPlayerControls controls;

    public void OnEnable()
    {
        if (controls == null)
        {
            controls = new MyPlayerControls();
            // Tell the "gameplay" action map that we want to get told about
            // when actions get triggered.
            controls.gameplay.SetCallbacks(this);
        }
        controls.gameplay.Enable();
    }

    public void OnDisable()
    {
        controls.gameplay.Disable();
    }

    public void OnUse(InputAction.CallbackContext context)
    {
        // 'Use' code here.
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        // 'Move' code here.
    }

}
```

### Using Action Assets with `PlayerInput`

The [`PlayerInput` component](Components.md#playerinput-component) provides a convenient way to handle input for one or multiple players. It requires you to set up all your actions in an Input Action Asset, which you can then assign to the [`PlayerInput`](Components.md#playerinput-component) component. [`PlayerInput`](Components.md#playerinput-component) can then automatically handle activating action maps and selecting control schemes for you. Check the docs on [GameObject Components for Input](Components.md) to learn mode.

![PlayerInput](Images/PlayerInput.png)
