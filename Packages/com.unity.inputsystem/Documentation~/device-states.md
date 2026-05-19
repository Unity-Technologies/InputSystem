---
uid: input-system-device-states
---

# Device states 

Like any other type of [Control](controls.md#control-state), each Device has a block of memory allocated to it which stores the state of all the Controls associated with the Device.

State changes are usually initiated through [state events](read-state-events.md) from the native backend, but you can use [`InputControl<>.WriteValueIntoState()`](../api/UnityEngine.InputSystem.InputControl-1.html#UnityEngine_InputSystem_InputControl_1_WriteValueIntoState__0_System_Void__) to manually overwrite the state of any Control.


| Topic | Description |
| --- | --- |

## Additional resources