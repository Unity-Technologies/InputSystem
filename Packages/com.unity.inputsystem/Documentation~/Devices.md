# Devices

* [Device Descriptions](#device-descriptions)
    * [Hijacking the Matching Process](#hijacking-the-matching-process)
* [Native Devices](#native-devices)
    * [Disconnected Devices](#disconnected-devices)
* [Device Creation](#device-creation)
* [Device IDs](#device-ids)
* [Device Usages](#device-usages)
* [Device Commands](#device-commands)
* [Working with Devices](#working-with-devices)
    * [Monitoring Devices](#monitoring-devices)
    * [Adding and Removing Devices](#adding-and-removing-devices)
    * [Modifying Existing Devices](#modifying-existing-devices)
    * [Creating Custom Devices](#creating-custom-devices)

Devices are the toplevel container for [controls](Controls.md). A device is itself a control.

The set of all currently present devices can be queried through `InputSystem.devices`.

## Device Descriptions

An `InputDescription` describes a device. This is primarily used during the device discovery process. When a new device is reported present (by the runtime or by the user), it is reported along with a device description. Based on the description, the system will then attempt to find a device layout that matches the description. This process is based on ["device matchers"](#matching).

After a device has been created, the description it was created from can be retrieved through the `InputDevice.description` property.

Every description has a set of standard fields:

|Field|Description|
|-----|-----------|
|`interfaceName`|Identifier for the interface/API that is making the device available. In many cases, this corresponds to the name of the platform but there are several more specific interfaces that are commonly used:<br><dl><dt><a href="https://www.usb.org/hid">HID</a></dt><dd>bar</dd><dt><a href="https://docs.microsoft.com/en-us/windows/desktop/inputdev/raw-input">RawInput</a></dt><dd></dd><dt><a href="https://docs.microsoft.com/en-us/windows/desktop/xinput/xinput-game-controller-apis-portal">XInput</a></dt><dd></dd><dl>This field is required.|
|`deviceClass`|A broad categorization of the device. For example, "Gamepad" or "Keyboard".|
|`product`|Name of the product as reported by the device/driver itself.|
|`manufacturer`|Name of the manufacturer as reported by the device/driver itself.|
|`version`|If available, provides the version of the driver or hardware for the device.|
|`serial`|If available, provides the serial number for the device.|
|`capabilities`|A string in JSON format describing device/interface-specific capabilities. See the [section on capabililities](#capabilities).|

### Capabilities

Aside from a number of standardized fields, such as `product` and `manufacturer`, a device description may contain a `capabilities` string in JSON format. This string is used to describe characteristics ...

### Matching

Matching an `InputDeviceDescription` to a registered layout is facilitated by `InputDeviceMatcher`. Each matcher loosely functions as a kind of regular expression. Each field can be independently matched with either a plain string or regular expression. Matching is case-insensitive. For a matcher to apply, all its individual expressions have to match.

Matchers can be added to any layout by calling `InputSystem.RegisterLayoutMatcher` or supplied when registering a layout.

```CSharp
// Register a new layout and supply a matcher for it.
InputSystem.RegisterLayoutMatcher<MyDevice>(
    matches: new InputDeviceMatcher()
        .WithInterface("HID")
        .WithProduct("MyDevice.*")
        .WithManufacturer("MyBrand");

// Register an alternate matcher for an already registered layout.
InputSystem.RegisterLayoutMatcher<MyDevice>(
    new InputDeviceMatcher()
        .WithInterface("HID")

```

If multiple matchers are matching the same `InputDeviceDescription`, the matcher that has the most

### Hijacking the Matching Process

It is possible to overrule the internal matching process from outside and thus select a different layout for a device than the system would normally choose. This also makes it possible to build new layouts on the fly.

TODO

## Native Devices

Devices that are reported by the runtime are considered "native". These devices come in through the `IInputRuntime.onDeviceDiscovered` callback which is handled internally by the input system. Externally, devices created this way can be told apart from others by them having their `InputDevice.native` property be true.

There are some special rules that apply to native devices:

* A native device will be remembered by the input system. This means that, for example, if at the time the device was reported the system has no matching layout but TODO

### Disconnected Devices

## Device IDs

Each device that is created will receive a unique, numeric ID. The ID can be accessed through `InputDevice.id`.

The IDs are managed by the runtime and allocated through `IInputRuntime.AllocateDeviceId`. The runtime itself does not keep a record about which ID corresponds to which device.

During a session of Unity, no ID will get used a second time. This means that even after removal from the system, a device will not ...

## Device Creation

Input devices are exclusively created through `InputDeviceBuilder`. This process is mostly internal.

TODO

## Device Usages

Like any `InputControl`, a device may have one or more usages associated with it. TODO

## Device Configuration

## Device Commands

While input events deliver data coming __from__ a device, commands are used to communicate in the opposite direction, i.e. to talk back at the device. This can be used for retrieving specific information from the device, for triggering functions on the device (such as rumble effects), or for a variety of other needs.

A command is send to a device through `InputDevice.ExecuteCommand<TCommand>` which, for native devices, will relay the command to `IInputRuntime.DeviceCommand`. It is possible to intercept/monitor device commands through `InputSystem.onDeviceCommand`.

Each device command TODO

### Sending Commands to Devices

### Adding Custom Device Comands

## Device State

Like any other type of [control](Controls.md#control-state), each device has a block of memory allocated to it which stores

### State Changes

State changes are usually initiated through [state events](Events.md#state-events) but can be

#### Monitoring State Changes

### Customizing State Management

Individual devices can alter the way their state is handled to some extent. This is useful for implementing logic such as accumulation-style controls (where each new value adds on top of the current value and then resets at the end of a frame; `<Pointer>/delta` is one such control) or

#### Synthesizing State

It can be desirable to make up new state from existing state. An example of such a use case is the `button` control that `Touchscreen` inherits from `Pointer`. Unlike for the mouse where this is a real button, for `Touchscreen` this is a synthetic control that does not correspond to actual data coming in from the device backend. Instead, the button is considered press if any touch is currently ongoing and released otherwise.

This can be achieved by using `InputState.Change` which allows feeding arbitrary state changes into the system without having to run them through the input event queue. The state changes will be directly and synchronously incorporated. State change [monitors](#monitoring-state-changes), however, will still trigger as expected.



## Working With Devices

### Monitoring Devices

In many situations, it is useful to know when new devices are added or when existing devices are removed. To be notified of such changes, use `InputSystem.onDeviceChange`.

```CSharp
InputSystem.onDeviceChange +=
    (device, change) =>
    {
        switch (change)
        {
            case InputDeviceChange.Added:
                Debug.Log("New device added: " + device);
                break;

            case InputDeviceChange.Removed:
                Debug.Log("Device removed: " + device);
                break;
        }
    };
```

Notifications through `InputSystem.onDeviceChange` are always delivered for a host of other device-related changes. See the API documentation for `InputDeviceChange` for more details (`////TODO: insert link here`).

### Adding and Removing Devices

Devices can be manually added and removed through the API.

TODO

### Modifying Existing Devices

TODO

-setting usage
-setting variant

### Creating Custom Devices

>NOTE: Here we will deal only with devices that have fixed layouts, i.e. with the case where we know exactly the specific model or models that we are dealing with. This is different from an interface such as HID where devices can describe themselves through the interface and may thus take a wide variety of forms. In this case, no fixed device layout will likely suffice. This more complicated case requires what in the input system is called a [Layout Builder](Layouts.md#layout-builders) which can build device layouts on the fly from information obtained at runtime.

The need to create a custom device generally arises in one of two ways:

1. You have an existing API that generates input and that you want to reflect into the input system.
2. You have a HID that is either ignored entirely by the input system or gets an auto-generated layout that does not work well enough for your needs.

In case 2), see [Overriding the HID Fallback](HID.md#overriding-the-hid-fallback) for details.

Here we will deal with case 1) where we want to create a new input device entirely from scratch and feed it input that we receive from a third-party API.

#### Step 1: The State Struct

The first step is to create a C# `struct` that represents the form in which input is received and stored and also describes the `InputControl` instances that should be created for the device in order to retrieve said state.

```CSharp
// A "state struct" describes the memory format used a device. Each device can
// receive and store memory in its custom format. InputControls are then connected
// the individual pieces of memory and read out values from them.
//
// In case it is important that the memory format matches 1:1 at the binary level
// to an external representation, it is generally advisable to use
// LayoutLind.Explicit.
[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct MyDeviceState : IInputStateTypeInfo
{
    // Every state format is tagged with a FourCC code that is used for type
    // checking. The characters can be anything. Choose something that allows
    // you do easily recognize memory belonging to your own device.
    public FourCC format => return new FourCC('M', 'Y', 'D', 'V');

    // InputControlAttributes on fields tell the input system to create controls
    // for the public fields found in the struct.

    // Assume a 16bit field of buttons. Create one button that is tied to
    // bit #3 (zero-based). Note that buttons do not need to be stored as bits.
    // They can also be stored as floats or shorts, for example. The
    // `InputControlAttribute.format` property determines which format the
    // data is stored in. If omitted, it is generally inferred from the value
    // type of the field.
    [InputControl(name = "button", layout = "Button", bit = 3)]
    public ushort buttons;

    // Create a floating-point axis. The name, if not supplied, is taken from
    // the field.
    [InputControl(layout = "Axis")]
    public short axis;
}
```

The `InputControlAttribute` annotations are used by the input system's layout mechanism to add controls to the layout of your device. For details, see the [documentation of the layout system](Layouts.md).

With the state struct in place, we now have a way to send input data to the input system and to store it there. The next thing we need is an `InputDevice` that uses our custom state struct and represents our custom device.

#### Step 2: The Device Class

Next, we need a class derived from one of the `InputDevice` base classes. We can either base our device directly on `InputDevice` or we can go and pick one of the more specific types of devices like `Gamepad`, for example.

Let's assume that our device isn't really like any of the existing device classes and thus derive directly from `InputDevice`.

```CSharp
// InputControlLayoutAttribute attribute is only necessary if you want
// to override default behavior that occurs when registering your device
// as a layout.
// The most common use of InputControlLayoutAttribute is to direct the system
// to a custom "state struct" through the `stateType` property. See below for details.
[InputControlLayout(displayName = "My Device", stateType = typeof(MyDeviceState))]
public class MyDevice : InputDevice
{
    // In the state struct, we added two controls that we now want to
    // surface on the device. This is for convenience only. The controls will
    // get added to the device either way. Exposing them as properties will
    // simply make it easier to get to the controls in code.

    public ButtonControl button { get; private set; }
    public AxisControl axis { get; private set; }

    // This method is called by the input system after the device has been
    // constructed but before it is added to the system. Here you can do
    // any last minute setup.
    protected override void FinishSetup()
    {
        base.FinishSetup();

        // NOTE: The controls are *created* by the input system automatically.
        //       This is why don't do `new` here but rather just look
        //       the controls up.
        button = GetChildControl<ButtonControl>("button");
        axis = GetChildControl<AxisControl>("axis");
    }
}
```

#### Step 3: The Update Method

By now, we have a device in place along with its associated state format, but if we now create the device like so

```CSharp
InputSystem.AddDevice<MyDevice>();
```

While this creates a fully set up device with our two controls on it, the device will simply sit there and not do anything. It will not receive input as right now, we don't yet have code that actually generates input. So, let's take care of that next.

Queuing input is easy. We can do that from pretty much anywhere (even from a thread) using `InputSystem.QueueStateEvent` or `InputSystem.QueueDeltaStateEvent`. For this demonstration, we will make use of `IInputUpdateCallbackReceiver` which, when implemented by any `InputDevice`, will add an `Update()` method that automatically gets called during `InputSystem.onBeforeUpdate` and thus can feed input events into the current input update.

>NOTE: To reemphasize, you don't need to do it this way. If you already have a place where input for your device becomes available, you can simply queue input events from there instead of using `IInputUpdateCallbackReceiver`.

```CSharp
public class MyDevice : InputDevice, IInputUpdateCallbackReceiver
{
    //...

    public void Update()
    {
        // In practice, this is where we would be reading out data from an external
        // API. Instead, here we just make up some (empty) input.
        var state = new MyDeviceState();
        InputSystem.QueueStateEvent(this, state);
    }
}
```

#### Step 4: Registration and Creation

Now we have a functioning device but there is not yet a place where the device will actually get added to the system. Also, because we are not yet registering our new type of device, we won't see it in the editor when, for example, creating bindings in the [action editor](ActionAssets.md#editing-action-assets).

One way to register out type of device with the system is to do some from within code that runs automatically as part of Unity starting up. To do so, we can modify the definition of `MyDevice` like so.

```CSharp
// Add the InitializeOnLoad attribute to automatically run the static
// constructor of the class after each C# domain load.
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class MyDevice : InputDevice, IInputUpdateCallbackReceiver
{
    //...

    static MyDevice()
    {
        // RegisterLayout() adds a "control layout" to the system.
        // These can be layouts for individual controls (like sticks)
        // or layouts for entire devices (which are themselves
        // control) like in our case.
        InputSystem.RegisterLayout<MyDevice>();
    }

    // We still need a way to also trigger execution of the static constructor
    // in the player. This can be achieved by adding the RuntimeInitializeOnLoadMethod
    // to an empty method.
    [RuntimeInitializeOnLoadMethod]
    private static void InitializeInPlayer() {}
}
```

This registers the device type with the system and we will see it in the control picker, for example. However, we still need a way to actually add an instance of the device when it is connected.

We could just call `InputSystem.AddDevice<MyDevice>()` somewhere but in a real-world setup, you will likely have to correlate the InputDevices you create with their identities in the third-party API. It may be tempting to do something like this

```CSharp
public class MyDevice : InputDevice, IInputUpdateCallbackReceiver
{
    //...

    // This will NOT work correctly!
    public ThirdPartyAPI.DeviceId externalId { get; set; }
}
```

and then set that on the device after calling `AddDevice<MyDevice>` but this will not work as expected. At least not in the editor.

The reason for this is rather technical in nature. The input system requires that devices can be created solely from their `InputDeviceDescription` in combination with the chosen layout (and layout variant). In addition to that, a fixed set of mutable per-device properties are supported such as device usages (i.e. `InputSystem.SetDeviceUsage` and related methods). This is what allows the system to easily re-create devices after domain reloads in the editor as well as to create replicas of remote devices when connecting to a player.

To comply with this requirement is actually rather simple. We simply cast that information provided by the third-party API into an `InputDeviceDescription` and then use what's referred to as an `InputDeviceMatcher` to match the description to our custom `MyDevice` layout.

Let's assume that the third-party API has two callbacks like this:

```CSharp
public static ThirdPartyAPI
{
    // Let's assume that the argument is a string that contains the
    // name of the device and no two devices will have the
    // same name in the external API.
    public static Action<string> deviceAdded;
    public static Action<string> deviceRemoved;
}
```

Now we can hook into those callbacks and create and destroy devices in response.

```CSharp
// For this demonstration, we use a MonoBehaviour with [ExecuteInEditMode]
// on it to run our setup code. Of course, you can do this many other ways.
[ExecuteInEditMode]
public class MyDeviceSupport : MonoBehaviour
{
    protected void OnEnable()
    {
        ThirdPartyAPI.deviceAdded += OnDeviceAdded;
        ThirdPartyAPI.deviceRemoved += OnDeviceRemoved;
    }

    protected void OnDisable()
    {
        ThirdPartyAPI.deviceAdded -= OnDeviceAdded;
        ThirdPartyAPI.deviceRemoved -= OnDeviceRemoved;
    }

    private void OnDeviceAdded(string name)
    {
        // Feed a description of the device into the system. In response, the
        // system will match it to the layouts it has and create a device.
        InputSystem.AddDevice(
            new InputDeviceDescription
            {
                interfaceName = "ThirdPartyAPI",
                product = name
            });
    }

    private void OnDeviceRemoved(string name)
    {
        var device = InputSystem.devices.FirstOrDefault(
            x => x.description == new InputDeviceDescription
            {
                interfaceName = "ThirdPartyAPI",
                product = name,
            });

        if (device != null)
            InputSystem.RemoveDevice(device);
    }

    // Let's also move the registration of MyDevice here from
    // the static constructor where we had it previously. Also,
    // we change the registration to also supply a matcher.
    protected void Awake()
    {
        // Add a match that catches any input device that reports its
        // interface as being "ThirdPartyAPI".
        InputSystem.RegisterLayout<MyDevice>(
            matches: new InputDeviceMatcher()
                .WithInterface("ThirdPartyAPI"));
    }
}
```

#### Step 5: `current` and `all` (Optional)

It can be very convenient to quickly access the last used device of a given type or to quickly list all devices of a specific type. We can do this by simply adding support for a `current` and for an `all` getter to the API of `MyDevice`.

```CSharp
public class MyDevice : InputDevice, IInputCallbackReceiver
{
    //...

    public static MyDevice current { get; private set; }

    public static IReadOnlyList<MyDevice> all => s_AllMyDevices;
    private static List<MyDevice> s_AllMyDevices = new List<MyDevice>();

    public override void MakeCurrent()
    {
        base.MakeCurrent();
        current = this;
    }

    protected override void OnAdded()
    {
        base.OnAdded();
        s_AllMyDevices.Add(this);
    }

    protected override void OnRemoved()
    {
        base.OnRemoved();
        s_AllMyDevices.Remove(this);
    }
}
```

#### Step 6: Device Commands (Optional)

A final, but optional, step is to add support for device commands. A "device command" is that opposite of input, i.e. it is data traveling __to__ the input device &ndash; and which may optionally also return data as part of the operation (much like a function call). This can be used to communicate with the backend of the device to query configuration or initiate effects such as haptics.

    ////TODO: ATM we're missing an overridable method to make this work
