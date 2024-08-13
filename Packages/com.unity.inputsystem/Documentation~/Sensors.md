---
uid: input-system-sensors
---
# Sensor support

- [Sampling frequency](#sampling-frequency)
- [`Accelerometer`](#accelerometer)
- [`Gyroscope`](#gyroscope)
- [`GravitySensor`](#gravitysensor)
- [`AttitudeSensor`](#attitudesensor)
- [`LinearAccelerationSensor`](#linearaccelerationsensor)
- [`MagneticFieldSensor`](#magneticfieldsensor)
- [`LightSensor`](#lightsensor)
- [`PressureSensor`](#pressuresensor)
- [`ProximitySensor`](#proximitysensor)
- [`HumiditySensor`](#humiditysensor)
- [`AmbientTemperatureSensor`](#ambienttemperaturesensor)
- [`StepCounter`](#stepcounter)
- [`HingeAngle`](#hingeangle)

Sensors are [`InputDevices`](Devices.md) that measure environmental characteristics of the device that the content is running on. Unity currently supports sensors on iOS and Android. Android supports a wider range of sensors than iOS.

>__Note__: To test your app on iOS or Android in the editor with sensor input from your mobile device, you can use the Unity Remote as described [here](Debugging.md#unity-remote). This currently supports [`Accelerometer`](#accelerometer), [`Gyroscope`](#gyroscope), [`GravitySensor`](#gravitysensor), [`AttitudeSensor`](#attitudesensor), and [`LinearAccelerationSensor`](#linearaccelerationsensor).

To determine whether a particular sensor is present, you can use its `.current` getter.

```CSharp
// Determine if a Gyroscope sensor device is present.
if (Gyroscope.current != null)
    Debug.Log("Gyroscope present");
```

Unlike other devices, sensors are disabled by default. To enable a sensor, call [`InputSystem.EnableDevice()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_EnableDevice_UnityEngine_InputSystem_InputDevice_)).

```CSharp
InputSystem.EnableDevice(Gyroscope.current);
```

To disable a sensor, call [`InputSystem.DisableDevice()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_DisableDevice_UnityEngine_InputSystem_InputDevice_System_Boolean_).

```CSharp
InputSystem.DisableDevice(Gyroscope.current);
```

To check whether a sensor is currently enabled, use [`InputDevice.enabled`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_enabled).

```CSharp
if (Gyroscope.current.enabled)
    Debug.Log("Gyroscope is enabled");
```

Each sensor Device implements a single Control which represents the data read by the sensor. The following sensors are available:

|Device|Android|iOS|**WebGL**|Control|Type|
|------|-------|---|-------|----|----|
|[`Accelerometer`](#accelerometer)|Yes|Yes|Yes(1)|[`acceleration`](../api/UnityEngine.InputSystem.Accelerometer.html#UnityEngine_InputSystem_Accelerometer_acceleration)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`Gyroscope`](#gyroscope)|Yes|Yes|Yes(1)|[`angularVelocity`](../api/UnityEngine.InputSystem.Gyroscope.html#UnityEngine_InputSystem_Gyroscope_angularVelocity)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`GravitySensor`](#gravitysensor)|Yes|Yes|Yes(1)|[`gravity`](../api/UnityEngine.InputSystem.GravitySensor.html#UnityEngine_InputSystem_GravitySensor_gravity)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`AttitudeSensor`](#attitudesensor)|Yes|Yes|Yes(1)|[`attitude`](../api/UnityEngine.InputSystem.AttitudeSensor.html#properties)|[`QuaternionControl`](../api/UnityEngine.InputSystem.Controls.QuaternionControl.html)|
|[`LinearAccelerationSensor`](#linearaccelerationsensor)|Yes|Yes|Yes(1)|[`acceleration`](../api/UnityEngine.InputSystem.LinearAccelerationSensor.html#UnityEngine_InputSystem_LinearAccelerationSensor_acceleration)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`MagneticFieldSensor`](#magneticfieldsensor)|Yes|No|No|[`magneticField`](../api/UnityEngine.InputSystem.MagneticFieldSensor.html#UnityEngine_InputSystem_MagneticFieldSensor_magneticField)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`LightSensor`](#lightsensor)|Yes|No|No|[`lightLevel`](../api/UnityEngine.InputSystem.LightSensor.html#UnityEngine_InputSystem_LightSensor_lightLevel)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`PressureSensor`](#pressuresensor)|Yes|No|No|[`atmosphericPressure`](../api/UnityEngine.InputSystem.PressureSensor.html#UnityEngine_InputSystem_PressureSensor_atmosphericPressure)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`ProximitySensor`](#proximitysensor)|Yes|No|No|[`distance`](../api/UnityEngine.InputSystem.ProximitySensor.html#UnityEngine_InputSystem_ProximitySensor_distance)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`HumiditySensor`](#humiditysensor)|Yes|No|No|[`relativeHumidity`](../api/UnityEngine.InputSystem.HumiditySensor.html#UnityEngine_InputSystem_HumiditySensor_relativeHumidity)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`AmbientTemperatureSensor`](#ambienttemperaturesensor)|Yes|No|No|[`ambientTemperature`](../api/UnityEngine.InputSystem.AmbientTemperatureSensor.html#UnityEngine_InputSystem_AmbientTemperatureSensor_ambientTemperature)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`StepCounter`](#stepcounter)|Yes|Yes|No|[`stepCounter`](../api/UnityEngine.InputSystem.StepCounter.html#UnityEngine_InputSystem_StepCounter_stepCounter)|[`IntegerControl`](../api/UnityEngine.InputSystem.Controls.IntegerControl.html)|
|[`HingeAngle`](#hingeangle)|Yes|No|No|[`angle`](../api/UnityEngine.InputSystem.HingeAngle.html#UnityEngine_InputSystem_HingeAngle_angle)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|

>__Notes__:
>1. Sensor support for WebGL on Android and iOS devices is available in Unity 2021.2

## Sampling frequency

Sensors sample continuously at a set interval. You can set or query the sampling frequency for each sensor using the [`samplingFrequency`](../api/UnityEngine.InputSystem.Sensor.html#UnityEngine_InputSystem_Sensor_samplingFrequency) property. The frequency is expressed in Hertz (number of samples per second).

```CSharp
// Get sampling frequency of gyro.
var frequency = Gyroscope.current.samplingFrequency;

// Set sampling frequency of gyro to sample 16 times per second.
Gyroscope.current.samplingFrequency = 16;
```

## <a name="accelerometer"></a>[`Accelerometer`](../api/UnityEngine.InputSystem.Accelerometer.html)

Use the accelerometer to measure the acceleration of a device. This is useful to control content by moving a device around. It reports the acceleration measured on a device both due to moving the device around, and due to gravity pulling the device down. You can use `GravitySensor` and `LinearAccelerationSensor` to get separate values for these. Values are affected by the [__Compensate Orientation__](Settings.md#compensate-orientation) setting.

 The following code traces all input events on the [`Accelerometer.current`](../api/UnityEngine.InputSystem.Accelerometer.html) device.
```CSharp
    private InputEventTrace trace;

    void StartTrace()
    {
        InputSystem.EnableDevice(Accelerometer.current);

        trace = new InputEventTrace(Accelerometer.current);
        trace.Enable();
    }

    void Update()
    {
        foreach (var e in trace)
        {
            //...
        }
        trace.Clear();
    }
```

## <a name="gyroscope"></a>[`Gyroscope`](../api/UnityEngine.InputSystem.Gyroscope.html)

Use the gyroscope to measure the angular velocity of a device. This is useful to control content by rotating a device. Values are affected by the [__Compensate Orientation__](Settings.md#compensate-orientation) setting.

## <a name="gravitysensor"></a>[`GravitySensor`](../api/UnityEngine.InputSystem.GravitySensor.html)

Use the gravity sensor to determine the direction of the gravity vector relative to a device. This is useful to control content by device orientation. This is usually derived from a hardware `Accelerometer`, by subtracting the effect of linear acceleration (see `LinearAccelerationSensor`). Values are affected by the [__Compensate Orientation__](Settings.md#compensate-orientation) setting.

## <a name="attitudesensor"></a>[`AttitudeSensor`](../api/UnityEngine.InputSystem.AttitudeSensor.html)

Use the attitude sensor to determine the orientation of a device. This is useful to control content by rotating a device. Values are affected by the [__Compensate Orientation__](Settings.md#compensate-orientation) setting.

**Note**: On Android devices, there are two types of attitude sensors: [**RotationVector**](https://developer.android.com/reference/android/hardware/Sensor#TYPE_ROTATION_VECTOR) and [**GameRotationVector**](https://developer.android.com/reference/android/hardware/Sensor#TYPE_GAME_ROTATION_VECTOR). Some Android devices have both types of sensor, while other devices may only have one or the other type available. These two types of attitude sensor behave slightly differently to each other. You can [read about the differences between them here](https://developer.android.com/guide/topics/sensors/sensors_position#sensors-pos-gamerot). Because of this variety in what type of rotation sensors are available across devices, when you require input from a rotation sensor on Android devices, you should include code that checks for your preferred type of rotation sensor with a fallback to the alternative type of rotation sensor if it is not present. For example:

```CSharp
AttitudeSensor attitudeSensor = InputSystem.GetDevice<AndroidRotationVector>();
if (attitudeSensor == null)
{
    attitudeSensor = InputSystem.GetDevice<AndroidGameRotationVector>();
    if (attitudeSensor == null)
       Debug.LogError("AttitudeSensor is not available");
}

if (attitudeSensor != null)
    InputSystem.EnableDevice(attitudeSensor);
```

## <a name="linearaccelerationsensor"></a>[`LinearAccelerationSensor`](../api/UnityEngine.InputSystem.LinearAccelerationSensor.html)

Use the accelerometer to measure the acceleration of a device. This is useful to control content by moving a device around. Linear acceleration is the acceleration of a device unaffected by gravity. This is usually derived from a hardware `Accelerometer`, by subtracting the effect of gravity (see `GravitySensor`). Values are affected by the [__Compensate Orientation__](Settings.md#compensate-orientation) setting.

## <a name="magneticfieldsensor"></a>[`MagneticFieldSensor`](../api/UnityEngine.InputSystem.MagneticFieldSensor.html)

This Input Device represents the magnetic field that affects the device which is running the content. Values are in micro-Tesla (μT) and measure the ambient magnetic field in the X, Y, and Z axis.

## <a name="lightsensor"></a>[`LightSensor`](../api/UnityEngine.InputSystem.LightSensor.html)

This Input Device represents the ambient light measured by the device which is running the content. Value is in SI lux units.

## <a name="pressuresensor"></a>[`PressureSensor`](../api/UnityEngine.InputSystem.PressureSensor.html)

This Input Device represents the atmospheric pressure measured by the device which is running the content. Value is in in hPa (millibar).

## <a name="proximitysensor"></a>[`ProximitySensor`](../api/UnityEngine.InputSystem.ProximitySensor.html)

This Input Device measures how close the device which is running the content is to the user. Phones typically use the proximity sensor to determine if the user is holding the phone to their ear or not. Values represent distance measured in centimeters.

>NOTE: The Samsung devices' proximity sensor is only enabled during calls and not when using speakerphone or Bluetooth earphones. This means the lock screen function won't work, allowing the user to use the display during the call. It is important to note that the proximity sensor only works during non-speakerphone or non-Bluetooth calls, as it is designed to prevent accidental touches during calls. However, the proximity sensor can work slightly differently on different Samsung phones.

## <a name="humiditysensor"></a>[`HumiditySensor`](../api/UnityEngine.InputSystem.HumiditySensor.html)

This Input Device represents the ambient air humidity measured by the device which is running the content. Values represent the relative ambient air humidity in percent.

## <a name="ambienttemperaturesensor"></a>[`AmbientTemperatureSensor`](../api/UnityEngine.InputSystem.AmbientTemperatureSensor.html)

This Input Device represents the ambient air temperature measured by the device which is running the content. Values represent temperature in Celsius degrees.

## <a name="stepcounter"></a>[`StepCounter`](../api/UnityEngine.InputSystem.StepCounter.html)

This Input Device represents the user's footstep count as measured by the device which is running the content.

>NOTE: To access the pedometer on iOS/tvOS devices, you need to enable the [__Motion Usage__ setting](Settings.md#iostvos) in the [Input Settings](Settings.md).

## <a name="hingeangle"></a>[`HingeAngle`](../api/UnityEngine.InputSystem.HingeAngle.html)

This Input Device represents hinge angle for foldable devices. For ex., Google Fold Android phone.

```CSharp
    [Serializable]
    class SensorCapabilities
    {
        public int sensorType;
        public float resolution;
        public int minDelay;
    }

    void Start()
    {
        if (HingeAngle.current != null)
        {
            InputSystem.EnableDevice(HingeAngle.current);
            var caps = JsonUtility.FromJson<SensorCapabilities>(HingeAngle.current.description.capabilities);
            Debug.Log($"HingeAngle Capabilities: resolution = {caps.resolution}, minDelay = {caps.minDelay}");
        }
    }

    void Update()
    {
        if (HingeAngle.current != null)
            Debug.Log($"HingeAngle={HingeAngle.current.angle.ReadValue()}");
    }
```
