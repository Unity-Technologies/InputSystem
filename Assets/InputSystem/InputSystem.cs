using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ISX.Remote;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using ISX.Editor;
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
    // The primary API for the input system.
    // Takes care of the singletons we need and presents a sanitized API.
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class InputSystem
    {
        #region Templates

        public static void RegisterTemplate(Type type, string name = null)
        {
            if (name == null)
                name = type.Name;

            s_Manager.RegisterTemplate(name, type);
        }

        public static void RegisterTemplate<T>(string name = null)
        {
            RegisterTemplate(typeof(T), name);
        }

        public static void RegisterTemplate(string json, string name = null)
        {
            s_Manager.RegisterTemplate(json, name);
        }

        // Register a constructor that delivers an InputTemplate instance on demand.
        //
        // The given expression must be a lambda expression solely comprised of a method call with
        // no arguments. Can be static or instance method call. If it is an instance method, the
        // instance object must be serializable.
        //
        // The reason for these restrictions and for not taking an arbitrary delegate is that we
        // need to be able to persist the template constructor between domain reloads.
        //
        // NOTE: The template that is being constructed must not vary over time (except between
        //       domain reloads).
        public static void RegisterTemplateConstructor(Expression<Func<InputTemplate>> constructor, string name,
            string baseTemplate = null, InputDeviceDescription? deviceDescription = null)
        {
            if (constructor == null)
                throw new ArgumentNullException(nameof(constructor));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            // Grab method and (optional) instance from lambda expression.
            var methodCall = constructor.Body as MethodCallExpression;
            if (methodCall == null)
                throw new ArgumentException(
                    $"Body of template constructor must be a method call (is a {constructor.Body.NodeType} instead)",
                    nameof(constructor));

            var method = methodCall.Method;
            var instance = methodCall.Object.NodeType == ExpressionType.Constant
                ? ((ConstantExpression)methodCall.Object).Value
                : Expression.Lambda(methodCall.Object).Compile().DynamicInvoke();

            // Register.
            s_Manager.RegisterTemplateConstructor(method, instance, name, baseTemplate: baseTemplate,
                deviceDescription: deviceDescription);
        }

        //public static void RegisterTemplateMethod<T>(string )

        public static string TryFindMatchingTemplate(InputDeviceDescription deviceDescription)
        {
            return s_Manager.TryFindMatchingTemplate(deviceDescription);
        }

        public static IEnumerable<string> ListTemplates()
        {
            throw new NotImplementedException();
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

        public static ReadOnlyArray<InputDevice> devices => s_Manager.devices;

        public static event Action<InputDevice, InputDeviceChange> onDeviceChange
        {
            add { s_Manager.onDeviceChange += value; }
            remove { s_Manager.onDeviceChange -= value; }
        }

        public static event Func<InputDeviceDescription, string, string> onDeviceDiscovered
        {
            add { s_Manager.onDeviceDiscovered += value; }
            remove { s_Manager.onDeviceDiscovered -= value; }
        }

        public static InputDevice AddDevice(string template, string name = null)
        {
            return s_Manager.AddDevice(template, name);
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
                throw new ArgumentNullException(nameof(device));

            // Make sure device is actually in the system.
            if (device.m_DeviceIndex == InputDevice.kInvalidDeviceIndex)
                throw new InvalidOperationException(
                    $"Cannot queue state event device '{device}' because device has not been added to system");

            ////REVIEW: does it make more sense to go off the 'stateBlock' on the device and let that determine size?

            var stateSize = UnsafeUtility.SizeOf<TState>();
            if (stateSize > StateEventBuffer.kMaxSize)
                throw new ArgumentException(
                    $"Size of '{typeof(TState).Name}' exceeds maximum supported state size of {StateEventBuffer.kMaxSize}",
                    nameof(state));
            var eventSize = UnsafeUtility.SizeOf<StateEvent>() + stateSize - 1;

            if (time < 0)
                time = Time.time;

            StateEventBuffer eventBuffer;
            eventBuffer.stateEvent =
                new StateEvent
            {
                baseEvent = new InputEvent(StateEvent.Type, eventSize, device.id, time),
                stateFormat = state.GetFormat()
            };


            fixed(byte* ptr = eventBuffer.stateEvent.stateData)
            {
                UnsafeUtility.MemCpy(new IntPtr(ptr), UnsafeUtility.AddressOf(ref state), stateSize);
            }

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
                throw new ArgumentNullException(nameof(control));

            if (control.stateBlock.bitOffset != 0)
                throw new InvalidOperationException($"Cannot send delta state events against bitfield controls: {control}");

            // Make sure device is actually in the system.
            var device = control.device;
            if (device.m_DeviceIndex == InputDevice.kInvalidDeviceIndex)
                throw new InvalidOperationException(
                    $"Cannot queue state event for control '{control}' on device '{device}' because device has not been added to system");

            if (time < 0)
                time = Time.time;

            var deltaSize = UnsafeUtility.SizeOf<TDelta>();
            if (deltaSize > DeltaStateEventBuffer.kMaxSize)
                throw new ArgumentException(
                    $"Size of state delta '{typeof(TDelta).Name}' exceeds maximum supported state size of {DeltaStateEventBuffer.kMaxSize}",
                    nameof(delta));

            ////TODO: recognize a matching C# representation of a state format and convert to what we expect for trivial cases
            if (deltaSize != control.stateBlock.alignedSizeInBytes)
                throw new NotImplementedException("Delta state and control format don't match");

            var eventSize = UnsafeUtility.SizeOf<DeltaStateEvent>() + deltaSize - 1;

            DeltaStateEventBuffer eventBuffer;
            eventBuffer.stateEvent =
                new DeltaStateEvent
            {
                baseEvent = new InputEvent(DeltaStateEvent.Type, eventSize, device.id, time),
                stateFormat = device.stateBlock.format,
                stateOffset = control.m_StateBlock.byteOffset
            };

            fixed(byte* ptr = eventBuffer.stateEvent.stateData)
            {
                UnsafeUtility.MemCpy(new IntPtr(ptr), UnsafeUtility.AddressOf(ref delta), deltaSize);
            }

            s_Manager.QueueEvent(ref eventBuffer.stateEvent);
        }

        public static void QueueDisconnectEvent(InputDevice device, double time = -1)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (device.id == InputDevice.kInvalidDeviceId)
                throw new InvalidOperationException("Device has not been added");

            if (time < 0)
                time = Time.time;

            var inputEvent = DisconnectEvent.Create(device.id, time);
            s_Manager.QueueEvent(ref inputEvent);
        }

        public static void QueueConnectEvent(InputDevice device, double time = -1)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (device.id == InputDevice.kInvalidDeviceId)
                throw new InvalidOperationException("Device has not been added");

            if (time < 0)
                time = Time.time;

            var inputEvent = ConnectEvent.Create(device.id, time);
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
                throw new ArgumentNullException(nameof(actions));
            return InputActionSet.FindEnabledActions(actions);
        }

        #endregion

        #region Remoting

        public static InputRemoting remote
        {
            get
            {
                if (s_Remote == null && s_Manager != null)
                {
                    #if UNITY_EDITOR
                    s_Remote = s_SystemObject.remote;
                    #else
                    s_Remote = new InputRemoting(s_Manager);
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

#if UNITY_EDITOR
        private static bool s_Initialized;
        private static InputSystemObject s_SystemObject;

        static InputSystem()
        {
            // Unity's InitializeOnLoad force-executes static class constructors without
            // checking if they have already been executed (violating C# semantics). So
            // if someone calls into InputSystem before Unity has gone through its InitializeOnLoad
            // sequence, we will see two executions of the class constructor for a single
            // domain load. We catch this with s_Initialized (which will reset on domain
            // reloads).

            if (s_Initialized)
                return;

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

            s_Initialized = true;
        }

        internal static void Reset()
        {
            if (s_SystemObject != null)
                UnityEngine.Object.DestroyImmediate(s_SystemObject);
            s_SystemObject = ScriptableObject.CreateInstance<InputSystemObject>();
            s_Manager = s_SystemObject.manager;
            s_Remote = s_SystemObject.remote;
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
                s_Manager.InstallGlobals();
                s_SerializedStateStack.RemoveAt(index);
            }
        }

#else
        #if DEVELOPMENT_BUILD
        private static RemoteInputNetworkTransportToEditor s_RemoteEditorConnection;
        #endif

        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeInPlayer()
        {
            // No domain reloads in the player so we don't need to look for existing
            // instances.
            s_Manager = new InputManager();

            ////TODO: put this behind a switch so that it is off by default
            // Automatically enable remoting in development players.
            #if DEVELOPMENT_BUILD
            s_Remote = new InputRemoting(s_Manager);
            s_RemoteEditorConnection = new RemoteInputNetworkTransportToEditor();
            s_Remote.Subscribe(s_RemoteEditorConnection);
            s_RemoteEditorConnection.Subscribe(s_Remote);
            s_Remote.StartSending();
            #endif
        }

#endif
    }
}
