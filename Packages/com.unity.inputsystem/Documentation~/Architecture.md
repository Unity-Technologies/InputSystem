# Architecture

The input system has a layered architecture. Principally, it divides into a low-level and a high-level layer.

# Native Backend

The foundation of the input system is the native backend code. This is highly platform-specific code which collects information about available devices and input data from devices. This code is not part of the Input System package, but is included in Unity itself instead, and has implementations for each runtime platform supported by Unity (This is also why some platform-specific input bugs can only be fixed by an update to Unity, and not by a new version of the Input System package).

The Input System interfaces with the native backend using [events](Events.md) which are sent by the native backend. These events notify the system of the creation and removal of input [devices](Devices.md), as well as of any updates to the state of devices. For efficiency and to avoid creating any garbage, the native backends reports the events as a simple buffer of raw, unmanaged memory containing a stream of events.

The Input System can also send data back to the native backend in the form of [commands](Devices.md#device-commands) sent to devices, which are also buffers of memory which are interpreted by the native backend and can have different meanings for different device types and platforms.

# Input System Low-Level

The low-level input system code will process and interpret the memory from the event stream provided by the native backend, and dispatch individual events.

The input system will create device representations for any newly discovered device in the event stream. For the low-level code, an input device is represented as a block of raw, unmanaged memory. If a state event is received for a device, the data from the state event will be written into the device's [state representation](Controls.md#control-state) in memory, so that the state always contains an up-to-date representation of the device and all it's controls.

The low-level system code also contains structs which describe the data layout of commonly known devices.

# Input System High-Level

It's the job of the high-level input system code to interpret the data in device's state buffers. To do this, it uses the concept of [layouts](Layouts.md), which describe the data layout of a device and it's controls in memory. Layouts are created from either the pre-defined structs of commonly known devices supplied by the low level system, or dynamically at runtime, as in the case of [generic HIDs](HID.md#auto-generated-layouts).

Based on the information in the layouts, the input system will then create [control](Controls.md) representations for each of the devices controls, which let you read the state of each individual control in a device.

The high-level system then also allows you to build another abstraction layer to map input controls to your game mechanics, by using [actions](Actions.md). Actions allow you to [bind](ActionBindings.md) one or multiple controls to an input in your game. The input system will then monitor these controls for state changes, and notify your game logic using [callbacks](Actions.md#responding-to-actions). You can also specify more complex behaviors for your actions using [Processors](Processors.md) (which perform processing on the input data before sending it to you) and [Interactions](Interactions.md) (which let you specify patterns of input on a control to listen to, such as multi-taps, etc).
