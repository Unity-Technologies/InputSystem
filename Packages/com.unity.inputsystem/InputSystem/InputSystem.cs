using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem.Haptics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.XInput;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Profiling;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEditor.Networking.PlayerConnection;
#else
using System.Linq;
using UnityEngine.Networking.PlayerConnection;
#endif

#if UNITY_EDITOR
using CustomBindingPathValidator = System.Func<string, System.Action>;
#endif

////TODO: allow aliasing processors etc

////REVIEW: rename all references to "frame" to refer to "update" instead (e.g. wasPressedThisUpdate)?

////TODO: add APIs to get to the state blocks (equivalent to what you currently get with e.g. InputSystem.devices[0].currentStatePtr)

////FIXME: modal dialogs (or anything that interrupts normal Unity operation) are likely a problem for the system as is; there's a good
////       chance the event queue will just get swamped; should be only the background queue though so I guess once it fills up we
////       simply start losing input but it won't grow infinitely

////REVIEW: make more APIs thread-safe?

////REVIEW: it'd be great to be able to set up monitors from control paths (independently of actions; or should we just use actions?)

////REVIEW: have InputSystem.onTextInput that's fired directly from the event processing loop?
////        (and allow text input events that have no associated target device? this way we don't need a keyboard to get text input)

////REVIEW: split lower-level APIs (anything mentioning events and state) off into InputSystemLowLevel API to make this API more focused?

