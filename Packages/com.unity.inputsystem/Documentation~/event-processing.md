---
uid: input-system-event-processing
---

# Event processing 

[Events](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html) are collected on a queue by the Unity runtime. This queue is regularly flushed out and the events on it processed. Events can be added to the queue manually by calling [`InputSystem.QueueEvent`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_QueueEvent_UnityEngine_InputSystem_LowLevel_InputEventPtr_).

Each time input is processed, [`InputSystem.Update`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_Update_) is called implicitly by the Unity runtime.

The interval at which this happens is determined by the ["Update Mode"](update-mode.md) configured in the settings. By default, input is processed in each frame __before__ <c>MonoBehaviour.Update</c> methods are called. If the setting is changed to process input in fixed updates, then this changes to input being processed each time before <c>MonoBehaviour.FixedUpdate</c> methods are called.

Normally, when input is processed, __all__ outstanding input events on the queue will be consumed. There are two exceptions to this, however.

When using [`UpdateMode.ProcessEventsInFixedUpdate`](../api/UnityEngine.InputSystem.InputSettings.UpdateMode.html#UnityEngine_InputSystem_InputSettings_UpdateMode_ProcessEventsInFixedUpdate), the Input System attempts to associate events with the timeslice of the corresponding <c>FixedUpdate</c>. This is based on the [timestamps](../api/UnityEngine.InputSystem.LowLevel.InputEvent.html#UnityEngine_InputSystem_LowLevel_InputEvent_time) of the events and a "best effort" at calculating the corresponding timeslice of the current <c>FixedUpdated</c>.

The other exception are [`BeforeRender`](../api/UnityEngine.InputSystem.LowLevel.InputUpdateType.html#UnityEngine_InputSystem_LowLevel_InputUpdateType_BeforeRender) updates. These updates are run after fixed or dynamic updates but before rendering and used used exclusively to update devices such as VR headsets that need the most up-to-date tracking data. Other input is not consumed from such updates and these updates are only enabled if such devices are actually present. `BeforeRender` updates are not considered separate frames as far as input is concerned.

>__Note__: Manually calling [`InputSystem.Update`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_Update_) is strongly advised against except within tests employing [`InputTestFixture`](../api/UnityEngine.InputSystem.InputTestFixture.html) or when explicitly setting the system to [manual update mode](../api/UnityEngine.InputSystem.InputSettings.UpdateMode.html#UnityEngine_InputSystem_InputSettings_UpdateMode_ProcessEventsManually).

Methods such as [`InputAction.WasPerformedThisFrame`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasPerformedThisFrame) and [`InputAction.WasPerformedThisFrame`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasPerformedThisFrame) operate implicitly based on the [`InputSystem.Update`] cadence described above. Meaning, that they refer to the state as per the __last__ fixed/dynamic/manual update happened.

You can query the [current/last update type](../api/UnityEngine.InputSystem.LowLevel.InputState.html#UnityEngine_InputSystem_LowLevel_InputState_currentUpdateType) and [count](../api/UnityEngine.InputSystem.LowLevel.InputState.html#UnityEngine_InputSystem_LowLevel_InputState_updateCount) from [`InputState`](../api/UnityEngine.InputSystem.LowLevel.InputState.html).