---
uid: input-system-migration
---
# Migrating from the old Input Manager

This page is provided to help you match input-related API from Unity's old, built-in input (known as the [Input Manager](https://docs.unity3d.com/Manual/class-InputManager.html)) to the corresponding API in the new Input System package.

## Read the introductory documentation first

If you're new to the Input System package and have landed on this page looking for documentation, it's best to read the [QuickStart Guide](QuickStartGuide.md), and the [Concepts](Concepts.md) and [Workflows](Workflows.md) pages from the introduction section of the documentation, so that you can make sure you're choosing the best workflow for your project's input requirements.

This is because there are a number of different ways to read input using the Input System, and many of the directly corresponding API methods on this page might give you the quickest but least flexible solution, and may not be suitable for a project with more complex requirements.

### Which system is enabled?

When installing the new Input System, Unity prompts you to enable the new input system and disable the old one. You can change this setting at any time later, by going to **Edit > Project Settings > Player > Other Settings > Active Input Handling**, [as described here](./Installation.md#enabling-the-new-input-backends).

There are scripting symbols defined which allow you to use conditional compilation based on which system is enabled, as shown in the example below.

```CSharp
#if ENABLE_INPUT_SYSTEM
    // New input system backends are enabled.
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    // Old input backends are enabled.
#endif
```

> **Note:** It is possible to have both systems enabled at the same time, in which case both sets of code in the example above above will be active.

## List of corresponding API in the old Input Manager new Input System package

All of the new APIs listed below are in the `UnityEngine.InputSystem` namespace. The namespace is omitted here for brevity. `UnityEngine.InputSystem` is referenced in full for easy disambiguation.


|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.acceleration`](https://docs.unity3d.com/ScriptReference/Input-acceleration.html)|[`Accelerometer.current.acceleration.ReadValue()`](../api/UnityEngine.InputSystem.Accelerometer.html).
[`Input.accelerationEventCount`](https://docs.unity3d.com/ScriptReference/Input-accelerationEventCount.html)<br/><a name="accelerationEvents"></a>[`Input.accelerationEvents`](https://docs.unity3d.com/ScriptReference/Input-accelerationEvents.html)|Acceleration events aren't made available separately from other input events. The following code traces all input events on the [`Accelerometer.current`](../api/UnityEngine.InputSystem.Accelerometer.html) device.
```CSharp
    private InputEventTrace trace;

    void StartTrace()
    {
        InputSystem.EnableDevice(Accelerometer.current);

        trace = new InputEventTrace(Accelerometer.current);
        trace.Enable();
    }

    void Update()
    {
        foreach (var e in trace)
        {
            //...
        }
        trace.Clear();
    }
```
|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.anyKey`](https://docs.unity3d.com/ScriptReference/Input-anyKey.html)|[`InputSystem.onAnyButtonPress`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onAnyButtonPress)<br/>Example:<br/>`InputSystem.onAnyButtonPress.CallOnce(ctrl => Debug.Log($"Button {ctrl} pressed!"));`
|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.anyKeyDown`](https://docs.unity3d.com/ScriptReference/Input-anyKeyDown.html)|[`Keyboard.current.anyKey.wasUpdatedThisFrame`](../api/UnityEngine.InputSystem.Keyboard.html)
[`Input.backButtonLeavesApp`](https://docs.unity3d.com/ScriptReference/Input-backButtonLeavesApp.html)|No corresponding API yet.
[`Input.compass`](https://docs.unity3d.com/ScriptReference/Input-compass.html)|No corresponding API yet.
[`Input.compensateSensors`](https://docs.unity3d.com/ScriptReference/Input-compensateSensors.html)|[`InputSystem.settings.compensateForScreenOrientation`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_compensateForScreenOrientation).
[`Input.compositionCursorPos`](https://docs.unity3d.com/ScriptReference/Input-compositionCursorPos.html)|[`Keyboard.current.SetIMECursorPosition(myPosition)`](../api/UnityEngine.InputSystem.Keyboard.html#UnityEngine_InputSystem_Keyboard_SetIMECursorPosition_UnityEngine_Vector2_)
[`Input.compositionString`](https://docs.unity3d.com/ScriptReference/Input-compositionString.html)|Subscribe to the [`Keyboard.onIMECompositionChange`](../api/UnityEngine.InputSystem.Keyboard.html#UnityEngine_InputSystem_Keyboard_onIMECompositionChange) event:

```CSharp
    var compositionString = "";
    Keyboard.current.onIMECompositionChange += composition =>
    {
        compositionString = composition.ToString();
    };
```
|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.deviceOrientation`](https://docs.unity3d.com/ScriptReference/Input-deviceOrientation.html)|No corresponding API yet.
<a name="gyro"></a>[`Input.gyro`](https://docs.unity3d.com/ScriptReference/Input-gyro.html)|The `UnityEngine.Gyroscope` class is replaced by multiple separate sensor Devices in the new Input System:<br/>[`Gyroscope`](../api/UnityEngine.InputSystem.Gyroscope.html) to measure angular velocity.<br/>[`GravitySensor`](../api/UnityEngine.InputSystem.GravitySensor.html) to measure the direction of gravity.<br/>[`AttitudeSensor`](../api/UnityEngine.InputSystem.AttitudeSensor.html) to measure the orientation of the device.<br/>[`Accelerometer`](../api/UnityEngine.InputSystem.Accelerometer.html) to measure the total acceleration applied to the device.<br/>[`LinearAccelerationSensor`](../api/UnityEngine.InputSystem.LinearAccelerationSensor.html) to measure acceleration applied to the device, compensating for gravity.
[`Input.gyro.attitude`](https://docs.unity3d.com/ScriptReference/Gyroscope-attitude.html)|[`AttitudeSensor.current.orientation.ReadValue()`](../api/UnityEngine.InputSystem.AttitudeSensor.html).
[`Input.gyro.enabled`](https://docs.unity3d.com/ScriptReference/Gyroscope-enabled.html)|Get: `Gyroscope.current.enabled`<br/>Set:<br/>`InputSystem.EnableDevice(Gyroscope.current);`<br/>`InputSystem.DisableDevice(Gyroscope.current);`<br/><br/>__Note__: The new Input System replaces `UnityEngine.Gyroscope` with multiple separate sensor devices. Substitute [`Gyroscope`](../api/UnityEngine.InputSystem.Gyroscope.html) with other sensors in the sample as needed. See the notes for `Input.gyro` above for details.
[`Input.gyro.gravity`](https://docs.unity3d.com/ScriptReference/Gyroscope-gravity.html)|[`GravitySensor.current.gravity.ReadValue()`](../api/UnityEngine.InputSystem.GravitySensor.html)
[`Input.gyro.rotationRate`](https://docs.unity3d.com/ScriptReference/Gyroscope-rotationRate.html)|[`Gyroscope.current.angularVelocity.ReadValue()`](../api/UnityEngine.InputSystem.Gyroscope.html).
[`Input.gyro.rotationRateUnbiased`](https://docs.unity3d.com/ScriptReference/Gyroscope-rotationRateUnbiased.html)|No corresponding API yet.
[`Input.gyro.updateInterval`](https://docs.unity3d.com/ScriptReference/Gyroscope-updateInterval.html)|[`Sensor.samplingFrequency`](../api/UnityEngine.InputSystem.Sensor.html#UnityEngine_InputSystem_Sensor_samplingFrequency)<br/>Example:<br/>`Gyroscope.current.samplingFrequency = 1.0f / updateInterval;`<br/><br/>__Notes__:<br/>[`samplingFrequency`](../api/UnityEngine.InputSystem.Sensor.html#UnityEngine_InputSystem_Sensor_samplingFrequency) is in Hz, not in seconds as [`updateInterval`](https://docs.unity3d.com/ScriptReference/Gyroscope-updateInterval.html), so you need to divide 1 by the value.<br/><br/>The new Input System replaces `UnityEngine.Gyroscope` with multiple separate sensor devices. Substitute [`Gyroscope`](../api/UnityEngine.InputSystem.Gyroscope.html) with other sensors in the sample as needed. See the notes for `Input.gyro` above for details.
[`Input.gyro.userAcceleration`](https://docs.unity3d.com/ScriptReference/Gyroscope-userAcceleration.html)|[`LinearAccelerationSensor.current.acceleration.acceleration.ReadValue()`](../api/UnityEngine.InputSystem.LinearAccelerationSensor.html)
[`Input.imeCompositionMode`](https://docs.unity3d.com/ScriptReference/Input-imeCompositionMode.html)|No corresponding API yet.
[`Input.imeIsSelected`](https://docs.unity3d.com/ScriptReference/Input-imeIsSelected.html)|Get: `Keyboard.current.imeSelected`<br/>Set: `Keyboard.current.SetIMEEnabled(true);`
[`Input.inputString`](https://docs.unity3d.com/ScriptReference/Input-inputString.html)|Subscribe to the [`Keyboard.onTextInput`](../api/UnityEngine.InputSystem.Keyboard.html#UnityEngine_InputSystem_Keyboard_onTextInput) event:<br/>`Keyboard.current.onTextInput += character => /* ... */;`
[`Input.location`](https://docs.unity3d.com/ScriptReference/Input-location.html)|No corresponding API yet.
[`Input.mousePosition`](https://docs.unity3d.com/ScriptReference/Input-mousePosition.html)|[`Mouse.current.position.ReadValue()`](../api/UnityEngine.InputSystem.Mouse.html)<br/>__Note__: Mouse simulation from touch isn't implemented yet.
[`Input.mousePresent`](https://docs.unity3d.com/ScriptReference/Input-mousePresent.html)|[`Mouse.current != null`](../api/UnityEngine.InputSystem.Mouse.html#UnityEngine_InputSystem_Mouse_current).
[`Input.multiTouchEnabled`](https://docs.unity3d.com/ScriptReference/Input-multiTouchEnabled.html)|No corresponding API yet.
[`Input.simulateMouseWithTouches`](https://docs.unity3d.com/ScriptReference/Input-multiTouchEnabled.html)|No corresponding API yet.
[`Input.stylusTouchSupported`](https://docs.unity3d.com/ScriptReference/Input-stylusTouchSupported.html)|No corresponding API yet.
[`Input.touchCount`](https://docs.unity3d.com/ScriptReference/Input-touchCount.html)|[`InputSystem.EnhancedTouch.Touch.activeTouches.Count`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeTouches)<br/>__Note__: Enable enhanced touch support first by calling [`InputSystem.EnhancedTouchSupport.Enable()`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable)
[`Input.touches`](https://docs.unity3d.com/ScriptReference/Input-touches.html)|[`InputSystem.EnhancedTouch.Touch.activeTouches`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeTouches)<br/>__Note__: Enable enhanced touch support first by calling [`InputSystem.EnhancedTouch.Enable()`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable)
[`Input.touchPressureSupported`](https://docs.unity3d.com/ScriptReference/Input-touchPressureSupported.html)|No corresponding API yet.
[`Input.touchSupported`](https://docs.unity3d.com/ScriptReference/Input-touchSupported.html)|[`Touchscreen.current != null`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_current)
[`Input.GetAccelerationEvent`](https://docs.unity3d.com/ScriptReference/Input.GetAccelerationEvent.html)|See notes for `Input.accelerationEvents` above.
<a name="getAxis"></a>[`Input.GetAxis`](https://docs.unity3d.com/ScriptReference/Input.GetAxis.html)|Set up an action as a 1D or 2D axis in the Actions Editor, then use [`InputAction.ReadValue`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_ReadValue__1) to read the value or 2D vector from the axis. There are some default built-in axis actions. See the [Quickstart Guide](QuickStartGuide.md) to get started quickly.
[`Input.GetAxisRaw`](https://docs.unity3d.com/ScriptReference/Input.GetAxisRaw.html)|Not directly applicable. You can use [`InputControl<>.ReadUnprocessedValue()`](../api/UnityEngine.InputSystem.InputControl-1.html#UnityEngine_InputSystem_InputControl_1_ReadUnprocessedValue) to read unprocessed values from any control.
[`Input.GetButton`](https://docs.unity3d.com/ScriptReference/Input.GetButton.html)|[`InputAction.IsPressed`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_IsPressed_)
|[`Input.GetButtonDown`](https://docs.unity3d.com/ScriptReference/Input.GetButtonDown.html)|[`InputAction.WasPressedThisFrame`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasPressedThisFrame_)
[`Input.GetButtonUp`](https://docs.unity3d.com/ScriptReference/Input.GetButtonUp.html)|[`InputAction.WasReleasedThisFrame`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_WasReleasedThisFrame_)
[`Input.GetJoystickNames`](https://docs.unity3d.com/ScriptReference/Input.GetJoystickNames.html)|There is no API that corresponds to this exactly. Here are various ways to discover connected Devices:

```
// Query a list of all connected Devices (does not allocate; read-only access).
InputSystem.devices

// Get notified when a Device is added or removed.
InputSystem.onDeviceChange +=
    (device, change) =>
    {
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed)
        {
            Debug.Log($"Device '{device}' was {change}");
        }
    }

// Find all gamepads and joysticks.
var devices = InputSystem.devices;
for (var i = 0; i < devices.Count; ++i)
{
    var device = devices[i];
    if (device is Joystick || device is Gamepad)
    {
        Debug.Log("Found " + device);
    }
}
```
|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.GetKey`](https://docs.unity3d.com/ScriptReference/Input.GetKey.html)|[`ButtonControl.isPressed`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html#UnityEngine_InputSystem_Controls_ButtonControl_isPressed) on the corresponding key:

```
// Using KeyControl property directly.
Keyboard.current.spaceKey.isPressed
Keyboard.current.aKey.isPressed // etc.

// Using Key enum.
Keyboard.current[Key.Space].isPressed

// Using key name.
((KeyControl)Keyboard.current["space"]).isPressed
```

>__Note__: The Input System identifies keys by physical layout, not according to the current language mapping of the keyboard. To query the name of the key according to the language mapping, use [`KeyControl.displayName`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_displayName).

|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.GetKeyDown`](https://docs.unity3d.com/ScriptReference/Input.GetKeyDown.html)|Use [`ButtonControl.wasPressedThisFrame`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html#UnityEngine_InputSystem_Controls_ButtonControl_wasPressedThisFrame) on the corresponding key:

```
// Using KeyControl property directly.
Keyboard.current.spaceKey.wasPressedThisFrame
Keyboard.current.aKey.wasPressedThisFrame // etc.

// Using Key enum.
Keyboard.current[Key.Space].wasPressedThisFrame

// Using key name.
((KeyControl)Keyboard.current["space"]).wasPressedThisFrame
```

>__Note__: The Input System identifies keys by physical layout, not according to the current language mapping of the keyboard. To query the name of the key according to the language mapping, use [`KeyControl.displayName`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_displayName).

|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.GetKeyUp`](https://docs.unity3d.com/ScriptReference/Input.GetKeyUp.html)|Use [`ButtonControl.wasReleasedThisFrame`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html#UnityEngine_InputSystem_Controls_ButtonControl_wasReleasedThisFrame) on the corresponding key:

```
// Using KeyControl property directly.
Keyboard.current.spaceKey.wasReleasedThisFrame
Keyboard.current.aKey.wasReleasedThisFrame // etc.

// Using Key enum.
Keyboard.current[Key.Space].wasReleasedThisFrame

// Using key name.
((KeyControl)Keyboard.current["space"]).wasReleasedThisFrame
```

>__Note__: The Input System identifies keys by physical layout, not according to the current language mapping of the keyboard. To query the name of the key according to the language mapping, use [`KeyControl.displayName`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_displayName).

|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.GetMouseButton`](https://docs.unity3d.com/ScriptReference/Input.GetMouseButton.html)|Use [`ButtonControl.isPressed`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html#UnityEngine_InputSystem_Controls_ButtonControl_isPressed) on the corresponding mouse button:

```
Mouse.current.leftButton.isPressed
Mouse.current.rightButton.isPressed
Mouse.current.middleButton.isPressed

// You can also go through all buttons on the mouse (does not allocate).
var controls = Mouse.current.allControls;
for (var i = 0; i < controls.Count; ++i)
{
    var button = controls[i] as ButtonControl;
    if (button != null && button.isPressed)
        /* ... */;
}

// Or look up controls by name.
((ButtonControl)Mouse.current["leftButton"]).isPressed
```
|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.GetMouseButtonDown`](https://docs.unity3d.com/ScriptReference/Input.GetMouseButtonDown.html)|Use [`ButtonControl.wasPressedThisFrame`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html#UnityEngine_InputSystem_Controls_ButtonControl_wasPressedThisFrame) on the corresponding mouse button:

```
Mouse.current.leftButton.wasPressedThisFrame
Mouse.current.rightButton.wasPressedThisFrame
Mouse.current.middleButton.wasPressedThisFrame
```
|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.GetMouseButtonUp`](https://docs.unity3d.com/ScriptReference/Input.GetMouseButtonUp.html)|Use [`ButtonControl.wasReleasedThisFrame`](../api/UnityEngine.InputSystem.Controls.ButtonControl.html#UnityEngine_InputSystem_Controls_ButtonControl_wasReleasedThisFrame) on the corresponding mouse button:

```
Mouse.current.leftButton.wasReleasedThisFrame
Mouse.current.rightButton.wasReleasedThisFrame
Mouse.current.middleButton.wasReleasedThisFrame
```

|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.GetTouch`](https://docs.unity3d.com/ScriptReference/Input.GetTouch.html)|Use [`InputSystem.EnhancedTouch.Touch.activeTouches[i]`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeTouches)<br/>__Note__: Enable enhanced touch support first by calling [`InputSystem.EnhancedTouch.Enable()`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable).
[`Input.IsJoystickPreconfigured`](https://docs.unity3d.com/ScriptReference/Input.IsJoystickPreconfigured.html)|Not needed. Devices which derive from [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) always correctly implement the mapping of axes and buttons to the corresponding [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html) members of the [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) class. [`Input.ResetInputAxes`](https://docs.unity3d.com/ScriptReference/Input.ResetInputAxes.html)|Not directly applicable.
[`UnityEngine.TouchScreenKeyboard`](https://docs.unity3d.com/ScriptReference/TouchScreenKeyboard.html)|No corresponding API yet. Use `TouchScreenKeyboard` for now.
