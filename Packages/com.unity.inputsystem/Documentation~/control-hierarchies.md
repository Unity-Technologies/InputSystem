# Control hierarchies

Controls can be arranged in hierarchies. The root of a control hierarchy is always a [device](devices.md). You can see some examples of hierarchies when browsing controls in the **Path** dropdown menu of the [Binding properties panel](./binding-properties-panel.md). For example, the D-Pad control of a gamepad has child controls of left, right, up, down, and the separte horizontal and vertical 1D axes.

The setup of hierarchies is exclusively controlled through [layouts](layouts.md).

You can access the parent of a Control using [`InputControl.parent`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_parent), and its children using [`InputControl.children`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_children). To access the flattened hierarchy of all Controls on a Device, use [`InputDevice.allControls`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_allControls).

