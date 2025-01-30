---
uid: input-system-joystick
---
# Joysticks

The Input System currently supports joysticks as generic [HIDs](http://localhost:57437/com.unity.inputsystem@1.12/manual/HID.html) only. The system attempts to identify controls based on the information provided in the HID descriptor of the device, but it might not always be accurate. These devices often work best when you allow the user to [manually remap the controls](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputActionRebindingExtensions.html).

To better support specific joysticks devices, you can also [provide your own custom mappings for those devices](http://localhost:57437/com.unity.inputsystem@1.12/manual/HID.html#creating-a-custom-device-layout). For more information, refer to the [HID](http://localhost:57437/com.unity.inputsystem@1.12/manual/HID.html) documentation.

## Controls

The Input System supports Generic HID Input Devices which are recognized as joysticks via the [Joystick](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Joystick.html) class. Joystick Devices can have any number of Controls as reported by the Device's HID descriptor.
