using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

// The input system stores a chunk of memory for each device. What that
// memory looks like we can determine ourselves. The easiest way is to just describe
// it as a struct.
//
// Each chunk of memory is tagged with a "format" identifier in the form
// of a "FourCC" (a 32-bit code comprised of four characters). Using
// IInputStateTypeInfo we allow the system to get to the FourCC specific
// to our struct.
public struct CustomDeviceState : IInputStateTypeInfo
{
    // We use "CUST" here as our custom format code. It can be anything really.
    // Should be sufficiently unique to identify our memory format, though.
    public FourCC format => new FourCC('C', 'U', 'S', 'T');

    // Next we just define fields that store the state for our input device.
    // The only thing really interesting here is the [InputControl] attributes.
    // These automatically attach InputControls to the various memory bits that
    // we define.
    //
    // To get started, let's say that our device has a bitfield of buttons. Each
    // bit indicates whether a certain button is pressed or not. For the sake of
    // demonstration, let's say our device has 16 possible buttons. So, we define
    // a ushort field that contains the state of each possible button on the
    // device.
    //
    // On top of that, we need to tell the input system about each button. Both
    // what to call it and where to find it. The "name" property tells the input system
    // what to call the control; the "layout" property tells it what type of control
    // to create ("Button" in our case); and the "bit" property tells it which bit
    // in the bitfield corresponds to the button.
    //
    // We also tell the input system about "display names" here. These are names
    // that get displayed in the UI and such.
    [InputControl(name = "firstButton", layout = "Button", bit = 0, displayName = "First Button")]
    [InputControl(name = "secondButton", layout = "Button", bit = 1, displayName = "Second Button")]
    [InputControl(name = "thirdButton", layout = "Button", bit = 2, displayName = "Third Button")]
    public ushort buttons;

