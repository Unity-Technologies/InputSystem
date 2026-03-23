using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEditorInternal;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Handles Editor-specific initialization and lifecycle management for the Input System.
    /// </summary>
    [InitializeOnLoad]
    internal static class InputSystemEditorInitializer
    {
        private static InputSystemStateManager s_StateManager;
        internal static InputSystemStateManager stateManager => s_StateManager;

        static InputSystemEditorInitializer()
        {
            OnGlobalInitialize(calledFromCtor: true);
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

        internal static bool IsDomainReloadDisabledForPlayMode()
        {
            #if !ENABLE_CORECLR
            if (!EditorSettings.enterPlayModeOptionsEnabled || (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0)
                return false;
            return true;
            #else
            return false;
            #endif
        }

        /// <summary>
        /// Called from InputSystem.GlobalInitialize via the s_EditorGlobalInitializeCallback.
        /// Handles the dual-call initialization pattern for Domain Reload disabled mode.
        /// </summary>
        private static void OnGlobalInitialize(bool calledFromCtor)
        {
            // When Domain Reloads are enabled, initialization is handled by [InitializeOnLoad]
            // via the static constructor. When DRs are disabled, the static ctor doesn't re-fire
            // on play mode entry, so we need RuntimeInitialize (calledFromCtor=false) to handle it.
            // The static ctor always fires on actual domain reload regardless.
            if (calledFromCtor || IsDomainReloadDisabledForPlayMode())
            {
                InitializeInEditor(calledFromCtor);
            }
        }

        internal static void InitializeInEditor(bool calledFromCtor, IInputRuntime runtime = null)
        {
            bool globalReset = calledFromCtor || !IsDomainReloadDisabledForPlayMode();

            // We must initialize a new InputManager object first thing since other parts
            // of the init flow depend on it.
            if (globalReset)
            {
                if (InputSystem.s_Manager != null)
                    InputSystem.s_Manager.Dispose();

                InputSystem.s_Manager = InputManager.CreateAndInitialize(runtime ?? NativeInputRuntime.instance, null);

                EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
                EditorApplication.projectChanged += OnEditorProjectChanged;

                InputSystem.s_Manager.runtime.onPlayModeChanged = InputSystem.OnPlayModeChange;
                InputSystem.s_Manager.runtime.onProjectChange = InputSystem.OnProjectChange;

                InputEditorUserSettings.s_Settings = new InputEditorUserSettings.SerializedState();

                #if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
                InputSystem.PerformDefaultPluginInitialization();
                #endif
            }

            var existingStateManagers = Resources.FindObjectsOfTypeAll<InputSystemStateManager>();
            if (existingStateManagers != null && existingStateManagers.Length > 0)
            {
                if (globalReset)
                {
                    s_StateManager = existingStateManagers[0];
                    InputSystem.s_Manager.RestoreStateWithoutDevices(s_StateManager.systemState.managerState);
                    InputDebuggerWindow.ReviveAfterDomainReload();

                    InputSystem.remoteConnection = s_StateManager.systemState.remoteConnection;
                    InputSystem.SetUpRemoting();
                    InputSystem.s_Remote.RestoreState(s_StateManager.systemState.remotingState, InputSystem.s_Manager);

                    InputSystem.s_Manager.m_SavedDeviceStates = s_StateManager.systemState.managerState.devices;
                    InputSystem.s_Manager.m_SavedAvailableDevices = s_StateManager.systemState.managerState.availableDevices;

                    // InputEditorUserSettings.s_Settings = s_StateManager.systemState.userSettings;

                    s_StateManager.systemState = new InputSystemState();
                }
            }
            else
            {
                s_StateManager = ScriptableObject.CreateInstance<InputSystemStateManager>();
                s_StateManager.hideFlags = HideFlags.HideAndDontSave;

                if (EditorBuildSettings.TryGetConfigObject(InputSettingsProvider.kEditorBuildSettingsConfigKey,
                    out InputSettings settingsAsset))
                {
                    InputSystem.s_Manager.settings = settingsAsset;
                }

                var savedActions = ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;
                if (savedActions != null)
                    InputSystem.s_Manager.actions = savedActions;

                InputEditorUserSettings.Load();
                SetUpEditorRemoting();
            }

            //TODO EDITOR CODE SPLIT: fix this
            // Debug.Assert(settings != null);
            // Debug.Assert(HasNativeObject(settings), "InputSettings has lost its native object");


            UnityRemoteSupport.Initialize();

            EditorApplication.delayCall += ShowRestartWarning;

            InputSystem.RunInitialUpdate();

            InputSystem.EnableActions();

            InitializeEditorHooks();
        }

        private static void InitializeEditorHooks()
        {
            InputSystem.s_OnActionsChanging = ValidateAndTrackActions;
            InputSystem.s_ShouldEnableActions = ShouldEnableActions;
            InputAnalytics.s_IsNewSystemBackendsEnabled = ShouldEnableActionsNewBackend;
            InputAnalytics.s_IsOldSystemBackendsEnabled = ShouldEnableActionsOldBackend;

            InputActionSetupExtensions.s_ApiUsageCallback = RegisterSetupApiUsage;
            InputActionSetupExtensions.s_SuppressAnalytics = SuppressSetupAnalytics;

#if UNITY_INPUT_SYSTEM_ENABLE_UI || PACKAGE_DOCS_GENERATION
            UnityEngine.InputSystem.UI.InputSystemUIInputModule.s_OnReset = OnUIInputModuleReset;
#endif

            InputActionAsset.s_OnMarkAsDirty = DirtyAssetTracker.TrackDirtyInputActionAsset;
            InputManager.s_GetProjectWideActions = () => ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;
            InputSystem.s_Manager.m_AddDevicesNotSupportedByProject = () => InputEditorUserSettings.addDevicesNotSupportedByProject;

            InputSystem.s_OnPlayModeChangeCallback = change => OnPlayModeChange((PlayModeStateChange)change);
            InputSystem.s_OnProjectChangeCallback = OnProjectChange;

            InputSystem.s_IsDomainReloadDisabled = IsDomainReloadDisabledForPlayMode;
            InputSystem.s_EditorGlobalInitializeCallback = OnGlobalInitialize;

            // Register test hook callbacks
            InputSystem.s_TestHookInitializeForPlayModeTests = TestHook_InitializeForPlayModeTests;
#if !ENABLE_CORECLR
            InputSystem.s_TestHookSimulateDomainReload = TestHook_SimulateDomainReload;
#endif
            InputSystem.s_TestHookEditorCleanup = TestHook_EditorCleanup;

            if (InputRuntime.s_Instance is NativeInputRuntime nativeRuntime)
            {
                nativeRuntime.m_IsInPlayMode = () => EditorApplication.isPlaying;
                nativeRuntime.m_IsEditorPaused = () => EditorApplication.isPaused;
                nativeRuntime.m_IsEditorActive = () => InternalEditorUtility.isApplicationActive;

                nativeRuntime.m_RegisterWantsToQuit = RegisterWantsToQuit;
                nativeRuntime.m_UnregisterWantsToQuit = UnregisterWantsToQuit;

                nativeRuntime.m_SetUnityRemoteMessageHandler = SetUnityRemoteMessageHandler;
                nativeRuntime.m_SetUnityRemoteGyroEnabledCallback = SetUnityRemoteGyroEnabled;
                nativeRuntime.m_SetUnityRemoteGyroUpdateIntervalCallback = SetUnityRemoteGyroUpdateInterval;

                nativeRuntime.m_SendEditorAnalytic = SendEditorAnalytic;
            }

            RemoteInputPlayerConnection.s_GetInstance = RemoteInputPlayerConnectionEditor.GetInstance;

            EnhancedTouch.EnhancedTouchSupport.s_BeforeAssemblyReloadCallback = RegisterBeforeAssemblyReload;
            EnhancedTouch.EnhancedTouchSupport.s_UnregisterBeforeAssemblyReloadCallback = UnregisterBeforeAssemblyReload;

            InputActionReference.s_IsSubAsset = AssetDatabase.IsSubAsset;
            InputActionReference.s_GetAssetPath = AssetDatabase.GetAssetPath;
            InputActionReference.s_LoadMainAssetAtPath = AssetDatabase.LoadMainAssetAtPath;
        }

        private static void SetUpEditorRemoting()
        {
            InputSystem.s_Remote = new InputRemoting(InputSystem.s_Manager);
            EditorApplication.delayCall += SetUpRemotingInternal;
        }

        private static void SetUpRemotingInternal()
        {
            if (InputSystem.remoteConnection == null)
            {
                InputSystem.remoteConnection = RemoteInputPlayerConnection.instance;
                InputSystem.remoteConnection.Bind(EditorConnection.instance, false);
            }

            InputSystem.s_Remote.Subscribe(InputSystem.remoteConnection);
            InputSystem.remoteConnection.Subscribe(InputSystem.s_Remote);
        }

        private static void OnSettingsChanged()
        {
            var settings = InputSystem.settings;
            if (settings != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(settings)))
            {
                EditorBuildSettings.AddConfigObject(InputSettingsProvider.kEditorBuildSettingsConfigKey,
                    settings, true);
            }
        }

        internal static void ValidateAndTrackActions(InputActionAsset value)
        {
            if (value != null)
            {
                if (!EditorUtility.IsPersistent(value))
                    throw new ArgumentException($"Assigning a non-persistent {nameof(InputActionAsset)} to this property is not allowed. The assigned asset needs to be persisted on disk inside the /Assets folder.");

                ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild = value;
            }
            else
            {
                ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild = null;
            }
        }

        internal static bool ShouldEnableActions()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode;
        }

        internal static bool ShouldEnableActionsNewBackend()
        {
            return EditorPlayerSettingHelpers.newSystemBackendsEnabled;
        }

        internal static bool ShouldEnableActionsOldBackend()
        {
            return EditorPlayerSettingHelpers.oldSystemBackendsEnabled;
        }

        private static void ShowRestartWarning()
        {
            if (!s_StateManager.newInputBackendsCheckedAsEnabled &&
                !EditorPlayerSettingHelpers.newSystemBackendsEnabled &&
                !Application.isBatchMode)
            {
                const string dialogText = "The new Input System Package is installed, but not configured to enable native device input, such as keyboard, mouse, or gamepad actions. " +
                    "\n\nThe Active Input Handling parameter must be set to \"Input System Package (New)\", under Project Settings > Player." +
                    "\n\nNote: Changing the active input handling requires to restart the Editor.";

                bool userChoseEnableAndRestart;
#if UNITY_6000_3_OR_NEWER
                userChoseEnableAndRestart = EditorUtility.DisplayDialog(
                    "Input System native platform backend not enabled",
                    dialogText,
                    "Enable & Restart",
                    "Don't Enable",
                    DialogOptOutDecisionType.ForThisSession,
                    "RestartInstalledInputHandlingWarning");
#else
                userChoseEnableAndRestart = EditorUtility.DisplayDialog(
                    "Input System native platform backend not enabled",
                    dialogText,
                    "Enable & Restart",
                    "Don't Enable");
#endif
                if (userChoseEnableAndRestart)
                {
                    EditorPlayerSettingHelpers.newSystemBackendsEnabled = true;
                    EditorHelpers.RestartEditorAndRecompileScripts();
                }
            }
            s_StateManager.newInputBackendsCheckedAsEnabled = true;
            EditorApplication.delayCall -= ShowRestartWarning;
        }

        private static void OnEditorPlayModeStateChanged(PlayModeStateChange change)
        {
            if (InputRuntime.s_Instance is NativeInputRuntime nativeRuntime)
            {
                nativeRuntime.DispatchPlayModeChange((int)change);
            }
        }

        private static void OnEditorProjectChanged()
        {
            if (InputRuntime.s_Instance is NativeInputRuntime nativeRuntime)
            {
                nativeRuntime.DispatchProjectChange();
            }
        }

        internal static void OnPlayModeChange(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    s_StateManager.settings = JsonUtility.ToJson(InputSystem.settings);
                    s_StateManager.exitEditModeTime = InputRuntime.s_Instance.currentTime;
                    s_StateManager.enterPlayModeTime = 0;

                    InputSystem.s_Manager.m_ExitEditModeTime = s_StateManager.exitEditModeTime;
                    InputSystem.s_Manager.m_EnterPlayModeTime = 0;
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    s_StateManager.enterPlayModeTime = InputRuntime.s_Instance.currentTime;
                    InputSystem.s_Manager.m_EnterPlayModeTime = s_StateManager.enterPlayModeTime;
                    InputSystem.s_Manager.SyncAllDevicesAfterEnteringPlayMode();
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    InputSystem.s_Manager.LeavePlayMode();
                    break;

                ////TODO: also nuke all callbacks installed on InputActions and InputActionMaps
                ////REVIEW: is there any other cleanup work we want to before? should we automatically nuke
                ////        InputDevices that have been created with AddDevice<> during play mode?
                case PlayModeStateChange.EnteredEditMode:
                    InputSystem.DisableActions(false);

                    // Nuke all InputUsers.
                    InputUser.ResetGlobals();

                    // Nuke all InputActionMapStates. Releases their unmanaged memory.
                    InputActionState.DestroyAllActionMapStates();

                    // Clear the Action reference from all InputActionReference objects
                    InputActionReference.InvalidateAll();

                    // Restore settings.
                    if (!string.IsNullOrEmpty(s_StateManager.settings))
                    {
                        JsonUtility.FromJsonOverwrite(s_StateManager.settings, InputSystem.settings);
                        s_StateManager.settings = null;
                        InputSystem.settings.OnChange();
                    }

                    // Reload input assets marked as dirty from disk
                    DirtyAssetTracker.ReloadDirtyAssets();
                    break;
            }
        }

        internal static void OnProjectChange()
        {
            ////TODO: use dirty count to find whether settings have actually changed
            // May have added, removed, moved, or renamed settings asset. Force a refresh
            // of the UI.
            InputSettingsProvider.ForceReload();

            // Also, if the asset holding our current settings got deleted, switch back to a
            // temporary settings object.
            // NOTE: We access m_Settings directly here to make sure we're not running into asserts
            //       from the settings getter checking it has a valid object.
            if (!HasNativeObject(InputSystem.s_Manager.settings))
            {
                var newSettings = ScriptableObject.CreateInstance<InputSettings>();
                newSettings.hideFlags = HideFlags.HideAndDontSave;
                InputSystem.settings = newSettings;
            }
        }

        private static bool HasNativeObject(Object obj)
        {
#if UNITY_6000_3_OR_NEWER
            return EditorUtility.EntityIdToObject(obj.GetEntityId()) != null;
#else
            return EditorUtility.InstanceIDToObject(obj.GetInstanceID()) != null;
#endif
        }

        #region Test Hooks

        private static void TestHook_InitializeForPlayModeTests(bool enableRemoting, IInputRuntime runtime)
        {
            InputSystem.s_Manager = InputManager.CreateAndInitialize(runtime, null);

            InputSystem.s_Manager.runtime.onPlayModeChanged = InputSystem.OnPlayModeChange;

            InputEditorUserSettings.s_Settings = new InputEditorUserSettings.SerializedState();

            if (enableRemoting)
                InputSystem.SetUpRemoting();

#if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
            InputSystem.s_PluginsInitialized = false;
            InputSystem.PerformDefaultPluginInitialization();
#endif
        }

#if !ENABLE_CORECLR
        private static void TestHook_SimulateDomainReload(IInputRuntime runtime)
        {
            InputSystem.s_Manager.TestHook_RemoveDevicesForSimulatedDomainReload();

            s_StateManager.OnBeforeSerialize();
            s_StateManager = null;
            InputSystem.s_Manager = null;
            InputSystem.s_PluginsInitialized = false;
            InitializeInEditor(calledFromCtor: true, runtime);
        }

#endif

        private static void TestHook_EditorCleanup()
        {
            EditorInputControlLayoutCache.Clear();
            InputDeviceDebuggerWindow.s_OnToolbarGUIActions.Clear();
            InputEditorUserSettings.s_Settings = new InputEditorUserSettings.SerializedState();
        }

        #endregion
    }
}
