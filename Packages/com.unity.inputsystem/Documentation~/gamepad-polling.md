---
uid: input-system-polling-gamepad
---

# Gamepad polling

The platform sets the default polling frequency to provide a good user experience for the devices supported on the platform. This frequency is guaranteed to be at least 60 Hz. You can override the polling frequency suggested by the target platform by explicitly setting [`InputSystem.pollingFrequency`](xref:UnityEngine.InputSystem.InputSystem.pollingFrequency) at runtime.

```c#

// Poll gamepads at 120 Hz.
InputSystem.pollingFrequency = 120;

```

Increased frequency should lead to an increased number of events on the respective devices. The timestamps provided on the events should follow the spacing dictated by the polling frequency. The asynchronous background polling depends on the operating system's thread scheduling and can vary.

> [!NOTE]
> On Windows (XInput controllers only), Universal Windows Platform (UWP), and Switch, Unity polls gamepads explicitly rather than deliver updates as events.
