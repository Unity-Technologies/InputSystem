# Pre-Release Notes

## Overview

This pre-release contains updates to the Input System which simplifies and improves some of the main workflows compared with earlier versions of the Input System package.

Because this is a pre-release, the rest of the documentation included with this version of the package has not yet been updated to reflect these changes. The documentation on this page explains the improvements and differences between these new features, and the previous version of the input system package. The improvements are as follows:

**New project-wide actions**

The Input System now allows you to configure actions in the Project Settings window, in a new Input Actions Settings panel. The actions configured here apply project-wide. This means you no longer need to create an Actions asset and set up a reference to your asset to read input actions. Instead, you can configure your actions in the Project Settings window, and read them directly from your scripts. You can still use Action assets if you like, but for many typical scenarios, they are no longer necessary.

**New default actions**

The new project-wide actions come pre-loaded with some default action maps that contain actions suitable for many typical game scenarios, including basic player character controls, and typical UI interactions. In many cases these are enough to allow you to immediately start using input in your project with no configuration required. You are free to either add to, edit, or delete these default configurations to suit your needs.


**Note**: The new features in this pre-release are **only documented on this page**. When reading the rest of the documentation in this package, please remember that it *will not mention these features*. So, for example, when another page in the documentation discusses **action assets**, or creating a reference to your action asset, you can in many cases use project-wide actions instead, as described on this page.

## Project-wide actions and default actions

Project-wide actions are similar to the actions you would previously define in an actions asset, however instead of being an asset that you create in the Editor, they are stored as part of your project’s settings, and are configured in the Project Settings window.

Compared with the previous workflow of creating an Action asset, and setting up a reference to that asset to access in your code, project-wide actions reduce the number of steps to set up input in your project, and reduces complexity in your project.

![The Input Actions settings panel in the Project Settings window, showing the default player actions.](images/ProjectSettingsInputActions.png)<br/>
*The Input Actions settings panel in the Project Settings window, showing the default player actions.*

The project-wide actions feature has some default action maps set up, which you can add to, modify or delete. They are actions which are useful in typical games, such as moving a player character with WSAD keys or a Joypad stick, pressing a button to jump or interact, as well as common UI controls such as pointing, submitting, or canceling within a user interface.

**Note**: If you’d like to delete all the default actions so that you can start from an empty configuration, you don’t need to delete the actions individually. You can delete the default Action Maps, which deletes all the Actions contained in those maps in one go.

### Reading project-wide actions

You can access the project-wide actions in your script by using the [InputSystem.actions](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_actions) property. For example:

    var myAction = InputSystem.actions.FindAction("Player/Jump");

The above line of code reads the "Jump" action, from the “Player” action map, which is one of the default actions that comes with the new project-wide actions feature.

Unlike Input Action assets, the project-wide actions are stored in your project’s Project Settings folder, so they do not appear in your Project window. The **InputSystem.actions** property is a built-in reference to that asset. This means you can use all the same techniques described throughout the rest of the documentation about [using action assets](Workflow-ActionsAsset.html), but instead of referencing an asset from your project, you can use the **InputSystem.actions property** in your scripts to reference the project-wide actions.

For example, here is the script from the [Action Assets workflow page](Workflow-ActionsAsset.html), adapted to use the project-wide actions, and the default actions in the "Player" action map.

```
using UnityEngine;
using UnityEngine.InputSystem;

public class ExampleScript : MonoBehaviour
{
    // private field to store move action reference
    private InputAction moveAction;

    void Awake()
    {
        // find the "move" action, and keep the reference to it, for use in Update
        moveAction = InputSystem.actions.FindAction("Player/move");
        // for the "jump" action, we add a callback method for when it is performed
        InputSystem.actions.FindAction("Player/jump").performed += OnJump;
    }

    void Update()
    {
        // our update loop polls the "move" action value each frame
        Vector2 moveVector = moveAction.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        // this is the "jump" action callback method
        Debug.Log("Jump!");
    }
}
```

Things to note about the above example script, as compared to the script on the [Action Assets workflow page](Workflow-ActionsAsset.html):

* Because there is a built-in reference to the project-wide actions, you do not need a public field with an assigned asset to get a reference to the actions.

* This script does not enable or disable action maps. Project-wide action maps are enabled by default. This means unlike with Action assets, you do not need to enable individual action maps in your script before being able to use them. You may still want to disable or enable action maps if you want to make use of different types of input in different parts of your project.

### Limitations

Because this is a pre-release, the project-wide actions feature is not yet complete and has some limitations you should be aware of.

**The project-wide actions cannot be referenced in an ActionsAsset field in the inspector.**

You can't assign the project-wide input actions asset to UI fields where you would normally assign an input action asset. For example, if you are using the PlayerInput component, you can’t assign the project wide actions to its "Actions" field. This means if you want to use the PlayerInput component, you must create an Actions asset and set up your input configuration there instead of in the project-wide actions.

**Some features of the project-wide actions editor are different, or missing, compared with the Actions Editor window for actions assets.**

Although the UI to edit the project-wide actions in the Project Settings window is very similar to the Actions Editor for action assets, there are some differences and missing features. In particular, the new project-wide actions editor uses a newer UI system, and therefore there are some cosmetic differences such as different icons, and some workflow features are missing such as some of the keyboard shortcuts. You also cannot yet access the project-wide actions [through a C# wrapper](Workflow-ActionsAsset.html#referencing-the-actions-asset-through-a-c-wrapper).
