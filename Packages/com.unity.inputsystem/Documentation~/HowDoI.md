# How Do I...?

Devices:

* [... check if the space key has been pressed this frame?](#-check-if-the-space-key-has-been-pressed-this-frame)
* [... find all connected gamepads?](#-find-all-connected-gamepads)
* [... find the gamepad currently used by the player?](#-find-the-gamepad-currently-used-by-the-player)
* [... know when a new device has been plugged in?](#-know-when-a-new-device-has-been-plugged-in)

Actions:

UIs:

## ... check if the space key has been pressed this frame?

```CSharp
    Keyboard.current.space.wasPressedThisFrame
```

Same deal works for other devices.

```CSharp
    Gamepad.current.aButton.wasPressedThisFrame
```

## ... find all connected gamepads?

You can ask [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html).

```CSharp
    var allGamepads = Gamepad.all;

    // Or more specific versions.
    var allPS4Gamepads = DualShockGamepadPS4.all;
```

Or you have some more involved options:

```CSharp

    // Go through all devices and select gamepads.
    InputSystem.devices.Select(x => x is Gamepad);

    // Query everything that is using the gamepad template or based on that template.
    // NOTE: Don't forget to Dispose() the result.
    InputSystem.FindControls("<gamepad>");
```

The last solution uses [control paths](Controls.md#control-paths).

## ... find the gamepad currently used by the player?

```CSharp
    var gamepad = Gamepad.current;

    // This works for other types of devices, too.
    var keyboard = Keyboard.current;
    var mouse = Mouse.current;
```

# ... know when a new device has been plugged in?

```CSharp
    InputSystem.onDeviceChange +=
        (device, change) =>
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    // New Device
                    break;
                case InputDeviceChange.Disconnected:
                    // Device got unplugged
                    break;
                case InputDeviceChange.Connected:
                    // Plugged back in
                    break;
                case InputDeviceChange.Removed:
                    // Remove from input system entirely; by default, devices stay in the system once discovered
                    break;
                default:
                    // See InputDeviceChange reference for other event types.
                    break;
            }
        }
```

For more details, see ["Monitoring Devices"](Devices.md#monitoring-devices).

# ... create a simple fire-type action?

One way to do so is directly in code:

```C#
    // Create action that binds to the primary action control on all devices.
    var action = new InputAction(binding: "*/{primaryAction}");

    // Have it run your code when action is triggered.
    action.performed += _ => Fire();

    // Start listening for control changes.
    action.Enable();
```

The second way is to simply have a serialized field of type InputAction in your MonoBehaviour like this:

```C#

public class MyControllerComponent : MonoBehaviour
{
    public InputAction fireAction;
    public InputAction walkAction;
}

```

In the editor, you will be presented with a nice inspector that allows you to add bindings to the actions and choose where the bindings go without having to fiddle around with path strings.

Note that you still need to enable the action in code and hook up your response. You can do so in the `Awake` method of your component.

```C#

    void Awake()
    {
        fireAction.performed += Fire;
        walkAction.performed += Walk;
    }

    void OnEnable()
    {
        fireAction.Enable();
        walkAction.Enable();
    }

    void OnDisable()
    {
        fireAction.Disable();
        walkAction.Disable();
    }

    void Fire(CallbackContext ctx)
    {
        //...
    }

    void Walk(CallbackContext ctx)
    {
        //...
    }

```

If you are worried about GC from the delegates, you can also use a polling approach rather than a callback-driven approach.

```C#

    void OnEnable()
    {
        fireAction.Enable();
        walkAction.Enable();
    }

    void OnDisable()
    {
        fireAction.Disable();
        walkAction.Disable();
    }

    void Update()
    {
        if (fireAction.triggered)
            Fire();
        if (walkAction.triggered)
            Walk();
    }

    void Fire()
    {
        //...
    }

    void Walk()
    {
        //...
    }

```

Typically you need to deal with multiple actions. In this case you may want to put these into an Input Actions asset file. Create an Input Actions asset by selecting `Input Actions` in the `Create` popup button in the Project view. Double click the asset to create and edit one of multiple Input Action maps containing Input Actions. If you then click `Generate C# Class` in the inspector for the asset, Unity will generate a wrapper class for your actions which you can use like this:

```C#
    MyInputActionAssetClass actions;

    public void OnEnable()
    {
        actions = new MyInputActionAssetClass();
        controls.myActionsMap.fire.performed += Fire;
        controls.myActionsMap.walk.performed += Walk;
    }
```

For more details, see ["Actions"](Actions.md).

# ... require a button to be held for 0.4 seconds before triggering an action?

Put a Hold interaction on the action. In code, this works like so:

```C#

    var action = new InputAction(binding: "*/{PrimaryAction}",
        modifiers: "hold(duration=0.4)");

```

To display UI feedback when the button starts being held, use the [`started`](../api/UnityEngine.InputSystem.InputAction.html#UnityEngine_InputSystem_InputAction_started) callback.

```C#

    action.started += _ => ShowGunChargeUI();
    action.performed += _ => FinishGunChargingAndHideChargeUI();
    action.cancelled += _ => HideChargeUI();

```

# ... use a "positive" and a "negative" button to drive an axis?

Use an "Axis" composite binding in the UI or in code and bind its parts to the respective buttons.

```CSharp
var accelerateAction = new InputAction("Accelerate");
accelerateAction.AddCompositeBinding("Axis")
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

There are parameters available to tweak the axis' behavior. See [here](ActionBindings.md#1d-axis) for details.

# ... create a UI to rebind input in my game?

Create a UI with a button to trigger rebinding. If the user clicks the button to bind a control to an action, use [`InputAction.PerformInteractiveRebinding`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_PerformInteractiveRebinding_UnityEngine_InputSystem_InputAction_) to handle the rebinding:

```C#
    void RemapButtonClicked(InputAction actionToRebind)
    {
        var rebindOperation = actionToRebind.PerformInteractiveRebinding()
                    // To avoid accidental input from mouse motion
                    .WithControlsExcluding("Mouse")
                    .OnMatchWaitForAnother(0.1f)
                    .Start();
    }
```

[//]: # (TODO: Link the remap screen from tanks demo once that has a linkable home)

# ... set up an action to specifically target the left-hand XRController?

```CSharp
    var action = new InputAction(binding: "/<XRController>{leftHand}/position");
```

Again, the inspector allows setting this up without having to deal with paths directly.

# ... wait for any button to be pressed on any device?

[//]: # (TODO this needs work)

One way is to use actions.

```CSharp
    var myAction = new InputAction(binding: "/*/<button>");
    myAction.onPerformed += (action, control) => Debug.Log($"Button {control.name} pressed!");
    myAction.Enable();
```

However, this is inefficient. The amount of processing an action has to do is directly correlated with the amount of controls it is targeting. Targeting every single button of every single device will yield a ton of controls and result in high processing overhead. The keyboard alone will contribute a ton of buttons each of which will have to be processed individually.

# ... make my left-hand XR controller my right-hand one?

```C#
    var controller = XRController.leftHand;
    InputSystem.SetUsage(controller, CommonUsages.RightHand);
```

[//]: # (TODO: A more efficient way is to just listen for any activity on any device and when there was activity, find out whether it came from a button.)

# ... get all current touches from the touchscreen?

The recommended way is to use [`EnhancedTouch.Touch.activeTouches`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeTouches):

```C#
    using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

    public void Update()
    {
        foreach (var touch in Touch.activeTouches)
            Debug.Log($"{touch.touchId}: {touch.screenPosition},{touch.phase}");
    }
```

>NOTE: Enable enhanced touch support first by calling [`InputSystem.EnhancedTouch.Enable()`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable).

(You can also use the lower-level [`Touchscreen.current.touches`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_touches) API)

# ... create a device?

```C#
    InputSystem.AddDevice("Gamepad");
```

The given string is a template name.

An alternative way is to tell about the availability of a device and let the system create a device from a matching template -- or not, if there is none (though if you later add one, the device will be created then).

```C#
    InputSystem.ReportAvailableDevice(
        new InputDeviceDescription
        {
            product = "AwesomeGadget",
            manufacturer = "Awesome Products Inc."
        }
    );
```

# ... create my own custom devices?

Two possible ways. If you are okay with using one of the existing C# InputDevice classes in code to interface with your device, you can just build on an existing template using JSON.

```
    {
        "name" : "MyDevice",
        "extend" : "Gamepad", // Or some other thing
        "controls" : [
            {
                "name" : "firstButton",
                "layout" : "Button",
                "offset" : 0,
                "bit": 0,
                "format" : "BIT",
            },
            {
                "name" : "secondButton",
                "layout" : "Button",
                "offset" : 0,
                "bit": 1,
                "format" : "BIT",
            },
            {
                "name" : "axis",
                "layout" : "Axis",
                "offset" : 4,
                "format" : "FLT",
                "parameters" : "clamp=true,clampMin=0,clampMax=1"
            }
        ]
    }
```
You simply register your template with the system and then instantiate it.

```C#
    InputSystem.RegisterTemplate(myDeviceJson);
    var device = InputSystem.AddDevice("MyDevice");
```

Alternatively, you can create your own InputDevice class and state layouts in C#.

```C#
    public struct MyDeviceState : IInputStateTypeInfo
    {
        // FourCC type codes are used identify the memory layouts of state blocks.
        public FourCC format => new FourCC('M', 'D', 'E', 'V');

        [InputControl(name = "firstButton", template = "Button", bit = 0)]
        [InputControl(name = "secondButton", template = "Button", bit = 1)]
        public int buttons;
        [InputControl(template = "Analog", parameters="clamp=true,clampMin=0,clampMax=1")]
        public float axis;
    }

    [InputState(typeof(MyDeviceState)]
    public class MyDevice : InputDevice
    {
        public ButtonControl firstButton { get; private set; }
        public ButtonControl secondButton { get; private set; }
        public AxisControl axis { get; private set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
             firstButton = setup.GetControl<ButtonControl>(this, "firstButton");
             secondButton = setup.GetControl<ButtonControl>(this, "secondButton");
             axis = setup.GetControl<AxisControl>(this, "axis");
             base.FinishSetup(setup);
        }
    }
```

To create an instance of your device, register it as a template and then instantiate it

```C#
    InputSystem.RegisterTemplate("MyDevice", typeof(MyDevice));
    InputSystem.AddDevice("MyDevice");
```

See the [documentation on HID](HID.md#overriding-the-hid-fallback) for a more detailed description.

[//]: # (TODO: need a way to give devices an opportunity to feed events; ATM you have to make that happen yourself and events will only go in the next update this way)

# ... deal with my gamepad data arriving in a format different from `GamepadState`?

Extend the "Gamepad" template and customize its controls.

A real-world example of this is the Xbox Controller on OSX which is supported through HID. Its template looks like this:

```JSON
{
    "name" : "XboxGamepadOSX",
    "extend" : "Gamepad",
    "format" : "HID",
    "device" : { "interface" : "HID", "product" : "Xbox.*Controller" },
    "controls" : [
        { "name" : "leftShoulder", "offset" : 2, "bit" : 8 },
        { "name" : "rightShoulder", "offset" : 2, "bit" : 9 },
        { "name" : "leftStickPress", "offset" : 2, "bit" : 14 },
        { "name" : "rightStickPress", "offset" : 2, "bit" : 15 },
        { "name" : "buttonSouth", "offset" : 2, "bit" : 12 },
        { "name" : "buttonEast", "offset" : 2, "bit" : 13 },
        { "name" : "buttonWest", "offset" : 2, "bit" : 14 },
        { "name" : "buttonNorth", "offset" : 2, "bit" : 15 },
        { "name" : "dpad", "offset" : 2 },
        { "name" : "dpad/up", "offset" : 0, "bit" : 8 },
        { "name" : "dpad/down", "offset" : 0, "bit" : 9 },
        { "name" : "dpad/left", "offset" : 0, "bit" : 10 },
        { "name" : "dpad/right", "offset" : 0, "bit" : 11 },
        { "name" : "start", "offset" : 2, "bit" : 4 },
        { "name" : "select", "offset" : 2, "bit" : 5 },
        { "name" : "xbox", "offset" : 2, "bit" : 2, "template" : "Button" },
        { "name" : "leftTrigger", "offset" : 4, "format" : "BYTE" },
        { "name" : "rightTrigger", "offset" : 5, "format" : "BYTE" },
        { "name" : "leftStick", "offset" : 6, "format" : "VC2S" },
        { "name" : "leftStick/x", "offset" : 0, "format" : "SHRT", "parameters" : "normalize,normalizeMin=-0.5,normalizeMax=0.5" },
        { "name" : "leftStick/y", "offset" : 2, "format" : "SHRT", "parameters" : "invert,normalize,normalizeMin=-0.5,normalizeMax=0.5" },
        { "name" : "rightStick", "offset" : 10, "format" : "VC2S" },
        { "name" : "rightStick/x", "offset" : 0, "format" : "SHRT", "parameters" : "normalize,normalizeMin=-0.5,normalizeMax=0.5" },
        { "name" : "rightStick/y", "offset" : 2, "format" : "SHRT", "parameters" : "invert,normalize,normalizeMin=-0.5,normalizeMax=0.5" }
    ]
}
```

The same principle applies if on your device some buttons are swapped, for example. Simply remap their offsets.

# ... have my own template used when the native backend discovers a specific device?

Simply describe the device in the template.

```
     {
        "name" : "MyGamepad",
        "extend" : "Gamepad",
        "device" : {
            // All strings in here are regexs and case-insensitive.
            "product" : "MyController",
            "manufacturer" : "MyCompany"
        }
     }
```

Note that you don't have to restart Unity in order for changes in your template to take affect on native devices. On every domain reload, changes will automatically be applied so you can just keep refining a template and your device will get recreated with the most up-to-date version.

[//]: # (# ... deal with my device being both a keyboard and a mouse?)
[//]: # (////TODO: working on allowing templates to create more than one device which can share state)

# ... add deadzoning to my gamepad sticks?

Simply put a deadzone processor on the sticks.

```JSON
     {
        "name" : "MyGamepad",
        "extend" : "Gamepad",
        "controls" : [
            {
                "name" : "leftStick",
                "processors" : "deadzone(min=0.125,max=0.925)"
            },
            {
                "name" : "rightStick",
                "processors" : "deadzone(min=0.125,max=0.925)"
            }
        ]
    }
```

You can do the same in your C# state structs.

```C#
    public struct MyDeviceState
    {
        [InputControl(processors = "deadzone(min=0.125,max=0.925)"]
        public StickControl leftStick;
        [InputControl(processors = "deadzone(min=0.125,max=0.925)"]
        public StickControl rightStick;
    }
```

In fact, the gamepad template already adds a deadzone processor which will take its min and max values from [`InputSettings.defaultDeadzoneMin`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultDeadzoneMin) and [`InputSettings.defaultDeadzoneMax`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultDeadzoneMax).

[//]: # (I'm still working on a way to do add a deadzone processor conveniently on the fly to an existing gamepad instance.)

# ... give my head tracking an extra update before rendering?

First enable before render updates on your device.

```JSON
    {
        "name" : "MyHMD",
        "extend" : "HMD",
        "beforeRender" : "Update"
    }
```

And then make sure you put extra StateEvents for your HMD on the queue right in time before rendering. Also, if your HMD is a combination of non-tracking and tracking controls, you can update just the tracking, if you want to, by sending a delta event instead of a full state event.

[//]: # (TODO: What is the "right queue"?)

[//]: # (TODO:# ... simulate HMD movement from mouse and keyboard?)
[//]: # (TODO:I'm working on a callback that allows state to be updated from state and see the change in the same frame)

# ... record events flowing through the system?

```C#

    var trace = new InputEventTrace(); // Can also give device ID to only
                                       // trace events for a specific device.

    trace.Enable();

    //... run stuff

    var current = new InputEventPtr();
    while (trace.GetNextEvent(ref current))
    {
        Debug.Log("Got some event: " + current);
    }

    // Also supports IEnumerable.
    foreach (var eventPtr in trace)
        Debug.Log("Got some event: " + eventPtr);

    // Trace consumes unmanaged resources. Make sure to dispose.
    trace.Dispose();

```

# ... see events as they are processed?

```C#

    InputSystem.onEvent +=
        (eventPtr, device) =>
        {
            // Can handle events yourself, for example, and then stop them
            // from further processing by marking them as handled.
            eventPtr.handled = true;
        };

```

[//]: # (TODO:# ... create an initial-engagement kind of screen?)


# ... see what devices I have and what state they are in?

Go to [`Windows >> Analysis >> Input Debugger`](Debugging.md). Double click on a device to see it's controls. You can also remotely see devices from Unity Players deployed to any connected devices, using the "Remote Devices…" popup button.

[//]: # (TODO: working on having device setups from players mirrored 1:1 into the running input system in the editor (including having their input available in the editor))
