using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Input.Haptics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.DualShock;
using UnityEngine.Experimental.Input.Plugins.HID;
using UnityEngine.Experimental.Input.Plugins.XInput;
using UnityEngine.Experimental.Input.Utilities;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Experimental.Input.Editor;
using UnityEditor.Networking.PlayerConnection;
#else
using UnityEngine.Networking.PlayerConnection;
#endif

#if !(NET_4_0 || NET_4_6 || NET_STANDARD_2_0 || UNITY_WSA)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

////TODO: move state change monitor API out of here (static InputStateChangeMonitor class?)

////TODO: rename RegisterControlProcessor to just RegisterProcessor

////REVIEW: make more APIs thread-safe?

////REVIEW: it'd be great to be able to set up monitors from control paths (independently of actions; or should we just use actions?)

////REVIEW: have InputSystem.onTextInput that's fired directly from the event processing loop?
////        (and allow text input events that have no associated target device? this way we don't need a keyboard to get text input)

////REVIEW: split lower-level APIs (anything mentioning events and state) off into InputSystemLowLevel API to make this API more focused?

////TODO: release native allocations when exiting

[assembly: InternalsVisibleTo("Unity.InputSystem.Tests")]

// Keep this in sync with "Packages/com.unity.inputsystem/package.json".
// NOTE: Unfortunately, System.Version doesn't use semantic versioning so we can't include
//       "-preview" suffixes here.
[assembly: AssemblyVersion("0.0.9")]

namespace UnityEngine.Experimental.Input
{
    using NotifyControlValueChangeAction = Action<InputControl, double, InputEventPtr, long>;
    using NotifyTimerExpiredAction = Action<InputControl, double, long, int>;

