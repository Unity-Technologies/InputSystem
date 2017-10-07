using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
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
        public static ReadOnlyArray<InputDevice> devices
        {
            get { return s_Manager.devices; }
        }

        public static event UnityAction<InputDevice, InputDeviceChange> onDeviceChange
        {
            add { s_Manager.onDeviceChange += value; }
            remove { s_Manager.onDeviceChange -= value; }
        }

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

        public static string TryFindTemplate(InputDeviceDescription deviceDescription)
        {
            return s_Manager.TryFindTemplate(deviceDescription);
        }

        public static InputDevice AddDevice(string template)
        {
            return s_Manager.AddDevice(template);
        }

        public static InputDevice AddDevice(InputDeviceDescription description)
        {
            return s_Manager.AddDevice(description);
        }

        public static void AddDevice(InputDevice device)
        {
            s_Manager.AddDevice(device);
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
            var stateSize = UnsafeUtility.SizeOf<TState>();
            if (stateSize > StateEventBuffer.kMaxSize)
                throw new ArgumentException(
                    $"Size of '{typeof(TState).Name}' exceeds maximum supported state size of {StateEventBuffer.kMaxSize}",
                    nameof(state));
            var eventSize = UnsafeUtility.SizeOf<StateEvent>() + stateSize - 1;

            if (time < 0)
                time = Time.time;

            StateEventBuffer eventBuffer;
            eventBuffer.stateEvent = new StateEvent();

            eventBuffer.stateEvent.baseEvent = new InputEvent(StateEvent.Type, eventSize, device.id, time);
            eventBuffer.stateEvent.stateFormat = state.GetFormat();

            fixed(byte* ptr = eventBuffer.stateEvent.stateData)
            {
                UnsafeUtility.MemCpy(new IntPtr(ptr), UnsafeUtility.AddressOf(ref state), stateSize);
            }

            s_Manager.QueueEvent(ref eventBuffer.stateEvent);
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

        internal static InputManager s_Manager;

#if UNITY_EDITOR
        private static bool s_Initialized;
        private static InputSystemObject m_SystemObject;

        static InputSystem()
        {
            // Unity's InitializeOnLoad force-executes static class constructors without
            // checking if they have already been executed (violating C# semantics). So
            // if someone class into InputSystem before Unity has gone through its InitializeOnLoad
            // sequence, we will see two execution of the class constructor for a single
            // domain load. We catch this with s_Initialized (which will reset on domain
            // reloads).

            if (s_Initialized)
                return;

            var existingSystemObjects = Resources.FindObjectsOfTypeAll<InputSystemObject>();
            if (existingSystemObjects != null && existingSystemObjects.Length > 0)
            {
                m_SystemObject = existingSystemObjects[0];
                s_Manager = m_SystemObject.manager;
                s_Manager.InstallGlobals();
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
            if (m_SystemObject != null)
                UnityEngine.Object.DestroyImmediate(m_SystemObject);
            m_SystemObject = ScriptableObject.CreateInstance<InputSystemObject>();
            s_Manager = m_SystemObject.manager;
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

        //have to also update current device statics

        internal static void Save()
        {
            ////TODO
        }

        internal static void Restore()
        {
            ////TODO
        }

#else
        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeInPlayer()
        {
            // No domain reloads in the player so we don't need to look for existing
            // instances.
            m_Manager = new InputManager();
        }

#endif
    }
}
