# The default project-wide actions

When you [create and assign default project-wide actions](./create-project-wide-actions.md) the Action Asset comes pre-configured with some default Actions such as "Move", "Jump", and more, which suit many common app and game scenarios. They are configured to read input from the most common types of input controller such as Keyboard, Mouse, Gamepad, Touchscreen, and extended reality (XR).

![image alt text](./Images/ProjectSettingsInputActionsSimpleShot.png)
*The Input System Package Project Settings after creating and assigning the default actions*

These default actions mean that in many cases, you can start scripting with the Input System without any configuration by referring to the names of the default actions that are already configured for you. You can also rename and reconfigure the default actions, or delete these default configurations to suit your needs.



### The legacy default Actions Asset

The Input System Package also comes with an asset called `DefaultInputActions.inputactions` containing a default set of Actions. This default actions asset is older than, and entirely separate from the default project-wide actions described above. 

This is a legacy asset that remains included in the package for backward compatibility only, and not recommended for use in new projects. It should not be confused with the default project-wide actions described above.

