---
uid: input-system-installation
---
# Installation guide

This page describes how to install and activate the **Input System** package for your Unity Project.

## Install the package

From Unity 6.7, Input System is automatically included for new projects from templates. 

If you've created an empty project, or are upgrading an existing project's input, install the Input System package:

1. Go to **Main Menu** > **Window** > **Package Manager**. 
1. Go to **Unity Registry**. 
1. From the package list, select **Input System**.
1. Select **Install**.
1. Follow any prompts to [enable the backends](#enable-the-new-input-backends).

## Select a back end

The Unity Editor has two back end options: 

- The legacy Input Manager for backwards compatibility.
- The Input System package for new or upgraded projects.

When you install the Input System package in your project, Unity will ask whether you want to enable the new backends. Click **Yes** to enable the new backends and disable the old backends. The Editor restarts to complete the change.

To manually select a back end:

1. Go to **Project Settings** > **Player** > **Other Settings**. 
1. In **Active Input Handling**, select a back end:
    - Input Manager (Old). Builds have the `ENABLE_LEGACY_INPUT_MANAGER=1` C# `#define`.
    - Input System Package (New). Builds have the `ENABLE_INPUT_SYSTEM=1` C# `#define`.
    - Both. Builds have both of the C# `#define`.
1. The Editor restarts with a new back end.

## Compatibility notes for existing projects

Before upgrading existing projects to use Input System, please ensure:

* You're using .NET 4 runtime or newer.
* You're using an Editor version of 2021.3 or newer. For older versions, match the package version to the Editor version as indicated by the **Release** tag in the [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui.html) window.

## Samples and demos

The Input System package includes several samples. To import a sample into your project:

1. Go to the **Package Manager** window.
1. Select the Input System package.
1. Go to the **Samples** tab. 
1. To import a sample, select **Import** next to its name.

For a more comprehensive demo, use the [Warriors](https://github.com/UnityTechnologies/InputSystem_Warriors) project.