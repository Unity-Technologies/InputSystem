using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif
using UnityEngine.InputSystem.LowLevel;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Extension of class to provide test-specific functionality
    /// </summary>
    public static partial class InputSystem
    {
#if UNITY_EDITOR
        internal static void TestHook_InitializeForPlayModeTests(bool enableRemoting, IInputRuntime runtime)
        {
            s_Manager = InputManager.CreateAndInitialize(runtime, null);

            s_Manager.runtime.onPlayModeChanged = InputSystem.OnPlayModeChange;
            s_Manager.runtime.onProjectChange = InputSystem.OnProjectChange;

            InputEditorUserSettings.s_Settings = new InputEditorUserSettings.SerializedState();

            if (enableRemoting)
                InputSystem.SetUpRemoting();

#if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
            InputSystem.PerformDefaultPluginInitialization();
#endif
        }

        internal static void TestHook_SimulateDomainReload(IInputRuntime runtime)
        {
            // This quite invasive goes into InputSystem internals. Unfortunately, we
            // have no proper way of simulating domain reloads ATM. So we directly call various
            // internal methods here in a sequence similar to what we'd get during a domain reload.
            // Since we're faking it, pass 'true' for calledFromCtor param.

            InputSystem.s_DomainStateManager.OnBeforeSerialize();
            InputSystem.s_DomainStateManager = null;
            InputSystem.s_Manager = null; // Do NOT Dispose()! The native memory cannot be freed as it's reference by saved state
            InputSystem.InitializeInEditor(true, runtime);
        }
#endif // UNITY_EDITOR

        /// <summary>
        /// Destroy the current setup of the input system.
        /// </summary>
        /// <remarks>
        /// NOTE: This also de-allocates data we're keeping in unmanaged memory!
        /// </remarks>
        internal static void TestHook_DestroyAndReset()
        {
            // NOTE: Does not destroy InputSystemObject. We want to destroy input system
            //       state repeatedly during tests but we want to not create InputSystemObject
            //       over and over.
            InputSystem.manager.Dispose();
            if (InputSystem.s_RemoteConnection != null)
                Object.DestroyImmediate(InputSystem.s_RemoteConnection);

#if UNITY_EDITOR
            EditorInputControlLayoutCache.Clear();
            InputDeviceDebuggerWindow.s_OnToolbarGUIActions.Clear();
            InputEditorUserSettings.s_Settings = new InputEditorUserSettings.SerializedState();
#endif

            InputSystem.s_Manager = null;
            InputSystem.s_RemoteConnection = null;
            InputSystem.s_Remote = null;
        }

        internal static void TestHook_RestoreFromSavedState(InputSystemState savedState)
        {
            s_Manager = savedState.manager;
            s_Remote = savedState.remote;
            s_RemoteConnection = savedState.remoteConnection;
        }

        internal static void TestHook_SwitchToDifferentInputManager(InputManager otherManager)
        {
            s_Manager = otherManager;
            InputStateBuffers.SwitchTo(otherManager.m_StateBuffers, otherManager.defaultUpdateType);
        }
    }
}

#endif // UNITY_EDITOR || UNITY_INCLUDE_TESTS