    /// <summary>
    /// This is the central hub for the input system.
    /// </summary>
    // Takes care of the singletons we need and presents a sanitized API.
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class InputSystem
    {
        #region Layouts

        /// <summary>
        /// Event that is signalled when the layout setup in the system changes.
        /// </summary>
        public static event Action<string, InputControlLayoutChange> onLayoutChange
        {
            add { s_Manager.onLayoutChange += value; }
            remove { s_Manager.onLayoutChange -= value; }
        }

        /// <summary>
        /// Register a control layout based on a type.
        /// </summary>
        /// <param name="type">Type to derive a control layout from. Must be derived from <see cref="InputControl"/>.</param>
        /// <param name="name">Name to use for the layout. If null or empty, the short name of the type will be used.</param>
        /// <param name="matches">Optional device description. If this is supplied, the layout will automatically
        /// be instantiated for newly discovered devices that match the description.</param>
        /// <remarks>
        /// When the layout is instantiated, the system will reflect on all public fields and properties of the type
        /// which have a value type derived from <see cref="InputControl"/> or which are annotated with <see cref="InputControlAttribute"/>.
        /// </remarks>
        public static void RegisterLayout(Type type, string name = null, InputDeviceMatcher? matches = null)
        {
            if (string.IsNullOrEmpty(name))
                name = type.Name;

            s_Manager.RegisterControlLayout(name, type);

            if (matches != null)
                s_Manager.RegisterControlLayoutMatcher(name, matches.Value);
        }

        /// <summary>
        /// Register a type as a control layout.
        /// </summary>
        /// <typeparam name="T">Type to derive a control layout from.</typeparam>
        /// <param name="name">Name to use for the layout. If null or empty, the short name of the type will be used.</param>
        /// <param name="matches">Optional device description. If this is supplied, the layout will automatically
        /// be instantiated for newly discovered devices that match the description.</param>
        /// <remarks>
        /// When the layout is instantiated, the system will reflect on all public fields and properties of the type
        /// which have a value type derived from <see cref="InputControl"/> or which are annotated with <see cref="InputControlAttribute"/>.
        /// </remarks>
        public static void RegisterLayout<T>(string name = null, InputDeviceMatcher? matches = null)
            where T : InputControl
        {
            RegisterLayout(typeof(T), name, matches);
        }

        /// <summary>
        /// Register a layout in JSON format.
        /// </summary>
        /// <param name="json">Layout in JSON format.</param>
        /// <param name="name">Optional name of the layout. If null or empty, the name is taken from the "name"
        /// property of the JSON data. If it is supplied, it will override the "name" property if present. If neither
        /// is supplied, an <see cref="ArgumentException"/> is thrown.</param>
        /// <param name="matches"></param>
        /// <exception cref="ArgumentException">No name has been supplied either through <paramref name="name"/>
        /// or the "name" JSON property.</exception>
        /// <remarks>
        /// Note that most errors in layouts will only be detected when instantiated (i.e. when a device or control is
        /// being created from a layout). The JSON data will, however, be parsed once on registration to check for a
        /// device description in the layout. JSON format errors will thus be detected during registration.
        /// </remarks>
        /// <example>
        /// <code>
        /// InputSystem.RegisterLayout(@"
        ///    {
        ///        ""name"" : ""MyDevice"",
        ///        ""controls"" : [
        ///            {
        ///                ""name"" : ""myThing"",
        ///                ""layout"" : ""MyControl"",
        ///                ""usage"" : ""LeftStick""
        ///            }
        ///        ]
        ///    }
        /// );
        /// </code>
        /// </example>
        public static void RegisterLayout(string json, string name = null, InputDeviceMatcher? matches = null)
        {
            s_Manager.RegisterControlLayout(json, name);

            if (matches != null)
                s_Manager.RegisterControlLayoutMatcher(name, matches.Value);
        }

        /// <summary>
        /// Register a layout that applies overrides to one or more other layouts.
        /// </summary>
        /// <param name="json">Layout in JSON format.</param>
        /// <param name="name">Optional name of the layout. If null or empty, the name is taken from the "name"
        /// property of the JSON data. If it is supplied, it will override the "name" property if present. If neither
        /// is supplied, an <see cref="ArgumentException"/> is thrown.</param>
        /// <remarks>
        /// Layout overrides are layout pieces that are applied on top of existing layouts.
        /// This can be used to modify any layout in the system non-destructively. The process works the
        /// same as extending an existing layout except that instead of creating a new layout
        /// by merging the derived layout and the base layout, the overrides are merged
        /// directly into the base layout.
        ///
        /// Layouts used as overrides look the same as normal layouts and have the same format.
        /// The only difference is that they are explicitly registered as overrides.
        ///
        /// Note that unlike "normal" layouts, layout overrides have the ability to extend
        /// multiple base layouts.
        /// </remarks>
        public static void RegisterLayoutOverride(string json, string name = null)
        {
            s_Manager.RegisterControlLayout(json, name, isOverride: true);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="layoutName"></param>
        /// <param name="matcher"></param>
        public static void RegisterLayoutMatcher(string layoutName, InputDeviceMatcher matcher)
        {
            s_Manager.RegisterControlLayoutMatcher(layoutName, matcher);
        }

        /// <summary>
        /// Register a builder that delivers an <see cref="InputControlLayout"/> instance on demand.
        /// </summary>
        /// <param name="builderExpression"></param>
        /// <param name="name"></param>
        /// <param name="baseLayout"></param>
        /// <param name="matches"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// The given expression must be a lambda expression solely comprised of a method call with
        /// no arguments. Can be static or instance method call. If it is an instance method, the
        /// instance object must be serializable.
        ///
        /// The reason for these restrictions and for not taking an arbitrary delegate is that we
        /// need to be able to persist the layout builder between domain reloads.
        ///
        /// Note that the layout that is being constructed must not vary over time (except between
        /// domain reloads).
        /// </remarks>
        /// <example>
        /// <code>
        /// [Serializable]
        /// class MyLayoutBuilder
        /// {
        ///     public InputControlLayout Build()
        ///     {
        ///         var builder = new InputControlLayout.Builder()
        ///             .WithType<MyDevice>();
        ///         builder.AddControl("button1").WithLayout("Button");
        ///         return builder.Build();
        ///     }
        /// }
        ///
        /// var builder = new MyLayoutBuilder();
        /// InputSystem.RegisterLayoutBuilder(() => builder.Build(), "MyLayout");
        /// </code>
        /// </example>
        public static void RegisterLayoutBuilder(Expression<Func<InputControlLayout>> builderExpression, string name,
            string baseLayout = null, InputDeviceMatcher? matches = null)
        {
            if (builderExpression == null)
                throw new ArgumentNullException("builderExpression");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name");

            // Grab method and (optional) instance from lambda expression.
            var methodCall = builderExpression.Body as MethodCallExpression;
            if (methodCall == null)
                throw new ArgumentException(
                    string.Format("Body of layout builder function must be a method call (is a {0} instead)",
                        builderExpression.Body.NodeType),
                    "builderExpression");

            var method = methodCall.Method;

            object instance = null;
            if (methodCall.Object != null)
            {
                // NOTE: We can't compile expressions on the fly here to invoke them as that
                //       won't work on AOT. So we have to perform a little bit of manual
                //       interpretation of the expression tree.

                switch (methodCall.Object.NodeType)
                {
                    // Method call on value referenced as constant.
                    case ExpressionType.Constant:
                        instance = ((ConstantExpression)methodCall.Object).Value;
                        break;

                    // Method call on variable being closed over (comes out as a field access).
                    case ExpressionType.MemberAccess:
                        // Get object that has the field.
                        var expr = ((MemberExpression)methodCall.Object).Expression;
                        var constantExpr = expr as ConstantExpression;
                        if (constantExpr == null)
                            throw new ArgumentException(
                                string.Format(
                                    "Body of layout builder function must be a method call on a constant or variable expression (accesses member of {0} instead)",
                                    expr.NodeType), "builderExpression");

                        // Get field.
                        var member = ((MemberExpression)methodCall.Object).Member;
                        var field = member as FieldInfo;
                        if (field == null)
                            throw new ArgumentException(
                                string.Format(
                                    "Body of layout builder function must be a method call on a constant or variable expression (member access does not access field but rather {0} {1})",
                                    member.GetType().Name, member.Name), "builderExpression");

                        // Read value.
                        instance = field.GetValue(constantExpr.Value);
                        break;

                    default:
                        throw new ArgumentException(
                            string.Format(
                                "Expression nodes of type {0} are not supported as the target of the method call in a builder expression",
                                methodCall.Object.NodeType), "builderExpression");
                }
            }

            // Register.
            s_Manager.RegisterControlLayoutBuilder(method, instance, name, baseLayout: baseLayout);
            if (matches != null)
                s_Manager.RegisterControlLayoutMatcher(name, matches.Value);
        }

        /// <summary>
        /// Remove an already registered layout from the system.
        /// </summary>
        /// <param name="name">Name of the layout to remove. Note that layout names are case-insensitive.</param>
        /// <remarks>
        /// Note that removing a layout also removes all devices that directly or indirectly
        /// use the layout.
        ///
        /// This method can be used to remove both control or device layouts.
        /// </remarks>
        public static void RemoveLayout(string name)
        {
            s_Manager.RemoveControlLayout(name);
        }

        /// <summary>
        /// Try to match a description for an input device to a layout.
        /// </summary>
        /// <param name="deviceDescription">Description of an input device.</param>
        /// <returns>Name of the layout that has been matched to the given description or null if no
        /// matching layout was found.</returns>
        /// <remarks>
        /// Layouts are matched by the <see cref="InputDeviceDescription"/> they were registered with (if any).
        /// The fields in a layout's device description are considered regular expressions which are matched
        /// against the values supplied in the given <paramref name="deviceDescription"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// var layoutName = InputSystem.TryFindMatchingLayout(
        ///     new InputDeviceDescription
        ///     {
        ///         product = "Xbox Wired Controller",
        ///         manufacturer = "Microsoft"
        ///     }
        /// );
        /// </code>
        /// </example>
        public static string TryFindMatchingLayout(InputDeviceDescription deviceDescription)
        {
            return s_Manager.TryFindMatchingControlLayout(ref deviceDescription);
        }

        /// <summary>
        /// Return a list with the names of all layouts that have been registered.
        /// </summary>
        /// <returns>A list of layout names.</returns>
        /// <seealso cref="ListLayouts(List{string})"/>
        public static List<string> ListLayouts()
        {
            var list = new List<string>();
            s_Manager.ListControlLayouts(list);
            return list;
        }

        /// <summary>
        /// Add the names of all layouts that have been registered to the given list.
        /// </summary>
        /// <param name="list">List to add the layout names to.</param>
        /// <returns>The number of names added to <paramref name="list"/>.</returns>
        /// <remarks>
        /// If the capacity of the given list is large enough, this method will not allocate.
        /// </remarks>
        public static int ListLayouts(List<string> list)
        {
            return s_Manager.ListControlLayouts(list);
        }

        public static List<string> ListLayoutsBasedOn(string baseLayout)
        {
            var result = new List<string>();
            ListLayoutsBasedOn(baseLayout, result);
            return result;
        }

        public static int ListLayoutsBasedOn(string baseLayout, List<string> list)
        {
            return s_Manager.ListControlLayouts(list, basedOn: baseLayout);
        }

        /// <summary>
        /// Try to load a layout instance.
        /// </summary>
        /// <param name="name">Name of the layout to load. Note that layout names are case-insensitive.</param>
        /// <returns>The constructed layout instance or null if no layout of the given name could be found.</returns>
        public static InputControlLayout TryLoadLayout(string name)
        {
            ////FIXME: this will intern the name even if the operation fails
            return s_Manager.TryLoadControlLayout(new InternedString(name));
        }

        #endregion

        #region Processors

        /// <summary>
        /// Register an <see cref="IInputControlProcessor{TValue}"/> with the system.
        /// </summary>
        /// <param name="type">Type that implements <see cref="IInputControlProcessor{TValue}"/>.</param>
        /// <param name="name">Name to use for the processor. If null or empty, name will be taken from short name
        /// of <paramref name="type"/> (if it ends in "Processor", that suffix will be clipped from the name).</param>
        public static void RegisterControlProcessor(Type type, string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = type.Name;
                if (name.EndsWith("Processor"))
                    name = name.Substring(0, name.Length - "Processor".Length);
            }

            s_Manager.processors.AddTypeRegistration(name, type);
        }

        public static void RegisterControlProcessor<T>(string name = null)
        {
            RegisterControlProcessor(typeof(T), name);
        }

        public static Type TryGetProcessor(string name)
        {
            return s_Manager.processors.LookupTypeRegistration(name);
        }

        #endregion

        #region Devices

        /// <summary>
        /// The list of currently connected devices.
        /// </summary>
        /// <remarks>
        /// Note that accessing this property does not allocate. It gives read-only access
        /// directly to the system's internal array of devices.
        ///
        /// The value returned by this property should not be held on to. When the device
        /// setup in the system changes, any value previously returned by this property
        /// becomes invalid. Query the property directly whenever you need it.
        /// </remarks>
        public static ReadOnlyArray<InputDevice> devices
        {
            get { return s_Manager.devices; }
        }

        /// <summary>
        /// Devices that have been disconnected but are retained by the input system in case
        /// they are plugged back in.
        /// </summary>
        /// <remarks>
        /// During gameplay it is undesirable to have the system allocate and release managed memory
        /// as devices are unplugged and plugged back in as it would ultimately lead to GC spikes
        /// during gameplay. To avoid that, input devices that have been reported by the <see cref="IInputRuntime">
        /// runtime</see> and are removed through <see cref="DeviceRemoveEvent">events</see> are retained
        /// by the system and then reused if the device is plugged back in.
        ///
        /// Note that the devices moved to disconnected status will still see a <see cref="InputDeviceChange.Removed"/>
        /// notification and a <see cref="InputDeviceChange.Added"/> notification when plugged back in.
        ///
        /// To determine if a newly discovered device is one we have seen before, the system uses a
        /// simple approach of comparing <see cref="InputDeviceDescription">device descriptions</see>.
        /// Note that there can be errors and a device may be incorrectly classified as <see cref="InputDeviceChange.Reconnected"/>
        /// when in fact it is a different device from before. The problem is that based on information
        /// made available by platforms, it can be inherently difficult to determine whether a device is
        /// indeed the very same one.
        ///
        /// For example, it is often not possible to determine with 100% certainty whether an identical looking device
        /// to one we've previously seen on a different USB port is indeed the very same device. OSs will usually
        /// reattach a USB device to its previous instance if it is plugged into the same USB port but create a
        /// new instance of the same device is plugged into a different port.
        ///
        /// For devices that do relay their <see cref="InputDeviceDescription.serial">serials</see> the matching
        /// is reliable.
        ///
        /// The list can be purged by calling <see cref="RemoveDisconnetedDevices"/>. Doing so, will release
        /// all reference we hold to the devices or any controls inside of them and allow the devices to be
        /// reclaimed by the garbage collector.
        ///
        /// Note that if you call <see cref="RemoveDevice"/> explicitly, the given device is not retained
        /// by the input system and will not appear on this list.
        ///
        /// Also note that devices on this list will be lost when domain reloads happen in the editor (i.e. on
        /// script recompilation and when entering play mode).
        /// </remarks>
        /// <seealso cref="RemoveDisconnectedDevices"/>
        public static ReadOnlyArray<InputDevice> disconnectedDevices
        {
            get
            {
                return new ReadOnlyArray<InputDevice>(s_Manager.m_DisconnectedDevices, 0,
                    s_Manager.m_DisconnectedDevicesCount);
            }
        }

        /// <summary>
        /// Event that is signalled when the device setup in the system changes.
        /// </summary>
        /// <remarks>
        /// This can be used to detect when devices are added or removed as well as
        /// detecting when existing devices change their configuration.
        /// </remarks>
        /// <example>
        /// <code>
        /// InputSystem.onDeviceChange +=
        ///     (device, change) =>
        ///     {
        ///         switch (change)
        ///         {
        ///             case InputDeviceChange.Added:
        ///                 Debug.Log("Device added: " + device);
        ///                 break;
        ///             case InputDeviceChange.Removed:
        ///                 Debug.Log("Device removed: " + device);
        ///                 break;
        ///             case InputDeviceChange.ConfigurationChanged:
        ///                 Debug.Log("Device coniguration changed: " + device);
        ///                 break;
        ///         }
        ///     };
        /// </code>
        /// </example>
        public static event Action<InputDevice, InputDeviceChange> onDeviceChange
        {
            add { s_Manager.onDeviceChange += value; }
            remove { s_Manager.onDeviceChange -= value; }
        }

        /// <summary>
        /// Event that is signalled when the system is trying to match a layout to
        /// a device it has discovered.
        /// </summary>
        /// <remarks>
        /// This event allows customizing the layout discovery process and to generate
        /// layouts on the fly, if need be. The system will invoke callbacks with the
        /// name of the layout it has matched (or <c>null</c> if it couldn't find any
        /// matching layout to the device based on the current layout setup. If all
        /// the callbacks return <c>null</c>, that layout will be instantiated. If,
        /// however, any of the callbacks returns a new name instead, the system will use that
        /// layout instead.
        ///
        /// To generate layouts on the fly, register them with the system in the callback and
        /// then return the name of the newly generated layout from the callback.
        ///
        /// Note that this callback will also be invoked if the system could not match any
        /// existing layout to the device. In that case, the <c>matchedLayout</c> argument
        /// to the callback will be <c>null</c>.
        ///
        /// Callbacks also receive a device ID and reference to the input runtime. For devices
        /// where more information has to be fetched from the runtime in order to generate a
        /// layout, this allows issuing <see cref="IInputRuntime.DeviceCommand"/> calls for the device.
        /// Note that for devices that are not coming from the runtime (i.e. devices created
        /// directly in script code), the device ID will be <see cref="InputDevice.kInvalidDeviceId"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// InputSystem.onFindLayoutForDevice +=
        ///     (deviceId, description, matchedLayout, runtime) =>
        ///     {
        ///         ////TODO: complete example
        ///     };
        /// </code>
        /// </example>
        public static event DeviceFindControlLayoutCallback onFindLayoutForDevice
        {
            add { s_Manager.onFindControlLayoutForDevice += value; }
            remove { s_Manager.onFindControlLayoutForDevice -= value; }
        }

        /// <summary>
        /// Frequency at which devices that need polling are being queried in the background.
        /// </summary>
        /// <remarks>
        /// Input data is gathered from platform APIs either as events or polled periodically.
        ///
        /// In the former case, where we get input as events, the platform is responsible for monitoring
        /// input devices and accumulating their state changes which the input runtime then periodically
        /// queries and sends off as <see cref="InputEvent">input events</see>.
        ///
        /// In the latter case, where input has to be explicitly polled from the system, the input runtime
        /// will periodically sample the state of input devices and send it off as input events. Wherever
        /// possible, this happens in the background at a fixed frequency. The <see cref="pollingFrequency"/>
        /// property controls the rate at which the sampling happens.
        ///
        /// The unit is Hertz. A value of 120, for example, means that devices are sampled 120 times
        /// per second.
        ///
        /// The default polling frequency is 60 Hz.
        ///
        /// For devices that are polled, the frequency setting will directly translate to changes in the
        /// <see cref="InputEvent.time">timestamp</see> patterns. At 60 Hz, for example, timestamps for
        /// a specific, polled device will be spaced at roughly 1/60th of a second apart.
        ///
        /// Note that it depends on the platform which devices are polled (if any). On Win32, for example,
        /// only XInput gamepads are polled.
        ///
        /// Also note that the polling frequency applies to all devices that are polled. It is not possible
        /// to set polling frequency on a per-device basis.
        /// </remarks>
        public static float pollingFrequency
        {
            get { return s_Manager.pollingFrequency; }
            set { s_Manager.pollingFrequency = value; }
        }

        /// <summary>
        /// Add a new device by instantiating the given device layout.
        /// </summary>
        /// <param name="layout">Name of the layout to instantiate. Must be a device layout. Note that
        /// layout names are case-insensitive.</param>
        /// <param name="name">Name to assign to the device. If null, the layout name is used instead. Note that
        /// device names are made unique automatically by the system by appending numbers to them (e.g. "gamepad",
        /// "gamepad1", "gamepad2", etc.).</param>
        /// <param name="variants">Semicolon-separated list of layout variants to use for the device.</param>
        /// <returns>The newly created input device.</returns>
        /// <remarks>
        /// Note that adding a device to the system will allocate and also create garbage on the GC heap.
        /// </remarks>
        /// <example>
        /// <code>
        /// InputSystem.AddDevice("Gamepad");
        /// </code>
        /// </example>
        public static InputDevice AddDevice(string layout, string name = null, string variants = null)
        {
            return s_Manager.AddDevice(layout, name, new InternedString(variants));
        }

        public static TDevice AddDevice<TDevice>(string name = null)
            where TDevice : InputDevice
        {
            var device = s_Manager.AddDevice(typeof(TDevice), name) as TDevice;
            if (device == null)
                throw new Exception(string.Format("Layout registered for type '{0}' did not produce a device of that type; layout probably has been overridden",
                    typeof(TDevice).Name));
            return device;
        }

        public static InputDevice AddDevice(InputDeviceDescription description)
        {
            return s_Manager.AddDevice(description);
        }

        public static void AddDevice(InputDevice device)
        {
            s_Manager.AddDevice(device);
        }

        public static void RemoveDevice(InputDevice device)
        {
            s_Manager.RemoveDevice(device);
        }

        /// <summary>
        /// Purge all disconnected devices from <see cref="disconnectedDevices"/>.
        /// </summary>
        /// <remarks>
        /// This will release all references held on to for these devices or any of their controls and will
        /// allow the devices to be reclaimed by the garbage collector.
        /// </remarks>
        /// <seealso cref="disconnectedDevices"/>
        public static void RemoveDisconnectedDevices()
        {
            throw new NotImplementedException();
        }

        public static InputDevice GetDevice(string nameOrLayout)
        {
            return s_Manager.TryGetDevice(nameOrLayout);
        }

        public static TDevice GetDevice<TDevice>()
            where TDevice : InputDevice
        {
            foreach (var device in devices)
            {
                var deviceOfType = device as TDevice;
                if (deviceOfType != null)
                    return deviceOfType;
            }

            return null;
        }

        /// <summary>
        /// Look up a device by its unique ID.
        /// </summary>
        /// <param name="deviceId">Unique ID of device. Such as given by <see cref="InputEvent.deviceId"/>.</param>
        /// <returns>The device for the given ID or null if no device with the given ID exists (or no longer exists).</returns>
        /// <remarks>
        /// Device IDs are not reused in a given session of the application (or Unity editor).
        /// </remarks>
        /// <seealso cref="InputEvent.deviceId"/>
        /// <seealso cref="InputDevice.id"/>
        /// <seealso cref="IInputRuntime.AllocateDeviceId"/>
        public static InputDevice GetDeviceById(int deviceId)
        {
            return s_Manager.TryGetDeviceById(deviceId);
        }

        /// <summary>
        /// Return the list of devices that have been reported by the <see cref="IInputRuntime">runtime</see>
        /// but could not be matched to any known <see cref="InputControlLayout">layout</see>.
        /// </summary>
        /// <returns>A list of descriptions of devices that could not be recognized.</returns>
        /// <remarks>
        /// If new layouts are added to the system or if additional <see cref="InputDeviceMatcher">matches</see>
        /// are added to existing layouts, devices in this list may appear or disappear.
        /// </remarks>
        /// <seealso cref="InputDeviceMatcher"/>
        /// <seealso cref="RegisterLayoutMatcher"/>
        public static List<InputDeviceDescription> GetUnsupportedDevices()
        {
            var list = new List<InputDeviceDescription>();
            GetUnsupportedDevices(list);
            return list;
        }

        public static int GetUnsupportedDevices(List<InputDeviceDescription> descriptions)
        {
            return s_Manager.GetUnsupportedDevices(descriptions);
        }

        public static void EnableDevice(InputDevice device)
        {
            s_Manager.EnableOrDisableDevice(device, true);
        }

        public static void DisableDevice(InputDevice device)
        {
            s_Manager.EnableOrDisableDevice(device, false);
        }

        ////REVIEW: should this be a device-level reset along with sending the reset IOCTL
        ////        or a control-level reset on just the memory state?
        public static void ResetDevice(InputDevice device)
        {
            throw new NotImplementedException();
        }

        ////REVIEW: should there be a global pause state? what about haptics that are issued *while* paused?

        /// <summary>
        /// Pause haptic effect playback on all devices.
        /// </summary>
        /// <remarks>
        /// Calls <see cref="Haptics.IHaptics.PauseHaptics"/> on all <see cref="InputDevice">input devices</see>
        /// that implement the interface.
        /// </remarks>
        /// <seealso cref="ResumeHaptics"/>
        /// <seealso cref="ResetHaptics"/>
        /// <example>
        /// <code>
        /// // When going into the menu from gameplay, pause haptics.
        /// gameplayControls.backAction.onPerformed +=
        ///     ctx =>
        ///     {
        ///         gameplayControls.Disable();
        ///         menuControls.Enable();
        ///         InputSystem.PauseHaptics();
        ///     };
        /// </code>
        /// </example>
        public static void PauseHaptics()
        {
            var devicesList = devices;
            var devicesCount = devicesList.Count;

            for (var i = 0; i < devicesCount; ++i)
            {
                var device = devicesList[i];
                var haptics = device as IHaptics;
                if (haptics != null)
                    haptics.PauseHaptics();
            }
        }

        /// <summary>
        /// Resume haptic effect playback on all devices.
        /// </summary>
        /// <remarks>
        /// Calls <see cref="Haptics.IHaptics.ResumeHaptics"/> on all <see cref="InputDevice">input devices</see>
        /// that implement the interface.
        /// </remarks>
        /// <seealso cref="PauseHaptics"/>
        public static void ResumeHaptics()
        {
            var devicesList = devices;
            var devicesCount = devicesList.Count;

            for (var i = 0; i < devicesCount; ++i)
            {
                var device = devicesList[i];
                var haptics = device as IHaptics;
                if (haptics != null)
                    haptics.ResumeHaptics();
            }
        }

        /// <summary>
        /// Stop haptic effect playback on all devices.
        /// </summary>
        /// <remarks>
        /// Will reset haptics effects on all devices to their default state.
        ///
        /// Calls <see cref="Haptics.IHaptics.ResetHaptics"/> on all <see cref="InputDevice">input devices</see>
        /// that implement the interface.
        /// </remarks>
        public static void ResetHaptics()
        {
            var devicesList = devices;
            var devicesCount = devicesList.Count;

            for (var i = 0; i < devicesCount; ++i)
            {
                var device = devicesList[i];
                var haptics = device as IHaptics;
                if (haptics != null)
                    haptics.ResetHaptics();
            }
        }

        #endregion

        #region Controls

        ////FIXME: we don't really store this on a per-control basis; make this a call that operates on devices instead
        public static void SetLayoutVariant(InputControl control, string variant)
        {
            s_Manager.SetLayoutVariant(control, variant);
        }

        public static void SetDeviceUsage(InputDevice device, string usage)
        {
            SetDeviceUsage(device, new InternedString(usage));
        }

        // May generate garbage.
        public static void SetDeviceUsage(InputDevice device, InternedString usage)
        {
            s_Manager.SetUsage(device, usage);
        }

        public static void AddDeviceUsage(InputDevice device, InternedString usage)
        {
            throw new NotImplementedException();
        }

        public static void RemoveDeviceUsage(InputDevice device, InternedString usage)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Find all controls that match the given <see cref="InputControlPath">control path</see>.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// // Find all gamepads (literally: that use the "Gamepad" layout).
        /// InputSystem.FindControls("&lt;Gamepad&gt;");
        ///
        /// // Find all sticks on all gamepads.
        /// InputSystem.FindControls("&lt;Gamepad&gt;/*stick");
        ///
        /// // Same but filter stick by type rather than by name.
        /// InputSystem.FindControls<StickControl>("&lt;Gamepad&gt;/*");
        /// </code>
        /// </example>
        /// <seealso cref="FindControls{TControl}"/>
        /// <seealso cref="FindControls{TControl}(string,ref UnityEngine.Experimental.Input.InputControlList{TControl})"/>
        public static InputControlList<InputControl> FindControls(string path)
        {
            return FindControls<InputControl>(path);
        }

        public static InputControlList<TControl> FindControls<TControl>(string path)
            where TControl : InputControl
        {
            var list = new InputControlList<TControl>();
            FindControls(path, ref list);
            return list;
        }

        public static int FindControls<TControl>(string path, ref InputControlList<TControl> controls)
            where TControl : InputControl
        {
            return s_Manager.GetControls(path, ref controls);
        }

        #endregion

        ////TODO: move this entire API out of InputSystem; too low-level and specialized
        #region State Change Monitors

        public static void AddStateChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex = -1)
        {
            if (control == null)
                throw new ArgumentNullException("control");
            if (monitor == null)
                throw new ArgumentNullException("monitor");
            if (control.device.m_DeviceIndex == InputDevice.kInvalidDeviceIndex)
                throw new ArgumentException(string.Format("Device for control '{0}' has not been added to system"));

            s_Manager.AddStateChangeMonitor(control, monitor, monitorIndex);
        }

