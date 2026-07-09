---
uid: input-system-index
---

# Input System

The **Input System** allows your users to control your game or app using a device, touch, or gestures.

## Introduction

Unity supports input through two separate systems, one older, and one newer.

The older system, which is built-in to the editor, is called the [Input Manager](https://docs.unity3d.com/Manual/class-InputManager.html). The Input Manager is part of the core Unity platform and is the default, if you do not install the Input System package.

This **Input System package** is a newer, more flexible system, which allows you to use any kind of Input Device to control your Unity content. It's intended to be a replacement for Unity's classic Input Manager. It is referred to as the "Input" System Package, or just **The Input System**. To use it, you must [install it into your project using the Package Manager](Installation.md).

> [!TIP]
> During the installation process for the **Input System** package, the installer offers to automatically deactivate the older built-in system.

To get started, refer to the [Workflows](workflows.md) section to decide how to use Input:

* Using actions, which is the recommended workflow.
* Using both actions and the PlayerInput component, which provides features for callbacks and multiplayer.
* Directly read device states with a script for fast prototyping or single-platform applications.

For a demo project, refer to the [Warriors demo](https://github.com/UnityTechnologies/InputSystem_Warriors) on GitHub.

![Screenshot of the Input Actions Editor window displaying the default action map, Actions, and Action Properties. They are displayed in 3 columns side-by-side in that order.](Images/ActionsEditor.png)<br/>
*The Input Actions Editor, displaying some of the default actions that come pre-configured with the Input System package.*
