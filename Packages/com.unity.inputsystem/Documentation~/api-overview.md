# API Overview

When scripting with Actions in the Input System, there are number of important API you can use, listed here:

## Namespace

The Input System's API is contained in the `UnityEngine.InputSystem` namespace. To use it, include the namespace as follows:

```
using UnityEngine.InputSystem;
```

## Important API

|API name|Description|
|-----|-----------|
|[`InputSystem.actions`](../api/UnityEngine.InputSystem.InputSystem.html)|A reference to the set of actions assigned as the [project-wide Actions](./about-project-wide-actions.md).|
|[`InputAction`](../api/UnityEngine.InputSystem.InputAction.html)|The class which represents an action. You can use a reference to an action to read the current value of the controls that it is bound to, or to trigger callbacks in response to input. This class corresponds to an entry in the **Actions**"** column of the [Input Actions editor](actions-editor.md).|
|[`InputActionMap`](../api/UnityEngine.InputSystem.InputActionMap.html)|The class which represents an [action map](create-edit-delete-action-maps.md). The API equivalent to an entry in the "Action Maps" column of the [Input Actions editor](actions-editor.md).|
|[`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html)|The relationship between an action and the specific device controls for which it receives input. For more information about Bindings and how to use them, see [bindings](bindings.md).|

## Actions

The [`InputAction`](../api/UnityEngine.InputSystem.InputAction.html) class represents an action in the Input System. These are the same actions that you [create in the actions editor](Actions.md).

With a reference to an action, you can then read values and state changes using eeither the [polling](polling-actions.md) or [callbacks](set-callbacks-on-actions.md) workflow.

Each action has a name ([`InputAction.name`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_name)), which must be unique within the Action Map that the Action belongs to, if any (see [`InputAction.actionMap`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_actionMap)). Each Action also has a unique ID ([`InputAction.id`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_id)), which you can use to reference the Action. The ID remains the same even if you rename the Action.

## Action maps

Each Action Map has a name ([`InputActionMap.name`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_name)), which must also be unique with respect to the other Action Maps present, if any. Each Action Map also has a unique ID ([`InputActionMap.id`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_id)), which you can use to reference the Action Map. The ID remains the same even if you rename the Action Map.

With a reference to an action map, you can then read all the [`actions`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_actions) which belong to that map.

