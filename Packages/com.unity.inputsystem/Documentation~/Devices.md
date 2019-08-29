# Devices

* [Device Descriptions](#device-descriptions)
    * [Hijacking the Matching Process](#hijacking-the-matching-process)
* [Native Devices](#native-devices)
    * [Disconnected Devices](#disconnected-devices)
* [Device IDs](#device-ids)
* [Device Usages](#device-usages)
* [Device Commands](#device-commands)
* [Working with Devices](#working-with-devices)
    * [Monitoring Devices](#monitoring-devices)
    * [Adding and Removing Devices](#adding-and-removing-devices)
    * [Creating Custom Devices](#creating-custom-devices)

Logically, devices are the top-level container for [controls](Controls.md). An [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) is itself a specialization of [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html). Physically, devices represent input devices attached to the computer, which can be used to control the content. See [supported devices](SupportedDevices.md) to see what kind of devices the input system currently supports.

The set of all currently present devices can be queried through [`InputSystem.devices`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_devices).

## Device Descriptions

An [`InputDeviceDescription`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html) describes a device. This is primarily used during the device discovery process. When a new device is reported present (by the runtime or by the user), it is reported along with a device description. Based on the description, the system will then attempt to find a device [layout](Layouts.md) that matches the description. This process is based on ["device matchers"](#matching).

After a device has been created, the description it was created from can be retrieved through the [`InputDevice.description`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_description) property.

Every description has a set of standard fields:

|Field|Description|
|-----|-----------|
|[`interfaceName`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_interfaceName)|Identifier for the interface/API that is making the device available. In many cases, this corresponds to the name of the platform but there are several more specific interfaces that are commonly used:<br><dl><dt><a href="https://www.usb.org/hid">HID</a></dt><dd>bar</dd><dt><a href="https://docs.microsoft.com/en-us/windows/desktop/inputdev/raw-input">RawInput</a></dt><dd></dd><dt><a href="https://docs.microsoft.com/en-us/windows/desktop/xinput/xinput-game-controller-apis-portal">XInput</a></dt><dd></dd><dl>This field is required.|
|[`deviceClass`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_deviceClass)|A broad categorization of the device. For example, "Gamepad" or "Keyboard".|
|[`product`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_product)|Name of the product as reported by the device/driver itself.|
|[`manufacturer`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_manufacturer)|Name of the manufacturer as reported by the device/driver itself.|
|[`version`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_version)|If available, provides the version of the driver or hardware for the device.|
|[`serial`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_serial)|If available, provides the serial number for the device.|
|[`capabilities`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_capabilities)|A string in JSON format describing device/interface-specific capabilities. See the [section on capabililities](#capabilities).|

### Capabilities

Aside from a number of standardized fields, such as `product` and `manufacturer`, a device description may contain a [`capabilities`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_capabilities) string in JSON format. This string is used to describe characteristics which help the input system with interpreting the data coming from a device and with mapping it to control representations. Not all device interfaces will report device capabilities. Examples of interface-specific device capabilities are [HID descriptors](HID.md). WebGL, Android and Linux use similar mechanisms to report available controls on connected gamepads.

### Matching

Matching an [`InputDeviceDescription`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html) to a registered layout is facilitated by [`InputDeviceMatcher`](../api/UnityEngine.InputSystem.Layouts.InputDeviceMatcher.html). Each matcher loosely functions as a kind of regular expression. Each field can be independently matched with either a plain string or regular expression. Matching is case-insensitive. For a matcher to apply, all its individual expressions have to match.

Matchers can be added to any layout by calling [`InputSystem.RegisterLayoutMatcher`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RegisterLayoutMatcher_System_String_UnityEngine_InputSystem_Layouts_InputDeviceMatcher_) or supplied when registering a layout.

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

If multiple matchers are matching the same [`InputDeviceDescription`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html), the matcher that has the larger number of properties to match against will be chosen to pick the layout for the device.

### Hijacking the Matching Process

It is possible to overrule the internal matching process from outside and thus select a different layout for a device than the system would normally choose. This also makes it possible to build new layouts on the fly. To do this, add a custom handler to the  [`InputSystem.onFindControlLayoutForDevice`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onFindLayoutForDevice) event. If your handler returns a non-null layout string, then this layout will be used.

## Native Devices

Devices that are reported by the runtime are considered "native". These devices come in through the `IInputRuntime.onDeviceDiscovered` callback which is handled internally by the input system. Externally, devices created this way can be told apart from others by them having their [`InputDevice.native`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_native) property be true.

A native device will be remembered by the input system. This means that, for example, if at the time the device was reported the system has no matching layout but a layout is registered later which matches the device, the device will be re-created using this layout.

### Disconnected Devices

If you want to get notified when input devices get disconnected, subscribe to the [`InputSystem.onDeviceChange`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceChange) event, and look for events of type [`InputDeviceChange.Disconnected`](../api/UnityEngine.InputSystem.InputDeviceChange.html).

The input system keeps track of disconnected devices in [`InputSystem.disconnectedDevices`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_disconnectedDevices). If one of these devices gets reconnected later, the input system can detect that the device was connected before, and will reuse it's [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) instance. This allows things like the [`PlayerInputManager`](Components.md) knowing to reassigning the device to the same [user](UserManagement.md) again.

## Device IDs

Each device that is created will receive a unique, numeric ID. The ID can be accessed through [`InputDevice.id`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_id).

The IDs are managed by the runtime and allocated through `IInputRuntime.AllocateDeviceId`. The runtime itself does not keep a record about which ID corresponds to which device.

During a session of Unity, no ID will get used a second time.

## Device Usages

Like any [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html), a device may have one or more usages associated with it. Usages can be queried with the [`usages`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_usages) property, and they can be set using [`InputSystem.SetDeviceUsage()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_SetDeviceUsage_UnityEngine_InputSystem_InputDevice_System_String_). Usages can be arbitrary strings with arbitrary meanings. One common case where the input system assigns devices usages is the handedness of XR Controllers, which are tagged with the "LeftHand" or "RightHand" usages.

## Device Commands

While input [events](Events.md) deliver data coming __from__ a device, commands are used to communicate in the opposite direction, i.e. to talk back at the device. This can be used for retrieving specific information from the device, for triggering functions on the device (such as rumble effects), or for a variety of other needs.

### Sending Commands to Devices

A command is send to a device through [`InputDevice.ExecuteCommand<TCommand>`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_ExecuteCommand__1___0__) which, for native devices, will relay the command to `IInputRuntime.DeviceCommand`. It is possible to intercept/monitor device commands through [`InputSystem.onDeviceCommand`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceCommand).

Each device command implements the [`IInputDeviceCommandInfo`](../api/UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.html) interface, which only requires implementing the [`typeStatic`](../api/UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.html#UnityEngine_InputSystem_LowLevel_IInputDeviceCommandInfo_typeStatic) property, which identifies the type of the command. The native implementation of the device should then understand how to handle that command. One common case is they `"HIDO"` command type which is used to send [HID output reports](HID.md#hid-output) to HIDs.

### Adding Custom Device Comands

To create custom device commands (for instance to support some functionality for a specific HID), create a `struct` containing all the data to be sent to the device, and make that struct implement the [`IInputDeviceCommandInfo`](../api/UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.html) interface by adding a [`typeStatic`](../api/UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.html#UnityEngine_InputSystem_LowLevel_IInputDeviceCommandInfo_typeStatic) property (which should return `"HIDO"` to send data to a HID).

You can then create an instance of this struct and populate all it's fields, and send it to the device using [`InputDevice.ExecuteCommand<TCommand>`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_ExecuteCommand__1___0__). The data layout of the struct must match the native representation of the data understood by the device.

## Device State

Like any other type of [control](Controls.md#control-state), each device has a block of memory allocated to it which stores the state of all the controls associated with the device.

### State Changes

State changes are usually initiated through [state events](Events.md#state-events) coming from the native backend, but you can overwrite the state of any control manually using [`InputControl<>.WriteValueIntoState()`](../api/UnityEngine.InputSystem.InputControl-1.html#UnityEngine_InputSystem_InputControl_1_WriteValueIntoState__0_System_Void__).

#### Monitoring State Changes

You can use [`InputState.AddChangeMonitor()`](../api/UnityEngine.InputSystem.LowLevel.InputState.html#UnityEngine_InputSystem_LowLevel_InputState_AddChangeMonitor_UnityEngine_InputSystem_InputControl_System_Action_UnityEngine_InputSystem_InputControl_System_Double_UnityEngine_InputSystem_LowLevel_InputEventPtr_System_Int64__System_Int32_System_Action_UnityEngine_InputSystem_InputControl_System_Double_System_Int64_System_Int32__) to register a callback to be called whenever the state of a control changes. The input system uses the same mechanism to implement [input actions](Actions.md).

#### Synthesizing State

It can be desirable to make up new state from existing state. An example of such a use case is the [`press`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_press) control that [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) inherits from [`Pointer`](../api/UnityEngine.InputSystem.Pointer.html). Unlike for the mouse where this is a real button, for [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) this is a [synthetic control](Controls.md#synthetic-controls) that does not correspond to actual data coming in from the device backend. Instead, the button is considered press if any touch is currently ongoing and released otherwise.

This can be achieved by using [`InputState.Change`](../api/UnityEngine.InputSystem.LowLevel.InputState.html#UnityEngine_InputSystem_LowLevel_InputState_Change__1_UnityEngine_InputSystem_InputControl___0_UnityEngine_InputSystem_LowLevel_InputUpdateType_UnityEngine_InputSystem_LowLevel_InputEventPtr_) which allows feeding arbitrary state changes into the system without having to run them through the input event queue. The state changes will be directly and synchronously incorporated. State change [monitors](#monitoring-state-changes), however, will still trigger as expected.

## Working With Devices

### Monitoring Devices

In many situations, it is useful to know when new devices are added or when existing devices are removed. To be notified of such changes, use [`InputSystem.onDeviceChange`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceChange).

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

Notifications through [`InputSystem.onDeviceChange`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceChange) are always delivered for a host of other device-related changes. See the [`InputDeviceChange` enum](../api/UnityEngine.InputSystem.InputDeviceChange.html) for more details.

### Adding and Removing Devices

Devices can be manually added and removed through the API, using [`InputSystem.AddDevice()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_AddDevice_UnityEngine_InputSystem_InputDevice_) and [`InputSystem.RemoveDevice()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RemoveDevice_UnityEngine_InputSystem_InputDevice_).

This allows you to create your own devices, which can be useful for testing purposes, or for creating virtual input devices which synthesize input from other events. An example for this are the [on-screen controls](OnScreen.md) offered by the input system (while these are provided by the input system, the input devices used for on-screen controls are completely created in code and have no [native representation](#native-devices)).

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

The [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html) annotations are used by the input system's layout mechanism to add controls to the layout of your device. For details, see the [documentation of the layout system](Layouts.md).

With the state struct in place, we now have a way to send input data to the input system and to store it there. The next thing we need is an [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) that uses our custom state struct and represents our custom device.

#### Step 2: The Device Class

Next, we need a class derived from one of the [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) base classes. We can either base our device directly on [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) or we can go and pick one of the more specific types of devices like [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html), for example.

Let's assume that our device isn't really like any of the existing device classes and thus derive directly from [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html).

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

Queuing input is easy. We can do that from pretty much anywhere (even from a thread) using [`InputSystem.QueueStateEvent`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_QueueStateEvent__1_UnityEngine_InputSystem_InputDevice___0_System_Double_) or [`InputSystem.QueueDeltaStateEvent`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_QueueDeltaStateEvent__1_UnityEngine_InputSystem_InputControl___0_System_Double_). For this demonstration, we will make use of [`IInputUpdateCallbackReceiver`](../api/UnityEngine.InputSystem.IInputUpdateCallbackReceiver.html) which, when implemented by any [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html), will add an [`OnUpdate()`](../api/UnityEngine.InputSystem.IInputUpdateCallbackReceiver.html#UnityEngine_InputSystem_IInputUpdateCallbackReceiver_OnUpdate) method that automatically gets called during [`InputSystem.onBeforeUpdate`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onBeforeUpdate) and thus can feed input events into the current input update.

>NOTE: To reemphasize, you don't need to do it this way. If you already have a place where input for your device becomes available, you can simply queue input events from there instead of using [`IInputUpdateCallbackReceiver`](../api/UnityEngine.InputSystem.IInputUpdateCallbackReceiver.html).

```CSharp
public class MyDevice : InputDevice, IInputUpdateCallbackReceiver
{
    //...

    public void OnUpdate()
    {
        // In practice, this is where we would be reading out data from an external
        // API. Instead, here we just make up some (empty) input.
        var state = new MyDeviceState();
        InputSystem.QueueStateEvent(this, state);
    }
}
```

#### Step 4: Registration and Creation

Now we have a functioning device but there is not yet a place where the device will actually get added to the system. Also, because we are not yet registering our new type of device, we won't see it in the editor when, for example, creating bindings in the [action editor](ActionAssets.md#editing-input-action-assets).

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

We could just call [`InputSystem.AddDevice<MyDevice>()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_AddDevice__1_System_String_) somewhere but in a real-world setup, you will likely have to correlate the InputDevices you create with their identities in the third-party API. It may be tempting to do something like this

```CSharp
public class MyDevice : InputDevice, IInputUpdateCallbackReceiver
{
    //...

    // This will NOT work correctly!
    public ThirdPartyAPI.DeviceId externalId { get; set; }
}
```

and then set that on the device after calling [`AddDevice<MyDevice>`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_AddDevice__1_System_String_) but this will not work as expected. At least not in the editor.

The reason for this is rather technical in nature. The input system requires that devices can be created solely from their [`InputDeviceDescription`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html) in combination with the chosen layout (and layout variant). In addition to that, a fixed set of mutable per-device properties are supported such as device usages (i.e. [`InputSystem.SetDeviceUsage()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_SetDeviceUsage_UnityEngine_InputSystem_InputDevice_System_String_) and related methods). This is what allows the system to easily re-create devices after domain reloads in the editor as well as to create replicas of remote devices when connecting to a player.

To comply with this requirement is actually rather simple. We simply cast that information provided by the third-party API into an [`InputDeviceDescription`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html) and then use what's referred to as an [`InputDeviceMatcher`](../api/UnityEngine.InputSystem.Layouts.InputDeviceMatcher.html) to match the description to our custom `MyDevice` layout.

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
[//]: # (#### Step 6: Device Commands (Optional))
[//]: # (A final, but optional, step is to add support for device commands. A "device command" is that opposite of input, i.e. it is data traveling __to__ the input device &ndash; and which may optionally also return data as part of the operation (much like a function call). This can be used to communicate with the backend of the device to query configuration or initiate effects such as haptics.)
[//]: # (TODO: ATM we're missing an overridable method to make this work)
