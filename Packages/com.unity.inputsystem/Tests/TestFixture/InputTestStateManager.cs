using System;
using System.Collections.Generic;
using Unity.Collections;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.Profiling;
using UnityEngine.InputSystem.EnhancedTouch;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Provides functions for saving and restore the InputSystem state across tests and domain reloads.
    /// </summary>
    internal class InputTestStateManager
    {
        public InputSystemState GetSavedState()
        {
            return m_SavedStateStack.Peek();
        }

        /// <summary>
        /// Push the current state of the input system onto a stack and
        /// reset the system to its default state.
        /// </summary>
        /// <remarks>
        /// The save stack is not able to survive domain reloads. It is intended solely
        /// for use in tests.
        /// </remarks>
        public void SaveAndReset(bool enableRemoting, IInputRuntime runtime)
        {
            ////FIXME: does not preserve global state in InputActionState
            ////TODO: preserve InputUser state
            ////TODO: preserve EnhancedTouchSupport state

            m_SavedStateStack.Push(new InputSystemState
            {
                manager = InputSystem.manager,
                remote = InputSystem.remoting,
                remoteConnection = InputSystem.remoteConnection,
                managerState = InputSystem.manager.SaveState(),
                remotingState = InputSystem.remoting?.SaveState() ?? new InputRemoting.SerializedState(),
#if UNITY_EDITOR
                userSettings = InputEditorUserSettings.s_Settings,
                systemObject = JsonUtility.ToJson(InputSystem.domainStateManager),
#endif
                inputActionState = InputActionState.SaveAndResetState(),
                touchState = EnhancedTouch.Touch.SaveAndResetState(),
                inputUserState = InputUser.SaveAndResetState()
            });

            Reset(enableRemoting, runtime ?? InputRuntime.s_Instance); // Keep current runtime.
        }

        /// <summary>
        /// Return the input system to its default state.
        /// </summary>
        public void Reset(bool enableRemoting, IInputRuntime runtime)
        {
            Profiler.BeginSample("InputSystem.Reset");

            UnityEngine.InputSystem.Editor.ProjectWideActionsAsset.TestHook_Disable();

            // Some devices keep globals. Get rid of them by pretending the devices
            // are removed.
            if (InputSystem.manager != null)
            {
                foreach (var device in InputSystem.manager.devices)
                    device.NotifyRemoved();

                InputSystem.manager.UninstallGlobals();
            }

#if UNITY_EDITOR
            // Perform special initialization for running Editor tests
            InputSystem.TestHook_InitializeForPlayModeTests(enableRemoting, runtime);
#else
            // For Player tests we can use the normal initialization
            InputSystem.InitializeInPlayer(runtime, false);
#endif // UNITY_EDITOR

            Mouse.s_PlatformMouseDevice = null;

            InputEventListener.s_ObserverState = default;
            InputUser.ResetGlobals();
            EnhancedTouchSupport.Reset();

            UnityEngine.InputSystem.Editor.ProjectWideActionsAsset.TestHook_Enable();

            Profiler.EndSample();
        }

        ////FIXME: this method doesn't restore things like InputDeviceDebuggerWindow.onToolbarGUI
        /// <summary>
        /// Restore the state of the system from the last state pushed with <see cref="SaveAndReset"/>.
        /// </summary>
        public void Restore()
        {
            Debug.Assert(m_SavedStateStack.Count > 0);

            // Load back previous state.
            var state = m_SavedStateStack.Pop();

            state.inputUserState.StaticDisposeCurrentState();
            state.touchState.StaticDisposeCurrentState();
            state.inputActionState.StaticDisposeCurrentState();

            InputSystem.TestHook_DestroyAndReset();

            state.inputUserState.RestoreSavedState();
            state.touchState.RestoreSavedState();
            state.inputActionState.RestoreSavedState();

            InputSystem.TestHook_RestoreFromSavedState(state);
            InputUpdate.Restore(state.managerState.updateState);

            InputSystem.manager.InstallRuntime(InputSystem.manager.runtime);
            InputSystem.manager.InstallGlobals();

            // IMPORTANT
            // If InputManager was using the "temporary" settings object, then it'll have been deleted during Reset()
            // and the saved Manager settings state will also be null, since it's a ScriptableObject.
            // In this case we manually create and set new temp settings object.
            if (InputSystem.manager.settings == null)
            {
                var tmpSettings = ScriptableObject.CreateInstance<InputSettings>();
                tmpSettings.hideFlags = HideFlags.HideAndDontSave;
                InputSystem.manager.settings = tmpSettings;
            }
            else InputSystem.manager.ApplySettings();

#if UNITY_EDITOR
            InputEditorUserSettings.s_Settings = state.userSettings;
            JsonUtility.FromJsonOverwrite(state.systemObject, InputSystem.domainStateManager);
#endif

            // Get devices that keep global lists (like Gamepad) to re-initialize them
            // by pretending the devices have been added.
            foreach (var device in InputSystem.devices)
            {
                device.NotifyAdded();
                device.MakeCurrent();
            }
        }

        private Stack<InputSystemState> m_SavedStateStack = new Stack<InputSystemState>();
    }
}
