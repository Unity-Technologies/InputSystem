---
uid: input-system-action-type-ref
---

# Action types reference

With an action selected in the [Actions Editor window](./actions-editor.md), the **Action Type** setting in the [actions panel](./input-actions-editor-window-reference.md#actions-panel-reference) allows you to [select the action type](./configure-action-type.md) from the following options:


| Value                         | Description                    |
| :---------------------------- | :----------------------------- |
| **Button**                    | Use this for device controls such as keyboard keys, mouse clicks, or gamepad buttons, which have only an on/off state, and no gradual value changes. Provides phase information and conflict resolution. |
| **Value**                     | Use this for device controls such as mouse movement, a joystick or gamepad stick, or device orientation that provides gradually changing input over a range of values. Provides phase information and conflict resolution. |
| **Pass Through**              | Use this for the same types as **Value**, but this type provides no phase information or conflict resolution. |

### Phase information and conflict resolution

For **Button** or **Value** action types, the Input System also provides data about the action such as whether it has started and stopped (known as the **Phase** of the action), and [conflict resolution](./binding-conflicts.md) in situations where you have mapped multiple bindings to the same action.

For **Pass Through** action types, the Input System only provides basic information about the values incoming from the device controls bound to it, and does not provide the extra data relating to the phase of the action, nor does it perform [conflict resolution](./binding-conflicts.md).

Because pass-through actions don't perform conflict resolution, it means they don't use concept of a specific control driving the action. Instead, any change to any of the controls bound to the action triggers a callback with that Control's value. This is useful if you want to process all input from a set of controls at once on the same action, rather than only the most actuated from the set.
