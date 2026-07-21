---
uid: input-system-update-mode
---

# Update Mode

This setting determines when the Input System processes input. The Input System can process input in one of three distinct ways:

|Type|Description|
|----|-----------|
|[`Process Events In Dynamic Update`](xref:UnityEngine.InputSystem.InputSettings.UpdateMode)|The Input System processes events at irregular intervals determined by the current framerate.|
|[`Process Events In Fixed Update`](xref:UnityEngine.InputSystem.InputSettings.UpdateMode)|The Input System processes events at fixed-length intervals. This corresponds to how [`MonoBehaviour.FixedUpdate`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html) operates. The length of each interval is determined by [`Time.fixedDeltaTime`](https://docs.unity3d.com/ScriptReference/Time-fixedDeltaTime.html).|
|[`Process Events Manually`](xref:UnityEngine.InputSystem.InputSettings.UpdateMode)|The Input System does not process events automatically. Instead, it processes them whenever you call [`InputSystem.Update()`](xref:UnityEngine.InputSystem.InputSystem).|

> [!NOTE]
> The system performs two additional types of updates in the form of  [`InputUpdateType.BeforeRender`](xref:UnityEngine.InputSystem.LowLevel.InputUpdateType) (late update for XR tracking Devices) and [`InputUpdateType.Editor`](xref:UnityEngine.InputSystem.LowLevel.InputUpdateType) (for EditorWindows). Neither of these update types change how the application consumes input.
