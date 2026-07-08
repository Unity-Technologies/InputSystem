#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Profiling;
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
        private static InputSystemStateManager s_DomainStateManager;
        internal static InputSystemStateManager domainStateManager => s_DomainStateManager;

        static readonly ProfilerMarker k_InputInitializeInEditorMarker = new ProfilerMarker("InputSystem.InitializeInEditor");

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

        private static void RegisterSetupApiUsage(InputAnalytics.AuthoringApi authoringApi)
        {
            InputExitPlayModeAnalytic.Register(authoringApi);
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
                UI.Editor.InputSystemUIInputModuleEditor.ReassignActions(module, asset);
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
            k_InputInitializeInEditorMarker.Begin();

            bool globalReset = calledFromCtor || !IsDomainReloadDisabledForPlayMode();

            // We must initialize a new InputManager object first thing since other parts
            // of the init flow depend on it.
            if (globalReset)
            {
                if (InputSystem.s_Manager != null)
                    InputSystem.s_Manager.Dispose();

                // Settings object should get set by an actual InputSettings asset.
                InputSystem.s_Manager = InputManager.CreateAndInitialize(runtime ?? NativeInputRuntime.instance, null);
                EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
                EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
                EditorApplication.projectChanged -= OnEditorProjectChanged;
                EditorApplication.projectChanged += OnEditorProjectChanged;

                InputSystem.s_Manager.runtime.onPlayModeChanged = InputSystem.OnPlayModeChange;
                InputSystem.s_Manager.runtime.onProjectChange = InputSystem.OnProjectChange;

                InputEditorUserSettings.s_Settings = new InputEditorUserSettings.SerializedState();
                InputSystem.onSettingsChange += OnSettingsChanged;

                #if !UNITY_DISABLE_DEFAULT_INPUT_PLUGIN_INITIALIZATION
                InputSystem.PerformDefaultPluginInitialization();
                #endif
            }

            var existingStateManagers = Resources.FindObjectsOfTypeAll<InputSystemStateManager>();

            // Upgrade migration: package versions up to and including 1.18 stored this state in a
            // ScriptableObject named InputSystemObject. That instance survives the domain reload
            // triggered by a package upgrade, but the renamed InputSystemStateManager type won't match
            // it. Adopt the surviving legacy instance's state into a new state manager so connected
            // devices (and other state) are preserved across the upgrade instead of being lost - native
            // only re-reports devices once per editor process, so there is no in-process recovery
            // otherwise. See ISX-2569.
            if (globalReset && (existingStateManagers == null || existingStateManagers.Length == 0))
            {
                var legacyObjects = Resources.FindObjectsOfTypeAll<InputSystemObject>();
                if (legacyObjects != null && legacyObjects.Length > 0)
                {
                    var legacy = legacyObjects[0];
                    var migrated = ScriptableObject.CreateInstance<InputSystemStateManager>();
                    migrated.hideFlags = HideFlags.HideAndDontSave;
                    migrated.systemState = legacy.systemState;
                    migrated.newInputBackendsCheckedAsEnabled = legacy.newInputBackendsCheckedAsEnabled;
                    migrated.settings = legacy.settings;
                    migrated.exitEditModeTime = legacy.exitEditModeTime;
                    migrated.enterPlayModeTime = legacy.enterPlayModeTime;

                    // The legacy instances are no longer needed; get rid of them so we don't migrate twice.
                    for (var i = 0; i < legacyObjects.Length; ++i)
                        Object.DestroyImmediate(legacyObjects[i]);

                    existingStateManagers = new[] { migrated };
                }
            }

            if (existingStateManagers != null && existingStateManagers.Length > 0)
            {
                if (globalReset)
                {
                    ////FIXME: does not preserve action map state

                    // If we're coming back out of a domain reload. We're restoring part of the
                    // InputManager state here but we're still waiting from layout registrations
                    // that happen during domain initialization.

                    s_DomainStateManager = existingStateManagers[0];
                    InputSystem.s_Manager.RestoreStateWithoutDevices(s_DomainStateManager.systemState.managerState);
                    InputDebuggerWindow.ReviveAfterDomainReload();

                    // Restore remoting state.
                    InputSystem.remoteConnection = s_DomainStateManager.systemState.remoteConnection;
                    InputSystem.SetUpRemoting();
                    InputSystem.s_Remote.RestoreState(s_DomainStateManager.systemState.remotingState, InputSystem.s_Manager);

                    // Get s_Manager to restore devices on first input update. By that time we
                    // should have all (possibly updated) layout information in place.
                    InputSystem.s_Manager.m_SavedDeviceStates = s_DomainStateManager.systemState.managerState.devices;
                    InputSystem.s_Manager.m_SavedAvailableDevices = s_DomainStateManager.systemState.managerState.availableDevices;

                    // Restore editor settings.
                    InputEditorUserSettings.s_Settings = s_DomainStateManager.systemState.userSettings;

                    // Get rid of saved state.
                    s_DomainStateManager.systemState = new InputSystemState();
                }
            }
            else
            {
                s_DomainStateManager = ScriptableObject.CreateInstance<InputSystemStateManager>();
                s_DomainStateManager.hideFlags = HideFlags.HideAndDontSave;

                // See if we have a settings asset in our EditorBuildSettings.
                if (EditorBuildSettings.TryGetConfigObject(InputSettingsProvider.kEditorBuildSettingsConfigKey,
                    out InputSettings settingsAsset))
                {
                    InputSystem.s_Manager.settings = settingsAsset;
                }

                // See if we have a saved actions object
                var savedActions = ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;
                if (savedActions != null)
                    InputSystem.s_Manager.actions = savedActions;

                InputEditorUserSettings.Load();
                SetUpEditorRemoting();
            }

            Debug.Assert(InputSystem.settings != null);
            Debug.Assert(HasNativeObject(InputSystem.settings), "InputSettings has lost its native object");

            UnityRemoteSupport.Initialize();

            // If native backends for new input system aren't enabled, ask user whether we should enable them
            // (requires restart). We only ask once per session and don't ask when running in batch mode.
            // The warning is delayed to delay call (called a short while after the Asset are loaded, on Inspector
            // update) to make sure it doesn't pop up while the editor is still loading or assets are not fully loaded -
            // this would cancel the import of large assets that are dependent on the InputSystem package and import
            // it as a dependency.
            EditorApplication.delayCall += ShowRestartWarning;

            InputSystem.RunInitialUpdate();

            InputSystem.EnableActions();

            InitializeEditorHooks();

            k_InputInitializeInEditorMarker.End();
        }

        private static void InitializeEditorHooks()
        {
            RegisterPlayModeHooks();
            RegisterAnalyticsHooks();
            RegisterAssetDatabaseHooks();
#if UNITY_INPUT_SYSTEM_ENABLE_UI || PACKAGE_DOCS_GENERATION
            RegisterUIModuleHooks();
#endif
            RegisterNativeInputRuntimeHooks();
            RegisterRemotingHooks();
            RegisterAssemblyReloadHooks();
            RegisterTestHooks();
        }

        private static void RegisterPlayModeHooks()
        {
            InputSystem.s_ShouldEnableActions = ShouldEnableActions;
            InputSystem.s_OnPlayModeChangeCallback = change => OnPlayModeChange((PlayModeStateChange)change);
            InputSystem.s_OnProjectChangeCallback = OnProjectChange;
            InputSystem.s_IsDomainReloadDisabled = IsDomainReloadDisabledForPlayMode;
            InputSystem.s_EditorGlobalInitializeCallback = OnGlobalInitialize;
            InputSystem.s_HasNativeObjectCallback = HasNativeObject;

            InputSystem.s_Manager.m_AddDevicesNotSupportedByProject = () => InputEditorUserSettings.addDevicesNotSupportedByProject;
        }

        private static void RegisterAnalyticsHooks()
        {
            InputAnalytics.IsNewSystemBackendsEnabled = ShouldEnableActionsNewBackend;
            InputAnalytics.IsOldSystemBackendsEnabled = ShouldEnableActionsOldBackend;
            InputActionSetupExtensions.s_ApiUsageCallback = RegisterSetupApiUsage;
            InputActionSetupExtensions.s_SuppressAnalytics = SuppressSetupAnalytics;
        }

        private static void RegisterAssetDatabaseHooks()
        {
            InputSystem.s_OnActionsChanging = ValidateAndTrackActions;
            InputActionAsset.s_OnMarkAsDirty = DirtyAssetTracker.TrackDirtyInputActionAsset;
            InputManager.s_GetProjectWideActions = () => ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;

            InputActionReference.s_CheckImmutableReference = CheckImmutableInputActionReference;

            InputSettings.s_GetIsInspectorIndented = () => EditorGUI.indentLevel > 0;
        }

        /// <summary>
        /// Prevents accidental mutation of the source asset if this <see cref="InputActionReference"/> is a
        /// persisted sub-asset within a .inputactions <see cref="InputActionAsset"/>.
        /// </summary>
        private static void CheckImmutableInputActionReference(InputActionReference reference)
        {
            if (!AssetDatabase.IsSubAsset(reference))
                return;

            var path = AssetDatabase.GetAssetPath(reference);
            if (path == null)
                return;

            var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (!mainAsset || mainAsset is not InputActionAsset)
                return;

            throw new InvalidOperationException(
                "Attempting to modify an immutable InputActionReference instance " +
                "that is part of an .inputactions asset. This is not allowed since it would modify the source " +
                "asset in which the reference is serialized and potentially corrupt it. " +
                "Instead use InputActionReference.Create(action) to create a new mutable " +
                "in-memory instance or serialize it as a separate asset if the intent is for changes to " +
                "survive domain reloads.");
        }