////TODO: release native allocations when exiting

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// This is the central hub for the input system.
    /// </summary>
    /// <remarks>
    /// This class has the central APIs for working with the input system. You
    /// can manage devices available in the system (<see cref="AddDevice{TDevice}"/>,
    /// <see cref="devices"/>, <see cref="onDeviceChange"/> and related APIs) or extend
    /// the input system with custom functionality (<see cref="RegisterLayout{TLayout}"/>,
    /// <see cref="RegisterInteraction{T}"/>, <see cref="RegisterProcessor{T}"/>,
    /// <see cref="RegisterBindingComposite{T}"/>, and related APIs).
    ///
    /// To control haptics globally, you can use <see cref="PauseHaptics"/>, <see cref="ResumeHaptics"/>,
    /// and <see cref="ResetHaptics"/>.
    ///
    /// To enable and disable individual devices (such as <see cref="Sensor"/> devices),
    /// you can use <see cref="EnableDevice"/> and <see cref="DisableDevice"/>.
    ///
    /// The input system is initialized as part of Unity starting up. It is generally safe
    /// to call the APIs here from any of Unity's script callbacks.
    ///
    /// Note that, like most Unity APIs, most of the properties and methods in this API can only
    /// be called on the main thread. However, select APIs like <see cref="QueueEvent"/> can be
    /// called from threads. Where this is the case, it is stated in the documentation.
    /// </remarks>

    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces", Justification = "Options for namespaces are limited due to the legacy input class. Agreed on this as the least bad solution.")]
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif

    public static partial class InputSystem
    {
        #region Layouts

        /// <summary>
        /// Event that is signalled when the layout setup in the system changes.
        /// </summary>
        /// <remarks>
        /// First parameter is the name of the layout that has changed and second parameter is the
        /// type of change that has occurred.
        ///
        /// <example>
        /// <code>
        /// InputSystem.onLayoutChange +=
        ///     (name, change) =>
        ///     {
        ///         switch (change)
        ///         {
        ///             case InputControlLayoutChange.Added:
        ///                 Debug.Log($"New layout {name} has been added");
        ///                 break;
        ///             case InputControlLayoutChange.Removed:
        ///                 Debug.Log($"Layout {name} has been removed");
        ///                 break;
        ///             case InputControlLayoutChange.Replaced:
        ///                 Debug.Log($"Layout {name} has been updated");
        ///                 break;
        ///         }
        ///     }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputControlLayout"/>
        public static event Action<string, InputControlLayoutChange> onLayoutChange
        {
            add
            {
                lock (s_Manager)
                    s_Manager.onLayoutChange += value;
            }
            remove
            {
                lock (s_Manager)
                    s_Manager.onLayoutChange -= value;
            }
        }

        /// <summary>
        /// Register a control layout based on a type.
        /// </summary>
        /// <param name="type">Type to derive a control layout from. Must be derived from <see cref="InputControl"/>.</param>
        /// <param name="name">Name to use for the layout. If null or empty, the short name of the type (<c>Type.Name</c>) will be used.</param>
        /// <param name="matches">Optional device matcher. If this is supplied, the layout will automatically
        /// be instantiated for newly discovered devices that match the description.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        /// <remarks>
        /// When the layout is instantiated, the system will reflect on all public fields and properties of the type
        /// which have a value type derived from <see cref="InputControl"/> or which are annotated with <see cref="InputControlAttribute"/>.
        ///
        /// The type can be annotated with <see cref="InputControlLayoutAttribute"/> for additional options
        /// but the attribute is not necessary for a type to be usable as a control layout. Note that if the type
        /// does have <see cref="InputControlLayoutAttribute"/> and has set <see cref="InputControlLayoutAttribute.stateType"/>,
        /// the system will <em>not</em> reflect on properties and fields in the type but do that on the given
        /// state type instead.
        ///
        /// <example>
        /// <code>
        /// // InputControlLayoutAttribute attribute is only necessary if you want
        /// // to override default behavior that occurs when registering your device
        /// // as a layout.
        /// // The most common use of InputControlLayoutAttribute is to direct the system
        /// // to a custom "state struct" through the `stateType` property. See below for details.
        /// [InputControlLayout(displayName = "My Device", stateType = typeof(MyDeviceState))]
        /// #if UNITY_EDITOR
        /// [InitializeOnLoad]
        /// #endif
        /// public class MyDevice : InputDevice
        /// {
        ///     public ButtonControl button { get; private set; }
        ///     public AxisControl axis { get; private set; }
        ///
        ///     // Register the device.
        ///     static MyDevice()
        ///     {
        ///         // In case you want instance of your device to automatically be created
        ///         // when specific hardware is detected by the Unity runtime, you have to
        ///         // add one or more "device matchers" (InputDeviceMatcher) for the layout.
        ///         // These matchers are compared to an InputDeviceDescription received from
        ///         // the Unity runtime when a device is connected. You can add them either
        ///         // using InputSystem.RegisterLayoutMatcher() or by directly specifying a
        ///         // matcher when registering the layout.
        ///         InputSystem.RegisterLayout&lt;MyDevice&gt;(
        ///             // For the sake of demonstration, let's assume your device is a HID
        ///             // and you want to match by PID and VID.
        ///             matches: new InputDeviceMatcher()
        ///                 .WithInterface("HID")
        ///                 .WithCapability("PID", 1234)
        ///                 .WithCapability("VID", 5678));
        ///     }
        ///
        ///     // This is only to trigger the static class constructor to automatically run
        ///     // in the player.
        ///     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        ///     private static void InitializeInPlayer() {}
        ///
        ///     protected override void FinishSetup()
        ///     {
        ///         base.FinishSetup();
        ///         button = GetChildControl&lt;ButtonControl&gt;("button");
        ///         axis = GetChildControl&lt;AxisControl&gt;("axis");
        ///     }
        /// }
        ///
        /// // A "state struct" describes the memory format used by a device. Each device can
        /// // receive and store memory in its custom format. InputControls are then connected
        /// // the individual pieces of memory and read out values from them.
        /// [StructLayout(LayoutKind.Explicit, Size = 32)]
        /// public struct MyDeviceState : IInputStateTypeInfo
        /// {
        ///     // In the case of a HID (which we assume for the sake of this demonstration),
        ///     // the format will be "HID". In practice, the format will depend on how your
        ///     // particular device is connected and fed into the input system.
        ///     // The format is a simple FourCC code that "tags" state memory blocks for the
        ///     // device to give a base level of safety checks on memory operations.
        ///     public FourCC format => return new FourCC('H', 'I', 'D');
        ///
        ///     // InputControlAttributes on fields tell the input system to create controls
        ///     // for the public fields found in the struct.
        ///
        ///     // Assume a 16bit field of buttons. Create one button that is tied to
        ///     // bit #3 (zero-based). Note that buttons do not need to be stored as bits.
        ///     // They can also be stored as floats or shorts, for example.
        ///     [InputControl(name = "button", layout = "Button", bit = 3)]
        ///     public ushort buttons;
        ///
        ///     // Create a floating-point axis. The name, if not supplied, is taken from
        ///     // the field.
        ///     [InputControl(layout = "Axis")]
        ///     public short axis;
        /// }
        /// </code>
        /// </example>
        ///
        /// Note that if <paramref name="matches"/> is supplied, it will immediately be matched
        /// against the descriptions (<see cref="InputDeviceDescription"/>) of all available devices.
        /// If it matches any description where no layout matched before, a new device will immediately
        /// be created (except if suppressed by <see cref="InputSettings.supportedDevices"/>). If it
        /// matches a description better (see <see cref="InputDeviceMatcher.MatchPercentage"/>) than
        /// the currently used layout, the existing device will be a removed and a new device with
        /// the newly registered layout will be created.
        ///
        /// See <see cref="Controls.StickControl"/> or <see cref="Gamepad"/> for examples of layouts.
        /// </remarks>
        /// <seealso cref="InputControlLayout"/>
        public static void RegisterLayout(Type type, string name = null, InputDeviceMatcher? matches = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

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
        /// <param name="matches">Optional device matcher. If this is supplied, the layout will automatically
        /// be instantiated for newly discovered devices that match the description.</param>
        /// <remarks>
        /// This method is equivalent to calling <see cref="RegisterLayout(Type,string,InputDeviceMatcher?)"/> with
        /// <c>typeof(T)</c>. See that method for details of the layout registration process.
        /// </remarks>
        /// <seealso cref="RegisterLayout(Type,string,InputDeviceMatcher?)"/>
        public static void RegisterLayout<T>(string name = null, InputDeviceMatcher? matches = null)
            where T : InputControl
        {
            RegisterLayout(typeof(T), name, matches);
        }

        /// <summary>
        /// Register a layout in JSON format.
        /// </summary>
        /// <param name="json">JSON data describing the layout.</param>
        /// <param name="name">Optional name of the layout. If null or empty, the name is taken from the "name"
        /// property of the JSON data. If it is supplied, it will override the "name" property if present. If neither
        /// is supplied, an <see cref="ArgumentException"/> is thrown.</param>
        /// <param name="matches">Optional device matcher. If this is supplied, the layout will automatically
        /// be instantiated for newly discovered devices that match the description.</param>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is null or empty.</exception>
        /// <exception cref="ArgumentException">No name has been supplied either through <paramref name="name"/>
        /// or the "name" JSON property.</exception>
        /// <remarks>
        /// The JSON format makes it possible to create new device and control layouts completely
        /// in data. They have to ultimately be based on a layout backed by a C# type, however (e.g.
        /// <see cref="Gamepad"/>).
        ///
        /// Note that most errors in layouts will only be detected when instantiated (i.e. when a device or control is
        /// being created from a layout). The JSON data will, however, be parsed once on registration to check for a
        /// device description in the layout. JSON format errors will thus be detected during registration.
        ///
        /// <example>
        /// <code>
        /// InputSystem.RegisterLayout(@"
        ///    {
        ///        ""name"" : ""MyDevice"",
        ///        ""controls"" : [
        ///            {
        ///                ""name"" : ""myButton"",
        ///                ""layout"" : ""Button""
        ///            }
        ///        ]
        ///    }
        /// );
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="RemoveLayout"/>
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
        /// The layout merging logic used for overrides, is the same as the one used for
        /// derived layouts, i.e. <see cref="InputControlLayout.MergeLayout"/>.
        ///
        /// Layouts used as overrides look the same as normal layouts and have the same format.
        /// The only difference is that they are explicitly registered as overrides.
        ///
        /// Note that unlike "normal" layouts, layout overrides have the ability to extend
        /// multiple base layouts. The changes from the override will simply be merged into
        /// each of the layouts it extends. Use the <c>extendMultiple</c> rather than the
        /// <c>extend</c> property in JSON to give a list of base layouts instead of a single
        /// one.
        ///
        /// <example>
        /// <code>
        /// // Override default button press points on the gamepad triggers.
        /// InputSystem.RegisterLayoutOverride(@"
        ///     {
        ///         ""name"" : ""CustomTriggerPressPoints"",
        ///         ""extend"" : ""Gamepad"",
        ///         ""controls"" : [
        ///             { ""name"" : ""leftTrigger"", ""parameters"" : ""pressPoint=0.25"" },
        ///             { ""name"" : ""rightTrigger"", ""parameters"" : ""pressPoint=0.25"" }
        ///         ]
        ///     }
        /// ");
        /// </code>
        /// </example>
        /// </remarks>
        public static void RegisterLayoutOverride(string json, string name = null)
        {
            s_Manager.RegisterControlLayout(json, name, isOverride: true);
        }

        /// <summary>
        /// Add an additional device matcher to an existing layout.
        /// </summary>
        /// <param name="layoutName">Name of the device layout that should be instantiated if <paramref name="matcher"/>
        /// matches an <see cref="InputDeviceDescription"/> of a discovered device.</param>
        /// <param name="matcher">Specification to match against <see cref="InputDeviceDescription"/> instances.</param>
        /// <remarks>
        /// Each device layout can have zero or more matchers associated with it. If any one of the
        /// matchers matches a given <see cref="InputDeviceDescription"/> (see <see cref="InputDeviceMatcher.MatchPercentage"/>)
        /// better than any other matcher (for the same or any other layout), then the given layout
        /// will be used for the discovered device.
        ///
        /// Note that registering a matcher may immediately lead to devices being created or recreated.
        /// If <paramref name="matcher"/> matches any devices currently on the list of unsupported devices
        /// (see <see cref="GetUnsupportedDevices()"/>), new <see cref="InputDevice"/>s will be created
        /// using the layout called <paramref name="layoutName"/>. Also, if <paramref name="matcher"/>
        /// matches the description of a device better than the matcher (if any) for the device's currently
        /// used layout, the device will be recreated using the given layout.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="layoutName"/> is <c>null</c> or empty/</exception>
        /// <exception cref="ArgumentException"><paramref name="matcher"/> is empty (<see cref="InputDeviceMatcher.empty"/>).</exception>
        /// <seealso cref="RegisterLayout(Type,string,InputDeviceMatcher?)"/>
        /// <seealso cref="TryFindMatchingLayout"/>
        public static void RegisterLayoutMatcher(string layoutName, InputDeviceMatcher matcher)
        {
            s_Manager.RegisterControlLayoutMatcher(layoutName, matcher);
        }

        /// <summary>
        /// Add an additional device matcher to the layout registered for <typeparamref name="TDevice"/>.
        /// </summary>
        /// <param name="matcher">A device matcher.</param>
        /// <typeparam name="TDevice">Type that has been registered as a layout. See <see cref="RegisterLayout{T}"/>.</typeparam>
        /// <remarks>
        /// Calling this method is equivalent to calling <see cref="RegisterLayoutMatcher(string,InputDeviceMatcher)"/>
        /// with the name under which <typeparamref name="TDevice"/> has been registered.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="matcher"/> is empty (<see cref="InputDeviceMatcher.empty"/>)
        /// -or- <typeparamref name="TDevice"/> has not been registered as a layout.</exception>
        public static void RegisterLayoutMatcher<TDevice>(InputDeviceMatcher matcher)
            where TDevice : InputDevice
        {
            s_Manager.RegisterControlLayoutMatcher(typeof(TDevice), matcher);
        }

        /// <summary>
        /// Register a builder that delivers an <see cref="InputControlLayout"/> instance on demand.
        /// </summary>
        /// <param name="buildMethod">Method to invoke to generate a layout when the layout is chosen.
        /// Should not cache the layout but rather return a fresh instance every time.</param>
        /// <param name="name">Name under which to register the layout. If a layout with the same
        /// name is already registered, the call to this method will replace the existing layout.</param>
        /// <param name="baseLayout">Name of the layout that the layout returned from <paramref name="buildMethod"/>
        /// will be based on. The system needs to know this in advance in order to update devices
        /// correctly if layout registrations in the system are changed.</param>
        /// <param name="matches">Optional matcher for an <see cref="InputDeviceDescription"/>. If supplied,
        /// it is equivalent to calling <see cref="RegisterLayoutMatcher"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buildMethod"/> is <c>null</c> -or-
        /// <paramref name="name"/> is <c>null</c> or empty.</exception>
        /// <remarks>
        /// Layout builders are most useful for procedurally building device layouts from metadata
        /// supplied by external systems. A good example is <see cref="HID"/> where the "HID" standard
        /// includes a way for input devices to describe their various inputs and outputs in the form
        /// of a <see cref="UnityEngine.InputSystem.HID.HID.HIDDeviceDescriptor"/>. While not sufficient to build a perfectly robust
        /// <see cref="InputDevice"/>, these descriptions are usually enough to at least make the device
        /// work out-of-the-box to some extent.
        ///
        /// The builder method would usually use <see cref="InputControlLayout.Builder"/> to build the
        /// actual layout.
        ///
        /// <example>
        /// <code>
        /// InputSystem.RegisterLayoutBuilder(
        ///     () =>
        ///     {
        ///         var builder = new InputControlLayout.Builder()
        ///             .WithType&lt;MyDevice&gt;();
        ///         builder.AddControl("button1").WithLayout("Button");
        ///         return builder.Build();
        ///     }, "MyCustomLayout"
        /// }
        /// </code>
        /// </example>
        ///
        /// Layout builders can be used in combination with <see cref="onFindLayoutForDevice"/> to
        /// build layouts dynamically for devices as they are connected to the system.
        ///
        /// Be aware that the same builder <em>must</em> not build different layouts. Each
        /// layout registered in the system is considered to be immutable for as long as it
        /// is registered. So, if a layout builder is registered under the name "Custom", for
        /// example, then every time the builder is invoked, it must return the same identical
        /// <see cref="InputControlLayout"/>.
        /// </remarks>
        /// <seealso cref="InputControlLayout.Builder"/>
        /// <seealso cref="InputSystem.onFindLayoutForDevice"/>
        public static void RegisterLayoutBuilder(Func<InputControlLayout> buildMethod, string name,
            string baseLayout = null, InputDeviceMatcher? matches = null)
        {
            if (buildMethod == null)
                throw new ArgumentNullException(nameof(buildMethod));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            s_Manager.RegisterControlLayoutBuilder(buildMethod, name, baseLayout: baseLayout);
            if (matches != null)
                s_Manager.RegisterControlLayoutMatcher(name, matches.Value);
        }

        /// <summary>
        /// Register a "baked" version of a device layout.
        /// </summary>
        /// <typeparam name="TDevice">C# class that represents the precompiled version of the device layout that the
        /// class is derived from.</typeparam>
        /// <param name="metadata">Metadata automatically generated for the precompiled layout.</param>
        /// <remarks>
        /// This method is used to register device implementations for which their layout has been "baked" into
        /// a C# class. To generate such a class, right-click a device layout in the input debugger and select
        /// "Generate Precompiled Layout". This generates a C# file containing a class that represents the precompiled
        /// version of the device layout. The class can be registered using this method.
        ///
        /// Note that registering a precompiled layout will not implicitly register the "normal" version of the layout.
        /// In other words, <see cref="RegisterLayout{TDevice}"/> must be called before calling this method.
        ///
        /// <example>
        /// <code>
        /// // Register the non-precompiled, normal version of the layout.
        /// InputSystem.RegisterLayout&lt;MyDevice&gt;();
        ///
        /// // Register a precompiled version of the layout.
        /// InputSystem.RegisterPrecompiledLayout&lt;PrecompiledMyDevice&gt;(PrecompiledMyDevice.metadata);
        ///
        /// // This implicitly uses the precompiled version.
        /// InputSystem.AddDevice&lt;MyDevice&gt;();
        /// </code>
        /// </example>
        ///
        /// The main advantage of precompiled layouts is that instantiating them is many times faster than the default
        /// device creation path. By default, when creating an <see cref="InputDevice"/>, the system will have to load
        /// the <see cref="InputControlLayout"/> for the device as well as any layouts used directly or indirectly by
        /// that layout. This in itself is a slow process that generates GC heap garbage and uses .NET reflection (which
        /// itself may add additional permanent data to the GC heap). In addition, interpreting the layouts to construct
        /// an <see cref="InputDevice"/> and populate it with <see cref="InputControl"/> children is not a fast process.
        ///
        /// A precompiled layout, however, has all necessary construction steps "baked" into the generated code. It will
        /// not use reflection and will generally generate little to no GC heap garbage.
        ///
        /// A precompiled layout derives from the C# device class whose layout is "baked". If, for example, you generate
        /// a precompiled version for <see cref="Keyboard"/>, the resulting class will be derived from <see cref="Keyboard"/>.
        /// When registering the precompiled layout. If someone afterwards creates a <see cref="Keyboard"/>, the precompiled
        /// version will implicitly be instantiated and thus skips the default device creation path that will construct
        /// a <see cref="Keyboard"/> device from an <see cref="InputControlLayout"/> (it will thus not require the
        /// <see cref="Keyboard"/> layout or any other layout it depends on to be loaded).
        ///
        /// Note that when layout overrides (see <see cref="RegisterLayoutOverride"/>) or new versions of
        /// existing layouts are registered (e.g. if you replace the built-in "Button" layout by registering
        /// a new layout with that name), precompiled layouts affected by the change will automatically be
        /// <em>removed</em>. This causes the system to fall back to the default device creation path which can
        /// take runtime layout changes into account.
        /// </remarks>
        public static void RegisterPrecompiledLayout<TDevice>(string metadata)
            where TDevice : InputDevice, new()
        {
            s_Manager.RegisterPrecompiledLayout<TDevice>(metadata);
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
        /// This method performs the same matching process that is invoked if a device is reported
        /// by the Unity runtime or using <see cref="AddDevice(InputDeviceDescription)"/>. The result
        /// depends on the matches (<see cref="InputDeviceMatcher"/>) registered for the device
        /// layout in the system.
        ///
        /// <example>
        /// <code>
        /// var layoutName = InputSystem.TryFindMatchingLayout(
        ///     new InputDeviceDescription
        ///     {
        ///         interface = "XInput",
        ///         product = "Xbox Wired Controller",
        ///         manufacturer = "Microsoft"
        ///     }
        /// );
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="RegisterLayoutMatcher{TDevice}"/>
        /// <seealso cref="RegisterLayoutMatcher(string,InputDeviceMatcher)"/>
        public static string TryFindMatchingLayout(InputDeviceDescription deviceDescription)
        {
            return s_Manager.TryFindMatchingControlLayout(ref deviceDescription);
        }

        /// <summary>
        /// Return a list with the names of all layouts that have been registered.
        /// </summary>
        /// <returns>A list of layout names.</returns>
        /// <seealso cref="LoadLayout"/>
        /// <seealso cref="ListLayoutsBasedOn"/>
        /// <seealso cref="RegisterLayout(System.Type,string,Nullable{InputDeviceMatcher})"/>
        public static IEnumerable<string> ListLayouts()
        {
            return s_Manager.ListControlLayouts();
        }

        /// <summary>
        /// List all the layouts that are based on the given layout.
        /// </summary>
        /// <param name="baseLayout">Name of a registered layout.</param>
        /// <exception cref="ArgumentNullException"><paramref name="baseLayout"/> is <c>null</c> or empty.</exception>
        /// <returns>The names of all registered layouts based on <paramref name="baseLayout"/>.</returns>
        /// <remarks>
        /// The list will not include layout overrides (see <see cref="RegisterLayoutOverride"/>).
        ///
        /// <example>
        /// <code>
        /// // List all gamepad layouts in the system.
        /// Debug.Log(string.Join("\n", InputSystem.ListLayoutsBasedOn("Gamepad"));
        /// </code>
        /// </example>
        /// </remarks>
        public static IEnumerable<string> ListLayoutsBasedOn(string baseLayout)
        {
            if (string.IsNullOrEmpty(baseLayout))
                throw new ArgumentNullException(nameof(baseLayout));
            return s_Manager.ListControlLayouts(basedOn: baseLayout);
        }

        ////TODO: allow loading an *unmerged* layout
        /// <summary>
        /// Load a registered layout.
        /// </summary>
        /// <param name="name">Name of the layout to load. Note that layout names are case-insensitive.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c> or empty.</exception>
        /// <returns>The constructed layout instance or <c>null</c> if no layout of the given name could be found.</returns>
        /// <remarks>
        /// The result of this method is what's called a "fully merged" layout, i.e. a layout with
        /// the information of all the base layouts as well as from all overrides merged into it. See
        /// <see cref="InputControlLayout.MergeLayout"/> for details.
        ///
        /// What this means in practice is that all inherited controls and settings will be present
        /// on the layout.
        ///
        /// <example>
        /// // List all controls defined for gamepads.
        /// var gamepadLayout = InputSystem.LoadLayout("Gamepad");
        /// foreach (var control in gamepadLayout.controls)
        /// {
        ///     // There may be control elements that are not introducing new controls but rather
        ///     // change settings on controls added indirectly by other layouts referenced from
        ///     // Gamepad. These are not adding new controls so we skip them here.
        ///     if (control.isModifyingExistingControl)
        ///         continue;
        ///
        ///     Debug.Log($"Control: {control.name} ({control.layout])");
        /// }
        /// </example>
        ///
        /// However, note that controls which are added from other layouts referenced by the loaded layout
        /// will not necessarily be visible on it (they will only if referenced by a <see cref="InputControlLayout.ControlItem"/>
        /// where <see cref="InputControlLayout.ControlItem.isModifyingExistingControl"/> is <c>true</c>).
        /// For example, let's assume we have the following layout which adds a device with a single stick.
        ///
        /// <example>
        /// <code>
        /// InputSystem.RegisterLayout(@"
        ///     {
        ///         ""name"" : ""DeviceWithStick"",
        ///         ""controls"" : [
        ///             { ""name"" : ""stick"", ""layout"" : ""Stick"" }
        ///         ]
        ///     }
        /// ");
        /// </code>
        /// </example>
        ///
        /// If we load this layout, the <c>"stick"</c> control will be visible on the layout but the
        /// X and Y (as well as up/down/left/right) controls added by the <c>"Stick"</c> layout will
        /// not be.
        /// </remarks>
        /// <seealso cref="RegisterLayout(Type,string,Nullable{InputDeviceMatcher})"/>
        public static InputControlLayout LoadLayout(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            ////FIXME: this will intern the name even if the operation fails
            return s_Manager.TryLoadControlLayout(new InternedString(name));
        }

        /// <summary>
        /// Load the layout registered for the given type.
        /// </summary>
        /// <typeparam name="TControl">An InputControl type.</typeparam>
        /// <returns>The layout registered for <typeparamref name="TControl"/> or <c>null</c> if no
        /// such layout exists.</returns>
        /// <remarks>
        /// This method is equivalent to calling <see cref="LoadLayout(string)"/> with the name
        /// of the layout under which <typeparamref name="TControl"/> has been registered.
        ///
        /// <example>
        /// <code>
        /// // Load the InputControlLayout generated from StickControl.
        /// var stickLayout = InputSystem.LoadLayout&lt;StickControl&gt;();
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="LoadLayout(string)"/>
        public static InputControlLayout LoadLayout<TControl>()
            where TControl : InputControl
        {
            return s_Manager.TryLoadControlLayout(typeof(TControl));
        }

        /// <summary>
        /// Return the name of the layout that the layout registered as <paramref name="layoutName"/>
        /// is based on.
        /// </summary>
        /// <param name="layoutName">Name of a layout as registered with a method such as <see
        /// cref="RegisterLayout{T}(string,InputDeviceMatcher?)"/>. Case-insensitive.</param>
        /// <returns>Name of the immediate parent layout of <paramref name="layoutName"/> or <c>null</c> if no layout
        /// with the given name is registered or if it is not based on another layout or if it is a layout override.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="layoutName"/> is <c>null</c> or empty.</exception>
        /// <remarks>
        /// This method does not work for layout overrides (which can be based on multiple base layouts). To find
        /// out which layouts a specific override registered with <see cref="RegisterLayoutOverride"/> is based on,
        /// load the layout with <see cref="LoadLayout"/> and inspect <see cref="InputControlLayout.baseLayouts"/>.
        /// This method will return <c>null</c> when <paramref name="layoutName"/> is the name of a layout override.
        ///
        /// One advantage of this method over calling <see cref="LoadLayout"/> and looking at <see cref="InputControlLayout.baseLayouts"/>
        /// is that this method does not have to actually load the layout but instead only performs a simple lookup.
        ///
        /// <example>
        /// <code>
        /// // Prints "Pointer".
        /// Debug.Log(InputSystem.GetNameOfBaseLayout("Mouse"));
        ///
        /// // Also works for control layouts. Prints "Axis".
        /// Debug.Log(InputSystem.GetNameOfBaseLayout("Button"));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputControlLayout.baseLayouts"/>
        public static string GetNameOfBaseLayout(string layoutName)
        {
            if (string.IsNullOrEmpty(layoutName))
                throw new ArgumentNullException(nameof(layoutName));

            var internedLayoutName = new InternedString(layoutName);
            if (InputControlLayout.s_Layouts.baseLayoutTable.TryGetValue(internedLayoutName, out var result))
                return result;

            return null;
        }

        /// <summary>
        /// Check whether the first layout is based on the second.
        /// </summary>
        /// <param name="firstLayoutName">Name of a registered <see cref="InputControlLayout"/>.</param>
        /// <param name="secondLayoutName">Name of a registered <see cref="InputControlLayout"/>.</param>
        /// <returns>True if <paramref name="firstLayoutName"/> is based on <paramref name="secondLayoutName"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="firstLayoutName"/> is <c>null</c> or empty -or-
        /// <paramref name="secondLayoutName"/> is <c>null</c> or empty.</exception>
        /// <remarks>
        /// This is
        /// <example>
        /// </example>
        /// </remarks>
        public static bool IsFirstLayoutBasedOnSecond(string firstLayoutName, string secondLayoutName)
        {
            if (string.IsNullOrEmpty(firstLayoutName))
                throw new ArgumentNullException(nameof(firstLayoutName));
            if (string.IsNullOrEmpty(secondLayoutName))
                throw new ArgumentNullException(nameof(secondLayoutName));

            var internedFirstName = new InternedString(firstLayoutName);
            var internedSecondName = new InternedString(secondLayoutName);

            if (internedFirstName == internedSecondName)
                return true;

            return InputControlLayout.s_Layouts.IsBasedOn(internedSecondName, internedFirstName);
        }

        #endregion

        #region Processors

        /// <summary>
        /// Register an <see cref="InputProcessor{TValue}"/> with the system.
        /// </summary>
        /// <param name="type">Type that implements <see cref="InputProcessor"/>.</param>
        /// <param name="name">Name to use for the processor. If <c>null</c> or empty, name will be taken from the short name
        /// of <paramref name="type"/> (if it ends in "Processor", that suffix will be clipped from the name). Names
        /// are case-insensitive.</param>
        /// <remarks>
        /// Processors are used by both bindings (see <see cref="InputBinding"/>) and by controls
        /// (see <see cref="InputControl"/>) to post-process input values as they are being requested
        /// from calls such as <see cref="InputAction.ReadValue{TValue}"/> or <see
        /// cref="InputControl{T}.ReadValue"/>.
        ///
        /// <example>
        /// <code>
        /// // Let's say that we want to define a processor that adds some random jitter to its input.
        /// // We have to pick a value type to operate on if we want to derive from InputProcessor&lt;T&gt;
        /// // so we go with float here.
        /// //
        /// // Also, as we will need to place our call to RegisterProcessor somewhere, we add attributes
        /// // to hook into Unity's initialization. This works differently in the editor and in the player,
        /// // so we use both [InitializeOnLoad] and [RuntimeInitializeOnLoadMethod].
        /// #if UNITY_EDITOR
        /// [InitializeOnLoad]
        /// #endif
        /// public class JitterProcessor : InputProcessor&lt;float&gt;
        /// {
        ///     // Add a parameter that defines the amount of jitter we apply.
        ///     // This will be editable in the Unity editor UI and can be set
        ///     // programmatically in code. For example:
        ///     //
        ///     //    myAction.AddBinding("&lt;Gamepad&gt;/rightTrigger",
        ///     //        processors: "jitter(amount=0.1)");
        ///     //
        ///     [Tooltip("Amount of jitter to apply. Will add a random value in the range [-amount..amount] "
        ///              + "to each input value.)]
        ///     public float amount;
        ///
        ///     // Process is called when an input value is read from a control. This is
        ///     // where we perform our jitter.
        ///     public override float Process(float value, InputControl control)
        ///     {
        ///         return float + Random.Range(-amount, amount);
        ///     }
        ///
        ///     // [InitializeOnLoad] will call the static class constructor which
        ///     // we use to call Register.
        ///     #if UNITY_EDITOR
        ///     static JitterProcessor()
        ///     {
        ///         Register();
        ///     }
        ///     #endif
        ///
        ///     // [RuntimeInitializeOnLoadMethod] will make sure that Register gets called
        ///     // in the player on startup.
        ///     // NOTE: This will also get called when going into play mode in the editor. In that
        ///     //       case we get two calls to Register instead of one. We don't bother with that
        ///     //       here. Calling RegisterProcessor twice here doesn't do any harm.
        ///     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        ///     static void Register()
        ///     {
        ///         // We don't supply a name here. The input system will take "JitterProcessor"
        ///         // and automatically snip off the "Processor" suffix thus leaving us with
        ///         // a name of "Jitter" (all this is case-insensitive).
        ///         InputSystem.RegisterProcessor&lt;JitterProcessor&gt;();
        ///     }
        /// }
        ///
        /// // It doesn't really make sense in our case as the default parameter editor is just
        /// // fine (it will pick up the tooltip we defined above) but let's say we want to replace
        /// // the default float edit field we get on the "amount" parameter with a slider. We can
        /// // do so by defining a custom parameter editor.
        /// //
        /// // NOTE: We don't need to have a registration call here. The input system will automatically
        /// //       find our parameter editor based on the JitterProcessor type parameter we give to
        /// //       InputParameterEditor&lt;T&gt;.
        /// #if UNITY_EDITOR
        /// public class JitterProcessorEditor : InputParameterEditor&lt;JitterProcessor&gt;
        /// {
        ///     public override void OnGUI()
        ///     {
        ///         target.amount = EditorGUILayout.Slider(m_AmountLabel, target.amount, 0, 0.25f);
        ///     }
        ///
        ///     private GUIContent m_AmountLabel = new GUIContent("Amount",
        ///         "Amount of jitter to apply. Will add a random value in the range [-amount..amount] "
        ///             + "to each input value.);
        /// }
        /// #endif
        /// </code>
        /// </example>
        ///
        /// Note that it is allowed to register the same processor type multiple types with
        /// different names. When doing so, the first registration is considered as the "proper"
        /// name for the processor and all subsequent registrations will be considered aliases.
        ///
        /// See the <a href="../manual/Processors.html">manual</a> for more details.
        /// </remarks>
        /// <seealso cref="InputProcessor{T}"/>
        /// <seealso cref="InputBinding.processors"/>
        /// <seealso cref="InputAction.processors"/>
        /// <seealso cref="InputControlLayout.ControlItem.processors"/>
        /// <seealso cref="UnityEngine.InputSystem.Editor.InputParameterEditor{TObject}"/>
        public static void RegisterProcessor(Type type, string name = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Default name to name of type without Processor suffix.
            if (string.IsNullOrEmpty(name))
            {
                name = type.Name;
                if (name.EndsWith("Processor"))
                    name = name.Substring(0, name.Length - "Processor".Length);
            }

            // Flush out any precompiled layout depending on the processor.
            var precompiledLayouts = s_Manager.m_Layouts.precompiledLayouts;
            foreach (var key in new List<InternedString>(precompiledLayouts.Keys)) // Need to keep key list stable while iterating; ToList() for some reason not available with .NET Standard 2.0 on Mono.
            {
                if (StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(precompiledLayouts[key].metadata, name, ';'))
                    s_Manager.m_Layouts.precompiledLayouts.Remove(key);
            }

            s_Manager.processors.AddTypeRegistration(name, type);
        }

        /// <summary>
        /// Register an <see cref="InputProcessor{TValue}"/> with the system.
        /// </summary>
        /// <typeparam name="T">Type that implements <see cref="InputProcessor"/>.</typeparam>
        /// <param name="name">Name to use for the processor. If <c>null</c> or empty, name will be taken from the short name
        /// of <typeparamref name="T"/> (if it ends in "Processor", that suffix will be clipped from the name). Names
        /// are case-insensitive.</param>
        /// <remarks>
        /// Processors are used by both bindings (see <see cref="InputBinding"/>) and by controls
        /// (see <see cref="InputControl"/>) to post-process input values as they are being requested
        /// from calls such as <see cref="InputAction.ReadValue{TValue}"/> or <see
        /// cref="InputControl{T}.ReadValue"/>.
        ///
        /// <example>
        /// <code>
        /// // Let's say that we want to define a processor that adds some random jitter to its input.
        /// // We have to pick a value type to operate on if we want to derive from InputProcessor&lt;T&gt;
        /// // so we go with float here.
        /// //
        /// // Also, as we will need to place our call to RegisterProcessor somewhere, we add attributes
        /// // to hook into Unity's initialization. This works differently in the editor and in the player,
        /// // so we use both [InitializeOnLoad] and [RuntimeInitializeOnLoadMethod].
        /// #if UNITY_EDITOR
        /// [InitializeOnLoad]
        /// #endif
        /// public class JitterProcessor : InputProcessor&lt;float&gt;
        /// {
        ///     // Add a parameter that defines the amount of jitter we apply.
        ///     // This will be editable in the Unity editor UI and can be set
        ///     // programmatically in code. For example:
        ///     //
        ///     //    myAction.AddBinding("&lt;Gamepad&gt;/rightTrigger",
        ///     //        processors: "jitter(amount=0.1)");
        ///     //
        ///     [Tooltip("Amount of jitter to apply. Will add a random value in the range [-amount..amount] "
        ///              + "to each input value.)]
        ///     public float amount;
        ///
        ///     // Process is called when an input value is read from a control. This is
        ///     // where we perform our jitter.
        ///     public override float Process(float value, InputControl control)
        ///     {
        ///         return float + Random.Range(-amount, amount);
        ///     }
        ///
        ///     // [InitializeOnLoad] will call the static class constructor which
        ///     // we use to call Register.
        ///     #if UNITY_EDITOR
        ///     static JitterProcessor()
        ///     {
        ///         Register();
        ///     }
        ///     #endif
        ///
        ///     // [RuntimeInitializeOnLoadMethod] will make sure that Register gets called
        ///     // in the player on startup.
        ///     // NOTE: This will also get called when going into play mode in the editor. In that
        ///     //       case we get two calls to Register instead of one. We don't bother with that
        ///     //       here. Calling RegisterProcessor twice here doesn't do any harm.
        ///     [RuntimeInitializeOnLoadMethod]
        ///     static void Register()
        ///     {
        ///         // We don't supply a name here. The input system will take "JitterProcessor"
        ///         // and automatically snip off the "Processor" suffix thus leaving us with
        ///         // a name of "Jitter" (all this is case-insensitive).
        ///         InputSystem.RegisterProcessor&lt;JitterProcessor&gt;();
        ///     }
        /// }
        ///
        /// // It doesn't really make sense in our case as the default parameter editor is just
        /// // fine (it will pick up the tooltip we defined above) but let's say we want to replace
        /// // the default float edit field we get on the "amount" parameter with a slider. We can
        /// // do so by defining a custom parameter editor.
        /// //
        /// // NOTE: We don't need to have a registration call here. The input system will automatically
        /// //       find our parameter editor based on the JitterProcessor type parameter we give to
        /// //       InputParameterEditor&lt;T&gt;.
        /// #if UNITY_EDITOR
        /// public class JitterProcessorEditor : InputParameterEditor&lt;JitterProcessor&gt;
        /// {
        ///     public override void OnGUI()
        ///     {
        ///         target.amount = EditorGUILayout.Slider(m_AmountLabel, target.amount, 0, 0.25f);
        ///     }
        ///
        ///     private GUIContent m_AmountLabel = new GUIContent("Amount",
        ///         "Amount of jitter to apply. Will add a random value in the range [-amount..amount] "
        ///             + "to each input value.);
        /// }
        /// #endif
        /// </code>
        /// </example>
        ///
        /// Note that it is allowed to register the same processor type multiple types with
        /// different names. When doing so, the first registration is considered as the "proper"
        /// name for the processor and all subsequent registrations will be considered aliases.
        ///
        /// See the <a href="../manual/Processors.html">manual</a> for more details.
        /// </remarks>
        /// <seealso cref="InputProcessor{T}"/>
        /// <seealso cref="InputBinding.processors"/>
        /// <seealso cref="InputAction.processors"/>
        /// <seealso cref="InputControlLayout.ControlItem.processors"/>
        /// <seealso cref="UnityEngine.InputSystem.Editor.InputParameterEditor{TObject}"/>
        public static void RegisterProcessor<T>(string name = null)
        {
            RegisterProcessor(typeof(T), name);
        }

        /// <summary>
        /// Return the processor type registered under the given name. If no such processor
        /// has been registered, return <c>null</c>.
        /// </summary>
        /// <param name="name">Name of processor. Case-insensitive.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c> or empty.</exception>
        /// <returns>The given processor type or <c>null</c> if not found.</returns>
        /// <seealso cref="RegisterProcessor{T}"/>
        public static Type TryGetProcessor(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            return s_Manager.processors.LookupTypeRegistration(name);
        }

        /// <summary>
        /// List the names of all processors have been registered.
        /// </summary>
        /// <returns>List of registered processors.</returns>
        /// <remarks>
        /// Note that the result will include both "proper" names and aliases registered
        /// for processors. If, for example, a given type <c>JitterProcessor</c> has been registered
        /// under both "Jitter" and "Randomize", it will appear in the list with both those names.
        /// </remarks>
        /// <seealso cref="TryGetProcessor"/>
        /// <seealso cref="RegisterProcessor{T}"/>
        public static IEnumerable<string> ListProcessors()
        {
            return s_Manager.processors.names;
        }

        #endregion

        #region Devices

        /// <summary>
        /// The list of currently connected devices.
        /// </summary>
        /// <value>Currently connected devices.</value>
        /// <remarks>
        /// Note that accessing this property does not allocate. It gives read-only access
        /// directly to the system's internal array of devices.
        ///
        /// The value returned by this property should not be held on to. When the device
        /// setup in the system changes, any value previously returned by this property
        /// may become invalid. Query the property directly whenever you need it.
        /// </remarks>
        /// <seealso cref="AddDevice{TDevice}"/>
        /// <seealso cref="RemoveDevice"/>
        public static ReadOnlyArray<InputDevice> devices => s_Manager.devices;

        /// <summary>
        /// Devices that have been disconnected but are retained by the input system in case
        /// they are plugged back in.
        /// </summary>
        /// <value>Devices that have been retained by the input system in case they are plugged
        /// back in.</value>
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
        /// The list can be purged by calling <see cref="FlushDisconnectedDevices"/>. Doing so, will release
        /// all reference we hold to the devices or any controls inside of them and allow the devices to be
        /// reclaimed by the garbage collector.
        ///
        /// Note that if you call <see cref="RemoveDevice"/> explicitly, the given device is not retained
        /// by the input system and will not appear on this list.
        ///
        /// Also note that devices on this list will be lost when domain reloads happen in the editor (i.e. on
        /// script recompilation and when entering play mode).
        /// </remarks>
        /// <seealso cref="FlushDisconnectedDevices"/>
        public static ReadOnlyArray<InputDevice> disconnectedDevices =>
            new ReadOnlyArray<InputDevice>(s_Manager.m_DisconnectedDevices, 0,
                s_Manager.m_DisconnectedDevicesCount);

        /// <summary>
        /// Event that is signalled when the device setup in the system changes.
        /// </summary>
        /// <value>Callback when device setup ni system changes.</value>
        /// <remarks>
        /// This can be used to detect when devices are added or removed as well as
        /// detecting when existing devices change their configuration.
        ///
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
        ///                 Debug.Log("Device configuration changed: " + device);
        ///                 break;
        ///         }
        ///     };
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Delegate reference is <c>null</c>.</exception>
        /// <seealso cref="devices"/>
        /// <seealso cref="AddDevice{TDevice}"/>
        /// <seealso cref="RemoveDevice"/>
        public static event Action<InputDevice, InputDeviceChange> onDeviceChange
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                lock (s_Manager)
                    s_Manager.onDeviceChange += value;
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                lock (s_Manager)
                    s_Manager.onDeviceChange -= value;
            }
        }

        ////REVIEW: this one isn't really well-designed and the means of intercepting communication
        ////        with the backend should be revisited >1.0
        /// <summary>
        /// Event that is signalled when an <see cref="InputDeviceCommand"/> is sent to
        /// an <see cref="InputDevice"/>.
        /// </summary>
        /// <value>Event that gets signalled on <see cref="InputDeviceCommand"/>s.</value>
        /// <remarks>
        /// This can be used to intercept commands and optionally handle them without them reaching
        /// the <see cref="IInputRuntime"/>.
        ///
        /// The first delegate in the list that returns a result other than <c>null</c> is considered
        /// to have handled the command. If a command is handled by a delegate in the list, it will
        /// not be sent on to the runtime.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Delegate reference is <c>null</c>.</exception>
        /// <seealso cref="InputDevice.ExecuteCommand{TCommand}"/>
        /// <seealso cref="IInputRuntime.DeviceCommand"/>
        public static event InputDeviceCommandDelegate onDeviceCommand
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                lock (s_Manager)
                    s_Manager.onDeviceCommand += value;
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                lock (s_Manager)
                    s_Manager.onDeviceCommand -= value;
            }
        }

        /// <summary>
        /// Event that is signalled when the system is trying to match a layout to
        /// a device it has discovered.
        /// </summary>
        /// <remarks>
        /// This event allows customizing the layout discovery process and to generate
        /// layouts on the fly, if need be. When a device is reported from the Unity
        /// runtime or through <see cref="AddDevice(InputDeviceDescription)"/>, it is
        /// reported in the form of an <see cref="InputDeviceDescription"/>. The system
        /// will take that description and run it through all the <see cref="InputDeviceMatcher"/>s
        /// that have been registered for layouts (<see cref="RegisterLayoutMatcher{TDevice}"/>).
        /// Based on that, it will come up with either no matching layout or with a single
        /// layout that has the highest matching score according to <see
        /// cref="InputDeviceMatcher.MatchPercentage"/> (or, in case multiple layouts have
        /// the same score, the first one to achieve that score -- which is quasi-non-deterministic).
        ///
        /// It will then take this layout name (which, again, may be empty) and invoke this
        /// event here passing it not only the layout name but also information such as the
        /// <see cref="InputDeviceDescription"/> for the device. Each of the callbacks hooked
        /// into the event will be run in turn. The <em>first</em> one to return a string
        /// that is not <c>null</c> and not empty will cause a switch from the layout the
        /// system has chosen to the layout that has been returned by the callback. The remaining
        /// layouts after that will then be invoked with that newly selected name but will not
        /// be able to change the name anymore.
        ///
        /// If none of the callbacks returns a string that is not <c>null</c> or empty,
        /// the system will stick with the layout that it had initially selected.
        ///
        /// Once all callbacks have been run, the system will either have a final layout
        /// name or not. If it does, a device is created using that layout. If it does not,
        /// no device is created.
        ///
        /// One thing this allows is to generate callbacks on the fly. Let's say that if
        /// an input device is reported with the "Custom" interface, we want to generate
        /// a layout for it on the fly. For details about how to build layouts dynamically
        /// from code, see <see cref="InputControlLayout.Builder"/> and <see cref="RegisterLayoutBuilder"/>.
        ///
        /// <example>
        /// <code>
        /// InputSystem.onFindLayoutForDevice +=
        ///     (deviceId, description, matchedLayout, runtime) =>
        ///     {
        ///         // If the system does have a matching layout, we do nothing.
        ///         // This could be the case, for example, if we already generated
        ///         // a layout for the device or if someone explicitly registered
        ///         // a layout.
        ///         if (!string.IsNullOrEmpty(matchedLayout))
        ///             return null; // Tell system we did nothing.
        ///
        ///         // See if the reported device uses the "Custom" interface. We
        ///         // are only interested in those.
        ///         if (description.interfaceName != "Custom")
        ///             return null; // Tell system we did nothing.
        ///
        ///         // So now we know that we want to build a layout on the fly
        ///         // for this device. What we do is to register what's called a
        ///         // layout builder. These can use C# code to build an InputControlLayout
        ///         // on the fly.
        ///
        ///         // First we need to come up with a sufficiently unique name for the layout
        ///         // under which we register the builder. This will usually involve some
        ///         // information from the InputDeviceDescription we have been supplied with.
        ///         // Let's say we can sufficiently tell devices on our interface apart by
        ///         // product name alone. So we just do this:
        ///         var layoutName = "Custom" + description.product;
        ///
        ///         // We also need an InputDeviceMatcher that in the future will automatically
        ///         // select our newly registered layout whenever a new device of the same type
        ///         // is connected. We can get one simply like so:
        ///         var matcher = InputDeviceMatcher.FromDescription(description);
        ///
        ///         // With these pieces in place, we can register our builder which
        ///         // mainly consists of a delegate that will get invoked when an instance
        ///         // of InputControlLayout is needed for the layout.
        ///         InputSystem.RegisterLayoutBuilder(
        ///             () =>
        ///             {
        ///                 // Here is where we do the actual building. In practice,
        ///                 // this would probably look at the 'capabilities' property
        ///                 // of the InputDeviceDescription we got and create a tailor-made
        ///                 // layout. But what you put in the layout here really depends on
        ///                 // the specific use case you have.
        ///                 //
        ///                 // We just add some preset things here which should still sufficiently
        ///                 // serve as a demonstration.
        ///                 //
        ///                 // Note that we can base our layout here on whatever other layout
        ///                 // in the system. We could extend Gamepad, for example. If we don't
        ///                 // choose a base layout, the system automatically implies InputDevice.
        ///
        ///                 var builder = new InputControlLayout.Builder()
        ///                     .WithDisplayName(description.product);
        ///
        ///                 // Add controls.
        ///                 builder.AddControl("stick")
        ///                     .WithLayout("Stick");
        ///
        ///                 return builder.Build();
        ///             },
        ///             layoutName,
        ///             matches: matcher);
        ///
        ///         // So, we want the system to use our layout for the device that has just
        ///         // been connected. We return it from this callback to do that.
        ///         return layoutName;
        ///     };
        /// </code>
        /// </example>
        ///
        /// Note that it may appear like one could simply use <see cref="RegisterLayoutBuilder"/>
        /// like below instead of going through <c>onFindLayoutForDevice</c>.
        ///
        /// <example>
        /// <code>
        /// InputSystem.RegisterLayoutBuilder(
        ///     () =>
        ///     {
        ///         // Layout building code from above...
        ///     },
        ///     "CustomLayout",
        ///     matches: new InputDeviceMatcher().WithInterface("Custom"));
        /// </code>
        /// </example>
        ///
        /// However, the difference here is that all devices using the "Custom" interface will
        /// end up with the same single layout -- which has to be identical. By hooking into
        /// <c>onFindLayoutForDevice</c>, it is possible to register a new layout for every new
        /// type of device that is discovered and thus build a multitude of different layouts.
        ///
        /// It is best to register for this callback during startup. One way to do it is to
        /// use <c>InitializeOnLoadAttribute</c> and <c>RuntimeInitializeOnLoadMethod</c>.
        /// </remarks>
        /// <seealso cref="RegisterLayoutBuilder"/>
        /// <seealso cref="InputControlLayout"/>
        public static event InputDeviceFindControlLayoutDelegate onFindLayoutForDevice
        {
            add
            {
                lock (s_Manager)
                    s_Manager.onFindControlLayoutForDevice += value;
            }
            remove
            {
                lock (s_Manager)
                    s_Manager.onFindControlLayoutForDevice -= value;
            }
        }

        ////REVIEW: should this be disambiguated more to separate it more from sensor sampling frequency?
        ////REVIEW: this should probably be exposed as an input setting
        /// <summary>
        /// Frequency at which devices that need polling are being queried in the background.
        /// </summary>
        /// <value>Polled device sampling frequency in Hertz.</value>
        /// <remarks>
        /// Input data is gathered from platform APIs either as events or polled periodically.
        ///
        /// In the former case, where we get input as events, the platform is responsible for monitoring
        /// input devices and sending their state changes which the Unity runtime receives
        /// and queues as <see cref="InputEvent"/>s. This form of input collection usually happens on a
        /// system-specific thread (which may be Unity's main thread) as part of how the Unity player
        /// loop operates. In most cases, this means that this form of input will invariably get picked up
        /// once per frame.
        ///
        /// In the latter case, where input has to be explicitly polled from the system, the Unity runtime
        /// will periodically sample the state of input devices and send it off as input events. Wherever
        /// possible, this happens in the background at a fixed frequency on a dedicated thread. The
        /// <c>pollingFrequency</c> property controls the rate at which this sampling happens.
        ///
        /// The unit is Hertz. A value of 120, for example, means that devices are sampled 120 times
        /// per second.
        ///
        /// The default polling frequency is 60 Hz.
        ///
        /// For devices that are polled, the frequency setting will directly translate to changes in the
        /// <see cref="InputEvent.time"/> patterns. At 60 Hz, for example, timestamps for a specific,
        /// polled device will be spaced at roughly 1/60th of a second apart.
        ///
        /// Note that it depends on the platform which devices are polled (if any). On Win32, for example,
        /// only XInput gamepads are polled.
        ///
        /// Also note that the polling frequency applies to all devices that are polled. It is not possible
        /// to set polling frequency on a per-device basis.
        /// </remarks>
        public static float pollingFrequency
        {
            get => s_Manager.pollingFrequency;
            set => s_Manager.pollingFrequency = value;
        }

        /// <summary>
        /// Add a new device by instantiating the given device layout.
        /// </summary>
        /// <param name="layout">Name of the layout to instantiate. Must be a device layout. Note that
        /// layout names are case-insensitive.</param>
        /// <param name="name">Name to assign to the device. If null, the layout's display name (<see
        /// cref="InputControlLayout.displayName"/> is used instead. Note that device names are made
        /// unique automatically by the system by appending numbers to them (e.g. "gamepad", "gamepad1",
        /// "gamepad2", etc.).</param>
        /// <param name="variants">Semicolon-separated list of layout variants to use for the device.</param>
        /// <exception cref="ArgumentNullException"><paramref name="layout"/> is <c>null</c> or empty.</exception>
        /// <returns>The newly created input device.</returns>
        /// <remarks>
        /// The device will be added to the <see cref="devices"/> list and a notification on
        /// <see cref="onDeviceChange"/> will be triggered.
        ///
        /// Note that adding a device to the system will allocate and also create garbage on the GC heap.
        ///
        /// <example>
        /// <code>
        /// // This is one way to instantiate the "Gamepad" layout.
        /// InputSystem.AddDevice("Gamepad");
        ///
        /// // In this case, because the "Gamepad" layout is based on the Gamepad
        /// // class, we can also do this instead:
        /// InputSystem.AddDevice&lt;Gamepad&gt;();
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="AddDevice{T}"/>
        /// <seealso cref="RemoveDevice"/>
        /// <seealso cref="onDeviceChange"/>
        /// <seealso cref="InputDeviceChange.Added"/>
        /// <seealso cref="devices"/>
        /// <seealso cref="RegisterLayout(Type,string,Nullable{InputDeviceMatcher})"/>
        public static InputDevice AddDevice(string layout, string name = null, string variants = null)
        {
            if (string.IsNullOrEmpty(layout))
                throw new ArgumentNullException(nameof(layout));
            return s_Manager.AddDevice(layout, name, new InternedString(variants));
        }

        /// <summary>
        /// Add a new device by instantiating the layout registered for type <typeparamref name="TDevice"/>.
        /// </summary>
        /// <param name="name">Name to assign to the device. If null, the layout's display name (<see
        /// cref="InputControlLayout.displayName"/> is used instead. Note that device names are made
        /// unique automatically by the system by appending numbers to them (e.g. "gamepad", "gamepad1",
        /// "gamepad2", etc.).</param>
        /// <typeparam name="TDevice">Type of device to add.</typeparam>
        /// <returns>The newly added device.</returns>
        /// <exception cref="InvalidOperationException">Instantiating the layout for <typeparamref name="TDevice"/>
        /// did not produce a device of type <typeparamref name="TDevice"/>.</exception>
        /// <remarks>
        /// The device will be added to the <see cref="devices"/> list and a notification on
        /// <see cref="onDeviceChange"/> will be triggered.
        ///
        /// Note that adding a device to the system will allocate and also create garbage on the GC heap.
        ///
        /// <example>
        /// <code>
        /// // Add a gamepad.
        /// InputSystem.AddDevice&lt;Gamepad&gt;();
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="RemoveDevice"/>
        /// <seealso cref="onDeviceChange"/>
        /// <seealso cref="InputDeviceChange.Added"/>
        /// <seealso cref="devices"/>
        public static TDevice AddDevice<TDevice>(string name = null)
            where TDevice : InputDevice
        {
            var device = s_Manager.AddDevice(typeof(TDevice), name);
            if (!(device is TDevice deviceOfType))
            {
                // Consider the entire operation as failed, so remove the device we just added.
                if (device != null)
                    RemoveDevice(device);
                throw new InvalidOperationException(
                    $"Layout registered for type '{typeof(TDevice).Name}' did not produce a device of that type; layout probably has been overridden");
            }
            return deviceOfType;
        }

        /// <summary>
        /// Tell the input system that a new device has become available.
        /// </summary>
        /// <param name="description">Description of the input device.</param>
        /// <returns>The newly created device that has been added to <see cref="devices"/>.</returns>
        /// <exception cref="ArgumentException">The given <paramref name="description"/> is empty -or-
        /// no layout can be found that matches the given device <paramref name="description"/>.</exception>
        /// <remarks>
        /// This method is different from methods such as <see cref="AddDevice(string,string,string)"/>
        /// or <see cref="AddDevice{TDevice}"/> in that it employs the usual matching process the
        /// same way that it happens when the Unity runtime reports an input device.
        ///
        /// In particular, the same procedure described in the documentation for <see cref="onFindLayoutForDevice"/>
        /// is employed where all registered <see cref="InputDeviceMatcher"/>s are matched against the
        /// supplied device description and the most suitable match determines the layout to use. This in
        /// turn is run through <see cref="onFindLayoutForDevice"/> to determine the final layout to use.
        ///
        /// If no suitable layout can be found, the method throws <c>ArgumentException</c>.
        /// <example>
        /// <code>
        /// InputSystem.AddDevice(
        ///     new InputDeviceDescription
        ///     {
        ///         interfaceName = "Custom",
        ///         product = "Product"
        ///     });
        /// </code>
        /// </example>
        /// </remarks>
        public static InputDevice AddDevice(InputDeviceDescription description)
        {
            if (description.empty)
                throw new ArgumentException("Description must not be empty", nameof(description));
            return s_Manager.AddDevice(description);
        }

        /// <summary>
        /// Add the given device back to the system.
        /// </summary>
        /// <param name="device">An input device. If the device is currently already added to
        /// the system (i.e. is in <see cref="devices"/>), the method will do nothing.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>
        /// This can be used when a device has been manually removed with <see cref="RemoveDevice"/>.
        ///
        /// The device will be added to the <see cref="devices"/> list and a notification on
        /// <see cref="onDeviceChange"/> will be triggered.
        ///
        /// It may be tempting to do the following but this will not work:
        ///
        /// <example>
        /// <code>
        /// // This will *NOT* work.
        /// var device = new Gamepad();
        /// InputSystem.AddDevice(device);
        /// </code>
        /// </example>
        ///
        /// <see cref="InputDevice"/>s, like <see cref="InputControl"/>s in general, cannot
        /// simply be instantiated with <c>new</c> but must be created by the input system
        /// instead.
        /// </remarks>
        /// <seealso cref="RemoveDevice"/>
        /// <seealso cref="AddDevice{TDevice}"/>
        /// <seealso cref="devices"/>
        public static void AddDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            s_Manager.AddDevice(device);
        }

        /// <summary>
        /// Remove a device from the system such that it no longer receives input and is no longer part of the
        /// set of devices in <see cref="devices"/>.
        /// </summary>
        /// <param name="device">Device to remove. If the device has already been removed (i.e. if <see cref="InputDevice.added"/>
        /// is false), the method does nothing.</param>
        /// <remarks>
        /// Actions that are bound to controls on the device will automatically unbind when the device
        /// is removed.
        ///
        /// When a device is removed, <see cref="onDeviceChange"/> will be triggered with <see cref="InputDeviceChange.Removed"/>.
        /// The device will be removed from <see cref="devices"/> as well as from any device-specific getters such as
        /// <see cref="Gamepad.all"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        /// <seealso cref="InputDevice.added"/>
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
        public static void FlushDisconnectedDevices()
        {
            s_Manager.FlushDisconnectedDevices();
        }

        /// <summary>
        /// Return the device with given name or layout <param name="nameOrLayout"/>.
        /// Returns null if no such device currently exists.
        /// </summary>
        /// <param name="nameOrLayout">Unique device name or layout to search for.</param>
        /// <returns>The device matching the given search criteria or null.</returns>
        /// <seealso cref="GetDevice(Type)"/>
        /// <seealso cref="GetDevice{TDevice}"/>
        /// <seealso cref="AddDevice{TDevice}"/>
        public static InputDevice GetDevice(string nameOrLayout)
        {
            return s_Manager.TryGetDevice(nameOrLayout);
        }

        ////REVIEW: this API seems inconsistent with GetDevice(string); both have very different meaning yet very similar signatures
        /// <summary>
        /// Return the most recently used device that is assignable to the given type <typeparamref name="TDevice"/>.
        /// Returns null if no such device currently exists.
        /// </summary>
        /// <typeparam name="TDevice">Type of device to look for.</typeparam>
        /// <returns>The device that is assignable to the given type or null.</returns>
        /// <seealso cref="GetDevice(string)"/>
        /// <seealso cref="GetDevice(Type)"/>
        public static TDevice GetDevice<TDevice>()
            where TDevice : InputDevice
        {
            return (TDevice)GetDevice(typeof(TDevice));
        }

        ////REVIEW: this API seems inconsistent with GetDevice(string); both have very different meaning yet very similar signatures
        /// <summary>
        /// Return the most recently used device that is assignable to the given type <param name="type"/>.
        /// Returns null if no such device currently exists.
        /// </summary>
        /// <param name="type">Type of the device</param>
        /// <returns>The device that is assignable to the given type or null.</returns>
        /// <seealso cref="GetDevice(string)"/>
        /// <seealso cref="GetDevice&lt;TDevice&gt;()"/>
        public static InputDevice GetDevice(Type type)
        {
            InputDevice result = null;
            var lastUpdateTime = -1.0;
            foreach (var device in devices)
            {
                if (!type.IsInstanceOfType(device))
                    continue;

                if (result == null || device.m_LastUpdateTimeInternal > lastUpdateTime)
                {
                    result = device;
                    lastUpdateTime = result.m_LastUpdateTimeInternal;
                }
            }

            return result;
        }

        ////REVIEW: this API seems inconsistent with GetDevice(string); both have very different meaning yet very similar signatures
        /// <summary>
        /// Return the device of the given type <typeparamref name="TDevice"/> that has the
        /// given usage assigned. Returns null if no such device currently exists.
        /// </summary>
        /// <param name="usage">Usage of the device, e.g. "LeftHand".</param>
        /// <typeparam name="TDevice">Type of device to look for.</typeparam>
        /// <returns>The device with the given type and usage or null.</returns>
        /// <remarks>
        /// Devices usages are most commonly employed to "tag" devices for a specific role.
        /// A common scenario, for example, is to distinguish which hand a specific <see cref="XR.XRController"/>
        /// is associated with. However, arbitrary usages can be assigned to devices.
        /// <example>
        /// <code>
        /// // Get the left hand XRController.
        /// var leftHand = InputSystem.GetDevice&lt;XRController&gt;(CommonUsages.leftHand);
        ///
        /// // Mark gamepad #2 as being for player 1.
        /// InputSystem.SetDeviceUsage(Gamepad.all[1], "Player1");
        /// // And later look it up.
        /// var player1Gamepad = InputSystem.GetDevice&lt;Gamepad&gt;(new InternedString("Player1"));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="GetDevice(string)"/>
        /// <seealso cref="SetDeviceUsage(InputDevice,string)"/>
        /// <seealso cref="InputControl.usages"/>
        public static TDevice GetDevice<TDevice>(InternedString usage)
            where TDevice : InputDevice
        {
            TDevice result = null;
            var lastUpdateTime = -1.0;
            foreach (var device in devices)
            {
                var deviceOfType = device as TDevice;
                if (deviceOfType == null)
                    continue;
                if (!deviceOfType.usages.Contains(usage))
                    continue;

                if (result == null || deviceOfType.m_LastUpdateTimeInternal > lastUpdateTime)
                {
                    result = deviceOfType;
                    lastUpdateTime = result.m_LastUpdateTimeInternal;
                }
            }

            return result;
        }

        /// <summary>
        /// Return the device of the given type <typeparamref name="TDevice"/> that has the
        /// given usage assigned. Returns null if no such device currently exists.
        /// </summary>
        /// <param name="usage">Usage of the device, e.g. "LeftHand".</param>
        /// <typeparam name="TDevice">Type of device to look for.</typeparam>
        /// <returns>The device with the given type and usage or null.</returns>
        /// <remarks>
        /// Devices usages are most commonly employed to "tag" devices for a specific role.
        /// A common scenario, for example, is to distinguish which hand a specific <see cref="XR.XRController"/>
        /// is associated with. However, arbitrary usages can be assigned to devices.
        /// </remarks>
        /// <seealso cref="GetDevice(InternedString)"/>
        /// <seealso cref="SetDeviceUsage(InputDevice,string)"/>
        /// <seealso cref="InputControl.usages"/>
        public static TDevice GetDevice<TDevice>(string usage)
            where TDevice : InputDevice
        {
            return GetDevice<TDevice>(new InternedString(usage));
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
        /// <seealso cref="InputDevice.deviceId"/>
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

        /// <summary>
        /// Populate a list of devices that have been reported by the <see cref="IInputRuntime">runtime</see>
        /// but could not be matched to any known <see cref="InputControlLayout">layout</see>.
        /// </summary>
        /// <param name="descriptions">A list to be populated with descriptions of devices that could not be recognized.</param>
        /// <returns>The number of devices that could not be recognized.</returns>
        /// <remarks>
        /// If new layouts are added to the system or if additional <see cref="InputDeviceMatcher">matches</see>
        /// are added to existing layouts, devices in this list may appear or disappear.
        /// </remarks>
        /// <seealso cref="InputDeviceMatcher"/>
        /// <seealso cref="RegisterLayoutMatcher"/>
        public static int GetUnsupportedDevices(List<InputDeviceDescription> descriptions)
        {
            return s_Manager.GetUnsupportedDevices(descriptions);
        }

        /// <summary>
        /// (Re-)enable the given device.
        /// </summary>
        /// <param name="device">Device to enable. If already enabled, the method will do nothing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This can be used after a device has been disabled with <see cref="DisableDevice"/> or
        /// with devices that start out in disabled state (usually the case for all <see cref="Sensor"/>
        /// devices).
        ///
        /// When enabled, a device will receive input when available.
        ///
        /// <example>
        /// <code>
        /// // Enable the gyroscope, if present.
        /// if (Gyroscope.current != null)
        ///     InputSystem.EnableDevice(Gyroscope.current);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="DisableDevice"/>
        /// <seealso cref="InputDevice.enabled"/>
        public static void EnableDevice(InputDevice device)
        {
            s_Manager.EnableOrDisableDevice(device, true);
        }

        /// <summary>
        /// Disable the given device, i.e. "mute" it.
        /// </summary>
        /// <param name="device">Device to disable. If already disabled, the method will do nothing.</param>
        /// <param name="keepSendingEvents">If true, no <see cref="LowLevel.DisableDeviceCommand"/> will be sent
        /// for the device. This means that the backend sending input events will not be notified about the device
        /// being disabled and will thus keep sending events. This can be useful when input is being rerouted from
        /// one device to another. For example, <see cref="TouchSimulation"/> uses this to disable the <see cref="Mouse"/>
        /// while redirecting its events to input on a <see cref="Touchscreen"/>.<br/><br/>This parameter is false by default.</param>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <remarks>
        /// A disabled device will not receive input and will remain in its default state. It will remain
        /// present in the system but without actually feeding input into it.
        ///
        /// Disabling devices is most useful for <see cref="Sensor"/> devices on battery-powered platforms
        /// where having a sensor enabled will increase energy consumption. Sensors will usually start
        /// out in disabled state and can be enabled, when needed, with <see cref="EnableDevice"/> and
        /// disabled again wth this method.
        ///
        /// However, disabling a device can be useful in other situations, too. For example, when simulating
        /// input (say, mouse input) locally from a remote source, it can be desirable to turn off the respective
        /// local device.
        ///
        /// To remove a device altogether, use <see cref="RemoveDevice"/> instead. This will not only silence
        /// input but remove the <see cref="InputDevice"/> instance from the system altogether.
        /// </remarks>
        /// <seealso cref="EnableDevice"/>
        /// <seealso cref="InputDevice.enabled"/>
        public static void DisableDevice(InputDevice device, bool keepSendingEvents = false)
        {
            s_Manager.EnableOrDisableDevice(device, false, keepSendingEvents ? InputManager.DeviceDisableScope.InFrontendOnly : default);
        }

        /// <summary>
        /// Issue a <see cref="RequestSyncCommand"/> on <paramref name="device"/>. This requests the device to
        /// send its current state as an event. If successful, the device will be updated in the next <see cref="InputSystem.Update"/>.
        /// </summary>
        /// <param name="device">An <see cref="InputDevice"/> that is currently part of <see cref="devices"/>.</param>
        /// <returns>True if the request succeeded, false if it fails.</returns>
        /// <remarks>
        /// It depends on the backend/platform implementation whether explicit synchronization is supported. If it is, the method
        /// will return true. If it is not, the method will return false and the request is ignored.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="device"/> has not been <see cref="InputDevice.added"/>.</exception>
        /// <seealso cref="RequestSyncCommand"/>
        /// <seealso cref="ResetDevice"/>
        public static bool TrySyncDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (!device.added)
                throw new InvalidOperationException($"Device '{device}' has not been added");
            return device.RequestSync();
        }

        /// <summary>
        /// Reset the state of the given device.
        /// </summary>
        /// <param name="device">Device to reset. Must be <see cref="InputDevice.added"/> to the system.</param>
        /// <param name="alsoResetDontResetControls">If true, also reset controls that are marked as <see cref="InputControlAttribute.dontReset"/>.
        /// Leads to <see cref="InputDeviceChange.HardReset"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="device"/> has not been <see cref="InputDevice.added"/>.</exception>
        /// <remarks>
        /// There are two different kinds of resets performed by the input system: a "soft" reset and a "hard" reset.
        ///
        /// A "hard" reset resets all controls on the device to their default state and also sends a <see cref="RequestResetCommand"/>
        /// to the backend, instructing to also reset its own internal state (if any) to the default.
        ///
        /// A "soft" reset will reset only controls that are not marked as <see cref="InputControlAttribute.noisy"/> and not marked as
        /// <see cref="InputControlAttribute.dontReset"/>. It will also not set a <see cref="RequestResetCommand"/> to the backend,
        /// i.e. the reset will be internal to the input system only (and thus can be partial in nature).
        ///
        /// By default, the method will perform a "soft" reset if <paramref name="device"/> has <see cref="InputControlAttribute.noisy"/>
        /// or <see cref="InputControlAttribute.dontReset"/> controls. If it does not, it will perform a "hard" reset.
        ///
        /// A "hard" reset can be forced by setting <paramref name="alsoResetDontResetControls"/> to true.
        ///
        /// <example>
        /// <code>
        /// // "Soft" reset the mouse. This will leave controls such as the mouse position intact
        /// // but will reset button press states.
        /// InputSystem.ResetDevice(Mouse.current);
        ///
        /// // "Hard" reset the mouse. This will wipe everything and reset the mouse to its default
        /// // state.
        /// InputSystem.ResetDevice(Mouse.current, alsoResetDontResetControls: true);
        /// </code>
        /// </example>
        ///
        /// Resetting a device will trigger a <see cref="InputDeviceChange.SoftReset"/> or <see cref="InputDeviceChange.HardReset"/>
        /// (based on the value of <paramref name="alsoResetDontResetControls"/>) notification on <see cref="onDeviceChange"/>.
        /// Also, all <see cref="InputAction"/>s currently in progress from controls on <paramref name="device"/> will be cancelled
        /// (see <see cref="InputAction.canceled"/>) in a way that guarantees for them to not get triggered. That is, a reset is
        /// semantically different from simply sending an event with default state. Using the latter, a button may be considered as
        /// going from pressed to released whereas with a device reset, the change back to unpressed state will not be considered
        /// a button release (and thus not trigger interactions that are waiting for a button release).
        /// </remarks>
        /// <seealso cref="TrySyncDevice"/>
        /// <seealso cref="InputDeviceChange.HardReset"/>
        /// <seealso cref="InputDeviceChange.SoftReset"/>
        /// <seealso cref="LowLevel.DeviceResetEvent"/>
        public static void ResetDevice(InputDevice device, bool alsoResetDontResetControls = false)
        {
            s_Manager.ResetDevice(device, alsoResetDontResetControls);
        }

        // Not an auto-upgrade as it implies a change in behavior.
        [Obsolete("Use 'ResetDevice' instead.", error: false)]
        public static bool TryResetDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            return device.RequestReset();
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
                if (device is IHaptics haptics)
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
                if (device is IHaptics haptics)
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
                if (device is IHaptics haptics)
                    haptics.ResetHaptics();
            }
        }

        #endregion

        #region Controls

        /// <summary>
        /// Set the usage tag of the given device to <paramref name="usage"/>.
        /// </summary>
        /// <param name="device">Device to set the usage on.</param>
        /// <param name="usage">New usage for the device.</param>
        /// <remarks>
        /// Usages allow to "tag" a specific device such that the tag can then be used in lookups
        /// and bindings. A common use is for identifying the handedness of an <see cref="XR.XRController"/>
        /// but the usages can be arbitrary strings.
        ///
        /// This method either sets the usages of the device to a single string (meaning it will
        /// clear whatever, if any usages, the device has when the method is called) or,
        /// if <paramref name="usage"/> is null or empty, resets the usages of the device
        /// to be empty. To add to a device's set of usages, call <see cref="AddDeviceUsage(InputDevice,string)"/>.
        /// To remove usages from a device, call <see cref="RemoveDeviceUsage(InputDevice,string)"/>.
        ///
        /// The set of usages a device has can be queried with <see cref="InputControl.usages"/> (a device
        /// is an <see cref="InputControl"/> and thus, like controls, has an associated set of usages).
        ///
        /// <example>
        /// <code>
        /// // Tag a gamepad to be associated with player #1.
        /// InputSystem.SetDeviceUsage(myGamepad, "Player1");
        ///
        /// // Create an action that binds to player #1's gamepad specifically.
        /// var action = new InputAction(binding: "&lt;Gamepad&gt;{Player1}/buttonSouth");
        ///
        /// // Move the tag from one gamepad to another.
        /// InputSystem.SetDeviceUsage(myGamepad, null); // Clears usages on 'myGamepad'.
        /// InputSystem.SetDeviceUsage(otherGamepad, "Player1");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        /// <seealso cref="InputControl.usages"/>
        /// <seealso cref="AddDeviceUsage(InputDevice,string)"/>
        /// <seealso cref="RemoveDeviceUsage(InputDevice,string)"/>
        /// <seealso cref="CommonUsages"/>
        /// <seealso cref="InputDeviceChange.UsageChanged"/>
        public static void SetDeviceUsage(InputDevice device, string usage)
        {
            SetDeviceUsage(device, new InternedString(usage));
        }

        /// <summary>
        /// Set the usage tag of the given device to <paramref name="usage"/>.
        /// </summary>
        /// <param name="device">Device to set the usage on.</param>
        /// <param name="usage">New usage for the device.</param>
        /// <remarks>
        /// Usages allow to "tag" a specific device such that the tag can then be used in lookups
        /// and bindings. A common use is for identifying the handedness of an <see cref="XR.XRController"/>
        /// but the usages can be arbitrary strings.
        ///
        /// This method either sets the usages of the device to a single string (meaning it will
        /// clear whatever, if any usages, the device has when the method is called) or,
        /// if <paramref name="usage"/> is null or empty, resets the usages of the device
        /// to be empty. To add to a device's set of usages, call <see cref="AddDeviceUsage(InputDevice,InternedString)"/>.
        /// To remove usages from a device, call <see cref="RemoveDeviceUsage(InputDevice,InternedString)"/>.
        ///
        /// The set of usages a device has can be queried with <see cref="InputControl.usages"/> (a device
        /// is an <see cref="InputControl"/> and thus, like controls, has an associated set of usages).
        ///
        /// If the set of usages on the device changes as a result of calling this method, <see cref="onDeviceChange"/>
        /// will be triggered with <see cref="InputDeviceChange.UsageChanged"/>.
        ///
        /// <example>
        /// <code>
        /// // Tag a gamepad to be associated with player #1.
        /// InputSystem.SetDeviceUsage(myGamepad, new InternedString("Player1"));
        ///
        /// // Create an action that binds to player #1's gamepad specifically.
        /// var action = new InputAction(binding: "&lt;Gamepad&gt;{Player1}/buttonSouth");
        ///
        /// // Move the tag from one gamepad to another.
        /// InputSystem.SetDeviceUsage(myGamepad, null); // Clears usages on 'myGamepad'.
        /// InputSystem.SetDeviceUsage(otherGamepad, new InternedString("Player1"));
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        /// <seealso cref="InputControl.usages"/>
        /// <seealso cref="AddDeviceUsage(InputDevice,InternedString)"/>
        /// <seealso cref="RemoveDeviceUsage(InputDevice,InternedString)"/>
        /// <seealso cref="CommonUsages"/>
        /// <seealso cref="InputDeviceChange.UsageChanged"/>
        public static void SetDeviceUsage(InputDevice device, InternedString usage)
        {
            s_Manager.SetDeviceUsage(device, usage);
        }

        /// <summary>
        /// Add a usage tag to the given device.
        /// </summary>
        /// <param name="device">Device to add the usage to.</param>
        /// <param name="usage">New usage to add to the device.</param>
        /// <remarks>
        /// Usages allow to "tag" a specific device such that the tag can then be used in lookups
        /// and bindings. A common use is for identifying the handedness of an <see cref="XR.XRController"/>
        /// but the usages can be arbitrary strings.
        ///
        /// This method adds a new usage to the device's set of usages. If the device already has
        /// the given usage, the method does nothing. To instead set the device's usages to a single
        /// one, use <see cref="SetDeviceUsage(InputDevice,string)"/>. To remove usages from a device,
        /// call <see cref="RemoveDeviceUsage(InputDevice,string)"/>.
        ///
        /// The set of usages a device has can be queried with <see cref="InputControl.usages"/> (a device
        /// is an <see cref="InputControl"/> and thus, like controls, has an associated set of usages).
        ///
        /// If the set of usages on the device changes as a result of calling this method, <see cref="onDeviceChange"/>
        /// will be triggered with <see cref="InputDeviceChange.UsageChanged"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="usage"/> is null or empty.</exception>
        /// <seealso cref="InputControl.usages"/>
        /// <seealso cref="SetDeviceUsage(InputDevice,string)"/>
        /// <seealso cref="RemoveDeviceUsage(InputDevice,string)"/>
        /// <seealso cref="CommonUsages"/>
        /// <seealso cref="InputDeviceChange.UsageChanged"/>
        public static void AddDeviceUsage(InputDevice device, string usage)
        {
            s_Manager.AddDeviceUsage(device, new InternedString(usage));
        }

        /// <summary>
        /// Add a usage tag to the given device.
        /// </summary>
        /// <param name="device">Device to add the usage to.</param>
        /// <param name="usage">New usage to add to the device.</param>
        /// <remarks>
        /// Usages allow to "tag" a specific device such that the tag can then be used in lookups
        /// and bindings. A common use is for identifying the handedness of an <see cref="XR.XRController"/>
        /// but the usages can be arbitrary strings.
        ///
        /// This method adds a new usage to the device's set of usages. If the device already has
        /// the given usage, the method does nothing. To instead set the device's usages to a single
        /// one, use <see cref="SetDeviceUsage(InputDevice,InternedString)"/>. To remove usages from a device,
        /// call <see cref="RemoveDeviceUsage(InputDevice,InternedString)"/>.
        ///
        /// The set of usages a device has can be queried with <see cref="InputControl.usages"/> (a device
        /// is an <see cref="InputControl"/> and thus, like controls, has an associated set of usages).
        ///
        /// If the set of usages on the device changes as a result of calling this method, <see cref="onDeviceChange"/>
        /// will be triggered with <see cref="InputDeviceChange.UsageChanged"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="usage"/> is empty.</exception>
        /// <seealso cref="InputControl.usages"/>
        /// <seealso cref="SetDeviceUsage(InputDevice,InternedString)"/>
        /// <seealso cref="RemoveDeviceUsage(InputDevice,InternedString)"/>
        /// <seealso cref="CommonUsages"/>
        /// <seealso cref="InputDeviceChange.UsageChanged"/>
        public static void AddDeviceUsage(InputDevice device, InternedString usage)
        {
            s_Manager.AddDeviceUsage(device, usage);
        }

        /// <summary>
        /// Remove a usage tag from the given device.
        /// </summary>
        /// <param name="device">Device to remove the usage from.</param>
        /// <param name="usage">Usage to remove from the device.</param>
        /// <remarks>
        /// This method removes an existing usage from the given device. If the device does not
        /// have the given usage tag, the method does nothing. Use <see cref="SetDeviceUsage(InputDevice,string)"/>
        /// or <see cref="AddDeviceUsage(InputDevice,string)"/> to add usages to a device.
        ///
        /// The set of usages a device has can be queried with <see cref="InputControl.usages"/> (a device
        /// is an <see cref="InputControl"/> and thus, like controls, has an associated set of usages).
        ///
        /// If the set of usages on the device changes as a result of calling this method, <see cref="onDeviceChange"/>
        /// will be triggered with <see cref="InputDeviceChange.UsageChanged"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="usage"/> is null or empty.</exception>
        /// <seealso cref="InputControl.usages"/>
        /// <seealso cref="SetDeviceUsage(InputDevice,string)"/>
        /// <seealso cref="AddDeviceUsage(InputDevice,string)"/>
        /// <seealso cref="CommonUsages"/>
        /// <seealso cref="InputDeviceChange.UsageChanged"/>
        public static void RemoveDeviceUsage(InputDevice device, string usage)
        {
            s_Manager.RemoveDeviceUsage(device, new InternedString(usage));
        }

        /// <summary>
        /// Remove a usage tag from the given device.
        /// </summary>
        /// <param name="device">Device to remove the usage from.</param>
        /// <param name="usage">Usage to remove from the device.</param>
        /// <remarks>
        /// This method removes an existing usage from the given device. If the device does not
        /// have the given usage tag, the method does nothing. Use <see cref="SetDeviceUsage(InputDevice,InternedString)"/>
        /// or <see cref="AddDeviceUsage(InputDevice,InternedString)"/> to add usages to a device.
        ///
        /// The set of usages a device has can be queried with <see cref="InputControl.usages"/> (a device
        /// is an <see cref="InputControl"/> and thus, like controls, has an associated set of usages).
        ///
        /// If the set of usages on the device changes as a result of calling this method, <see cref="onDeviceChange"/>
        /// will be triggered with <see cref="InputDeviceChange.UsageChanged"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="usage"/> is empty.</exception>
        /// <seealso cref="InputControl.usages"/>
        /// <seealso cref="SetDeviceUsage(InputDevice,InternedString)"/>
        /// <seealso cref="AddDeviceUsage(InputDevice,InternedString)"/>
        /// <seealso cref="CommonUsages"/>
        /// <seealso cref="InputDeviceChange.UsageChanged"/>
        public static void RemoveDeviceUsage(InputDevice device, InternedString usage)
        {
            s_Manager.RemoveDeviceUsage(device, usage);
        }

        /// <summary>
        /// Find the first control that matches the given control path.
        /// </summary>
        /// <param name="path">Path of a control, e.g. <c>"&lt;Gamepad&gt;/buttonSouth"</c>. See <see cref="InputControlPath"/>
        /// for details.</param>
        /// <returns>The first control that matches the given path or <c>null</c> if no control matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c> or empty.</exception>
        /// <remarks>
        /// If multiple controls match the given path, which result is considered the first is indeterminate.
        ///
        /// <example>
        /// <code>
        /// // Add gamepad.
        /// InputSystem.AddDevice&lt;Gamepad&gt;();
        ///
        /// // Look up various controls on it.
        /// var aButton = InputSystem.FindControl("&lt;Gamepad&gt;/buttonSouth");
        /// var leftStickX = InputSystem.FindControl("*/leftStick/x");
        /// var bButton = InputSystem.FindControl"*/{back}");
        ///
        /// // This one returns the gamepad itself as devices are also controls.
        /// var gamepad = InputSystem.FindControl("&lt;Gamepad&gt;");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputControlPath"/>
        /// <seealso cref="InputControl.path"/>
        public static InputControl FindControl(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var devices = s_Manager.devices;
            var numDevices = devices.Count;

            for (var i = 0; i < numDevices; ++i)
            {
                var device = devices[i];
                var control = InputControlPath.TryFindControl(device, path);
                if (control != null)
                    return control;
            }

            return null;
        }

        /// <summary>
        /// Find all controls that match the given <see cref="InputControlPath">control path</see>.
        /// </summary>
        /// <param name="path">Control path to search for</param>
        /// <returns>List of <see cref="InputControl"/> which matched the given search criteria</returns>
        /// <example>
        /// <code>
        /// // Find all gamepads (literally: that use the "Gamepad" layout).
        /// InputSystem.FindControls("&lt;Gamepad&gt;");
        ///
        /// // Find all sticks on all gamepads.
        /// InputSystem.FindControls("&lt;Gamepad&gt;/*stick");
        ///
        /// // Same but filter stick by type rather than by name.
        /// InputSystem.FindControls&lt;StickControl&gt;("&lt;Gamepad&gt;/*");
        /// </code>
        /// </example>
        /// <seealso cref="FindControls{TControl}(string)"/>
        /// <seealso cref="FindControls{TControl}(string,ref UnityEngine.InputSystem.InputControlList{TControl})"/>
        public static InputControlList<InputControl> FindControls(string path)
        {
            return FindControls<InputControl>(path);
        }

        /// <summary>
        /// Find all controls that match the given <see cref="InputControlPath">control path</see>.
        /// </summary>
        /// <param name="path">Control path to search for</param>
        /// <typeparam name="TControl">Type of control <see cref="InputControl"/>.</typeparam>
        /// <returns>Generic list of <see cref="InputControl"/> which matched the given search criteria</returns>
        /// <seealso cref="FindControls{InputControl}(string)"/>
        /// <seealso cref="FindControls{TControl}(string,ref UnityEngine.InputSystem.InputControlList{TControl})"/>
        public static InputControlList<TControl> FindControls<TControl>(string path)
            where TControl : InputControl
        {
            var list = new InputControlList<TControl>();
            FindControls(path, ref list);
            return list;
        }

        /// <summary>
        /// Populate a list with all controls that match the given <see cref="InputControlPath">control path</see>.
        /// </summary>
        /// <param name="path">Control path to search for</param>
        /// <param name="controls">Generic list of <see cref="InputControl"/> to populate with the search results</param>
        /// <typeparam name="TControl">Type of control <see cref="InputControl"/>.</typeparam>
        /// <returns>Count of controls which matched the given search criteria</returns>
        /// <seealso cref="FindControls{TControl}(string)"/>
        /// <seealso cref="FindControls{TControl}(string,ref UnityEngine.InputSystem.InputControlList{TControl})"/>
        public static int FindControls<TControl>(string path, ref InputControlList<TControl> controls)
            where TControl : InputControl
        {
            return s_Manager.GetControls(path, ref controls);
        }

        #endregion

        #region Events

        internal static bool isProcessingEvents => s_Manager.isProcessingEvents;

        /// <summary>
        /// Called during <see cref="Update"/> for each event that is processed.
        /// </summary>
        /// <remarks>
        /// Every time the input system updates (see <see cref="InputSettings.updateMode"/>
        /// or <see cref="Update"/> for details about when and how this happens),
        /// it flushes all events from the internal event buffer.
        ///
        /// As the Input System reads events from the buffer one by one, it will trigger this
        /// callback for each event which originates from a recognized device, before then proceeding
        /// to process the event. If any of the callbacks sets <see cref="InputEvent.handled"/>
        /// to true, the event will be skipped and ignored.
        ///
        /// Note that a device that is disabled (see <see cref="InputDevice.enabled"/>) may still get
        /// this event signalled for it. A <see cref="DisableDeviceCommand"/> will usually be sent to
        /// backends when a device is disabled but a backend may or may not respond to the command and
        /// thus may or may not keep sending events for the device.
        ///
        /// Note that the Input System does NOT sort events by timestamps (<see cref="InputEvent.time"/>).
        /// Instead, they are consumed in the order they are produced. This means that they
        /// will also surface on this callback in that order.
        ///
        /// <example>
        /// <code>
        /// // Treat left+right mouse button as middle mouse button.
        /// // (Note: This example is more for demonstrative purposes; it isn't necessarily a good use case)
        /// InputSystem.onEvent +=
        ///    (eventPtr, device) =>
        ///    {
        ///        // Only deal with state events.
        ///        if (!eventPtr.IsA&lt;StateEvent&gt;())
        ///            return;
        ///
        ///        if (!(device is Mouse mouse))
        ///            return;
        ///
        ///        mouse.leftButton.ReadValueFromEvent(eventPtr, out var lmbDown);
        ///        mouse.rightButton.ReadValueFromEvent(eventPtr, out var rmbDown);
        ///
        ///        if (lmbDown > 0 &amp;&amp; rmbDown > 0)
        ///            mouse.middleButton.WriteValueIntoEvent(1f, eventPtr);
        ///    };
        /// </code>
        /// </example>
        ///
        /// The property returns an <see cref="InputEventListener"/> struct that, beyond adding and removing
        /// callbacks, can be used to flexibly listen in on the event stream.
        ///
        /// <example>
        /// <code>
        /// // Listen for mouse events.
        /// InputSystem.onEvent
        ///     .ForDevice(Mouse.current)
        ///     .Call(e => Debug.Log("Mouse event"));
        /// </code>
        /// </example>
        ///
        /// If you are looking for a way to capture events, <see cref="InputEventTrace"/> may be of
        /// interest and an alternative to directly hooking into this event.
        ///
        /// If you are looking to monitor changes to specific input controls, state change monitors
        /// (see <see cref="InputState.AddChangeMonitor(InputControl,IInputStateChangeMonitor,long,uint)"/>
        /// are usually a more efficient and convenient way to set this up.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Delegate reference is <c>null</c>.</exception>
        /// <seealso cref="QueueEvent(InputEventPtr)"/>
        /// <seealso cref="InputEvent"/>
        /// <seealso cref="Update"/>
        /// <seealso cref="InputSettings.updateMode"/>
        public static InputEventListener onEvent
        {
            // The listener syntax is an artificial struct. Setting it has no effect.
            // Its only purpose is to give us access to both the += and -= syntax of C# events
            // and at the same time provide a springboard into IObservable.
            get => default;
            // ReSharper disable once ValueParameterNotUsed
            set {}
        }

        /// <summary>
        /// Listen through <see cref="onEvent"/> for a button to be pressed.
        /// </summary>
        /// <remarks>
        /// The listener will get triggered whenever a <see cref="ButtonControl"/> on any device in the list of <see cref="devices"/>
        /// goes from not being pressed to being pressed.
        ///
        /// <example>
        /// <code>
        /// // Response to the first button press. Calls our delegate
        /// // and then immediately stops listening.
        /// InputSystem.onAnyButtonPress
        ///     .CallOnce(ctrl => Debug.Log($"Button {ctrl} was pressed"));
        /// </code>
        /// </example>
        ///
        /// Note that the listener will get triggered from the first button that was found in a pressed state in a
        /// given <see cref="InputEvent"/>. If multiple buttons are pressed in an event, the listener will not
        /// get triggered multiple times. To get all button presses in an event, use <see cref="InputControlExtensions.GetAllButtonPresses"/>
        /// and instead listen directly through <see cref="onEvent"/>.
        ///
        /// <example>
        /// <code>
        /// InputSystem.onEvent
        ///     .Where(e => e.HasButtonPress())
        ///     .CallOnce(eventPtr =>
        ///     {
        ///         foreach (var button in l.eventPtr.GetAllButtonPresses())
        ///             Debug.Log($"Button {button} was pressed");
        ///     });
        /// </code>
        /// </example>
        ///
        /// There is a certain overhead to listening for button presses so it is best to have listeners
        /// installed only while the information is actually needed.
        ///
        /// <example>
        /// <code>
        /// // Script that will spawn a new player when a button on a device is pressed.
        /// public class JoinPlayerOnPress : MonoBehaviour
        /// {
        ///     // We instantiate this GameObject to create a new player object.
        ///     // Expected to have a PlayerInput component in its hierarchy.
        ///     public GameObject playerPrefab;
        ///
        ///     // We want to remove the event listener we install through InputSystem.onAnyButtonPress
        ///     // after we're done so remember it here.
        ///     private IDisposable m_EventListener;
        ///
        ///     // When enabled, we install our button press listener.
        ///     void OnEnable()
        ///     {
        ///         // Start listening.
        ///         m_EventListener =
        ///             InputSystem.onAnyButtonPress
        ///                 .Call(OnButtonPressed)
        ///     }
        ///
        ///     // When disabled, we remove our button press listener.
        ///     void OnDisable()
        ///     {
        ///         m_EventListener.Dispose();
        ///     }
        ///
        ///     void OnButtonPressed(InputControl button)
        ///     {
        ///         var device = button.device;
        ///
        ///         // Ignore presses on devices that are already used by a player.
        ///         if (PlayerInput.FindFirstPairedToDevice(device) != null)
        ///             return;
        ///
        ///         // Create a new player.
        ///         var player = PlayerInput.Instantiate(playerPrefab, pairWithDevice: device);
        ///
        ///         // If the player did not end up with a valid input setup,
        ///         // unjoin the player.
        ///         if (player.hasMissingRequiredDevices)
        ///             Destroy(player);
        ///
        ///         // If we only want to join a single player, could uninstall our listener here
        ///         // or use CallOnce() instead of Call() when we set it up.
        ///     }
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="ButtonControl.isPressed"/>
        /// <seealso cref="onEvent"/>
        public static IObservable<InputControl> onAnyButtonPress =>
            onEvent
                .Select(e => e.GetFirstButtonPressOrNull()).Where(c => c != null);

        /// <summary>
        /// Add an event to the internal event queue.
        /// </summary>
        /// <param name="eventPtr">Event to add to the internal event buffer.</param>
        /// <exception cref="ArgumentException"><paramref name="eventPtr"/> is not
        /// valid (see <see cref="InputEventPtr.valid"/>).</exception>
        /// <exception cref="InvalidOperationException">The method was called from
        /// within event processing more than 1000 times. To avoid deadlocking, this
        /// results in an exception being thrown.</exception>
        /// <remarks>
        /// The event will be copied in full to the internal event buffer meaning that
        /// you can release memory for the event after it has been queued. The internal event
        /// buffer is flushed on the next input system update (see <see cref="Update"/>).
        /// Note that if input is process in <c>FixedUpdate()</c> (see <see cref="InputSettings.updateMode"/>),
        /// then the event may not get processed until its <see cref="InputEvent.time"/> timestamp
        /// is within the update window of the input system.
        ///
        /// As part of queuing, the event will receive its own unique ID (see <see cref="InputEvent.eventId"/>).
        /// Note that this ID will be written into the memory buffer referenced by <paramref cref="eventPtr"/>
        /// meaning that after calling <c>QueueEvent</c>, you will see the event ID with which the event
        /// was queued.
        ///
        /// Events that are queued during event processing will get processed in the same update.
        /// This happens, for example, when queuing input from within <see cref="onEvent"/> or from
        /// action callbacks such as <see cref="InputAction.performed"/>.
        ///
        /// The total size of <see cref="InputEvent"/>s processed in a single update is limited by
        /// <see cref="InputSettings.maxEventBytesPerUpdate"/>. This also prevents deadlocks when
        /// each processing of an event leads to one or more additional events getting queued.
        ///
        /// <example>
        /// <code>
        /// // Queue an input event on the first gamepad.
        /// var gamepad = Gamepad.all[0];
        /// using (StateEvent.From(gamepad, out var eventPtr))
        /// {
        ///     gamepad.leftStick.WriteValueIntoEvent(new Vector2(0.123f, 0.234f), eventPtr);
        ///     InputSystem.QueueEvent(eventPtr);
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="Update"/>
        /// <seealso cref="onEvent"/>
        /// <seealso cref="onBeforeUpdate"/>
        /// <seealso cref="InputEvent"/>
        public static void QueueEvent(InputEventPtr eventPtr)
        {
            if (!eventPtr.valid)
                throw new ArgumentException("Received a null event pointer", nameof(eventPtr));

            s_Manager.QueueEvent(eventPtr);
        }

        /// <summary>
        /// Add an event to the internal event queue.
        /// </summary>
        /// <typeparam name="TEvent">Type of event to look enqueue.</typeparam>
        /// <param name="inputEvent">Event to add to the internal event buffer.</param>
        /// <remarks>
        /// The event will be copied in full to the internal event buffer. The internal event
        /// buffer is flushed on the next input system update (see <see cref="Update"/>).
        /// Note that if input is process in <c>FixedUpdate()</c> (see <see cref="InputSettings.updateMode"/>),
        /// then the event may not get processed until its <see cref="InputEvent.time"/> timestamp
        /// is within the update window of the input system.
        ///
        /// As part of queuing, the event will receive its own unique ID (see <see cref="InputEvent.eventId"/>).
        /// Note that this ID will be written into <paramref name="inputEvent"/>
        /// meaning that after calling this method, you will see the event ID with which the event
        /// was queued.
        ///
        /// <example>
        /// <code>
        /// // Queue a disconnect event on the first gamepad.
        /// var inputEvent = DeviceRemoveEvent(Gamepad.all[0].deviceId);
        /// InputSystem.QueueEvent(inputEvent);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="Update"/>
        /// <seealso cref="onEvent"/>
        /// <seealso cref="onBeforeUpdate"/>
        public static void QueueEvent<TEvent>(ref TEvent inputEvent)
            where TEvent : struct, IInputEventTypeInfo
        {
            s_Manager.QueueEvent(ref inputEvent);
        }

        ////REVIEW: consider moving these out into extension methods in UnityEngine.InputSystem.LowLevel

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
        /// <summary>
        /// Queue a <see cref="StateEvent"/> to update the input state of the given device.
        /// </summary>
        /// <param name="device">Device whose input state to update</param>
        /// <param name="state"></param>
        /// <param name="time">Timestamp for the event. If not supplied, the current time is used. Note
        /// that if the given time is in the future and events processed in
        /// <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html">FixedUpdate</a> (see <see cref="InputSettings.updateMode"/>),
        /// the event will only get processed once the actual time has caught up with the given time.</param>
        /// <typeparam name="TState">Type of input state, such as <see cref="MouseState"/>. Must match the expected
        /// type of state of <paramref name="device"/>.</typeparam>
        /// <remarks>
        /// The given state must match exactly what is expected by the given device. If unsure, an alternative
        /// is to grab the state as an event directly from the device using <see
        /// cref="StateEvent.From(InputDevice,out InputEventPtr,Unity.Collections.Allocator)"/> which can then
        /// be queued using <see cref="QueueEvent(InputEventPtr)"/>.
        ///
        /// <example>
        /// <code>
        /// // Allocates temporary, unmanaged memory for the event.
        /// // using statement automatically disposes the memory once we have queued the event.
        /// using (StateEvent.From(Mouse.current, out var eventPtr))
        /// {
        ///     // Use controls on mouse to write values into event.
        ///     Mouse.current.position.WriteValueIntoEvent(new Vector(123, 234), eventPtr);
        ///
        ///     // Queue event.
        ///     InputSystem.QueueEvent(eventPtr);
        /// }
        /// </code>
        /// </example>
        ///
        /// The event will only be queued and not processed right away. This means that the state of
        /// <paramref name="device"/> will not change immediately as a result of calling this method. Instead,
        /// the event will be processed as part of the next input update.
        ///
        /// Note that this method updates the complete input state of the device including all of its
        /// controls. To update just part of the state of a device, you can use <see cref="QueueDeltaStateEvent{TDelta}"/>
        /// (however, note that there are some restrictions; see documentation).
        /// <example>
        /// <code>
        /// InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = new Vector(123, 234) });
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="device"/> has not been added to the system
        /// (<see cref="AddDevice(InputDevice)"/>) and thus cannot receive events.</exception>
        /// <exception cref="ArgumentException"></exception>
        public static unsafe void QueueStateEvent<TState>(InputDevice device, TState state, double time = -1)
            where TState : struct, IInputStateTypeInfo
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            // Make sure device is actually in the system.
            if (device.m_DeviceIndex == InputDevice.kInvalidDeviceIndex)
                throw new InvalidOperationException(
                    $"Cannot queue state event for device '{device}' because device has not been added to system");

            ////REVIEW: does it make more sense to go off the 'stateBlock' on the device and let that determine size?

            var stateSize = (uint)UnsafeUtility.SizeOf<TState>();
            if (stateSize > StateEventBuffer.kMaxSize)
                throw new ArgumentException(
                    $"Size of '{typeof(TState).Name}' exceeds maximum supported state size of {StateEventBuffer.kMaxSize}",
                    nameof(state));
            var eventSize = UnsafeUtility.SizeOf<StateEvent>() + stateSize - StateEvent.kStateDataSizeToSubtract;

            if (time < 0)
                time = InputRuntime.s_Instance.currentTime;
            else
                time += InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

            StateEventBuffer eventBuffer;
            eventBuffer.stateEvent =
                new StateEvent
            {
                baseEvent = new InputEvent(StateEvent.Type, (int)eventSize, device.deviceId, time),
                stateFormat = state.format
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

        /// <summary>
        /// Queue a <see cref="DeltaStateEvent"/> to update part of the input state of the given device.
        /// </summary>
        /// <param name="control">Control on a device to update state of.</param>
        /// <param name="delta">New state for the control. Type of state must match the state of the control.</param>
        /// <param name="time"></param>
        /// <typeparam name="TDelta"></typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is null.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static unsafe void QueueDeltaStateEvent<TDelta>(InputControl control, TDelta delta, double time = -1)
            where TDelta : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (control.stateBlock.bitOffset != 0)
                throw new InvalidOperationException(
                    $"Cannot send delta state events against bitfield controls: {control}");

            // Make sure device is actually in the system.
            var device = control.device;
            if (device.m_DeviceIndex == InputDevice.kInvalidDeviceIndex)
                throw new InvalidOperationException(
                    $"Cannot queue state event for control '{control}' on device '{device}' because device has not been added to system");

            if (time < 0)
                time = InputRuntime.s_Instance.currentTime;
            else
                time += InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

            var deltaSize = (uint)UnsafeUtility.SizeOf<TDelta>();
            if (deltaSize > DeltaStateEventBuffer.kMaxSize)
                throw new ArgumentException(
                    $"Size of state delta '{typeof(TDelta).Name}' exceeds maximum supported state size of {DeltaStateEventBuffer.kMaxSize}",
                    nameof(delta));

            ////TODO: recognize a matching C# representation of a state format and convert to what we expect for trivial cases
            if (deltaSize != control.stateBlock.alignedSizeInBytes)
                throw new ArgumentException(
                    $"Size {deltaSize} of delta state of type {typeof(TDelta).Name} provided for control '{control}' does not match size {control.stateBlock.alignedSizeInBytes} of control",
                    nameof(delta));

            var eventSize = UnsafeUtility.SizeOf<DeltaStateEvent>() + deltaSize - 1;

            DeltaStateEventBuffer eventBuffer;
            eventBuffer.stateEvent =
                new DeltaStateEvent
            {
                baseEvent = new InputEvent(DeltaStateEvent.Type, (int)eventSize, device.deviceId, time),
                stateFormat = device.stateBlock.format,
                stateOffset = control.m_StateBlock.byteOffset - device.m_StateBlock.byteOffset
            };

            var ptr = eventBuffer.stateEvent.stateData;
            UnsafeUtility.MemCpy(ptr, UnsafeUtility.AddressOf(ref delta), deltaSize);

            s_Manager.QueueEvent(ref eventBuffer.stateEvent);
        }

        /// <summary>
        /// Queue a <see cref="DeviceConfigurationEvent"/> that signals that the configuration of the given device has changed
        /// and that cached configuration will thus have to be refreshed.
        /// </summary>
        /// <param name="device">Device whose configuration has changed.</param>
        /// <param name="time">Timestamp for the event. If not supplied, the current time will be used.</param>
        /// <remarks>
        /// All state of an input device that is not input or output state is considered its "configuration".
        ///
        /// A simple example is keyboard layouts. A <see cref="Keyboard"/> will typically have an associated
        /// keyboard layout that dictates the function of each key and which can be changed by the user at the
        /// system level. In the input system, the current keyboard layout can be queried via <see cref="Keyboard.keyboardLayout"/>.
        /// When the layout changes at the system level, the input backend sends a configuration change event
        /// to signal that the configuration of the keyboard has changed and that cached data may be outdated.
        /// In response, <see cref="Keyboard"/> will flush out cached information such as the name of the keyboard
        /// layout and display names (<see cref="InputControl.displayName"/>) of individual keys which causes them
        /// to be fetched again from the backend the next time they are accessed.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="device"/> has not been added
        /// (<see cref="InputDevice.added"/>; <see cref="AddDevice(InputDevice)"/>) and thus cannot
        /// receive events.</exception>
        public static void QueueConfigChangeEvent(InputDevice device, double time = -1)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (device.deviceId == InputDevice.InvalidDeviceId)
                throw new InvalidOperationException("Device has not been added");

            if (time < 0)
                time = InputRuntime.s_Instance.currentTime;
            else
                time += InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

            var inputEvent = DeviceConfigurationEvent.Create(device.deviceId, time);
            s_Manager.QueueEvent(ref inputEvent);
        }

        /// <summary>
        /// Queue a <see cref="TextEvent"/> on the given device.
        /// </summary>
        /// <param name="device">Device to queue the event on.</param>
        /// <param name="character">Text character to input through the event.</param>
        /// <param name="time">Optional event time stamp. If not supplied, the current time will be used.</param>
        /// <remarks>
        /// Text input is sent to devices character by character. This allows sending strings of arbitrary
        /// length without necessary incurring GC overhead.
        ///
        /// For the event to have any effect on <paramref name="device"/>, the device must
        /// implement <see cref="ITextInputReceiver"/>. It will see <see cref="ITextInputReceiver.OnTextInput"/>
        /// being called when the event is processed.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="device"/> is a device that has not been
        /// added to the system.</exception>
        /// <seealso cref="Keyboard.onTextInput"/>
        public static void QueueTextEvent(InputDevice device, char character, double time = -1)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (device.deviceId == InputDevice.InvalidDeviceId)
                throw new InvalidOperationException("Device has not been added");

            if (time < 0)
                time = InputRuntime.s_Instance.currentTime;
            else
                time += InputRuntime.s_CurrentTimeOffsetToRealtimeSinceStartup;

            var inputEvent = TextEvent.Create(device.deviceId, character, time);
            s_Manager.QueueEvent(ref inputEvent);
        }

        /// <summary>
        /// Run a single update of input state.
        /// </summary>
        /// <remarks>
        /// Except in tests and when using <see cref="InputSettings.UpdateMode.ProcessEventsManually"/>, this method should not
        /// normally be called. The input system will automatically update as part of the player loop as
        /// determined by <see cref="InputSettings.updateMode"/>. Calling this method is equivalent to
        /// inserting extra frames, i.e. it will advance the entire state of the input system by one complete
        /// frame.
        ///
        /// When using <see cref="InputUpdateType.Manual"/>, this method MUST be called for input to update in the
        /// player. Not calling the method as part of the player loop may result in excessive memory
        /// consumption and/or potential loss of input.
        ///
        /// Each update will flush out buffered input events and cause them to be processed. This in turn
        /// will update the state of input devices (<see cref="InputDevice"/>) and trigger actions (<see cref="InputAction"/>)
        /// that monitor affected device state.
        /// </remarks>
        /// <seealso cref="InputUpdateType"/>
        /// <seealso cref="InputSettings.updateMode"/>
        public static void Update()
        {
            s_Manager.Update();
        }

        internal static void Update(InputUpdateType updateType)
        {
            if (updateType != InputUpdateType.None && (s_Manager.updateMask & updateType) == 0)
                throw new InvalidOperationException(
                    $"'{updateType}' updates are not enabled; InputSystem.settings.updateMode is set to '{settings.updateMode}'");
            s_Manager.Update(updateType);
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
        public static event Action onBeforeUpdate
        {
            add
            {
                lock (s_Manager)
                    s_Manager.onBeforeUpdate += value;
            }
            remove
            {
                lock (s_Manager)
                    s_Manager.onBeforeUpdate -= value;
            }
        }

        /// <summary>
        /// Event that is fired after the input system has completed an update and processed all pending events.
        /// </summary>
        /// <seealso cref="onBeforeUpdate"/>
        /// <seealso cref="Update"/>
        public static event Action onAfterUpdate
        {
            add
            {
                lock (s_Manager)
                    s_Manager.onAfterUpdate += value;
            }
            remove
            {
                lock (s_Manager)
                    s_Manager.onAfterUpdate -= value;
            }
        }

        #endregion

        #region Settings

        /// <summary>
        /// The current configuration of the input system.
        /// </summary>
        /// <value>Global configuration object for the input system.</value>
        /// <remarks>
        /// The input system can be configured on a per-project basis. Settings can either be created and
        /// installed on the fly or persisted as assets in the project.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Value is null when setting the property.</exception>
        public static InputSettings settings
        {
            get => s_Manager.settings;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (s_Manager.m_Settings == value)
                    return;

                // In the editor, we keep track of the settings asset through EditorBuildSettings.
                #if UNITY_EDITOR
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(value)))
                {
                    EditorBuildSettings.AddConfigObject(InputSettingsProvider.kEditorBuildSettingsConfigKey,
                        value, true);
                }
                #endif

                s_Manager.settings = value;
            }
        }

        /// <summary>
        /// Event that is triggered if any of the properties in <see cref="settings"/> changes or if
        /// <see cref="settings"/> is replaced entirely with a new <see cref="InputSettings"/> object.
        /// </summary>
        /// <seealso cref="settings"/>
        /// <seealso cref="InputSettings"/>
        public static event Action onSettingsChange
        {
            add => s_Manager.onSettingsChange += value;
            remove => s_Manager.onSettingsChange -= value;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Callback that can be used to display a warning and draw additional custom Editor UI for bindings.
        /// </summary>
        /// <seealso cref="InputBinding"/>
        /// <remarks>
        /// This allows Users to control the behavior of the <see cref="InputActionAsset"/> Editor.
        /// Specifically this controls whether a warning icon will appear next to a particular
        /// <see cref="InputBinding"/> in the list and also draw custom UI content for it once
        /// it is selected.
        /// By default no callbacks exist and therefore no warnings or custom content will be shown.
        /// A User interested in customizing this behavior is expected to provide a callback function here.
        /// This callback function will receive the binding path to be inspected.
        /// The callback is then expected to either return null to indicate no warning is to be displayed
        /// for this binding path or a <see cref="System.Action"/> which contains the custom rendering function
        /// to be shown in the Binding properties panel when a InputBinding has been selected.
        /// Returning any <see cref="System.Action"/> will also display a small warning icon next to the
        /// particular <see cref="InputBinding"/> in the list, regardless of the contents of that function.
        /// </remarks>
        ///
        /// <example>
        /// <code>
        /// InputSystem.customBindingPathValidators += (string bindingPath) => {
        ///     // Mark <Gamepad> bindings with a warning
        ///     if (!bindingPath.StartsWith("<Gamepad>"))
        ///         return null;
        ///
        ///     // Draw the warning information in the Binding Properties panel
        ///     return () =>
        ///     {
        ///         GUILayout.BeginVertical("GroupBox");
        ///         GUILayout.BeginHorizontal();
        ///         GUILayout.Box(EditorGUIUtility.FindTexture("console.warnicon.sml"));
        ///         GUILayout.Label(
        ///             "This binding is inactive because it refers to a disabled OpenXR interaction profile.",
        ///             EditorStyles.wordWrappedLabel);
        ///         GUILayout.EndHorizontal();
        ///
        ///         GUILayout.Button("Manage Interaction Profiles");
        ///         GUILayout.EndVertical();
        ///     };
        /// };
        /// </code>
        /// </example>
        public static event CustomBindingPathValidator customBindingPathValidators
        {
            add => s_Manager.customBindingPathValidators += value;
            remove => s_Manager.customBindingPathValidators -= value;
        }

        /// <summary>
        /// Invokes any custom UI rendering code for this Binding Path in the editor.
        /// </summary>
        /// <seealso cref="customBindingPathValidators"/>
        /// <remarks>
        /// This is called internally by the <see cref="InputActionAsset"/> Editor while displaying
        /// the properties for a <see cref="InputBinding"/>.
        /// This is not intended to be called directly.
        /// Please use <see cref="customBindingPathValidators"/> instead.
        /// </remarks>
        internal static void OnDrawCustomWarningForBindingPath(string bindingPath)
        {
            s_Manager.OnDrawCustomWarningForBindingPath(bindingPath);
        }

        /// <summary>
        /// Determines if any warning icon is to be displayed for this Binding Path in the editor.
        /// </summary>
        /// <seealso cref="customBindingPathValidators"/>
        /// <remarks>
        /// This is called internally by the <see cref="InputActionAsset"/> Editor while displaying
        /// the list of each <see cref="InputBinding"/>.
        /// This is not intended to be called directly.
        /// Please use <see cref="customBindingPathValidators"/> instead.
        /// </remarks>
        internal static bool ShouldDrawWarningIconForBinding(string bindingPath)
        {
            return s_Manager.ShouldDrawWarningIconForBinding(bindingPath);
        }

#endif

        #endregion

        #region Actions

        /// <summary>
        /// Event that is signalled when the state of enabled actions in the system changes or
        /// when actions are triggered.
        /// </summary>
        /// <remarks>
        /// The object received by the callback is either an <see cref="InputAction"/>,
        /// <see cref="InputActionMap"/>, or <see cref="InputActionAsset"/> depending on whether the
        /// <see cref="InputActionChange"/> affects a single action, an entire action map, or an
        /// entire action asset.
        ///
        /// For <see cref="InputActionChange.BoundControlsAboutToChange"/> and <see cref="InputActionChange.BoundControlsChanged"/>,
        /// the given object is an <see cref="InputAction"/> if the action is not part of an action map,
        /// an <see cref="InputActionMap"/> if the actions are part of a map but not part of an asset, and an
        /// <see cref="InputActionAsset"/> if the actions are part of an asset. In other words, the notification is
        /// sent for the topmost object in the hierarchy.
        /// </remarks>
        /// <example>
        /// <code>
        /// InputSystem.onActionChange +=
        ///     (obj, change) =>
        ///     {
        ///         if (change == InputActionChange.ActionPerformed)
        ///         {
        ///             var action = (InputAction)obj;
        ///             var control = action.activeControl;
        ///             //...
        ///         }
        ///         else if (change == InputActionChange.ActionMapEnabled)
        ///         {
        ///             var actionMap = (InputActionMap)obj;
        ///             //...
        ///         }
        ///         else if (change == InputActionChange.BoundControlsChanged)
        ///         {
        ///             // This is one way to deal with the fact that obj may be an InputAction
        ///             // InputActionMap, or InputActionAsset and may be part of an InputActionAsset or not.
        ///             var action = obj as InputAction;
        ///             var actionMap = action?.actionMap ?? obj as InputActionMap;
        ///             var actionAsset = actionMap?.asset ?? obj as InputActionAsset;
        ///
        ///             // Note that if bound controls are changed on any map in an asset, there *will*
        ///             // be a BoundControlsChanged notification for the entire asset.
        ///
        ///             //...
        ///         }
        ///     };
        /// </code>
        /// </example>
        /// <seealso cref="InputAction.controls"/>
        public static event Action<object, InputActionChange> onActionChange
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                InputActionState.s_GlobalState.onActionChange.AddCallback(value);
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                InputActionState.s_GlobalState.onActionChange.RemoveCallback(value);
            }
        }

        /// <summary>
        /// Register a new type of interaction with the system.
        /// </summary>
        /// <param name="type">Type that implements the interaction. Must support <see cref="InputInteraction"/>.</param>
        /// <param name="name">Name to register the interaction with. This is used in bindings to refer to the interaction
        /// (e.g. an interactions called "Tap" can be added to a binding by listing it in its <see cref="InputBinding.interactions"/>
        /// property). If no name is supplied, the short name of <paramref name="type"/> is used (with "Interaction" clipped off
        /// the name if the type name ends in that).</param>
        /// <example>
        /// <code>
        /// // Interaction that is performed when control resets to default state.
        /// public class ResetInteraction : InputInteraction
        /// {
        ///     public void Process(ref InputInteractionContext context)
        ///     {
        ///         if (context.isWaiting &amp;&amp; !context.controlHasDefaultValue)
        ///             context.Started();
        ///         else if (context.isStarted &amp;&amp; context.controlHasDefaultValue)
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
        /// <seealso cref="RegisterInteraction{T}"/>
        /// <seealso cref="TryGetInteraction"/>
        /// <seealso cref="ListInteractions"/>
        public static void RegisterInteraction(Type type, string name = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrEmpty(name))
            {
                name = type.Name;
                if (name.EndsWith("Interaction"))
                    name = name.Substring(0, name.Length - "Interaction".Length);
            }

            s_Manager.interactions.AddTypeRegistration(name, type);
        }

        /// <summary>
        /// Register a new type of interaction with the system.
        /// </summary>
        /// <typeparam name="T">Type that implements the interaction. Must support <see cref="InputInteraction"/>.</typeparam>
        /// <param name="name">Name to register the interaction with. This is used in bindings to refer to the interaction
        /// (e.g. an interactions called "Tap" can be added to a binding by listing it in its <see cref="InputBinding.interactions"/>
        /// property). If no name is supplied, the short name of <typeparamref name="T"/> is used (with "Interaction" clipped off
        /// the name if the type name ends in that).</param>
        /// <seealso cref="IInputInteraction"/>
        /// <seealso cref="RegisterInteraction(Type, string)"/>
        /// <seealso cref="TryGetInteraction"/>
        /// <seealso cref="ListInteractions"/>
        public static void RegisterInteraction<T>(string name = null)
        {
            RegisterInteraction(typeof(T), name);
        }

        ////REVIEW: can we move the getters and listers somewhere else? maybe `interactions` and `processors` properties and such?

        /// <summary>
        /// Search for a registered interaction type with the given name.
        /// </summary>
        /// <param name="name">Name of the registered interaction to search for.</param>
        /// <returns>The type of the interaction, if one was previously registered with the give name, otherwise null.</returns>
        /// <seealso cref="IInputInteraction"/>
        /// <seealso cref="RegisterInteraction"/>
        /// <seealso cref="ListInteractions"/>
        public static Type TryGetInteraction(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            return s_Manager.interactions.LookupTypeRegistration(name);
        }

        /// <summary>
        /// Gets the names of of all currently registered interactions.
        /// </summary>
        /// <returns>A list of currently registered interaction names.</returns>
        /// <seealso cref="IInputInteraction"/>
        /// <seealso cref="RegisterInteraction"/>
        /// <seealso cref="TryGetInteraction"/>
        public static IEnumerable<string> ListInteractions()
        {
            return s_Manager.interactions.names;
        }

        /// <summary>
        /// Register a new type of binding composite with the system.
        /// </summary>
        /// <param name="type">Type that implements the binding composite. Must support <see cref="InputBindingComposite"/>.</param>
        /// <param name="name">Name to register the binding composite with. This is used in bindings to refer to the composite.</param>
        /// <seealso cref="InputBindingComposite"/>
        /// <seealso cref="RegisterBindingComposite{T}"/>
        /// <seealso cref="TryGetBindingComposite"/>
        public static void RegisterBindingComposite(Type type, string name)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrEmpty(name))
            {
                name = type.Name;
                if (name.EndsWith("Composite"))
                    name = name.Substring(0, name.Length - "Composite".Length);
            }

            s_Manager.composites.AddTypeRegistration(name, type);
        }

        /// <summary>
        /// Register a new type of binding composite with the system.
        /// </summary>
        /// <typeparam name="T">Type that implements the binding composite. Must support <see cref="InputBindingComposite"/>.</typeparam>
        /// <param name="name">Name to register the binding composite with. This is used in bindings to refer to the composite.</param>
        /// <seealso cref="InputBindingComposite"/>
        /// <seealso cref="RegisterBindingComposite(Type, string)"/>
        /// <seealso cref="TryGetBindingComposite"/>
        public static void RegisterBindingComposite<T>(string name = null)
        {
            RegisterBindingComposite(typeof(T), name);
        }

        /// <summary>
        /// Search for a registered binding composite type with the given name.
        /// </summary>
        /// <param name="name">Name of the registered binding composite to search for.</param>
        /// <returns>The type of the binding composite, if one was previously registered with the give name, otherwise null.</returns>
        /// <seealso cref="InputBindingComposite"/>
        /// <seealso cref="RegisterBindingComposite"/>
        public static Type TryGetBindingComposite(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            return s_Manager.composites.LookupTypeRegistration(name);
        }

        /// <summary>
        /// Disable all actions (and implicitly all action sets) that are currently enabled.
        /// </summary>
        /// <seealso cref="ListEnabledActions()"/>
        /// <seealso cref="InputAction.Disable"/>
        public static void DisableAllEnabledActions()
        {
            InputActionState.DisableAllActions();
        }

        /// <summary>
        /// Return a list of all the actions that are currently enabled in the system.
        /// </summary>
        /// <returns>A new list instance containing all currently enabled actions.</returns>
        /// <remarks>
        /// To avoid allocations, use <see cref="ListEnabledActions(List{UnityEngine.InputSystem.InputAction})"/>.
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
                throw new ArgumentNullException(nameof(actions));
            return InputActionState.FindAllEnabledActions(actions);
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
        public static InputRemoting remoting => s_Remote;

        #endregion

        /// <summary>
        /// The current version of the input system package.
        /// </summary>
        /// <value>Current version of the input system.</value>
        public static Version version => new Version(kAssemblyVersion);

        /// <summary>
        /// Property for internal use that allows setting the player to run in the background.
        /// </summary>
        /// <remarks>
        /// Some platforms don't care about <see cref="Application.runInBackground"/> and for those we need to
        /// enable it manually through this propriety.
        /// </remarks>
        /// <param name="value">The boolean value to set to <see cref="NativeInputRuntime.runInBackground"/></param>
        public static bool runInBackground
        {
            get => s_Manager.m_Runtime.runInBackground;
            set => s_Manager.m_Runtime.runInBackground = value;
        }

        ////REVIEW: restrict metrics to editor and development builds?
        /// <summary>
        /// Get various up-to-date metrics about the input system.
        /// </summary>
        /// <value>Up-to-date metrics on input system activity.</value>
        public static InputMetrics metrics => s_Manager.metrics;

        internal static InputManager s_Manager;
        internal static InputRemoting s_Remote;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        internal static RemoteInputPlayerConnection s_RemoteConnection;

        private static void SetUpRemoting()
        {
            Debug.Assert(s_Manager != null);

            #if UNITY_EDITOR
            s_Remote = new InputRemoting(s_Manager);
            // NOTE: We use delayCall as our initial startup will run in editor initialization before
            //       PlayerConnection is itself ready. If we call Bind() directly here, we won't
            //       see any errors but the callbacks we register for will not trigger.
            EditorApplication.delayCall += SetUpRemotingInternal;
            #else
            s_Remote = new InputRemoting(s_Manager);
            SetUpRemotingInternal();
            #endif
        }

        private static void SetUpRemotingInternal()
        {
            if (s_RemoteConnection == null)
            {
                #if UNITY_EDITOR
                s_RemoteConnection = RemoteInputPlayerConnection.instance;
                s_RemoteConnection.Bind(EditorConnection.instance, false);
                #else
                s_RemoteConnection = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();
                s_RemoteConnection.Bind(PlayerConnection.instance, PlayerConnection.instance.isConnected);
                #endif
            }

            s_Remote.Subscribe(s_RemoteConnection); // Feed messages from players into editor.
            s_RemoteConnection.Subscribe(s_Remote); // Feed messages from editor into players.
        }

        #if !UNITY_EDITOR
        private static bool ShouldEnableRemoting()
        {
#if UNITY_INCLUDE_TESTS
            var isRunningTests = true;
#else
            var isRunningTests = false;
#endif
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
        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.SubsystemRegistration)]
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

        // Initialization is triggered by accessing InputSystem. Some parts (like InputActions)
        // do not rely on InputSystem and thus can be accessed without tapping InputSystem.
        // This method will explicitly make sure we trigger initialization.
        internal static void EnsureInitialized()
        {
        }