        public static IInputStateChangeMonitor AddStateChangeMonitor(InputControl control, NotifyControlValueChangeAction valueChangeCallback, int monitorIndex = -1, NotifyTimerExpiredAction timerExpiredCallback = null)
        {
            if (valueChangeCallback == null)
                throw new ArgumentNullException("valueChangeCallback");
            var monitor = new StateChangeMonitorDelegate
            {
                valueChangeCallback = valueChangeCallback,
                timerExpiredCallback = timerExpiredCallback
            };
            AddStateChangeMonitor(control, monitor, monitorIndex);
            return monitor;
        }

        public static void RemoveStateChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex = -1)
        {
            if (control == null)
                throw new ArgumentNullException("control");
            if (monitor == null)
                throw new ArgumentNullException("monitor");

            s_Manager.RemoveStateChangeMonitor(control, monitor, monitorIndex);
        }

        /// <summary>
        /// Put a timeout on a previously registered state change monitor.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="monitor"></param>
        /// <param name="time"></param>
        /// <param name="monitorIndex"></param>
        /// <param name="timerIndex"></param>
        /// <remarks>
        /// If by the given <paramref name="time"/>, no state change has been registered on the control monitored
        /// by the given <paramref name="monitor">state change monitor</paramref>, <see cref="IInputStateChangeMonitor.NotifyTimerExpired"/>
        /// will be called on <paramref name="monitor"/>. If a state change happens by the given <paramref name="time"/>,
        /// the monitor is notified as usual and the timer is automatically removed.
        /// </remarks>
        public static void AddStateChangeMonitorTimeout(InputControl control, IInputStateChangeMonitor monitor, double time, long monitorIndex = -1, int timerIndex = -1)
        {
            if (monitor == null)
                throw new ArgumentNullException("monitor");

            s_Manager.AddStateChangeMonitorTimeout(control, monitor, time, monitorIndex, timerIndex);
        }

        public static void RemoveStateChangeMonitorTimeout(IInputStateChangeMonitor monitor, long monitorIndex = -1, int timerIndex = -1)
        {
            if (monitor == null)
                throw new ArgumentNullException("monitor");

            s_Manager.RemoveStateChangeMonitorTimeout(monitor, monitorIndex, timerIndex);
        }

        private class StateChangeMonitorDelegate : IInputStateChangeMonitor
        {
            public NotifyControlValueChangeAction valueChangeCallback;
            public NotifyTimerExpiredAction timerExpiredCallback;

            public void NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr, long monitorIndex)
            {
                valueChangeCallback(control, time, eventPtr, monitorIndex);
            }

            public void NotifyTimerExpired(InputControl control, double time, long monitorIndex, int timerIndex)
            {
                if (timerExpiredCallback != null)
                    timerExpiredCallback(control, time, monitorIndex, timerIndex);
            }
        }

        #endregion

        #region Events

        public static event Action<InputEventPtr> onEvent
        {
            add { s_Manager.onEvent += value; }
            remove { s_Manager.onEvent -= value; }
        }

        public static void QueueEvent(InputEventPtr eventPtr)
        {
            s_Manager.QueueEvent(eventPtr);
        }

        public static void QueueEvent<TEvent>(ref TEvent inputEvent)
            where TEvent : struct, IInputEventTypeInfo
        {
            s_Manager.QueueEvent(ref inputEvent);
        }

        ////REVIEW: consider moving these out into extension methods in UnityEngine.Experimental.Input.LowLevel

        ////TODO: find a more elegant solution for this
        // Mono will ungracefully poop exceptions if we try to use LayoutKind.Explicit in generic
        // structs. So we can't just stuff a generic TState into a StateEvent<TState> and enforce
        // proper layout. Thus the jumping through lots of ugly hoops here.
        private unsafe struct StateEventBuffer
        {
            public StateEvent stateEvent;
            public const int kMaxSize = 512;
            public fixed byte data[kMaxSize - 1]; // StateEvent already adds one.
        }
        public static unsafe void QueueStateEvent<TState>(InputDevice device, TState state, double time = -1)
            where TState : struct, IInputStateTypeInfo
        {
            if (device == null)
                throw new ArgumentNullException("device");

            // Make sure device is actually in the system.
            if (device.m_DeviceIndex == InputDevice.kInvalidDeviceIndex)
                throw new InvalidOperationException(
                    string.Format("Cannot queue state event device '{0}' because device has not been added to system",
                        device));

            ////REVIEW: does it make more sense to go off the 'stateBlock' on the device and let that determine size?

            var stateSize = (uint)UnsafeUtility.SizeOf<TState>();
            if (stateSize > StateEventBuffer.kMaxSize)
                throw new ArgumentException(
                    string.Format("Size of '{0}' exceeds maximum supported state size of {1}", typeof(TState).Name,
                        StateEventBuffer.kMaxSize),
                    "state");
            var eventSize = UnsafeUtility.SizeOf<StateEvent>() + stateSize - 1;

            if (time < 0)
                time = InputRuntime.s_Instance.currentTime;
            else
                time += InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

            StateEventBuffer eventBuffer;
            eventBuffer.stateEvent =
                new StateEvent
            {
                baseEvent = new InputEvent(StateEvent.Type, (int)eventSize, device.id, time),
                stateFormat = state.GetFormat()
            };

            var ptr = eventBuffer.stateEvent.stateData;
            UnsafeUtility.MemCpy(ptr, UnsafeUtility.AddressOf(ref state), stateSize);

            s_Manager.QueueEvent(ref eventBuffer.stateEvent);
        }

        private unsafe struct DeltaStateEventBuffer
        {
            public DeltaStateEvent stateEvent;
            public const int kMaxSize = 512;
            public fixed byte data[kMaxSize - 1]; // DeltaStateEvent already adds one.
        }
        public static unsafe void QueueDeltaStateEvent<TDelta>(InputControl control, TDelta delta, double time = -1)
            where TDelta : struct
        {
            if (control == null)
                throw new ArgumentNullException("control");

            if (control.stateBlock.bitOffset != 0)
                throw new InvalidOperationException(
                    string.Format("Cannot send delta state events against bitfield controls: {0}", control));

            // Make sure device is actually in the system.
            var device = control.device;
            if (device.m_DeviceIndex == InputDevice.kInvalidDeviceIndex)
                throw new InvalidOperationException(
                    string.Format(
                        "Cannot queue state event for control '{0}' on device '{1}' because device has not been added to system",
                        control, device));

            if (time < 0)
                time = InputRuntime.s_Instance.currentTime;
            else
                time += InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

            var deltaSize = (uint)UnsafeUtility.SizeOf<TDelta>();
            if (deltaSize > DeltaStateEventBuffer.kMaxSize)
                throw new ArgumentException(
                    string.Format("Size of state delta '{0}' exceeds maximum supported state size of {1}",
                        typeof(TDelta).Name, DeltaStateEventBuffer.kMaxSize),
                    "delta");

            ////TODO: recognize a matching C# representation of a state format and convert to what we expect for trivial cases
            if (deltaSize != control.stateBlock.alignedSizeInBytes)
                throw new NotImplementedException("Delta state and control format don't match");

            var eventSize = UnsafeUtility.SizeOf<DeltaStateEvent>() + deltaSize - 1;

            DeltaStateEventBuffer eventBuffer;
            eventBuffer.stateEvent =
                new DeltaStateEvent
            {
                baseEvent = new InputEvent(DeltaStateEvent.Type, (int)eventSize, device.id, time),
                stateFormat = device.stateBlock.format,
                stateOffset = control.m_StateBlock.byteOffset - device.m_StateBlock.byteOffset
            };

            var ptr = eventBuffer.stateEvent.stateData;
            UnsafeUtility.MemCpy(ptr, UnsafeUtility.AddressOf(ref delta), deltaSize);

            s_Manager.QueueEvent(ref eventBuffer.stateEvent);
        }

        public static void QueueConfigChangeEvent(InputDevice device, double time = -1)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (device.id == InputDevice.kInvalidDeviceId)
                throw new InvalidOperationException("Device has not been added");

            if (time < 0)
                time = InputRuntime.s_Instance.currentTime;
            else
                time += InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

            var inputEvent = DeviceConfigurationEvent.Create(device.id, time);
            s_Manager.QueueEvent(ref inputEvent);
        }

        /// <summary>
        /// Queue a text input event on the given device.
        /// </summary>
        /// <param name="device">Device to queue the event on.</param>
        /// <param name="character">Text character to input through the event.</param>
        /// <param name="time">Optional event time stamp. If not supplied, the current time will be used.</param>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="device"/> is a device that has not been
        /// added to the system.</exception>
        public static void QueueTextEvent(InputDevice device, char character, double time = -1)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (device.id == InputDevice.kInvalidDeviceId)
                throw new InvalidOperationException("Device has not been added");

            if (time < 0)
                time = InputRuntime.s_Instance.currentTime;
            else
                time += InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

            var inputEvent = TextEvent.Create(device.id, character, time);
            s_Manager.QueueEvent(ref inputEvent);
        }

        ////REVIEW: call ForceUpdate?
        public static void Update()
        {
            s_Manager.Update();
        }

        public static void Update(InputUpdateType updateType)
        {
            s_Manager.Update(updateType);
        }

        ////TODO: disable collection of input if all input updates are disabled
        /// <summary>
        /// Mask that determines which updates are run by the input system.
        /// </summary>
        /// <remarks>
        /// By default, all update types are enabled. Disabling a specific update
        ///
        /// Clearing all flags in this mask will disable all input processing. Note, however,
        /// that it will not currently disable collection of input.
        /// </remarks>
        public static InputUpdateType updateMask
        {
            get { return s_Manager.updateMask; }
            set { s_Manager.updateMask = value; }
        }

        /// <summary>
        /// Event that is fired before the input system updates.
        /// </summary>
        /// <remarks>
        /// The input system updates in sync with player loop and editor updates. Input updates
        /// are run right before the respective script update. For example, an input update for
        /// <see cref="InputUpdateType.Dynamic"/> is run before <c>MonoBehaviour.Update</c> methods
        /// are executed.
        ///
        /// The update callback itself is triggered before the input system runs its own update and
        /// before it flushes out its event queue. This means that events queued from a callback will
        /// be fed right into the upcoming update.
        /// </remarks>
        /// <seealso cref="onAfterUpdate"/>
        /// <seealso cref="Update"/>
        public static event Action<InputUpdateType> onBeforeUpdate
        {
            add { s_Manager.onBeforeUpdate += value; }
            remove { s_Manager.onBeforeUpdate -= value; }
        }

        /// <summary>
        /// Event that is fired after the input system has completed an update and processed all pending events.
        /// </summary>
        /// <seealso cref="onBeforeUpdate"/>
        /// <seealso cref="Update"/>
        public static event Action<InputUpdateType> onAfterUpdate
        {
            add { s_Manager.onAfterUpdate += value; }
            remove { s_Manager.onAfterUpdate -= value; }
        }

        #endregion

        #region Actions

        /// <summary>
        /// Event that is signalled when the state of enabled actions in the system changes or
        /// when actions are triggered.
        /// </summary>
        public static event Action<object, InputActionChange> onActionChange
        {
            add { InputActionMapState.s_OnActionChange.Append(value); }
            remove { InputActionMapState.s_OnActionChange.Remove(value); }
        }

        /// <summary>
        /// Register a new type of interaction with the system.
        /// </summary>
        /// <param name="type">Type that implements the interaction. Must support <see cref="IInputInteraction"/>.</param>
        /// <param name="name">Name to register the interaction with. This is used in bindings to refer to the interaction
        /// (e.g. an interactions called "Tap" can be added to a binding by listing it in its <see cref="InputBinding.interactions"/>
        /// property). If no name is supplied, the short name of <paramref name="type"/> is used (with "Interaction" clipped off
        /// the name if the type name ends in that).</param>
        /// <example>
        /// <code>
        /// // Interaction that is performed when control resets to default state.
        /// public class ResetInteraction : IInputInteraction
        /// {
        ///     public void Process(ref InputInteractionContext context)
        ///     {
        ///         if (context.isWaiting && !context.controlHasDefaultValue)
        ///             context.Started();
        ///         else if (context.isStarted && context.controlHasDefaultValue)
        ///             context.Performed();
        ///     }
        /// }
        ///
        /// // Make interaction globally available on bindings.
        /// // "Interaction" suffix in type name will get dropped automatically.
        /// InputSystem.RegisterInteraction(typeof(ResetInteraction));
        ///
        /// // Set up action with binding that has the 'reset' interaction applied to it.
        /// var action = new InputAction(binding: "/&lt;Gamepad>/buttonSouth", interactions: "reset");
        /// </code>
        /// </example>
        /// <seealso cref="IInputInteraction"/>
        public static void RegisterInteraction(Type type, string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = type.Name;
                if (name.EndsWith("Interaction"))
                    name = name.Substring(0, name.Length - "Interaction".Length);
            }

            s_Manager.interactions.AddTypeRegistration(name, type);
        }

        public static void RegisterInteraction<T>(string name = null)
        {
            RegisterInteraction(typeof(T), name);
        }

        public static Type TryGetInteraction(string name)
        {
            return s_Manager.interactions.LookupTypeRegistration(name);
        }

        public static IEnumerable<string> ListInteractions()
        {
            return s_Manager.interactions.names;
        }

        public static IEnumerable<string> ListProcessors()
        {
            return s_Manager.processors.names;
        }

        public static void RegisterBindingComposite(Type type, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = type.Name;
                if (name.EndsWith("Composite"))
                    name = name.Substring(0, name.Length - "Composite".Length);
            }

            s_Manager.composites.AddTypeRegistration(name, type);
        }

        public static void RegisterBindingComposite<T>(string name = null)
        {
            RegisterBindingComposite(typeof(T), name);
        }

        public static Type TryGetBindingComposite(string name)
        {
            return s_Manager.composites.LookupTypeRegistration(name);
        }

        /// <summary>
        /// Disable all actions (and implicitly all action sets) that are currently enabled.
        /// </summary>
        /// <seealso cref="ListEnabledActions"/>
        /// <seealso cref="InputAction.Disable"/>
        public static void DisableAllEnabledActions()
        {
            InputActionMapState.DisableAllActions();
        }

        /// <summary>
        /// Return a list of all the actions that are currently enabled in the system.
        /// </summary>
        /// <returns>A new list instance containing all currently enabled actions.</returns>
        /// <remarks>
        /// To avoid allocations, use <see cref="ListEnabledActions(List{UnityEngine.Experimental.Input.InputAction})"/>.
        /// </remarks>
        /// <seealso cref="InputAction.enabled"/>
        public static List<InputAction> ListEnabledActions()
        {
            var result = new List<InputAction>();
            ListEnabledActions(result);
            return result;
        }

        /// <summary>
        /// Add all actions that are currently enabled in the system to the given list.
        /// </summary>
        /// <param name="actions">List to add actions to.</param>
        /// <returns>The number of actions added to the list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="actions"/> is null.</exception>
        /// <remarks>
        /// If the capacity of the given list is large enough, this method will not allocate memory.
        /// </remarks>
        public static int ListEnabledActions(List<InputAction> actions)
        {
            if (actions == null)
                throw new ArgumentNullException("actions");
            return InputActionMapState.FindAllEnabledActions(actions);
        }

        #endregion

        #region Remoting

        /// <summary>
        /// The local InputRemoting instance which can mirror local input to a remote
        /// input system or can make input in a remote system available locally.
        /// </summary>
        /// <remarks>
        /// In the editor, this is always initialized. In players, this will be null
        /// if remoting is disabled (which it is by default in release players).
        /// </remarks>
        public static InputRemoting remoting
        {
            get { return s_Remote; }
        }

        #endregion

        /// <summary>
        /// The current version of the input system package.
        /// </summary>
        public static Version version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        ////TODO: put metrics gathering behind #if

        public static InputMetrics GetMetrics()
        {
            return s_Manager.metrics;
        }

        internal static InputManager s_Manager;
        internal static InputRemoting s_Remote;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        internal static RemoteInputPlayerConnection s_RemoteConnection;

        private static void SetUpRemoting()
        {
            Debug.Assert(s_Manager != null);

            s_Remote = new InputRemoting(s_Manager);

            #if UNITY_EDITOR
            // NOTE: We use delayCall as our initial startup will run in editor initialization before
            //       PlayerConnection is itself ready. If we call Bind() directly here, we won't
            //       see any errors but the callbacks we register for will not trigger.
            EditorApplication.delayCall += SetUpRemotingInternal;
            #else
            SetUpRemotingInternal();
            #endif
        }

        private static void SetUpRemotingInternal()
        {
            if (s_RemoteConnection == null)
            {
                s_RemoteConnection = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();
                #if UNITY_EDITOR
                s_RemoteConnection.Bind(EditorConnection.instance, false);
                #else
                s_RemoteConnection.Bind(PlayerConnection.instance, PlayerConnection.instance.isConnected);
                #endif
            }

            s_Remote.Subscribe(s_RemoteConnection); // Feed messages from players into editor.
            s_RemoteConnection.Subscribe(s_Remote); // Feed messages from editor into players.
        }

        #if !UNITY_EDITOR
        private static bool ShouldEnableRemoting()
        {
            ////FIXME: is there a better way to detect whether we are running tests?
            var isRunningTests = Application.productName == "UnityTestFramework";
            if (isRunningTests)
                return false; // Don't remote while running tests.
            return true;
        }

        #endif
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR

        // The rest here is internal stuff to manage singletons, survive domain reloads,
        // and to support the reset ability for tests.
        static InputSystem()
        {
            #if UNITY_EDITOR
            InitializeInEditor();
            #else
            InitializeInPlayer();
            #endif
        }

        ////FIXME: Unity is not calling this method if it's inside an #if block that is not
        ////       visible to the editor; that shouldn't be the case
        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RunInitializeInPlayer()
        {
            // We're using this method just to make sure the class constructor is called
            // so we don't need any code in here. When the engine calls this method, the
            // class constructor will be run if it hasn't been run already.

            // IL2CPP has a bug that causes the class constructor to not be run when
            // the RuntimeInitializeOnLoadMethod is invoked. So we need an explicit check
            // here until that is fixed (case 1014293).
#if !UNITY_EDITOR
            if (s_Manager == null)
                InitializeInPlayer();
#endif
        }

