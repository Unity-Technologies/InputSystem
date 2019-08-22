# Sensor Support

Sensors are [`InputDevices`](Devices.md) measuring environmental characteristics of the device playing the content. Sensors are currently supported on iOS and Android (with Android supporting a wider range of sensors then iOS).

Unlike other devices, sensors start out being disabled. To enable a sensor, call [`InputSystem.EnableDevice()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_EnableDevice_UnityEngine_InputSystem_InputDevice_)).

```
InputSystem.EnableDevice(Gyroscope.current);
```

To disable a sensor again, call [`InputSystem.DisableDevice()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_DisableDevice_UnityEngine_InputSystem_InputDevice_).

```
InputSystem.DisableDevice(Gyroscope.current);
```

To check whether a sensor is currently enabled, use [`InputDevice.enabled`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_enabled).

```
if (Gyroscope.current.enabled)
    Debug.Log("Gyroscope is enabled");
```

Each sensor device implements a single control which represents the data read by the sensor. The following Sensors are available:

|Device|Android|iOS|Control|Type|
|------|-------|---|-------|----|
|[`Accelerometer`](#accelerometer)|Yes|Yes|[`acceleration`](../api/UnityEngine.InputSystem.Accelerometer.html#UnityEngine_InputSystem_Accelerometer_acceleration)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`Gyroscope`](#gyroscope)|Yes|Yes|[`angularVelocity`](../api/UnityEngine.InputSystem.Gyroscope.html#UnityEngine_InputSystem_Gyroscope_angularVelocity)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`GravitySensor`](#gravitysensor)|Yes|Yes|[`gravity`](../api/UnityEngine.InputSystem.GravitySensor.html#UnityEngine_InputSystem_GravitySensor_gravity)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`AttitudeSensor`](#attitudesensor)|Yes|Yes|[`attitude`](../api/UnityEngine.InputSystem.AttitudeSensor.html#properties)|[`QuaternionControl`](../api/UnityEngine.InputSystem.Controls.QuaternionControl.html)|
|[`LinearAccelerationSensor`](#linearaccelerationsensor)|Yes|Yes|[`acceleration`](../api/UnityEngine.InputSystem.LinearAccelerationSensor.html#UnityEngine_InputSystem_LinearAccelerationSensor_acceleration)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`MagneticFieldSensor`](#magneticfieldsensor)|Yes|No|[`magneticField`](../api/UnityEngine.InputSystem.MagneticFieldSensor.html#UnityEngine_InputSystem_MagneticFieldSensor_magneticField)|[`Vector3Control`](../api/UnityEngine.InputSystem.Controls.Vector3Control.html)|
|[`LightSensor`](#lightsensor)|Yes|No|[`lightLevel`](../api/UnityEngine.InputSystem.LightSensor.html#UnityEngine_InputSystem_LightSensor_lightLevel)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`PressureSensor`](#pressuresensor)|Yes|No|[`atmosphericPressure`](../api/UnityEngine.InputSystem.PressureSensor.html#UnityEngine_InputSystem_PressureSensor_atmosphericPressure)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`ProximitySensor`](#proximitysensor)|Yes|No|[`distance`](../api/UnityEngine.InputSystem.ProximitySensor.html#UnityEngine_InputSystem_ProximitySensor_distance)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`HumiditySensor`](#humiditysensor)|Yes|No|[`relativeHumidity`](../api/UnityEngine.InputSystem.HumiditySensor.html#UnityEngine_InputSystem_HumiditySensor_relativeHumidity)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`AmbientTemperatureSensor`](#ambienttemperaturesensor)|Yes|No|[`ambientTemperature`](../api/UnityEngine.InputSystem.AmbientTemperatureSensor.html#UnityEngine_InputSystem_AmbientTemperatureSensor_ambientTemperature)|[`AxisControl`](../api/UnityEngine.InputSystem.Controls.AxisControl.html)|
|[`StepCounter`](#stepcounter)|Yes|No|[`stepCounter`](../api/UnityEngine.InputSystem.StepCounter.html#UnityEngine_InputSystem_StepCounter_stepCounter)|[`IntegerControl`](../api/UnityEngine.InputSystem.Controls.IntegerControl.html)|

## Sampling Frequency

Sensors sample continuously at a set interval. The sampling frequency for each sensors can queried or set using the [`samplingFrequency`](../api/UnityEngine.InputSystem.Sensor.html#UnityEngine_InputSystem_Sensor_samplingFrequency) property. The frequency is expressed in Hertz (i.e. number of samples per second).

```
// Get sampling frequency of gyro.
var frequency = Gyroscope.current.samplingFrequency;

// Set sampling frequency of gyro to sample 16 times per second.
Gyroscope.current.samplingFrequency = 16;
```

## <a name="accelerometer"></a>[`Accelerometer`](../api/UnityEngine.InputSystem.Accelerometer.html)

An accelerometer lets you measure the acceleration of a device, and can be useful to control content by moving a device around. Note that the accelerometer will report the acceleration measured on a device both due to moving the device around, and due gravity pulling the device down. You can use `GravitySensor` and `LinearAccelerationSensor` to get decoupled values for these.

## <a name="gyroscope"></a>[`Gyroscope`](../api/UnityEngine.InputSystem.Gyroscope.html)

A gyroscope let's you measure the angular velocity of a device, and can be useful to control content by rotating a device.

## <a name="gravitysensor"></a>[`GravitySensor`](../api/UnityEngine.InputSystem.GravitySensor.html)

A gravity sensor let's you determine the direction of the gravity vector relative to a device, and can be useful to control content by device orientation. This is usually derived from a hardware `Accelerometer`, by subtracting the effect of linear acceleration (see `LinearAccelerationSensor`).

## <a name="attitudesensor"></a>[`AttitudeSensor`](../api/UnityEngine.InputSystem.AttitudeSensor.html)

An attitude sensor let's you determine the orientation of a device, and can be useful to control content by rotating a device.

## <a name="linearaccelerationsensor"></a>[`LinearAccelerationSensor`](../api/UnityEngine.InputSystem.LinearAccelerationSensor.html)

An accelerometer let's you measure the acceleration of a device, and can be useful to control content by moving a device around. Linear acceleration is the acceleration of a device unaffected by gravity forces. This is usually derived from a hardware `Accelerometer`, by subtracting the effect of gravity (see `GravitySensor`).

## <a name="magneticfieldsensor"></a>[`MagneticFieldSensor`](../api/UnityEngine.InputSystem.MagneticFieldSensor.html)

Input device representing the magnetic field affecting the device playing the content. Values are in micro-Tesla (uT) and measure the ambient magnetic field in the X, Y and Z axis.

## <a name="lightsensor"></a>[`LightSensor`](../api/UnityEngine.InputSystem.LightSensor.html)

Input device representing the ambient light measured by the device playing the content. Value is in SI lux units.

## <a name="pressuresensor"></a>[`PressureSensor`](../api/UnityEngine.InputSystem.PressureSensor.html)

Input device representing the atmospheric pressure measured by the device playing the content. Value is in in hPa (millibar).

## <a name="proximitysensor"></a>[`ProximitySensor`](../api/UnityEngine.InputSystem.ProximitySensor.html)

Input device representing the proximity of the device playing the content to the user. The proximity sensor is usually used by phones to determine if the user is holding the phone to their ear or not. Values represent distance measured in centimeters.

## <a name="humiditysensor"></a>[`HumiditySensor`](../api/UnityEngine.InputSystem.HumiditySensor.html)

Input device representing the ambient air humidity measured by the device playing the content. Values represent the relative ambient air humidity in percent.

## <a name="ambienttemperaturesensor"></a>[`AmbientTemperatureSensor`](../api/UnityEngine.InputSystem.AmbientTemperatureSensor.html)

Input device representing the ambient air temperature measured by the device playing the content. Values represent temperature in degree Celsius.

## <a name="stepcounter"></a>[`StepCounter`](../api/UnityEngine.InputSystem.StepCounter.html)

Input device representing the foot steps taken by the user as measured by the device playing the content.
