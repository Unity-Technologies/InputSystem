---
uid: input-system-action-assets
---
# Input Action Assets

* [Creating Action Assets](#creating-input-action-assets)
* [Editing Action Assets](#editing-input-action-assets)
* [Using Action Assets](#using-input-action-assets)

An Input Action Asset is an Asset which contains [Input Actions](Actions.md) and their associated [Bindings](ActionBindings.md) and [Control Schemes](ActionBindings.md#control-schemes). These Assets have the `.inputactions` file extension and are stored in a plain JSON format.

## Creating Input Action Assets

To create an Asset that contains [Input Actions](Actions.md) in Unity, right-click in the __Project__ window or go to __Assets > Create > Input Actions__ from Unity's main menu.

## Editing Input Action Assets

To bring up the Action editor, double-click an `.inputactions` Asset in the Project Browser, or select the __Edit Asset__ button in the Inspector for that Asset. You can have more than one editor window open at the same time, but not for the same Asset.

![Action Editor Window](Images/MyGameActions.png)

The Action editor appears as a separate window, which you can also dock into Unity's main UI.

>__Note__: For details about how Action Maps, Actions, and Bindings work, see documentation on [Actions](Actions.md).

By default, Unity doesn't save edits you make in the Action Asset window when you save the Project. To save your changes, select __Save Asset__ in the window's toolbar. To discard your changes, close the window and choose __Don't Save__ when prompted. Alternatively, you can toggle auto-saving on by enabling the __Auto-Save__ checkbox in the toolbar. This saves any changes to that Asset.

>__Note__: This setting affects all `.inputactions` Assets, and persists across Unity Editor sessions.




## Using Input Action Assets

### Auto-generating script code for Actions

One of the most convenient ways to work with `.inputactions` Assets in scripts is to automatically generate a C# wrapper class for them. This removes the need to manually look up Actions and Action Maps using their names, and also provides an easier way to set up callbacks.

To enable this option, tick the __Generate C# Class__ checkbox in the importer properties in the Inspector of the `.inputactions` Asset, then select __Apply__.

![MyPlayerControls Importer Settings](Images/FireActionInputAssetInspector.png)

You can optionally choose a path name, class name, and namespace for the generated script, or keep the default values.

This generates a C# script that simplifies working with the Asset.

```CSharp
using UnityEngine;
using UnityEngine.InputSystem;

// IGameplayActions is an interface generated from the "gameplay" action map
// we added (note that if you called the action map differently, the name of
// the interface will be different). This was triggered by the "Generate Interfaces"
// checkbox.
public class MyPlayerScript : MonoBehaviour, IGameplayActions
{
    // MyPlayerControls is the C# class that Unity generated.
    // It encapsulates the data from the .inputactions asset we created
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

>__Note__: To regenerate the .cs file, right-click the .inputactions asset in the Project Browser and choose "Reimport".

### Using Action Assets with `PlayerInput`

The [Player Input](PlayerInput.md) component provides a convenient way to handle input for one or multiple players. It requires you to set up all your Actions in an Input Action Asset, which you can then assign to the Player Input component. The Player Input component can then automatically handle activating Action Maps and selecting Control Schemes for you.

![PlayerInput](Images/PlayerInput.png)

### Modifying Input Action Assets at runtime
There are several ways to modify an Input Action Asset at runtime. Any modifications that you make during Play mode to an Input Action Asset do not persist in the Input Action Asset after you exit Play mode. This means you can test your application in a realistic manner in the Editor without having to worry about inadvertently modifying the asset. For examples on how to modify an Input Action Asset, see the documentation on [Creating Actions in code](Actions.md#creating-actions-in-code) and [Changing Bindings](ActionBindings.md#changing-bindings).
