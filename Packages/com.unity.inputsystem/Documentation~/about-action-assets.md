
# About action assets

The Input System stores your configuration of [Input Actions](Actions.md) and their associated [Bindings](ActionBindings.md), [Action Maps](ActionsEditor.html#configure-action-maps) and [Control Schemes](ActionBindings.md#control-schemes) in an [Action Asset](ActionAssets.md) file. These Assets have the `.inputactions` file extension and are stored in a plain JSON format.

The input system creates an Action Asset when you set up the [default project-wide actions](ProjectWideActions.md), but you can also create new Action Assets directly in the Project window.

For most common scenarios, you do not need to use more than one Input Action Asset. This is because an Action Asset can contain multiple [Action Maps](ActionsEditor.html#configure-action-maps), which each containing a set of actions relevant to the various parts of your project (such as UI navigation, gameplay, etc).

## Project-Wide Actions

You can assign an individual Action Asset to be available "project-wide", which means the actions within that asset are available more conveniently through the Input System API without needing to set up references to the asset.

When you [assign an Action Asset as project-wide](assign-project-wide-actions.md), it also becomes automatically [preloaded](https://docs.unity3d.com/ScriptReference/PlayerSettings.GetPreloadedAssets.html) when your app starts up, and is kept available until it terminates.

Unless you have specific project requirements that require more than one Action Asset, the recommended workflow is to use a single Action Asset assigned as the project-wide actions.

## Modifying Input Action Assets at runtime
There are several ways to modify an Input Action Asset at runtime. Any modifications that you make during Play mode to an Input Action Asset do not persist in the Input Action Asset after you exit Play mode. This means you can test your application in a realistic manner in the Editor without having to worry about inadvertently modifying the asset. For examples on how to modify an Input Action Asset, see the documentation on [Creating Actions in code](Actions.md#creating-actions-in-code) and [Changing Bindings](ActionBindings.md#changing-bindings).


