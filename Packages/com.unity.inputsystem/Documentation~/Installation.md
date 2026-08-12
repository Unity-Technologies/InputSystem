---
uid: input-system-installation
---
# Install the Input System package

Install the Input System package and enable the input back end for your project.

## Install the package

Unity 6.1 and later include the Input System package in projects created from some templates.

If your project doesn't include the Input System package, you can install it:

1. In the main menu, go to **Window** > **Package Manager**.
1. Go to **Unity Registry**.
1. From the package list, select **Input System**.
1. Select **Install**.
1. Follow any prompts to [enable the backends](#select-a-back-end).

## Select a back end

The Unity Editor has two back end options:

- The Input Manager for backwards compatibility.
- The Input System package for new or upgraded projects.

When you install the Input System package in your project, Unity asks whether to enable the Input System back ends. Select **Yes** to enable the Input System back ends and disable the Input Manager back ends. The Editor restarts to complete the change.

To manually select a back end:

1. In the main menu, go to **Edit** > **Project Settings** > **Player**.
1. In the **Other Settings** section, set **Active Input Handling** to select a back end:
    - Input Manager (Old): Builds have the `ENABLE_LEGACY_INPUT_MANAGER=1` C# `#define`.
    - Input System Package (New): Builds have the `ENABLE_INPUT_SYSTEM=1` C# `#define`.
    - Both: Builds have both of the C# `#define`.

The Editor restarts with a new back end.

## Import samples and demos

The Input System package includes several samples. To import a sample into your project:

1. Go to **Window** > **Package Manager**.
1. Select the Input System package.
1. Select **Samples**.
1. To import a sample, select **Import** next to its name.

For a more comprehensive demo, use the [Warriors](https://github.com/UnityTechnologies/InputSystem_Warriors) project.

## Additional resources

* [Quickstart guide](quick-start-guide.md)
* [Migrate from the old input system](migrate-from-old-input-system.md)
* [Input System workflows](Workflows.md)
