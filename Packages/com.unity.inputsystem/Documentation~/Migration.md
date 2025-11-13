---
uid: input-system-migration
---
# Migrating from the old Input Manager

This page is provided to help you match input-related API from Unity's old, built-in input (known as the [Input Manager](https://docs.unity3d.com/Manual/class-InputManager.html)) to the corresponding API in the new Input System package.

## Read the introductory documentation first

If you're new to the Input System package and have landed on this page looking for documentation, it's best to read the [QuickStart Guide](xref:input-system-quickstart), and the [Concepts](xref:basic-concepts) and [Workflows](xref:input-system-workflows) pages from the introduction section of the documentation, so that you can make sure you're choosing the best workflow for your project's input requirements.

This is because there are a number of different ways to read input using the Input System, and some of the directly corresponding API methods on this page might give you the quickest - but least flexible - solution, and may not be suitable for a project with more complex requirements.

## Which system is enabled?

When installing the new Input System, Unity prompts you to enable the new input system and disable the old one. You can change this setting at any time later, by going to **Edit > Project Settings > Player > Other Settings > Active Input Handling**, [as described here](xref:input-system-installation#enable-the-new-input-backends).

There are scripting symbols defined which allow you to use conditional compilation based on which system is enabled, as shown in the example below.

```CSharp
#if ENABLE_INPUT_SYSTEM
    // New input system backends are enabled.
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    // Old input backends are enabled.
#endif
```

> [!NOTE]
> It is possible to have both systems enabled at the same time, in which case both sets of code in the example above above will be active.

## Comparison of API in the old Input Manager and the new Input System package

Below is a list comparing the API from the old Input Manager with the corresponding API for the new Input System package.
All of the new Input System package APIs listed below are in the `UnityEngine.InputSystem` namespace. The namespace is omitted here for brevity.

### Action-based input

Action-based input refers to reading pre-configured named axes, buttons, or other controls. ([Read more about Action-based input](xref:input-system-workflow-project-wide-actions))

- In the old Input Manager, these are defined in the **Axes** list, in the **Input Manager** section of the **Project Settings** window. _(Below, left)_
- In the new Input System, these are defined in the [Actions Editor](xref:input-system-configuring-input), which can be found in the **Input System Package** section of the **Project Settings** window, or by opening an [Action Asset](xref:input-system-action-assets). _(Below, right)_

![](Images/InputManagerVsInputActions.png)</br>_On the left, the old Input Manager Axes Configuration window, in Project settings. On the right, the new Input System's [Actions Editor](xref:input-system-configuring-input)._

> [!NOTE]
> In some cases for named axes and buttons, the new Input System requires slightly more code than the old Input Manager, but this results in better performance. This is because in the new Input System, the logic is separated into two parts: the first is to find and store a reference to the action (usually done once, for example in your `Start` method), and the second is to read the action (usually done every frame, for example in your `Update` method). In contrast, the old Input Manager used a string-based API to "find" and "read" the value at the same time, because it was not possible to store a reference to a button or axis. This results in worse performance, because the axis or button is looked up each time the value is read.

To find and store references to actions, which can be axes or buttons use [`FindAction`](xref:UnityEngine.InputSystem.InputActionAsset.FindAction(System.String,System.Boolean)). For example:
```
 // A 2D axis action named "Move"
InputAction moveAction = InputSystem.actions.FindAction("Move");

 // A button action named "Jump"
InputAction jumpAction = InputSystem.actions.FindAction("Jump");
```

Then, to read the action values, use the following:

|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.GetAxis`](https://docs.unity3d.com/ScriptReference/Input.GetAxis.html)<br/>In the old Input Manager System, all axes are 1D and return float values. For example, to read the horizontal and vertical axes:<br/>`float h = Input.GetAxis("Horizontal");`<br/>`float v = Input.GetAxis("Vertical");`<br/><br/> | Use [`ReadValue`](xref:UnityEngine.InputSystem.InputBindingComposite`1.ReadValue(UnityEngine.InputSystem.InputBindingCompositeContext@)) on the reference to the action to read the current value of the axis. In the new Input System, axes can be 1D, 2D or other value types. You must specify the correct value type that corresponds with how the action is set up. This example shows a 2D axis:<br/>`Vector2 moveVector = moveAction.ReadValue<Vector2>();`.<br/><br/>
[`Input.GetButton`](https://docs.unity3d.com/ScriptReference/Input.GetButton.html)<br/>Example:<br/>`bool jumpValue = Input.GetButton("Jump");`<br/><br/>|Use [`IsPressed`](xref:UnityEngine.InputSystem.InputAction.IsPressed*) on the reference to the action to read the button value.<br/>Example:<br/>`bool jumpValue = jumpAction.IsPressed();`.<br/><br/>
[`Input.GetButtonDown`](https://docs.unity3d.com/ScriptReference/Input.GetButtonDown.html)<br/>Example: `bool jump = Input.GetButtonDown("Jump");`<br/><br/>|Use [`WasPressedThisFrame`](xref:UnityEngine.InputSystem.InputAction.WasPressedThisFrame*) on the reference to the action to read if the button was pressed this frame.<br/>Example: `bool jumpValue = jumpAction.WasPressedThisFrame();`.<br/><br/>
[`Input.GetButtonUp`](https://docs.unity3d.com/ScriptReference/Input.GetButtonUp.html)<br/>Example: `bool jump = Input.GetButtonUp("Jump");`<br/><br/>|Use [`WasReleasedThisFrame`](xref:UnityEngine.InputSystem.InputAction.WasReleasedThisFrame*) on the reference to the action to read whether the button was released this frame.<br/>Example: `bool jumpValue = jumpAction.WasReleasedThisFrame();`.<br/><br/>
[`Input.GetAxisRaw`](https://docs.unity3d.com/ScriptReference/Input.GetAxisRaw.html)<br/>For example, to read the raw values of the horizontal and vertical axes:<br/>`float h = Input.GetAxisRaw("Horizontal");`<br/>`float v = Input.GetAxisRaw("Vertical");`<br/><br/>|No direct equivalent, but if there are [processors](UsingProcessors.md) associated with the action, you can use [`InputControl<>.ReadUnprocessedValue()`](xref:UnityEngine.InputSystem.InputControl`1.ReadUnprocessedValue) to read unprocessed values.<br/>Example: `Vector2 moveVector = moveAction.ReadUnprocessedValue();`<br/>Note: This returns the same value as ReadValue when there are no processors on the action.



### Directly reading Gamepad and Joystick controls

Directly reading hardware controls bypasses the new Input System's action-based workflow, which has some benefits and some drawbacks. ([Read more about directly reading devices](xref:input-system-workflow-direct))


|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.GetKey`](https://docs.unity3d.com/ScriptReference/Input.GetKey.html)<br/>Example: `Input.GetKey(KeyCode.JoystickButton0)`<br/><br/>|Use [`isPressed`](xref:UnityEngine.InputSystem.Controls.ButtonControl.isPressed) on the corresponding Gamepad button.<br/>Example: `InputSystem.GamePad.current.buttonNorth.isPressed`.<br/>
[`Input.GetKeyDown`](https://docs.unity3d.com/ScriptReference/Input.GetKeyDown.html)<br/>Example: `Input.GetKeyDown(KeyCode.JoystickButton0)`<br/><br/>|Use [`wasPressedThisFrame`](xref:UnityEngine.InputSystem.Controls.ButtonControl.wasPressedThisFrame) on the corresponding Gamepad button.<br/>Example: `InputSystem.GamePad.current.buttonNorth.WasPressedThisFrame`.<br/>
[`Input.GetKeyUp`](https://docs.unity3d.com/ScriptReference/Input.GetKeyUp.html)<br/>Example: `Input.GetKeyUp(KeyCode.JoystickButton0)`<br/><br/>|Use [`wasReleasedThisFrame`](xref:UnityEngine.InputSystem.Controls.ButtonControl.wasReleasedThisFrame) on the corresponding Gamepad button.<br/>Example: `InputSystem.GamePad.current.buttonNorth.wasReleasedThisFrame`.<br/>
[`Input.GetJoystickNames`](https://docs.unity3d.com/ScriptReference/Input.GetJoystickNames.html)|There is no API that corresponds to this exactly, but there are examples of [how to read all connected devices here](Gamepad.html#discover-all-connected-devices).
[`Input.IsJoystickPreconfigured`](https://docs.unity3d.com/ScriptReference/Input.IsJoystickPreconfigured.html)|Not needed. Devices which derive from [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) always correctly implement the mapping of axes and buttons to the corresponding [`InputControl`](xref:UnityEngine.InputSystem.InputControl) members of the [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad) class. [`Input.ResetInputAxes`](https://docs.unity3d.com/ScriptReference/Input.ResetInputAxes.html)



### Keyboard
|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.GetKey`](https://docs.unity3d.com/ScriptReference/Input.GetKey.html)<br/>Example: `Input.GetKey(KeyCode.Space)`<br/><br/>|Use [`isPressed`](xref:UnityEngine.InputSystem.Controls.ButtonControl.isPressed) on the corresponding key.<br/> Example: `InputSystem.Keyboard.current.spaceKey.isPressed`<br/><br/>
[`Input.GetKeyDown`](https://docs.unity3d.com/ScriptReference/Input.GetKeyDown.html)<br/>Example: `Input.GetKeyDown(KeyCode.Space)`<br/><br/>|Use [`wasPressedThisFrame`](xref:UnityEngine.InputSystem.Controls.ButtonControl.wasPressedThisFrame) on the corresponding key.<br/> Example: `InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame`<br/><br/>
[`Input.GetKeyUp`](https://docs.unity3d.com/ScriptReference/Input.GetKeyUp.html)<br/>Example: `Input.GetKeyUp(KeyCode.Space)`<br/><br/>|Use [`wasReleasedThisFrame`](xref:UnityEngine.InputSystem.Controls.ButtonControl.wasReleasedThisFrame) on the corresponding key.<br/> Example: `InputSystem.Keyboard.current.spaceKey.wasReleasedThisFrame`<br/><br/>
[`Input.anyKey`](https://docs.unity3d.com/ScriptReference/Input-anyKey.html)|Use [`onAnyButtonPress`](xref:UnityEngine.InputSystem.InputSystem.onAnyButtonPress).<br/>This also includes controller buttons as well as keyboard keys.
[`Input.anyKeyDown`](https://docs.unity3d.com/ScriptReference/Input-anyKeyDown.html)|Use [`Keyboard.current.anyKey.wasUpdatedThisFrame`](xref:UnityEngine.InputSystem.Keyboard.anyKey)
[`Input.compositionCursorPos`](https://docs.unity3d.com/ScriptReference/Input-compositionCursorPos.html)|Use [`Keyboard.current.SetIMECursorPosition(myPosition)`](xref:UnityEngine.InputSystem.Keyboard.SetIMECursorPosition(UnityEngine.Vector2))
[`Input.compositionString`](https://docs.unity3d.com/ScriptReference/Input-compositionString.html)|Subscribe to the [`Keyboard.onIMECompositionChange`](xref:UnityEngine.InputSystem.Keyboard.onIMECompositionChange).
[`Input.imeCompositionMode`](https://docs.unity3d.com/ScriptReference/Input-imeCompositionMode.html)|Use: [`Keyboard.current.SetIMEEnabled(true)`](xref:UnityEngine.InputSystem.Keyboard.SetIMEEnabled(System.Boolean))<br/>Also see: [Keyboard text input documentation](Keyboard.html#ime).
[`Input.imeIsSelected`](https://docs.unity3d.com/ScriptReference/Input-imeIsSelected.html)|Use: [`Keyboard.current.imeSelected`](xref:UnityEngine.InputSystem.Keyboard.imeSelected)
[`Input.inputString`](https://docs.unity3d.com/ScriptReference/Input-inputString.html)|Subscribe to the [`Keyboard.onTextInput`](xref:UnityEngine.InputSystem.Keyboard.onTextInput) event:<br/>`Keyboard.current.onTextInput += character => /* ... */;`

### Mouse

|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.GetMouseButton`](https://docs.unity3d.com/ScriptReference/Input.GetMouseButton.html)<br/>Example: `Input.GetMouseButton(0)`|Use [`isPressed`](xref:UnityEngine.InputSystem.Controls.ButtonControl.isPressed) on the corresponding mouse button.<br/>Example: `InputSystem.Mouse.current.leftButton.isPressed`
[`Input.GetMouseButtonDown`](https://docs.unity3d.com/ScriptReference/Input.GetMouseButtonDown.html)<br/>Example: `Input.GetMouseButtonDown(0)`|Use [`wasPressedThisFrame`](xref:UnityEngine.InputSystem.Controls.ButtonControl.wasPressedThisFrame) on the corresponding mouse button.<br/>Example: `InputSystem.Mouse.current.leftButton.wasPressedThisFrame`
[`Input.GetMouseButtonUp`](https://docs.unity3d.com/ScriptReference/Input.GetMouseButtonUp.html)<br/>Example: `Input.GetMouseButtonUp(0)`|Use [`wasReleasedThisFrame`](xref:UnityEngine.InputSystem.Controls.ButtonControl.wasReleasedThisFrame) on the corresponding mouse button.<br/>Example: `InputSystem.Mouse.current.leftButton.wasReleasedThisFrame`
[`Input.mousePosition`](https://docs.unity3d.com/ScriptReference/Input-mousePosition.html)|Use [`Mouse.current.position.ReadValue()`](xref:UnityEngine.InputSystem.Mouse)<br/>Example: `Vector2 position = Mouse.current.position.ReadValue();`<br/>__Note__: Mouse simulation from touch isn't implemented yet.
[`Input.mousePresent`](https://docs.unity3d.com/ScriptReference/Input-mousePresent.html)|No corresponding API yet.

### Touch and Pen

|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.GetTouch`](https://docs.unity3d.com/ScriptReference/Input.GetTouch.html)<br/>For example:<br/>`Touch touch = Input.GetTouch(0);`<br/>`Vector2 touchPos = touch.position;`|Use [`EnhancedTouch.Touch.activeTouches[i]`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)<br/>Example: `Vector2 touchPos = EnhancedTouch.Touch.activeTouches[0].position;`<br/>__Note__: Enable enhanced touch support first by calling [`EnhancedTouch.Enable()`](xref:UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable).
[`Input.multiTouchEnabled`](https://docs.unity3d.com/ScriptReference/Input-multiTouchEnabled.html)|No corresponding API yet.
[`Input.simulateMouseWithTouches`](https://docs.unity3d.com/ScriptReference/Input-simulateMouseWithTouches.html)|No corresponding API yet.
[`Input.stylusTouchSupported`](https://docs.unity3d.com/ScriptReference/Input-stylusTouchSupported.html)|No corresponding API yet.
[`Input.touchCount`](https://docs.unity3d.com/ScriptReference/Input-touchCount.html)|[`EnhancedTouch.Touch.activeTouches.Count`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)<br/>__Note__: Enable enhanced touch support first by calling [`EnhancedTouchSupport.Enable()`](xref:UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable)
[`Input.touches`](https://docs.unity3d.com/ScriptReference/Input-touches.html)|[`EnhancedTouch.Touch.activeTouches`](xref:UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)<br/>__Note__: Enable enhanced touch support first by calling [`EnhancedTouch.Enable()`](xref:UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable)
[`Input.touchPressureSupported`](https://docs.unity3d.com/ScriptReference/Input-touchPressureSupported.html)|No corresponding API yet.
[`Input.touchSupported`](https://docs.unity3d.com/ScriptReference/Input-touchSupported.html)|[`Touchscreen.current != null`](xref:UnityEngine.InputSystem.Touchscreen.current)
[`Input.backButtonLeavesApp`](https://docs.unity3d.com/ScriptReference/Input-backButtonLeavesApp.html)|No corresponding API yet.
[`GetPenEvent`](https://docs.unity3d.com/ScriptReference/Input.GetPenEvent.html)<br/>[`GetLastPenContactEvent`](https://docs.unity3d.com/ScriptReference/Input.GetLastPenContactEvent.html)<br/>[`ResetPenEvents`](https://docs.unity3d.com/ScriptReference/Input.ResetPenEvents.html)<br/>[`ClearLastPenContactEvent`](https://docs.unity3d.com/ScriptReference/Input.ClearLastPenContactEvent.html)|Use: [`Pen.current`](xref:UnityEngine.InputSystem.Pen.current)<br/>See the [Pen, tablet and stylus support](xref:input-system-pen) docs for more information.
<hr/>



> [!NOTE]
> [`UnityEngine.TouchScreenKeyboard`](https://docs.unity3d.com/ScriptReference/TouchScreenKeyboard.html) is not part of the old Input Manager API, so you can continue to use it when migrating to the new Input System package.

### Sensors
|Input Manager (Old)|Input System (New)|
|--|--|
[`Input.acceleration`](https://docs.unity3d.com/ScriptReference/Input-acceleration.html)|[`Accelerometer.current.acceleration.ReadValue()`](xref:UnityEngine.InputSystem.Accelerometer).
[`Input.accelerationEventCount`](https://docs.unity3d.com/ScriptReference/Input-accelerationEventCount.html)<br/>[`Input.accelerationEvents`](https://docs.unity3d.com/ScriptReference/Input-accelerationEvents.html)|Acceleration events aren't made available separately from other input events. See the [accelerometer code sample on the Sensors page](Sensors.html#accelerometer).
[`Input.compass`](https://docs.unity3d.com/ScriptReference/Input-compass.html)|No corresponding API yet.
[`Input.compensateSensors`](https://docs.unity3d.com/ScriptReference/Input-compensateSensors.html)|[`InputSettings.compensateForScreenOrientation`](xref:UnityEngine.InputSystem.InputSettings.compensateForScreenOrientation).
[`Input.deviceOrientation`](https://docs.unity3d.com/ScriptReference/Input-deviceOrientation.html)|No corresponding API yet.
[`Input.gyro`](https://docs.unity3d.com/ScriptReference/Input-gyro.html)|The `UnityEngine.Gyroscope` class is replaced by multiple separate sensor Devices in the new Input System:<br/>[`Gyroscope`](xref:UnityEngine.InputSystem.Gyroscope) to measure angular velocity.<br/>[`GravitySensor`](xref:UnityEngine.InputSystem.GravitySensor) to measure the direction of gravity.<br/>[`AttitudeSensor`](xref:UnityEngine.InputSystem.AttitudeSensor) to measure the orientation of the device.<br/>[`Accelerometer`](xref:UnityEngine.InputSystem.Accelerometer) to measure the total acceleration applied to the device.<br/>[`LinearAccelerationSensor`](xref:UnityEngine.InputSystem.LinearAccelerationSensor) to measure acceleration applied to the device, compensating for gravity.
[`Input.gyro.attitude`](https://docs.unity3d.com/ScriptReference/Gyroscope-attitude.html)|[`AttitudeSensor.current.orientation.ReadValue()`](xref:UnityEngine.InputSystem.AttitudeSensor).
[`Input.gyro.enabled`](https://docs.unity3d.com/ScriptReference/Gyroscope-enabled.html)|Get: `Gyroscope.current.enabled`<br/>Set:<br/>`EnableDevice(Gyroscope.current);`<br/>`DisableDevice(Gyroscope.current);`<br/><br/>__Note__: The new Input System replaces `UnityEngine.Gyroscope` with multiple separate sensor devices. Substitute [`Gyroscope`](xref:UnityEngine.InputSystem.Gyroscope) with other sensors in the sample as needed. See the notes for `Input.gyro` above for details.
[`Input.gyro.gravity`](https://docs.unity3d.com/ScriptReference/Gyroscope-gravity.html)|[`GravitySensor.current.gravity.ReadValue()`](xref:UnityEngine.InputSystem.GravitySensor)
[`Input.gyro.rotationRate`](https://docs.unity3d.com/ScriptReference/Gyroscope-rotationRate.html)|[`Gyroscope.current.angularVelocity.ReadValue()`](xref:UnityEngine.InputSystem.Gyroscope).
[`Input.gyro.rotationRateUnbiased`](https://docs.unity3d.com/ScriptReference/Gyroscope-rotationRateUnbiased.html)|No corresponding API yet.
[`Input.gyro.updateInterval`](https://docs.unity3d.com/ScriptReference/Gyroscope-updateInterval.html)|[`Sensor.samplingFrequency`](xref:UnityEngine.InputSystem.Sensor.samplingFrequency)<br/>Example:<br/>`Gyroscope.current.samplingFrequency = 1.0f / updateInterval;`<br/><br/>__Notes__:<br/>[`samplingFrequency`](xref:UnityEngine.InputSystem.Sensor.samplingFrequency) is in Hz, not in seconds as [`updateInterval`](https://docs.unity3d.com/ScriptReference/Gyroscope-updateInterval.html), so you need to divide 1 by the value.<br/><br/>The new Input System replaces `UnityEngine.Gyroscope` with multiple separate sensor devices. Substitute [`Gyroscope`](xref:UnityEngine.InputSystem.Gyroscope) with other sensors in the sample as needed. See the notes for `Input.gyro` above for details.
[`Input.gyro.userAcceleration`](https://docs.unity3d.com/ScriptReference/Gyroscope-userAcceleration.html)|[`LinearAccelerationSensor.current.acceleration.ReadValue()`](xref:UnityEngine.InputSystem.LinearAccelerationSensor)
[`Input.location`](https://docs.unity3d.com/ScriptReference/Input-location.html)|No corresponding API yet.
[`Input.GetAccelerationEvent`](https://docs.unity3d.com/ScriptReference/Input.GetAccelerationEvent.html)|See notes for `Input.accelerationEvents` above.
