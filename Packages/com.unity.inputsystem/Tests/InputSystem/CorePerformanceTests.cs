#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Reflection;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
using NUnit.Framework;
using UnityEngine;

////TODO: just fold performance tests into "normal" tests; performance should be a functional aspect like everything else

public class CorePerformanceTests
{
    [SetUp]
    public void Setup()
    {
        InputSystem.SaveAndReset();
    }

    [TearDown]
    public void TearDown()
    {
        InputSystem.Restore();
    }

    ////TODO: same test but with several actions listening on each gamepad
    // Performing a full state update on 10 devices should take less than 0.01 ms.
    [Test]
    [Category("Performance")]
    [Ignore("TODO")]
    public void TODO_CanUpdate10GamepadsInLessThanPointZeroOneMilliseconds()
    {
        const int kNumGamepads = 10;

        var gamepads = new Gamepad[kNumGamepads];
        for (var i = 0; i < kNumGamepads; ++i)
            gamepads[i] = InputSystem.AddDevice<Gamepad>();

        var startTime = Time.realtimeSinceStartup;

        // Generate a full state update for each gamepad.
        for (var i = 0; i < kNumGamepads; ++i)
            InputSystem.QueueStateEvent(gamepads[i], new GamepadState());

        // Now run the update.
        InputSystem.Update();

        var endTime = Time.realtimeSinceStartup;
        var totalTime = endTime - startTime;

        Assert.That(totalTime, Is.LessThan(0.01 / 1000.0));
        Debug.Log(string.Format("{0}: {1}ms", MethodBase.GetCurrentMethod().Name, totalTime * 1000));
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
