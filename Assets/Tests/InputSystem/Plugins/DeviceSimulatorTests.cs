#if UNITY_EDITOR && UNITY_2021_1_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.DeviceSimulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.TestTools;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class DeviceSimulatorTests
{
    [UnityTest]
    [Category("Device Simulator")]
    public IEnumerator InputEventsArePropagated()
    {
        EnhancedTouchSupport.Enable();
        var plugin = new InputSystemPlugin();
        plugin.OnCreate();
        yield return null;

        plugin.OnTouchEvent(CreateTouch(0, new Vector2(5, 5), UnityEditor.DeviceSimulation.TouchPhase.Began));
        yield return null;
        Assert.Greater(Touch.activeTouches.Count, 0);
        Assert.AreEqual(Touch.activeTouches[0].screenPosition, new Vector2(5, 5));
        Assert.AreEqual(Touch.activeTouches[0].phase, TouchPhase.Began);

        yield return null;
        Assert.AreEqual(Touch.activeTouches[0].screenPosition, new Vector2(5, 5));
        Assert.AreEqual(Touch.activeTouches[0].phase, TouchPhase.Stationary);

        plugin.OnTouchEvent(CreateTouch(0, new Vector2(10, 10), UnityEditor.DeviceSimulation.TouchPhase.Moved));
        yield return null;
        Assert.AreEqual(Touch.activeTouches[0].screenPosition, new Vector2(10, 10));
        Assert.AreEqual(Touch.activeTouches[0].phase, TouchPhase.Moved);

        plugin.OnTouchEvent(CreateTouch(0, new Vector2(5, 5), UnityEditor.DeviceSimulation.TouchPhase.Ended));
        yield return null;
        Assert.AreEqual(Touch.activeTouches[0].screenPosition, new Vector2(5, 5));
        Assert.AreEqual(Touch.activeTouches[0].phase, TouchPhase.Ended);

        yield return null;
        Assert.AreEqual(Touch.activeTouches.Count, 0);

        plugin.OnDestroy();
    }

    [Test]
    [Category("Device Simulator")]
    public void TouchscreenAddedAndRemoved()
    {
        var plugin = new InputSystemPlugin();
        plugin.OnCreate();
        var touchscreen = plugin.SimulatorTouchscreen;
        Assert.IsTrue(touchscreen.added);

        plugin.OnDestroy();
        Assert.IsFalse(touchscreen.added);
    }

    private TouchEvent CreateTouch(int touchId, Vector2 position, UnityEditor.DeviceSimulation.TouchPhase phase)
    {
        var touch = new TouchEvent();
        var type = typeof(TouchEvent);
        object touchObject = touch;
        var touchIdProperty = type.GetField("<touchId>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        var positionProperty = type.GetField("<position>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        var phaseProperty = type.GetField("<phase>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        touchIdProperty.SetValue(touchObject, touchId);
        positionProperty.SetValue(touchObject, position);
        phaseProperty.SetValue(touchObject, phase);
        touch = (TouchEvent)touchObject;
        return touch;
    }
}

#endif
