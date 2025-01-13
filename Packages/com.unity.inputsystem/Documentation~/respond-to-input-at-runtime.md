# Respond to input at runtime

There are two main techniques you can use to respond to Actions in your project. These are to either use **polling** or an **event-driven** approach.

- The **Polling** approach refers to the technique of repeatedly checking the current state of the Actions you are interested in. Typically you would do this in the `Update()` method of a `MonoBehaviour` script.
- The **Event-driven** approach involves creating your own methods in code that are automatically called when an action is performed.

For most common scenarios, especially action games where the user's input should have a continuous effect on an in-game character, **Polling** is usually simpler and easier to implement.

For other situations where input is less frequent and directed to various different GameObjects in your scene, an event-driven approach might be more suitable.


---- (TODO the below info was part of "responding to actions" but is most likely duplicate info found on other pages. need to check before removing.)


### Action types

Each Action can be one of three different [Action types](../api/UnityEngine.InputSystem.InputActionType.html). You can select the Action type in the Input Action editor window, or by specifying the `type` parameter when calling the [`InputAction()`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction__ctor_System_String_UnityEngine_InputSystem_InputActionType_System_String_System_String_System_String_System_String_) constructor. The Action type influences how the Input System processes state changes for the Action. The default Action type is `Value`.

#### Value

This is the default Action type. Use this for any inputs which should track continuous changes to the state of a Control.

 [`Value`](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_Value) type actions continuously monitor all the Controls which are bound to the Action, and then choose the one which is the most actuated to be the Control driving the Action, and report the values from that Control in callbacks, triggered whenever the value changes. If a different bound Control actuated more, then that Control becomes the Control driving the Action, and the Action starts reporting values from that Control. This process is called [conflict resolution](ActionBindings.md#conflicting-inputs). This is useful if you want to allow different Controls to control an Action in the game, but only take input from one Control at the same time.

When the Action initially enables, it performs an [initial state check](ActionBindings.md#initial-state-check) of all bound Controls. If any of them is actuated, the Action then triggers a callback with the current value.

#### Button

This is very similar to [`Value`](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_Value), but [`Button`](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_Button) type Actions can only be bound to [`ButtonControl`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html) Controls, and don't perform an initial state check like [`Value`](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_Value) Actions do (see the Value section above). Use this for inputs that trigger an Action once every time they are pressed. The initial state check is usually not useful in such cases, because it can trigger actions if the button is still held down from a previous press when the Action was enabled.

#### Pass-Through

 [`Pass-Through`](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_PassThrough) Actions bypass the [conflict resolution](ActionBindings.md#conflicting-inputs) process described above for `Value` Actions and don't use the concept of a specific Control driving the Action. Instead, any change to any bound Control triggers a callback with that Control's value. This is useful if you want to process all input from a set of Controls.

### Debugging Actions

To see currently enabled Actions and their bound Controls, use the [Input Debugger](Debugging.md#debugging-actions).

You can also use the [`InputActionVisualizer`](Debugging.md#inputactionvisualizer) component from the Visualizers sample to get an on-screen visualization of an Action's value and Interaction state in real-time.

### Using Actions with multiple players

You can use the same Action definitions for multiple local players (for example, in a local co-op game). For more information, see documentation on the [Player Input Manager](PlayerInputManager.md) component.