    // Let's say our device also has a stick. However, the stick isn't stored
    // simply as two floats but as two unsigned bytes with the midpoint of each
    // axis located at value 127. We can simply define two consecutive byte
    // fields to represent the stick and annotate them like so.
    //
    // First, let's introduce stick control itself. This one is simple. We don't
    // yet worry about X and Y individually as the stick as whole will itself read the
    // component values from those controls.
    [InputControl(name = "stick", layout = "Stick", displayName = "Main Stick")]
    // So that's what we need next. By default, both X and Y on "Stick" are floating-point
    // controls so here we need to individually configure them the way they work for our
    // stick.
    //
    // NOTE: We don't mention things as "layout" and such here. The reason is that we are
    //       modifying a control already defined by "Stick". This means that we only need
    //       to set the values that are different from what "Stick" stick itself already
    //       configures. And since "Stick" configures both "X" and "Y" to be "Axis" controls,
    //       we don't need to worry about that here.
    //
    // Using "format", we tell the controls how their data is stored. As bytes in our case
    // so we use "BYTE" (check the documentation for InputStateBlock for details on that).
    //
    // NOTE: We don't use "SBYT" (signed byte) here. Our values are not signed. They are
    //       unsigned. It's just that our "resting" (i.e. mid) point is at 127 and not at 0.
    //
    // And then, using the various "normalize" parameters, we tell the input system how to
    // deal with the fact that our midpoint is located smack in the middle of our value range.
    // Using "normalize" (which is equivalent to "normalize=true") we instruct the control
    // to normalize values. Using "normalizeZero=0.5", we tell it that our midpoint is located
    // at 0.5 (AxisControl will convert the BYTE value to a [0..1] floating-point value with
    // 0=0 and 255=1) and that our lower limit is "normalizeMin=0" and our upper limit is
    // "normalizeMax=1". Put another way, it will map [0..1] to [-1..1].
    [InputControl(name = "stick/x", format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
    public byte x;
    [InputControl(name = "stick/y", format = "BYTE", parameters = "normalize,normalizeMin=0,normalizeMax=1,normalizeZero=0.5")]
    public byte y;
}

// Now that we have the state struct all sorted out, we have a way to lay out the memory
// for our device and we have a way to map InputControls to pieces of that memory. What
// we're still missing, however, is a way to represent our device as a whole within the
// input system.
//
// For that, we start with a class derived from InputDevice. We could also base this
// on something like Mouse or Gamepad in case our device is an instance of one of those
// specific types but for this demonstration, let's assume our device is nothing like
// those devices (if we base our devices on those layouts, we have to correctly map the
// controls we inherit from those devices).
//
// Other than deriving from InputDevice, there are two other noteworthy things here.
//
// For one, we want to ensure that the call to InputSystem.RegisterLayout happens as
// part of startup. Doing so ensures that the layout is known to the input system and
// thus appears in the control picker. So we use [InitializeOnLoad] and [RuntimeInitializeOnLoadMethod]
// here to ensure initialization in both the editor and the player.
//
// Also, we use the [InputControlLayout] attribute here. This attribute is optional on
// types that are used as layouts in the input system. In our case, we have to use it
// to tell the input system about the state struct we are using to define the memory
// layout we are using and the controls tied to it.
#if UNITY_EDITOR
[InitializeOnLoad] // Call static class constructor in editor.
#endif
[InputControlLayout(stateType = typeof(CustomDeviceState))]
public class CustomDevice : InputDevice, IInputUpdateCallbackReceiver
{
    // [InitializeOnLoad] will ensure this gets called on every domain (re)load
    // in the editor.
    #if UNITY_EDITOR
    static CustomDevice()
    {
        // Trigger our RegisterLayout code in the editor.
        Initialize();
    }

    #endif

    // In the player, [RuntimeInitializeOnLoadMethod] will make sure our
    // initialization code gets called during startup.
    [RuntimeInitializeOnLoadMethod]
    private static void Initialize()
    {
        // Register our device with the input system. We also register
        // a "device matcher" here. These are used when a device is discovered
        // by the input system. Each device is described by an InputDeviceDescription
        // and an InputDeviceMatcher can be used to match specific properties of such
        // a description. See the documentation of InputDeviceMatcher for more
        // details.
        InputSystem.RegisterLayout<CustomDevice>(
            matches: new InputDeviceMatcher()
                .WithInterface("Custom"));
    }

    // While our device is fully functional at this point, we can refine the API
    // for it a little bit. One thing we can do is expose the controls for our
    // device directly. While anyone can look up our controls using strings, exposing
    // the controls as properties makes it simpler to work with the device in script.
    public ButtonControl firstButton { get; private set; }
    public ButtonControl secondButton { get; private set; }
    public ButtonControl thirdButton { get; private set; }
    public StickControl stick { get; private set; }

    // FinishSetup is where our device setup is finalized. Here we can look up
    // the controls that have been created.
    protected override void FinishSetup()
    {
        base.FinishSetup();

        firstButton = GetChildControl<ButtonControl>("firstButton");
        secondButton = GetChildControl<ButtonControl>("secondButton");
        thirdButton = GetChildControl<ButtonControl>("thirdButton");
        stick = GetChildControl<StickControl>("stick");
    }

    // We can also expose a '.current' getter equivalent to 'Gamepad.current'.
    // Whenever our device receives input, MakeCurrent() is called. So we can
    // simply update a '.current' getter based on that.
    public static CustomDevice current { get; private set; }
    public override void MakeCurrent()
    {
        base.MakeCurrent();
        current = this;
    }

    // When one of our custom devices is removed, we want to make sure that if
    // it is the '.current' device, we null out '.current'.
    protected override void OnRemoved()
    {
        base.OnRemoved();
        if (current == this)
            current = null;
    }

    // So, this is all great and nice. But we have one problem. No one is actually
    // creating an instance of our device yet. Which means that while we can bind
    // to controls on the device from actions all we want, at runtime we will never
    // actually receive input from our custom device. For that to happen, we need
    // to make sure that an instance of the device is created at some point.
    //
    // This one's a bit tricky. Because it really depends on how the device is
    // actually discovered in practice. In most real-world scenarios, there will be
    // some external API that notifies us when a device under its domain is added or
    // removed. In response, we would report a device being added (using
    // InputSystem.AddDevice(new InputDeviceDescription { ... }) or removed
    // (using DeviceRemoveEvent).
    //
    // In this demonstration, we don't have an external API to query. And we don't
    // really have another criteria by which to determine when a device of our custom
    // type should be added.
    //
    // So let's fake it here. Let's simply say that if there isn't an instance
    // of our device yet, we report one.


    // So, now we actually create an instance of CustomDevice. But... there's no
    // input on it coming through. This is because no one is actually queuing events
    // for the device. ...
    public void OnUpdate()
    {
    }
}
