#if UNITY_EDITOR || DEVELOPMENT_BUILD
using NUnit.Framework;
using UnityEngine.Experimental.Input;
using UnityEngine.Profiling;

////TODO: write a test that generates a pseudo-random event sequence and runs it through a pseudo-random
////      update pattern and verifies the state and action outcome is as expected

internal class CoreStressTests
{
    [Test]
    [Category("Stress")]
    [Ignore("TODO")]
    public void TODO_Stress_512GamepadsAnd10000Events()
    {
        const int kGamepadCount = 512;
        const int kEventCount = 10000;

        Profiler.BeginSample("CreateDevices");
        var gamepads = new Gamepad[kGamepadCount];
        for (var i = 0; i < kGamepadCount; ++i)
            gamepads[i] = InputSystem.AddDevice<Gamepad>();
        Profiler.EndSample();

        Profiler.BeginSample("QueueEvents");
        var gamepadIndex = 0;
        for (var i = 0; i < kEventCount; ++i)
        {
            //InputSystem.QueueStateEvent(gamepads[gamepadIndex], );

            ++gamepadIndex;
            gamepadIndex = gamepadIndex % kGamepadCount;
        }
        Profiler.EndSample();

        Profiler.BeginSample("ProcessEvents");
        Profiler.EndSample();

        //verify

        Assert.Fail();
    }

    [Test]
    [Category("Stress")]
    [Ignore("TODO")]
    public void TODO_Stress_100ActionMapsInAssetWith1000Actions()
    {
        Profiler.BeginSample("CreateDevices");
        Profiler.EndSample();

        Profiler.BeginSample("CreateActions");
        Profiler.EndSample();

        Profiler.BeginSample("EnableActions");
        Profiler.EndSample();

        Profiler.BeginSample("ProcessEvents");
        Profiler.EndSample();

        Assert.Fail();
    }
}
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
