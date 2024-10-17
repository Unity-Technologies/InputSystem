
## Control usages

A Control can have one or more associated usages. A usage is a string that denotes the Control's intended use. An example of a Control usage is `Submit`, which labels a Control that is commonly used to confirm a selection in the UI. On a gamepad, this usage is commonly found on the `buttonSouth` Control.

You can access a Control's usages using the [`InputControl.usages`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_usages) property.

Usages can be arbitrary strings. However, a certain set of usages is very commonly used and comes predefined in the API in the form of the [`CommonUsages`](../api/UnityEngine.InputSystem.CommonUsages.html) static class. Check out the [`CommonUsages` scripting API page](../api/UnityEngine.InputSystem.CommonUsages.html) for an overview.
