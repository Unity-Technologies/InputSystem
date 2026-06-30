---
uid: input-system-pointer-ui-support
---

# Pointer input UI support

A pointer is a position from which clicks and scrolls can trigger to interact with UI elements at the pointer's position. Pointer-type input comes from the following actions:

*  [`point`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule)
* [`leftClick`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule)
* [`rightClick`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule)
* [`middleClick`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule)
* [`scrollWheel`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule)

The UI input model doesn't associate pointers and cursors together. However, it pins the pointer for both mouse and pen input at `1,1` depending on the state of `Cursor.lockState`. The cursor lock behavior is defined by the **Cursor Lock Behavior** property of the [UI Input Module](using-ui-input-module.md).

## Multiple pointer devices

Multiple pointer devices can feed input into a single UI Input Module. [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) devices can have multiple concurrent pointers (each finger contact is one pointer) on a single device.

Because multiple pointer devices can feed into the same set of actions, it's important to set the [action type](about-action-control-types.md#action-type) to [PassThrough](xref:UnityEngine.InputSystem.InputActionType). This ensures that no filtering is applied to input on these actions and that instead every input is relayed as is.

From the perspective of [`InputSystemUIInputModule`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule), each [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) that has one or more controls bound to one of the pointer-type actions is a unique pointer. Also, for each [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) devices, each separate [`TouchControl`](xref:UnityEngine.InputSystem.Controls.TouchControl) that has one or more of its controls bound to the those actions is its own unique pointer as well. Each pointer receives a unique [`pointerId`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.PointerEventData.html#UnityEngine_EventSystems_PointerEventData_pointerId) which generally corresponds to the [`deviceId`](xref:UnityEngine.InputSystem.InputDevice) of the pointer. However, for touch, this will be a combination of [deviceId](xref:UnityEngine.InputSystem.InputDevice) and [`touchId`](xref:UnityEngine.InputSystem.Controls.TouchControl). Use [`ExtendedPointerEventData.touchId`](xref:UnityEngine.InputSystem.UI.ExtendedPointerEventData) to find the ID for a touch event.

If you bind a device to a pointer-type action such as [Left Click](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) without also binding it to [Point](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule), the UI Input Module recognizes the device as not able to point, and attempts to route its input into another pointer. For example, if you bind [Left Click](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) to the `Space` key and [Point](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) to the position of the mouse, then pressing the space bar results in a left-click at the current position of the mouse.

For pointer-type input (and [tracked-type input](supported-ui-input-types-tracked.md)), [`InputSystemUIInputModule`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) sends [`ExtendedPointerEventData`](xref:UnityEngine.InputSystem.UI.ExtendedPointerEventData) instances, which are an extended version of the base `PointerEventData`. These events contain additional data such as the [device](xref:UnityEngine.InputSystem.UI.ExtendedPointerEventData) and [pointer type](xref:UnityEngine.InputSystem.UI.ExtendedPointerEventData) that the Input System has used to generate the event.

### Pointer behavior for multiple devices

The UI Module's [Pointer Behavior](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) property defines how to deal with concurrent input from multiple pointers.

#### Unify mouse and pen input but separate touch and track input
[Single Mouse or Pen But Multi Touch And Track](xref:UnityEngine.InputSystem.UI.UIPointerBehavior) behaves like [Single Unified Pointer](xref:UnityEngine.InputSystem.UI.UIPointerBehavior) for all input that is not classified as touch or tracked input, and behaves like [All Pointers As Is](xref:UnityEngine.InputSystem.UI.UIPointerBehavior) for tracked and touch input.

If concurrent input is received on a [`Mouse`](xref:UnityEngine.InputSystem.Mouse) and [`Pen`](xref:UnityEngine.InputSystem.Pen), for example, the input of both is fed into the same UI pointer instance. The position input of one will overwrite the position of the other.

When input is received from touch or tracked devices, the single unified pointer for mice and pens is removed, including [`IPointerExit`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.IPointerExitHandler.html) events being sent in case the mouse/pen cursor is currently hovering over objects. This is the default behavior.

#### Unify all pointer inputs
[Single Unified Pointer](xref:UnityEngine.InputSystem.UI.UIPointerBehavior) unifies all pointer input so that there is only ever a single pointer. This includes touch and tracked input. This means, for example, that regardless of how many devices feed input into [Point](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule), only the last input in a frame takes effect and becomes the current UI pointer's position.

#### Treat all pointers as separate
[All Pointers As Is](xref:UnityEngine.InputSystem.UI.UIPointerBehavior) treats each input as separate and individual. Any device, including touch and tracked devices that feed input pointer-type actions, is its own pointer (or multiple pointers for touch input). This might mean that there are multiple pointers in the UI, and several objects might be pointed at at the same time.
