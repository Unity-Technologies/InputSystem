# Query sensors in code

To determine whether a particular sensor is present, you can use its .current getter.

```c

// Determine if a Gyroscope sensor device is present.
if (Gyroscope.current != null)
    Debug.Log("Gyroscope present");

```

Unlike other devices, sensors are disabled by default. To enable a sensor, call [InputSystem.EnableDevice](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_EnableDevice_UnityEngine_InputSystem_InputDevice_).

```c

InputSystem.EnableDevice(Gyroscope.current);

```

To disable a sensor, call [InputSystem.DisableDevice](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_DisableDevice_UnityEngine_InputSystem_InputDevice_System_Boolean_).

```c

InputSystem.DisableDevice(Gyroscope.current);

```

To check whether a sensor is currently enabled, use [InputDevice.enabled](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_enabled).

```c

if (Gyroscope.current.enabled)
    Debug.Log("Gyroscope is enabled");

```

# Sample sensor frequency

Sensors sample continuously at a set interval. You can set or query the sampling frequency for each sensor using the [samplingFrequency](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Sensor.html#UnityEngine_InputSystem_Sensor_samplingFrequency) property. The frequency is expressed in Hertz (number of samples per second).

```c

// Get sampling frequency of gyro.
var frequency = Gyroscope.current.samplingFrequency;

// Set sampling frequency of gyro to sample 16 times per second.
Gyroscope.current.samplingFrequency = 16;
```

## Measure a device's acceleration

Use the accelerometer to measure the acceleration of a device. This is useful to control content by moving a device around. It reports the acceleration measured on a device both due to moving the device around, and due to gravity pulling the device down. You can use GravitySensor and LinearAccelerationSensor to get separate values for these. Values are affected by the [Compensate Orientation](http://localhost:57437/com.unity.inputsystem@1.12/manual/Settings.html#compensate-orientation) setting.

The following code traces all input events on the [Accelerometer.current](http://localhost:57437/com.unity.inputsystem@1.12/api/UnityEngine.InputSystem.Accelerometer.html) device.

```c

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

## Determine the orientation of a device

Use the attitude sensor to determine the orientation of a device. This is useful to control content by rotating a device. Values are affected by the [Compensate Orientation](http://localhost:57437/com.unity.inputsystem@1.12/manual/Settings.html#compensate-orientation) setting.

On Android devices, there are two types of attitude sensors: [RotationVector](https://developer.android.com/reference/android/hardware/Sensor#TYPE_ROTATION_VECTOR) and [GameRotationVector](https://developer.android.com/reference/android/hardware/Sensor#TYPE_GAME_ROTATION_VECTOR). Some Android devices have both types of sensor, while other devices only have one or the other type available. These two types of attitude sensor behave slightly differently to each other. You can [read about the differences between them here](https://developer.android.com/guide/topics/sensors/sensors_position#sensors-pos-gamerot). 

Because of this variety in what type of rotation sensors are available across devices, when you require input from a rotation sensor on Android devices, you should include code that checks for your preferred type of rotation sensor with a fallback to the alternative type of rotation sensor if it is not present. For example:

```c

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
