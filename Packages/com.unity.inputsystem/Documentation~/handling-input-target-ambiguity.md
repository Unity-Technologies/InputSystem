---
uid: input-system-target-ambiguity
---

# Handling input target ambiguity

Understand how to manage ambiguities between input for your application's user interface (UI), and input for other parts of your application.

> [!NOTE]
> The Input System package includes a sample project called **UI vs Game Input**. The sample demonstrates how to deal with ambiguities between inputs for UI and inputs for the game.

## How Unity processes UI input

Unity processes UI input through the same mechanisms as the input for the rest of your application. There's no automatic mechanism that prevents the UI and the rest of your code from consuming the same input (such as a mouse click).

## Strategies for avoiding ambiguity

Ambiguities can appear between, for example, code that responds to [`UI.Button.onClick`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.UI.Button.html#UnityEngine_UI_Button_onClick) and code that responds to [`InputAction.performed`](xref:UnityEngine.InputSystem.InputAction) for an action bound to `<Mouse>/leftButton`.

Whether such ambiguities exist depends on how you implement the UI and the UI input. The following UI implementation strategies are examples of how you can avoid these ambiguities:

* Perform all interaction through UI elements. Render a scene in the background, but perform all interaction through UI events (including those such as 'background' clicks on the `Canvas`).
* Place UI over a 2D/3D scene, but don’t let the user directly interact with the UI.
* Place UI over a 2D/3D scene, but create a clear "mode" switch that determines whether interaction applies to the UI or the scene. For example, a first-person game on desktop might employ a [cursor lock](https://docs.unity3d.com/ScriptReference/Cursor-lockState.html) which directs input to the game when it is engaged, and to the UI when it is not engaged.

There are also specific ambiguities that can arise for [pointer input](supported-ui-input-types-pointer.md) and [navigation input](supported-ui-input-types-navigation.md).

## Handling pointer input ambiguities

Input from pointers (mice, touchscreens, pens) can be ambiguous depending on whether or not the pointer is over a UI element when initiating an interaction. For example, if there is a button on screen, then clicking on the button may lead to a different outcome than clicking outside of the button and within the game scene.

If all pointer input is handled with UI events, no ambiguities arise as the UI will implicitly route input to the respective receiver. If, however, input within the UI is handled with UI events and input in the game is handled with [actions](actions.md), pointer input will by default lead to *both* being triggered.

The easiest way to resolve such ambiguities is to respond to in-game actions by [polling](polling-actions.md) from inside [`MonoBehaviour.Update`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html) methods and using [`EventSystem.IsPointerOverGameObject`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.EventSystem.html?q=ispointerovergameobject#UnityEngine_EventSystems_EventSystem_IsPointerOverGameObject) to find out whether the pointer is over UI or not. Another way is to use [`EventSystem.RaycastAll`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.EventSystem.html?q=ispointerovergameobj#UnityEngine_EventSystems_EventSystem_RaycastAll_UnityEngine_EventSystems_PointerEventData_System_Collections_Generic_List_UnityEngine_EventSystems_RaycastResult__) to determine if the pointer is currently over UI.

> [!NOTE]
> Calling [`EventSystem.IsPointerOverGameObject`](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.EventSystem.html?q=ispointerovergameobject#UnityEngine_EventSystems_EventSystem_IsPointerOverGameObject) from within [`InputAction`](xref:UnityEngine.InputSystem.InputAction) callbacks such as [`InputAction.performed`](xref:UnityEngine.InputSystem.InputAction) lead to a warning. The UI updates separately after input processing, so UI state corresponds to that of the last frame/update while input is being processed.

## Handling navigation input ambiguities

For navigation-type devices such as gamepads, joysticks, and keyboards, you must decide explicitly whether to use input for the UI's `Move`, `Submit`, and `Cancel` inputs or for the game. This can be done by either splitting control on a device or by having an explicit mode switch.

Splitting input on a device is done by simply using certain controls for operating the UI while using others to operate the game. For example, you could use the d-pad on gamepads to operate UI selection while using the sticks for in-game character control. This setup requires adjusting the bindings used by the UI actions accordingly.

An explicit mode switch is implemented by temporarily switching to UI control while suspending in-game actions. For example, the left trigger on the gamepad could bring up an item selection wheel which then puts the game in a mode where the sticks are controlling UI selection, the A button confirms the selection, and the B button closes the item selection wheel. No ambiguities arise as in-game actions will not respond while the UI is in the "foreground".
