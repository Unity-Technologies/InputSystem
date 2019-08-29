# HID Support

HID ("Human interface device") is a [specification](https://www.usb.org/hid) to describe peripheral user input devices connected to computers via USB or Bluetooth. HID is commonly used to implement devices such as Gamepads, Joysticks, Racing Wheels, etc.

HIDs (both via USB and via Bluetooth) are directly supported by the input system on Windows, MacOS, and UWP. On other platforms, HIDs may be supported but not delivered through HID-specific APIs (example: on Linux, gamepad and joystick HIDs are supported through SDL; other HIDs are not supported).

Every HID comes with a descriptor that describes the device. The descriptor of a HID can be browsed through from the input debugger by pressing the "HID Descriptor" button in the device debugger window. The HID descriptor specifies the type of the device (by reporting entry numbers in the [HID usage tables](https://www.usb.org/document-library/hid-usage-tables-112)), and a list of all controls on the device, along with their data ranges and usages.

![HID Descriptor](Images/HIDDescriptor.png)

HIDs are handled in one of two ways:

1. The system has a known layout for the specific HID.
2. A layout is auto-generated for the HID on the fly.

## Auto-generated layouts

By default, the input system will create layouts and device representations for any HID which reports it's usage as `GenericDesktop/Joystick`, `GenericDesktop/Gamepad` or `GenericDesktop/MultiAxisController` (see the [HID usage table specifications](https://www.usb.org/document-library/hid-usage-tables-112) for more info). You can override that behavior to support any other device by adding a handler to the [`HIDSupport.shouldCreateHID`](../api/UnityEngine.InputSystem.HID.HIDSupport.html#UnityEngine_InputSystem_HID_HIDSupport_shouldCreateHID) event.

Nor when the input system automatically create a layouts for a HID, these devices are currently always reported as [`Joysticks`](Joystick.md), represented by the [`Joystick` device class]((../api/UnityEngine.InputSystem.Joystick.html). The first elements with a reported HID usage of `GenericDesktop/X` and `GenericDesktop/Y` together form the joystick's [`stick`](../api/UnityEngine.InputSystem.Joystick.html#UnityEngine_InputSystem_Joystick_stick) control. Then controls are added for all further HID axis or button elements, using the control names reported by the HID specification (which tend to be rather generic). The first control with a HID usage of `Button/Button 1` will be assigned to the joystick's [`trigger`](../api/UnityEngine.InputSystem.Joystick.html#UnityEngine_InputSystem_Joystick_trigger) control.

The auto-generated layouts represent a "best effort" on the part of the input system. As the way HIDs describe themselves as per standard is too ambiguous in practice, generated layouts may lead to controls that do not work the way they should. For example, while the layout builder can identify hat switches and D-Pads, it can often only make guesses as to which direction represents which. The same goes for individual buttons which generally are not assigned any meaning in HID.

The best way to resolve the situation of HIDs not working as expected is to add a custom layout and thus by-pass auto-generation altogether. See [Overriding the HID Fallback](#overriding-the-hid-fallback) for details.

## HID Output

HIDs can support output, for instance to toggle lights or force feedback motors on a gamepad. Output is controlled by sending commands known as "HID Output Reports" to a device. Output reports use device-specific data formats. You can send a HID Output Reports to a device by calling [`InputDevice.ExecuteCommand`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_ExecuteCommand__1___0__) to send a command struct with the [`typeStatic`](../api/UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.html#properties) property set as `"HIDO""`. The command struct will then contain the device-specific data sent out to the HID.

## Overriding the HID Fallback

Often, when using the layouts auto-generated for HIDs, the result will be suboptimal. Controls will not receive proper names specific to the device, some controls may not work as expected, and some controls that use vendor-specific formats may not appear altogether.

The best way to deal with this is to set up a custom device layout specifically for your device. This will override the default auto-generation and give you full control over how the device is exposed.

For this demonstration, we pretend that the input system does not already have a custom layout for the PS4 DualShock controller and that we want to add such a layout.

We know that we want to expose the controller as a [`Gamepad`](Gamepad.md) and we roughly know the HID data format from various sources on the web. So, let's take it step by step from here.

>NOTE: In case you do __not__ know the format of a given HID you want to support, a good strategy can be to just open the input debugger with the device plugged in and pop up both the debugger view for the device as well as the window showing the HID descriptor. Then you can go through the controls one by one, see what happens in the debug view and correlate that to the controls in the HID descriptor. It can also be useful to double-click individual events and compare the raw data coming in from the device. If you select two events in the event trace, you can then right-click and choose "Compare" to get a view that shows only the differences between the two events.

### Step 1: The State Struct

First step is describing to the input system in detail the format that input data for the device comes in as well as describing the [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html) instances that should read out individual pieces of information from that data.

We know that the HID input reports we get from the PS4 controller roughly look like this:

```C++
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

Based on this, we can quite straightforwardly translate this into a C# struct:

```CSharp
// We receive data as raw HID input reports. Thus this struct
// describes the raw binary format of such a report.
[StructLayout(LayoutKind.Explicit, Size = 32)]
struct DualShock4HIDInputReport : IInputStateTypeInfo
{
    // All HID input reports are tagged with the 'HID ' FourCC.
    // Thus this is the format we need to use for this state struct.
    public FourCC format => new FourCC('H', 'I', 'D');

    // HID input reports may start with an 8-bit report ID. It depends on the device
    // whether this is present or not. On the PS4 DualShock controller, it is
    // present. We don't really need to add the field but let's do so for completeness
    // sake and it can also be helpful during debugging.
    [FieldOffset(0)] public byte reportId;

    // The InputControl annotations here probably look a little scary but what we do
    // here is relatively straightforward. The fields we add we annotate with
    // [FieldOffset] to force them to the right location and then we add InputControl
    // to attach controls to the fields. Each InputControl attribute does one of only
    // two things: either it adds a new control or it modifies an existing control.
    // Given that our layout is based on Gamepad, almost all the controls here are
    // inherited from Gamepad and we just modify settings on them.

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

Assembling that struct was quite involved. Luckily, this concludes the hard part. The rest is just some easy setup code.

### Step 2: The InputDevice

Next, we need an `InputDevice` to represent our device. Given that we are dealing with a gamepad, we do so by creating a new subclass of `Gamepad`.

>For simplicity's sake we ignore the fact here that there is a class `DualShockGamepad` that the actual `DualShockGamepadHID` is based on.

```CSharp
// Using InputControlLayoutAttribute, we tell the system about the state
// struct we created and thus also about where to find all the InputControl
// attributes that we placed on there. This is how the input system knows
// what controls to create and how to configure them.
[InputControlLayout(stateType = typeof(DualShock4HIDInputReport)]
public DualShock4GamepadHID : Gamepad
{
}
```

### Step 3: Registering the Device

The last step is to register our new type of device with the system and set things up such that when a PS4 controller is connected, it will get picked up by our custom device and not by the default HID fallback.

In essence, all this requires is a call to [`InputSystem.RegisterLayout<T>`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RegisterLayout__1_System_String_System_Nullable_UnityEngine_InputSystem_Layouts_InputDeviceMatcher__) and giving it an [`InputDeviceMatcher`](../api/UnityEngine.InputSystem.Layouts.InputDeviceMatcher.html) that matches the description for a PS4 DualShock HID. We can theoretically place this call anywhere but the best point for registering layouts is generally during startup. Doing so ensures that our custom layout is visible to the Unity editor and thus can be seen, for example, in the input control picker.

We can do insert our registration into the startup sequence by modifying the code for our `DualShock4GamepadHID` device slightly.

```CSharp
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

    // In the player, trigger the calling of our static constructor
    // by having an empty method annotated with RuntimeInitializeOnLoadMethod.
    [RuntimeInitializeOnLoad]
    static void Init() {}
}
```

Now, any device matching the manufacturer and product name strings or the vendor and product IDs in its HID descriptor will be picked up by our custom layout, and be represented in the system as a `DualShock4GamepadHID` device instance. Also check the documentation about [device matching](Devices.md#matching).
