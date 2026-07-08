---
uid: input-system-compensate-orientation
---

# Compensate orientation

If this setting is enabled, rotation values reported by [sensors](devices-sensors.md) are rotated around the Z axis as follows:

|Screen orientation|Effect on rotation values|
|---|---|
|[`ScreenOrientation.Portrait`](https://docs.unity3d.com/ScriptReference/ScreenOrientation.html)|Values remain unchanged|
|[`ScreenOrientation.PortraitUpsideDown`](https://docs.unity3d.com/ScriptReference/ScreenOrientation.html)|Values rotate by 180 degrees.|
|[`ScreenOrientation.LandscapeLeft`](https://docs.unity3d.com/ScriptReference/ScreenOrientation.html)|Values rotate by 90 degrees.|
|[`ScreenOrientation.LandscapeRight`](https://docs.unity3d.com/ScriptReference/ScreenOrientation.html)|Values rotate by 270 degrees.|

This setting affects the following sensors:

* [`Gyroscope`](xref:UnityEngine.InputSystem.Gyroscope)
* [`GravitySensor`](xref:UnityEngine.InputSystem.GravitySensor)
* [`AttitudeSensor`](xref:UnityEngine.InputSystem.AttitudeSensor)
* [`Accelerometer`](xref:UnityEngine.InputSystem.Accelerometer)
* [`LinearAccelerationSensor`](xref:UnityEngine.InputSystem.LinearAccelerationSensor)
