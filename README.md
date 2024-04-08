# Unity Input System (Package)

The Unity Input System package is an extension package to the [Unity Platform](https://unity.com/products/unity-platform) which provides a system to configure game actions and access input devices to interact with Unity content. It is intended to be a more powerful, flexible, and configurable replacement to [Unity Input API](https://docs.unity3d.com/ScriptReference/Input.html) (the `UnityEngine.Input` class).

## Prerequisites

The current version of the Input System requires Unity 2019 LTS, Unity 2020 LTS, Unity 2021 or Unity Beta (may be subject to instabilities) and included tooling to compile and run. The recommended way of installing Unity is via [Unity Hub](https://unity3d.com/get-unity/download).

## Getting Started

For instructions on how to get started using the Input System within Unity see [Input System Manual - Installation](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/index.html?subfolder=/manual/Installation.html).

Tutorials on how to use the Input System are available as part of:
- [Unity Learn - Using the Input System in Unity](https://learn.unity.com/project/using-the-input-system-in-unity) - Video tutorials on how to use the Input System in Unity.
- [Warriors Demo Project](https://github.com/UnityTechnologies/InputSystem_Warriors) - A demo project illustrating a wide range of tools and features in the Input System, including local multi-player using different input methods.
- Example projects part of this repository:
    - [Custom Composite](Assets/Samples/CustomComposite) - Shows how to implement and register a custom `InputBindingComposite`.
    - [Custom Device](Assets/Samples/CustomDevice) - Demonstrates how to add and author a custom device.
    - [Custom Device Usages](Assets/Samples/CustomDeviceUsages) - An example of how to tag devices with custom "usages" and how to bind actions specifically to devices with only those usages.
    - [Gamepad Mouse Cursor](Assets/Samples/GamepadMouseCursor) - An example of UI pointer navigation driven from gamepad input.
    - [In-game Hints](Assets/Samples/InGameHints) - Illustrates how to display text in the UI that involves action bindings as well as object interaction.
    - [Input Recorder](Assets/Samples/InputRecorder) - Demonstrates how to use [`InputEventTrace`](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/index.html?subfolder=/api/UnityEngine.InputSystem.LowLevel.InputEventTrace.html) and [`InputRecorder`](./InputRecorder.cs).
    - [On-screen Controls](Assets/Samples/OnScreenControls) - Demonstrates how to set up and use on-screen gamepad-like controls.
    - [Rebinding UI](Assets/Samples/RebindingUI) - Demonstrates how to set up a rebinding UI to reconfigure bindings during run-time.
    - [Simple Demo](Assets/Samples/SimpleDemo) - Shows how to set up a simple character controller using actions, action asset as well as using the `PlayerInput` component.
    - [Simple Multiplayer](Assets/Samples/SimpleMultiplayer) - Demonstrates a basic split-screen local multiplayer setup where players can join by pressing buttons on the supported devices to join the game.
    - [UI vs Game Input](Assets/Samples/UIvsGameInput) - Illustrates how to handle input and resolve ambiguities that arise when overlaying UI elements in the game-view.
    - [Visualizers](Assets/Samples/Visualizers) - Provides various input data visualizations for common devices.

## How to use a released versions of the package within a Unity project

All released versions of the Input System package are available via the Unity Package Manager, see [`Input System Manual - Installation`](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/index.html?subfolder=/manual/Installation.html) for instructions how to fetch the package and compatible versions of samples and install them into your Unity project.

## How to use the latest changes of the package in a Unity project

To test out the latest (unreleased) changes:

1. Clone [develop](https://github.com/Unity-Technologies/InputSystem/tree/develop). The intention is to always keep the `develop` branch in a releasable state, but it reflects current development and may contain bugs or unexpected behavior that was not present in the latest released version.
2. Add the local package to your project by following the steps described in [Unity Manual - Installing a package from a local folder](https://docs.unity3d.com/Manual/upm-ui-local.html) and select `Packages/com.unity.inputsystem/package.json`.

## Recommended way of developing the Input System

1. Clone [develop](https://github.com/Unity-Technologies/InputSystem/tree/develop) or the desired branch or release tag.
2. Open the root folder of the repository in the Unity Editor. This way you have access to tests and samples that are excluded when importing only the package into another Unity project.

During development, run Input System automated tests by selecting `Window > General > Test Runner` to access the Test Runner and select `Run All` for `PlayMode` or `EditMode` tests.

## Contribution & Feedback
This project is developed by Unity Technologies but welcomes user contributions and feedback.

If you have any feedback or questions about Unity's Input System, you are invited to join us on the [Unity Forums](https://forum.unity.com/forums/new-input-system.103/).

If you want to contribute to the development of the Input System see [CONTRIBUTIONS.md](https://github.com/Unity-Technologies/InputSystem/blob/develop/CONTRIBUTIONS.md) for additional information.

## License

This package is distributed under the [Unity Companion License for Unity-dependent projects](LICENSE.md) license with addition of [third party licenses](Third%20Party%20Notices.md) which applies to the [Assets/Samples/RebindingUI](Assets/Samples/RebindingUI) example project specifically.
