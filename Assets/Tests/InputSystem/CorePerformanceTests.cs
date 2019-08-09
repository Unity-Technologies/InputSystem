#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Reflection;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using NUnit.Framework;
using UnityEngine;

// These tests are all explicit (i.e. manually run) as we don't have a performance testing
// rig in place.

// IMPORTANT: When running in editor, make sure to turn off debugging (disable "Editor Attaching" in
//            editor preferences and restart editor) when running tests here. If debugging is enabled,
//            the code will run A LOT slower.

internal class CorePerformanceTests : InputTestFixture
{
    ////TODO: same test but with several actions listening on each gamepad
    // Performing a full state update on 10 devices should take less than 0.01 ms.
    // STATUS: On 2014 MBP with 2.8GHz i7, passes in less than half that time.
    [Test]
    [Category("Performance")]
    [Explicit]
    public void CanUpdate10GamepadsInLessThanPointZeroOneMilliseconds()
    {
        const int kNumGamepads = 10;

        var gamepads = new Gamepad[kNumGamepads];
        for (var i = 0; i < kNumGamepads; ++i)
            gamepads[i] = InputSystem.AddDevice<Gamepad>();

        // Perform initial update to get any first-update-only stuff out of the way.
        InputSystem.Update();

        var bestTime = float.MaxValue;
        for (var n = 0; n < 20; ++n)
        {
            var startTime = Time.realtimeSinceStartup;

            for (var i = 0; i < kNumGamepads; ++i)
                InputSystem.QueueStateEvent(gamepads[i], new GamepadState());

            InputSystem.Update();

            var endTime = Time.realtimeSinceStartup;
            var totalTime = endTime - startTime;

            bestTime = Mathf.Min(totalTime, bestTime);
        }


        Assert.That(bestTime, Is.LessThan(0.01 / 1000.0));
        Debug.Log($"{MethodBase.GetCurrentMethod().Name}: {bestTime * 1000}ms");
    }

    #if UNITY_EDITOR
    [Test]
    [Category("Performance")]
    [Ignore("TODO")]
    public void TODO_CanSaveAndRestoreSystemInLessThan10Milliseconds() // Currently it's >200ms!
    {
        Assert.Fail();
    }

    #endif
}
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
