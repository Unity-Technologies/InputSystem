[//]: # (//TODO: the screenshots are outdated and need updating)

# Migrate From Old Input System

This guide provides a listing of the APIs in `UnityEngine.Input` (and related APIs in `UnityEngine`) and their corresponding APIs in the new input system. Not all APIs have a corresponding version in the new API yet.

Note: The new APIs are currently in the `UnityEngine.InputSystem` namespace. The namespace is omitted here for brevity. `UnityEngine.InputSystem` is referenced in full for easy disambiguation.

## [`UnityEngine.Input`](https://docs.unity3d.com/ScriptReference/Input.html)

### [`UnityEngine.Input.acceleration`](https://docs.unity3d.com/ScriptReference/Input-acceleration.html)

`Accelerometer.current.acceleration.ReadValue()`

### [`UnityEngine.Input.accelerationEventCount`](https://docs.unity3d.com/ScriptReference/Input-accelerationEventCount.html)

See next section.

### [`UnityEngine.Input.accelerationEvents`](https://docs.unity3d.com/ScriptReference/Input-accelerationEvents.html)

Acceleration events are not made available separately from other input events. The following code will trace all input events on the `Accelerometer.curren` device.

```
    private InputEventTrace trace;

    void StartTrace()
    {
        trace = new InputEventTrace() {deviceId = Accelerometer.current.id};
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

### [`UnityEngine.Input.anyKey`](https://docs.unity3d.com/ScriptReference/Input-anyKey.html)

For keyboard keys:

`Keyboard.current.anyKey.isPressed`

For mouse buttons:

`Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed`

### [`UnityEngine.Input.anyKeyDown`](https://docs.unity3d.com/ScriptReference/Input-anyKeyDown.html)

`Keyboard.current.anyKey.wasUpdatedThisFrame`

### [`UnityEngine.Input.backButtonLeavesApp`](https://docs.unity3d.com/ScriptReference/Input-backButtonLeavesApp.html)

No corresponding API yet.

### [`UnityEngine.Input.compass`](https://docs.unity3d.com/ScriptReference/Input-compass.html)

No corresponding API yet.

### [`UnityEngine.Input.compensateSensors`](https://docs.unity3d.com/ScriptReference/Input-compensateSensors.html)

`Processors.CompensateDirectionProcessor` and `ProcessorsCompensateRotationProcessor` in combination with `InputSystem.settings.compensateForScreenOrientation`.

### [`UnityEngine.Input.compositionCursorPos`](https://docs.unity3d.com/ScriptReference/Input-compositionCursorPos.html)

`Keyboard.current.SetIMECursorPosition(myPosition)`

### [`UnityEngine.Input.compositionString`](https://docs.unity3d.com/ScriptReference/Input-compositionString.html)

```
	var compositionString = "";
	Keyboard.current.onIMECompositionChange += composition =>
	{
		compositionString = composition.ToString();
	};
```        

### [`UnityEngine.Input.deviceOrientation`](https://docs.unity3d.com/ScriptReference/Input-deviceOrientation.html)

No corresponding API yet.

### [`UnityEngine.Input.gyro`](https://docs.unity3d.com/ScriptReference/Input-gyro.html)

`Gyroscope.current`

### [`UnityEngine.Input.gyro.attitude`](https://docs.unity3d.com/ScriptReference/Gyroscope-attitude.html)

`AttitudeSensor.current.orientation.ReadValue()`

### [`UnityEngine.Input.gyro.enabled`](https://docs.unity3d.com/ScriptReference/Gyroscope-enabled.html)

```
// Get.
Gyroscope.current.enabled

// Set.
InputSystem.EnableDevice(Gyroscope.current);
InputSystem.DisableDevice(Gyroscope.current);
```

Note: `UnityEngine.Gyroscope` is replaced by multiple separate sensor devices in the new Input System (`Gyroscope`, `GravitySensor`, `AttitudeSensor`, `Accelerometer`, etc). Substitute `Gyroscope` with other sensors in the sample as needed.

### [`UnityEngine.Input.gyro.gravity`](https://docs.unity3d.com/ScriptReference/Gyroscope-gravity.html)

`GravitySensor.current.gravity.ReadValue()`

### [`UnityEngine.Input.gyro.rotationRate`](https://docs.unity3d.com/ScriptReference/Gyroscope-rotationRate.html)

`Gyroscope.current.angularVelocity.ReadValue()`

### [`UnityEngine.Input.gyro.rotationRateUnbiased`](https://docs.unity3d.com/ScriptReference/Gyroscope-rotationRateUnbiased.html)

No corresponding API yet.

### [`UnityEngine.Input.gyro.updateInterval`](https://docs.unity3d.com/ScriptReference/Gyroscope-updateInterval.html)

`Gyroscope.current.samplingFrequency = 1.0f / updateInterval;`

Notes: 
* `samplingFrequency` is in Hz, not in seconds as `updateInterval`, so you need to divide 1 by the value.
* `UnityEngine.Gyroscope` is replaced by multiple separate sensor devices in the new Input System (`Gyroscope`, `GravitySensor`, `AttitudeSensor`, `Accelerometer`, etc). Substitute `Gyroscope` with other sensors in the sample as needed.


### [`UnityEngine.Input.gyro.userAcceleration`](https://docs.unity3d.com/ScriptReference/Gyroscope-userAcceleration.html)

`Accelerometer.current.acceleration.acceleration.ReadValue()`

### [`UnityEngine.Input.imeCompositionMode`](https://docs.unity3d.com/ScriptReference/Input-imeCompositionMode.html)

No corresponding API yet.

### [`UnityEngine.Input.imeIsSelected`](https://docs.unity3d.com/ScriptReference/Input-imeIsSelected.html)

```
// Get:
Keyboard.current.imeSelected

// Set:
Keyboard.current.SetIMEEnabled(true);
```

### [`UnityEngine.Input.inputString`](https://docs.unity3d.com/ScriptReference/Input-inputString.html)

```
Keyboard.current.onText +=
    character => /* ... */;
```

### [`UnityEngine.Input.location`](https://docs.unity3d.com/ScriptReference/Input-location.html)

No corresponding API yet.

### [`UnityEngine.Input.mousePosition`](https://docs.unity3d.com/ScriptReference/Input-mousePosition.html)

`Mouse.current.position.ReadValue()`

Note: Mouse simulation from touch is not implemented yet.

### [`UnityEngine.Input.mousePresent`](https://docs.unity3d.com/ScriptReference/Input-mousePresent.html)

`Mouse.current != null`

### [`UnityEngine.Input.multiTouchEnabled`](https://docs.unity3d.com/ScriptReference/Input-multiTouchEnabled.html)

No corresponding API yet.

### [`UnityEngine.Input.simulateMouseWithTouches`](https://docs.unity3d.com/ScriptReference/Input-multiTouchEnabled.html)

No corresponding API yet.

### [`UnityEngine.Input.stylusTouchSupported`](https://docs.unity3d.com/ScriptReference/Input-stylusTouchSupported.html)

No corresponding API yet.

### [`UnityEngine.Input.touchCount`](https://docs.unity3d.com/ScriptReference/Input-touchCount.html)

`InputSystem.EnhancedTouch.Touch.activeTouches.Count`

### [`UnityEngine.Input.touches`](https://docs.unity3d.com/ScriptReference/Input-touches.html)

`InputSystem.EnhancedTouch.Touch.activeTouches`

### [`UnityEngine.Input.touchPressureSupported`](https://docs.unity3d.com/ScriptReference/Input-touchPressureSupported.html)

No corresponding API yet.

### [`UnityEngine.Input.touchSupported`](https://docs.unity3d.com/ScriptReference/Input-touchSupported.html)

`Touchscreen.current != null`

### [`UnityEngine.Input.GetAccelerationEvent`](https://docs.unity3d.com/ScriptReference/Input.GetAccelerationEvent.html)

See `UnityEngine.Input.accelerationEvents`.

### [`UnityEngine.Input.GetAxis`](https://docs.unity3d.com/ScriptReference/Input.GetAxis.html)

There is no global setup corresponding exactly to "virtual axis" setups in the old player input settings. Instead, sets of "input actions" can be set up as independent assets or put directly on your C# components.

As an example, let's recreate the following axis configuration:

![Fire1 Action in Old Input Manager](./Images/FireActionOldInputManager.png)

#### Option A: Put input actions on your component

1. Declare one or more fields or properties type `InputAction`.
   ```
   public class MyComponent : MonoBehaviour
   {
       public InputAction fireAction;
   ```
2. Hook up a response to the action.
   ```
       void Awake()
       {
           fireAction.performed += ctx => Fire();
       }

       void Fire()
       {
           //...
       }
   ```
3. Put the component on a `GameObject` and configure bindings in the inspector by clicking the plus sign on the bindings list to add bindings and using the "Pick" button to pick controls to bind to.

   ![MyComponent fireAction](./Images/MyComponentFireAction.png)
4. Enable and disable the action as needed.
   ```
       void OnEnable()
       {
           fireAction.Enable();
       }

       void OnDisable()
       {
           fireAction.Disable();
       }
   ```

#### Option B: Create input action asset

1. Create an input action asset by right-clicking in the project browser and selecting "Create >> Input Actions" (alternatively you can go to "Assets >> Create >> Input Actions" in the main menu bar). Give a name to the asset.
2. Double-click the asset to open the Input Actions editor window.
3. In the "Action Maps" column click "+" to add a new action map.
4. Double-click the "New action map" name to give the set a better name. E.g. "gameplay".
5. In the "Actions" column click "+" to add a new action.
6. Double-click the action to give it a name.
7. Add bindings to the action by clicking "+" on the acttion and using the Path popup button in the right column to select controls.
8. Enable "Generate C# Wrapper Class" in the inspector for the asset and hit "Apply". Your inspector should now look something like this:

   ![MyControls.inputactions](./Images/FireActionInputAsset.png)
9. Add an instance of the generated C# wrapper class to your component.
   ```
   public class MyComponent : MonoBehaviour
   {
       MyControls controls;
   ```
   
10. Create the instance and hook up a response to the fire action.

   ```
       public void Awake()
       {
		   controls = new MyControls();
           controls.gameplay.fire.performed += ctx => Fire();
       }
   ```
11. Enable and disable the action as appropriate.
   ```
       public void OnEnable()
       {
           controls.Enable();
       }

       public void OnDisable()
       {
           controls.Disable();
       }
   ```

#### Hints

- To force button-like behavior on the control referenced in a binding, add a "Press" interaction to it.
- You can access the control that triggered an action from the callback. Through it, you can also query its current value.
   ```
   fireAction.performed +=
       ctx =>
       {
           var control = ctx.control; // Grab control.
           var value = ctx.GetValue<float>(); // Read value from control.

           // Can do control-specific checks.
           var button = control as ButtonControl;
           if (button != null && button.wasPressedThisFrame)
               /* ... */;
       }
   ```

### [`UnityEngine.Input.GetAxisRaw`](https://docs.unity3d.com/ScriptReference/Input.GetAxisRaw.html)

Not directly applicable. You can use access any `InputControl.ReadUnprocessedValue()` to read unprocessed values from any control.

### [`UnityEngine.Input.GetButton`](https://docs.unity3d.com/ScriptReference/Input.GetButton.html)

See `UnityEngine.Input.GetAxis` for how to set up a binding to a button or axis.

### [`UnityEngine.input.GetButtonDown`](https://docs.unity3d.com/ScriptReference/Input.GetButtonDown.html)

See `UnityEngine.Input.GetAxis` for how to set up a binding to a button or axis. You can use the `Press` interaction to detect when a button is pressed.

You can also use `ButtonControl.wasPressedThisFrame` to detect if a button was pressed on the raw control without using bindings.

### [`UnityEngine.input.GetButtonUp`](https://docs.unity3d.com/ScriptReference/Input.GetButtonUp.html)

See `UnityEngine.Input.GetAxis` for how to set up a binding to a button or axis. You can use the `Press` interaction with a "Release Only" trigger to detect when a button is released.

You can also use `ButtonControl.wasReleasedThisFrame` to detect if a button was pressed on the raw control without using bindings.

### [`UnityEngine.Input.GetJoystickNames`](https://docs.unity3d.com/ScriptReference/Input.GetJoystickNames.html)

There is no API that corresponds to this 100% (for good reason; `GetJoystickNames` was never a good API).

Here are various ways to discover connected devices:

```
// Query a list of all connected devices (does not allocate; read-only access)
InputSystem.devices

// Get notified when a device is added or removed
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

### [`UnityEngine.Input.GetKey`](https://docs.unity3d.com/ScriptReference/Input.GetKey.html)

```
// Using KeyControl property directly.
Keyboard.current.spaceKey.isPressed
Keyboard.current.aKey.isPressed // etc.

// Using Key enum.
Keyboard.current[Key.Space].isPressed

// Using key name.
((KeyControl)Keyboard.current["space"]).isPressed
```

Note: Keys are identified by physical layout not according to the current language mapping of the keyboard. To query the name of the key according to the language mapping, use `KeyControl.displayName`.

### [`UnityEngine.Input.GetKeyDown`](https://docs.unity3d.com/ScriptReference/Input.GetKeyDown.html)

```
// Using KeyControl property directly.
Keyboard.current.spaceKey.wasPressedThisFrame
Keyboard.current.aKey.wasPressedThisFrame // etc.

// Using Key enum.
Keyboard.current[Key.Space].wasPressedThisFrame

// Using key name.
((KeyControl)Keyboard.current["space"]).wasPressedThisFrame
```

Note: Keys are identified by physical layout not according to the current language mapping of the keyboard. To query the name of the key according to the language mapping, use `KeyControl.displayName`.

### [`UnityEngine.Input.GetKeyUp`](https://docs.unity3d.com/ScriptReference/Input.GetKeyUp.html)

```
// Using KeyControl property directly.
Keyboard.current.spaceKey.wasReleasedThisFrame
Keyboard.current.aKey.wasReleasedThisFrame // etc.

// Using Key enum.
Keyboard.current[Key.Space].wasReleasedThisFrame

// Using key name.
((KeyControl)Keyboard.current["space"]).wasReleasedThisFrame
```

Note: Keys are identified by physical layout not according to the current language mapping of the keyboard. To query the name of the key according to the language mapping, use `KeyControl.displayName`.

### [`UnityEngine.Input.GetMouseButton`](https://docs.unity3d.com/ScriptReference/Input.GetMouseButton.html)

```
Mouse.current.leftButton.isPressed
Mouse.current.rightButton.isPressed
Mouse.current.middleButton.isPressed

// You can also go through all buttons on the mouse (does not allocate)
var controls = Mouse.current.allControls;
for (var i = 0; i < controls.Count; ++i)
{
    var button = controls[i] as ButtonControl;
    if (button != null && button.isPressed)
        /* ... */;
}

// Or look up controls by name
((ButtonControl)Mouse.current["leftButton"]).isPressed
```

### [`UnityEngine.Input.GetMouseButtonDown`](https://docs.unity3d.com/ScriptReference/Input.GetMouseButtonDown.html)

```
Mouse.current.leftButton.wasPressedThisFrame
Mouse.current.rightButton.wasPressedThisFrame
Mouse.current.middleButton.wasPressedThisFrame
```

### [`UnityEngine.Input.GetMouseButtonUp`](https://docs.unity3d.com/ScriptReference/Input.GetMouseButtonUp.html)

```
Mouse.current.leftButton.wasReleasedThisFrame
Mouse.current.rightButton.wasReleasedThisFrame
Mouse.current.middleButton.wasReleasedThisFrame
```

### [`UnityEngine.Input.GetTouch`](https://docs.unity3d.com/ScriptReference/Input.GetTouch.html)

`InputSystem.EnhancedTouch.Touch.activeTouches[i]`

### [`UnityEngine.Input.IsJoystickPreconfigured`](https://docs.unity3d.com/ScriptReference/Input.IsJoystickPreconfigured.html)

Not directly applicable. But in general, you should assume that devices which derive from `Gamepad` will correctly implement the mapping of axes and buttons to the corresponding `InputControl` members of the `Gamepad` class.

### [`UnityEngine.Input.ResetInputAxes`](https://docs.unity3d.com/ScriptReference/Input.ResetInputAxes.html)

Not directly applicable.

## [`UnityEngine.TouchScreenKeyboard`](https://docs.unity3d.com/ScriptReference/TouchScreenKeyboard.html)

No corresponding API yet. Use `TouchScreenKeyboard` for now.
