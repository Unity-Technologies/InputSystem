---
uid: input-system-unity-remote
---

# Use mobile device input in the Editor (Unity Remote)

The Unity Remote is an app available for iOS and Android which allows using a mobile device for input while running in the Unity Editor. You can find details about the app and how to install it in the [Unity manual](https://docs.unity3d.com/Manual/UnityRemote5.html).

If you would like to try out the Unity Remote app, you can [install](Installation.md#install-samples) the "Unity Remote" sample that is provided with the Input System package.

> [!NOTE]
> Joysticks/gamepads are not yet supported over the Unity Remote. No joystick/gamepad input from the mobile device will come through in the editor.

> [!NOTE]
> This requires Unity 2021.2.18 or later.

When in play mode in the Editor and connected to the Unity Remote app, you will see a number of Devices have been added with the [`InputDevice.remote`](xref:UnityEngine.InputSystem.InputDevice) flag set to true:

- [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen)
- [`Accelerometer`](xref:UnityEngine.InputSystem.Accelerometer)

If a gyro is present on the mobile device:

- [`Gyroscope`](xref:UnityEngine.InputSystem.Gyroscope)
- [`AttitudeSensor`](xref:UnityEngine.InputSystem.AttitudeSensor)
- [`LinearAccelerationSensor`](xref:UnityEngine.InputSystem.LinearAccelerationSensor)
- [`GravitySensor`](xref:UnityEngine.InputSystem.GravitySensor)

These Devices can be used just like local Devices. They will receive input from the connected mobile device which in turn will receive the rendered output of the game running in the editor.

The [`Accelerometer`](xref:UnityEngine.InputSystem.Accelerometer) device will automatically be enabled and will not need you to call [`InputSystem.EnableDevice`](xref:UnityEngine.InputSystem.InputSystem) explicitly. Setting the sampling frequency on the accelerometer from the Unity Remote using [`Sensor.samplingFrequency`](xref:UnityEngine.InputSystem.Sensor) has no effect.

The remaining sensors listed above will need to be explicitly enabled with [`InputSystem.EnableDevice`](xref:UnityEngine.InputSystem.InputSystem) just like local sensors. Setting the sampling frequency on these sensors from the Unity Remote using [`Sensor.samplingFrequency`](xref:UnityEngine.InputSystem.Sensor) will be relayed to the device but note that setting the frequency on one of them will set it for all of them.

Touch coordinates from the device will be translated to the screen coordinates of the Game View inside the Editor.
