---
uid: input-system-action-assets
---
# Input action assets

An input action asset is an asset which contains a set of [input action](xref:input-system-actions) definitions and their associated [Bindings](xref:input-system-action-bindings) and [control schemes](xref:input-system-action-bindings#control-schemes). These assets have the `.inputactions` file extension and are stored in a plain JSON format.

The input system creates an action asset when you set up the [default project-wide actions](xref:project-wide-actions), but you can also create new action assets directly in the Project window.

For most common scenarios, you do not need to use more than one input action asset. It is usually simpler to configure your project-wide action definition in the Project Settings window.


## Creating input action assets

To create an asset that contains [input actions](xref:input-system-actions) in Unity, right-click in the __Project__ window or go to __Assets > Create > Input Actions__ from Unity's main menu.

## Editing input action assets

To open the Input Actions Editor, double-click an `.inputactions` asset in the Project Browser, or select the __Edit Asset__ button in the Inspector for that asset. You can have more than one editor window open at the same time, but not for the same asset.

This Input Actions Editor is identical to the one that opens in the [Project Settings window](xref:input-system-configuring-input).


## Using input action assets


## Type-safe C# API generation

Input action assets allow you to **generate a C# class** from your action definitions, so you can refer to your actions in a type-safe manner from code. This means you can avoid looking up your actions by string.

### Auto-generating script code for actions

One of the most convenient ways to work with `.inputactions` assets in scripts is to automatically generate a C# wrapper class for them. This provides an easier way to set up callbacks and avoid manually looking up actions and action maps by name.

To enable this option, enable the __Generate C# Class__ property in the input action asset's Inspector, then select __Apply__.

![The input action asset's Inspector window displays the enabled Generate C# Class property with default values for the C# class's file, name, and namespace settings.](Images/FireActionInputAssetInspector.png)

You can optionally choose a path name, class name, and namespace for the generated script, or keep the default values.

This generates a C# script that simplifies working with the asset.

```CSharp
using UnityEngine;
using UnityEngine.InputSystem;

// IGameplayActions is an interface generated from the newly added "gameplay"
// action map, triggered by the "Generate Interfaces" checkbox. Note that if
// you change the default values for the action map, the name of the interface
// will be different.
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
            // Tell the "gameplay" action map that we want to be
            // notified when actions get triggered.
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

> [!NOTE]
> To regenerate the .cs file, right-click the .inputactions asset in the Project window and choose "Reimport".

### Using action assets with `PlayerInput`

The [Player Input](xref:input-system-player-input) component provides a convenient way to handle input for one or multiple players. You can assign your action asset to the Player Input component so that it can then automatically handle activating action maps and selecting control schemes for you.

![The PlayerInput component appears with Player set as the Default Map and the Behavior set to Invoke Unity Events.](Images/PlayerInput.png)

### Modifying input action assets at runtime

There are several ways to modify an input action asset at runtime. Any modifications that you make during Play mode to an input action asset do not persist in the asset after you exit Play mode. This means you can test your application in a realistic way in the Editor without having to worry about inadvertently modifying the asset. For examples on how to modify an input action asset, refer to [Create actions in code](xref:input-system-actions#create-actions-in-code) and [Change Bindings](xref:input-system-action-bindings#change-bindings).


### The default actions asset

> [!NOTE]
> The default actions asset is entirely separate from the [default project-wide actions](xref:project-wide-actions). It is a legacy asset that is included in the package for backwards compatibility.

The Input System package provides an asset called `DefaultInputActions.inputactions` which you can reference directly in your projects like any other Unity asset. The asset is also available in code form through the [`DefaultInputActions`](xref:UnityEngine.InputSystem.DefaultInputActions) class.

```CSharp
void Start()
{
    // Create an instance of the default actions.
    var actions = new DefaultInputActions();
    actions.Player.Look.performed += OnLook;
    actions.Player.Move.performed += OnMove;
    actions.Enable();
}
```
