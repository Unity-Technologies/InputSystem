using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using InputSystem = UnityEngine.InputSystem.InputSystem;
using Touchscreen = UnityEngine.InputSystem.Touchscreen;

partial class CoreTests
{
    public override void TearDown()
    {
        UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
        base.TearDown();
    }

    private static bool TouchesAreEqual(Touch first, Touch second)
    {
        return
            first.fingerId == second.fingerId &&
            first.position == second.position &&
            first.rawPosition == second.rawPosition &&
            first.deltaPosition == second.deltaPosition &&
            first.deltaTime == second.deltaTime &&
            first.tapCount == second.tapCount &&
            first.phase == second.phase &&

            first.pressure == second.pressure &&
            first.maximumPossiblePressure == second.maximumPossiblePressure &&
            first.type == second.type &&
            first.altitudeAngle == second.altitudeAngle &&
            first.azimuthAngle == second.azimuthAngle &&
            first.radius == second.radius &&
            first.radiusVariance == second.radius;
    }

    [UnityTest]
    [Category("API")]
    public IEnumerator API_CanReadTouchEventsThroughTouchesAPI()
    {
        // Check default values before device is enabled
        Assert.That(Input.touchSupported, Is.False);
        Assert.That(Input.multiTouchEnabled, Is.True);
        Assert.That(Input.touchCount, Is.EqualTo(0));
        Assert.That(Input.touches.Length, Is.EqualTo(Input.touchCount));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => { Input.GetTouch(0); });

        var touch = InputSystem.AddDevice<Touchscreen>();

