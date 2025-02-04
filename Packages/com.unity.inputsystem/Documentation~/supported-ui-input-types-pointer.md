# Pointer input UI support

A pointer is a position from which clicks and scrolls can trigger to interact with UI elements at the pointer's position. Pointer-type input comes from the following actions:

*  [`point`](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_point)
* [`leftClick`](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_leftClick)
* [`rightClick`](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_rightClick)
* [`middleClick`](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_middleClick)
* [`scrollWheel`](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_scrollWheel)

The UI input model doesn't associate pointers and cursors together. However, it pins the pointer for both mouse and pen input at `1,1` depending on the state of `Cursor.lockState`. The cursor lock behavior is defined by the **Cursor Lock Behavior** property of the [UI Input Module](using-ui-input-module.md).

## Multiple pointer devices

Multiple pointer devices can feed input into a single UI Input Module. [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) devices can have multiple concurrent pointers (each finger contact is one pointer) on a single device.

Because multiple pointer devices can feed into the same set of actions, it's important to set the [action type](./RespondingToActions.md#action-types) to [PassThrough](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_PassThrough). This ensures that no filtering is applied to input on these actions and that instead every input is relayed as is.

From the perspective of [`InputSystemUIInputModule`](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html), each [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) that has one or more controls bound to one of the pointer-type actions is a unique pointer. Also, for each [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) devices, each separate [`TouchControl`](../api/UnityEngine.InputSystem.Controls.TouchControl.html) that has one or more of its controls bound to the those actions is its own unique pointer as well. Each pointer receives a unique [`pointerId`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.PointerEventData.html#UnityEngine_EventSystems_PointerEventData_pointerId) which generally corresponds to the [`deviceId`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_deviceId) of the pointer. However, for touch, this will be a combination of [deviceId](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_deviceId) and [`touchId`](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_touchId). Use [`ExtendedPointerEventData.touchId`](../api/UnityEngine.InputSystem.UI.ExtendedPointerEventData.html#UnityEngine_InputSystem_UI_ExtendedPointerEventData_touchId) to find the ID for a touch event.

If you bind a device to a pointer-type action such as [Left Click](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_leftClick) without also binding it to [Point](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_point), the UI Input Module recognizes the device as not able to point, and attempts to route its input into another pointer. For example, if you bind [Left Click](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_leftClick) to the `Space` key and [Point](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_point) to the position of the mouse, then pressing the space bar results in a left-click at the current position of the mouse.

For pointer-type input (and [tracked-type input](supported-ui-input-types-tracked.md)), [`InputSystemUIInputModule`](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html) sends [`ExtendedPointerEventData`](../api/UnityEngine.InputSystem.UI.ExtendedPointerEventData.html) instances, which are an extended version of the base `PointerEventData`. These events contain additional data such as the [device](../api/UnityEngine.InputSystem.UI.ExtendedPointerEventData.html#UnityEngine_InputSystem_UI_ExtendedPointerEventData_device) and [pointer type](../api/UnityEngine.InputSystem.UI.ExtendedPointerEventData.html#UnityEngine_InputSystem_UI_ExtendedPointerEventData_pointerType) that the Input System has used to generate the event.

### Pointer behavior for multiple devices

The UI Module's [Pointer Behavior](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_pointerBehavior) property defines how to deal with concurrent input from multiple pointers.

#### Unify mouse and pen input but separate touch and track input
[Single Mouse or Pen But Multi Touch And Track](../api/UnityEngine.InputSystem.UI.UIPointerBehavior.html#UnityEngine_InputSystem_UI_UIPointerBehavior_SingleMouseOrPenButMultiTouchAndTrack) behaves like [Single Unified Pointer](../api/UnityEngine.InputSystem.UI.UIPointerBehavior.html#UnityEngine_InputSystem_UI_UIPointerBehavior_SingleUnifiedPointer) for all input that is not classified as touch or tracked input, and behaves like [All Pointers As Is](../api/UnityEngine.InputSystem.UI.UIPointerBehavior.html#UnityEngine_InputSystem_UI_UIPointerBehavior_AllPointersAsIs) for tracked and touch input.

If concurrent input is received on a [`Mouse`](../api/UnityEngine.InputSystem.Mouse.html) and [`Pen`](../api/UnityEngine.InputSystem.Pen.html), for example, the input of both is fed into the same UI pointer instance. The position input of one will overwrite the position of the other.

When input is received from touch or tracked devices, the single unified pointer for mice and pens is removed, including [`IPointerExit`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.IPointerExitHandler.html) events being sent in case the mouse/pen cursor is currently hovering over objects. This is the default behavior.

#### Unify all pointer inputs
[Single Unified Pointer](../api/UnityEngine.InputSystem.UI.UIPointerBehavior.html#UnityEngine_InputSystem_UI_UIPointerBehavior_SingleUnifiedPointer) unifies all pointer input so that there is only ever a single pointer. This includes touch and tracked input. This means, for example, that regardless of how many devices feed input into [Point](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_point), only the last input in a frame takes effect and becomes the current UI pointer's position.

#### Treat all pointers as separate
[All Pointers As Is](../api/UnityEngine.InputSystem.UI.UIPointerBehavior.html#UnityEngine_InputSystem_UI_UIPointerBehavior_AllPointersAsIs) treats each input as separate and individual. Any device, including touch and tracked devices that feed input pointer-type actions, is its own pointer (or multiple pointers for touch input). This might mean that there are multiple pointers in the UI, and several objects might be pointed at at the same time.
