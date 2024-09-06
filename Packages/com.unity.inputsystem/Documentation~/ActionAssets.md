---
uid: input-system-action-assets
---
# Input Action Assets


# Concept title

First line: Summarize purpose of page in one sentence, starting with
an active verb. No links or formatting.

One to three paragraphs: Introduce the key takeaways of the body content, in
order of appearance. No headings, images, or tables.

Final paragraph (optional): Links to related documentation the user
might be looking for instead.

## Descriptive heading

Use descriptive headings to break up the content and make the page easy to
scan. Organize content logically and introduce ideas gradually.

### Detailed heading

Don't use headings below H3 level.

## Additional resources

- [Link](related-content-on-Unity-owned-platforms)
- [Link](related-content-on-Unity-owned-platforms)

--------------------------------------------


- [Descriptive heading](#descriptive-heading)
  - [Detailed heading](#detailed-heading)
- [Additional resources](#additional-resources)
- [Project-Wide Actions](#project-wide-actions)
- [Using Input Action Assets](#using-input-action-assets)
- [Type-safe C# API Generation](#type-safe-c-api-generation)
  - [Auto-generating script code for Actions](#auto-generating-script-code-for-actions)
  - [Using Action Assets with `PlayerInput`](#using-action-assets-with-playerinput)
  - [Modifying Input Action Assets at runtime](#modifying-input-action-assets-at-runtime)
  - [The Default Actions Asset](#the-default-actions-asset)

The Input System stores your configuration of [Input Actions](Actions.md) and their associated [Bindings](ActionBindings.md), [Action Maps](ActionsEditor.html#configure-action-maps) and [Control Schemes](ActionBindings.md#control-schemes) in an [Action Asset](ActionAssets.md) file.

While it's possible to have more than one Action Asset in a project, most projects only ever need a single Action Asset. 

These Assets have the `.inputactions` file extension and are stored in a plain JSON format.

The input system creates an Action Asset when you set up the [default project-wide actions](ProjectWideActions.md), but you can also create new Action Assets directly in the Project window.

For most common scenarios, you do not need to use more than one Input Action Asset. This is because an Action Asset can contain multiple [Action Maps](ActionsEditor.html#configure-action-maps), which each containing a set of actions relevant to the various parts of your project (such as UI navigation, gameplay, etc).

## Project-Wide Actions

The Input System's **project-wide actions** feature allows you to choose an individual Action Asset to be available project-wide, which means the actions within that asset are available more conveniently through the Input System API without needing to set up references to a specific asset.

When you assign an Action Asset as project-wide, it also becomes automatically [preloaded](https://docs.unity3d.com/ScriptReference/PlayerSettings.GetPreloadedAssets.html) when your app starts up, and is kept available until it terminates.

Unless you have specific project requirements that require more than one Action Asset, the recommended workflow is to use a single Action Asset assigned as the project-wide actions.




## Using Input Action Assets


## Type-safe C# API Generation

Input Action Assets allow you to **generate a C# class** from your action definitions, which allow you to refer to your actions in a type-safe manner from code. This means you can avoid looking up your actions by string.

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

The [Player Input](PlayerInput.md) component provides a convenient way to handle input for one or multiple players. You can assign your Action Asset to the Player Input component so that it can then automatically handle activating Action Maps and selecting Control Schemes for you.

![PlayerInput](Images/PlayerInput.png)

### Modifying Input Action Assets at runtime
There are several ways to modify an Input Action Asset at runtime. Any modifications that you make during Play mode to an Input Action Asset do not persist in the Input Action Asset after you exit Play mode. This means you can test your application in a realistic manner in the Editor without having to worry about inadvertently modifying the asset. For examples on how to modify an Input Action Asset, see the documentation on [Creating Actions in code](Actions.md#creating-actions-in-code) and [Changing Bindings](ActionBindings.md#changing-bindings).


### The Default Actions Asset

An asset called `DefaultInputActions.inputactions` containing a default setup of Actions comes with the Input System Package. You can reference this asset directly in your projects like any other Unity asset. However, the asset is also available in code form through the [`DefaultInputActions`](../api/UnityEngine.InputSystem.DefaultInputActions.html) class.

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

> __Note:__ This default actions asset is older than, and entirely separate from the [default project-wide actions](ProjectWideActions.md). It is a legacy asset that remains included in the package for backward compatibility.
