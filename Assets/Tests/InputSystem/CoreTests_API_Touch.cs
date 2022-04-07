using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

partial class CoreTests
{
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
        Assert.That(Input.touchSupported, Is.False);
        var touch = InputSystem.AddDevice<Touchscreen>();
        Assert.That(Input.touchSupported, Is.True);

        Assert.That(Input.touchCount, Is.EqualTo(0));
        Assert.That(Input.touches.Length, Is.EqualTo(Input.touchCount));

        // Perform input
        BeginTouch(1, new Vector2(1, 2), time: 0.3);
        yield return null;
        Assert.That(Input.touchCount, Is.EqualTo(1));
        Assert.That(Input.touches.Length, Is.EqualTo(Input.touchCount));

        var first = Input.GetTouch(0);
        Assert.IsTrue(TouchesAreEqual(Input.touches[0], first));

        Assert.That(first.fingerId, Is.EqualTo(1));
        Assert.That(first.position, Is.EqualTo(new Vector2(1.0f, 2.0f)));
        Assert.That(first.rawPosition, Is.EqualTo(new Vector2(1.0f, 2.0f)));
        Assert.That(first.deltaPosition, Is.EqualTo(new Vector2(0.0f, 0.0f)));
        Assert.That(first.deltaTime, Is.EqualTo(0));
        Assert.That(first.tapCount, Is.EqualTo(0));
        Assert.That(first.phase, Is.EqualTo(UnityEngine.TouchPhase.Began));
        Assert.That(first.pressure, Is.EqualTo(1.0f));
        Assert.That(first.maximumPossiblePressure, Is.EqualTo(1.0f));
        Assert.That(first.type, Is.EqualTo(TouchType.Direct));
        Assert.That(first.altitudeAngle, Is.EqualTo(0));
        Assert.That(first.azimuthAngle, Is.EqualTo(0));
        Assert.That(first.radius, Is.EqualTo(0));
        Assert.That(first.radiusVariance, Is.EqualTo(0));

        // Continue motion
        MoveTouch(1, new Vector2(15, 25), time: 0.4);
        yield return null;
        Assert.That(Input.touchCount, Is.EqualTo(1));
        Assert.That(Input.touches.Length, Is.EqualTo(Input.touchCount));

        var middle = Input.GetTouch(0);
        Assert.IsTrue(TouchesAreEqual(Input.touches[0], middle));
        Assert.IsFalse(TouchesAreEqual(first, middle));

        Assert.That(middle.fingerId, Is.EqualTo(1));
        Assert.That(middle.position, Is.EqualTo(new Vector2(15.0f, 25.0f)));
        Assert.That(middle.rawPosition, Is.EqualTo(new Vector2(1.0f, 2.0f)));
        Assert.That(middle.deltaPosition, Is.EqualTo(new Vector2(0.0f, 0.0f)));
        Assert.That(middle.deltaTime, Is.EqualTo(0));
        Assert.That(middle.tapCount, Is.EqualTo(0));
        Assert.That(middle.phase, Is.EqualTo(UnityEngine.TouchPhase.Moved));
        Assert.That(middle.pressure, Is.EqualTo(1.0f));
        Assert.That(middle.maximumPossiblePressure, Is.EqualTo(1.0f));
        Assert.That(middle.type, Is.EqualTo(TouchType.Direct));
        Assert.That(middle.altitudeAngle, Is.EqualTo(0));
        Assert.That(middle.azimuthAngle, Is.EqualTo(0));
        Assert.That(middle.radius, Is.EqualTo(0));
        Assert.That(middle.radiusVariance, Is.EqualTo(0));

        // Finish motion
        EndTouch(1, new Vector2(15, 25), time: 0.5);
        yield return null;
        Assert.That(Input.touchCount, Is.EqualTo(1));
        Assert.That(Input.touches.Length, Is.EqualTo(Input.touchCount));

        var last = Input.GetTouch(0);
        Assert.IsTrue(TouchesAreEqual(Input.touches[0], last));
        Assert.IsFalse(TouchesAreEqual(middle, last));

        Assert.That(last.fingerId, Is.EqualTo(1));
        Assert.That(last.position, Is.EqualTo(new Vector2(15.0f, 25.0f)));
        Assert.That(last.rawPosition, Is.EqualTo(new Vector2(1.0f, 2.0f)));
        Assert.That(last.deltaPosition, Is.EqualTo(new Vector2(0.0f, 0.0f))); // TODO
        Assert.That(last.deltaTime, Is.EqualTo(0));                           // TODO
        Assert.That(last.tapCount, Is.EqualTo(0));
        Assert.That(last.phase, Is.EqualTo(UnityEngine.TouchPhase.Ended));
        Assert.That(last.pressure, Is.EqualTo(1.0f));          // TODO
        Assert.That(last.maximumPossiblePressure, Is.EqualTo(1.0f));
        Assert.That(last.type, Is.EqualTo(TouchType.Direct));  // TODO: test for Indirect
        Assert.That(last.altitudeAngle, Is.EqualTo(0));        // TODO
        Assert.That(last.azimuthAngle, Is.EqualTo(0));         // TODO
        Assert.That(last.radius, Is.EqualTo(0));               // TODO
        Assert.That(last.radiusVariance, Is.EqualTo(0));       // TODO
    }

    [UnityTest]
    [Category("API")]
    [Ignore("TODO: get taps working")]
    public IEnumerator API_CanReadTouchTapEventsThroughTouchesAPI()
    {
        //var delay = InputSystem.settings.multiTapDelayTime;
        var touch = InputSystem.AddDevice<Touchscreen>();

        // Perform short tap
        BeginTouch(1, new Vector2(1.5f, 2.5f), time: 0.3);
        EndTouch(1, new Vector2(1.5f, 2.5f), time: 0.35);
        yield return null;

        Assert.That(Input.touchCount, Is.EqualTo(1));
        var first = Input.GetTouch(0);
        Assert.That(first.fingerId, Is.EqualTo(1));
        Assert.That(first.position, Is.EqualTo(new Vector2(1.5f, 2.5f)));
        Assert.That(first.rawPosition, Is.EqualTo(new Vector2(1.5f, 2.5f)));
        Assert.That(first.deltaPosition, Is.EqualTo(new Vector2(0.0f, 0.0f)));
        Assert.That(first.deltaTime, Is.EqualTo(0));
        Assert.That(first.tapCount, Is.EqualTo(1));  // TODO: This Fails
    }
}
