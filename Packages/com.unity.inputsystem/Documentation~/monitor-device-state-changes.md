---
uid: input-system-monitor-device-state-changes
---

# Monitor device state changes 

You can use [`InputState.AddChangeMonitor()`](../api/UnityEngine.InputSystem.LowLevel.InputState.html#UnityEngine_InputSystem_LowLevel_InputState_AddChangeMonitor_UnityEngine_InputSystem_InputControl_System_Action_UnityEngine_InputSystem_InputControl_System_Double_UnityEngine_InputSystem_LowLevel_InputEventPtr_System_Int64__System_Int32_System_Action_UnityEngine_InputSystem_InputControl_System_Double_System_Int64_System_Int32__) to register a callback to be called whenever the state of a Control changes. The Input System uses the same mechanism to implement [input Actions](actions.md).