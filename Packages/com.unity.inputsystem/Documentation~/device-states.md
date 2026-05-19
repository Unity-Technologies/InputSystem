---
uid: input-system-device-states
---

# Device states

Observe and change Device control state from script beyond what native backends send.

Each [Device](devices-scripting.md) stores control values in memory, usually updated by [state events](read-state-events.md). Use change monitors to react when values change, or synthesize state when you need derived or manual updates.

| **Topic** | **Description** |
| :--- | :--- |
| **[Monitor device state changes](monitor-device-state-changes.md)** | Register callbacks with `InputState.AddChangeMonitor` when control state changes. |
| **[Synthesize a device state change](synthesize-device-state-change.md)** | Push state changes with `InputState.Change` for derived or manual control values. |

## Additional resources

- [Controls](controls.md)
- [Read state events](read-state-events.md)
- [Device commands](device-commands.md)
- [Working with devices](working-with-devices.md)
