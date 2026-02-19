---
uid: input-system-handle-device-loss
---

# Handle loss of a device 

If paired Input Devices disconnect during the session, the system notifies the [`InputUser`](../api/UnityEngine.InputSystem.Users.InputUser.html) class. It still keeps track of the Device, and automatically re-pairs the Device if it becomes available again.

To get notifications about these changes, subscribe to the [`InputUser.onChange`](../api/UnityEngine.InputSystem.Users.InputUser.html#UnityEngine_InputSystem_Users_InputUser_onChange) event.
