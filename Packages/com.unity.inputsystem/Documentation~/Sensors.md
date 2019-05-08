    ////WIP

# Sensor Support

Sensors are currently only supported on iOS and Android.

Unlike other devices, sensors start out being disabled. To enable a sensor, call `InputSystem.EnableDevice()`.

```
InputSystem.EnableDevice(Gyroscope.current);
```

To disable a sensor again, call `InputSystem.DisableDevice()`.

```
InputSystem.DisableDevice(Gyroscope.current);
```

To check whether a sensor is currently enabled, use `InputDevice.enabled`.

```
if (Gyroscope.current.enabled)
    Debug.Log("Gyroscope is enabled");
```

## Sampling Frequency

Sensors sample continuously at a set interval. The sampling frequency for each sensors can queried or set using the `samplingFrequency` property. The frequency is expressed in Hertz (i.e. number of samples per second).

```
// Get sampling frequency of gyro.
var frequency = Gyroscope.current.samplingFrequency;

// Set sampling frequency of gyro to sample 16 times per second.
Gyroscope.current.samplingFrequency = 16;
```

## Gyroscope

## Accelerometer

## Compass
