# Configure Action type

When you select an Action in the [Actions Editor Window](./actions-editor.md), you can edit its properties in the right-hand pane of the window.

The first of these properties is the Action Type.

![Action Properties](Images/ActionProperties.png)

 The Action type influences how the Input System processes state changes for the Action, and relate to whether this action represents a discrete on/off button-style interaction or a value that can change gradually over time.

The Action Type setting allows you to choose between **Button**, **Value** or **Pass Through**. The default Action type is Value.

For device controls such as keyboard keys, mouse clicks, or gamepad buttons, which have only an on/off state, and no gradual value changes, select **Button**.

For device controls such as mouse movement, a joystick or gamepad stick, or device orientation that provides gradually changing input over a range of values, select **Value**.

If you select **Button** or **Value* as your Action Type, the Input System also provides data about the action such as whether it has started and stopped (known as the **Phase** of the action), and [conflict resolution](./dealing-with-binding-conflicts.md) in situations where you have mapped multiple bindings to the same action.

The third option, **Pass Through**, is also a value type, and as such is suitable for the same types of device controls as described for **Value**. The difference is that if your action is set to PassThrough, the Input System only provides basic information about the values incoming from the device controls bound to it, and does not provide the extra data relating to the phase of the action, nor does it perform [conflict resolution](./dealing-with-binding-conflicts.md).

Because pass-through actions don't perform conflict resolution, it means they don't use concept of a specific control driving the action. Instead, any change to any of the controls bound to the action triggers a callback with that Control's value. This is useful if you want to process all input from a set of controls, rather than only the most actuated from the set.
