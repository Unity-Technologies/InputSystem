////TODO: This page is work in progress

The system breaks down into a "passive" and an "active" part.

# Passive

The "passive" part is concerned with capturing state using minimal per-frame processing. It is largely set up automatically but can be configured/adjusted by the user. It has a zero-setup API that will be familiar to users of Unity's existing input system.

## Controls

A control captures a (possibly structured) value.

There is a range of pre-defined control types: [ButtonControl](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/ButtonControl.cs), [AxisControl](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/AxisControl.cs), [StickControl](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/StickControl.cs), [PoseControl](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/PoseControl.cs), etc. New control types can be written by anyone.

### Hierarchies and Paths

Controls are named and form hierarchies. Controls in a hierarchy can be looked up by path (e.g. "/gamepad/leftStick" but can also include patterns like in "/*/leftStick").

Path matching is efficient and does not allocate garbage.

In addition to wildcards, controls can be matched by usage (`/gamepad/{primaryAction}`) and by template (`/gamepad/<button>`).

Matching is case-insensitive.

    ////REVIEW: the path syntax seems more elaborate than it needs to be

### Usages

Every control may have one or more usages associated with it. Usages give meaning to a control. For example, there's a "Back" usage which is associated with the "Escape" key on a keyboard and with the "B" button on a gamepad.

### Processors

A control can have one or more associated processors that are arranged in a stack. When retrieving a value of a control from its state, the value is subsequently passed through the stack and every processor can alter the value along the way.

A processor can have parameters which can be set from templates. Any public field on a processor is considered a possible parameter.

There are various predefined processors such as [DeadzoneProcessor](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/Processors/DeadzoneProcessor.cs), [InvertProcessor](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/Processors/InvertProcessor.cs), [ClampProcessor](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/Processors/ClampProcessor.cs), [NormalizeProcessor](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/Processors/NormalizeProcessor.cs), and [CurveProcessor](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/Processors/CurveProcessor.cs).

Note that as the system permits structured values, it's possible to perform operations like deadzoning properly on Vector2s.

## Templates

Templates describe control hierarchies. [InputControlSetup](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/InputControlSetup.cs) turns them into a hierarchy of [InputControls](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/InputControl.cs).

Templates can be constructed in two ways:

1. From JSON
2. Through reflection on control and state types

Templates have to be registered explicitly with the system. There is no automatic scanning. Any template can be replaced at any time by simply registering a template with an already registered name. Replacing templates will automatically take effect on all devices that are using the template.

A control hierarchy must be completely described by its template. It it thus possible to re-create any control hierarchy by reprocessing its template.

Internally, templates are represented using [InputTemplate](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/InputTemplate.cs). However, these objects are created on-demand only inside [InputControlSetup](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/InputControlSetup.cs) and not kept in memory past device creation. For templates derived from types, only a reference to the type and name are kept around. For templates created through JSON, the JSON data itself is kept around. For templates built in code through InputTemplateBuilder, the serialized JSON of the resulting template is kept around.

### Variants

Any single template can have multiple variations. For example, the "Gamepad" template has both a default, right-handed setup and a "Lefty" variant which swaps triggers, sticks, and shoulder buttons.

## Devices

Devices are controls that sit at the root of a control hierarchy. They have to be instances of [InputDevice](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Devices/InputDevice.cs).

Devices act as central storage containers to their control hierarchy. State blocks in the global buffers are assigned on a per-device basis.

Every device gets a unique numeric ID when added to the system. The IDs are managed by the native input system.

Devices must have unique names. When a device is added to the system with an already used named, the name will automatically be adjusted (e.g. "gamepad" becomes "gamepad1").

Lookup of control paths starts with devices at the root (e.g. "/gamepad*/leftStick").

## State

Values of controls are stored in state buffers. There are multiple buffers serving different purposes but all devices in the system share the same buffers and all buffers share the same single allocation in unmanaged memory.

Every control hierarchy gets its own layout which is determined by [InputControlSetup](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/InputControlSetup.cs). The layouts can be a combination of automatic layouting as well as fixed offset layouts supplied by templates.

State is bit-addressable. [ButtonControl](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/ButtonControl.cs) uses this to store its state as a single bit and allows several buttons to be packed into a single int (see [GamepadState](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Devices/Gamepad.cs) for an example).

By far the easiest way to define state is to use state structs. These are simply C# structs with fields that are annotated with [InputControlAttribute](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Controls/InputControlAttribute.cs). A state struct can be associated with a device class using [InputStateAttribute](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/State/InputStateAttribute.cs). The struct can then be used to send state updates for the device. The template system will automatically pick up the struct and incorporate its information into templates.

An example of such a struct is [GamepadState](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Devices/Gamepad.cs). State structs can also be embedded within each other which, in the case of [GamepadState](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Devices/Gamepad.cs) is done for [GamepadOutputState](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Devices/Gamepad.cs).

>There are no "smarts" built into the state system. If you need specific behavior in your state over time, you
>have to build that behavior into the state event generation part. An example are pointer deltas which require
>both accumulation during frames and resetting between frames. The system cannot do that automatically for you.

State is double-buffered to keep a copy of the current values of controls and a copy of the previous values. Buffer swapping is automatic and is handled on a per-device basis, i.e. for every device we decide when to swap based on state events coming in (see [InputStateBuffers](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/State/InputStateBuffers.cs) for an explanation).

