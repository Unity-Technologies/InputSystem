---
uid: input-system-virtual-mouse-config
---

# Configure a Virtual Mouse

To configure the Virtual Mouse component with the Unity UI system:

1. Create a UI GameObject with an **Image** component. This GameObject is the mouse pointer. It can help to rename it "_Pointer_".
2. Parent the pointer GameObject as a child of your **Canvas** GameObject that contains the UI which the cursor should operate on.
3. Set the anchor position of the GameObject's `RectTransform` to the bottom left.
4. Ensure your pointer GameObject is the last child of the Canvas so that the cursor draws on top of everything else.
5. Add a **Virtual Mouse** component to the GameObject.
6. Drag the **Image** component of the pointer GameObject into the **Cursor Graphic** field of the Virtual Mouse component.
7. Drag the **Rect Transform** component of the pointer GameObject to the **Cursor Transform** field of the Virtual Mouse component.

> [!NOTE]
> Do not set up gamepads and joysticks for [navigation input](supported-ui-input-types-navigation.md) while using the Virtual Mouse component. If, for example, the Virtual Mouse component is configured to receive input from gamepads, and `Move`, `Submit`, and `Cancel` on the UI Input Module are also linked to the gamepad, then the UI receives input from the gamepad on two channels, and triggers the input twice.

## Control the virtual mouse with the Input System

To configure the input to drive the virtual mouse, do one of the following:

* Add bindings on the various actions (such as **Stick Action**).
* Enable **Use Reference** and link existing actions from an input actions asset.

## Control the system mouse cursor with the virtual mouse

To set the virtual mouse to control the system mouse cursor, set [Cursor Mode](xref:UnityEngine.InputSystem.UI.VirtualMouseInput) to **Hardware Cursor If Available**.

In this mode, the **Cursor Graphic** is hidden when a system mouse is present, and you use [Mouse.WarpCursorPosition](xref:UnityEngine.InputSystem.Mouse) to move the system mouse cursor instead of the software cursor. The transform linked through **Cursor Transform** is not updated.
