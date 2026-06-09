---
uid: input-system-controls
---

# Controls

![A flowchart showing the general workflow of the Input System, with icons representing the different concepts. It starts with the User icon, which then leads into the Input Device and its Controls icon. This then leads into the Action Map and Actions concept. The Input Device and Action Map and Actions icons are collectively grouped under the Binding header. This leads into the final icon representing your action code.](Images/ConceptsOverview.png)

A **control** is a part of a [device](devices.md) that sends values to the Input System when [actuated](control-actuation.md), such as the buttons and sticks on a gamepad, or the keys on a keyboard.


| **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[Introduction to controls](introduction-to-controls.md)** | An introduction to the concept of controls. |
| **[Control hierarchies](control-hierarchies.md)** | Learn about how controls are arranged hierarchically.  |
| **[Control types reference](control-types-reference.md)** | The types of control defined in the Input System. |
| **[Control usages](control-usages.md)** | Understand what a control usage is. |
| **[Control paths](control-paths.md)** | Learn about control paths and how to use them to refer to controls.  |
| **[Control state](control-state.md)** | Details about how a control's state is stored and accessed. |
| **[Record control state history](record-control-state-history.md)** | How to record a control's state history over time. |
| **[Control actuation](control-actuation.md)** | Whether or not a control is currently being used by the user. |
| **[Noisy controls](noisy-controls.md)** | Controls which can change value without any actual or intentional user interaction such as the accelerometer. |
| **[Synthetic controls](synthetic-controls.md)** | A type of virtual control with values synthesized from input from a physical control on the device.  |
| **[Optimize controls](optimize-controls.md)** | Detailed information about increasing input performance in some specialized scenarios. |

## Additional resources

- [Bindings](bindings.md)
- [Devices](devices.md)
- [Layouts](layouts.md)
- [Device states](device-states.md)
