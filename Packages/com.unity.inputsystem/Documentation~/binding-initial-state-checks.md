---
uid: input-system-binding-initial-state-checks
---

# Binding initial state checks

After an action is [enabled](enable-actions.md), it will start reacting to input as it comes in. However, at the time the action is enabled, one or more of the controls that are [bound](./add-duplicate-delete-binding.md) to an action may already have a non-default state, for example if a user was currently pressing a button or pushing a thumb stick.

Using what is referred to as an "initial state check", an Action can be made to respond to such a non-default state as if the state change happened *after* the Action was enabled. The way this works is that in the first input [update](xref:UnityEngine.InputSystem.InputSystem) after the Action was enabled, all its bound controls are checked in turn. If any of them has a non-default state, the Action responds right away.

This check is implicitly enabled for actions whose [Action Type](./configure-action-type.md) is set to **Value**. If, for example, you have a "Move" Action bound to the left stick on the gamepad and the stick is already pushed in a direction when "Move" is enabled, the character will immediately start walking.

By default, actions whose [Action Type](./configure-action-type.md) is set to **Button** or **Pass-Through**, do not perform this check. A button that is pressed when its respective Action is enabled first needs to be released and then pressed again for it to trigger the Action. The initial state check is usually not useful in such cases, because it can trigger actions if the button is still held down from a previous press when the action was enabled.

However, you can manually enable initial state checks on these types of actions by doing the following:

1. Select the action in the [Actions panel](./input-actions-editor-window-reference.md#actions-panel-reference) of the [Actions Editor window](./actions-editor.md)
2. Enable the **Initial State Check** option in the [Actions Properties panel](./action-properties-panel-reference.md) to the right.
