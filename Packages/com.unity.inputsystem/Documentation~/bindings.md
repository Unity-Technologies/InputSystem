---
uid: input-system-bindings-landing
---

# Bindings

![A flowchart showing the general workflow of the Input System, with icons representing the different concepts. It starts with the User icon, which then leads into the Input Device and its Controls icon. This then leads into the Action Map and Actions concept. The Input Device and Action Map and Actions icons are collectively grouped under the Binding header. This leads into the final icon representing your action code.](Images/ConceptsOverview.png)

A **binding** represents a connection between an [Action](actions.md) and one or more [Controls](controls.md) identified by a [Control path](./control-paths.md).

| **Topic** | **Description** |
| :--- | :--- |
| **[Introduction to bindings](introduction-to-bindings.md)** | Learn the basic concepts of bindings. |
| **[Binding types](binding-types.md)** | Configure how bindings map to actions with binding types. |
| **[Composite bindings](composite-bindings.md)** | Bindings made up of multiple simple bindings acting together. |
| **[Add, duplicate or delete a binding](add-duplicate-delete-binding.md)** | Learn how to add, duplicate or delete bindings. |
| **[Select a control for binding](select-control-binding.md)** | Learn how to choose a specific control that a binding is bound to, such as a specific button or stick on a gamepad, or a specific keyboard key. |
| **[Edit composite bindings](edit-composite-bindings.md)**| Add, edit, and delete composite bindings in the **Actions Editor** window. |
| **[Group bindings to control schemes](group-binding-to-control-scheme.md)** | Group types of related bindings together according to their control type, so that you can enable or disable groups of bindings |
| **[Binding resolution](binding-resolution.md)** | Learn how the Input Systems resolves binding configurations to currently-connected input devices. |
| **[Restrict binding resolution to a specific device](restrict-binding-resolution-to-device.md)** | Specify which devices a binding should resolve to. |
| **[Binding conflicts](binding-conflicts.md)** | Learn how the Input System resolves conflicting or ambiguous situations, such as when multiple bindings map to the same action. |
| **[Initial state checks](binding-initial-state-checks.md)** | Learn how the Input System deals with if a control is already pressed when an action is enabled, and how to modify this behavior.  |

## Additional resources

- [Actions](actions.md)
- [Control paths](control-paths.md)
- [Control schemes](control-schemes.md)
- [User rebinding at runtime](user-rebinding-runtime.md)
