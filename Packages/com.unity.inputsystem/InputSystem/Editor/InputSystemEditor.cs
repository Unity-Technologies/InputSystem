using System.Collections.Generic;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Processors;

namespace UnityEngine.InputSystem.Editor
{
    [InitializeOnLoad]
    public static class InputSystemEditor
    {
        static InputSystemEditor()
        {
            InputSystem.onSettingsChanged += OnSettingsChanged;
            InputSystem.onReset += OnReset;
            InputSystem.onDestroy += OnDestroy;
            InputSystem.onSave += OnSave;
            InputSystem.onRestore += OnRestore;
            InputSystem.gameIsPlayingAndHasFocus = () =>
                InputSystem.s_Manager.m_Runtime.isInPlayMode &&
                !InputSystem.s_Manager.m_Runtime.isPaused &&
                (InputSystem.s_Manager.m_HasFocus || InputEditorUserSettings.lockInputToGameView);
            InputSystem.addDevicesNotSupportedByProject = () => InputEditorUserSettings.addDevicesNotSupportedByProject;
            InitializeInEditor();
        }

#if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
        internal static void PerformEditorDefaultPluginInitialization()
        {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
            HIDSupportEditor.Initialize();
#endif
        }

#endif

        internal static InputSystemObject s_SystemObject;

        internal static void InitializeInEditor(IInputRuntime runtime = null)
        {
            Profiling.Profiler.BeginSample("InputSystem.InitializeInEditor");
            InputSystem.Reset(runtime: runtime);

            var existingSystemObjects = Resources.FindObjectsOfTypeAll<InputSystemObject>();
            if (existingSystemObjects != null && existingSystemObjects.Length > 0)
            {
                ////FIXME: does not preserve action map state

                // We're coming back out of a domain reload. We're restoring part of the
                // InputManager state here but we're still waiting from layout registrations
                // that happen during domain initialization.
                s_SystemObject = existingSystemObjects[0];
                InputSystem.s_Manager.RestoreStateWithoutDevices(s_SystemObject.systemState.managerState);
                //    InputDebuggerWindow.ReviveAfterDomainReload();

                // Restore remoting state.
                InputSystem.s_RemoteConnection = s_SystemObject.systemState.remoteConnection;
                InputSystem.SetUpRemoting();
                InputSystem.s_Remote.RestoreState(s_SystemObject.systemState.remotingState, InputSystem.s_Manager);

                // Get manager to restore devices on first input update. By that time we
                // should have all (possibly updated) layout information in place.
                InputSystem.s_Manager.m_SavedDeviceStates = s_SystemObject.systemState.managerState.devices;
                InputSystem.s_Manager.m_SavedAvailableDevices = s_SystemObject.systemState.managerState.availableDevices;

                Debug.Log("Set Settings 1");
                // Restore editor settings.
                InputEditorUserSettings.s_Settings = s_SystemObject.userSettings;

                // Get rid of saved state.
                s_SystemObject.systemState = new InputSystem.State();
            }
            else
            {
                s_SystemObject = ScriptableObject.CreateInstance<InputSystemObject>();
                s_SystemObject.hideFlags = HideFlags.HideAndDontSave;
                // See if we have a remembered settings object.
                if (EditorBuildSettings.TryGetConfigObject(InputSettingsProvider.kEditorBuildSettingsConfigKey,
                    out InputSettings settingsAsset))
                {
                    if (InputSystem.s_Manager.m_Settings.hideFlags == HideFlags.HideAndDontSave)
                        ScriptableObject.DestroyImmediate(InputSystem.s_Manager.m_Settings);
                    InputSystem.s_Manager.m_Settings = settingsAsset;
                    InputSystem.s_Manager.ApplySettings();
                }

                InputEditorUserSettings.Load();

                InputSystem.SetUpRemoting();
            }

            Debug.Assert(InputSystem.settings != null);
            Debug.Assert(EditorUtility.InstanceIDToObject(InputSystem.settings.GetInstanceID()) != null,
                "InputSettings has lost its native object");

            // If native backends for new input system aren't enabled, ask user whether we should
            // enable them (requires restart). We only ask once per session and don't ask when
            // running in batch mode.
            if (!s_SystemObject.newInputBackendsCheckedAsEnabled &&
                !EditorPlayerSettingHelpers.newSystemBackendsEnabled &&
                !InputSystem.s_Manager.m_Runtime.isInBatchMode)
            {
                const string dialogText = "This project is using the new input system package but the native platform backends for the new input system are not enabled in the player settings. " +
                    "This means that no input from native devices will come through." +
                    "\n\nDo you want to enable the backends. Doing so requires a restart of the editor.";

                if (EditorUtility.DisplayDialog("Warning", dialogText, "Yes", "No"))
                    EditorPlayerSettingHelpers.newSystemBackendsEnabled = true;
            }
            s_SystemObject.newInputBackendsCheckedAsEnabled = true;

            InputSystem.RunInitialUpdate();

            Profiling.Profiler.EndSample();
        }