        // Check values after device is enabled
        Assert.That(Input.touchSupported, Is.True);
        Assert.That(Input.multiTouchEnabled, Is.True);
        Assert.That(Input.touchCount, Is.EqualTo(0));
        Assert.That(Input.touches.Length, Is.EqualTo(Input.touchCount));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => { Input.GetTouch(0); });

        // Perform input
        BeginTouch(1, new Vector2(1, 2), time: 0.3, queueEventOnly: true);
        yield return null;
        Assert.That(Input.touchCount, Is.EqualTo(1));
        Assert.That(Input.touches.Length, Is.EqualTo(Input.touchCount));
        {
            var first = Input.GetTouch(0);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => { Input.GetTouch(1); });
            Assert.IsTrue(TouchesAreEqual(Input.touches[0], first));

            Assert.That(first.fingerId, Is.EqualTo(1));
            Assert.That(first.position, Is.EqualTo(new Vector2(1.0f, 2.0f)));
            Assert.That(first.rawPosition, Is.EqualTo(new Vector2(1.0f, 2.0f)));
            Assert.That(first.deltaPosition, Is.EqualTo(new Vector2(0.0f, 0.0f)));
            Assert.That(first.deltaTime, Is.EqualTo(0.0f).Within(float.Epsilon));
            Assert.That(first.tapCount, Is.EqualTo(0));
            Assert.That(first.phase, Is.EqualTo(TouchPhase.Began));
            Assert.That(first.pressure, Is.EqualTo(1.0f));
            Assert.That(first.maximumPossiblePressure, Is.EqualTo(1.0f));
            Assert.That(first.type, Is.EqualTo(TouchType.Direct));
            Assert.That(first.altitudeAngle, Is.EqualTo(0));
            Assert.That(first.azimuthAngle, Is.EqualTo(0));
        }
        InputSystem.Update();

        // Continue motion
        MoveTouch(1, new Vector2(15, 25), time: 0.4, queueEventOnly: true);
        yield return null;
        Assert.That(Input.touchCount, Is.EqualTo(1));
        Assert.That(Input.touches.Length, Is.EqualTo(Input.touchCount));
        {
            var middle = Input.GetTouch(0);
            Assert.IsTrue(TouchesAreEqual(Input.touches[0], middle));

            Assert.That(middle.fingerId, Is.EqualTo(1));
            Assert.That(middle.position, Is.EqualTo(new Vector2(15.0f, 25.0f)));
            Assert.That(middle.rawPosition, Is.EqualTo(new Vector2(1.0f, 2.0f)));
            Assert.That(middle.deltaPosition, Is.EqualTo(new Vector2(14.0f, 23.0f)));
            Assert.That(middle.deltaTime, Is.EqualTo(0.1f).Within(float.Epsilon));
            Assert.That(middle.phase, Is.EqualTo(TouchPhase.Moved));
        }
        InputSystem.Update();

        // Finish motion
        EndTouch(1, new Vector2(15, 25), time: 0.55, queueEventOnly: true);
        yield return null;
        Assert.That(Input.touchCount, Is.EqualTo(1));
        Assert.That(Input.touches.Length, Is.EqualTo(Input.touchCount));
        {
            var last = Input.GetTouch(0);
            Assert.IsTrue(TouchesAreEqual(Input.touches[0], last));

            Assert.That(last.fingerId, Is.EqualTo(1));
            Assert.That(last.position, Is.EqualTo(new Vector2(15.0f, 25.0f)));
            Assert.That(last.rawPosition, Is.EqualTo(new Vector2(1.0f, 2.0f)));
            Assert.That(last.deltaPosition, Is.EqualTo(new Vector2(0.0f, 0.0f)));
            Assert.That(last.deltaTime, Is.EqualTo(0.15f).Within(float.Epsilon));
            Assert.That(last.phase, Is.EqualTo(TouchPhase.Ended));
        }

        InputSystem.Update();
        yield return null;
        Assert.That(Input.touchCount, Is.EqualTo(0));
        Assert.That(Input.touches.Length, Is.EqualTo(Input.touchCount));
    }

    [UnityTest]
    [Category("API")]
    public IEnumerator API_CanReadTouchTapEventsThroughTouchesAPI()
    {
        var touch = InputSystem.AddDevice<Touchscreen>();
        {
            // Perform tap
            BeginTouch(1, new Vector2(1.5f, 2.5f), time: 0.3);
            EndTouch(1, new Vector2(1.5f, 2.5f), time: 0.35, queueEventOnly: true);
            yield return null;

            Assert.That(Input.touchCount, Is.EqualTo(1));
            var t = Input.GetTouch(0);
            Assert.That(t.deltaTime, Is.EqualTo(0.05f).Within(float.Epsilon));
            Assert.That(t.tapCount, Is.EqualTo(1));
        }

        {
            // Perform touch for longer than tap time
            var beginTime = 0.6;
            var holdTime = InputSystem.settings.multiTapDelayTime + 0.1f;

            BeginTouch(1, new Vector2(1.5f, 2.5f), time: beginTime);
            EndTouch(1, new Vector2(1.5f, 2.5f), time: beginTime + holdTime, queueEventOnly: true);
            yield return null;

            Assert.That(Input.touchCount, Is.EqualTo(1));
            var t = Input.GetTouch(0);
            Assert.That(t.deltaTime, Is.EqualTo(holdTime).Within(float.Epsilon));
            Assert.That(t.tapCount, Is.EqualTo(0));
        }
    }

    [UnityTest]
    [Category("API")]
    public IEnumerator API_CanReadTouchPressureAndRadiusThroughTouchesAPI()
    {
        var touch = InputSystem.AddDevice<Touchscreen>();
        {
            InputSystem.QueueStateEvent(touch,
                new TouchState
                {
                    touchId = 1,
                    phase = UnityEngine.InputSystem.TouchPhase.Began,
                    position = new Vector2(7.5f, 8.5f),
                    radius = new Vector2(1.5f, 2.5f),
                    pressure = 1.23f,
                    isIndirectTouch = true
                });
            yield return null;

            Assert.That(Input.touchCount, Is.EqualTo(1));
            var t = Input.GetTouch(0);
            Assert.That(t.radius, Is.EqualTo(2.0f).Within(float.Epsilon));
            Assert.That(t.radiusVariance, Is.EqualTo(0.5f).Within(float.Epsilon));
            Assert.That(t.pressure, Is.EqualTo(1.23f).Within(float.Epsilon));
            Assert.That(t.maximumPossiblePressure, Is.EqualTo(1.0f).Within(float.Epsilon));

            //Assert.That(t.type, Is.EqualTo(TouchType.Indirect)); // TODO: This actually Direct. Need to also test for Stylus case
            Assert.That(t.altitudeAngle, Is.EqualTo(0));           // TODO: No way to set this
            Assert.That(t.azimuthAngle, Is.EqualTo(0));            // TODO: No way to set this
        }
    }
}
