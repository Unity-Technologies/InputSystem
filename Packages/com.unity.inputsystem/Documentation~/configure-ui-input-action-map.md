# Configure the UI Input Action Map

The default [Project-Wide Actions asset](./about-project-wide-actions.md) comes with a "**UI**" Action Map, which contains all the actions required for UI interaction. To configure the bindings for these actions, use the [Actions Editor](./actions-editor.md). 

To open the Actions Editor:

1. Go to **Project Settings > Input System Package**
1. In the **Action Maps** column, select **UI**.

![ProjectSettingsInputActionsUIActionMap](Images/ProjectSettingsInputActionsUIActionMap.png)


The default [Project-Wide Actions asset](./about-project-wide-actions.md) comes with all the required actions to be compatible with UI Toolkit and Unity UI.

You can modify, add, or remove bindings to the named actions in the UI action map to suit your project, however in order to remain compatible with UI Toolkit, the name of the action map ("**UI**"), the names of the actions it contains, and their respective **Action Types** must remain the same.

To see the specific actions and types that are expected by the [UI Input Module](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html) class, refer to the [UI Action Map reference](ui-action-map-reference).

You can also reset the UI action map to its default bindings by selecting **Reset** from the **More (⋮)** menu, at the top right of the actions editor window. However, this resets both the 'Player' and 'UI' action maps to their default bindings.

To restore functionality to runtime `OnGUI` methods, you can change the **Active Input Handling** setting to "**Both**". Doing this means that Unity processes the input twice which could introduce a small performance impact.

This only affects runtime (play mode) `OnGUI` methods. Editor GUI code is unaffected and continues to receive input events.