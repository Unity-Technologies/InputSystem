using System;
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

        public static void RegisterTemplate(InputTemplate template)
        {
            m_Manager.RegisterTemplate(template);
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
                
                // Post-domain-reload steps.
                m_Manager.InitializeStatics();
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
                     TakeSnapshot();
                     break;
                 
                 case PlayModeStateChange.EnteredEditMode:
                     RestoreSnapshot();
                     break;
            }
        }

        internal static void TakeSnapshot()
        {
            ////TODO
        }

        internal static void RestoreSnapshot()
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