In certain scenarios, we have more than one set of double buffers. In the editor, this is always the case in order to separate edit mode from play mode device state (only one receives state events based on focus while the other is dormant). Also, for player updates, it is the case if both dynamic and fixed updates are enabled (the default because we can't know where the user is querying state). A game is expected to decide on which update to employ and to disable the update it doesn't need. In a player, it will then have a single set of double buffers.

### State Change Monitors

Byte (or even bit) regions in states can be assigned "change monitors". If monitors are set up for a particular state and that state receives a state event, the system will perform MemCmps of the state-to-be-assigned to the state-currently-stored. If contents of the given memory region in the state are different, a change notification is triggered.

This system is not publicly accessible ATM and is specific to actions. `InputManager.AddStateChangeMonitor()` sets up a monitor and `InputAction.NotifyControlValueChanged()` receives the notifications (*after* the new state has been committed to memory using MemCpy). Opening up change monitors may make sense for users who want to build their own action system on top of the raw device layer.

## Events

Events change the state of the system.

There's two main kinds of events:

1. Events that push new state into devices ([StateEvent](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Events/StateEvent.cs) is a full-state update, [DeltaEvent](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Events/DeltaEvent.cs) is a partial-state update).
2. Events that relate other relevant information about devices ([disconnects](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Events/DisonnectEvent.cs) and [reconnects](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Events/ConnectEvent.cs), [text input](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Events/TextInput.cs), etc).

Events are accumulated on a queue which sits in the engine itself (`NativeInputSystem`). The event representation is shared with native code (`Modules/Input`) and is flexible. An event basically is a FourCC type code, a size in bytes, a timestamps, and a flexible payload. We put some upper bound on the size of events but they can be large.

State events (both [StateEvents](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Events/StateEvent.cs) and [DeltaEvent](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Events/DeltaEvent.cs)) employ identical state layouts with the control hierarchy of the device they are targeting. Basic FourCC type checks are in place to catch blatant errors.

The data in state events is simply memcopied over the current state of the device. A device can choose to send a state snapshot every single frame (e.g. an XInput gamepad backend would query connected gamepads at regular intervals and push their full-state snapshots as single events into the system) or can choose to send events only when actual value changes are detected.

For high-frequency value changes where it may be important to send every single value change with its own time stamp instead of aggregating events at the source, it may be advisable to use [DeltaEvent](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Events/DeltaEvent.cs) instead of sending a full device state snapshot every time.

There is no class representation of events. The user can listen to the event stream but will get an [InputEventPtr](https://github.com/Unity-Technologies/InputSystemX/blob/master/Assets/InputSystem/Events/InputEventPtr.cs) that require unsafe code in order to work with the data. For the most part, the event stream is *not* meant for consumption at a user level. It is expected that users dealing with events directly will mostly be those authoring new device backends.

## Output

Output/haptics is implemented through state events that are sent in the opposite direction. Output controls use the same state layout and allocation logic that other controls use.

For a device to be able to receive output, it has to provide a buffer into which the system will write output state events as they appear.

# Active

The "active" part of the system requires explicit setup by the user and incurs processing overhead proportional to the amount of enabled functionality. Unlike the "passive" part, it is concerned with state *change* rather than with state itself.

Actions are an optional feature. A user can stick entirely to the passive part of the system and work with devices and controls only.

    ////TODO: Actions still need the most fleshing out ATM. What's there so far is the ability to flexibly target controls and run code when controls change value. We need more than that.

## Actions

An action monitors for state change and invokes user code in response to them.

An action has four phases:

1. Waiting
2. Started
3. Performed
4. Cancelled

>Actions try to keep their per-frame processing minimal but because state changes in entire blocks, actions
>can only know that a piece of state has changed that *contains* the value the action is interested in. To
>find out whether the actual value inside the state has changed, an action still has to do work.
>
>Actions employ state change monitors to not have to poll every single state they are interested in
>for every single frame. See "State Change Monitors" above.

### Sources

Every action needs to know the controls it should monitor for state changes. The number of controls is not limited.

These sources are determined for an action by giving it a binding which contains a path that is used to match controls in the system.

If the control setup of the system is changed, actions will automatically update their set of controls. This means that as new devices are added, for example, if any of their controls match an action's source path, they will automatically hook up with actions.

## Modifiers

A modifier controls an action's progression through its phases. An example is a "hold" modifier that will only go to the "Performed" phase after a specified amount of time has elapsed and will go to the "Cancelled" phase if the monitored state goes back to its default before that (in other words, in the case of a button that would mean releasing the button).

## Bindings

A binding correlates an action with one or more sources.

## Action Sets

An action set groups a set of actions and allows them to be enabled and disabled in bulk. Action sets also allow applying binding sets to the entire group of actions as well as getting binding sets from the currently used sources of the actions in a set.

Action sets can be constructed in two ways:

1. By loading them from JSON.
2. Manually in code.

Action sets can also be converted back to JSON and stored for later use.

>NOTE: The system makes it *possible* for action sets to be stored as JSON assets. However, it itself makes no
>effort for setting up a system to do so. It does supply an ActionSetObject wrapper and associated
>CustomInspector, though.

## Binding Sets

Binding sets can be constructed in two ways:

1. By loading them from JSON.
2. Manually in code.

Binding sets can also be converted back to JSON and stored for later use.

>NOTE: The system makes it *possible* for binding sets to be stored as JSON assets. However, it itself makes no
>effort for setting up a system to handle that. It does supply a BindingSetObject wrapper and associated
>CustomInspector, though.