#if UNITY_EDITOR
        private static InputSystemObject s_SystemObject;

        private static void InitializeInEditor()
        {
            Reset();

            var existingSystemObjects = Resources.FindObjectsOfTypeAll<InputSystemObject>();
            if (existingSystemObjects != null && existingSystemObjects.Length > 0)
            {
                ////FIXME: does not preserve action map state

                // We're coming back out of a domain reload. We're restoring part of the
                // InputManager state here but we're still waiting from layout registrations
                // that happen during domain initialization.

                s_SystemObject = existingSystemObjects[0];
                s_Manager.RestoreStateWithoutDevices(s_SystemObject.systemState.managerState);
                InputDebuggerWindow.ReviveAfterDomainReload();

                // Restore remoting state.
                s_RemoteConnection = s_SystemObject.systemState.remoteConnection;
                SetUpRemoting();
                s_Remote.RestoreState(s_SystemObject.systemState.remotingState, s_Manager);

                // Get manager to restore devices on first input update. By that time we
                // should have all (possibly updated) layout information in place.
                s_Manager.m_SavedDeviceStates = s_SystemObject.systemState.managerState.devices;
                s_Manager.m_SavedAvailableDevices = s_SystemObject.systemState.managerState.availableDevices;

                // Get rid of saved state.
                s_SystemObject.systemState = new State();
            }
            else
            {
                s_SystemObject = ScriptableObject.CreateInstance<InputSystemObject>();
                s_SystemObject.hideFlags = HideFlags.HideAndDontSave;
                SetUpRemoting();
            }

            EditorApplication.playModeStateChanged += OnPlayModeChange;

            // If native backends for new input system aren't enabled, ask user whether we should
            // enable them (requires restart). We only ask once per session.
            if (!s_SystemObject.newInputBackendsCheckedAsEnabled &&
                !EditorPlayerSettings.newSystemBackendsEnabled)
            {
                const string dialogText = "This project is using the new input system package but the native platform backends for the new input system are not enabled in the player settings. " +
                    "This means that no input from native devices will come through." +
                    "\n\nDo you want to enable the backends. Doing so requires a restart of the editor.";

                if (EditorUtility.DisplayDialog("Warning", dialogText, "Yes", "No"))
                    EditorPlayerSettings.newSystemBackendsEnabled = true;
            }
            s_SystemObject.newInputBackendsCheckedAsEnabled = true;
        }

        internal static void OnPlayModeChange(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    ResetUpdateMask();
                    break;

                ////TODO: also nuke all callbacks installed on InputActions and InputActionMaps
                case PlayModeStateChange.EnteredEditMode:
                    ////REVIEW: is there any other cleanup work we want to before? should we automatically nuke
                    ////        InputDevices that have been created with AddDevice<> during play mode?
                    DisableAllEnabledActions();
                    ResetUpdateMask();
                    break;
            }
        }

        private static void ResetUpdateMask()
        {
            // Preserve before-render status as that is enabled in accordance with whether we
            // have devices that requested it.
            if ((updateMask & InputUpdateType.BeforeRender) == InputUpdateType.BeforeRender)
                updateMask = InputUpdateType.Default | InputUpdateType.BeforeRender;
            else
                updateMask = InputUpdateType.Default;
        }

