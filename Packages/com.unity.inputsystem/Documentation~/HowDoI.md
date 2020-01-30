# How do I…?

Devices:

* […check if the space key has been pressed this frame?](#check-if-the-space-key-has-been-pressed-this-frame)
* […find all connected gamepads?](#find-all-connected-gamepads)
* […find the gamepad that the player is currently using?](#find-the-gamepad-that-the-player-is-currently-using)
* […know when a new device has been plugged in?](#know-when-a-new-device-has-been-plugged-in)

Actions:

* […create a simple Fire-type Action?](#create-a-simple-fire-type-action)
* […require a button to be held for 0.4 seconds before triggering an Action?](#require-a-button-to-be-held-for-04-seconds-before-triggering-an-action)
* […use a "positive" and a "negative" button to drive an axis?](#use-a-positive-and-a-negative-button-to-drive-an-axis)
* […create a UI to rebind input in my game?](#create-a-ui-to-rebind-input-in-my-game)

## …check if the space key has been pressed this frame?

Use this code:

```CSharp
    Keyboard.current.space.wasPressedThisFrame
```

You can adapt this code to other Devices that have buttons or other types of input:

```CSharp
    Gamepad.current.aButton.wasPressedThisFrame
```

## …find all connected gamepads?

Use the [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html) class:

```CSharp
    var allGamepads = Gamepad.all;

    // Or more specific versions.
    var allPS4Gamepads = DualShockGamepadPS4.all;
```

Alternatively, you can use more generic Device queries using LINQ expressions or [control paths](Controls.md#control-paths):

```CSharp

    // Go through all devices and select gamepads.
    InputSystem.devices.Select(x => x is Gamepad);

    // Query everything that is using the gamepad template or based on that template.
    // NOTE: Don't forget to Dispose() the result.
    InputSystem.FindControls("<gamepad>");
```

## …find the gamepad that the player is currently using?

Use this code:

```CSharp
    var gamepad = Gamepad.current;

    // This works for other types of devices, too.
    var keyboard = Keyboard.current;
    var mouse = Mouse.current;
```

# …know when a new Device has been plugged in?

Use this code:

```CSharp
    InputSystem.onDeviceChange +=
        (device, change) =>
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    // New Device.
                    break;
                case InputDeviceChange.Disconnected:
                    // Device got unplugged.
                    break;
                case InputDeviceChange.Connected:
                    // Plugged back in.
                    break;
                case InputDeviceChange.Removed:
                    // Remove from Input System entirely; by default, Devices stay in the system once discovered.
                    break;
                default:
                    // See InputDeviceChange reference for other event types.
                    break;
            }
        }
```

For more details, see documentation on ["Monitoring Devices"](Devices.md#monitoring-devices).

# …create a simple Fire-type Action?

Use this code:

```C#
    // Create an Action that binds to the primary action control on all devices.
    var action = new InputAction(binding: "*/{primaryAction}");

    // Have it run your code when the Action is triggered.
    action.performed += _ => Fire();

    // Start listening for control changes.
    action.Enable();
```

You can also have a serialized field of type `InputAction` in your `MonoBehaviour`, like this:

```C#

public class MyControllerComponent : MonoBehaviour
{
    public InputAction fireAction;
    public InputAction walkAction;
}

```

The Editor lets you add and edit Bindings for the Actions in the Inspector window.

>__Note__: You still need to enable the Action in code and hook up your response. You can do so in the `Awake` method of your component:

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

If you're worried about allocations from the delegates, you can use a polling approach rather than a callback-driven approach.

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

Typically, you need to deal with more than one Action. In this case, you might want to put these into an Input Actions Asset file. To create an Input Actions Asset, follow these steps:

1. In the Project view, click __Create__.
2. Select __Input Actions__.
3. Double-click the Asset to create and edit one or multiple Input Action Maps containing Input Actions.

If you then click __Generate C# Class__ in the Inspector for that Asset, Unity generates a wrapper class for your Actions which you can use like this:

```C#
    MyInputActionAssetClass actions;

    public void OnEnable()
    {
        actions = new MyInputActionAssetClass();
        controls.myActionsMap.fire.performed += Fire;
        controls.myActionsMap.walk.performed += Walk;
    }
```

For more details, see documentation on [Actions](Actions.md).

# …require a button to be held for 0.4 seconds before triggering an Action?

Put a [hold Interaction](Interactions.md#hold) on the Action, like this:

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

# …use a "positive" and a "negative" button to drive an axis?

Use an [axis composite](ActionBindings.md#1d-axis) binding in the UI or in code, and bind its parts to the respective buttons.

```CSharp
var accelerateAction = new InputAction("Accelerate");
accelerateAction.AddCompositeBinding("Axis")
    .With("Positive", "<Gamepad>/rightTrigger")
    .With("Negative", "<Gamepad>/leftTrigger");
```

# …create a UI to rebind input in my game?

Create a UI with a button to trigger rebinding. If the user clicks the button to bind a control to an action, use [`InputAction.PerformInteractiveRebinding`](../api/UnityEngine.InputSystem.InputActionRebindingExtensions.html#UnityEngine_InputSystem_InputActionRebindingExtensions_PerformInteractiveRebinding_UnityEngine_InputSystem_InputAction_System_Int32_) to handle the rebinding:

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

You can install the Tanks Demo sample for the Input System package using the Package Manager, which has an example of an interactive rebinding UI.

# …set up an Action to specifically target the left-hand XR controller?

Use this code:

```CSharp
    var action = new InputAction(binding: "/<XRController>{leftHand}/position");
```

You can also set this up for any Input Bindings in the Inspector or the Input Action Asset editor window without having to deal with paths directly.

# …wait for any button to be pressed on any device?

[//]: # (TODO this needs work)

Use this code:

```CSharp
    var myAction = new InputAction(binding: "/*/<button>");
    myAction.onPerformed += (action, control) => Debug.Log($"Button {control.name} pressed!");
    myAction.Enable();
```

>__Note__: This is inefficient and resource-intensive. The amount of processing an Action has to do is directly correlated with the amount of Controls it targets. Targeting every single button of every single Device yields a large number of Controls and results in a high processing overhead. For example, the keyboard alone contributes many buttons, each of which has to be processed individually.

[//]: # (TODO: A more efficient way is to just listen for any activity on any device and when there was activity, find out whether it came from a button.)

# …make my left-hand XR controller my right-hand one?

Use this code:

```C#
    var controller = XRController.leftHand;
    InputSystem.SetUsage(controller, CommonUsages.RightHand);
```

# …get all current touches from the touchscreen?

The recommended way is to use [`EnhancedTouch.Touch.activeTouches`](../api/UnityEngine.InputSystem.EnhancedTouch.Touch.html#UnityEngine_InputSystem_EnhancedTouch_Touch_activeTouches):

```C#
    using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

    public void Update()
    {
        foreach (var touch in Touch.activeTouches)
            Debug.Log($"{touch.touchId}: {touch.screenPosition},{touch.phase}");
    }
```

>__Note__: You must first enable enhanced touch support by calling  [`InputSystem.EnhancedTouch.Enable()`](../api/UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.html#UnityEngine_InputSystem_EnhancedTouch_EnhancedTouchSupport_Enable).

You can also use the lower-level [`Touchscreen.current.touches`](../api/UnityEngine.InputSystem.Touchscreen.html#UnityEngine_InputSystem_Touchscreen_touches) API.

# …create a Device?

Use this code:

```C#
    InputSystem.AddDevice<Gamepad>();
```

An alternative way is to inform the Input System that a Device is available, and let the system create the Device from a matching template. If no template exists, the Input System doesn't create the Device until you add one.

```C#
    InputSystem.ReportAvailableDevice(
        new InputDeviceDescription
        {
            product = "AwesomeGadget",
            manufacturer = "Awesome Products Inc."
        }
    );
```

# …create my own custom Devices?

There are two possible ways to do this.

If you want to use one of the existing C# [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) classes in code to interface with your Device, you can build on an existing template using JSON:

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
You then register your template with the system and then instantiate it:

```C#
    InputSystem.RegisterTemplate(myDeviceJson);
    var device = InputSystem.AddDevice("MyDevice");
```

Alternatively, you can create your own [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) class and state layouts in C#.

```C#
    public struct MyDeviceState : IInputStateTypeInfo
    {
        // FourCC type codes are used to identify the memory layouts of state blocks.
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

To create an instance of your Device, register it as a template and then instantiate it:

```C#
    InputSystem.RegisterTemplate("MyDevice", typeof(MyDevice));
    InputSystem.AddDevice("MyDevice");
```

For more information, see documentation on [HID](HID.md#overriding-the-hid-fallback).

[//]: # (TODO: need a way to give devices an opportunity to feed events; ATM you have to make that happen yourself and events will only go in the next update this way)

# …deal with my gamepad data arriving in a format different from `GamepadState`?

Extend the "Gamepad" template and customize its Controls.

A real-world example of this is the Xbox Controller on macOS, which is supported through HID. Its template looks like this:

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

The same principle applies if some buttons on your Device are swapped, for example. In this case, you can remap their offsets.

# …force the Input System to use my own template when the native backend discovers a specific Device?

Describe the Device in the template, like this:

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

Note that you don't have to restart Unity in order for changes in your template to take effect on native Devices. The Input System applies changes automatically on every domain reload, so you can just keep refining a template and your Device is recreated with the most up-to-date version every time scripts are recompiled.

[//]: # (# …deal with my device being both a keyboard and a mouse?)
[//]: # (////TODO: working on allowing templates to create more than one device which can share state)

# …add deadzoning to my gamepad sticks?

Put a [stick deadzone Processor](Processors.md#stick-deadzone) on the sticks, like this:

```JSON
     {
        "name" : "MyGamepad",
        "extend" : "Gamepad",
        "controls" : [
            {
                "name" : "leftStick",
                "processors" : "stickDeadzone(min=0.125,max=0.925)"
            },
            {
                "name" : "rightStick",
                "processors" : "stickDeadzone(min=0.125,max=0.925)"
            }
        ]
    }
```

You can do the same in your C# state structs.

```C#
    public struct MyDeviceState
    {
        [InputControl(processors = "stickDeadzone(min=0.125,max=0.925)"]
        public StickControl leftStick;
        [InputControl(processors = "stickDeadzone(min=0.125,max=0.925)"]
        public StickControl rightStick;
    }
```

The gamepad template already adds stick deadzone processors which take their min and max values from [`InputSettings.defaultDeadzoneMin`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultDeadzoneMin) and [`InputSettings.defaultDeadzoneMax`](../api/UnityEngine.InputSystem.InputSettings.html#UnityEngine_InputSystem_InputSettings_defaultDeadzoneMax).

[//]: # (I'm still working on a way to do add a deadzone processor conveniently on the fly to an existing gamepad instance.)

# …give my head tracking an extra update before rendering?

First, enable before render updates on your Device.

```JSON
    {
        "name" : "MyHMD",
        "extend" : "HMD",
        "beforeRender" : "Update"
    }
```

Then, make sure you put extra `StateEvents` for your HMD on the queue right in time before rendering. Also, if your HMD is a combination of non-tracking and tracking controls, you can update just the tracking by sending a delta event instead of a full state event.

[//]: # (TODO: What is the "right queue"?)

[//]: # (TODO:# …simulate HMD movement from mouse and keyboard?)
[//]: # (TODO:I'm working on a callback that allows state to be updated from state and see the change in the same frame)

# …record events flowing through the system?

Use this code:

```C#

    var trace = new InputEventTrace(); // Can also give device ID to only
                                       // trace events for a specific device.

    trace.Enable();

    //…run stuff

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

# …see events as they're processed?

Use this code:

```C#

    InputSystem.onEvent +=
        (eventPtr, device) =>
        {
            // Can handle events yourself, for example, and then stop them
            // from further processing by marking them as handled.
            eventPtr.handled = true;
        };

```

[//]: # (TODO:# …create an initial-engagement kind of screen?)


# …see what Devices I have and what state they're in?

Go to __Windows > Analysis > Input Debugger__(Debugging.md), then double click on a Device to see its Controls. You can also click the __Remote Devices__ button to remotely see Devices from Unity Players deployed to any connected computers or devices.

[//]: # (TODO: working on having device setups from players mirrored 1:1 into the running input system in the editor (including having their input available in the editor))
