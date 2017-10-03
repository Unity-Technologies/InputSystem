using System;
using System.Collections.ObjectModel;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
            get { return m_Manager.devices; }
        }
        
        public static void RegisterTemplate(Type type, string name = null)
        {
            if (name == null)
                name = type.Name;

            m_Manager.RegisterTemplate(name, type);
        }

        public static void RegisterTemplate<T>(string name = null)
        {
            RegisterTemplate(typeof(T), name);
        }

        public static void RegisterTemplate(string json, string name = null)
        {
            m_Manager.RegisterTemplate(json, name);
        }

        public static InputDevice AddDevice(string template)
        {
            return m_Manager.AddDevice(template);
        }

        public static InputDevice AddDevice(InputDeviceDescriptor descriptor)
        {
            throw new NotImplementedException();
        }

        public static void AddDevice(InputDevice device)
        {
            m_Manager.AddDevice(device);
        }

        public static void QueueEvent<TEvent>(TEvent inputEvent)
            where TEvent : struct, IInputEventTypeInfo
        {
            m_Manager.QueueEvent(inputEvent);
        }

        ////REVIEW: should we actually expose the Update() methods or should these be internal?
        public static void Update()
        {
            m_Manager.Update();
        }
        
        public static void Update(InputUpdateType updateType)
        {
            m_Manager.Update(updateType);
        }

        private static InputManager m_Manager;
        
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
            
            // We may get InitializeOnLoad-related calls to the static class constructor
            // *after* 
            var existingSystemObjects = Resources.FindObjectsOfTypeAll<InputSystemObject>();
            if (existingSystemObjects != null && existingSystemObjects.Length > 0)
            {
                m_SystemObject = existingSystemObjects[0];
                m_Manager = m_SystemObject.manager;
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
            m_Manager = m_SystemObject.manager;
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