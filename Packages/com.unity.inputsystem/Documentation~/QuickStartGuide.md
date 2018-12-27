# Quick Start Guide

>NOTE: For information on how to install the new input system, please see [Installation](Installation.md).

## Getting Input Directly From An Input Device

The quickest way to get started in script is to just query state directly from input devices. For example, the following code grabs the gamepad last used by the player and reads out its current state:

```CSharp
using UnityEngine;
using UnityEngine.Experimental.Input;

public class MyPlayerScript : MonoBehaviour
{
    void FixedUpdate()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null)
            return; // No gamepad connected.

        if (gamepad.rightTrigger.wasPressedThisFrame)
        {
            // 'Use' code here
        }

        Vector2 move = gamepad.leftStick.ReadValue();
        // 'Move' code here
    }
}
```

The same approach works for other types of devices, e.g. `Keyboard.current`, `Mouse.current`, `Touchscreen.current`, etc.

# Getting Input Indirectly Through An Input Action

Getting input directly from an input device is quick and convenient but requires a separate path for each type of device and also makes it hard to later change which control on the device leads to which action being taken by the game.

An alternative is to use actions as an intermediary between input devices and the responses they trigger in the game:

```CSharp
using UnityEngine;
using UnityEngine.Experimental.Input;

public class MyPlayerScript : MonoBehaviour
{
    public InputAction useAction;
    public InputAction moveAction;

    public void Awake()
    {
        useAction.performed += OnUse;
        moveAction.performed += OnMove;
    }

    public void OnEnable()
    {
        useAction.Enable();
        moveAction.Enable();
    }

    public void OnDisable()
    {
        useAction.Disable();
        moveAction.Disable();
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

After adding this component to a `GameObject`, the actions on the component can be graphically edited in the inspector. We can add bindings to actions by clicking the plus icon for an action.

![Add Binding To Action In Component](Images/AddBindingToActionInComponent.png)

Double-click the newly added binding to show its properties.

![Double Click Binding On Action In Component](Images/DoubleClickBindingOnActionInComponent.png)

Click the path dropdown on the binding to show the control picker. You can either search for controls by name or navigate to them by going through the hierarchy.

![Edit Binding On Action In Component](Images/EditBindingOnActionInComponent.png)

# Moving The Actions Into An Asset

Unfortunately, as more actions are added, this approach gets unwieldy fast. To simplify handling of the actions, we can move all of them into an asset and use the action asset editor instead of configuring actions directly on a component.

To create the asset, right-click in the Project window or open the `Assets` entry in Unity's main menu. From there, select `Create >> Input Actions`.

![Create Input Actions](Images/CreateInputActions.png)

Give the asset a name (such as `MyPlayerControls`) and double-click the asset to open the action editor.

![Action Editor Window](Images/ActionEditorWindow.png)

The left column contains the "action maps" in the asset. Each map is a collection of actions that can be enabled and disabled in bulk. The middle column contains the actions in the map that is selected. And the right-most column contains the properties of the action or binding selected in the middle column.

Click the plus icon next to "Action Maps" to add new action maps and the plus icon next to "Actions" to add new actions.

![Edit Binding In Action Asset](Images/EditBindingInActionAsset.png)

>NOTE: Edits made in the action asset window are not saved automatically with the project. To save your changes, click `Save Asset` in the window's toolbar. To discard your changes, close the window and choose "Don't Save".

After creating the asset, we could go straight to using it in `MyPlayerScript`. However, there are several helpers available that simplify working with the asset from script.

Select the asset in the Project window and in the inspector, tick `Generate C# Class`. When ticked, several other options become available. Also tick the `Generate Interfaces` checkbox. Finally, hit `Apply`.

![MyPlayerControls Importer Settings](Images/MyPlayerControlsImporterSettings.png)

This will generate a C# script that makes working with the asset a lot simpler. After doing this, we can change `MyPlayerScript` like so:

```CSharp
using UnityEngine;
using UnityEngine.Experimental.Input;

// IGameplayActions is an interface generate from the "gameplay" action map
// we added (note that if you called the action map differently, the name of
// the interface will be different). This was triggered by the "Generate Interfaces"
// checkbox.
public class MyPlayerScript : MonoBehaviour, IGameplayActions
{
    // MyPlayerControls is the C# class that has been generated for us.
    // It wraps around a reference to the .inputactions asset we created
    // and automatically looks up all the maps and actions for us.
    public MyPlayerControls controls;

    public void Awake()
    {
        // Tell the "gameplay" action map that we want to get told about
        // when actions get triggered.
        controls.gameplay.SetCallbacks(this);
    }

    public void OnEnable()
    {
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

With this in place, we can assign the asset we have created to our `MyPlayerScript` component in the editor.

![MyPlayerScript Controls Assigned](Images/MyPlayerScriptControlsAssigned.png)

## Limiting the Set of Supported Devices

Usually, any given game or application will only support a specific set of ways it can be interacted with.

...

# Adding Control Schemes

...

# Adding UI Support

...
