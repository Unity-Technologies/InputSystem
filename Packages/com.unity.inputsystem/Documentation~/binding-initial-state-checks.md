# Binding initial state checks

After an Action is [enabled](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_enabled), it will start reacting to input as it comes in. However, at the time the Action is enabled, one or more of the Controls that are [bound](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_controls) to an action may already have a non-default state at that point.
x
Using what is referred to as an "initial state check", an Action can be made to respond to such a non-default state as if the state change happened *after* the Action was enabled. The way this works is that in the first input [update](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_Update_) after the Action was enabled, all its bound controls are checked in turn. If any of them has a non-default state, the Action responds right away.

This check is implicitly enabled for [Value](RespondingToActions.md#value) actions. If, for example, you have a `Move` Action bound to the left stick on the gamepad and the stick is already pushed in a direction when `Move` is enabled, the character will immediately start walking.

By default, [Button](RespondingToActions.md#button) and [Pass-Through](RespondingToActions.md#pass-through) type Actions, do not perform this check. A button that is pressed when its respective Action is enabled first needs to be released and then pressed again for it to trigger the Action.

However, you can manually enable initial state checks on these types of Actions using the checkbox in the editor:

![Initial State Check](./Images/InitialStateCheck.png)
