#if UNITY_EDITOR || UNITY_ANDROID
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Android;
using UnityEngine.Experimental.Input.Plugins.Android.LowLevel;
using NUnit.Framework;

class AndroidSensorTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    public void Devices_CanCreateSensors()
    {
        var sensorTypes = new[] {
            typeof(AndroidAccelerometer),
            typeof(AndroidMagneticField),
            typeof(AndroidOrientation),
            typeof(AndroidGyroscope),
            typeof(AndroidLight),
            typeof(AndroidPressure),
            typeof(AndroidProximity),
            typeof(AndroidTemperature),
            typeof(AndroidGravity),
            typeof(AndroidLinearAcceleration),
            typeof(AndroidRotationVector),
            typeof(AndroidRelativeHumidity),
            typeof(AndroidAmbientTemperature),
            typeof(AndroidMagneticFieldUncalibrated),
            typeof(AndroidGameRotationVector),
            typeof(AndroidGyroscopeUncalibrated),
            typeof(AndroidSignificantMotion),
            typeof(AndroidStepDetector),
            typeof(AndroidStepCounter),
            typeof(AndroidGeomagneticRotationVector),
            typeof(AndroidHeartRate)
        };

        foreach (var s in sensorTypes)
        {
            var setup = new InputDeviceBuilder(s.Name);
            var device = setup.Finish();

            Assert.That(device, Is.AssignableTo<Sensor>());
        }
    }

    private InputDeviceDescription GetSensorDescription(AndroidSenorType type)
    {
        return new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidSensor",
            capabilities = new AndroidSensorCapabilities()
            {
                sensorType = type
            }.ToJson()
        };
    }

    [Test]
    [Category("Devices")]
    public void Devices_AccelerometerWorks()
    {
        var accelerometer = (Accelerometer)InputSystem.AddDevice(GetSensorDescription(AndroidSenorType.Accelerometer));

        InputSystem.QueueStateEvent(accelerometer,
            new AndroidSensorState()
            .WithData(new[] { 0.1f, 0.2f, 0.3f }));

        InputSystem.Update();

        Assert.That(accelerometer.acceleration.ReadValue(), Is.EqualTo(new Vector3(0.1f, 0.2f, 0.3f)).Within(0.000001));
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
