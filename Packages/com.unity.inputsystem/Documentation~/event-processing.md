---
uid: input-system-event-processing
---

# Event processing

[Events](xref:UnityEngine.InputSystem.LowLevel.InputEvent) are collected on a queue by the Unity runtime. This queue is regularly flushed out and the events on it processed. Events can be added to the queue manually by calling [`InputSystem.QueueEvent`](xref:UnityEngine.InputSystem.InputSystem).

Each time input is processed, [`InputSystem.Update`](xref:UnityEngine.InputSystem.InputSystem) is called implicitly by the Unity runtime.

The interval at which this happens is determined by the ["Update Mode"](update-mode.md) configured in the settings. By default, input is processed in each frame __before__ <c>MonoBehaviour.Update</c> methods are called. If the setting is changed to process input in fixed updates, then this changes to input being processed each time before <c>MonoBehaviour.FixedUpdate</c> methods are called.

Normally, when input is processed, __all__ outstanding input events on the queue will be consumed. There are two exceptions to this, however.

When using [`UpdateMode.ProcessEventsInFixedUpdate`](xref:UnityEngine.InputSystem.InputSettings.UpdateMode), the Input System attempts to associate events with the timeslice of the corresponding <c>FixedUpdate</c>. This is based on the [timestamps](xref:UnityEngine.InputSystem.LowLevel.InputEvent) of the events and a "best effort" at calculating the corresponding timeslice of the current <c>FixedUpdated</c>.

The other exception are [`BeforeRender`](xref:UnityEngine.InputSystem.LowLevel.InputUpdateType) updates. These updates are run after fixed or dynamic updates but before rendering and used used exclusively to update devices such as VR headsets that need the most up-to-date tracking data. Other input is not consumed from such updates and these updates are only enabled if such devices are actually present. `BeforeRender` updates are not considered separate frames as far as input is concerned.

> [!NOTE]
> Manually calling [`InputSystem.Update`](xref:UnityEngine.InputSystem.InputSystem) is strongly advised against except within tests employing [`InputTestFixture`](xref:UnityEngine.InputSystem.InputTestFixture) or when explicitly setting the system to [manual update mode](xref:UnityEngine.InputSystem.InputSettings.UpdateMode).

Methods such as [`InputAction.WasPerformedThisFrame`](xref:UnityEngine.InputSystem.InputAction) and [`InputAction.WasPerformedThisFrame`](xref:UnityEngine.InputSystem.InputAction) operate implicitly based on the [`InputSystem.Update`] cadence described above. Meaning, that they refer to the state as per the __last__ fixed/dynamic/manual update happened.

You can query the [current/last update type](xref:UnityEngine.InputSystem.LowLevel.InputState) and [count](xref:UnityEngine.InputSystem.LowLevel.InputState) from [`InputState`](xref:UnityEngine.InputSystem.LowLevel.InputState).
