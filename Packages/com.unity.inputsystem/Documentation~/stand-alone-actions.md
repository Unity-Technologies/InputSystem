### Stand-alone actions in MonoBehaviours

As an alternative workflow, you can declare individual [Input Action](../api/UnityEngine.InputSystem.InputAction.html) and [Input Action Maps](../api/UnityEngine.InputSystem.InputActionMap.html) as fields directly inside `MonoBehaviour` components.

```CSharp
using UnityEngine;
using UnityEngine.InputSystem;

public class ExampleScript : MonoBehaviour
{
    public InputAction move;
    public InputAction jump;
}
```

The result is similar to using an Actions defined in the Input Actions editor, except the Actions are defined in the GameObject's properties and saved as Scene or Prefab data, instead of in a dedicated Asset.

When you embed actions like this, by defining serialized InputAction fields in a MonoBehaviour, the GameObject's Inspector window displays an interface similar to the Actions column of the [Actions Editor](./ActionsEditor.md), which allows you to set up the bindings for those actions. For example:

![MyBehavior Inspector](Images/Workflow-EmbeddedActionsInspector.png)

* To add or remove Actions or Bindings, click the Add (+) or Remove (-) icon in the header.
* To edit Bindings, double-click them.<br>
* To edit Actions, double-click them in an Action Map, or click the gear icon on individual Action properties.<br>
* You can also right-click entries to bring up a context menu, and you can drag them. Hold the Alt key and drag an entry to duplicate it.

Unlike the project-wide actions in the Project Settings window, you must manually enable and disable Actions and Action Maps that are embedded in MonoBehaviour components.

When you use this workflow, the serialised action configurations are stored with the parent GameObject as part of the scene, opposite to being serialised with an Action Asset. This can be useful if you want to bundle the control bindings and behaviour together in a single monobehaviour or prefab, so it can be distributed together. However, this can also make it harder to organize your full set of control bindings if they are distributed across multiple prefabs or scenes.