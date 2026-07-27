#if UNITY_EDITOR

using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.DeviceSimulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Layouts;
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

    [Test]
    [Category("Device Simulator")]
    public void ConflictingDevicesAreNotDisabledOnCreate()
    {
        var mouse = AddNativeMouse();
        Assert.That(mouse.native, Is.True);

        var plugin = new InputSystemPlugin();
        plugin.OnCreate();

        // Conflicting devices are only disabled once the Simulator gains focus, not on create.
        Assert.That(mouse.enabled, Is.True);

        plugin.OnDestroy();
    }

    [Test]
    [Category("Device Simulator")]
    public void ConflictingDeviceAddedWhileSimulatorFocused_IsDisabledThenReenabledOnDestroy()
    {
        var plugin = new InputSystemPlugin();
        plugin.OnCreate();

        // Simulate the Simulator window being focused (bypasses the panel-based OnUpdate).
        plugin.SetConflictingDevicesDisabled(true);

        var mouse = AddNativeMouse();

        Assert.That(mouse.native, Is.True);
        Assert.That(mouse.enabled, Is.False);   // disabled via the OnDeviceChange gate

        plugin.OnDestroy();
        Assert.That(mouse.enabled, Is.True);     // ReenableConflictingDevices restores it
    }

    [Test]
    [Category("Device Simulator")]
    public void ConflictingDevicesReenabledWhenSimulatorLosesFocus()
    {
        var mouse = AddNativeMouse();

        var plugin = new InputSystemPlugin();
        plugin.OnCreate();

        plugin.SetConflictingDevicesDisabled(true);    // Simulator gained focus
        Assert.That(mouse.enabled, Is.False);

        plugin.SetConflictingDevicesDisabled(false);   // Simulator lost focus
        Assert.That(mouse.enabled, Is.True);

        plugin.OnDestroy();
    }

    [Test]
    [Category("Device Simulator")]
    public void ConflictingDeviceAddedWhileSimulatorNotFocused_StaysEnabled()
    {
        var plugin = new InputSystemPlugin();
        plugin.OnCreate();
        // m_ConflictingDevicesDisabled defaults to false (Simulator not focused).

        var mouse = AddNativeMouse();

        Assert.That(mouse.enabled, Is.True);

        plugin.OnDestroy();
    }

    // Reports a native Mouse through the test runtime (device.native == true, which the plugin's
    // disable logic requires) and returns the resolved device rather than relying on Mouse.current.
    private Mouse AddNativeMouse()
    {
        var deviceId = runtime.ReportNewInputDevice(
            new InputDeviceDescription { deviceClass = "Mouse", interfaceName = "Test" });
        InputSystem.Update();
        return (Mouse)InputSystem.GetDeviceById(deviceId);
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
