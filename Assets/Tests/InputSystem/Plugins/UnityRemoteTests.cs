#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools.Utils;
using Gyroscope = UnityEngine.InputSystem.Gyroscope;

internal class UnityRemoteTests : CoreTestsFixture
{
    public override void TearDown()
    {
        UnityRemoteSupport.ResetGlobalState();
        base.TearDown();
    }

    // We have a kill-switch as a safety fallback just in case this feature somehow
    // ends up causing problems.
    [Test]
    [Category("Remote")]
    public void Remote_CanDisableUnityRemoteSupport()
    {
        InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kDisableUnityRemoteSupport, true);

        SendUnityRemoteMessage(UnityRemoteSupport.HelloMessage.Create());

        Assert.That(UnityRemoteSupport.isConnected, Is.False);

        InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kDisableUnityRemoteSupport, false);

        SendUnityRemoteMessage(UnityRemoteSupport.HelloMessage.Create());

        Assert.That(UnityRemoteSupport.isConnected, Is.True);
    }

    [Test]
    [Category("Remote")]
    public void Remote_CanReceiveTouchInputFromUnityRemote()
    {
        // Make sure that our screen geometry *differs* from the one in the remote so
        // we can check whether the scaling works properly.
        runtime.screenSize = new Vector2(1024, 768);

        SendUnityRemoteMessage(UnityRemoteSupport.HelloMessage.Create());

        Assert.That(Touchscreen.current, Is.Not.Null);
        Assert.That(Touchscreen.current.remote, Is.True);

        // Communicate screen size.
        SendUnityRemoteMessage(new UnityRemoteSupport.OptionsMessage
        {
            dimension1 = 640,
            dimension2 = 480
        });

        SendUnityRemoteMessage(new UnityRemoteSupport.TouchInputMessage
        {
            positionX = 123,
            positionY = 234,
            phase = (int)UnityEngine.TouchPhase.Began,
            id = 0 // Old input system allows zero IDs so test with it here.
        });
        InputSystem.Update();

        const float kExpectedX = 123f / 640f * 1024f;
        const float kExpectedY = 234f / 480f * 768f;

        Assert.That(Touchscreen.current.primaryTouch.isInProgress, Is.True);
        Assert.That(Touchscreen.current.primaryTouch.touchId.ReadValue(), Is.EqualTo(1)); // Should +1 to every ID.
        Assert.That(Touchscreen.current.primaryTouch.position.ReadValue(),
            Is.EqualTo(new Vector2(kExpectedX, kExpectedY)).Using(Vector2EqualityComparer.Instance));

        SendUnityRemoteMessage(new UnityRemoteSupport.GoodbyeMessage());

        Assert.That(Touchscreen.current, Is.Null);
    }

    //roll accelerometer into this?
    [Test]
    [Category("Remote")]
    public void Remote_CanReceiveGyroscopeInputFromUnityRemote()
    {
        SendUnityRemoteMessage(UnityRemoteSupport.HelloMessage.Create());

        Assert.That(Gyroscope.current, Is.Null);
        Assert.That(AttitudeSensor.current, Is.Null);
        Assert.That(GravitySensor.current, Is.Null);
        Assert.That(LinearAccelerationSensor.current, Is.Null);

        // Indicate presence but say it's disabled.
        SendUnityRemoteMessage(new UnityRemoteSupport.GyroSettingsMessage { enabled = 0 });

        // The sensor InputDevices we have present the various data contained in the gyro messages
        // from the Unity Remote all as different types of sensors. So, we end up with multiple
        // devices here instead of a single sensor:
        // - Gyroscope: rotationRate
        // - AttitudeSensor: attitude
        // - GravitySensor: gravity
        // - LinearAccelerationSensor: userAcceleration

        Assert.That(Gyroscope.current, Is.Not.Null);
        Assert.That(Gyroscope.current.remote, Is.True);
        Assert.That(Gyroscope.current.enabled, Is.False);

        Assert.That(AttitudeSensor.current, Is.Not.Null);
        Assert.That(AttitudeSensor.current.remote, Is.True);
        Assert.That(AttitudeSensor.current.enabled, Is.False);

        Assert.That(GravitySensor.current, Is.Not.Null);
        Assert.That(GravitySensor.current.remote, Is.True);
        Assert.That(GravitySensor.current.enabled, Is.False);

        Assert.That(LinearAccelerationSensor.current, Is.Not.Null);
        Assert.That(LinearAccelerationSensor.current.remote, Is.True);
        Assert.That(LinearAccelerationSensor.current.enabled, Is.False);

        // Now the app enables the gyro.
        InputSystem.EnableDevice(Gyroscope.current);

        Assert.That(runtime.unityRemoteGyroEnabled, Is.True);
        Assert.That(Gyroscope.current.enabled, Is.True);
        Assert.That(AttitudeSensor.current.enabled, Is.False);
        Assert.That(GravitySensor.current.enabled, Is.False);
        Assert.That(LinearAccelerationSensor.current.enabled, Is.False);

        // Send input on the gyro.
        SendUnityRemoteMessage(new UnityRemoteSupport.GyroInputMessage
        {
            rotationRateX = 123,
            rotationRateY = 234,
            rotationRateZ = 345,
            attitudeX = 456,
            attitudeY = 567,
            attitudeZ = 678,
            attitudeW = 789,
            gravityX = 987,
            gravityY = 876,
            gravityZ = 765,
            userAccelerationX = 654,
            userAccelerationY = 543,
            userAccelerationZ = 432
        });
        InputSystem.Update();

        Assert.That(Gyroscope.current.angularVelocity.ReadValue(), Is.EqualTo(new Vector3(123, 234, 345)));
        Assert.That(AttitudeSensor.current.attitude.ReadValue(), Is.EqualTo(default(Quaternion)));
        Assert.That(GravitySensor.current.gravity.ReadValue(), Is.EqualTo(default(Vector3)));
        Assert.That(LinearAccelerationSensor.current.acceleration.ReadValue(), Is.EqualTo(default(Vector3)));

        // Enable the remaining sensors.
        InputSystem.EnableDevice(AttitudeSensor.current);
        InputSystem.EnableDevice(GravitySensor.current);
        InputSystem.EnableDevice(LinearAccelerationSensor.current);

        Assert.That(runtime.unityRemoteGyroEnabled, Is.True);
        Assert.That(AttitudeSensor.current.enabled, Is.True);
        Assert.That(GravitySensor.current.enabled, Is.True);
        Assert.That(LinearAccelerationSensor.current.enabled, Is.True);

        // Make sure we respond properly if GyroSettingsMessages is received again.
        SendUnityRemoteMessage(new UnityRemoteSupport.GyroSettingsMessage { enabled = 1 });

        Assert.That(InputSystem.devices.Count(d => d is Gyroscope), Is.EqualTo(1));
        Assert.That(InputSystem.devices.Count(d => d is AttitudeSensor), Is.EqualTo(1));
        Assert.That(InputSystem.devices.Count(d => d is GravitySensor), Is.EqualTo(1));
        Assert.That(InputSystem.devices.Count(d => d is LinearAccelerationSensor), Is.EqualTo(1));

        // Update gyro.
        SendUnityRemoteMessage(new UnityRemoteSupport.GyroInputMessage
        {
            rotationRateX = 111,
            rotationRateY = 222,
            rotationRateZ = 333,
            attitudeX = 444,
            attitudeY = 555,
            attitudeZ = 666,
            attitudeW = 777,
            gravityX = 888,
            gravityY = 999,
            gravityZ = 121,
            userAccelerationX = 131,
            userAccelerationY = 141,
            userAccelerationZ = 151
        });
        InputSystem.Update();

        Assert.That(Gyroscope.current.angularVelocity.ReadValue(), Is.EqualTo(new Vector3(111, 222, 333)));
        Assert.That(AttitudeSensor.current.attitude.ReadValue(), Is.EqualTo(new Quaternion(444, 555, 666, 777)));
        Assert.That(GravitySensor.current.gravity.ReadValue(), Is.EqualTo(new Vector3(888, 999, 121)));
        Assert.That(LinearAccelerationSensor.current.acceleration.ReadValue(), Is.EqualTo(new Vector3(131, 141, 151)));

        // Set update interval.
        Gyroscope.current.samplingFrequency = 123.456f;

        Assert.That(runtime.unityRemoteGyroUpdateInterval, Is.EqualTo(123.456f));
        Assert.That(Gyroscope.current.samplingFrequency, Is.EqualTo(123.456f));

        SendUnityRemoteMessage(new UnityRemoteSupport.GoodbyeMessage());

        Assert.That(Gyroscope.current, Is.Null);
        Assert.That(AttitudeSensor.current, Is.Null);
        Assert.That(GravitySensor.current, Is.Null);
        Assert.That(LinearAccelerationSensor.current, Is.Null);
    }

    [Test]
    [Category("Remote")]
    public void Remote_CanReceiveAccelerometerInputFromUnityRemote()
    {
        SendUnityRemoteMessage(UnityRemoteSupport.HelloMessage.Create());

        // Should always be created. For gyros, we have explicit "is present?" logic but
        // accelerometers are assumed to be present on every mobile device running the Unity Remote.
        Assert.That(Accelerometer.current, Is.Not.Null);
        Assert.That(Accelerometer.current.remote, Is.True);

        // Also, it should not require explicit enabling.
        Assert.That(Accelerometer.current.enabled, Is.True);

        // Update acceleration.
        SendUnityRemoteMessage(new UnityRemoteSupport.AccelerometerInputMessage
        {
            accelerationX = 123,
            accelerationY = 234,
            accelerationZ = 345,
            deltaTime = 456
        });
        InputSystem.Update();

        Assert.That(Accelerometer.current.acceleration.ReadValue(), Is.EqualTo(new Vector3(123, 234, 345)));

        // Disabling it should work as normal.
        InputSystem.DisableDevice(Accelerometer.current);

        SendUnityRemoteMessage(new UnityRemoteSupport.AccelerometerInputMessage
        {
            accelerationX = 234,
            accelerationY = 345,
            accelerationZ = 456,
            deltaTime = 567
        });
        InputSystem.Update();

        Assert.That(Accelerometer.current.acceleration.ReadValue(), Is.EqualTo(new Vector3(123, 234, 345)));

        InputSystem.EnableDevice(Accelerometer.current);

        SendUnityRemoteMessage(new UnityRemoteSupport.AccelerometerInputMessage
        {
            accelerationX = 345,
            accelerationY = 456,
            accelerationZ = 567,
            deltaTime = 678
        });
        InputSystem.Update();

        Assert.That(Accelerometer.current.acceleration.ReadValue(), Is.EqualTo(new Vector3(345, 456, 567)));

        SendUnityRemoteMessage(new UnityRemoteSupport.GoodbyeMessage());

        Assert.That(Accelerometer.current, Is.Null);
    }

    // We don't currently support joystick input coming from the Unity Remote.
    [Test]
    [Category("Remote")]
    [Ignore("TODO")]
    public void TODO_Remote_CanReceiveJoystickInputFromUnityRemote()
    {
        Assert.Fail();
    }

    private unsafe void SendUnityRemoteMessage<TMessage>(TMessage message)
        where TMessage : unmanaged, UnityRemoteSupport.IUnityRemoteMessage
    {
        if (runtime.onUnityRemoteMessage == null)
            return;

        var ptr = UnsafeUtility.AddressOf(ref message);
        *(byte*)ptr = message.staticType;
        *(int*)((byte*)ptr + 1) = UnsafeUtility.SizeOf<TMessage>();

        runtime.onUnityRemoteMessage(new IntPtr(ptr));
    }
}
#endif // UNITY_EDITOR
