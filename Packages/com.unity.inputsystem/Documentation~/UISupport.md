---
uid: input-system-ui-support
---
# UI support

## Overview and compatibility

Unity has [various UI solutions](https://docs.unity3d.com/Manual/UIToolkits.html). The Input System package's compatibility and workflow with these solutions varies depending on which UI solution you are using, and which version of Unity you are using.

In some cases you must use the **UI Input Module** (a component supplied in the Input System package) to define which actions are passed through from the Input System to the UI.

The three main UI solutions are **UI Toolkit**, **Unity UI**, and **IMGUI**. The compatibility and workflow for each of these are as follows:

**For [**UI Toolkit**](https://docs.unity3d.com/Manual/UIElements.html), also known as "UI Elements" (an XML/CSS style UI solution):**

- From Unity 2023.2 and onwards, the UI actions defined in the default [project-wide actions](xref:project-wide-actions) directly map to UI Toolkit input. You do not need to use the UI Input Module component.</br></br>
- In versions of Unity prior to 2023.2, you must use the UI Input Module component to define which actions are passed through from the Input System to the UI.
- Refer to UI Toolkit [Runtime UI event system and input handling](https://docs.unity3d.com/Manual/UIE-Runtime-Event-System.html) for more information on how to configure UI Toolkit input.

**For [**Unity UI**](https://docs.unity3d.com/Packages/com.unity.ugui@latest), also known as "uGUI" (a GameObject and Component style UI solution):**

When using Unity UI (uGUI), you must always use the UI Input Module component to define which actions are passed through from the Input System to the UI.

**For [**IMGUI**](https://docs.unity3d.com/Manual/GUIScriptingGuide.html) (a script-based "Immediate Mode" UI using the [`OnGUI`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnGUI.html) method):**

The Input System package is **not compatible** with IMGUI, however you can still use the Input System for other parts of your project such as gameplay. See the [Immediate Mode GUI](#immediate-mode-gui) section for more information.

The compatibility above is summarized in the following table:

UI Solution|Compatible|UI Input Module component
-|-|-
UI Toolkit (2023.2+)|Yes|Not required
UI Toolkit (pre 2023.2)|Yes|Required
Unity UI (uGUI)|Yes|Required
IMGUI|No|n/a


## Setting up UI input

The default [project-wide actions](xref:project-wide-actions) comes with a "**UI**" Action Map, that contains all the actions required for UI interaction (shown in the image below). You can configure the bindings for these actions in the [Actions Editor](xref:input-system-configuring-input). Go to **Project Settings > Input System Package**, then select "**UI**" in the Action Maps column.

![ProjectSettingsInputActionsUIActionMap](Images/ProjectSettingsInputActionsUIActionMap.png)

## Required Actions for UI

The default project-wide actions comes with all the required actions to be compatible with UI Toolkit and Unity UI.

You can modify, add, or remove bindings to the named actions in the UI action map to suit your project, however in order to remain compatible with UI Toolkit, the name of the action map ("**UI**"), the names of the actions it contains, and their respective **Action Types** must remain the same.

These specific actions and types, which are expected by the [UI Input Module](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) class, are as follows:

**Action**|**Action Type**|**Control Type**|**Description**
-|-|-|-
Navigate|PassThrough|Vector2|A vector used to select the currently active UI [selectable](https://docs.unity3d.com/Manual/script-Selectable.html) in gamepad or arrow-key [navigation-type input](#navigation-type-input).
Submit|Button|Button|Submits the currently selected UI [selectable](https://docs.unity3d.com/Manual/script-Selectable.html) in [navigation-type input](#navigation-type-input)
Cancel|Button|Button|Exits any interaction with the currently selected UI [selectable](https://docs.unity3d.com/Manual/script-Selectable.html) in [navigation-type input](#navigation-type-input)
Point|PassThrough|Vector2|A 2D screen position. The cursor for [pointer-type](#pointer-type-input) interaction.
Click|PassThrough|Button|The primary button for [pointer-type](#pointer-type-input) interaction.
RightClick|PassThrough|Button|The secondary button for [pointer-type](#pointer-type-input) interaction.
MiddleClick|PassThrough|Button|The middle button for [pointer-type](#pointer-type-input) interaction.
ScrollWheel|PassThrough|Vector2|The scrolling gesture for [pointer-type](#pointer-type-input) interaction.
Tracked Device Position|PassThrough|Vector3|A 3D position of one or multiple spatial tracking devices, such as XR hand controllers. In combination with Tracked Device Orientation, this allows XR-style UI interactions by pointing at UI [selectables](https://docs.unity3d.com/Manual/script-Selectable.html) in space. See [tracked-type input](#tracked-type-input).
Tracked Device Orientation|PassThrough|Quaternion|a `Quaternion` representing the rotation of one or multiple spatial tracking devices, such as XR hand controllers. In combination with [Tracked Device Position](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.trackedDevicePosition), this allows XR-style UI interactions by pointing at UI [selectables](https://docs.unity3d.com/Manual/script-Selectable.html) in space. See [tracked-type input](#tracked-type-input).

You can also reset the UI action map to its default bindings by selecting **Reset** from the **More (⋮)** menu, at the top right of the actions editor window. However, this will reset both the 'Player' and 'UI' action maps to their default bindings.

## The UI Input Module component

When working with Unity UI (uGUI), or when using UI Toolkit in versions of Unity prior to Unity 2023.2, you must use the **UI Input Module** component which defines which actions are passed through to your UI, as well as some other UI-related input settings.

> [!NOTE]
> If you have an instance of the [Input System UI Input Module](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) component in your scene, the settings on that component takes priority and are used instead of the UI settings in your project-wide actions. Also, The UI action map will be enabled, along with the default action map specified on any UI Input Module component in the scene.

The UI Input module is implemented in the class [`InputSystemUIInputModule`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule).

### Using the UI Input Module

The UI Input Module is a component which you must add to a GameObject in your scene in order for your UI to receive input from the Input System. To do this:

1. Create a new empty GameObject
2. Click [**Add Component**](https://docs.unity3d.com/Manual/UsingComponents.html) in the inspector
3. In the search field displayed, type `input system ui`.
4. Select **Input System UI Input Module** to add it to the GameObject.

    ![The Add Component search bar displays 'input system ui' to highlight the 'Input System UI Input Module' component.](Images/InputSystemUIInputModuleAdd.png){height="220" width="282"}


### UI Input Module properties

You can use the following properties to configure [InputSystemUIInputModule](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule):

|**Property**|**Description**|
|--------|-----------|
|[Move Repeat Delay](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.moveRepeatDelay)|The initial delay (in seconds) between generating an initial [IMoveHandler.OnMove](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.IMoveHandler.html) navigation event and generating repeated navigation events when the __Move__ Action stays actuated.|
|[Move Repeat Rate](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.moveRepeatDelay)|The interval (in seconds) between generating repeat navigation events when the __Move__ Action stays actuated. Note that this is capped by the frame rate; there will not be more than one move repeat event each frame so if the frame rate dips below the repeat rate, the effective repeat rate will be lower than this setting.|
|[Actions Asset](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.actionsAsset)|An [Input Action Asset](xref:input-system-action-assets) containing all the Actions to control the UI. You can choose which Actions in the Asset correspond to which UI inputs using the following properties.<br><br>By default, this references a built-in Asset named *DefaultInputActions*, which contains common default Actions for driving UI. If you want to set up your own Actions, [create a custom Input Action Asset](xref:input-system-action-assets#creating-input-action-assets) and assign it here. When you assign a new Asset reference to this field in the Inspector, the Editor attempts to automatically map Actions to UI inputs based on common naming conventions.|
|[Deselect on Background Click](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.deselectOnBackgroundClick)|By default, when the pointer is clicked and does not hit any `GameObject`, the current selection is cleared. This, however, can get in the way of keyboard and gamepad navigation which will want to work off the currently selected object. To prevent automatic deselection, set this property to false.|
|[Pointer Behavior](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.pointerBehavior)|How to deal with multiple pointers feeding input into the UI. See [pointer-type input](#pointer-type-input).|
|[Cursor Lock Behavior](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.cursorLockBehavior)|Controls the origin point of UI raycasts when the cursor is locked. |


### How the bindings work

The UI input module can deal with three different types of input:

- Pointer-type input
- Navigation-type input
- Tracked-type input

For each of these types of input, input is sourced and combined from a specific set of Actions as detailed below.

#### Pointer-type input

To the UI, a pointer is a position from which clicks and scrolls can be triggered to interact with UI elements at the pointer's position. Pointer-type input is sourced from [point](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.point), [leftClick](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.leftClick), [rightClick](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.rightClick), [middleClick](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.middleClick), and [scrollWheel](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.scrollWheel).

The UI input module does not have an association between pointers and cursors. In general, the UI is oblivious to whether a cursor exists for a particular pointer. However, for mouse and pen input, the UI input module will respect [Cursor.lockState](https://docs.unity3d.com/ScriptReference/Cursor-lockState.html) and pin the pointer position at `(-1,-1)` whenever the cursor is locked. This behavior can be changed through the [Cursor Lock Behavior](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.cursorLockBehavior) property of the [InputSystemUIInputModule](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule).

Multiple pointer Devices may feed input into a single UI input module. Also, in the case of [Touchscreen](xref:UnityEngine.InputSystem.Touchscreen), a single Device can have the ability to have multiple concurrent pointers (each finger contact is one pointer).

Because multiple pointer Devices can feed into the same set of Actions, it is important to set the [action type](xref:input-system-responding#action-types) to [PassThrough](xref:UnityEngine.InputSystem.InputActionType.PassThrough). This ensures that no filtering is applied to input on these actions and that instead every input is relayed as is.

From the perspective of [InputSystemUIInputModule](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule), each [InputDevice](xref:UnityEngine.InputSystem.InputDevice) that has one or more controls bound to one of the pointer-type actions is considered a unique pointer. Also, for each [Touchscreen](xref:UnityEngine.InputSystem.Touchscreen) devices, each separate [TouchControl](xref:UnityEngine.InputSystem.Controls.TouchControl) that has one or more of its controls bound to the those actions is considered its own unique pointer as well. Each pointer receives a unique [pointerId](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.PointerEventData.html#UnityEngine_EventSystems_PointerEventData_pointerId) which generally corresponds to the [deviceId](xref:UnityEngine.InputSystem.InputDevice.deviceId) of the pointer. However, for touch, this will be a combination of [deviceId](xref:UnityEngine.InputSystem.InputDevice.deviceId) and [touchId](xref:UnityEngine.InputSystem.Controls.TouchControl.touchId). Use [ExtendedPointerEventData.touchId](xref:UnityEngine.InputSystem.UI.ExtendedPointerEventData.touchId) to find the ID for a touch event.

You can influence how the input module deals with concurrent input from multiple pointers using the [Pointer Behavior](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.pointerBehavior) setting.

|**Pointer Behavior**|**Description**|
|------------------|-----------|
|[Single Mouse or Pen But Multi Touch And Track](xref:UnityEngine.InputSystem.UI.UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack)|Behaves like [Single Unified Pointer](xref:UnityEngine.InputSystem.UI.UIPointerBehavior.SingleUnifiedPointer) for all input that is not classified as touch or tracked input, and behaves like [All Pointers As Is](xref:UnityEngine.InputSystem.UI.UIPointerBehavior.AllPointersAsIs) for tracked and touch input.<br><br>If concurrent input is received on a [Mouse](xref:UnityEngine.InputSystem.Mouse) and [`Pen`](xref:UnityEngine.InputSystem.Pen), for example, the input of both is fed into the same UI pointer instance. The position input of one will overwrite the position of the other.<br><br>Note that when input is received from touch or tracked devices, the single unified pointer for mice and pens is __removed__ including [IPointerExit](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.IPointerExitHandler.html) events being sent in case the mouse/pen cursor is currently hovering over objects.<br><br>This is the default behavior.|
|[Single Unified Pointer](xref:UnityEngine.InputSystem.UI.UIPointerBehavior.SingleUnifiedPointer)|All pointer input is unified such that there is only ever a single pointer. This includes touch and tracked input. This means, for example, that regardless how many devices feed input into [Point](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.point), only the last such input in a frame will take effect and become the current UI pointer's position.|
|[All Pointers As Is](xref:UnityEngine.InputSystem.UI.UIPointerBehavior.AllPointersAsIs)|The UI input module will not unify any pointer input. Any device, including touch and tracked devices that feed input pointer-type actions, will be its own pointer (or multiple pointers for touch input).<br><br>Note: This might mean that there will be an arbitrary number of pointers in the UI, and several objects might be pointed at concurrently.|

If you bind a device to a pointer-type action such as [Left Click](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.leftClick) without also binding it to [Point](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.point), the UI input module will recognize the device as not being able to point and try to route its input into that of another pointer. For example, if you bind [Left Click](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.leftClick) to the `Space` key and [Point](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.point) to the position of the mouse, then pressing the space bar will result in a left click at the current position of the mouse.

For pointer-type input (as well as for [tracked-type input](#tracked-type-input)), [InputSystemUIInputModule](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) will send [ExtendedPointerEventData](xref:UnityEngine.InputSystem.UI.ExtendedPointerEventData) instances which are an extended version of the base `PointerEventData`. These events contain additional data such as the [device](xref:UnityEngine.InputSystem.UI.ExtendedPointerEventData.device) and [pointer type](xref:UnityEngine.InputSystem.UI.ExtendedPointerEventData.pointerType) which the event has been generated from.

#### Navigation-type input

Navigation-type input controls the current selection based on motion read from the [move](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.move) action. Additionally, input from
[submit](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.submit) will trigger `ISubmitHandler` on the currently selected object and
[cancel](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.cancel) will trigger `ICancelHandler` on it.

Unlike with [pointer-type](#pointer-type-input), where multiple pointer inputs may exist concurrently (think two touches or left- and right-hand tracked input), navigation-type input does not have multiple concurrent instances. In other words, only a single [move](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.move) vector and a single [submit](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.submit) and [cancel](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.cancel) input will be processed by the UI module each frame. However, these inputs need not necessarily come from one single Device always. Arbitrary many inputs can be bound to the respective actions.

While, [move](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.move) should be set to [PassThrough](xref:UnityEngine.InputSystem.InputActionType.PassThrough) Action type, it is important that [submit](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.submit) and
[cancel](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.cancel) be set to the [Button](xref:UnityEngine.InputSystem.InputActionType.Button) Action type.

Navigation input is non-positional, that is, unlike with pointer-type input, there is no screen position associcated with these actions. Rather, navigation actions always operate on the current selection.

#### Tracked-type input

Input from [tracked devices](xref:UnityEngine.InputSystem.TrackedDevice) such as [XR controllers](xref:UnityEngine.InputSystem.XR.XRController) and [HMDs](xref:UnityEngine.InputSystem.XR.XRHMD) essentially behaves like [pointer-type input](#pointer-type-input). The main difference is that the world-space device position and orientation sourced from  [trackedDevicePosition](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.trackedDevicePosition) and  [trackedDeviceOrientation](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.trackedDeviceOrientation) is translated into a screen-space position via raycasting.

> [!IMPORTANT]
> Because multiple tracked Devices can feed into the same set of Actions, it is important to set the [action type](xref:input-system-responding#action-types) to [PassThrough](xref:UnityEngine.InputSystem.InputActionType.PassThrough). This ensures that no filtering is applied to input on these actions and that instead every input is relayed as is.

For this raycasting to work, you need to add [TrackedDeviceRaycaster](xref:UnityEngine.InputSystem.UI.TrackedDeviceRaycaster) to the `GameObject` that has the UI's `Canvas` component. This `GameObject` will usually have a `GraphicRaycaster` component which, however, only works for 2D screen-space raycasting. You can put [TrackedDeviceRaycaster](xref:UnityEngine.InputSystem.UI.TrackedDeviceRaycaster) alongside `GraphicRaycaster` and both can be enabled at the same time without advserse effect.

![The Graphic Raycaster component appears under the Canvas and Canvas Scaler components. The Add Component window appears with the Tracked Device Rayster component selected.](Images/TrackedDeviceRaycasterComponentMenu.png){width="486" height="658"}


![The Tracked Device Rayster component appears under the Canvas component.](Images/TrackedDeviceRaycaster.png){width="485" height="150"}


Clicks on tracked devices do not differ from other [pointer-type input](#pointer-type-input). Therefore, actions such as [Left Click](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule.leftClick) work for tracked devices just like they work for other pointers.

### Other notes about the UI Input Module

#### Upgrading from the Input Manager and the older Standalone Input Module

The Unity UI (uGUI) package contains an older equivalent module called **[Standalone Input Module](https://docs.unity3d.com/Manual/script-StandaloneInputModule.html)** which performs the same kind of integration between the Unity UI and the legacy Input Manager system.

If you have one of these older Standalone Input Module components on a GameObject in your project, and the Input System is installed, Unity displays a button in the Inspector offering to automatically replace it with the equivalent newer Input System UI Input Module for you.

#### UI Input Module priority

The UI Input Module component is not required with UI Toolkit in Unity 2023.2 and onwards. However, if you do use it, the settings on that component take priority over the UI settings in your project-wide actions.

#### Technical details

Input support for both [Unity UI](https://docs.unity3d.com/Manual/com.unity.ugui.html) and [UI Toolkit](https://docs.unity3d.com/Manual/UIElements.html) is based on the same [EventSystem](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/EventSystem.html) and [BaseInputModule](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/InputModules.html) subsystem. In other words, the same input setup based on [InputSystemUIInputModule](#setting-up-ui-input) supports input in either UI solution and nothing extra needs to be done.

Internally, UI Toolkit installs an event listener in the form of the `PanelEventHandler` component which intercepts events that `InputSystemUIInputModule` sends and translates them into UI Toolkit-specific events that are then routed into the visual tree. If you employ `EventSystem.SetUITookitEventSystemOverride`, this default mechanism is bypassed.

> [!NOTE]
> XR ([tracked-type input](#tracked-type-input)) is not yet supported in combination with UI Toolkit. This means that you cannot use devices such as VR controllers to operate interfaces created with UI Toolkit.

There are some additional things worth noting:

* UI Toolkit handles raycasting internally. No separate raycaster component is needed like for uGUI. This means that [TrackedDeviceRaycaster](xref:UnityEngine.InputSystem.UI.TrackedDeviceRaycaster) does not work together with UI Toolkit.
* A pointer click and a gamepad submit action are distinct at the event level in UI Toolkit. This means that if you, for example, use
  ```CSharp
  button.RegisterCallback<ClickEvent>(_ => ButtonWasClicked());
  ```
  the handler is not invoked when the button is "clicked" with the gamepad (a `NavigationSubmitEvent` and not a `ClickEvent`). If, however, you use
  ```CSharp
  button.clicked += () => ButtonWasClicked();
  ```
  the handle is invoked in both cases.




## Multiplayer UIs

The Input System can also handle multiple separate UI instances on the screen controlled separately from different input Bindings. This is useful if you want to have multiple local players share a single screen with different controllers, so that every player can control their own UI instance. To allow this, you need to replace the [Event System](https://docs.unity3d.com/Manual/script-EventSystem.html) component from Unity with the Input System's [Multiplayer Event System](xref:UnityEngine.InputSystem.UI.MultiplayerEventSystem) component.

![The Multiplayer Event System component appears without any GameObjects connected.](Images/MultiplayerEventSystem.png){width="535" height="154"}

Unlike the Event System component, you can have multiple Multiplayer Event Systems active in the Scene at the same time. That way, you can have multiple players, each with their own UI Input Module and Multiplayer Event System components, and each player can have their own set of Actions driving their own UI instance. If you are using the [Player Input](xref:input-system-player-input) component, you can also set it to automatically configure the player's UI Input Module to use the player's Actions. See the documentation on [Player Input](xref:input-system-player-input#ui-input) to learn how.

The properties of the Multiplayer Event System component are identical to those from the Event System component. Additionally, the Multplayer Event System component adds a [Player Root](xref:UnityEngine.InputSystem.UI.MultiplayerEventSystem.playerRoot) property, which you can set to a GameObject that contains all the UI [selectables](https://docs.unity3d.com/Manual/script-Selectable.html) this event system should handle in its hierarchy. Mouse input that this event system processes then ignores any UI selectables which are not on any GameObject in the Hierarchy under  [Player Root](xref:UnityEngine.InputSystem.UI.MultiplayerEventSystem.playerRoot).

## Virtual mouse cursor control

If your application uses gamepads and joysticks as an input, you can use the [navigation Actions](#navigation-type-input) to operate the UI. However, it usually involves extra work to make the UI work well with navigation. An alternative way to operate the UI is to allow gamepads and joysticks to drive the cursor from a "virtual mouse cursor".

The Input System package provides a **Virtual Mouse** component for this purpose.

> [!NOTE]
> This component is only compatible with the [Unity UI](https://docs.unity3d.com/Manual/com.unity.ugui.html) (uGUI) system, and not UI Toolkit.

To see an example of the Virtual Mouse in a project, see the [Gamepad Mouse Cursor sample](xref:input-system-installation#install-samples) included with the Input System package.

### Using the Virtual Mouse component

To set up the Virtual Mouse component with the Unity UI system:

1. Create a UI GameObject with an **Image** component. This GameObject is the mouse pointer. It can help to rename it "_Pointer_".
2. Parent the pointer GameObject as a child of your **Canvas** GameObject that contains the UI which the cursor should operate on.
3. Set the anchor position of the GameObject's `RectTransform` to the bottom left.
4. Ensure your pointer GameObject is the last child of the Canvas so that the cursor draws on top of everything else.
5. Add a **Virtual Mouse** component to the GameObject.
6. Drag the **Image** component of the pointer GameObject into the **Cursor Graphic** field of the Virtual Mouse component.
7. Drag the **Rect Transform** component of the pointer GameObject to the **Cursor Transform** field of the Virtual Mouse component.
8. If you want the virtual mouse to control the system mouse cursor, set [Cursor Mode](xref:UnityEngine.InputSystem.UI.VirtualMouseInput.cursorMode) to **Hardware Cursor If Available**. In this mode, the **Cursor Graphic** is hidden when a system mouse is present and you use [Mouse.WarpCursorPosition](xref:UnityEngine.InputSystem.Mouse.WarpCursorPosition(UnityEngine.Vector2)) to move the system mouse cursor instead of the software cursor. The transform linked through **Cursor Transform** is not updated in that case.
9.  To configure the input to drive the virtual mouse, either add  bindings on the various actions (such as **Stick Action**), or enable **Use Reference** and link existing actions from an Input Actions asset.

> [!IMPORTANT]
> Make sure the UI Input Module component on the UI's **Event System** does not receive navigation input from the same devices that feed into the Virtual Mouse component. If, for example, the Virtual Mouse component is set up to receive input from gamepads, and `Move`, `Submit`, and `Cancel` on the UI Input Module are also linked to the gamepad, then the UI receives input from the gamepad on two channels.

![The Virtual Mouse component appears with the Stick Action and Left Button Action connected to Player Move and Attack Actions.](Images/VirtualMouseInput.png){width="484" height="373"}

At runtime, the component adds a virtual [Mouse](xref:UnityEngine.InputSystem.Mouse) device which the [InputSystemUIInputModule](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) component picks up. The controls of the `Mouse` are fed input based on the actions configured on the [VirtualMouseInput](xref:UnityEngine.InputSystem.UI.VirtualMouseInput) component.

Note that the resulting [Mouse](xref:UnityEngine.InputSystem.Mouse) input is visible in all code that picks up input from the mouse device. You can therefore use the component for mouse simulation elsewhere, not just with [InputSystemUIInputModule](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule).

> [!NOTE]
> Do not set up gamepads and joysticks for [navigation input](#navigation-type-input) while using the Virtual Mouse component. If both the Virtual Mouse component and navigation are configured, input is triggered twice: once via the pointer input path, and once via the navigation input path. If you encounter problems such as where buttons are pressed twice, this is likely the problem.

## Distinguishing between UI and game input

UI in Unity receives input through the same mechanisms as the input for the rest of your game or app. There is no automatic mechanism that implicitly ensures that if a certain input &ndash; such as a click &ndash; is consumed by the UI, it is not also received by your gameplay code.

This can create ambiguities between, for example, code that responds to [`UI.Button.onClick`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.UI.Button.html#UnityEngine_UI_Button_onClick) and code that responds to [`InputAction.performed`](xref:UnityEngine.InputSystem.InputAction.performed) of an Action bound to `<Mouse>/leftButton`.

Whether such ambiguities exist depends on *how* UIs are used. For example, you can avoid ambiguities by implementing your UI in one of the following ways:

* All interaction is performed through UI elements. A 2D/3D scene is rendered in the background but all interaction is performed through UI events (including those such as 'background' clicks on the `Canvas`).
* UI is overlaid over a 2D/3D scene but the UI elements cannot be interacted with directly.
* UI is overlaid over a 2D/3D scene but there is a clear "mode" switch that determines if interaction is picked up by UI or by the game. For example, a first-person game on desktop may employ a [cursor lock](https://docs.unity3d.com/ScriptReference/Cursor-lockState.html) and direct input to the game while it is engaged whereas it may leave all interaction to the UI while the lock is not engaged.

When ambiguities arise, they do so differently for [pointer-type](#pointer-type-input) and [navigation-type](#navigation-type-input).

>[!NOTE]
>A sample called "**UI vs Game Input**" is provided with the package and can be installed from the Unity Package Manager UI in the editor. The sample demonstrates how to deal with a situation where ambiguities arise between inputs for UI and inputs for the game.

### Handling ambiguities for pointer-type input

Input from pointers (mice, touchscreens, pens) can be ambiguous depending on whether or not the pointer is over a UI element when initiating an interaction. For example, if there is a button on screen, then clicking on the button may lead to a different outcome than clicking outside of the button and within the game scene.

If all pointer input is handled via UI events, no ambiguities arise as the UI will implicitly route input to the respective receiver. If, however, input within the UI is handled via UI events and input in the game is handled via [Actions](xref:input-system-actions), pointer input will by default lead to *both* being triggered.

The easiest way to resolve such ambiguities is to respond to in-game actions by [polling](xref:input-system-responding#polling-actions) from inside [`MonoBehaviour.Update`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html) methods and using [`EventSystem.IsPointerOverGameObject`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.EventSystem.html?q=ispointerovergameobject#UnityEngine_EventSystems_EventSystem_IsPointerOverGameObject) to find out whether the pointer is over UI or not. Another way is to use [`EventSystem.RaycastAll`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.EventSystem.html?q=ispointerovergameobj#UnityEngine_EventSystems_EventSystem_RaycastAll_UnityEngine_EventSystems_PointerEventData_System_Collections_Generic_List_UnityEngine_EventSystems_RaycastResult__) to determine if the pointer is currently over UI.

>[!NOTE]
>Calling [`EventSystem.IsPointerOverGameObject`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.EventSystem.html?q=ispointerovergameobject#UnityEngine_EventSystems_EventSystem_IsPointerOverGameObject) from within [`InputAction`](xref:UnityEngine.InputSystem.InputAction) callbacks such as [`InputAction.performed`](xref:UnityEngine.InputSystem.InputAction.performed) will lead to a warning. The UI updates separately *after* input processing and UI state thus corresponds to that of the *last* frame/update while input is being processed.

### Handling ambiguities for navigation-type input

Ambiguities for navigation-type Devices such as gamepads and joysticks (but also keyboards) cannot arise the same way that it does for pointers. Instead, your application has to decide explicitly whether to use input for the UI's `Move`, `Submit`, and `Cancel` inputs or for the game. This can be done by either splitting control on a Device or by having an explicit mode switch.

Splitting input on a Device is done by simply using certain controls for operating the UI while using others to operate the game. For example, you could use the d-pad on gamepads to operate UI selection while using the sticks for in-game character control. This setup requires adjusting the bindings used by the UI Actions accordingly.

An explicit mode switch is implemented by temporarily switching to UI control while suspending in-game Actions. For example, the left trigger on the gamepad could bring up an item selection wheel which then puts the game in a mode where the sticks are controlling UI selection, the A button confirms the selection, and the B button closes the item selection wheel. No ambiguities arise as in-game actions will not respond while the UI is in the "foreground".



## Immediate Mode GUI

The Input System package does not support [Immediate Mode GUI](https://docs.unity3d.com/Manual/GUIScriptingGuide.html) (IMGUI) methods at runtime.

However, if you need to use IMGUI for your UI, it is possible to use legacy Input Manager input for your IMGUI user interface, while also using the Input System package for your in-game input.

When the Editor's [**Active Input Handling**](https://docs.unity3d.com/Manual/class-PlayerSettings.html) setting is set to "**Input System Package**" (which is the default, when using the Input System package), the `OnGUI` methods in your player code won't receive any input events.

To restore functionality to runtime `OnGUI` methods, you can change the **Active Input Handling** setting to "**Both**". Doing this means that Unity processes the input twice which could introduce a small performance impact.

This only affects runtime (play mode) OnGUI methods. Editor GUI code is unaffected and will receive input events regardless.