#if UNITY_EDITOR
        internal static InputSystemObject s_SystemObject;

        internal static void InitializeInEditor(IInputRuntime runtime = null)
        {
            Profiler.BeginSample("InputSystem.InitializeInEditor");
            Reset(runtime: runtime);

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

                // Restore editor settings.
                InputEditorUserSettings.s_Settings = s_SystemObject.systemState.userSettings;

                // Get rid of saved state.
                s_SystemObject.systemState = new State();
            }
            else
            {
                s_SystemObject = ScriptableObject.CreateInstance<InputSystemObject>();
                s_SystemObject.hideFlags = HideFlags.HideAndDontSave;

                // See if we have a remembered settings object.
                if (EditorBuildSettings.TryGetConfigObject(InputSettingsProvider.kEditorBuildSettingsConfigKey,
                    out InputSettings settingsAsset))
                {
                    if (s_Manager.m_Settings.hideFlags == HideFlags.HideAndDontSave)
                        ScriptableObject.DestroyImmediate(s_Manager.m_Settings);
                    s_Manager.m_Settings = settingsAsset;
                    s_Manager.ApplySettings();
                }

                InputEditorUserSettings.Load();

                SetUpRemoting();
            }

            Debug.Assert(settings != null);
            #if UNITY_EDITOR
            Debug.Assert(EditorUtility.InstanceIDToObject(settings.GetInstanceID()) != null,
                "InputSettings has lost its native object");
            #endif

            // If native backends for new input system aren't enabled, ask user whether we should
            // enable them (requires restart). We only ask once per session and don't ask when
            // running in batch mode.
            if (!s_SystemObject.newInputBackendsCheckedAsEnabled &&
                !EditorPlayerSettingHelpers.newSystemBackendsEnabled &&
                !s_Manager.m_Runtime.isInBatchMode)
            {
                const string dialogText = "This project is using the new input system package but the native platform backends for the new input system are not enabled in the player settings. " +
                    "This means that no input from native devices will come through." +
                    "\n\nDo you want to enable the backends? Doing so will *RESTART* the editor.";

                if (EditorUtility.DisplayDialog("Warning", dialogText, "Yes", "No"))
                {
                    EditorPlayerSettingHelpers.newSystemBackendsEnabled = true;
                    EditorHelpers.RestartEditorAndRecompileScripts();
                }
            }
            s_SystemObject.newInputBackendsCheckedAsEnabled = true;

            RunInitialUpdate();

            Profiler.EndSample();
        }

        internal static void OnPlayModeChange(PlayModeStateChange change)
        {
            ////REVIEW: should we pause haptics when play mode is paused and stop haptics when play mode is exited?

            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    s_SystemObject.settings = JsonUtility.ToJson(settings);
                    s_SystemObject.exitEditModeTime = InputRuntime.s_Instance.currentTime;
                    s_SystemObject.enterPlayModeTime = 0;
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    s_SystemObject.enterPlayModeTime = InputRuntime.s_Instance.currentTime;
                    s_Manager.SyncAllDevicesAfterEnteringPlayMode();
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    s_Manager.LeavePlayMode();
                    break;

                ////TODO: also nuke all callbacks installed on InputActions and InputActionMaps
                ////REVIEW: is there any other cleanup work we want to before? should we automatically nuke
                ////        InputDevices that have been created with AddDevice<> during play mode?
                case PlayModeStateChange.EnteredEditMode:

                    // Nuke all InputUsers.
                    InputUser.ResetGlobals();

                    // Nuke all InputActionMapStates. Releases their unmanaged memory.
                    InputActionState.DestroyAllActionMapStates();

                    // Restore settings.
                    if (!string.IsNullOrEmpty(s_SystemObject.settings))
                    {
                        JsonUtility.FromJsonOverwrite(s_SystemObject.settings, settings);
                        s_SystemObject.settings = null;
                        settings.OnChange();
                    }

                    // reload input action assets marked as dirty from disk
                    if (s_TrackedDirtyAssets == null)
                        return;

                    foreach (var assetGuid in s_TrackedDirtyAssets)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                        if (string.IsNullOrEmpty(assetPath))
                            continue;

                        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    }

                    s_TrackedDirtyAssets.Clear();

                    break;
            }
        }

        private static void OnProjectChange()
        {
            ////TODO: use dirty count to find whether settings have actually changed
            // May have added, removed, moved, or renamed settings asset. Force a refresh
            // of the UI.
            InputSettingsProvider.ForceReload();

            // Also, if the asset holding our current settings got deleted, switch back to a
            // temporary settings object.
            // NOTE: We access m_Settings directly here to make sure we're not running into asserts
            //       from the settings getter checking it has a valid object.
            if (EditorUtility.InstanceIDToObject(s_Manager.m_Settings.GetInstanceID()) == null)
            {
                var newSettings = ScriptableObject.CreateInstance<InputSettings>();
                newSettings.hideFlags = HideFlags.HideAndDontSave;
                settings = newSettings;
            }
        }

        private static HashSet<string> s_TrackedDirtyAssets;

        /// <summary>
        /// Keep track of InputActionAsset assets that you want to re-load on exiting Play mode. This is useful because
        /// some user actions, such as adding a new input binding at runtime, change the in-memory representation of the
        /// input action asset and those changes survive when exiting Play mode. If you re-open an Input
        /// Action Asset in the Editor that has been changed this way, you see the new bindings that have been added
        /// during Play mode which you might not typically want to happen.
        ///
        /// You can avoid this by force re-loading from disk any asset that has been marked as dirty.
        /// </summary>
        /// <param name="asset"></param>
        internal static void TrackDirtyInputActionAsset(InputActionAsset asset)
        {
            if (s_TrackedDirtyAssets == null)
                s_TrackedDirtyAssets = new HashSet<string>();

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string assetGuid, out long _) == false)
                return;

            s_TrackedDirtyAssets.Add(assetGuid);
        }

