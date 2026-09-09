---
uid: input-system-control-state-history
---

# Record control state history

If you want to access the history of value changes on a control (for example, in order to compute exit velocity on a touch release), you can record state changes over time with [`InputStateHistory`](xref:UnityEngine.InputSystem.LowLevel.InputStateHistory) or [`InputStateHistory<TValue>`](xref:UnityEngine.InputSystem.LowLevel.InputStateHistory`1). The latter restricts controls to those of a specific value type, which in turn simplifies some of the API.

[!code-cs[history](Packages/com.unity.inputsystem/DocCodeSamples.Tests/RecordControlStateHistory.cs#history)]

For example, if you want to have the last 100 samples of the left stick on the gamepad available, you can use this code:

[!code-cs[100samples](Packages/com.unity.inputsystem/DocCodeSamples.Tests/RecordControlStateHistory.cs#100samples)]