#if UNITY_INPUT_SYSTEM_ENABLE_UI || PACKAGE_DOCS_GENERATION
        private static void RegisterUIModuleHooks()
        {
            UnityEngine.InputSystem.UI.InputSystemUIInputModule.s_OnReset = OnUIInputModuleReset;
        }

#endif

        private static void RegisterNativeInputRuntimeHooks()
        {
            if (InputRuntime.s_Instance is not NativeInputRuntime nativeRuntime)
                return;

            nativeRuntime.m_IsInPlayMode = () => EditorApplication.isPlaying;
            nativeRuntime.m_IsEditorPaused = () => EditorApplication.isPaused;
            nativeRuntime.m_IsEditorActive = () => InternalEditorUtility.isApplicationActive;

            nativeRuntime.m_RegisterWantsToQuit = RegisterWantsToQuit;
            nativeRuntime.m_UnregisterWantsToQuit = UnregisterWantsToQuit;

            nativeRuntime.m_SendEditorAnalytic = SendEditorAnalytic;
        }

        private static void RegisterRemotingHooks()
        {
            RemoteInputPlayerConnection.s_GetInstance = RemoteInputPlayerConnectionEditor.GetInstance;
        }

        private static void RegisterAssemblyReloadHooks()
        {
            EnhancedTouch.EnhancedTouchSupport.s_BeforeAssemblyReloadCallback = RegisterBeforeAssemblyReload;
            EnhancedTouch.EnhancedTouchSupport.s_UnregisterBeforeAssemblyReloadCallback = UnregisterBeforeAssemblyReload;
        }

        private static void RegisterTestHooks()
        {
            InputSystemTestHooks.s_TestHookInitializeForPlayModeTests = TestHook_InitializeForPlayModeTests;
#if !ENABLE_CORECLR
            InputSystemTestHooks.s_TestHookSimulateDomainReload = TestHook_SimulateDomainReload;
#endif
            InputSystemTestHooks.s_TestHookEditorCleanup = TestHook_EditorCleanup;
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
            if (!s_DomainStateManager.newInputBackendsCheckedAsEnabled &&
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
            s_DomainStateManager.newInputBackendsCheckedAsEnabled = true;
            EditorApplication.delayCall -= ShowRestartWarning;
        }

        private static void OnEditorPlayModeStateChanged(PlayModeStateChange change)
        {
            if (InputRuntime.s_Instance is NativeInputRuntime nativeRuntime)
            {
                nativeRuntime.DispatchPlayModeChange((InputPlayModeChange)change);
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
                    s_DomainStateManager.settings = JsonUtility.ToJson(InputSystem.settings);
                    s_DomainStateManager.exitEditModeTime = InputRuntime.s_Instance.currentTime;
                    s_DomainStateManager.enterPlayModeTime = 0;

                    InputSystem.s_Manager.m_ExitEditModeTime = s_DomainStateManager.exitEditModeTime;
                    InputSystem.s_Manager.m_EnterPlayModeTime = 0;
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    s_DomainStateManager.enterPlayModeTime = InputRuntime.s_Instance.currentTime;
                    InputSystem.s_Manager.m_EnterPlayModeTime = s_DomainStateManager.enterPlayModeTime;
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
                    if (!string.IsNullOrEmpty(s_DomainStateManager.settings))
                    {
                        JsonUtility.FromJsonOverwrite(s_DomainStateManager.settings, InputSystem.settings);
                        s_DomainStateManager.settings = null;
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

            s_DomainStateManager.OnBeforeSerialize();
            s_DomainStateManager = null;
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
#endif
