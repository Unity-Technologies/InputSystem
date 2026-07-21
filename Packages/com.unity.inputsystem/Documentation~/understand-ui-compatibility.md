---
uid: input-system-ui-sys-comp
---

# Understand UI system compatibility

Unity has [three major UI solutions](https://docs.unity3d.com/Manual/UIToolkits.html). The Input System package's compatibility and workflow with these solutions varies depending on which UI solution you are using, and which version of Unity you are using.

The three main UI systems are:

* [**UI Toolkit**](https://docs.unity3d.com/Manual/UIElements.html)
* [**Unity UI**](https://docs.unity3d.com/Packages/com.unity.ugui@latest)
* [**IMGUI**](https://docs.unity3d.com/Manual/GUIScriptingGuide.html)

Some systems and versions require that you use the Input System’s [UI Input Module component](using-ui-input-module.md) to pass actions from the Input System to the UI.

## Compatibility summary
UI system|Compatible|UI Input Module component
-|-|-
UI Toolkit (2023.2+)|Yes|Not required
UI Toolkit (pre 2023.2)|Yes|Required
Unity UI (uGUI)|Yes|Required
IMGUI|No|n/a

## UI Toolkit compatibility
[**UI Toolkit**](https://docs.unity3d.com/Manual/UIElements.html), also known as "UI Elements", is an XML/CSS style UI solution.

- In versions of Unity prior to 2023.2, you must use the [UI Input Module component](using-ui-input-module.md) to pass actions from the Input System to the UI.
- From Unity 2023.2 and onwards, the UI actions defined in the default [project-wide actions](./about-project-wide-actions.md) directly map to UI Toolkit input.</br></br>

For more information on how to configure UI Toolkit input, refer to UI Toolkit [Runtime UI event system and input handling](https://docs.unity3d.com/Manual/UIE-Runtime-Event-System.html).

### UI Toolkit event system

Internally, UI Toolkit installs the `PanelEventHandler` component as an event listener. The listener intercepts events that `InputSystemUIInputModule` sends, and translates them into events that are specific to UI Toolkit. Those events are then routed into the visual tree.

If you employ `EventSystem.SetUITookitEventSystemOverride`, this default mechanism is bypassed.

A pointer click and a gamepad submit action are distinct at the event level in UI Toolkit. For example, the following script does not invoke the handler when the button is "clicked" with the gamepad (a `NavigationSubmitEvent` and not a `ClickEvent`):

 ```CSharp
 button.RegisterCallback<ClickEvent>(_ => ButtonWasClicked());
 ```

However, the following script invokes the handler in both cases:

 ```CSharp
 button.clicked += () => ButtonWasClicked();
 ```

### UI Toolkit XR support
XR ([tracked-type input](supported-ui-input-types-tracked.md)) is not yet supported in combination with UI Toolkit. This means that you cannot use devices such as VR controllers to operate interfaces created with UI Toolkit.

### UI Toolkit raycasting
UI Toolkit handles raycasting internally. No separate raycaster component is necessary. This means that UI Toolkit does not support [TrackedDeviceRaycaster](xref:UnityEngine.InputSystem.UI.TrackedDeviceRaycaster).

## Unity UI (uGUI) compatibility
[**Unity UI**](https://docs.unity3d.com/Packages/com.unity.ugui@latest), also known as "uGUI", is a GameObject and Component-based UI solution).

When using uGUI, you always need to use the [UI Input Module component](using-ui-input-module.md) to pass actions from the Input System to the UI.

The uGUI package contains an older equivalent module called **[Standalone Input Module](https://docs.unity3d.com/Manual/script-StandaloneInputModule.html)**, which performs the same kind of integration between the Unity UI and the legacy Input Manager system.

If you have one of these older Standalone Input Module components on a GameObject in your project, and the Input System is installed, Unity displays a button in the Inspector offering to automatically replace it with the equivalent newer Input System UI Input Module for you.

## IMGUI compatibility
[**IMGUI**](https://docs.unity3d.com/Manual/GUIScriptingGuide.html) is a script-based "Immediate Mode" UI which uses the [`OnGUI`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnGUI.html) method.

The Input System package is not compatible with IMGUI. However you can still use the Input System for other parts of your project, such as gameplay. For more information, refer to [Use IMGUI alongside the Input System package](use-imgui-alongside-input-system.md).
