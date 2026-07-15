---
uid: input-system-input-comp-ref
---

# UI Input Module component reference

 Use the UI Input Module to configure UI-specific actions and inputs. To view the UI Input Module, refer to [Access the UI Input Module](access-ui-input-module.md).

The properties on the UI Input Module correspond to the [`InputSystemUIInputModule`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) class.

|**Property**|**Description**|
|--------|-----------|
|**Move Repeat Delay**|The initial delay (in seconds) between generating an initial [IMoveHandler.OnMove](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.IMoveHandler.html) navigation event and generating repeated navigation events when the __Move__ action stays actuated.|
|**Move Repeat Rate**|The interval (in seconds) between generating repeat navigation events when the __Move__ action stays actuated. Note that this is capped by the frame rate; there will not be more than one move repeat event each frame so if the frame rate dips below the repeat rate, the effective repeat rate will be lower than this setting.|
|**XR Tracking Origin** | Define the transform that represents the real-world transform for tracking devices. |
|**Deselect on Background Click**|Clear the current selection when the pointer is clicked and does not hit any `GameObject`. To prevent automatic deselection, deactivate this property.|
|**Pointer Behavior**|How to deal with multiple pointers feeding input into the UI. Refer to [pointer-type input](supported-ui-input-types-pointer.md). The options are: </br></br>- **Single Mouse or Pen But Multi Touch And Track**: Behaves like **Single Unified Pointer** for all input that is not classified as touch or tracked input, and behaves like **All Pointers As Is** for tracked and touch input.<br>- **Single Unified Pointer**: All pointer input is unified such that there is only ever a single pointer. This includes touch and tracked input. </br>- **All Pointers As Is**: The UI Input Module does not unify any pointer input. Any device, including touch and tracked devices that feed pointer input actions, has its own pointer (or multiple pointers for touch input). <br/><br/>For more details on these pointer behaviors, refer to [Pointer input UI support: Pointer behavior](supported-ui-input-types-pointer.md#pointer-behavior-for-multiple-devices).|
|**Scroll Delta Per Tick** | Define the scroll wheel speed sent to Unity UI (uGUI) components. The value in this field is a multiplier of the  `PointerEventData.scrollDelta` value. |
|**Actions Asset**|An [action asset](action-assets.md) containing all the actions to control the UI. You can choose which actions in the Asset correspond to which UI inputs using the following properties.<br><br>By default, this references a built-in asset named `DefaultInputActions`, which contains common default actions for driving UI. If you want to set up your own actions, [create a custom input action asset](create-empty-action-asset.md) and assign it here.<br/><br/> When you assign a new asset reference to this field in the Inspector, the Editor attempts to automatically map actions to UI inputs based on common naming conventions, and lists the actions underneath the **Actions Asset** property.<br/><br/>|
|**Cursor Lock Behavior**|Controls the origin point of UI raycasts when the cursor is locked. By default, the available options are: <br/><br/>- **Outside Screen** <br/>-  **Screen Center** |
