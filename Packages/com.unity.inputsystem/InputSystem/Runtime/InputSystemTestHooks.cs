using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.LowLevel;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Extension of class to provide test-specific functionality.
    /// Editor-specific operations are handled via callbacks set by the Editor assembly.
    /// </summary>
    public static partial class InputSystem
    {
#if UNITY_EDITOR
        internal static Action<bool, IInputRuntime> s_TestHookInitializeForPlayModeTests;

        internal static void TestHook_InitializeForPlayModeTests(bool enableRemoting, IInputRuntime runtime)
        {
            s_TestHookInitializeForPlayModeTests?.Invoke(enableRemoting, runtime);
        }

#if !ENABLE_CORECLR
        internal static Action<IInputRuntime> s_TestHookSimulateDomainReload;

        internal static void TestHook_SimulateDomainReload(IInputRuntime runtime)
        {
            s_TestHookSimulateDomainReload?.Invoke(runtime);
        }

#endif
#endif // UNITY_EDITOR

        internal static Action s_TestHookEditorCleanup;

        internal static void TestHook_DestroyAndReset()
        {
            InputSystem.s_Manager?.Dispose();
            if (InputSystem.s_RemoteConnection != null)
                Object.DestroyImmediate(InputSystem.s_RemoteConnection);

            s_TestHookEditorCleanup?.Invoke();

            InputSystem.s_Manager = null;
            InputSystem.s_RemoteConnection = null;
            InputSystem.s_Remote = null;
        }

        internal static void TestHook_RestoreFromSavedState(InputManager manager, InputRemoting remote, RemoteInputPlayerConnection remoteConnection)
        {
            s_Manager = manager;
            s_Remote = remote;
            s_RemoteConnection = remoteConnection;
        }

        internal static void TestHook_SwitchToDifferentInputManager(InputManager otherManager)
        {
            s_Manager = otherManager;
            InputStateBuffers.SwitchTo(otherManager.m_StateBuffers, otherManager.defaultUpdateType);
        }
    }
}

#endif // UNITY_EDITOR || UNITY_INCLUDE_TESTS
