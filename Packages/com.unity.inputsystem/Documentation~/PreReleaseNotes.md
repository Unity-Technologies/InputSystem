# Pre-Release Notes

## Overview

This pre-release contains updates to the Input System which simplify and improve some of the main workflows compared with earlier versions of the Input System package. This page describes the main differences introduced, and assumes you are familiar with the workflow in the previous versions of the Input System package.

## New project-wide actions

The Input System now allows you to configure actions in the Project Settings window, in the new Input Actions Settings panel. The actions configured here are available from anywhere in the project. This means you no longer need to create an Actions asset and set up a reference to your asset to read input actions. Instead, you can configure actions in the Project Settings window, and read them directly from your scripts. You can still use Action assets if you like, but for many typical scenarios, they are no longer necessary.

Project-wide actions are similar to the actions you would previously define in an actions asset, however instead of being an asset that you create in the Editor, they are stored as part of your project’s settings, and are configured in the Project Settings window.

Compared with the previous workflow of creating an Action asset, and setting up a reference to that asset to access in your code, project-wide actions reduce the number of steps to set up input in your project, and reduces complexity in your project.

![The Input Actions settings panel in the Project Settings window, showing the default player actions.](images/ProjectSettingsInputActionsSimpleShot.png)<br/>
*The Input Actions settings panel in the Project Settings window, showing the default player actions.*

## New default actions

The new project-wide actions come pre-configured with some default actions that are suitable for many typical game scenarios, including some basic player character actions, and some typical UI-related actions. In many cases these might be enough to allow you to immediately start using input in your project with no configuration required. You can either add to, edit, or delete these default configurations to suit your needs.

If you’d like to delete all the default actions so that you can start from an empty configuration, you don’t need to delete the actions individually. You can delete the default Action Maps, which deletes all the Actions contained in those maps in one go.

## Scripting with the project-wide actions

Unlike the older Input Action assets, the project-wide actions are stored in your project’s Project Settings folder, so they do not appear in your Project window. The `InputSystem.actions` property allows you to access the project-wide actions. It is a built-in reference to that "hidden" asset. This means instead of referencing an asset from your project, you can use the `InputSystem.actions` property in your scripts to reference the project-wide actions.

To get started quickly using the new project-wide actions, see the [Quickstart Guide](QuickStartGuide.html).

If you used older versions of the Input System package, you might want to note these things about the Quickstart example script, compared with the older workflows in the previous versions of the Input System Package:

* You do not need a public field with an assigned Action asset to get a reference to the actions, because the `InputSystem.actions` always references the project-wide actions.

* The script does not enable or disable action maps. Project-wide action maps are enabled by default. This means unlike with the older Action assets, you do not need to enable individual action maps in your script before being able to use them. You may still want to disable or enable action maps if you want to make use of different types of input in different parts of your project.