#else
        private static void InitializeInPlayer(IInputRuntime runtime = null, InputSettings settings = null)
        {
            if (settings == null)
                settings = Resources.FindObjectsOfTypeAll<InputSettings>().FirstOrDefault() ?? ScriptableObject.CreateInstance<InputSettings>();

            // No domain reloads in the player so we don't need to look for existing
            // instances.
            s_Manager = new InputManager();
            s_Manager.Initialize(runtime ?? NativeInputRuntime.instance, settings);

#if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
            PerformDefaultPluginInitialization();
#endif

            // Automatically enable remoting in development players.
#if DEVELOPMENT_BUILD
            if (ShouldEnableRemoting())
                SetUpRemoting();
#endif
        }

#endif // UNITY_EDITOR

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RunInitialUpdate()
        {
            // Request an initial Update so that user methods such as Start and Awake
            // can access the input devices.
            //
            // NOTE: We use InputUpdateType.None here to run a "null" update. InputManager.OnBeforeUpdate()
            //       and InputManager.OnUpdate() will both early out when comparing this to their update
            //       mask but will still restore devices. This means we're not actually processing input,
            //       but we will force the runtime to push its devices.
            Update(InputUpdateType.None);
        }

#if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
        private static void PerformDefaultPluginInitialization()
        {
            UISupport.Initialize();

            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA || UNITY_ANDROID || UNITY_IOS || UNITY_TVOS
            XInputSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_PS4 || UNITY_PS5 || UNITY_WSA || UNITY_ANDROID || UNITY_IOS || UNITY_TVOS
            DualShockSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
            HIDSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_ANDROID
            Android.AndroidSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
            iOS.iOSSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_STANDALONE_OSX
            OSX.OSXSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_WEBGL
            WebGL.WebGLSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WSA
            Switch.SwitchSupportHID.Initialize();
            #endif

            #if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !UNITY_FORCE_INPUTSYSTEM_XR_OFF
            XR.XRSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_STANDALONE_LINUX
            Linux.LinuxSupport.Initialize();
            #endif

            #if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS || UNITY_TVOS || UNITY_WSA
            OnScreen.OnScreenSupport.Initialize();
            #endif

            #if (UNITY_EDITOR || UNITY_STANDALONE) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
            Steam.SteamSupport.Initialize();
            #endif

            #if UNITY_EDITOR
            UnityRemoteSupport.Initialize();
            #endif
        }

