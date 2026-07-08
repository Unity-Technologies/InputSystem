---
uid: input-system-about-respond-input
---

# About responding to input

The Input System offers various ways to respond to input at runtime, from the recommended workflow using [actions](actions.md) and [bindings](bindings.md), to more direct techniques such as reading device controls directly.

## Recommended workflow

Working with the Input System's [recommended workflow](workflows.md) in your project involves two phases which you must approach in this order:

1. Configure your project's actions.
2. Implement responses to actions.

### Configure your project's actions

You must first [configure input for your project](setting-up-input.md) as [actions](actions.md), before you can respond to those actions.

In some situations you might find the configuration in the [default project-wide actions](default-actions.md) covers all your needs, and you can go straight to implementing responses to input. In other cases you might want to start with the default configuration and [add or modify actions](configure-actions.md), or you can [start with an empty configuration and define your own](create-empty-action-asset.md).

### Implement responses to actions

Once you have your actions set up, you can implement responses to those actions.

There are two main techniques you can use to respond to actions in your project. These are to either use **polling** or an **event-driven** approach.

- The **polling** approach refers to the technique of repeatedly checking the current state of your actions. Typically you do this in the `Update()` method of a `MonoBehaviour` script. Refer to [polling actions](polling-actions.md) for further information.

- The **event-driven** approach involves creating your own methods in code that are automatically called when an action is performed. Refer to [Set callbacks on actions](set-callbacks-on-actions.md) for further information.

For most common scenarios, especially action games where the user's input has a continuous centralized effect on an in-game character, **polling** is usually simpler and easier to implement. For other situations where input is less continuous, or directed to many different areas in your scene, an event-driven approach might be more appropriate.

## Other workflows

The Input System also allows you to read device states directly, which bypasses many of the features such as actions and bindings. This workflow is suitable for fast prototyping, or single fixed platform scenarios, but is a less flexible workflow because it bypasses some useful Input System features.

Refer to [Read devices directly](read-devices-directly.md) for further information about this workflow.
