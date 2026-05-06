using System;
using UnityEngine.InputSystem.LowLevel;

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Test-only helpers that access InputSystem internals.
    /// Editor-specific operations are registered as delegates by the Editor assembly.
    /// </summary>
    internal static class InputSystemTestHooks
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
            if (InputSystem.remoteConnection != null)
                Object.DestroyImmediate(InputSystem.remoteConnection);

            s_TestHookEditorCleanup?.Invoke();

            InputSystem.s_Manager = null;
            InputSystem.remoteConnection = null;
            InputSystem.s_Remote = null;
        }

        internal static void TestHook_RestoreFromSavedState(InputManager manager, InputRemoting remote, RemoteInputPlayerConnection remoteConnection)
        {
            InputSystem.s_Manager = manager;
            InputSystem.s_Remote = remote;
            InputSystem.remoteConnection = remoteConnection;
        }

        internal static void TestHook_SwitchToDifferentInputManager(InputManager otherManager)
        {
            InputSystem.s_Manager = otherManager;
            InputStateBuffers.SwitchTo(otherManager.m_StateBuffers, otherManager.defaultUpdateType);
        }
    }
}

#endif // UNITY_EDITOR || UNITY_INCLUDE_TESTS
