---
uid: input-system-nav-ui-support
---

# Navigation input UI support

Navigation-type input controls the current selection based on motion read from the [`move`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) action. Additionally, input from
[`submit`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) will trigger `ISubmitHandler` on the currently selected object and
[`cancel`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) will trigger `ICancelHandler` on it.

Navigation-type input doesn't have multiple concurrent instances. The UI module only processes a single [`move`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) vector and a single [`submit`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) and [`cancel`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) input per frame. However, these inputs don't need to come from one single device. You can bind multiple inputs to each action.

The [`move`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) input must be set to the [`PassThrough`](xref:UnityEngine.InputSystem.InputActionType) action type. The [`submit`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) and
[`cancel`](xref:UnityEngine.InputSystem.UI.InputSystemUIInputModule) inputs must be set to the [`Button`](xref:UnityEngine.InputSystem.InputActionType) Action type.

Navigation input is non-positional. There is no screen position associated with navigation actions. Instead, navigation actions always operate on the current selection.
