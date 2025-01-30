# Respond to input at runtime

There are two main techniques you can use to respond to Actions in your project. These are to either use **polling** or an **event-driven** approach.

- The **Polling** approach refers to the technique of repeatedly checking the current state of the Actions you are interested in. Typically you would do this in the `Update()` method of a `MonoBehaviour` script.
- The **Event-driven** approach involves creating your own methods in code that are automatically called when an action is performed.

For most common scenarios, especially action games where the user's input should have a continuous effect on an in-game character, **Polling** is usually simpler and easier to implement.

For other situations where input is less frequent and directed to various different GameObjects in your scene, an event-driven approach might be more suitable.


---- (TODO the below info was part of "responding to actions" but is most likely duplicate info found on other pages. need to check before removing.)



### Debugging Actions

To see currently enabled Actions and their bound Controls, use the [Input Debugger](Debugging.md#debugging-actions).

You can also use the [`InputActionVisualizer`](Debugging.md#inputactionvisualizer) component from the Visualizers sample to get an on-screen visualization of an Action's value and Interaction state in real-time.

### Using Actions with multiple players

You can use the same Action definitions for multiple local players (for example, in a local co-op game). For more information, see documentation on the [Player Input Manager](PlayerInputManager.md) component.
