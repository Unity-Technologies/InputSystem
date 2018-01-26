using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using ISX.LowLevel;
using ISX.Utilities;
using UnityEngineInternal.Input;

#if UNITY_EDITOR
using UnityEditor;
using ISX.Editor;
#else
using UnityEngine.Networking.PlayerConnection;
#endif

#if !NET_4_0
using ISX.Net35Compatibility;
#endif

// I'd like to call the DLLs UnityEngine.Input and UnityEngine.Input.Tests
// but the .asmdef mechanism doesn't seem to work properly when there's periods
// in the name of the .asmdef file and it also doesn't seem to work correctly
// when the name of the .asmdef file and the name of the assembly don't match.
// At least, while it compiles, I get missing references errors looking at the
// .asmdef files in the inspector and the test runner doesn't seem to be able
// to run.
//
// Unfortunately, we need the attribute for the test rig to be able to access
// InputSystem.Save(), InputSystem.Restore(), and InputSystem.Reset(). Don't
// feel comfortable exposing those even though I'd prefer for the tests to not
// be able to rely on internals.
[assembly: InternalsVisibleTo("InputSystemTests")]

namespace ISX
{
    /// <summary>
    /// This is the central hub for the input system.
    /// </summary>
    // Takes care of the singletons we need and presents a sanitized API.
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class InputSystem
    {
        #region Templates

        /// <summary>
        /// Event that is signalled when the template setup in the system changes.
        /// </summary>
        public static event Action<string, InputTemplateChange> onTemplateChange
        {
            add { s_Manager.onTemplateChange += value; }
            remove { s_Manager.onTemplateChange -= value; }
        }

        public static void RegisterTemplate(Type type, string name = null, InputDeviceDescription? deviceDescription = null)
        {
            if (name == null)
                name = type.Name;

            s_Manager.RegisterTemplate(name, type, deviceDescription);
        }

        public static void RegisterTemplate<T>(string name = null, InputDeviceDescription? deviceDescription = null)
        {
            RegisterTemplate(typeof(T), name, deviceDescription);
        }

        public static void RegisterTemplate(string json, string name = null)
        {
            s_Manager.RegisterTemplate(json, name);
        }

        /// <summary>
        /// Register a constructor that delivers an <see cref="InputTemplate"/> instance on demand.
        /// </summary>
        /// <param name="constructor"></param>
        /// <param name="name"></param>
        /// <param name="baseTemplate"></param>
        /// <param name="deviceDescription"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// The given expression must be a lambda expression solely comprised of a method call with
        /// no arguments. Can be static or instance method call. If it is an instance method, the
        /// instance object must be serializable.
        ///
        /// The reason for these restrictions and for not taking an arbitrary delegate is that we
        /// need to be able to persist the template constructor between domain reloads.
        ///
        /// Note that the template that is being constructed must not vary over time (except between
        /// domain reloads).
        /// </remarks>
        /// <example>
        /// <code>
        /// [Serializable]
        /// class MyTemplateConstructor
        /// {
        ///     public InputTemplate Build()
        ///     {
        ///         var builder = new InputTemplate.Builder()
        ///             .WithType<MyDevice>();
        ///         builder.AddControl("button1").WithTemplate("Button");
        ///         return builder.Build();
        ///     }
        /// }
        ///
        /// var constructor = new MyTemplateConstructor();
        /// InputSystem.RegisterTemplateConstructor(() => constructor.Build(), "MyTemplate");
        /// </code>
        /// </example>
        public static void RegisterTemplateConstructor(Expression<Func<InputTemplate>> constructor, string name,
            string baseTemplate = null, InputDeviceDescription? deviceDescription = null)
        {
            if (constructor == null)
                throw new ArgumentNullException("constructor");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name");

            // Grab method and (optional) instance from lambda expression.
            var methodCall = constructor.Body as MethodCallExpression;
            if (methodCall == null)
                throw new ArgumentException(
                    string.Format("Body of template constructor must be a method call (is a {0} instead)",
                        constructor.Body.NodeType),
                    "constructor");

            var method = methodCall.Method;
            var instance = methodCall.Object.NodeType == ExpressionType.Constant
                ? ((ConstantExpression)methodCall.Object).Value
                : Expression.Lambda(methodCall.Object).Compile().DynamicInvoke();

            // Register.
            s_Manager.RegisterTemplateConstructor(method, instance, name, baseTemplate: baseTemplate,
                deviceDescription: deviceDescription);
        }

        /// <summary>
        /// Remove an already registered template from the system.
        /// </summary>
        /// <param name="name">Name of the template to remove. Note that template names are case-insensitive.</param>
        /// <remarks>
        /// Note that removing a template also removes all devices that directly or indirectly
        /// use the template.
        ///
        /// This method can be used to remove both control or device templates.
        /// </remarks>
        public static void RemoveTemplate(string name)
        {
            s_Manager.RemoveTemplate(name);
        }

        public static string TryFindMatchingTemplate(InputDeviceDescription deviceDescription)
        {
            return s_Manager.TryFindMatchingTemplate(deviceDescription);
        }

        public static List<string> ListTemplates()
        {
            var list = new List<string>();
            s_Manager.ListTemplates(list);
            return list;
        }

        public static int ListTemplates(List<string> list)
        {
            return s_Manager.ListTemplates(list);
        }

        public static InputTemplate TryLoadTemplate(string name)
        {
            ////FIXME: this will intern the name even if the operation fails
            return s_Manager.TryLoadTemplate(new InternedString(name));
        }

        #endregion

        #region Processors

        public static void RegisterProcessor(Type type, string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = type.Name;
                if (name.EndsWith("Processor"))
                    name = name.Substring(0, name.Length - "Processor".Length);
            }

            s_Manager.RegisterProcessor(name, type);
        }

