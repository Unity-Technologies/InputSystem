---
uid: input-system-workflows
---

# Input System Workflows

There are multiple ways to use the Input System, however the primary and recommended workflow is to use the **Input Actions** panel in the **Project Settings window** to configure your project-wide Actions and Bindings, get references to those Actions in your code's Start method, then read the values for those actions in your Update method.

There are other workflows which can suit more unusual situations, for example you can use the **PlayerInput component** together with Actions and Bindings which adds a further layer of abstraction, allowing you to connect actions to your event handlers without requiring any intermediate code, and easily handle multiplayer scenarios.

You can choose to configure Actions and Bindings in the Editor UI, or you can set up everything through scripting. Or you can take a more direct approach by omitting the Actions and Bindings features altogether and instead use script to directly read the state of devices.

The descriptions below describe these main workflows and link to more detailed description of them.

|Workflows   |Description   |
|---|---|
|[**Using Actions**](Workflow-Actions.md)|This is the **recommended** workflow for most situations. In this workflow, you use the [Actions Editor window](./ActionsEditor.md) to configure sets of actions and bindings, then set up references and read the values for those actions in your code.|
|[**Using Actions and the PlayerInput Component**](Workflow-PlayerInput.md)|This workflow provides extra features that allow you to connect up **callbacks** directly from Actions to your own callback handler methods, removing the need to deal with Action references in your code. It also provides features that are useful in **local multiplayer** scenarios such as device assignment and split-screen functionality.|
|[**Directly read device states**](Workflow-Direct.md)|This workflow is a simplified, script-only approach which bypasses the Actions and Bindings features entirely. Instead your script explicitly references specific device controls (such as "left gamepad stick") and reads the values directly. This is suitable for **fast prototyping**, or single fixed platform scenarios. It is a **less flexible** workflow because it bypasses some of the main input system features|



> **Note**: Because the Input System has multiple workflows, the code samples used throughout this documentation also vary, often demonstrating techniques using various workflows. For example, some code samples may use Action references, and some may use the workflow of reading input directly from devices.
