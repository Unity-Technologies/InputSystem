---
uid: input-system-project-wide-actions
---
# About project-wide actions

You can assign an individual Action Asset to be available "project-wide", which means the actions within that asset are available more conveniently through the Input System API without needing to set up references to the asset.

When you [assign an Action Asset as project-wide](assign-project-wide-actions.md), it also becomes automatically [preloaded](https://docs.unity3d.com/ScriptReference/PlayerSettings.GetPreloadedAssets.html) when your app starts up, and is kept available until it terminates.

Unless you have specific project requirements that require more than one Action Asset, the recommended workflow is to use a single Action Asset assigned as the project-wide actions.


## Edit project-wide actions

Once you have created and assigned project-wide actions, the Input System Package page in Project Settings displays the **Actions Editor** interface. Read more about how to use the [Actions Editor](actions-editor.md) to configure your actions.

![image alt text](./Images/ProjectSettingsInputActionsSimpleShot.png)
*The Input System Package Project Settings after creating and assigning the default actions*


## Using project-wide actions in code

The benefit of assign an Action Asset as the project-wide actions is that you can access the actions directly through the [`InputSystem.actions`](../api/UnityEngine.InputSystem.InputSystem.html) property directly, rather than needing to set up a reference to your Action Asset first.

For example, you can get a reference to an action named "Move" in your project-wide actions using a line of code like this:

```
  InputSystem.actions.FindAction("Move");
```

Project-wide actions are also enabled by default.
