---
uid: input-system-custom-class-layout
---

# Use a custom class to create a layout

You can create your own [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) class and state layouts in C# to create a custom layout as follows:

```c#

   public struct MyDeviceState : IInputStateTypeInfo
    {
        // FourCC type codes are used to identify the memory layouts of state blocks.
        public FourCC format => new FourCC('M', 'D', 'E', 'V');

        [InputControl(name = "firstButton", layout = "Button", bit = 0)]
        [InputControl(name = "secondButton", layout = "Button", bit = 1)]
        public int buttons;
        [InputControl(layout = "Analog", parameters="clamp=true,clampMin=0,clampMax=1")]
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

To create an instance of your device, register it as a layout and then instantiate it:

```c#

   InputSystem.RegisterControlLayout("MyDevice", typeof(MyDevice));
    InputSystem.AddDevice("MyDevice");

```

## Use a custom class to control a complex controller

This example workflow uses the same technique as the previous example, but provides more detail by using the PS4 DualShock controller as a more complex device to set up.

The following example assumes that the Input System doesn't already have a custom layout for the PS4 DualShock controller, and that you want to add such a layout.

In this example, you want to expose the controller as a [gamepad](gamepads-introduction.md) and you roughly know the HID data format used by the Device.

If you don't know the format of a given HID you want to support:

1. Plug the device into your computer
1. Open the Input Debugger (**Window** > **Analysis** > **Input Debugger**)
1. Open both the debugger view for the device and the window showing the HID descriptor
1. Go through the controls one by one, refer to the debug view, and correlate that to the controls in the HID descriptor.

You can also double-click individual events and compare the raw data coming in from the device. If you select two events in the event trace, you can then right-click them and choose **Compare** to open a window that shows only the differences between the two events.

### Define the input data

The first step is to describe in detail what format that input data for the device comes in, and the [`InputControl`](xref:UnityEngine.InputSystem.InputControl) instances that should read out individual pieces of information from that data.

The HID input reports from the PS4 controller look approximately like this:

```

struct PS4InputReport
{
    byte reportId;             // #0
    byte leftStickX;           // #1
    byte leftStickY;           // #2
    byte rightStickX;          // #3
    byte rightStickY;          // #4
    byte dpad : 4;             // #5 bit #0 (0=up, 2=right, 4=down, 6=left)
    byte squareButton : 1;     // #5 bit #4
    byte crossButton : 1;      // #5 bit #5
    byte circleButton : 1;     // #5 bit #6
    byte triangleButton : 1;   // #5 bit #7
    byte leftShoulder : 1;     // #6 bit #0
    byte rightShoulder : 1;    // #6 bit #1
    byte leftTriggerButton : 2;// #6 bit #2
    byte rightTriggerButton : 2;// #6 bit #3
    byte shareButton : 1;      // #6 bit #4
    byte optionsButton : 1;    // #6 bit #5
    byte leftStickPress : 1;   // #6 bit #6
    byte rightStickPress : 1;  // #6 bit #7
    byte psButton : 1;         // #7 bit #0
    byte touchpadPress : 1;    // #7 bit #1
    byte padding : 6;
    byte leftTrigger;          // #8
    byte rightTrigger;         // #9
}

```

You can translate this into a C\# struct:

```c#

// We receive data as raw HID input reports. This struct
// describes the raw binary format of such a report.
[StructLayout(LayoutKind.Explicit, Size = 32)]
struct DualShock4HIDInputReport : IInputStateTypeInfo
{
    // Because all HID input reports are tagged with the 'HID ' FourCC,
    // this is the format we need to use for this state struct.
    public FourCC format => new FourCC('H', 'I', 'D');

    // HID input reports can start with an 8-bit report ID. It depends on the device
    // whether this is present or not. On the PS4 DualShock controller, it is
    // present. We don't really need to add the field, but let's do so for the sake of
    // completeness. This can also help with debugging.
    [FieldOffset(0)] public byte reportId;

    // The InputControl annotations here probably look a little scary, but what we do
    // here is relatively straightforward. The fields we add we annotate with
    // [FieldOffset] to force them to the right location, and then we add InputControl
    // to attach controls to the fields. Each InputControl attribute can only do one of
    // two things: either it adds a new control or it modifies an existing control.
    // Given that our layout is based on Gamepad, almost all the controls here are
    // inherited from Gamepad, and we just modify settings on them.

