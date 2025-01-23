# Supported input types in the UI Input Module

The UI Input Module supports three types of input:

- [Pointer input](#pointer-input)
- [Navigation input](#navigation-input)
- [Tracked input](#tracked-input)

For each input type, the UI Input Module sources and combines input from a specific set of Actions.

<a name="pointer-input"></a>

## Pointer input
To the UI, a pointer is a position from which clicks and scrolls can be triggered to interact with UI elements at the pointer's position. Pointer-type input is sourced from [point](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_point), [leftClick](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_leftClick), [rightClick](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_rightClick), [middleClick](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_middleClick), and [scrollWheel](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_scrollWheel).


The UI input module does not have an association between pointers and cursors. In general, the UI is oblivious to whether a cursor exists for a particular pointer. However, for mouse and pen input, the UI input module will respect [Cusor.lockState](https://docs.unity3d.com/ScriptReference/Cursor-lockState.html) and pin the pointer position at `(-1,-1)` whenever the cursor is locked. This behavior can be changed through the [Cursor Lock Behavior](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_cursorLockBehavior) property of the [InputSystemUIInputModule](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html).


Multiple pointer Devices can feed input into a single UI input module. Also, in the case of [Touchscreen](../api/UnityEngine.InputSystem.Touchscreen.html), a single Device can have the ability to have multiple concurrent pointers (each finger contact is one pointer).


Because multiple pointer Devices can feed into the same set of Actions, it is important to set the [action type](./RespondingToActions.md#action-types) to [PassThrough](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_PassThrough). This ensures that no filtering is applied to input on these actions and that instead every input is relayed as is.


From the perspective of [InputSystemUIInputModule](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html), each [InputDevice](../api/UnityEngine.InputSystem.InputDevice.html) that has one or more controls bound to one of the pointer-type actions is considered a unique pointer. Also, for each [Touchscreen](../api/UnityEngine.InputSystem.Touchscreen.html) devices, each separate [TouchControl](../api/UnityEngine.InputSystem.Controls.TouchControl.html) that has one or more of its controls bound to the those actions is considered its own unique pointer as well. Each pointer receives a unique [pointerId](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.PointerEventData.html#UnityEngine_EventSystems_PointerEventData_pointerId) which generally corresponds to the [deviceId](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_deviceId) of the pointer. However, for touch, this will be a combination of [deviceId](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_deviceId) and [touchId](../api/UnityEngine.InputSystem.Controls.TouchControl.html#UnityEngine_InputSystem_Controls_TouchControl_touchId). Use [ExtendedPointerEventData.touchId](../api/UnityEngine.InputSystem.UI.ExtendedPointerEventData.html#UnityEngine_InputSystem_UI_ExtendedPointerEventData_touchId) to find the ID for a touch event.

To influence how the input module deals with concurrent input from multiple pointers, use the UI Input Module’s [**Pointer Behavior**](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_pointerBehavior) setting.

If you bind a device to a pointer-type action such as [Left Click](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_leftClick) without also binding it to [Point](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_point), the UI input module will recognize the device as not being able to point and try to route its input into that of another pointer. For example, if you bind [Left Click](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_leftClick) to the `Space` key and [Point](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_point) to the position of the mouse, then pressing the space bar will result in a left click at the current position of the mouse.

For pointer-type input (as well as for [tracked-type input](#tracked-type-input)), [InputSystemUIInputModule](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html) will send [ExtendedPointerEventData](../api/UnityEngine.InputSystem.UI.ExtendedPointerEventData.html) instances which are an extended version of the base `PointerEventData`. These events contain additional data such as the [device](../api/UnityEngine.InputSystem.UI.ExtendedPointerEventData.html#UnityEngine_InputSystem_UI_ExtendedPointerEventData_device) and [pointer type](../api/UnityEngine.InputSystem.UI.ExtendedPointerEventData.html#UnityEngine_InputSystem_UI_ExtendedPointerEventData_pointerType) which the event has been generated from.

<a name="navigation-input"></a>

## Navigation input

Navigation-type input controls the current selection based on motion read from the [move](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_move) action. Additionally, input from
[submit](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_submit) will trigger `ISubmitHandler` on the currently selected object and
[cancel](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_cancel) will trigger `ICancelHandler` on it.

Unlike with [pointer-type](#pointer-type-input), where multiple pointer inputs may exist concurrently (think two touches or left- and right-hand tracked input), navigation-type input does not have multiple concurrent instances. In other words, only a single [move](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_move) vector and a single [submit](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_submit) and [cancel](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_cancel) input will be processed by the UI module each frame. However, these inputs need not necessarily come from one single Device always. Arbitrary many inputs can be bound to the respective actions.

While, [move](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_move) should be set to [PassThrough](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_PassThrough) Action type, it is important that [submit](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_submit) and
[cancel](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_cancel) be set to the [Button](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_Button) Action type.

Navigation input is non-positional, that is, unlike with pointer-type input, there is no screen position associcated with these actions. Rather, navigation actions always operate on the current selection.

<a name="tracked-input"></a>


## Tracked input

Input from [tracked devices](../api/UnityEngine.InputSystem.TrackedDevice.html) such as [XR controllers](../api/UnityEngine.InputSystem.XR.XRController.html) and [HMDs](../api/UnityEngine.InputSystem.XR.XRHMD.html) essentially behaves like [pointer-type input](#pointer-type-input). The main difference is that the world-space device position and orientation sourced from  [trackedDevicePosition](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_trackedDevicePosition) and  [trackedDeviceOrientation](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_trackedDeviceOrientation) is translated into a screen-space position via raycasting.


> **Important:**
>Because multiple tracked Devices can feed into the same set of Actions, it is important to set the [action type](./RespondingToActions.md#action-types) to [PassThrough](../api/UnityEngine.InputSystem.InputActionType.html#UnityEngine_InputSystem_InputActionType_PassThrough). This ensures that no filtering is applied to input on these actions and that instead every input is relayed as is.

For this raycasting to work, you need to add [TrackedDeviceRaycaster](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html) to the `GameObject` that has the UI's `Canvas` component. This `GameObject` will usually have a `GraphicRaycaster` component which, however, only works for 2D screen-space raycasting. You can put [TrackedDeviceRaycaster](../api/UnityEngine.InputSystem.UI.TrackedDeviceRaycaster.html) alongside `GraphicRaycaster` and both can be enabled at the same time without advserse effect.

![TrackedDeviceRayster Add Component](Images/TrackedDeviceRaycasterComponentMenu.png)

![TrackedDeviceRayster Properties](Images/TrackedDeviceRaycaster.png)

Clicks on tracked devices do not differ from other [pointer-type input](#pointer-type-input). Therefore, actions such as [Left Click](../api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html#UnityEngine_InputSystem_UI_InputSystemUIInputModule_leftClick) work for tracked devices just like they work for other pointers.