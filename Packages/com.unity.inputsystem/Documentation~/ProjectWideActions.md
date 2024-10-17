---
uid: project-wide-actions
---



## Edit project-wide actions

Once you have created and assigned project-wide actions, the Input System Package page in Project Settings displays the **Actions Editor** interface. Read more about how to use the [Actions Editor](ActionsEditor.md) to configure your actions.

![image alt text](./Images/ProjectSettingsInputActionsSimpleShot.png)
*The Input System Package Project Settings after creating and assigning the default actions*


## Using project-wide actions in code

The benefit of assign an Action Asset as the project-wide actions is that you can access the actions directly through the [`InputSystem.actions`](../api/UnityEngine.InputSystem.InputSystem.html) property directly, rather than needing to set up a reference to your Action Asset first.

For example, you can get a reference to an action named "Move" in your project-wide actions using a line of code like this:

```
  InputSystem.actions.FindAction("Move");
```

Project-wide actions are also enabled by default.
