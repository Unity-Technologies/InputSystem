using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

partial class CoreTests
{
    [Test]
    [Category("API")]
    public void API_CanReadGyroThroughGyroscopeAPI()
    {
        var gyro = InputSystem.AddDevice<Gyroscope>();
        var accel = InputSystem.AddDevice<LinearAccelerationSensor>();
        var gravity = InputSystem.AddDevice<GravitySensor>();
        var attitude = InputSystem.AddDevice<AttitudeSensor>();

        Assert.That(Input.isGyroAvailable, Is.True);

        ////TODO
    }

    [Test]
    [Category("API")]
    public void API_CanReadGyroThroughGyroscopeAPI_WhenNoGyroIsPresent_AllValuesRemainAtDefault()
    {
        Assert.That(Input.isGyroAvailable, Is.False);

        // Presence of gyro is *not* indicated by null property. Instead, it returns
        // a gyro where all values are at default and don't change.
        Assert.That(Input.gyro, Is.Not.Null);
        Assert.That(Input.gyro.enabled, Is.False);
        Assert.That(Input.gyro.attitude, Is.EqualTo(default(Quaternion)));
        Assert.That(Input.gyro.rotationRate, Is.EqualTo(default(Vector3)));
        Assert.That(Input.gyro.rotationRateUnbiased, Is.EqualTo(default(Vector3)));
        Assert.That(Input.gyro.userAcceleration, Is.EqualTo(default(Vector3)));
        Assert.That(Input.gyro.updateInterval, Is.EqualTo(default(float)));

        // When there is no gyro, enabling it should do nothing.
        Assert.That(() => Input.gyro.enabled = true, Throws.Nothing);
        Assert.That(Input.gyro.enabled, Is.False);
        Assert.That(() => Input.gyro.updateInterval = 0.123f, Throws.Nothing);
        Assert.That(Input.gyro.updateInterval, Is.EqualTo(default(float)));
    }
}
