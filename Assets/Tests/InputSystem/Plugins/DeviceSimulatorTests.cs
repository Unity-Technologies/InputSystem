#if UNITY_EDITOR && UNITY_2021_1_OR_NEWER

using System.Collections;
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

public class DeviceSimulatorTests : InputTestFixture
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
        var activeTouches = Touch.activeTouches;
        Assert.Greater(activeTouches.Count, 0);
        Assert.AreEqual(new Vector2(5, 5), Touch.activeTouches[0].screenPosition);
        Assert.AreEqual(TouchPhase.Began, Touch.activeTouches[0].phase);

        yield return null;
        Assert.AreEqual(new Vector2(5, 5), Touch.activeTouches[0].screenPosition);
        Assert.AreEqual(TouchPhase.Stationary, Touch.activeTouches[0].phase);

        plugin.OnTouchEvent(CreateTouch(0, new Vector2(10, 10), UnityEditor.DeviceSimulation.TouchPhase.Moved));
        yield return null;
        Assert.AreEqual(new Vector2(10, 10), Touch.activeTouches[0].screenPosition);
        Assert.AreEqual(TouchPhase.Moved, Touch.activeTouches[0].phase);

        plugin.OnTouchEvent(CreateTouch(0, new Vector2(5, 5), UnityEditor.DeviceSimulation.TouchPhase.Ended));
        yield return null;
        Assert.AreEqual(new Vector2(5, 5), Touch.activeTouches[0].screenPosition);
        Assert.AreEqual(TouchPhase.Ended, Touch.activeTouches[0].phase);

        yield return null;
        Assert.AreEqual(Touch.activeTouches.Count, 0);

        plugin.OnDestroy();
        EnhancedTouchSupport.Disable();
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

        var touchIdAutoBackingField = type.GetField($"<{nameof(TouchEvent.touchId)}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        var positionAutoBackingField = type.GetField($"<{nameof(TouchEvent.position)}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        var phaseAutoBackingField = type.GetField($"<{nameof(TouchEvent.phase)}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(touchIdAutoBackingField);
        Assert.NotNull(positionAutoBackingField);
        Assert.NotNull(phaseAutoBackingField);

        touchIdAutoBackingField.SetValue(touchObject, touchId);
        positionAutoBackingField.SetValue(touchObject, position);
        phaseAutoBackingField.SetValue(touchObject, phase);

        touch = (TouchEvent)touchObject;
        return touch;
    }
}

#endif
