---
uid: input-system-polling-gamepad
---

# Gamepad polling

You can control polling frequency manually. The default polling frequency is 60 Hz. Use [`InputSystem.pollingFrequency`](xref:UnityEngine.InputSystem.InputSystem.pollingFrequency) to get or set the frequency.

```c#

// Poll gamepads at 120 Hz.
InputSystem.pollingFrequency = 120;

```

Increased frequency should lead to an increased number of events on the respective devices. The timestamps provided on the events should follow the spacing dictated by the polling frequency. The asynchronous background polling depends on the operating system's thread scheduling and can vary.

>[!NOTE] 
>On Windows (XInput controllers only), Universal Windows Platform (UWP), and Switch, Unity polls gamepads explicitly rather than deliver updates as events.  