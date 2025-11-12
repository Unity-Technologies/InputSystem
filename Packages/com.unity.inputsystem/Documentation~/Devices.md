---
uid: input-system-devices
---
# Devices

Physically, Input Devices represent devices attached to the computer, which a user can use to control the app. Logically, Input Devices are the top-level container for [Controls](xref:input-system-controls). The [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) class is itself a specialization of [`InputControl`](xref:UnityEngine.InputSystem.InputControl). See [supported Devices](xref:input-system-supported-devices) to see what kind of Devices the Input System currently supports.

To query the set of all currently present Devices, you can use [`InputSystem.devices`](xref:UnityEngine.InputSystem.InputSystem.devices).

## Device descriptions

The Input System uses the device description defined as a [`InputDeviceDescription`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription) primarily during the Device discovery process. When a new Device is reported (by the runtime or by the user), the system then attempts to find a Device [layout](xref:input-system-layouts) that matches the Device description contained in the report. This process is based on [Device matching](#matching).

After a Device has been created, you can retrieve the description it was created from through the [`InputDevice.description`](xref:UnityEngine.InputSystem.InputDevice.description) property.

Every description has a set of standard fields:

|Field|Description|
|-----|-----------|
|[`interfaceName`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription.interfaceName)|Identifier for the interface/API that is making the Device available. In many cases, this corresponds to the name of the platform, but there are several more specific interfaces that are commonly used: [HID](https://www.usb.org/hid), [RawInput](https://docs.microsoft.com/en-us/windows/desktop/inputdev/raw-input), [XInput](https://docs.microsoft.com/en-us/windows/desktop/xinput/xinput-game-controller-apis-portal).<br>This field is required.|
|[`deviceClass`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription.deviceClass)|A broad categorization of the Device. For example, "Gamepad" or "Keyboard".|
|[`product`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription.product)|Name of the product as reported by the Device/driver itself.|
|[`manufacturer`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription.manufacturer)|Name of the manufacturer as reported by the Device/driver itself.|
|[`version`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription.version)|If available, provides the version of the driver or hardware for the Device.|
|[`serial`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription.serial)|If available, provides the serial number for the Device.|
|[`capabilities`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription.capabilities)|A string in JSON format that describes Device/interface-specific capabilities. See the [section on capabilities](#capabilities).|

### Capabilities

Aside from a number of standardized fields, such as `product` and `manufacturer`, a Device description can contain a [`capabilities`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription.capabilities) string in JSON format. This string describes characteristics which help the Input System to interpret the data from a Device, and map it to Control representations. Not all Device interfaces report Device capabilities. Examples of interface-specific Device capabilities are [HID descriptors](xref:input-system-hid). WebGL, Android, and Linux use similar mechanisms to report available Controls on connected gamepads.

### Matching

[`InputDeviceMatcher`](xref:UnityEngine.InputSystem.Layouts.InputDeviceMatcher) instances handle matching an [`InputDeviceDescription`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription) to a registered layout. Each matcher loosely functions as a kind of regular expression. Each field in the description can be independently matched with either a plain string or regular expression. Matching is not case-sensitive. For a matcher to apply, all of its individual expressions have to match.

To matchers to any layout, call [`InputSystem.RegisterLayoutMatcher`](xref:UnityEngine.InputSystem.InputSystem.RegisterLayoutMatcher(System.String,UnityEngine.InputSystem.Layouts.InputDeviceMatcher)). You can also supply them when you register a layout.

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

If multiple matchers identifies the same [`InputDeviceDescription`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription), the Input System chooses the matcher that has the larger number of properties to match against.

#### Hijacking the matching process

You can overrule the internal matching process from outside to select a different layout for a Device than the system would normally choose. This also makes it possible to quickly build new layouts. To do this, add a custom handler to the  [`InputSystem.onFindControlLayoutForDevice`](xref:UnityEngine.InputSystem.InputSystem.onFindLayoutForDevice) event. If your handler returns a non-null layout string, then the Input System uses this layout.

### Device lifecycle

#### Device creation

Once the system has chosen a [layout](xref:input-system-layouts) for a device, it instantiates an [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) and populates it with [`InputControls`](xref:UnityEngine.InputSystem.InputControl) as the layout dictates. This process is internal and happens automatically.

> [!NOTE]
> You can't create valid [`InputDevices`](xref:UnityEngine.InputSystem.InputDevice) and [`InputControls`](xref:UnityEngine.InputSystem.InputControl) by manually instantiating them with `new`. To guide the creation process, you must use [layouts](xref:input-system-layouts).

After the Input System assembles the [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice), it calls [`FinishSetup`](xref:UnityEngine.InputSystem.InputControl.FinishSetup*) on each control of the device and on the device itself. Use this to finalize the setup of the Controls.

After an [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) is fully assembled, the Input System adds it to the system. As part of this process, the Input System calls [`MakeCurrent`](xref:UnityEngine.InputSystem.InputDevice.MakeCurrent*) on the Device, and signals  [`InputDeviceChange.Added`](xref:UnityEngine.InputSystem.InputDeviceChange.Added) on [`InputSystem.onDeviceChange`](xref:UnityEngine.InputSystem.InputSystem.onDeviceChange). The Input System also calls [`InputDevice.OnAdded`](xref:UnityEngine.InputSystem.InputDevice.OnAdded*).

Once added, the [`InputDevice.added`](xref:UnityEngine.InputSystem.InputDevice.added) flag is set to true.

To add devices manually, you can call one of the `InputSystem.AddDevice` methods such as [`InputSystem.AddDevice(layout)`](xref:UnityEngine.InputSystem.InputSystem.AddDevice(System.String,System.String,System.String)).

```CSharp
// Add a gamepad. This bypasses the matching process and creates a device directly
// with the Gamepad layout.
InputSystem.AddDevice<Gamepad>();

// Add a device such that the matching process is employed:
InputSystem.AddDevice(new InputDeviceDescription
{
    interfaceName = "XInput",
    product = "Xbox Controller",
});
```

When a device is added, the Input System automatically issues a [sync request](xref:UnityEngine.InputSystem.LowLevel.RequestSyncCommand) on the device. This instructs the device to send an event representing its current state. Whether this request succeeds depends on the whether the given device supports the sync command.

#### Device removal

When a Device is disconnected, it is removed from the system. A notification appears for [`InputDeviceChange.Removed`](xref:UnityEngine.InputSystem.InputDeviceChange) (sent via [`InputSystem.onDeviceChange`](xref:UnityEngine.InputSystem.InputSystem.onDeviceChange)) and the Devices are removed from the [`devices`](xref:UnityEngine.InputSystem.InputSystem.onDeviceChange) list. The system also calls [`InputDevice.OnRemoved`](xref:UnityEngine.InputSystem.InputDevice.OnRemoved*).

The [`InputDevice.added`](xref:UnityEngine.InputSystem.InputDevice.added) flag is reset to false in the process.

Note that Devices are not destroyed when removed. Device instances remain valid and you can still access them in code. However, trying to read values from the controls of these Devices leads to exceptions.

#### Device resets

Resetting a Device resets its Controls to their default state. You can do this manually using [`InputSystem.ResetDevice`](xref:UnityEngine.InputSystem.InputSystem.ResetDevice(UnityEngine.InputSystem.InputDevice,System.Boolean)):

```CSharp
    InputSystem.ResetDevice(Gamepad.current);
```

There are two types of resets as determined by the second parameter to [`InputSystem.ResetDevice`](xref:UnityEngine.InputSystem.InputSystem.ResetDevice(UnityEngine.InputSystem.InputDevice,System.Boolean)):

|Reset Type|Description|
|----|-----------|
|**Soft** Resets|This is the default. With this type, only controls that are not marked as [`dontReset`](xref:input-system-layouts#control-items) are reset to their default value. This excludes controls such as [`Pointer.position`](xref:UnityEngine.InputSystem.Pointer.position) from resets and thus prevents mouse positions resetting to `(0,0)`.|
|**Hard** Resets|In this type, all controls are reset to their default value regardless of whether they have [`dontReset`](xref:input-system-layouts#control-items) set or not.|

Resetting Controls this way is visible on [Actions](xref:input-system-actions). If you reset a Device that is currently driving one or more Action, the Actions are cancelled. This cancellation is different from sending an event with default state. Whereas the latter may inadvertently [perform](xref:UnityEngine.InputSystem.InputAction.performed) Actions (for example, a button that was pressed would not appear to have been released), a reset will force clean cancellation.

Resets may be triggered automatically by the Input System depending on [application focus](#background-and-focus-change-behavior).

#### Device syncs

A Device may be requested to send an event with its current state through [`RequestSyncCommand`](xref:UnityEngine.InputSystem.LowLevel.RequestSyncCommand). It depends on the platform and type of Device whether this is supported or not.

A synchronization request can be explicitly sent using [`InputSystem.TrySyncDevice`](xref:UnityEngine.InputSystem.InputSystem.TrySyncDevice(UnityEngine.InputSystem.InputDevice)). If the device supports sync requests, the method returns true and an [`InputEvent`](xref:UnityEngine.InputSystem.LowLevel.InputEvent) will have been queued on the device for processing in the next [update](xref:UnityEngine.InputSystem.InputSystem.Update*).

Synchronization requests are also automatically sent by the Input System in certain situations. See [Background and focus change behavior](#background-and-focus-change-behavior) for more details.

#### Device enabling and disabling

When a Device is added, the Input System sends it an initial [`QueryEnabledStateCommand`](xref:UnityEngine.InputSystem.LowLevel.QueryEnabledStateCommand) to find out whether the device is currently enabled or not. The result of this is reflected in the [`InputDevice.enabled`](xref:UnityEngine.InputSystem.InputDevice.enabled) property.

When disabled, no events other than removal ([`DeviceRemoveEvent`](xref:UnityEngine.InputSystem.LowLevel.DeviceRemoveEvent)) and configuration change ([`DeviceConfigurationEvent`](xref:UnityEngine.InputSystem.LowLevel.DeviceConfigurationEvent)) events are processed for a Device, even if they are sent.

A Device can be manually disabled and re-enabled via [`InputSystem.DisableDevice`](xref:UnityEngine.InputSystem.InputSystem.DisableDevice*) and [`InputSystem.EnableDevice`](xref:UnityEngine.InputSystem.InputSystem.EnableDevice*) respectively.

Note that [sensors](xref:input-system-sensors) start in a disabled state by default, and you need to enable them in order for them to generate events.

The Input System may automatically disable and re-enable Devices in certain situations, as detailed in the [next section](#background-and-focus-change-behavior).

#### Background and focus change behavior

In general, input is tied to [application focus](https://docs.unity3d.com/ScriptReference/Application-isFocused.html). This means that Devices do not receive input while the application is not in the foreground and thus no [Actions](xref:input-system-actions) will receive input either. When the application comes back into focus, all devices will receive a [sync](#device-syncs) request to have them send their current state (which may have changed while the application was in the background) to the application. Devices that do not support sync requests will see a [soft reset](#device-resets) that resets all Controls not marked as [`dontReset`](xref:input-system-layouts#control-items) to their default state.

On platforms such as iOS and Android, that do not support running Unity applications in the background, this is the only supported behavior.

If the application is configured to run while in the background (that is, not having focus), input behavior can be selected from several options. This is supported in two scenarios:

* In Unity's [Player Settings](https://docs.unity3d.com/Manual/class-PlayerSettings.html) you can explicity enable `Run In Background` for specific players that support it (such as Windows or Mac standalone players). Note that in these players this setting is always enabled automatically in *development* players.
* In the editor, application focus is tied to focus on the Game View. If no Game View is focused, the application is considered to be running in the background. However, while in play mode, the editor will *always* keep running the player loop regardless of focus on the Game View window. This means that in the editor, `Run In Background` is considered to always be enabled.

If the application is configured this way to keep running while in the background, the player loop and thus the Input System, too, will keep running even when the application does not have focus. What happens with respect to input then depends on two factors:

1. On the ability of individual devices to receive input while the application is not running in the foreground. This is only supported by a small subset of devices and platforms. VR devices ([`TrackedDevice`](xref:UnityEngine.InputSystem.TrackedDevice)) such as HMDs and VR controllers generally support this.

    To find out whether a specific device supports this, you can query the [`InputDevice.canRunInBackground`](xref:UnityEngine.InputSystem.InputDevice.canRunInBackground) property. This property can also be forced to true or false via a Device's [layout](xref:input-system-layouts#control-items).

    > [!NOTE]
    > [`InputDevice.canRunInBackground`](xref:UnityEngine.InputSystem.InputDevice.canRunInBackground) is overridden by the editor in certain situations (see table below). In general, the value of the property does not have to be the same between the editor and the player and depends on the specific platform and device.

2. On two settings you can find in the project-wide [Input Settings](xref:input-system-settings): [`InputSettings.backgroundBehavior`](xref:UnityEngine.InputSystem.InputSettings.backgroundBehavior) and [`InputSettings.editorInputBehaviorInPlayMode`](xref:UnityEngine.InputSystem.InputSettings.editorInputBehaviorInPlayMode). The table below shows a detailed breakdown of how input behaviors vary based on these two settings and in relation to the `Run In Background` player setting in Unity.

The following table shows the full matrix of behaviors according to the [Input Settings](xref:input-system-settings) and whether the game is running in the editor or in the player.

![Focus Behavior](Images/FocusBehavior.png)

#### Domain reloads in the Editor

The Editor reloads the C# application domain whenever it reloads and recompiles scripts, or when the Editor goes into Play mode. This requires the Input System to reinitialize itself after each domain reload. During this process, the Input System attempts to recreate devices that were instantiated before the domain reload. However, the state of each Device doesn't carry across, which means that Devices reset to their default state on domain reloads.

Note that layout registrations do not persist across domain reloads. Instead, the Input System relies on all registrations to become available as part of the initialization process (for example, by using `[InitializeOnLoad]` to run registration as part of the domain startup code in the Editor). This allows you to change registrations and layouts in script, and the change to immediately take effect after a domain reload.

## Native Devices

Devices that the [native backend](xref:input-system-architecture#native-backend) reports are considered native (as opposed to Devices created from script code). To identify these Devices, you can check the [`InputDevice.native`](xref:UnityEngine.InputSystem.InputDevice.native) property.

The Input System remembers native Devices. For example, if the system has no matching layout when the Device is first reported, but a layout which matches the device is registered later, the system uses this layout to recreate the Device.

You can force the Input System to use your own [layout](xref:input-system-layouts) when the native backend discovers a specific Device, by describing the Device in the layout, like this:

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

> [!NOTE]
> You don't have to restart Unity in order for changes in your layout to take effect on native Devices. The Input System applies changes automatically on every domain reload, so you can just keep refining a layout and your Device is recreated with the most up-to-date version every time scripts are recompiled.


### Disconnected Devices

If you want to get notified when Input Devices disconnect, subscribe to the [`InputSystem.onDeviceChange`](xref:UnityEngine.InputSystem.InputSystem.onDeviceChange) event, and look for events of type [`InputDeviceChange.Disconnected`](xref:UnityEngine.InputSystem.InputDeviceChange).

The Input System keeps track of disconnected Devices in [`InputSystem.disconnectedDevices`](xref:UnityEngine.InputSystem.InputSystem.disconnectedDevices). If one of these Devices reconnects later, the Input System can detect that the Device was connected before, and reuses its [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) instance. This allows the [`PlayerInputManager`](xref:input-system-player-input-manager) to reassign the Device to the same [user](xref:input-system-user-management) again.

## Device IDs

Each Device that is created receives a unique numeric ID. You can access this ID through [`InputDevice.deviceId`](xref:UnityEngine.InputSystem.InputDevice.deviceId).

All IDs are only used once per Unity session.

## Device usages

Like any [`InputControl`](xref:UnityEngine.InputSystem.InputControl), a Device can have usages associated with it. You can query usages with the [`usages`](xref:UnityEngine.InputSystem.InputControl.usages) property, and use[`InputSystem.SetDeviceUsage()`](xref:UnityEngine.InputSystem.InputSystem.SetDeviceUsage(UnityEngine.InputSystem.InputDevice,System.String)) to set them. Usages can be arbitrary strings with arbitrary meanings. One common case where the Input System assigns Devices usages is the handedness of XR controllers, which are tagged with the "LeftHand" or "RightHand" usages.

## Device commands

While input [events](xref:input-system-events) deliver data from a Device, commands send data back to the Device. The Input System uses these to retrieve specific information from the Device, to trigger functions on the Device (such as rumble effects), and for a variety of other needs.

### Sending commands to Devices

The Input System sends commands to the Device through [`InputDevice.ExecuteCommand<TCommand>`](xref:UnityEngine.InputSystem.InputDevice.ExecuteCommand``1(``0@)). To monitor Device commands, use [`InputSystem.onDeviceCommand`](xref:UnityEngine.InputSystem.InputSystem.onDeviceCommand).

Each Device command implements the [`IInputDeviceCommandInfo`](xref:UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo) interface, which only requires the [`typeStatic`](xref:UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.typeStatic) property to identify the type of the command. The native implementation of the Device should then understand how to handle that command. One common case is the `"HIDO"` command type which is used to send [HID output reports](xref:input-system-hid#hid-output) to HIDs.

### Adding custom device Commands

To create custom Device commands (for example, to support some functionality for a specific HID), create a `struct` that contains all the data to be sent to the Device, and add a [`typeStatic`](xref:UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo.typeStatic) property to make that struct implement the [`IInputDeviceCommandInfo`](xref:UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo) interface. To send data to a HID, this property should return `"HIDO"`.

You can then create an instance of this struct and populate all its fields, then use [`InputDevice.ExecuteCommand<TCommand>`](xref:UnityEngine.InputSystem.InputDevice.ExecuteCommand``1(``0@)) to send it to the Device. The data layout of the struct must match the native representation of the data as the device interprets it.

## Device state

Like any other type of [Control](xref:input-system-controls#control-state), each Device has a block of memory allocated to it which stores the state of all the Controls associated with the Device.

### State changes

State changes are usually initiated through [state events](xref:input-system-events#state-events) from the native backend, but you can use [`InputControl<>.WriteValueIntoState()`](xref:UnityEngine.InputSystem.InputControl`1.WriteValueIntoState(`0,System.Void*)) to manually overwrite the state of any Control.

#### Monitoring state changes

You can use [`InputState.AddChangeMonitor()`](xref:UnityEngine.InputSystem.LowLevel.InputState.AddChangeMonitor(UnityEngine.InputSystem.InputControl,System.Action{UnityEngine.InputSystem.InputControl,System.Double,UnityEngine.InputSystem.LowLevel.InputEventPtr,System.Int64},System.Int32,System.Action{UnityEngine.InputSystem.InputControl,System.Double,System.Int64,System.Int32})) to register a callback to be called whenever the state of a Control changes. The Input System uses the same mechanism to implement [input Actions](xref:input-system-actions).

#### Synthesizing state

The Input System can synthesize a new state from an existing state. An example of such a synthesized state is the [`press`](xref:UnityEngine.InputSystem.Pointer.press) button  Control that [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) inherits from [`Pointer`](xref:UnityEngine.InputSystem.Pointer). Unlike a mouse, which has a physical button, for [`Touchscreen`](xref:UnityEngine.InputSystem.Touchscreen) this is a [synthetic Control](xref:input-system-controls#synthetic-controls) that doesn't correspond to actual data coming in from the Device backend. Instead, the Input System considers the button to be pressed if any touch is currently ongoing, and released otherwise.

To do this, the Input System uses [`InputState.Change`](xref:UnityEngine.InputSystem.LowLevel.InputState.Change``1(UnityEngine.InputSystem.InputControl,``0,UnityEngine.InputSystem.LowLevel.InputUpdateType,UnityEngine.InputSystem.LowLevel.InputEventPtr)), which allows feeding arbitrary state changes into the system without having to run them through the input event queue. The Input System incorporates state changes directly and synchronously. State change [monitors](#monitoring-state-changes) still trigger as expected.

## Working with Devices

### Monitoring Devices

To be notified when new Devices are added or existing Devices are removed, use [`InputSystem.onDeviceChange`](xref:UnityEngine.InputSystem.InputSystem.onDeviceChange).

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

[`InputSystem.onDeviceChange`](xref:UnityEngine.InputSystem.InputSystem.onDeviceChange) delivers notifications for other device-related changes as well. See the [`InputDeviceChange` enum](xref:UnityEngine.InputSystem.InputDeviceChange) for more information.

### Adding and removing Devices

To manually add and remove Devices through the API, use [`InputSystem.AddDevice()`](xref:UnityEngine.InputSystem.InputSystem.AddDevice(UnityEngine.InputSystem.InputDevice)) and [`InputSystem.RemoveDevice()`](xref:UnityEngine.InputSystem.InputSystem.RemoveDevice(UnityEngine.InputSystem.InputDevice)).

This allows you to create your own Devices, which can be useful for testing purposes, or for creating virtual Input Devices which synthesize input from other events. As an example, see the [on-screen Controls](xref:input-system-on-screen) that the Input System provides. The Input Devices used for on-screen Controls are created entirely in code and have no [native representation](#native-devices).

### Creating custom Devices

> [!NOTE]
> This example deals only with Devices that have fixed layouts (that is, you know the specific model or models that you want to implement). This is different from an interface such as HID, where Devices can describe themselves through the interface and take on a wide variety of forms. A fixed Device layout can't cover self-describing Devices, so you need to use a [layout builder](xref:input-system-layouts#generated-layouts) to build Device layouts from information you obtain at runtime.

There are two main situations in which you might need to create a custom Device:

1. You have an existing API that generates input, and which you want to reflect into the Input System.
2. You have an HID that the Input System ignores, or that the Input system auto-generates a layout for that doesn't work well enough for your needs.

For the second scenario, see [Overriding the HID Fallback](xref:input-system-hid#creating-a-custom-device-layout).

The steps below deal with the first scenario, where you want to create a new Input Device entirely from scratch and provide input to it from a third-party API.

#### Step 1: The state struct

The first step is to create a C# `struct` that represents the form in which the system receives and stores input, and also describes the `InputControl` instances that the Input System must create for the Device in order to retrieve its state.

```CSharp
// A "state struct" describes the memory format that a Device uses. Each Device can
// receive and store memory in its custom format. InputControls then connect to
// the individual pieces of memory and read out values from them.
//
// If it's important for the memory format to match 1:1 at the binary level
// to an external representation, it's generally advisable to use
// LayoutLind.Explicit.
[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct MyDeviceState : IInputStateTypeInfo
{
    // You must tag every state with a FourCC code for type
    // checking. The characters can be anything. Choose something that allows
    // you to easily recognize memory that belongs to your own Device.
    public FourCC format => new FourCC('M', 'Y', 'D', 'V');

    // InputControlAttributes on fields tell the Input System to create Controls
    // for the public fields found in the struct.

    // Assume a 16bit field of buttons. Create one button that is tied to
    // bit #3 (zero-based). Note that buttons don't need to be stored as bits.
    // They can also be stored as floats or shorts, for example. The
    // InputControlAttribute.format property determines which format the
    // data is stored in. If omitted, the system generally infers it from the value
    // type of the field.
    [InputControl(name = "button", layout = "Button", bit = 3)]
    public ushort buttons;

    // Create a floating-point axis. If a name is not supplied, it is taken
    // from the field.
    [InputControl(layout = "Axis")]
    public short axis;
}
```

The Input System's layout mechanism uses [`InputControlAttribute`](xref:UnityEngine.InputSystem.Layouts.InputControlAttribute) annotations to add Controls to the layout of your Device. For details, see the [layout system](xref:input-system-layouts) documentation.

With the state struct in place, you now have a way to send input data to the Input System and store it there. The next thing you need is an [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) that uses your custom state struct and represents your custom Device.

#### Step 2: The Device class

Next, you need a class derived from one of the [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice) base classes. You can either base your Device directly on [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice), or you can pick a more specific Device type, like [`Gamepad`](xref:UnityEngine.InputSystem.Gamepad).

This example assumes that your Device doesn't fit into any of the existing Device classes, so it derives directly from [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice).

```CSharp
// InputControlLayoutAttribute attribute is only necessary if you want
// to override the default behavior that occurs when you register your Device
// as a layout.
// The most common use of InputControlLayoutAttribute is to direct the system
// to a custom "state struct" through the `stateType` property. See below for details.
[InputControlLayout(displayName = "My Device", stateType = typeof(MyDeviceState))]
public class MyDevice : InputDevice
{
    // In the state struct, you added two Controls that you now want to
    // surface on the Device, for convenience. The Controls
    // get added to the Device either way. When you expose them as properties,
    // it is easier to get to the Controls in code.

    public ButtonControl button { get; private set; }
    public AxisControl axis { get; private set; }

    // The Input System calls this method after it constructs the Device,
    // but before it adds the device to the system. Do any last-minute setup
    // here.
    protected override void FinishSetup()
    {
        base.FinishSetup();

        // NOTE: The Input System creates the Controls automatically.
        //       This is why don't do `new` here but rather just look
        //       the Controls up.
        button = GetChildControl<ButtonControl>("button");
        axis = GetChildControl<AxisControl>("axis");
    }
}
```

#### Step 3: The Update method

You now have a Device in place along with its associated state format. You can call the following method to create a fully set-up Device with your two Controls on it:

```CSharp
InputSystem.AddDevice<MyDevice>();
```

However, this Device doesn't receive input yet, because you haven't added any code that generates input. To do that, you can use [`InputSystem.QueueStateEvent`](xref:UnityEngine.InputSystem.InputSystem.QueueStateEvent``1(UnityEngine.InputSystem.InputDevice,``0,System.Double)) or [`InputSystem.QueueDeltaStateEvent`](xref:UnityEngine.InputSystem.InputSystem.QueueDeltaStateEvent``1(UnityEngine.InputSystem.InputControl,``0,System.Double)) from anywhere, including from a thread. The following example uses [`IInputUpdateCallbackReceiver`](xref:UnityEngine.InputSystem.LowLevel.IInputUpdateCallbackReceiver), which, when implemented by any [`InputDevice`](xref:UnityEngine.InputSystem.InputDevice), adds an [`OnUpdate()`](xref:UnityEngine.InputSystem.LowLevel.IInputUpdateCallbackReceiver.OnUpdate) method that automatically gets called during [`InputSystem.onBeforeUpdate`](xref:UnityEngine.InputSystem.InputSystem.onBeforeUpdate) and provides input events to the current input update.

> [!NOTE]
> If you already have a place where input for your device becomes available, you can skip this step and queue input events from there instead of using [`IInputUpdateCallbackReceiver`](xref:UnityEngine.InputSystem.LowLevel.IInputUpdateCallbackReceiver).

```CSharp
public class MyDevice : InputDevice, IInputUpdateCallbackReceiver
{
    //...

    public void OnUpdate()
    {
        // In practice, this would read out data from an external
        // API. This example uses some empty input.
        var state = new MyDeviceState();
        InputSystem.QueueStateEvent(this, state);
    }
}
```

#### Step 4: Device registration and creation

You now have a functioning device, but you haven't registered it (added it to the system) yet. This means you can't see the device when, for example, you create bindings in the [Action editor](xref:input-system-action-assets#editing-input-action-assets).

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
        // Controls) like in our case.
        InputSystem.RegisterLayout<MyDevice>();
    }

    // You still need a way to trigger execution of the static constructor
    // in the Player. To do this, you can add the RuntimeInitializeOnLoadMethod
    // to an empty method.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeInPlayer() {}
}
```

This registers the Device type with the system and makes it available in the Control picker. However, you still need a way to add an instance of the Device when it is connected.

In theory, you could call [`InputSystem.AddDevice<MyDevice>()`](xref:UnityEngine.InputSystem.InputSystem.AddDevice``1(System.String)) somewhere, but in a real-world setup you likely have to correlate the Input Devices you create with their identities in the third-party API.

It might be tempting to do something like this:

```CSharp
public class MyDevice : InputDevice, IInputUpdateCallbackReceiver
{
    //...

    // This does NOT work correctly.
    public ThirdPartyAPI.DeviceId externalId { get; set; }
}
```

and then set that on the Device after calling [`AddDevice<MyDevice>`](xref:UnityEngine.InputSystem.InputSystem.AddDevice``1(System.String)). However, this doesn't work as expected in the Editor, because the Input System requires Devices to be created solely from their [`InputDeviceDescription`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription) in combination with the chosen layout (and layout variant). In addition, the system supports a fixed set of mutable per-device properties such as device usages (that is, [`InputSystem.SetDeviceUsage()`](xref:UnityEngine.InputSystem.InputSystem.SetDeviceUsage(UnityEngine.InputSystem.InputDevice,System.String)) and related methods). This allows the system to easily recreate Devices after domain reloads in the Editor, as well as to create replicas of remote Devices when connecting to a Player. To comply with this requirement, you must cast that information provided by the third-party API into an [`InputDeviceDescription`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription) and then use an [`InputDeviceMatcher`](xref:UnityEngine.InputSystem.Layouts.InputDeviceMatcher) to match the description to our custom `MyDevice` layout.

This example assumes that the third-party API has two callbacks, like this:

```CSharp
public static ThirdPartyAPI
{
    // This example assumes that the argument is a string that
    // contains the name of the Device, and that no two Devices
    // have the same name in the external API.
    public static Action<string> deviceAdded;
    public static Action<string> deviceRemoved;
}
```

You can hook into those callbacks and create and destroy devices in response.

```CSharp
// This example uses a MonoBehaviour with [ExecuteInEditMode]
// on it to run the setup code. You can do this many other ways.
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
        // system matches it to the layouts it has and creates a Device.
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

    // Move the registration of MyDevice from the
    // static constructor to here, and change the
    // registration to also supply a matcher.
    protected void Awake()
    {
        // Add a match that catches any Input Device that reports its
        // interface as "ThirdPartyAPI".
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

#### Step 6: Device Commands (Optional)

A final, but optional, step is to add support for Device commands. A "device command" is that opposite of input. In other words, it consists of data traveling __to__ the input device, which might also return data as part of the operation (much like a function call). You can use this to communicate with the backend of the device in order to query configuration, or to initiate effects such as haptics. At the moment there isn't a proper interface available for this, however there are still some scenarios that can be solved with the current interfaces.

E.g. the following shows, when implementing a non-hardware-backed device (simulated device), how to simulate hardware reporting that the device can be run in the background and supports sync commands. This is useful to prevent the device from cancelling Actions when application focus is lost and restored. For more info see [Device syncs](#device-syncs)

```CSharp
public class MyDevice : InputDevice, IInputCallbackReceiver
{
    //...

    protected override unsafe long ExecuteCommand(InputDeviceCommand* commandPtr)
    {
        var type = commandPtr->type;
        if (type == RequestSyncCommand.Type)
        {
            // Report that the device supports the sync command and has handled it.
            // This will prevent device reset during focus changes.
            result = InputDeviceCommand.GenericSuccess;
            return true;
        }

        if (type == QueryCanRunInBackground.Type)
        {
            // Notify that the device supports running in the background.
            ((QueryCanRunInBackground*)commandPtr)->canRunInBackground = true;
            result = InputDeviceCommand.GenericSuccess;
            return true;
        }

        result = default;
        return false;
    }
}
```
