using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Input.Haptics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Plugins.DualShock;
using UnityEngine.Experimental.Input.Plugins.HID;
using UnityEngine.Experimental.Input.Plugins.XInput;
using UnityEngine.Experimental.Input.Utilities;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Experimental.Input.Editor;
#else
using UnityEngine.Networking.PlayerConnection;
#endif

#if !(NET_4_0 || NET_4_6 || NET_STANDARD_2_0)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

////REVIEW: it'd be great to be able to set up monitors from control paths (independently of actions; or should we just use actions?)

////REVIEW: have InputSystem.onTextInput that's fired directly from the event processing loop?
////        (and allow text input events that have no associated target device? this way we don't need a keyboard to get text input)

////REVIEW: split lower-level APIs (anything mentioning events and state) off into InputSystemLowLevel API to make this API more focused?

////TODO: release native allocations when exiting

[assembly: InternalsVisibleTo("Unity.InputSystem.Tests")]

// Keep this in sync with "Packages/com.unity.inputsystem/package.json".
// NOTE: Unfortunately, System.Version doesn't use semantic versioning so we can't include
//       "-preview" suffixes here.
[assembly: AssemblyVersion("0.0.3")]

namespace UnityEngine.Experimental.Input
{
    using NotifyControlValueChangeAction = Action<InputControl, double, long>;
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
        public static event Action<string, InputControlLayoutChange> onControlLayoutChange
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
        public static void RegisterControlLayout(Type type, string name = null, InputDeviceMatcher? matches = null)
        {
            if (string.IsNullOrEmpty(name))
                name = type.Name;

            s_Manager.RegisterControlLayout(name, type, matches);
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
        public static void RegisterControlLayout<T>(string name = null, InputDeviceMatcher? matches = null)
            where T : InputControl
        {
            RegisterControlLayout(typeof(T), name, matches);
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
        /// InputSystem.RegisterControlLayout(@"
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
        public static void RegisterControlLayout(string json, string name = null, InputDeviceMatcher? matches = null)
        {
            s_Manager.RegisterControlLayout(json, name, matcher: matches);
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
        public static void RegisterControlLayoutOverride(string json, string name = null)
        {
            s_Manager.RegisterControlLayout(json, name, isOverride: true);
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
        /// InputSystem.RegisterControlLayoutBuilder(() => builder.Build(), "MyLayout");
        /// </code>
        /// </example>
        public static void RegisterControlLayoutBuilder(Expression<Func<InputControlLayout>> builderExpression, string name,
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
            s_Manager.RegisterControlLayoutBuilder(method, instance, name, baseLayout: baseLayout,
                deviceMatcher: matches);
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
        public static void RemoveControlLayout(string name)
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
        /// var layoutName = InputSystem.TryFindMatchingControlLayout(
        ///     new InputDeviceDescription
        ///     {
        ///         product = "Xbox Wired Controller",
        ///         manufacturer = "Microsoft"
        ///     }
        /// );
        /// </code>
        /// </example>
        public static string TryFindMatchingControlLayout(InputDeviceDescription deviceDescription)
        {
            return s_Manager.TryFindMatchingControlLayout(ref deviceDescription);
        }

        /// <summary>
        /// Return a list with the names of all layouts that have been registered.
        /// </summary>
        /// <returns>A list of layout names.</returns>
        /// <seealso cref="ListControlLayouts"/>
        public static List<string> ListControlLayouts()
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
        public static int ListControlLayouts(List<string> list)
        {
            return s_Manager.ListControlLayouts(list);
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
        /// <param name="name">Name to use for the process. If null or empty, name will be taken from short name
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
        /// The value return by this property should not be held on to. When the device
        /// setup in the system changes, any value previously returned by this property
        /// becomes invalid. Query the property directly whenever you need it.
        /// </remarks>
        public static ReadOnlyArray<InputDevice> devices
        {
            get { return s_Manager.devices; }
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
        /// InputSystem.onFindControlLayoutForDevice +=
        ///     (deviceId, description, matchedLayout, runtime) =>
        ///     {
        ///         ////TODO: complete example
        ///     };
        /// </code>
        /// </example>
        public static event DeviceFindControlLayoutCallback onFindControlLayoutForDevice
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

        public static InputDevice TryGetDevice(string nameOrLayout)
        {
            return s_Manager.TryGetDevice(nameOrLayout);
        }

        public static InputDevice GetDevice(string nameOrLayout)
        {
            return s_Manager.GetDevice(nameOrLayout);
        }

        public static InputDevice TryGetDeviceById(int deviceId)
        {
            return s_Manager.TryGetDeviceById(deviceId);
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

        public static void SetUsage(InputDevice device, string usage)
        {
            SetUsage(device, new InternedString(usage));
        }

        // May generate garbage.
        public static void SetUsage(InputDevice device, InternedString usage)
        {
            s_Manager.SetUsage(device, usage);
        }

        public static void AddUsage(InputDevice device, InternedString usage)
        {
            throw new NotImplementedException();
        }

        public static void RemoveUsage(InputDevice device, InternedString usage)
        {
            throw new NotImplementedException();
        }

        public static List<InputControl> GetControls(string path)
        {
            var list = new List<InputControl>();
            GetControls(path, list);
            return list;
        }

        public static int GetControls(string path, List<InputControl> controls)
        {
            var wrapper = new ArrayOrListWrapper<InputControl>(controls);
            return GetControls(path, ref wrapper);
        }

        internal static int GetControls(string path, ref ArrayOrListWrapper<InputControl> controls)
        {
            return s_Manager.GetControls(path, ref controls);
        }

        #endregion

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

            public void NotifyControlValueChanged(InputControl control, double time, long monitorIndex)
            {
                valueChangeCallback(control, time, monitorIndex);
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
                stateOffset = control.m_StateBlock.byteOffset
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
        public static event Action<InputUpdateType> onUpdate
        {
            add { s_Manager.onUpdate += value; }
            remove { s_Manager.onUpdate -= value; }
        }

        #endregion

        #region Actions

        public static void RegisterInteraction(Type type, string name)
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
            get
            {
                if (s_Remote == null && s_Manager != null)
                {
                    #if UNITY_EDITOR
                    s_Remote = s_SystemObject.remote;
                    #endif
                }
                return s_Remote;
            }
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
            var existingSystemObjects = Resources.FindObjectsOfTypeAll<InputSystemObject>();
            if (existingSystemObjects != null && existingSystemObjects.Length > 0)
            {
                s_SystemObject = existingSystemObjects[0];
                s_SystemObject.ReviveAfterDomainReload();
                s_Manager = s_SystemObject.manager;
                s_Remote = s_SystemObject.remote;
                #if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
                PerformDefaultPluginInitialization();
                #endif
                InputDebuggerWindow.ReviveAfterDomainReload();
            }
            else
            {
                Reset();
            }

            EditorApplication.playModeStateChanged += OnPlayModeChange;

            // If native backends for new input system aren't enabled, ask user whether we should
            // enable them (requires restart). We only ask once per session.
            if (!s_SystemObject.newInputBackendsCheckedAsEnabled &&
                !EditorPlayerSettings.newSystemBackendsEnabled)
            {
                const string dialogText = "This project is using the new input system package but the native platform backends for the new input system are not enabled in the player settings." +
                    "This means that no input from native devices will come through." +
                    "\n\nDo you want to enable the backends. Doing so requires a restart of the editor.";

                if (EditorUtility.DisplayDialog("Warning", dialogText, "Yes", "No"))
                    EditorPlayerSettings.newSystemBackendsEnabled = true;
            }
            s_SystemObject.newInputBackendsCheckedAsEnabled = true;
        }

        // We don't want play mode modifications to layouts and controls to seep
        // back out into edit so we take a snapshot of the InputManager state before
        // going into play mode and then restore it when going back to edit mode.
        // NOTE: We *do* want device discoveries that have happened to still show
        //       through in edit mode, though not with any layout settings made by
        //       the game code.
        internal static void OnPlayModeChange(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    Save();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    Restore();
                    DisableAllEnabledActions();
                    break;
            }
        }

#else
        #if DEVELOPMENT_BUILD
        private static RemoteInputPlayerConnection s_ConnectionToEditor;
        #endif

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
            {
                s_ConnectionToEditor = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();
                s_Remote = new InputRemoting(s_Manager, startSendingOnConnect: true);
                s_Remote.Subscribe(s_ConnectionToEditor);
                s_ConnectionToEditor.Subscribe(s_Remote);
                s_ConnectionToEditor.Bind(PlayerConnection.instance, PlayerConnection.instance.isConnected);
            }
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

#endif

        // For testing, we want the ability to push/pop system state even in the player.
        // However, we don't want it in release players.
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        internal static void Reset()
        {
            #if UNITY_EDITOR
            if (s_SystemObject != null)
                Object.DestroyImmediate(s_SystemObject);
            s_SystemObject = ScriptableObject.CreateInstance<InputSystemObject>();
            s_Manager = s_SystemObject.manager;
            s_Remote = s_SystemObject.remote;
            #if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
            PerformDefaultPluginInitialization();
            #endif
            #else
            if (s_Manager != null)
                s_Manager.Destroy();
            ////TODO: reset remote
            InitializeInPlayer();
            #endif
        }

        private static List<InputManager.SerializedState> s_SerializedStateStack;

        ////REVIEW: what should we do with the remote here?

        internal static void Save()
        {
            if (s_SerializedStateStack == null)
                s_SerializedStateStack = new List<InputManager.SerializedState>();
            s_SerializedStateStack.Add(s_Manager.SaveState());
        }

        internal static void Restore()
        {
            if (s_SerializedStateStack != null && s_SerializedStateStack.Count > 0)
            {
                // This is a little contrived. Expected behavior would be that InputManager.Destroy()
                // will also take all devices down and release allocate state buffers. However, if we do
                // that, then InputManager.SaveState() would have to duplicate state buffer memory and
                // every time we'd have a domain reload, we'd needlessy duplicate and destroy input state
                // in native -- which is just adding yet more stuff to an operation that already takes
                // way too long.
                //
                // So, instead we do *NOT* release state buffers when we do a Reset(). Meaning that the
                // saved state we store in s_SerializedStateStack uses whatever the InputManager owned.
                // However, when restoring, we do want to get rid of whatever the InputManager currently
                // holds on to, so we flush things out here.
                if (s_Manager.devices.Count > 0)
                    s_Manager.m_StateBuffers.FreeAll();

                Reset();

                // Load back previous state.
                var index = s_SerializedStateStack.Count - 1;
                s_Manager.RestoreState(s_SerializedStateStack[index]);
                s_SerializedStateStack.RemoveAt(index);
            }
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

#endif
    }
}
