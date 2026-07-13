---
uid: input-system-supported-sensors-ref
---

# Supported sensors reference

Each sensor device implements a single control which represents the data read by the sensor. The following sensors are available:

| Sensor | Description | Android | iOS | WebGL |
| :---- | :---- | :---- | :---- | :---- |
| [`Accelerometer`](xref:UnityEngine.InputSystem.Accelerometer) | Measures the acceleration of a device. | Yes | Yes | Yes |
| [`Gyroscope`](xref:UnityEngine.InputSystem.Gyroscope) | Measures the angular velocity of a device. | Yes | Yes | Yes |
| [`GravitySensor`](xref:UnityEngine.InputSystem.GravitySensor) | Determines the direction of the gravity vector relative to the device. | Yes | Yes | Yes |
| [`AttitudeSensor`](xref:UnityEngine.InputSystem.AttitudeSensor) | Determine the orientation of a device. | Yes | Yes | Yes |
| [`LinearAccelerationSensor`](xref:UnityEngine.InputSystem.LinearAccelerationSensor) | Measures the acceleration of a device unaffected by gravity. | Yes | Yes | Yes |
| [`MagneticFieldSensor`](xref:UnityEngine.InputSystem.MagneticFieldSensor) | Represents the magnetic field that affects the device. | Yes | No | No |
| [`LightSensor`](xref:UnityEngine.InputSystem.LightSensor) | Represents the ambient light measured by the device. | Yes | No | No |
| [`PressureSensor`](xref:UnityEngine.InputSystem.PressureSensor) | Represents the atmospheric pressure measured by the device. | Yes | No | No |
| [`ProximitySensor`](xref:UnityEngine.InputSystem.ProximitySensor) | Measures how close the device is to the user. | Yes | No | No |
| [`HumiditySensor`](xref:UnityEngine.InputSystem.HumiditySensor) | Represents the ambient air humidity. | Yes | No | No |
| [`AmbientTemperatureSensor`](xref:UnityEngine.InputSystem.AmbientTemperatureSensor) | Represents the ambient air temperature. | Yes | No | No |
| [`StepCounter`](xref:UnityEngine.InputSystem.StepCounter) | Represents the user's footstep count. | Yes | Yes | No |
| [`HingeAngle`](xref:UnityEngine.InputSystem.HingeAngle) | Represents the hinge angle of foldable devices. | Yes | No | No |