        private static void OnReset(bool enableRemoting, IInputRuntime runtime, InputSettings settings)
        {
            InputSystem.s_Manager = new InputManager();
            InputSystem.s_Manager.Initialize(runtime ?? NativeInputRuntime.instance, settings);
            InputSystem.s_Manager.processors.AddTypeRegistration("AutoWindowSpace", typeof(EditorWindowSpaceProcessor));

            InputSystem.s_Manager.m_Runtime.onPlayModeChanged = OnPlayModeChange;
            InputSystem.s_Manager.m_Runtime.onProjectChange = OnProjectChange;

            InputEditorUserSettings.s_Settings = new InputEditorUserSettings.SerializedState();

            if (enableRemoting)
                InputSystem.SetUpRemoting();

            #if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
            InputSystem.PerformDefaultPluginInitialization();
            PerformEditorDefaultPluginInitialization();
            #endif
        }

        private static void OnDestroy()
        {
            EditorInputControlLayoutCache.Clear();
            InputDeviceDebuggerWindow.s_OnToolbarGUIActions.Clear();
            InputEditorUserSettings.s_Settings = new InputEditorUserSettings.SerializedState();
        }

        private static Stack<InputEditorUserSettings.SerializedState> s_SavedStateStack;

        private static void OnSave()
        {
            if (s_SavedStateStack == null)
                s_SavedStateStack = new Stack<InputEditorUserSettings.SerializedState>();

            s_SavedStateStack.Push(InputEditorUserSettings.s_Settings);
        }

        private static void OnRestore()
        {
            InputEditorUserSettings.s_Settings = s_SavedStateStack.Pop();
        }

        private static void OnSettingsChanged(InputSettings value)
        {
            // In the editor, we keep track of the settings asset through EditorBuildSettings.
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(value)))
                EditorBuildSettings.AddConfigObject(InputSettingsProvider.kEditorBuildSettingsConfigKey, value, true);
        }

        private static void OnPlayModeChange(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    s_SystemObject.settings = JsonUtility.ToJson(InputSystem.settings);
                    break;

                ////TODO: also nuke all callbacks installed on InputActions and InputActionMaps
                ////REVIEW: is there any other cleanup work we want to before? should we automatically nuke
                ////        InputDevices that have been created with AddDevice<> during play mode?
                case PlayModeStateChange.EnteredEditMode:

                    // Nuke all InputActionMapStates. Releases their unmanaged memory.
                    InputActionState.DestroyAllActionMapStates();

                    // Restore settings.
                    if (!string.IsNullOrEmpty(s_SystemObject.settings))
                    {
                        JsonUtility.FromJsonOverwrite(s_SystemObject.settings, InputSystem.settings);
                        s_SystemObject.settings = null;
                        InputSystem.settings.OnChange();
                    }

                    break;
            }
        }

        private static void OnProjectChange()
        {
            // May have added, removed, moved, or renamed settings asset. Force a refresh
            // of the UI.
            // TODOInputSettingsProvider.ForceReload();

            // Also, if the asset holding our current settings got deleted, switch back to a
            // temporary settings object.
            // NOTE: We access m_Settings directly here to make sure we're not running into asserts
            //       from the settings getter checking it has a valid object.
            if (EditorUtility.InstanceIDToObject(InputSystem.s_Manager.m_Settings.GetInstanceID()) == null)
            {
                var newSettings = ScriptableObject.CreateInstance<InputSettings>();
                newSettings.hideFlags = HideFlags.HideAndDontSave;
                InputSystem.settings = newSettings;
            }
        }
    }
}
