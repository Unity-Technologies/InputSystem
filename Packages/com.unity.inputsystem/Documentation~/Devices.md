# Devices

* [Device descriptions](#device-descriptions)
    * [Hijacking the matching process](#hijacking-the-matching-process)
* [Native Devices](#native-devices)
    * [Disconnected Devices](#disconnected-devices)
* [Device IDs](#device-ids)
* [Device usages](#device-usages)
* [Device commands](#device-commands)
* [Working with Devices](#working-with-devices)
    * [Monitoring Devices](#monitoring-devices)
    * [Adding and removing Devices](#adding-and-removing-devices)
    * [Creating custom Devices](#creating-custom-devices)

Logically, Input Devices are the top-level container for [Controls](Controls.md). The [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) class is itself a specialization of [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html). Physically, Input Devices represent devices attached to the computer, which can be used to control the app. See [supported Devices](SupportedDevices.md) to see what kind of Devices the Input System currently supports.

You can query the set of all currently present Devices using [`InputSystem.devices`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_devices).

## Device descriptions

An [`InputDeviceDescription`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html) describes a Device. The Input System uses this primarily during the Device discovery process. When a new Device is reported (by the runtime or by the user), the report contains a Device description. Based on the description, the system then attempts to find a Device [layout](Layouts.md) that matches the description. This process is based on [Device matchers](#matching).

After a Device has been created, you can retrieve the description it was created from through the [`InputDevice.description`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_description) property.

Every description has a set of standard fields:

|Field|Description|
|-----|-----------|
|[`interfaceName`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_interfaceName)|Identifier for the interface/API that is making the Device available. In many cases, this corresponds to the name of the platform, but there are several more specific interfaces that are commonly used: [HID](https://www.usb.org/hid), [RawInput](https://docs.microsoft.com/en-us/windows/desktop/inputdev/raw-input), [XInput](https://docs.microsoft.com/en-us/windows/desktop/xinput/xinput-game-controller-apis-portal).<br>This field is required.|
|[`deviceClass`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_deviceClass)|A broad categorization of the Device. For example, "Gamepad" or "Keyboard".|
|[`product`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_product)|Name of the product as reported by the Device/driver itself.|
|[`manufacturer`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_manufacturer)|Name of the manufacturer as reported by the Device/driver itself.|
|[`version`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_version)|If available, provides the version of the driver or hardware for the Device.|
|[`serial`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_serial)|If available, provides the serial number for the Device.|
|[`capabilities`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_capabilities)|A string in JSON format describing Device/interface-specific capabilities. See the [section on capabililities](#capabilities).|

### Capabilities

Aside from a number of standardized fields, such as `product` and `manufacturer`, a Device description can contain a [`capabilities`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html#UnityEngine_InputSystem_Layouts_InputDeviceDescription_capabilities) string in JSON format. This string describes characteristics which help the Input System with interpreting the data coming from a Device, and mapping it to Control representations. Not all Device interfaces will report Device capabilities. Examples of interface-specific Device capabilities are [HID descriptors](HID.md). WebGL, Android and Linux use similar mechanisms to report available Controls on connected gamepads.

### Matching

[`InputDeviceMatcher`](../api/UnityEngine.InputSystem.Layouts.InputDeviceMatcher.html)  instances handle matching an [`InputDeviceDescription`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html) to a registered layout. Each matcher loosely functions as a kind of regular expression. Each field in the description can be independently matched with either a plain string or regular expression. Matching is case-insensitive. For a matcher to apply, all its individual expressions have to match.

Matchers can be added to any layout by calling [`InputSystem.RegisterLayoutMatcher`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RegisterLayoutMatcher_System_String_UnityEngine_InputSystem_Layouts_InputDeviceMatcher_). You can also supply them when registering a layout.

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

If multiple matchers are matching the same [`InputDeviceDescription`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html), the Input System chooses the matcher that has the larger number of properties to match against.

### Hijacking the matching process

You can overrule the internal matching process from outside and thus select a different layout for a Device than the system would normally choose. This also makes it possible to build new layouts on the fly. To do this, add a custom handler to the  [`InputSystem.onFindControlLayoutForDevice`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onFindLayoutForDevice) event. If your handler returns a non-null layout string, then the Input System will use this layout.

## Native Devices

Devices that the [native backend](Architecture.md#native-backend) reports are considered native (as opposed to Devices created from script code). You can identify these Devices by checking the [`InputDevice.native`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_native) property.

The Input System remembers native Devices. For example, if the system has no matching layout when the Device is first reported, but a layout which matches the device is registered later, the system recreates the Device using this layout.

### Disconnected Devices

If you want to get notified when Input Devices get disconnected, subscribe to the [`InputSystem.onDeviceChange`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceChange) event, and look for events of type [`InputDeviceChange.Disconnected`](../api/UnityEngine.InputSystem.InputDeviceChange.html).

The Input System keeps track of disconnected Devices in [`InputSystem.disconnectedDevices`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_disconnectedDevices). If one of these Devices gets reconnected later, the Input System can detect that the Device was connected before, and will reuse its [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) instance. This allows the [`PlayerInputManager`](Components.md) to reassign the Device to the same [user](UserManagement.md) again.

## Device IDs

Each Device that is created will receive a unique numeric ID. You can access this ID through [`InputDevice.deviceId`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_deviceId).

All IDs are only used once per Unity session.

## Device usages

Like any [`InputControl`](../api/UnityEngine.InputSystem.InputControl.html), a Device can have usages associated with it. You can query usages with the [`usages`](../api/UnityEngine.InputSystem.InputControl.html#UnityEngine_InputSystem_InputControl_usages) property, and set them using [`InputSystem.SetDeviceUsage()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_SetDeviceUsage_UnityEngine_InputSystem_InputDevice_System_String_). Usages can be arbitrary strings with arbitrary meanings. One common case where the Input System assigns Devices usages is the handedness of XR controllers, which are tagged with the "LeftHand" or "RightHand" usages.

## Device commands

While input [events](Events.md) deliver data coming from a Device, commands send data back to the Device. This is used for retrieving specific information from the Device, for triggering functions on the Device (such as rumble effects), or for a variety of other needs.

### Sending commands to Devices

A command is send to a Device through [`InputDevice.ExecuteCommand<TCommand>`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_ExecuteCommand__1___0__). To monitor Device commands, use [`InputSystem.onDeviceCommand`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceCommand).

Each Device command implements the [`IInputDeviceCommandInfo`](../api/UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.html) interface, which only requires implementing the [`typeStatic`](../api/UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.html#UnityEngine_InputSystem_LowLevel_IInputDeviceCommandInfo_typeStatic) property to identify the type of the command. The native implementation of the Device should then understand how to handle that command. One common case is the `"HIDO"` command type which is used to send [HID output reports](HID.md#hid-output) to HIDs.

### Adding custom device Comands

To create custom Device commands (for instance to support some functionality for a specific HID), create a `struct` containing all the data to be sent to the Device, and make that struct implement the [`IInputDeviceCommandInfo`](../api/UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.html) interface by adding a [`typeStatic`](../api/UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.html#UnityEngine_InputSystem_LowLevel_IInputDeviceCommandInfo_typeStatic) property. To send data to a HID, this property should return `"HIDO"`.

You can then create an instance of this struct and populate all its fields, and send it to the Device using [`InputDevice.ExecuteCommand<TCommand>`](../api/UnityEngine.InputSystem.InputDevice.html#UnityEngine_InputSystem_InputDevice_ExecuteCommand__1___0__). The data layout of the struct must match the native representation of the data as the device interprets it.

## Device state

Like any other type of [Control](Controls.md#control-state), each Device has a block of memory allocated to it which stores the state of all the Controls associated with the Device.

### State changes

State changes are usually initiated through [state events](Events.md#state-events) coming from the native backend, but you can manually overwrite the state of any Control using [`InputControl<>.WriteValueIntoState()`](../api/UnityEngine.InputSystem.InputControl-1.html#UnityEngine_InputSystem_InputControl_1_WriteValueIntoState__0_System_Void__).

#### Monitoring state changes

You can use [`InputState.AddChangeMonitor()`](../api/UnityEngine.InputSystem.LowLevel.InputState.html#UnityEngine_InputSystem_LowLevel_InputState_AddChangeMonitor_UnityEngine_InputSystem_InputControl_System_Action_UnityEngine_InputSystem_InputControl_System_Double_UnityEngine_InputSystem_LowLevel_InputEventPtr_System_Int64__System_Int32_System_Action_UnityEngine_InputSystem_InputControl_System_Double_System_Int64_System_Int32__) to register a callback to be called whenever the state of a Control changes. The Input System uses the same mechanism to implement [input Actions](Actions.md).

#### Synthesizing state

The Input System can synthesize new state from existing state. An example of such synthesized state is the [`press`](../api/UnityEngine.InputSystem.Pointer.html#UnityEngine_InputSystem_Pointer_press) button  Control that [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) inherits from [`Pointer`](../api/UnityEngine.InputSystem.Pointer.html). Unlike a mouse, which has a physical button, for [`Touchscreen`](../api/UnityEngine.InputSystem.Touchscreen.html) this is a [synthetic Control](Controls.md#synthetic-controls) that doesn't correspond to actual data coming in from the Device backend. Instead, the Input System considers the button to be pressed if any touch is currently ongoing, and released otherwise.

This is achieved by using [`InputState.Change`](../api/UnityEngine.InputSystem.LowLevel.InputState.html#UnityEngine_InputSystem_LowLevel_InputState_Change__1_UnityEngine_InputSystem_InputControl___0_UnityEngine_InputSystem_LowLevel_InputUpdateType_UnityEngine_InputSystem_LowLevel_InputEventPtr_), which allows feeding arbitrary state changes into the system without having to run them through the input event queue. The Input System incorporates state changes directly and synchronously. State change [monitors](#monitoring-state-changes), still trigger as expected.

## Working With Devices

### Monitoring Devices

In many situations, it is useful to know when new Devices are added or existing Devices are removed. To be notified of such changes, use [`InputSystem.onDeviceChange`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceChange).

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

[`InputSystem.onDeviceChange`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onDeviceChange) delivers notifications for other device-related changes as well. See the [`InputDeviceChange` enum](../api/UnityEngine.InputSystem.InputDeviceChange.html) for more information.

### Adding and removing Devices

You can manually add and remove Devices through the API, using [`InputSystem.AddDevice()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_AddDevice_UnityEngine_InputSystem_InputDevice_) and [`InputSystem.RemoveDevice()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RemoveDevice_UnityEngine_InputSystem_InputDevice_).

This allows you to create your own Devices, which can be useful for testing purposes, or for creating virtual Input Devices which synthesize input from other events. An example for this are the [on-screen Controls](OnScreen.md) provided by the Input System. The Input Devices used for on-screen Controls are created entirely in code and have no [native representation](#native-devices).

### Creating custom Devices

>__Note__: This example deals only with Devices that have fixed layouts,that is, you know the specific model or models that you want to implement. This is different from an interface such as HID, where Devices can describe themselves through the interface and take on a wide variety of forms. A fixed Device layout can't cover self-describing Devices, so you need to use a [layout builder](Layouts.md#layout-builders) to build Device layouts on the fly from information you obtain at run time.

There are two main situations in which you might need to create a custom Device:

1. You have an existing API that generates input and that you want to reflect into the Input System.
2. You have a HID that is either ignored entirely by the Input System, or gets an auto-generated layout that does not work well enough for your needs.

For the second scenario, see [Overriding the HID Fallback](HID.md#overriding-the-hid-fallback).

The steps below deal with the first scenario, where you want to create a new Input Device entirely from scratch and feed it input from a third-party API.

#### Step 1: The state struct

The first step is to create a C# `struct` that represents the form in which the system receives and stores input, and also describes the `InputControl` instances that should be created for the Device in order to retrieve said state.

```CSharp
// A "state struct" describes the memory format used by a Device. Each Device can
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
    // you do easily recognize memory belonging to your own Device.
    public FourCC format => return new FourCC('M', 'Y', 'D', 'V');

    // InputControlAttributes on fields tell the Input System to create Controls
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

The Input System's layout mechanism uses [`InputControlAttribute`](../api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html) annotations to add Controls to the layout of your Device. For details, see the [layout system](Layouts.md) documentation.

With the state struct in place, you now have a way to send input data to the Input System and to store it there. The next thing you need is an [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) that uses your custom state struct and represents your custom Device.

#### Step 2: The Device class

Next, you need a class derived from one of the [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) base classes. You can either base your Device directly on [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html) or you can pick a more specific Device types like [`Gamepad`](../api/UnityEngine.InputSystem.Gamepad.html).

Let's assume that your Device doesn't fit into any of the existing Device classes and thus derive directly from [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html).

```CSharp
// InputControlLayoutAttribute attribute is only necessary if you want
// to override default behavior that occurs when registering your Device
// as a layout.
// The most common use of InputControlLayoutAttribute is to direct the system
// to a custom "state struct" through the `stateType` property. See below for details.
[InputControlLayout(displayName = "My Device", stateType = typeof(MyDeviceState))]
public class MyDevice : InputDevice
{
    // In the state struct, we added two Controls that we now want to
    // surface on the Device. This is for convenience only. The Controls will
    // get added to the Device either way. Exposing them as properties will
    // simply make it easier to get to the Controls in code.

    public ButtonControl button { get; private set; }
    public AxisControl axis { get; private set; }

    // This method is called by the Input System after the Device has been
    // constructed but before it is added to the system. Here you can do
    // any last minute setup.
    protected override void FinishSetup()
    {
        base.FinishSetup();

        // NOTE: The Controls are *created* by the Input System automatically.
        //       This is why don't do `new` here but rather just look
        //       the Controls up.
        button = GetChildControl<ButtonControl>("button");
        axis = GetChildControl<AxisControl>("axis");
    }
}
```

#### Step 3: The Update method

You now have a Device in place along with its associated state format. You can call the following method to create a fully set up Device with your two Controls on it:

```CSharp
InputSystem.AddDevice<MyDevice>();
```

However, this Device doesn't receive input yet, because you haven't added any code that actually generates input. To do that, you can use [`InputSystem.QueueStateEvent`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_QueueStateEvent__1_UnityEngine_InputSystem_InputDevice___0_System_Double_) or [`InputSystem.QueueDeltaStateEvent`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_QueueDeltaStateEvent__1_UnityEngine_InputSystem_InputControl___0_System_Double_) from virtually anywhere, including from a thread. The following example uses [`IInputUpdateCallbackReceiver`](../api/UnityEngine.InputSystem.LowLevel.IInputUpdateCallbackReceiver.html) which, when implemented by any [`InputDevice`](../api/UnityEngine.InputSystem.InputDevice.html), adds an [`OnUpdate()`](../api/UnityEngine.InputSystem.LowLevel.IInputUpdateCallbackReceiver.html#UnityEngine_InputSystem_LowLevel_IInputUpdateCallbackReceiver_OnUpdate) method that automatically gets called during [`InputSystem.onBeforeUpdate`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_onBeforeUpdate) and feeds input events into the current input update.

>__Note__: If you already have a place where input for your device becomes available, you can skip this step and queue input events from there instead of using [`IInputUpdateCallbackReceiver`](../api/UnityEngine.InputSystem.LowLevel.IInputUpdateCallbackReceiver.html).

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

#### Step 4: Device registration and creation

You now have a functioning device, but you haven't added it to the system yet. Also, because the device hasn't been registered, you won't see it in the editor when, for example, you create bindings in the [action editor](ActionAssets.md#editing-input-action-assets).

You can register your device type with the system from within the code that runs automatically as part of Unity's startup. To do so, modify the definition of `MyDevice` like so:

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
        // RegisterLayout() adds a "Control layout" to the system.
        // These can be layouts for individual Controls (like sticks)
        // or layouts for entire Devices (which are themselves
        // Control) like in our case.
        InputSystem.RegisterLayout<MyDevice>();
    }

    // We still need a way to also trigger execution of the static constructor
    // in the player. This can be achieved by adding the RuntimeInitializeOnLoadMethod
    // to an empty method.
    [RuntimeInitializeOnLoadMethod]
    private static void InitializeInPlayer() {}
}
```

This registers the Device type with the system and makes it available in the Control picker. However, you still need a way to actually add an instance of the Device when it is connected.

In theory, you could call [`InputSystem.AddDevice<MyDevice>()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_AddDevice__1_System_String_) somewhere but in a real-world setup you likely have to correlate the Input Devices you create with their identities in the third-party API. Itmight be tempting to do something like this

```CSharp
public class MyDevice : InputDevice, IInputUpdateCallbackReceiver
{
    //...

    // This will NOT work correctly!
    public ThirdPartyAPI.DeviceId externalId { get; set; }
}
```

and then set that on the Device after calling [`AddDevice<MyDevice>`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_AddDevice__1_System_String_) but this doesn't work as expected in the editor.

This is because the Input System requires Devices to be created solely from their [`InputDeviceDescription`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html) in combination with the chosen layout (and layout variant). In addition, the system supports a fixed set of mutable per-device properties such as device usages (that is, [`InputSystem.SetDeviceUsage()`](../api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_SetDeviceUsage_UnityEngine_InputSystem_InputDevice_System_String_) and related methods). This allows the system to easily recreate Devices after domain reloads in the Editor, as well as to create replicas of remote Devices when connecting to a Player. To comply with this requirement, you must cast that information provided by the third-party API into an [`InputDeviceDescription`](../api/UnityEngine.InputSystem.Layouts.InputDeviceDescription.html) and then use an [`InputDeviceMatcher`](../api/UnityEngine.InputSystem.Layouts.InputDeviceMatcher.html) to match the description to our custom `MyDevice` layout.

Let's assume that the third-party API has two callbacks like this:

```CSharp
public static ThirdPartyAPI
{
    // Let's assume that the argument is a string that contains the
    // name of the Device and no two Devices will have the
    // same name in the external API.
    public static Action<string> deviceAdded;
    public static Action<string> deviceRemoved;
}
```

You can hook into those callbacks and create and destroy devices in response.

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
        // Feed a description of the Device into the system. In response, the
        // system will match it to the layouts it has and create a Device.
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
        // Add a match that catches any Input Device that reports its
        // interface as being "ThirdPartyAPI".
        InputSystem.RegisterLayout<MyDevice>(
            matches: new InputDeviceMatcher()
                .WithInterface("ThirdPartyAPI"));
    }
}
```

#### Step 5: `current` and `all` (optional)

For convenience, you can quickly access the last used device of a given type, or list all devices of a specific type. To do this, add support for a `current` and for an `all` getter to the API of `MyDevice`.

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
[//]: # (A final, but optional, step is to add support for Device commands. A "device command" is that opposite of input, i.e. it is data traveling __to__ the input device &ndash; and which may optionally also return data as part of the operation (much like a function call). This can be used to communicate with the backend of the device to query configuration or initiate effects such as haptics.)
[//]: # (TODO: ATM we're missing an overridable method to make this work)