    [InputControl(name = "leftStick", layout = "Stick", format = "VC2B")]
    [InputControl(name = "leftStick/x", offset = 0, format = "BYTE",
        parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
    [InputControl(name = "leftStick/left", offset = 0, format = "BYTE",
        parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0,clampMax=0.5,invert")]
    [InputControl(name = "leftStick/right", offset = 0, format = "BYTE",
        parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0.5,clampMax=1")]
    [InputControl(name = "leftStick/y", offset = 1, format = "BYTE",
        parameters = "invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
    [InputControl(name = "leftStick/up", offset = 1, format = "BYTE",
        parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0,clampMax=0.5,invert")]
    [InputControl(name = "leftStick/down", offset = 1, format = "BYTE",
        parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0.5,clampMax=1,invert=false")]
    [FieldOffset(1)] public byte leftStickX;
    [FieldOffset(2)] public byte leftStickY;

    [InputControl(name = "rightStick", layout = "Stick", format = "VC2B")]
    [InputControl(name = "rightStick/x", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
    [InputControl(name = "rightStick/left", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0,clampMax=0.5,invert")]
    [InputControl(name = "rightStick/right", offset = 0, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0.5,clampMax=1")]
    [InputControl(name = "rightStick/y", offset = 1, format = "BYTE", parameters = "invert,normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
    [InputControl(name = "rightStick/up", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0,clampMax=0.5,invert")]
    [InputControl(name = "rightStick/down", offset = 1, format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5,clamp,clampMin=0.5,clampMax=1,invert=false")]
    [FieldOffset(3)] public byte rightStickX;
    [FieldOffset(4)] public byte rightStickY;

    [InputControl(name = "dpad", format = "BIT", layout = "Dpad", sizeInBits = 4, defaultState = 8)]
    [InputControl(name = "dpad/up", format = "BIT", layout = "DiscreteButton", parameters = "minValue=7,maxValue=1,nullValue=8,wrapAtValue=7", bit = 0, sizeInBits = 4)]
    [InputControl(name = "dpad/right", format = "BIT", layout = "DiscreteButton", parameters = "minValue=1,maxValue=3", bit = 0, sizeInBits = 4)]
    [InputControl(name = "dpad/down", format = "BIT", layout = "DiscreteButton", parameters = "minValue=3,maxValue=5", bit = 0, sizeInBits = 4)]
    [InputControl(name = "dpad/left", format = "BIT", layout = "DiscreteButton", parameters = "minValue=5, maxValue=7", bit = 0, sizeInBits = 4)]
    [InputControl(name = "buttonWest", displayName = "Square", bit = 4)]
    [InputControl(name = "buttonSouth", displayName = "Cross", bit = 5)]
    [InputControl(name = "buttonEast", displayName = "Circle", bit = 6)]
    [InputControl(name = "buttonNorth", displayName = "Triangle", bit = 7)]
    [FieldOffset(5)] public byte buttons1;

    [InputControl(name = "leftShoulder", bit = 0)]
    [InputControl(name = "rightShoulder", bit = 1)]
    [InputControl(name = "leftTriggerButton", layout = "Button", bit = 2)]
    [InputControl(name = "rightTriggerButton", layout = "Button", bit = 3)]
    [InputControl(name = "select", displayName = "Share", bit = 4)]
    [InputControl(name = "start", displayName = "Options", bit = 5)]
    [InputControl(name = "leftStickPress", bit = 6)]
    [InputControl(name = "rightStickPress", bit = 7)]
    [FieldOffset(6)] public byte buttons2;

    [InputControl(name = "systemButton", layout = "Button", displayName = "System", bit = 0)]
    [InputControl(name = "touchpadButton", layout = "Button", displayName = "Touchpad Press", bit = 1)]
    [FieldOffset(7)] public byte buttons3;

    [InputControl(name = "leftTrigger", format = "BYTE")]
    [FieldOffset(8)] public byte leftTrigger;

    [InputControl(name = "rightTrigger", format = "BYTE")]
    [FieldOffset(9)] public byte rightTrigger;

    [FieldOffset(30)] public byte batteryLevel;
}

```

### Create an InputDevice instance to represent your device

Next, you need an `InputDevice` to represent your device. Because you're dealing with a gamepad, you must create a new subclass of Gamepad.

For simplicity, this example ignores the fact that there is a `DualShockGamepad` class that the actual `DualShockGamepadHID` is based on.

```c#

// Using InputControlLayoutAttribute, we tell the system about the state
// struct we created, which includes where to find all the InputControl
// attributes that we placed on there. This is how the Input System knows
// what controls to create and how to configure them.
[InputControlLayout(stateType = typeof(DualShock4HIDInputReport)]
public DualShock4GamepadHID : Gamepad
{
}

```

### Register your device

The last step is to register your new type of device and set up the Input System so that when a PlayStation 4 controller is connected, the Input System generates your custom Device instead of using the default HID fallback.

This requires a call to [`InputSystem.RegisterLayout<T>`](xref:UnityEngine.InputSystem.InputSystem.RegisterLayout``1(System.String,System.Nullable{UnityEngine.InputSystem.Layouts.InputDeviceMatcher})), giving it an [`InputDeviceMatcher`](xref:UnityEngine.InputSystem.Layouts.InputDeviceMatcher) that matches the description for a PlayStation 4 DualShock HID. In theory, you can place this call anywhere, but the best point for registering layouts is generally during startup. Doing so ensures that your custom layout is visible to the Unity Editor and therefore exposed, for example, in the Input Control picker.

You can insert your registration into the startup sequence by modifying the code for your `DualShock4GamepadHID` device as follows:

```c#

[InputControlLayout(stateType = typeof(DualShock4HIDInputReport)]
#if UNITY_EDITOR
[InitializeOnLoad] // Make sure static constructor is called during startup.
#endif
public DualShock4GamepadHID : Gamepad
{
    static DualShock4GamepadHID()
    {
        // This is one way to match the device.
        InputSystem.RegisterLayout<DualShock4GamepadHID>(
            new InputDeviceMatcher()
                .WithInterface("HID")
                .WithManufacturer("Sony.+Entertainment")
                .WithProduct("Wireless Controller"));

        // Alternatively, you can also match by PID and VID, which is generally
        // more reliable for HIDs.
        InputSystem.RegisterLayout<DualShock4GamepadHID>(
            matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithCapability("vendorId", 0x54C) // Sony Entertainment.
                .WithCapability("productId", 0x9CC)); // Wireless controller.
    }

    // In the Player, to trigger the calling of the static constructor,
    // create an empty method annotated with RuntimeInitializeOnLoadMethod.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init() {}
}

```

Your custom layout now picks up any device that matches the manufacturer and product name strings, or the vendor and product IDs in its HID descriptor. The Input System now represents a `DualShock4GamepadHID` device instance.

For more information, refer to the [device matching](device-matching.md) documentation.
