---
uid: input-system-virtual-mouse
---

# Use a Virtual Mouse for UI cursor control

Drive uGUI pointer input from gamepads and joysticks with a simulated mouse device.

The [Virtual Mouse](xref:UnityEngine.InputSystem.UI.VirtualMouseInput) component feeds a virtual [Mouse](xref:UnityEngine.InputSystem.Mouse) device that the [UI Input Module](using-ui-input-module.md) uses for point-and-click UI. Start with the introduction, then configure the pointer and actions in your scene.

> [!NOTE]
> The Virtual Mouse component is only compatible with the [Unity UI](https://docs.unity3d.com/Manual/com.unity.ugui.html) (uGUI) system, and not UI Toolkit or IMGUI.

| **Topic** | **Description** |
| :--- | :--- |
| **[Introduction to Virtual Mouse for UI cursor control](introduction-virtual-mouse.md)** | Learn when to use Virtual Mouse instead of navigation-only UI and how it simulates a Mouse device. |
| **[Configure a Virtual Mouse](configure-virtual-mouse-input.md)** | Set up the pointer graphic, Canvas hierarchy, stick actions, and UI Input Module wiring. |
| **[Virtual Mouse component reference](virtual-mouse-component-reference.md)** | Look up `VirtualMouseInput` properties for cursor, axis, and button actions. |

## Additional resources

- [Pointer input UI support](supported-ui-input-types-pointer.md)
- [Navigation input UI support](supported-ui-input-types-navigation.md)
- [Using UI Input Module for UI support](using-ui-input-module.md)
- [Configure UI Input Actions](configure-ui-input-action-map.md)