        public static void RegisterProcessor<T>(string name = null)
        {
            RegisterProcessor(typeof(T), name);
        }

        public static Type TryGetProcessor(string name)
        {
            return s_Manager.TryGetProcessor(name);
        }

        #endregion

        #region Devices

        /// <summary>
        /// The list of currently connected devices.
        /// </summary>
        /// <remarks>
        /// Note that accessing this property does not allocate. It gives read-only access
        /// directly to the system's internal array of devices.
        /// </remarks>
        public static ReadOnlyArray<InputDevice> devices
        {
            get { return s_Manager.devices; }
        }

        /// <summary>
        /// Event that is signalled when the device setup in the system changes.
        /// </summary>
        /// <remarks>
        /// This can be used to detect when device are added or removed as well as
        /// detecting when existing device change their configuration.
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
        /// Event that is signalled when the system is trying to match a template to
        /// a device it has discovered.
        /// </summary>
        /// <remarks>
        /// This event allows customizing the template discovery process and to generate
        /// templates on the fly, if need be. The system will invoke callbacks with the
        /// name of the template it has matched to the device based on the current template setup.
        /// If all the callbacks return <c>null</c>, that template will be instantiated. If,
        /// however, any of the callbacks returns a new name instead, the system will use that
        /// template instead.
        ///
        /// To generate templates on the fly, register them with the system in the callback and
        /// then return the name of the newly generated template from the callback.
        ///
        /// Note that this callback will also be invoked if the system could not match any
        /// existing template to the device. In that case, the <c>matchedTemplate</c> argument
        /// to the callback will be <c>null</c>.
        ///
        /// Callbacks also receive a device ID and reference to the input runtime. For devices
        /// where more information has to be fetched from the runtime in order to generate a
        /// template, this allows issuing <see cref="IInputRuntime.IOCTL"/> calls for the device.
        /// Note that for devices that are not coming from the runtime (i.e. devices created
        /// directly in script code), the device ID will be <see cref="InputDevice.kInvalidDeviceId"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// InputSystem.onFindTemplateForDevice +=
        ///     (deviceId, description, matchedTemplate, runtime) =>
        ///     {
        ///         ////TODO: complete example
        ///     };
        /// </code>
        /// </example>
        public static event DeviceFindTemplateCallback onFindTemplateForDevice
        {
            add { s_Manager.onFindTemplateForDevice += value; }
            remove { s_Manager.onFindTemplateForDevice -= value; }
        }

        /// <summary>
        /// Add a new device by instantiating the given device template.
        /// </summary>
        /// <param name="template">Name of the template to instantiate. Must be a device template. Note that
        /// template names are case-insensitive.</param>
        /// <param name="name">Name to assign to the device. If null, the template name is used instead. Note that
        /// device names are made unique automatically by the system by appending numbers to them (e.g. "gamepad",
        /// "gamepad1", "gamepad2", etc.).</param>
        /// <returns>The newly created input device.</returns>
        /// <remarks>
        /// Note that adding a device to the system will allocate and also create garbage on the GC heap.
        /// </remarks>
        /// <example>
        /// <code>
        /// InputSystem.AddDevice("Gamepad");
        /// </code>
        /// </example>
        public static InputDevice AddDevice(string template, string name = null)
        {
            return s_Manager.AddDevice(template, name);
        }

