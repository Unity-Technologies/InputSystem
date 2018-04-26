#if UNITY_EDITOR || UNITY_ANDROID
using System;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.Android;
using UnityEngine.Experimental.Input.Plugins.Android.LowLevel;
using NUnit.Framework;

class AndroidSensorTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    [TestCase(typeof(AndroidAccelerometer))]
    [TestCase(typeof(AndroidMagneticField))]
    [TestCase(typeof(AndroidOrientation))]
    [TestCase(typeof(AndroidGyroscope))]
    [TestCase(typeof(AndroidLight))]
    [TestCase(typeof(AndroidPressure))]
    [TestCase(typeof(AndroidProximity))]
    [TestCase(typeof(AndroidTemperature))]
    [TestCase(typeof(AndroidGravity))]
    [TestCase(typeof(AndroidLinearAcceleration))]
    [TestCase(typeof(AndroidRotationVector))]
    [TestCase(typeof(AndroidRelativeHumidity))]
    [TestCase(typeof(AndroidAmbientTemperature))]
    [TestCase(typeof(AndroidMagneticFieldUncalibrated))]
    [TestCase(typeof(AndroidGameRotationVector))]
    [TestCase(typeof(AndroidGyroscopeUncalibrated))]
    [TestCase(typeof(AndroidSignificantMotion))]
    [TestCase(typeof(AndroidStepDetector))]
    [TestCase(typeof(AndroidStepCounter))]
    [TestCase(typeof(AndroidGeomagneticRotationVector))]
    [TestCase(typeof(AndroidHeartRate))]
    public void Devices_CanCreateAndroidSensors(Type type)
    {
        var device = InputSystem.AddDevice(type.Name);

        Assert.That(device, Is.AssignableTo<Sensor>());
        Assert.That(device, Is.TypeOf(type));
    }

    [Test]
    [Category("Devices")]
    public void Devices_SupportsAndroidAccelerometer()
    {
        var accelerometer = (Accelerometer)InputSystem.AddDevice(
                new InputDeviceDescription
        {
            interfaceName = "Android",
            deviceClass = "AndroidSensor",
            capabilities = new AndroidSensorCapabilities()
            {
                sensorType = AndroidSensorType.Accelerometer
            }.ToJson()
        });

        InputSystem.QueueStateEvent(accelerometer,
            new AndroidSensorState()
            .WithData(0.1f, 0.2f, 0.3f));

        InputSystem.Update();

        ////TODO: test processing of AndroidAccelerationProcessor

        Assert.That(accelerometer.acceleration.x.ReadValue(), Is.EqualTo(0.1).Within(0.000001));
        Assert.That(accelerometer.acceleration.y.ReadValue(), Is.EqualTo(0.2).Within(0.000001));
        Assert.That(accelerometer.acceleration.z.ReadValue(), Is.EqualTo(0.3).Within(0.000001));
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
