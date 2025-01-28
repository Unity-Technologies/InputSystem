# Configure a Virtual Mouse for UI input
To configure the Virtual Mouse component with the Unity UI system:

1. Create a UI GameObject with an **Image** component. This GameObject is the mouse pointer. It can help to rename it "_Pointer_".
2. Parent the pointer GameObject as a child of your **Canvas** GameObject that contains the UI which the cursor should operate on.
3. Set the anchor position of the GameObject's `RectTransform` to the bottom left.
4. Ensure your pointer GameObject is the last child of the Canvas so that the cursor draws on top of everything else.
5. Add a **Virtual Mouse** component to the GameObject.
6. Drag the **Image** component of the pointer GameObject into the **Cursor Graphic** field of the Virtual Mouse component.
7. Drag the **Rect Transform** component of the pointer GameObject to the **Cursor Transform** field of the Virtual Mouse component.

>[!NOTE]
> Do not set up gamepads and joysticks for [navigation input](#navigation-type-input) while using the Virtual Mouse component.  If, for example, the Virtual Mouse component is configured to receive input from gamepads, and `Move`, `Submit`, and `Cancel` on the UI Input Module are also linked to the gamepad, then the UI receives input from the gamepad on two channels, and triggers the input twice.

If you want the virtual mouse to control the system mouse cursor:
Set [Cursor Mode](../api/UnityEngine.InputSystem.UI.VirtualMouseInput.html#UnityEngine_InputSystem_UI_VirtualMouseInput_cursorMode) to **Hardware Cursor If Available**. In this mode, the **Cursor Graphic** is hidden when a system mouse is present and you use [Mouse.WarpCursorPosition](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_WarpCursorPosition_UnityEngine_Vector2_) to move the system mouse cursor instead of the software cursor. The transform linked through **Cursor Transform** is not updated in that case.


To configure the input to drive the virtual mouse, do one of the following:

* Add bindings on the various actions (such as **Stick Action**).
* Enable **Use Reference** and link existing actions from an Input Actions asset.