        public static TDevice AddDevice<TDevice>(string name = null)
            where TDevice : InputDevice
        {
            var device = s_Manager.AddDevice(typeof(TDevice), name) as TDevice;
            if (device == null)
                throw new Exception(string.Format("Template registered for type '{0}' did not produce a device of that type; template probably has been overridden",
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

        public static InputDevice TryGetDevice(string nameOrTemplate)
        {
            return s_Manager.TryGetDevice(nameOrTemplate);
        }

        public static InputDevice GetDevice(string nameOrTemplate)
        {
            return s_Manager.GetDevice(nameOrTemplate);
        }

        public static InputDevice TryGetDeviceById(int deviceId)
        {
            return s_Manager.TryGetDeviceById(deviceId);
        }

        ////REVIEW: this seems somewhat pointless without also agreeing on an ID for the device
        public static void ReportAvailableDevice(InputDeviceDescription description)
        {
            s_Manager.ReportAvailableDevice(description);
        }

        public static int GetUnrecognizedDevices(List<InputDeviceDescription> descriptions)
        {
            return s_Manager.GetUnrecognizedDevices(descriptions);
        }

        #endregion

        #region Controls

        public static void SetVariant(InputControl control, string variant)
        {
            s_Manager.SetVariant(control, variant);
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
            return s_Manager.GetControls(path, controls);
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
            NativeInputSystem.QueueInputEvent(eventPtr.data);
        }

        public static void QueueEvent<TEvent>(ref TEvent inputEvent)
            where TEvent : struct, IInputEventTypeInfo
        {
            s_Manager.QueueEvent(ref inputEvent);
        }

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
                time = Time.time;

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
        public static unsafe void QueueStateEvent<TDelta>(InputControl control, TDelta delta, double time = -1)
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
                time = Time.time;

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
                time = Time.time;

            var inputEvent = DeviceConfigurationEvent.Create(device.id, time);
            s_Manager.QueueEvent(ref inputEvent);
        }

        public static void QueueTextEvent(InputDevice device, char character, double time = -1)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (device.id == InputDevice.kInvalidDeviceId)
                throw new InvalidOperationException("Device has not been added");

            if (time < 0)
                time = Time.time;

            var inputEvent = TextEvent.Create(device.id, character, time);
            s_Manager.QueueEvent(ref inputEvent);
        }

        ////REVIEW: should we actually expose the Update() methods or should these be internal?
        public static void Update()
        {
            s_Manager.Update();
        }

        public static void Update(InputUpdateType updateType)
        {
            s_Manager.Update(updateType);
        }

        #endregion

        #region Actions

        public static void RegisterModifier(Type type, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = type.Name;
                if (name.EndsWith("Modifier"))
                    name = name.Substring(0, name.Length - "Modifier".Length);
            }

            s_Manager.RegisterModifier(name, type);
        }

        public static void RegisterModifier<T>(string name = null)
        {
            RegisterModifier(typeof(T), name);
        }

        public static Type TryGetModifier(string name)
        {
            return s_Manager.TryGetModifier(name);
        }

        public static IEnumerable<string> ListModifiers()
        {
            return s_Manager.ListModifiers();
        }

        public static void DisableAllEnabledActions()
        {
            InputActionSet.DisableAllEnabledActions();
        }

        // Return a list of all the actions that are currently enabled in the system.
        public static List<InputAction> FindAllEnabledActions()
        {
            var result = new List<InputAction>();
            FindAllEnabledActions(result);
            return result;
        }

        // Add all actions that are currently enabled in the system to the given list
        // and return the number of such actions that have been found.
        public static int FindAllEnabledActions(List<InputAction> actions)
        {
            if (actions == null)
                throw new ArgumentNullException("actions");
            return InputActionSet.FindEnabledActions(actions);
        }

        #endregion

        #region Plugins

        public static void RegisterPluginManager(IInputPluginManager manager)
        {
            s_Manager.RegisterPluginManager(manager);
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
                InputDebuggerWindow.ReviveAfterDomainReload();
            }
            else
            {
                Reset();
            }

            EditorApplication.playModeStateChanged += OnPlayModeChange;
        }

        // We don't want play mode modifications to templates and controls to seep
        // back out into edit so we take a snapshot of the InputManager state before
        // going into play mode and then restore it when going back to edit mode.
        // NOTE: We *do* want device discoveries that have happened to still show
        //       through in edit mode, though not with any template settings made by
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
            s_Manager.Initialize();

            ////TODO: put this behind a switch so that it is off by default
            // Automatically enable remoting in development players.
            #if DEVELOPMENT_BUILD
            s_ConnectionToEditor = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();
            s_Remote = new InputRemoting(s_Manager, startSendingOnConnect: true);
            s_Remote.Subscribe(s_ConnectionToEditor);
            s_ConnectionToEditor.Subscribe(s_Remote);
            s_ConnectionToEditor.Bind(PlayerConnection.instance, PlayerConnection.instance.isConnected);
            #endif
        }

#endif // UNITY_EDITOR

        // For testing, we want the ability to push/pop system state even in the player.
        // However, we don't want it in release players.
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        internal static void Reset()
        {
            #if UNITY_EDITOR
            if (s_SystemObject != null)
                UnityEngine.Object.DestroyImmediate(s_SystemObject);
            s_SystemObject = ScriptableObject.CreateInstance<InputSystemObject>();
            s_Manager = s_SystemObject.manager;
            s_Remote = s_SystemObject.remote;
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
                var index = s_SerializedStateStack.Count - 1;
                s_Manager.RestoreState(s_SerializedStateStack[index]);
                s_SerializedStateStack.RemoveAt(index);
            }
        }

#endif
    }
}
