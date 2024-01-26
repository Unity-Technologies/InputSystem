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
        public InputSystem.State GetSavedState()
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

            m_SavedStateStack.Push(new InputSystem.State
            {
                manager = InputSystem.s_Manager,
                remote = InputSystem.s_Remote,
                remoteConnection = InputSystem.s_RemoteConnection,
                managerState = InputSystem.s_Manager.SaveState(),
                remotingState = InputSystem.s_Remote?.SaveState() ?? new InputRemoting.SerializedState(),
#if UNITY_EDITOR
                userSettings = InputEditorUserSettings.s_Settings,
                systemObject = JsonUtility.ToJson(InputSystem.s_SystemObject),
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

            InputSystem.DisableActionsForTests();

            // Some devices keep globals. Get rid of them by pretending the devices
            // are removed.
            if (InputSystem.s_Manager != null)
            {
                foreach (var device in InputSystem.s_Manager.devices)
                    device.NotifyRemoved();

                InputSystem.s_Manager.UninstallGlobals();
            }

#if UNITY_EDITOR

            InputSystem.s_Manager = InputManager.CreateAndInitialize(runtime, null);

            InputSystem.s_Manager.runtime.onPlayModeChanged = InputSystem.OnPlayModeChange;
            InputSystem.s_Manager.runtime.onProjectChange = InputSystem.OnProjectChange;

            InputEditorUserSettings.s_Settings = new InputEditorUserSettings.SerializedState();

            if (enableRemoting)
                InputSystem.SetUpRemoting();

#if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
            InputSystem.PerformDefaultPluginInitialization();
#endif

#else
            // For tests need to use default InputSettings
            InputSystem.InitializeInPlayer(runtime, false);
#endif // UNITY_EDITOR

            Mouse.s_PlatformMouseDevice = null;

            InputEventListener.s_ObserverState = default;
            InputUser.ResetGlobals();
            EnhancedTouchSupport.Reset();

            InputSystem.EnableActionsForTests();

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
            InputSystem.s_Manager.Dispose();
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

            // Nuke what we have.
            Destroy();

            state.inputUserState.RestoreSavedState();
            state.touchState.RestoreSavedState();
            state.inputActionState.RestoreSavedState();

            InputSystem.s_Manager = state.manager;
            InputSystem.s_Remote = state.remote;
            InputSystem.s_RemoteConnection = state.remoteConnection;

            InputUpdate.Restore(state.managerState.updateState);

            InputSystem.s_Manager.InstallRuntime(InputSystem.s_Manager.runtime);
            InputSystem.s_Manager.InstallGlobals();

            // IMPORTANT
            // If InputManager was using the "temporary" settings object, then it'll have been deleted during Reset()
            // and the saved Manager settings state will also be null, since it's a ScriptableObject.
            // In this case we manually create and set new temp settings object.
            if (InputSystem.s_Manager.settings == null)
            {
                var tmpSettings = ScriptableObject.CreateInstance<InputSettings>();
                tmpSettings.hideFlags = HideFlags.HideAndDontSave;
                InputSystem.s_Manager.settings = tmpSettings;
            }
            else InputSystem.s_Manager.ApplySettings();

#if UNITY_EDITOR
            InputEditorUserSettings.s_Settings = state.userSettings;
            JsonUtility.FromJsonOverwrite(state.systemObject, InputSystem.s_SystemObject);
#endif

            // Get devices that keep global lists (like Gamepad) to re-initialize them
            // by pretending the devices have been added.
            foreach (var device in InputSystem.devices)
            {
                device.NotifyAdded();
                device.MakeCurrent();
            }
        }

        private Stack<InputSystem.State> m_SavedStateStack = new Stack<InputSystem.State>();
    }
}
