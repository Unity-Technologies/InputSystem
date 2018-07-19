This page highlights a number of ways in how [InputSystemX](https://github.com/Unity-Technologies/InputSystemX) differs from [InputSystem](https://github.com/Unity-Technologies/InputSystem). It also tries to highlight similarities.

The system has been designed with a key focus being performance. My three-paragraph sales pitch would read like this:

>It's built on a much more efficient and streamlined state system where the state of all devices collapses into a single unmanaged allocation, where all state updating is just memcpys and all state change detection (only for explicitly enabled actions) is just memcmps. It can consume state in pretty much whatever format (including raw HID input reports) with no state conversions necessary in the pipeline. All final value conditioning (like e.g. unpacking a trigger that's stored as a single byte and returning it as a normalized float) is done on-demand and only when values are actually queried by the user. The processing pipeline is fully configurable but the user only pays for processing that sits on controls actually queried by the user.
>
>There is no routing anymore, there are no managed C# objects for events and thus no pooling and unmarshaling. The event processing is readily jobifyable and the managed stack for event processing is a total of 2 levels deep. Native will normally send full-device state updates in single events (though it may also update partial state; it's still just memcpy, simply has an extra offset).
>
>The complexity of the system has significantly gone down as has the OO-yness of it. I expect to reach the point of meaningful feature parity at under 10k lines of code (old system was >16k) while at the same time reaching significantly increased levels of performance as well as flexibility.

## Events

* Native and managed use the same event representation
* The structure of native events is still the same
* Events still come in as a chunk of raw memory passed on directly from native
* The event stream can still be listened to and the InputEventPtr wrapper shields from most low-level details but close-up work with events requires unsafe code now
* There is only one event queue and it sits in native
* There are no C# event objects anymore
* There is no routing of events; InputManager processes events in a (more or less) tight loop (jobifyable)
* There are no events anymore that require interpretation

>What this means is that before you had something like PointerEvent, for example, which encapsulated multiple state changes on a pointer device. The Pointer class in its ProcessEventIntoState() method would then figure out how those state changes translated into value changes on controls.
>
>What you have now is only StateEvents and DeltaEvents. Both memcpy data directly into the state buffer of a device. There can be no interpretation of which data has to go where -- the event needs to come in in the right format.

* Events are no longer time-sliced

>What this means is that the system will not try to parcel out events individually to fixed updates. Instead, all events will be processed every update (before render updates are still special, though).
>
>One consequence of this is that if you get multiple fixed updates in a single dynamic update, the second fixed update will not see any change in values.

## State

* State storage is managed centrally; all devices share the same chunk of (unmanaged) memory
* State cannot accumulate anymore (-> pointer deltas)
* State cannot automatically reset anymore (-> pointer deltas)

>What these two items mean is that before you were able to just send a bunch of pointer events each having a pointer motion delta. The system would then go and aggregate multiple deltas that happened in a single frame and combine them into a final value. It would also go and reset deltas automatically at the beginning of the next frame.
>
>To get the same effect now, accumulation and resetting has to be handled at the source. I.e. code that generates state that includes deltas has to accumulate samples itself and has to make sure it is sending events to reset deltas when necessary. You can't write an equivalent of the old DeltaAxisControl in the new system.

* Double buffering (previous and current) is handled centrally instead of per-control
* State memory is always linear; a device maps to a single contiguous block of memory
* Default values *have* to be all zeros; custom defaults are not supported

## Devices

* Device profiles are replaced by templates which can both describe as well as alter control setups
* Devices/control setups (templates) can be described using JSON
* They can also be created automatically by reflecting on state structures and control classes
* Metadata for discovered devices is still delivered through InputDeviceDescription (renamed from InputDeviceDescriptor)
* Every device has a numeric ID now which is managed by native

## Controls

* Controls still represent values (possibly structured)
* Controls form hierarchies now
* Devices are the root entry points into control hierarchies
* Devices are themselves controls now
* Controls and devices can be looked up using flexible path strings that can match an arbitrary number of controls
* The concept of "common controls" is replaced by the concept of "usage" which gives meaning to a control
* Control hierarchies are now created in a single place (InputControlSetup) from a single source of data (InputTemplate)
* Remapping happens as a side effect of control setup rather than being performed at the event level; as such there is no associated cost with it
* There are no control IDs anymore
* Control setups are entirely data driven (though there is support to create the data from code through reflection)

## Actions

* Actions can be lose (as in not part of collections)
* Actions can be created in code without assets to back them
* Actions now deal with state change rather than state values
* This also means that actions can now detect changes happening in the same frame
* Actions solely work through callbacks now (started, performed, cancelled)
>This is subject to change. The final version will likely present both a polling-based and an event-based interface but no callback-based interface as these come with a number of problems (lack of control by the user over when processing happens, lack of natural sync points for threading, difficulty of managing registrations, etc.)
* Actions monitor for state value changes in bulk rather than on a per-action basis
* Actions can perform holds, taps, swipes, and so on
* Bindings are a simple structs now containing a simple action name -> source path mapping
* Actions can be grouped in named sets
* Action sets can be read from and written to JSON
* Bindings can get grouped in named sets
* Binding sets can be read from and written to JSON
* Action sets no longer generate code

## Also

* Comments and tests :)
