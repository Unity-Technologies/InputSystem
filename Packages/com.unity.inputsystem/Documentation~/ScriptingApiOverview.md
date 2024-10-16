# API Overview

When scripting with Actions in the Input System, there are number of important API you can use:

|API name|Description|
|-----|-----------|
|[`InputSystem.actions`](../api/UnityEngine.InputSystem.InputSystem.html)|A reference to the set of actions assigned as the [project-wide Actions](./ProjectWideActions.md).|
|[`InputActionMap`](../api/UnityEngine.InputSystem.InputActionMap.html)|A named collection of Actions. The API equivalent to an entry in the "Action Maps" column of the [Input Actions editor](ActionsEditor.md).|
|[`InputAction`](../api/UnityEngine.InputSystem.InputAction.html)|A named Action that can return the current value of the controls that it is bound to, or can trigger callbacks in response to input. The API equivalent to an entry in the "Actions" column of the [Input Actions editor](ActionsEditor.md).|
|[`InputActionAsset`](../api/UnityEngine.InputSystem.InputActionAsset.html)|An asset that contains a collection of action, action map, binding, and control scheme definitions. The API equivalent of the [Actions Asset](ActionAssets.md).|
|[`InputBinding`](../api/UnityEngine.InputSystem.InputBinding.html)|The relationship between an Action and the specific device controls for which it receives input. For more information about Bindings and how to use them, see [Action Bindings](ActionBindings.md).|

Each Action has a name ([`InputAction.name`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_name)), which must be unique within the Action Map that the Action belongs to, if any (see [`InputAction.actionMap`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_actionMap)). Each Action also has a unique ID ([`InputAction.id`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_id)), which you can use to reference the Action. The ID remains the same even if you rename the Action.

Each Action Map has a name ([`InputActionMap.name`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_name)), which must also be unique with respect to the other Action Maps present, if any. Each Action Map also has a unique ID ([`InputActionMap.id`](../api/UnityEngine.InputSystem.InputActionMap.html#UnityEngine_InputSystem_InputActionMap_id)), which you can use to reference the Action Map. The ID remains the same even if you rename the Action Map.