#endif // UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION

        // For testing, we want the ability to push/pop system state even in the player.
        // However, we don't want it in release players.
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>
        /// Return the input system to its default state.
        /// </summary>
        private static void Reset(bool enableRemoting = false, IInputRuntime runtime = null)
        {
            Profiler.BeginSample("InputSystem.Reset");

            // Some devices keep globals. Get rid of them by pretending the devices
            // are removed.
            if (s_Manager != null)
            {
                foreach (var device in s_Manager.devices)
                    device.NotifyRemoved();

                s_Manager.UninstallGlobals();
            }

            // Create temporary settings. In the tests, this is all we need. But outside of tests,d
            // this should get replaced with an actual InputSettings asset.
            var settings = ScriptableObject.CreateInstance<InputSettings>();
            settings.hideFlags = HideFlags.HideAndDontSave;

            #if UNITY_EDITOR
            s_Manager = new InputManager();
            s_Manager.Initialize(runtime ?? NativeInputRuntime.instance, settings);

            s_Manager.m_Runtime.onPlayModeChanged = OnPlayModeChange;
            s_Manager.m_Runtime.onProjectChange = OnProjectChange;

            InputEditorUserSettings.s_Settings = new InputEditorUserSettings.SerializedState();

            if (enableRemoting)
                SetUpRemoting();

            #if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
            PerformDefaultPluginInitialization();
            #endif

            #else
            InitializeInPlayer(runtime, settings);
            #endif

            Mouse.s_PlatformMouseDevice = null;

            InputEventListener.s_ObserverState = default;
            InputUser.ResetGlobals();
            EnhancedTouchSupport.Reset();
            Profiler.EndSample();
        }

        /// <summary>
        /// Destroy the current setup of the input system.
        /// </summary>
        /// <remarks>
        /// NOTE: This also de-allocates data we're keeping in unmanaged memory!
        /// </remarks>
        private static void Destroy()
        {
            // NOTE: Does not destroy InputSystemObject. We want to destroy input system
            //       state repeatedly during tests but we want to not create InputSystemObject
            //       over and over.

            s_Manager.Destroy();
            if (s_RemoteConnection != null)
                Object.DestroyImmediate(s_RemoteConnection);
            #if UNITY_EDITOR
            EditorInputControlLayoutCache.Clear();
            InputDeviceDebuggerWindow.s_OnToolbarGUIActions.Clear();
            InputEditorUserSettings.s_Settings = new InputEditorUserSettings.SerializedState();
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
            #if UNITY_EDITOR
            [SerializeField] public InputEditorUserSettings.SerializedState userSettings;
            [SerializeField] public string systemObject;
            #endif
            ////TODO: make these saved states capable of surviving domain reloads
            [NonSerialized] public ISavedState inputActionState;
            [NonSerialized] public ISavedState touchState;
            [NonSerialized] public ISavedState inputUserState;
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

            ////FIXME: does not preserve global state in InputActionState
            ////TODO: preserve InputUser state
            ////TODO: preserve EnhancedTouchSupport state

            s_SavedStateStack.Push(new State
            {
                manager = s_Manager,
                remote = s_Remote,
                remoteConnection = s_RemoteConnection,
                managerState = s_Manager.SaveState(),
                remotingState = s_Remote?.SaveState() ?? new InputRemoting.SerializedState(),
                #if UNITY_EDITOR
                userSettings = InputEditorUserSettings.s_Settings,
                systemObject = JsonUtility.ToJson(s_SystemObject),
                #endif
                inputActionState = InputActionState.SaveAndResetState(),
                touchState = EnhancedTouch.Touch.SaveAndResetState(),
                inputUserState = InputUser.SaveAndResetState()
            });

            Reset(enableRemoting, runtime ?? InputRuntime.s_Instance); // Keep current runtime.
        }

        ////FIXME: this method doesn't restore things like InputDeviceDebuggerWindow.onToolbarGUI
        /// <summary>
        /// Restore the state of the system from the last state pushed with <see cref="SaveAndReset"/>.
        /// </summary>
        internal static void Restore()
        {
            Debug.Assert(s_SavedStateStack != null && s_SavedStateStack.Count > 0);

            // Load back previous state.
            var state = s_SavedStateStack.Pop();

            state.inputUserState.StaticDisposeCurrentState();
            state.touchState.StaticDisposeCurrentState();
            state.inputActionState.StaticDisposeCurrentState();

            // Nuke what we have.
            Destroy();

            state.inputUserState.RestoreSavedState();
            state.touchState.RestoreSavedState();
            state.inputActionState.RestoreSavedState();

            s_Manager = state.manager;
            s_Remote = state.remote;
            s_RemoteConnection = state.remoteConnection;

            InputUpdate.Restore(state.managerState.updateState);

            s_Manager.InstallRuntime(s_Manager.m_Runtime);
            s_Manager.InstallGlobals();
            s_Manager.ApplySettings();

            #if UNITY_EDITOR
            InputEditorUserSettings.s_Settings = state.userSettings;
            JsonUtility.FromJsonOverwrite(state.systemObject, s_SystemObject);
            #endif

            // Get devices that keep global lists (like Gamepad) to re-initialize them
            // by pretending the devices have been added.
            foreach (var device in devices)
            {
                device.NotifyAdded();
                device.MakeCurrent();
            }
        }

#endif
    }
}
