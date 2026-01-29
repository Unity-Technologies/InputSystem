---
uid: input-system-sensors
---
# Sensor support

Sensors are [`InputDevices`](xref:input-system-devices) that measure environmental characteristics of the device that the content is running on. Unity currently supports sensors on iOS and Android. Android supports a wider range of sensors than iOS.

> [!NOTE]
> To test your app on iOS or Android in the editor with sensor input from your mobile device, you can use the Unity Remote as described [here](xref:input-system-debugging#unity-remote). This currently supports [`Accelerometer`](#accelerometer), [`Gyroscope`](#gyroscope), [`GravitySensor`](#gravitysensor), [`AttitudeSensor`](#attitudesensor), and [`LinearAccelerationSensor`](#linearaccelerationsensor).

To determine whether a particular sensor is present, you can use its `.current` getter.

```CSharp
// Determine if a Gyroscope sensor device is present.
if (Gyroscope.current != null)
    Debug.Log("Gyroscope present");
```

Unlike other devices, sensors are disabled by default. To enable a sensor, call [`InputSystem.EnableDevice()`](xref:UnityEngine.InputSystem.InputSystem.EnableDevice(UnityEngine.InputSystem.InputDevice))).

```CSharp
InputSystem.EnableDevice(Gyroscope.current);
```

To disable a sensor, call [`InputSystem.DisableDevice()`](xref:UnityEngine.InputSystem.InputSystem.DisableDevice(UnityEngine.InputSystem.InputDevice,System.Boolean)).

```CSharp
InputSystem.DisableDevice(Gyroscope.current);
```

To check whether a sensor is currently enabled, use [`InputDevice.enabled`](xref:UnityEngine.InputSystem.InputDevice.enabled).

```CSharp
if (Gyroscope.current.enabled)
    Debug.Log("Gyroscope is enabled");
```

Each sensor Device implements a single Control which represents the data read by the sensor. The following sensors are available:

