---
uid: input-system-action-api-overview
---

# Scripting with actions API overview

When scripting with actions in the Input System, there are number of important APIs you can use, listed here.

## Namespace

The Input System's API is contained in the `UnityEngine.InputSystem` namespace. To use it, include the namespace as follows:

```
using UnityEngine.InputSystem;
```

## Important APIs

|API name|Description|
|-----|-----------|
|[`InputSystem.actions`](xref:UnityEngine.InputSystem.InputSystem)|A reference to the set of actions assigned as the [project-wide Actions](./about-project-wide-actions.md).|
|[`InputAction`](xref:UnityEngine.InputSystem.InputAction)|The class which represents an action. You can use a reference to an action to read the current value of the controls that it is bound to, or to trigger callbacks in response to input. This class corresponds to an entry in the **Actions** column of the [Input Actions editor](actions-editor.md).|
|[`InputActionMap`](xref:UnityEngine.InputSystem.InputActionMap)|The class which represents an [action map](create-edit-delete-action-maps.md). The API equivalent to an entry in the **Action Maps** column of the [Input Actions editor](actions-editor.md).|
|[`InputBinding`](xref:UnityEngine.InputSystem.InputBinding)|The relationship between an action and the specific device controls for which it receives input. For more information about bindings and how to use them, refer to [Bindings](bindings.md).|

## Actions

The [`InputAction`](xref:UnityEngine.InputSystem.InputAction) class represents an action in the Input System. These are the same actions that you [create in the actions editor](actions.md).

With a reference to an action, you can then read values and state changes using either the [polling](polling-actions.md) or [callbacks](set-callbacks-on-actions.md) workflow.

Each action has a name ([`InputAction.name`](xref:UnityEngine.InputSystem.InputAction)), which must be unique within the action map that the action belongs to, if any (refer to [`InputAction.actionMap`](xref:UnityEngine.InputSystem.InputAction)). Each action also has a unique ID ([`InputAction.id`](xref:UnityEngine.InputSystem.InputAction)), which you can use to reference the action. The ID remains the same even if you rename the action.

## Action maps

Each action map has a name ([`InputActionMap.name`](xref:UnityEngine.InputSystem.InputActionMap)), which must also be unique with respect to the other action maps present, if any. Each action map also has a unique ID ([`InputActionMap.id`](xref:UnityEngine.InputSystem.InputActionMap)), which you can use to reference the action map. The ID remains the same even if you rename the action map.

With a reference to an action map, you can then read all the [`actions`](xref:UnityEngine.InputSystem.InputActionMap) which belong to that map.
