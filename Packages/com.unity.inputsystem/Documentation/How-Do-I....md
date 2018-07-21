# ... check if the space key has been pressed this frame?

```C#
    Keyboard.current.space.wasJustPressed
```

Same kinda deal works for other devices.

```C#
    Gamepad.current.aButton.wasJustPressed
```

# ... find all connected gamepads?

You can ask `Gamepad`.

```C#
    var allGamepads = Gamepad.all;
```

Or you have some more involved options:

```C#

    // Go through all devices and select gamepads.
    InputSystem.devices.Select(x => x is Gamepad);

    // Query everything that is using the gamepad template or based on that template.
    InputSystem.GetControls("/<gamepad>");

    // Fetch all devices with "gamepad" in their names (not a good idea; no guarantee
    // a gamepad is actually named that way).
    InputSystem.GetControls("/gamepad*");

```

# ... know when a new device has been plugged in?

```C#
    InputSystem.onDeviceChange +=
        (device, change) =>
        {
            if (change == InputDeviceChange.Added)
                /* New Device */;
            else if (change == InputDeviceChange.Disconnected)
                /* Device got unplugged */;
            else if (change == InputDeviceChange.Connected)
                /* Plugged back in */;
            else if (change == InputDeviceChange.Removed)
                /* Remove from input system entirely; by default, devices stay in the system once discovered */;
        }
```

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

Note that you still need to enable the action in code and hook up your response. You can do so in the Awake() method of your component.

```C#

    void Awake()
    {
        fireAction.performed += _ => Fire;
        walkAction.performed += _ => Walk;
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

    void Fire(InputAction action, InputControl control)
    {
        //...
    }

    void Walk(InputAction action, InputControl control)
    {
        //...
    }

```

    ////TODO: Working on a way to nicely package up actions in action sets

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
        if (fireAction.hasBeenPerformedThisFrame)
            Fire();
        if (walkAction.hasBeenPerformedThisFrame)
            Walk();
    }

    void Fire()
    {
        //...
    }

    void Walk(InputAction action, InputControl control)
    {
        //...
    }

```

    ////TODO: that last way I'm still working on

# ... require a button to be held for 0.4 seconds before triggering an action?

Put a HoldModifier on the action. In code, this works like so:

```C#

    var action = new InputAction(binding: "*/{PrimaryAction}",
        modifiers: "hold(duration=0.4)");

```

To display UI feedback when the button starts being held, use the `started` callback.

```C#

    action.started += _ => ShowGunChargeUI();
    action.performed += _ => FinishGunChargingAndHideChargeUI();
    action.cancelled += _ => HideChargeUI();

```

    ////TODO: still working on the modifier mechanics

# ... require the left trigger to be held and then the A button to be pressed and held for 0.4 seconds?

```C#

    var action = new InputAction();
    action.AddBinding("/<gamepad>/leftTrigger")
        .CombinedWith("/<gamepad>/buttonSouth", modifiers: "hold(duration=0.4)");

```

Again, setting this up with the inspector in the editor is an alternative to dealing with the path strings directly.

# ... use a "positive" and a "negative" key to drive an axis?

    ////TODO: working on this
    ////      ATM you can do this at the control level by, for example, customizing
    ////      the Keyboard template to add a custom AxisControl that reads its state
    ////      from two buttons.
    ////      Figuring out how this could be done more easily on the fly and at the
    ////      action level. Do we really need it though?

# ... separate the actions in my game from user-overridable bindings?

Put your actions in one JSON file and put your default bindings in another JSON file. At runtime, load the actions and then load either the default bindings or a customized version from the user's profile.

```C#

    ////TODO: still fleshing out the APIs for this

```

# ... create a UI to rebind input in my game?

    ////TODO

# ... set up an action to specifically target the left-hand XRController?

```C#

    var action = new InputAction(binding: "/<XRController>{leftHand}/position");

```

Again, the inspector allows setting this up without having to deal with paths directly.

# ... wait for any button to be pressed on any device?

    ////TODO: this needs work

One way is to use actions.

```C#
    var myAction = new InputAction(binding: "/*/<button>");
    myAction.onPerformed += (action, control) => Debug.Log($"Button {control.name} pressed!");
    myAction.Enable();
```

However, this is dirt inefficient. The amount of processing an action has to do is directly correlated with the amount of controls it is targeting. Targeting every single button of every single device will yield a ton of controls and result in high processing overhead. The keyboard alone will contribute a ton of buttons each of which will have to be processed individually.

A more efficient way is to just listen for any activity on any device and when there was activity, find out whether it came from a button.

```C#
    ... still being worked on; can already listen on whole devices but you won't know what control caused the state change.
```

# .. switch to a lefty gamepad?

```C#

    var gamepad = Gamepad.current; // Or whatever gamepad you are using.
    InputSystem.SetVariant(gamepad, "Lefty");

```

This will swap the sticks, the triggers, and the shoulder buttons.

# ... make my left-hand XR controller my right-hand one?

```C#
    var controller = XRController.leftHand;
    InputSystem.SetUsage(controller, CommonUsages.RightHand);
```

# ... get all current touches from the touchscreen?

    ////TODO

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
             // ... customize control setup
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
        public static FourCC kFormat = new FourCC('M', 'D', 'E', 'V');

        [InputControl(name = "firstButton", template = "Button", bit = 0)]
        [InputControl(name = "secondButton", template = "Button", bit = 1)]
        public int buttons;
        [InputControl(template = "Analog", parameters="clamp=true,clampMin=0,clampMax=1")]
        public float axis;

        public FourcCC GetFormat()
        {
             return kFormat;
        }
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

////TODO: need a way to give devices an opportunity to feed events; ATM you have to make that happen yourself and events will only go in the next update this way

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

# ... deal with my device being both a keyboard and a mouse?

    ////TODO: working on allowing templates to create more than one device which can share state

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

In fact, the gamepad template already adds a deadzone processor which will take its min and max values from `InputConfiguration.DefaultDeadzoneMin` and `InputConfiguration.DefaultDeadzoneMax`.

    I'm still working on a way to do add a deadzone processor conveniently on the fly to an existing gamepad instance.

# ... give my head tracking an extra update before rendering?

First enable before render updates on your device.

```JSON
    {
        "name" : "MyHMD",
        "extend" : "HMD",
        "beforeRender" : "Update"
    }
```

And then make sure you put extra StateEvents for your HMD on the queue right in time before rendering. Also, if your HMD is a combination of non-tracking and tracking controls, you can update just the tracking, if you want to, by sending a DeltaEvent instead of a full StateEvent.

# ... simulate HMD movement from mouse and keyboard?

    I'm working on a callback that allows state to be updated from state and see the change in the same frame

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
        eventPtr =>
        {
            // Can handle events yourself, for example, and then stop them
            // from further processing by marking them as handled.
            eventPtr.handled = true;
        };

```

# ... see what devices I have and what state they are in?

Go to `Windows >> Input Debugger`.

    ////TODO: working on having device setups from players mirrored 1:1 into the running input system in the editor (including having their input available in the editor)