|Device|Android|iOS|**WebGL**|Control|Type|
|------|-------|---|-------|----|----|
|[`Accelerometer`](#accelerometer)|Yes|Yes|Yes [<sup>&#8224;</sup>](#fn1)|[`acceleration`](xref:UnityEngine.InputSystem.Accelerometer.acceleration)|[`Vector3Control`](xref:UnityEngine.InputSystem.Controls.Vector3Control)|
|[`Gyroscope`](#gyroscope)|Yes|Yes|Yes [<sup>&#8224;</sup>](#fn1)|[`angularVelocity`](xref:UnityEngine.InputSystem.Gyroscope.angularVelocity)|[`Vector3Control`](xref:UnityEngine.InputSystem.Controls.Vector3Control)|
|[`GravitySensor`](#gravitysensor)|Yes|Yes|Yes [<sup>&#8224;</sup>](#fn1)|[`gravity`](xref:UnityEngine.InputSystem.GravitySensor.gravity)|[`Vector3Control`](xref:UnityEngine.InputSystem.Controls.Vector3Control)|
|[`AttitudeSensor`](#attitudesensor)|Yes|Yes|Yes [<sup>&#8224;</sup>](#fn1)|[`attitude`](xref:UnityEngine.InputSystem.AttitudeSensor.attitude)|[`QuaternionControl`](xref:UnityEngine.InputSystem.Controls.QuaternionControl)|
|[`LinearAccelerationSensor`](#linearaccelerationsensor)|Yes|Yes|Yes [<sup>&#8224;</sup>](#fn1)|[`acceleration`](xref:UnityEngine.InputSystem.LinearAccelerationSensor.acceleration)|[`Vector3Control`](xref:UnityEngine.InputSystem.Controls.Vector3Control)|
|[`MagneticFieldSensor`](#magneticfieldsensor)|Yes|No|No|[`magneticField`](xref:UnityEngine.InputSystem.MagneticFieldSensor.magneticField)|[`Vector3Control`](xref:UnityEngine.InputSystem.Controls.Vector3Control)|
|[`LightSensor`](#lightsensor)|Yes|No|No|[`lightLevel`](xref:UnityEngine.InputSystem.LightSensor.lightLevel)|[`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl)|
|[`PressureSensor`](#pressuresensor)|Yes|No|No|[`atmosphericPressure`](xref:UnityEngine.InputSystem.PressureSensor.atmosphericPressure)|[`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl)|
|[`ProximitySensor`](#proximitysensor)|Yes|No|No|[`distance`](xref:UnityEngine.InputSystem.ProximitySensor.distance)|[`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl)|
|[`HumiditySensor`](#humiditysensor)|Yes|No|No|[`relativeHumidity`](xref:UnityEngine.InputSystem.HumiditySensor.relativeHumidity)|[`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl)|
|[`AmbientTemperatureSensor`](#ambienttemperaturesensor)|Yes|No|No|[`ambientTemperature`](xref:UnityEngine.InputSystem.AmbientTemperatureSensor.ambientTemperature)|[`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl)|
|[`StepCounter`](#stepcounter)|Yes|Yes|No|[`stepCounter`](xref:UnityEngine.InputSystem.StepCounter.stepCounter)|[`IntegerControl`](xref:UnityEngine.InputSystem.Controls.IntegerControl)|
|[`HingeAngle`](#hingeangle)|Yes|No|No|[`angle`](xref:UnityEngine.InputSystem.HingeAngle.angle)|[`AxisControl`](xref:UnityEngine.InputSystem.Controls.AxisControl)|


<a name="fn1" id="fn1"></a>

> [!NOTE]
>   **&#8224;** Sensor support for WebGL on Android and iOS devices is available in Unity 2021.2.

## Sampling frequency

Sensors sample continuously at a set interval. You can set or query the sampling frequency for each sensor using the [`samplingFrequency`](xref:UnityEngine.InputSystem.Sensor.samplingFrequency) property. The frequency is expressed in Hertz (number of samples per second).

```CSharp
// Get sampling frequency of gyro.
var frequency = Gyroscope.current.samplingFrequency;

// Set sampling frequency of gyro to sample 16 times per second.
Gyroscope.current.samplingFrequency = 16;
```

## <a name="accelerometer"></a>[`Accelerometer`](xref:UnityEngine.InputSystem.Accelerometer)

Use the accelerometer to measure the acceleration of a device. This is useful to control content by moving a device around. It reports the acceleration measured on a device both due to moving the device around, and due to gravity pulling the device down. You can use `GravitySensor` and `LinearAccelerationSensor` to get separate values for these. Values are affected by the [__Compensate Orientation__](xref:input-system-settings#compensate-orientation) setting.

 The following code traces all input events on the [`Accelerometer.current`](xref:UnityEngine.InputSystem.Accelerometer) device.
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

## <a name="gyroscope"></a>[`Gyroscope`](xref:UnityEngine.InputSystem.Gyroscope)

Use the gyroscope to measure the angular velocity of a device. This is useful to control content by rotating a device. Values are affected by the [__Compensate Orientation__](xref:input-system-settings#compensate-orientation) setting.

## <a name="gravitysensor"></a>[`GravitySensor`](xref:UnityEngine.InputSystem.GravitySensor)

Use the gravity sensor to determine the direction of the gravity vector relative to a device. This is useful to control content by device orientation. This is usually derived from a hardware `Accelerometer`, by subtracting the effect of linear acceleration (see `LinearAccelerationSensor`). Values are affected by the [__Compensate Orientation__](xref:input-system-settings#compensate-orientation) setting.

## <a name="attitudesensor"></a>[`AttitudeSensor`](xref:UnityEngine.InputSystem.AttitudeSensor)

Use the attitude sensor to determine the orientation of a device. This is useful to control content by rotating a device. Values are affected by the [__Compensate Orientation__](xref:input-system-settings#compensate-orientation) setting.

On Android devices, there are two types of attitude sensors:

- [**RotationVector**](https://developer.android.com/reference/android/hardware/Sensor#TYPE_ROTATION_VECTOR)
- [**GameRotationVector**](https://developer.android.com/reference/android/hardware/Sensor#TYPE_GAME_ROTATION_VECTOR)

Some Android devices have both types of sensor, while other devices may only have one or the other type available. These two types of attitude sensor behave slightly differently to each other. You can [read about the differences between them here](https://developer.android.com/guide/topics/sensors/sensors_position#sensors-pos-gamerot).

Because of this variety in what type of rotation sensors are available across devices, when you require input from a rotation sensor on Android devices, you should include code that checks for your preferred type of rotation sensor with a fallback to the alternative type of rotation sensor if it is not present. For example:

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

## <a name="linearaccelerationsensor"></a>[`LinearAccelerationSensor`](xref:UnityEngine.InputSystem.LinearAccelerationSensor)

Use the accelerometer to measure the acceleration of a device. This is useful to control content by moving a device around. Linear acceleration is the acceleration of a device unaffected by gravity. This is usually derived from a hardware `Accelerometer`, by subtracting the effect of gravity (see `GravitySensor`). Values are affected by the [__Compensate Orientation__](xref:input-system-settings#compensate-orientation) setting.

## <a name="magneticfieldsensor"></a>[`MagneticFieldSensor`](xref:UnityEngine.InputSystem.MagneticFieldSensor)

This Input Device represents the magnetic field that affects the device which is running the content. Values are in micro-Tesla (μT) and measure the ambient magnetic field in the X, Y, and Z axis.

## <a name="lightsensor"></a>[`LightSensor`](xref:UnityEngine.InputSystem.LightSensor)

This Input Device represents the ambient light measured by the device which is running the content. Value is in SI lux units.

## <a name="pressuresensor"></a>[`PressureSensor`](xref:UnityEngine.InputSystem.PressureSensor)

This Input Device represents the atmospheric pressure measured by the device which is running the content. Value is in in hPa (millibar).

## <a name="proximitysensor"></a>[`ProximitySensor`](xref:UnityEngine.InputSystem.ProximitySensor)

This Input Device measures how close the device which is running the content is to the user. Phones typically use the proximity sensor to determine if the user is holding the phone to their ear or not. Values represent distance measured in centimeters.

> [!NOTE]
> The Samsung devices' proximity sensor is only enabled during calls and not when using speakerphone or Bluetooth earphones. This means the lock screen function won't work, allowing the user to use the display during the call. It is important to note that the proximity sensor only works during non-speakerphone or non-Bluetooth calls, as it is designed to prevent accidental touches during calls. However, the proximity sensor can work slightly differently on different Samsung phones.

## <a name="humiditysensor"></a>[`HumiditySensor`](xref:UnityEngine.InputSystem.HumiditySensor)

This Input Device represents the ambient air humidity measured by the device which is running the content. Values represent the relative ambient air humidity in percent.

## <a name="ambienttemperaturesensor"></a>[`AmbientTemperatureSensor`](xref:UnityEngine.InputSystem.AmbientTemperatureSensor)

This Input Device represents the ambient air temperature measured by the device which is running the content. Values represent temperature in Celsius degrees.

## <a name="stepcounter"></a>[`StepCounter`](xref:UnityEngine.InputSystem.StepCounter)

This Input Device represents the user's footstep count as measured by the device which is running the content.

> [!NOTE]
> To access the pedometer on iOS/tvOS devices, you need to enable the [__Motion Usage__ setting](xref:input-system-settings#iostvos) in the [Input Settings](xref:input-system-settings).

## <a name="hingeangle"></a>[`HingeAngle`](xref:UnityEngine.InputSystem.HingeAngle)

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
