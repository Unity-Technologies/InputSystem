# The default project-wide actions

When you [create and assign default project-wide actions](CreateActionAsset.md) the Action Asset comes pre-configured with some default Actions such as "Move", "Jump", and more, which suit many common app and game scenarios. They are configured to read input from the most common types of input controller such as Keyboard, Mouse, Gamepad, Touchscreen, and extended reality (XR).

![image alt text](./Images/ProjectSettingsInputActionsSimpleShot.png)
*The Input System Package Project Settings after creating and assigning the default actions*

These default actions mean that in many cases, you can start scripting with the Input System without any configuration by referring to the names of the default actions that are already configured for you. You can also rename and reconfigure the default actions, or delete these default configurations to suit your needs.

If you’d like to delete all the default actions so that you can start from an empty configuration, you don’t need to delete the individual actions one-by-one. You can delete the each Action Map, which deletes all the Actions contained in the maps in one go.

You can also delete all action maps, or reset all the actions back to the default values from the **more** (⋮) menu at the top right of the Input Actions section of the settings window, below the Project Settings window search field.

![The Input Actions **more** menu as displayed in the Project Settings window](images/InputActionsSettingsMoreMenu.png)

> **Note:** this **more** (⋮) menu is not available when the Actions Editor is open in a separate window, it is only present in the Project Settings window.

### The legacy default Actions Asset

The Input System Package also comes with an asset called `DefaultInputActions.inputactions` containing a default setup of Actions. This default actions asset is older than, and entirely separate from the default project-wide actions described above. It is a legacy asset that remains included in the package for backward compatibility. You can reference this asset directly in your projects like any other Unity asset. However, the asset is also available in code form through the [`DefaultInputActions`](../api/UnityEngine.InputSystem.DefaultInputActions.html) class.

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
