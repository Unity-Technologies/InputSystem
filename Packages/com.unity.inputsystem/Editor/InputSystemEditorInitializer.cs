using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEditorInternal;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Handles Editor-specific initialization and lifecycle management for the Input System.
    /// </summary>
    [InitializeOnLoad]
    internal static class InputSystemEditorInitializer
    {
        internal static InputSystemObject s_SystemObject;
        private static HashSet<string> s_TrackedDirtyAssets;

        static InputSystemEditorInitializer()
        {
            InitializeInEditor();

            // Hook into Input System property setters to add Editor-specific behavior
            InputSystem.onSettingsChange += OnSettingsChanged;
            InputSystem.s_OnActionsChanging = ValidateAndTrackActions;
            InputSystem.s_ShouldEnableActions = ShouldEnableActions;

            // Register analytics callbacks for InputActionSetupExtensions
            InputActionSetupExtensions.s_ApiUsageCallback = RegisterSetupApiUsage;
            InputActionSetupExtensions.s_SuppressAnalytics = SuppressSetupAnalytics;

            // Register callback for InputSystemUIInputModule Reset()
            #if UNITY_INPUT_SYSTEM_ENABLE_UI || PACKAGE_DOCS_GENERATION
            UnityEngine.InputSystem.UI.InputSystemUIInputModule.s_OnReset = OnUIInputModuleReset;
            #endif

            // Update Editor state in runtime
            UpdateEditorState();
            EditorApplication.update += UpdateEditorState;

            // Register callbacks for Runtime to access Editor functionality
            InputActionAsset.s_OnMarkAsDirty = TrackDirtyInputActionAsset;
            InputManager.s_GetProjectWideActions = () => ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;
            InputSystem.s_Manager.m_AddDevicesNotSupportedByProject = InputEditorUserSettings.addDevicesNotSupportedByProject;

            // Register callback for tests to call InputSystem.OnPlayModeChange (takes int)
            InputSystem.s_OnPlayModeChangeCallback = change => OnPlayModeChange((PlayModeStateChange)change);

            // Register Editor-specific callbacks on NativeInputRuntime
            if (InputRuntime.s_Instance is NativeInputRuntime nativeRuntime)
            {
                // Shutdown callback
                nativeRuntime.m_RegisterWantsToQuit = RegisterWantsToQuit;
                nativeRuntime.m_UnregisterWantsToQuit = UnregisterWantsToQuit;

                // Unity Remote callbacks
                nativeRuntime.m_SetUnityRemoteMessageHandler = SetUnityRemoteMessageHandler;
                nativeRuntime.m_SetUnityRemoteGyroEnabledCallback = SetUnityRemoteGyroEnabled;
                nativeRuntime.m_SetUnityRemoteGyroUpdateIntervalCallback = SetUnityRemoteGyroUpdateInterval;

                // Editor Analytics callback
                nativeRuntime.m_SendEditorAnalytic = SendEditorAnalytic;
            }

            // Register RemoteInputPlayerConnection instance getter
            RemoteInputPlayerConnection.s_GetInstance = RemoteInputPlayerConnectionEditor.GetInstance;

            // Register EnhancedTouchSupport assembly reload callbacks
            EnhancedTouch.EnhancedTouchSupport.s_BeforeAssemblyReloadCallback = RegisterBeforeAssemblyReload;
            EnhancedTouch.EnhancedTouchSupport.s_UnregisterBeforeAssemblyReloadCallback = UnregisterBeforeAssemblyReload;

            // Register InputActionReference asset database callbacks
            InputActionReference.s_IsSubAsset = AssetDatabase.IsSubAsset;
            InputActionReference.s_GetAssetPath = AssetDatabase.GetAssetPath;
            InputActionReference.s_LoadMainAssetAtPath = AssetDatabase.LoadMainAssetAtPath;
        }

        #region Editor Callbacks for NativeInputRuntime

        // Track AssemblyReloadEvents callbacks so we can unregister them
        private static readonly Dictionary<Action, AssemblyReloadEvents.AssemblyReloadCallback> s_AssemblyReloadCallbacks =
            new Dictionary<Action, AssemblyReloadEvents.AssemblyReloadCallback>();

        private static void RegisterBeforeAssemblyReload(Action action)
        {
            if (s_AssemblyReloadCallbacks.ContainsKey(action))
                return;

            AssemblyReloadEvents.AssemblyReloadCallback callback = () => action();
            s_AssemblyReloadCallbacks[action] = callback;
            AssemblyReloadEvents.beforeAssemblyReload += callback;
        }

        private static void UnregisterBeforeAssemblyReload(Action action)
        {
            if (s_AssemblyReloadCallbacks.TryGetValue(action, out var callback))
            {
                AssemblyReloadEvents.beforeAssemblyReload -= callback;
                s_AssemblyReloadCallbacks.Remove(action);
            }
        }

        private static void RegisterWantsToQuit(Func<bool> handler)
        {
            EditorApplication.wantsToQuit += handler;
        }

        private static void UnregisterWantsToQuit(Func<bool> handler)
        {
            EditorApplication.wantsToQuit -= handler;
        }

        // Unity Remote support
        private static Func<IntPtr, bool> s_CurrentUnityRemoteMessageHandler;

        private static void SetUnityRemoteMessageHandler(Func<IntPtr, bool> handler)
        {
            if (s_CurrentUnityRemoteMessageHandler != null)
            {
                var removeMethod = GetUnityRemoteAPIMethod("RemoveMessageHandler");
                removeMethod?.Invoke(null, new object[] { s_CurrentUnityRemoteMessageHandler });
            }

            s_CurrentUnityRemoteMessageHandler = handler;

            if (handler != null)
            {
                var addMethod = GetUnityRemoteAPIMethod("AddMessageHandler");
                addMethod?.Invoke(null, new object[] { handler });
            }
        }

        private static void SetUnityRemoteGyroEnabled(bool value)
        {
            var setMethod = GetUnityRemoteAPIMethod("SetGyroEnabled");
            setMethod?.Invoke(null, new object[] { value });
        }

        private static void SetUnityRemoteGyroUpdateInterval(float interval)
        {
            var setMethod = GetUnityRemoteAPIMethod("SetGyroUpdateInterval");
            setMethod?.Invoke(null, new object[] { interval });
        }

        private static System.Reflection.MethodInfo GetUnityRemoteAPIMethod(string methodName)
        {
            var editorAssembly = typeof(EditorApplication).Assembly;
            var genericRemoteClass = editorAssembly.GetType("UnityEditor.Remote.GenericRemote");
            if (genericRemoteClass == null)
                return null;

            return genericRemoteClass.GetMethod(methodName);
        }

        // Editor Analytics
        private static void SendEditorAnalytic(InputAnalytics.IInputAnalytic analytic)
        {
            #if ENABLE_CLOUD_SERVICES_ANALYTICS
            #if UNITY_2023_2_OR_NEWER
            EditorAnalytics.SendAnalytic(analytic);
            #elif UNITY_INPUT_SYSTEM_ENABLE_ANALYTICS || UNITY_2023_1_OR_NEWER
            var info = analytic.info;
            EditorAnalytics.RegisterEventWithLimit(info.Name, info.MaxEventsPerHour, info.MaxNumberOfElements, InputAnalytics.kVendorKey);
            EditorAnalytics.SendEventWithLimit(info.Name, analytic);
            #endif
            #endif
        }

        #endregion

        private static void UpdateEditorState()
        {
            if (InputRuntime.s_Instance is NativeInputRuntime nativeRuntime)
            {
                nativeRuntime.m_IsInPlayMode = EditorApplication.isPlaying;
                nativeRuntime.m_IsEditorPaused = EditorApplication.isPaused;
                nativeRuntime.m_IsEditorActive = InternalEditorUtility.isApplicationActive;
            }

            // Update Editor settings
            InputSystem.s_Manager.m_AddDevicesNotSupportedByProject = InputEditorUserSettings.addDevicesNotSupportedByProject;
        }

        private static void RegisterSetupApiUsage(int api)
        {
            InputExitPlayModeAnalytic.Register((InputExitPlayModeAnalytic.Api)api);
        }

        private static void SuppressSetupAnalytics(bool suppress)
        {
            InputExitPlayModeAnalytic.suppress = suppress;
        }

        #if UNITY_INPUT_SYSTEM_ENABLE_UI || PACKAGE_DOCS_GENERATION
        private static void OnUIInputModuleReset(UnityEngine.InputSystem.UI.InputSystemUIInputModule module)
        {
            var asset = (InputActionAsset)AssetDatabase.LoadAssetAtPath(
                PlayerInputEditor.kDefaultInputActionsAssetPath,
                typeof(InputActionAsset));

            if (asset != null)
            {
                UnityEngine.InputSystem.UI.Editor.InputSystemUIInputModuleEditor.ReassignActions(module, asset);
            }
        }

        #endif

        internal static void InitializeInEditor()
        {
            // Call Runtime reset
            InputSystem.Reset();

            var existingSystemObjects = Resources.FindObjectsOfTypeAll<InputSystemObject>();
            if (existingSystemObjects != null && existingSystemObjects.Length > 0)
            {
                // We're coming back out of a domain reload
                s_SystemObject = existingSystemObjects[0];
                InputSystem.s_Manager.RestoreStateWithoutDevices(s_SystemObject.systemState.managerState);
                InputDebuggerWindow.ReviveAfterDomainReload();

                // Restore remoting state
                InputSystem.s_RemoteConnection = s_SystemObject.systemState.remoteConnection;
                InputSystem.SetUpRemoting();
                InputSystem.s_Remote.RestoreState(s_SystemObject.systemState.remotingState, InputSystem.s_Manager);

                // Get manager to restore devices on first input update
                InputSystem.s_Manager.m_SavedDeviceStates = s_SystemObject.systemState.managerState.devices;
                InputSystem.s_Manager.m_SavedAvailableDevices = s_SystemObject.systemState.managerState.availableDevices;

                // Get rid of saved state
                s_SystemObject.systemState = new InputSystem.State();
            }
            else
            {
                s_SystemObject = ScriptableObject.CreateInstance<InputSystemObject>();
                s_SystemObject.hideFlags = HideFlags.HideAndDontSave;

                // Load settings
                if (EditorBuildSettings.TryGetConfigObject(InputSettingsProvider.kEditorBuildSettingsConfigKey,
                    out InputSettings settingsAsset))
                {
                    if (InputSystem.s_Manager.m_Settings.hideFlags == HideFlags.HideAndDontSave)
                        ScriptableObject.DestroyImmediate(InputSystem.s_Manager.m_Settings);
                    InputSystem.s_Manager.m_Settings = settingsAsset;
                    InputSystem.s_Manager.ApplySettings();
                }

                // Load project-wide actions
                var savedActions = ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;
                if (savedActions != null)
                    InputSystem.s_Manager.actions = savedActions;

                InputEditorUserSettings.Load();
                SetUpEditorRemoting();
            }

            // Subscribe to Editor events directly (cleaner separation from Runtime)
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            EditorApplication.projectChanged += OnEditorProjectChanged;

            // Initialize Unity Remote support (Editor-only)
            UnityRemoteSupport.Initialize();

            // Check for backend settings
            EditorApplication.delayCall += ShowRestartWarning;

            // Run initial update
            InputSystem.RunInitialUpdate();
        }

        /// <summary>
        /// Editor-specific remoting setup that uses EditorApplication.delayCall
        /// </summary>
        private static void SetUpEditorRemoting()
        {
            InputSystem.s_Remote = new InputRemoting(InputSystem.s_Manager);
            // NOTE: We use delayCall as our initial startup will run in editor initialization before
            //       PlayerConnection is itself ready. If we call Bind() directly here, we won't
            //       see any errors but the callbacks we register for will not trigger.
            EditorApplication.delayCall += SetUpRemotingInternal;
        }

        private static void SetUpRemotingInternal()
        {
            if (InputSystem.s_RemoteConnection == null)
            {
                InputSystem.s_RemoteConnection = RemoteInputPlayerConnection.instance;
                InputSystem.s_RemoteConnection.Bind(EditorConnection.instance, false);
            }

            InputSystem.s_Remote.Subscribe(InputSystem.s_RemoteConnection); // Feed messages from players into editor.
            InputSystem.s_RemoteConnection.Subscribe(InputSystem.s_Remote); // Feed messages from editor into players.
        }

        /// <summary>
        /// Called when InputSystem.settings changes to track it in EditorBuildSettings
        /// </summary>
        private static void OnSettingsChanged()
        {
            var settings = InputSystem.settings;
            if (settings != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(settings)))
            {
                EditorBuildSettings.AddConfigObject(InputSettingsProvider.kEditorBuildSettingsConfigKey,
                    settings, true);
            }
        }

        /// <summary>
        /// Validates and tracks InputActionAsset assignments in the Editor
        /// </summary>
        internal static void ValidateAndTrackActions(InputActionAsset value)
        {
            if (value != null)
            {
                // Do not allow assigning non-persistent assets (pure in-memory objects)
                if (!EditorUtility.IsPersistent(value))
                    throw new ArgumentException($"Assigning a non-persistent {nameof(InputActionAsset)} to this property is not allowed. The assigned asset needs to be persisted on disk inside the /Assets folder.");

                // Track reference to enable including it in built Players
                ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild = value;
            }
        }

        /// <summary>
        /// Checks if actions should be enabled (Editor-specific check for play mode)
        /// </summary>
        internal static bool ShouldEnableActions()
        {
            // Abort if not in play-mode in editor
            return EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void ShowRestartWarning()
        {
            if (!s_SystemObject.newInputBackendsCheckedAsEnabled &&
                !EditorPlayerSettingHelpers.newSystemBackendsEnabled &&
                !Application.isBatchMode)
            {
                const string dialogText = "This project is using the new input system package but the native platform backends for the new input system are not enabled in the player settings. " +
                    "This means that no input from native devices will come through." +
                    "\n\nDo you want to enable the backends? Doing so will *RESTART* the editor.";

                if (EditorUtility.DisplayDialog("Warning", dialogText, "Yes", "No"))
                {
                    EditorPlayerSettingHelpers.newSystemBackendsEnabled = true;
                    EditorHelpers.RestartEditorAndRecompileScripts();
                }
            }
            s_SystemObject.newInputBackendsCheckedAsEnabled = true;
            EditorApplication.delayCall -= ShowRestartWarning;
        }

        /// <summary>
        /// Called by EditorApplication.playModeStateChanged
        /// </summary>
        private static void OnEditorPlayModeStateChanged(PlayModeStateChange change)
        {
            // Dispatch to runtime callback if registered
            if (InputRuntime.s_Instance is NativeInputRuntime nativeRuntime)
            {
                nativeRuntime.DispatchPlayModeChange((int)change);
            }

            // Handle the change
            OnPlayModeChange(change);
        }

        /// <summary>
        /// Called by EditorApplication.projectChanged
        /// </summary>
        private static void OnEditorProjectChanged()
        {
            // Dispatch to runtime callback if registered
            if (InputRuntime.s_Instance is NativeInputRuntime nativeRuntime)
            {
                nativeRuntime.DispatchProjectChange();
            }

            // Handle the change
            OnProjectChange();
        }

        /// <summary>
        /// Public method for tests and direct calls (with PlayModeStateChange enum)
        /// </summary>
        internal static void OnPlayModeChange(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    s_SystemObject.settings = JsonUtility.ToJson(InputSystem.settings);
                    s_SystemObject.exitEditModeTime = InputRuntime.s_Instance.currentTime;
                    s_SystemObject.enterPlayModeTime = 0;

                    // Set times in InputManager for event filtering
                    InputSystem.s_Manager.m_ExitEditModeTime = s_SystemObject.exitEditModeTime;
                    InputSystem.s_Manager.m_EnterPlayModeTime = 0;
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    s_SystemObject.enterPlayModeTime = InputRuntime.s_Instance.currentTime;

                    // Update time in InputManager
                    InputSystem.s_Manager.m_EnterPlayModeTime = s_SystemObject.enterPlayModeTime;
                    InputSystem.s_Manager.SyncAllDevicesAfterEnteringPlayMode();
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    InputSystem.s_Manager.LeavePlayMode();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    InputSystem.DisableActions(false);

                    // Nuke all InputUsers
                    InputUser.ResetGlobals();

                    // Nuke all InputActionMapStates
                    InputActionState.DestroyAllActionMapStates();

                    // Clear the Action reference from all InputActionReference objects
                    InputActionReference.InvalidateAll();

                    // Restore settings
                    if (!string.IsNullOrEmpty(s_SystemObject.settings))
                    {
                        JsonUtility.FromJsonOverwrite(s_SystemObject.settings, InputSystem.settings);
                        s_SystemObject.settings = null;
                        InputSystem.settings.OnChange();
                    }

                    // Reload input action assets marked as dirty from disk
                    if (s_TrackedDirtyAssets != null)
                    {
                        foreach (var assetGuid in s_TrackedDirtyAssets)
                        {
                            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                            if (!string.IsNullOrEmpty(assetPath))
                                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                        }
                        s_TrackedDirtyAssets.Clear();
                    }
                    break;
            }
        }

        internal static void OnProjectChange()
        {
            InputSettingsProvider.ForceReload();

            // If the asset holding our current settings got deleted, switch back to a temporary settings object
            if (EditorUtility.InstanceIDToObject(InputSystem.s_Manager.m_Settings.GetInstanceID()) == null)
            {
                var newSettings = ScriptableObject.CreateInstance<InputSettings>();
                newSettings.hideFlags = HideFlags.HideAndDontSave;
                InputSystem.settings = newSettings;
            }
        }

        internal static void TrackDirtyInputActionAsset(InputActionAsset asset)
        {
            if (s_TrackedDirtyAssets == null)
                s_TrackedDirtyAssets = new HashSet<string>();

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string assetGuid, out long _))
                s_TrackedDirtyAssets.Add(assetGuid);
        }
    }
}