#else
        private static void InitializeInPlayer()
        {
            // No domain reloads in the player so we don't need to look for existing
            // instances.
            s_Manager = new InputManager();
            s_Manager.Initialize(NativeInputRuntime.instance);

            #if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
            PerformDefaultPluginInitialization();
            #endif

            ////TODO: put this behind a switch so that it is off by default
            // Automatically enable remoting in development players.
            #if DEVELOPMENT_BUILD
            if (ShouldEnableRemoting())
                SetUpRemoting();
            #endif

            // Send an initial Update so that user methods such as Start and Awake
            // can access the input devices prior to their Update methods.
            Update();
        }

#endif // UNITY_EDITOR

#if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
        internal static void PerformDefaultPluginInitialization()
        {
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_XBOXONE || UNITY_WSA
            XInputSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_PS4 || UNITY_WSA
            DualShockSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
            HIDSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_ANDROID
            Plugins.Android.AndroidSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
            Plugins.iOS.iOSSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_SWITCH
            Plugins.Switch.SwitchSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WSA
            Plugins.XR.XRSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_TVOS || UNITY_WSA
            Plugins.OnScreen.OnScreenSupport.Initialize();
            #endif

            #if (UNITY_EDITOR || UNITY_STANDALONE) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
            Plugins.Steam.SteamSupport.Initialize();
            #endif
        }

