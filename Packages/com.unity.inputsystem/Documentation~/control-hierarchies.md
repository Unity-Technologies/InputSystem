# Control hierarchies

Controls can form hierarchies. The root of a Control hierarchy is always a [Device](Devices.md).

The setup of hierarchies is exclusively controlled through [layouts](Layouts.md).

You can access the parent of a Control using [`InputControl.parent`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_parent), and its children using [`InputControl.children`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_children). To access the flattened hierarchy of all Controls on a Device, use [`InputDevice.allControls`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_allControls).