#endif // UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION

        // For testing, we want the ability to push/pop system state even in the player.
        // However, we don't want it in release players.
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>
        /// Return the input system to its default state.
        /// </summary>
        internal static void Reset(bool enableRemoting = false, IInputRuntime runtime = null)
        {
            #if UNITY_EDITOR

            s_Manager = new InputManager();
            s_Manager.Initialize(runtime ?? NativeInputRuntime.instance);

            if (enableRemoting)
                SetUpRemoting();

            #if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
            PerformDefaultPluginInitialization();
            #endif

            #else
            InitializeInPlayer();
            #endif
        }

        /// <summary>
        /// Destroy the current setup of the input system.
        /// </summary>
        /// <remarks>
        /// NOTE: This also de-allocates data we're keeping in unmanaged memory!
        /// </remarks>
        internal static void Destroy()
        {
            // NOTE: Does not destroy InputSystemObject. We want to destroy input system
            //       state repeatedly during tests but we want to not create InputSystemObject
            //       over and over.

            InputActionMapState.ResetGlobals();
            s_Manager.Destroy();
            if (s_RemoteConnection != null)
                Object.DestroyImmediate(s_RemoteConnection);
            #if UNITY_EDITOR
            EditorInputControlLayoutCache.Clear();
            InputDeviceDebuggerWindow.s_OnToolbarGUIActions.Clear();
            #endif

            s_Manager = null;
            s_RemoteConnection = null;
            s_Remote = null;
        }

        /// <summary>
        /// Snapshot of the state used by the input system.
        /// </summary>
        /// <remarks>
        /// Can be taken across domain reloads.
        /// </remarks>
        [Serializable]
        internal struct State
        {
            [NonSerialized] public InputManager manager;
            [NonSerialized] public InputRemoting remote;
            [SerializeField] public RemoteInputPlayerConnection remoteConnection;
            [SerializeField] public InputManager.SerializedState managerState;
            [SerializeField] public InputRemoting.SerializedState remotingState;
        }

        private static Stack<State> s_SavedStateStack;

        internal static State GetSavedState()
        {
            return s_SavedStateStack.Peek();
        }

        /// <summary>
        /// Push the current state of the input system onto a stack and
        /// reset the system to its default state.
        /// </summary>
        /// <remarks>
        /// The save stack is not able to survive domain reloads. It is intended solely
        /// for use in tests.
        /// </remarks>
        internal static void SaveAndReset(bool enableRemoting = false, IInputRuntime runtime = null)
        {
            if (s_SavedStateStack == null)
                s_SavedStateStack = new Stack<State>();

            ////FIXME: does not preserve global state in InputActionMapState

            s_SavedStateStack.Push(new State
            {
                manager = s_Manager,
                remote = s_Remote,
                remoteConnection = s_RemoteConnection,
                managerState = s_Manager.SaveState(),
                remotingState = s_Remote.SaveState(),
            });

            Reset(enableRemoting, runtime ?? InputRuntime.s_Instance); // Keep current runtime.
        }

        /// <summary>
        /// Restore the state of the system from the last state pushed with <see cref="SaveAndReset"/>.
        /// </summary>
        internal static void Restore()
        {
            Debug.Assert(s_SavedStateStack != null && s_SavedStateStack.Count > 0);

            // Nuke what we have.
            Destroy();

            // Load back previous state.
            var state = s_SavedStateStack.Pop();
            s_Manager = state.manager;
            s_Remote = state.remote;
            s_RemoteConnection = state.remoteConnection;

            InputConfiguration.Restore(state.managerState.configuration);
            InputUpdate.Restore(state.managerState.updateState);

            s_Manager.InstallGlobals();
        }

#endif
    }
}
