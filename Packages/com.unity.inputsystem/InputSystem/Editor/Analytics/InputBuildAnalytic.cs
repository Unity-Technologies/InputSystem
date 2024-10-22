#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Content;
using UnityEditor.Build.Reporting;
using UnityEngine.Serialization;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Analytics for tracking Player Input component user engagement in the editor.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
        maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
    internal class InputBuildAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
    {
        public const string kEventName = "input_build_completed";
        public const int kMaxEventsPerHour = 100; // default: 1000
        public const int kMaxNumberOfElements = 100; // default: 1000

        private readonly BuildReport m_BuildReport;

        public InputBuildAnalytic(BuildReport buildReport)
        {
            m_BuildReport = buildReport;
        }

        public InputAnalytics.InputAnalyticInfo info =>
            new InputAnalytics.InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);

#if UNITY_2023_2_OR_NEWER
        public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
        public bool TryGatherData(out InputAnalytics.IInputAnalyticData data, out Exception error)
#endif
        {
            InputSettings defaultSettings = null;
            try
            {
                defaultSettings = ScriptableObject.CreateInstance<InputSettings>();
                data = new InputBuildAnalyticData(m_BuildReport, InputSystem.settings, defaultSettings);
                error = null;
                return true;
            }
            catch (Exception e)
            {
                data = null;
                error = e;
                return false;
            }
            finally
            {
                if (defaultSettings != null)
                    Object.DestroyImmediate(defaultSettings);
            }
        }

        /// <summary>
        /// Input system build analytics data structure.
        /// </summary>
        [Serializable]
        internal struct InputBuildAnalyticData : UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
        {
            #region InputSettings

            [Serializable]
            public enum UpdateMode
            {
                ProcessEventsInBothFixedAndDynamicUpdate = 0, // Note: Deprecated
                ProcessEventsInDynamicUpdate = 1,
                ProcessEventsInFixedUpdate = 2,
                ProcessEventsManually = 3,
            }

            [Serializable]
            public enum BackgroundBehavior
            {
                ResetAndDisableNonBackgroundDevices = 0,
                ResetAndDisableAllDevices = 1,
                IgnoreFocus = 2
            }

            [Serializable]
            public enum EditorInputBehaviorInPlayMode
            {
                PointersAndKeyboardsRespectGameViewFocus = 0,
                AllDevicesRespectGameViewFocus = 1,
                AllDeviceInputAlwaysGoesToGameView = 2
            }

            [Serializable]
            public enum InputActionPropertyDrawerMode
            {
                Compact = 0,
                MultilineEffective = 1,
                MultilineBoth = 2
            }

            public InputBuildAnalyticData(BuildReport report, InputSettings settings, InputSettings defaultSettings)
            {
                switch (settings.updateMode)
                {
                    case 0: // ProcessEventsInBothFixedAndDynamicUpdate (deprecated/removed)
                        update_mode = UpdateMode.ProcessEventsInBothFixedAndDynamicUpdate;
                        break;
                    case InputSettings.UpdateMode.ProcessEventsManually:
                        update_mode = UpdateMode.ProcessEventsManually;
                        break;
                    case InputSettings.UpdateMode.ProcessEventsInDynamicUpdate:
                        update_mode = UpdateMode.ProcessEventsInDynamicUpdate;
                        break;
                    case InputSettings.UpdateMode.ProcessEventsInFixedUpdate:
                        update_mode = UpdateMode.ProcessEventsInFixedUpdate;
                        break;
                    default:
                        throw new Exception("Unsupported updateMode");
                }

                switch (settings.backgroundBehavior)
                {
                    case InputSettings.BackgroundBehavior.IgnoreFocus:
                        background_behavior = BackgroundBehavior.IgnoreFocus;
                        break;
                    case InputSettings.BackgroundBehavior.ResetAndDisableAllDevices:
                        background_behavior = BackgroundBehavior.ResetAndDisableAllDevices;
                        break;
                    case InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices:
                        background_behavior = BackgroundBehavior.ResetAndDisableNonBackgroundDevices;
                        break;
                    default:
                        throw new Exception("Unsupported background behavior");
                }

                switch (settings.editorInputBehaviorInPlayMode)
                {
                    case InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus:
                        editor_input_behavior_in_playmode = EditorInputBehaviorInPlayMode
                            .PointersAndKeyboardsRespectGameViewFocus;
                        break;
                    case InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus:
                        editor_input_behavior_in_playmode = EditorInputBehaviorInPlayMode
                            .AllDevicesRespectGameViewFocus;
                        break;
                    case InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView:
                        editor_input_behavior_in_playmode = EditorInputBehaviorInPlayMode
                            .AllDeviceInputAlwaysGoesToGameView;
                        break;
                    default:
                        throw new Exception("Unsupported editor background behavior");
                }

                switch (settings.inputActionPropertyDrawerMode)
                {
                    case InputSettings.InputActionPropertyDrawerMode.Compact:
                        input_action_property_drawer_mode = InputActionPropertyDrawerMode.Compact;
                        break;
                    case InputSettings.InputActionPropertyDrawerMode.MultilineBoth:
                        input_action_property_drawer_mode = InputActionPropertyDrawerMode.MultilineBoth;
                        break;
                    case InputSettings.InputActionPropertyDrawerMode.MultilineEffective:
                        input_action_property_drawer_mode = InputActionPropertyDrawerMode.MultilineEffective;
                        break;
                    default:
                        throw new Exception("Unsupported editor property drawer mode");
                }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
                var inputSystemActions = InputSystem.actions;
                var actionsPath = inputSystemActions == null ? null : AssetDatabase.GetAssetPath(inputSystemActions);
                has_projectwide_input_action_asset = !string.IsNullOrEmpty(actionsPath);
#else
                has_projectwide_input_action_asset = false;
#endif

                var settingsPath = settings == null ? null : AssetDatabase.GetAssetPath(settings);
                has_settings_asset = !string.IsNullOrEmpty(settingsPath);

                compensate_for_screen_orientation = settings.compensateForScreenOrientation;
                default_deadzone_min = settings.defaultDeadzoneMin;
                default_deadzone_max = settings.defaultDeadzoneMax;
                default_button_press_point = settings.defaultButtonPressPoint;
                button_release_threshold = settings.buttonReleaseThreshold;
                default_tap_time = settings.defaultTapTime;
                default_slow_tap_time = settings.defaultSlowTapTime;
                default_hold_time = settings.defaultHoldTime;
                tap_radius = settings.tapRadius;
                multi_tap_delay_time = settings.multiTapDelayTime;
                max_event_bytes_per_update = settings.maxEventBytesPerUpdate;
                max_queued_events_per_update = settings.maxQueuedEventsPerUpdate;
                supported_devices = settings.supportedDevices.ToArray();
                disable_redundant_events_merging = settings.disableRedundantEventsMerging;
                shortcut_keys_consume_input = settings.shortcutKeysConsumeInput;

                feature_optimized_controls_enabled = settings.IsFeatureEnabled(InputFeatureNames.kUseOptimizedControls);
                feature_read_value_caching_enabled = settings.IsFeatureEnabled(InputFeatureNames.kUseReadValueCaching);
                feature_paranoid_read_value_caching_checks_enabled =
                    settings.IsFeatureEnabled(InputFeatureNames.kParanoidReadValueCachingChecks);

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
                feature_use_imgui_editor_for_assets =
                    settings.IsFeatureEnabled(InputFeatureNames.kUseIMGUIEditorForAssets);
#else
                feature_use_imgui_editor_for_assets = false;
#endif
                feature_disable_unity_remote_support =
                    settings.IsFeatureEnabled(InputFeatureNames.kDisableUnityRemoteSupport);
                feature_run_player_updates_in_editmode =
                    settings.IsFeatureEnabled(InputFeatureNames.kRunPlayerUpdatesInEditMode);

                has_default_settings = InputSettings.AreEqual(settings, defaultSettings);

                build_guid = report != null ? report.summary.guid.ToString() : string.Empty; // Allows testing
            }

            /// <summary>
            /// Represents <see cref="InputSettings.updateMode"/> and indicates how the project handles updates.
            /// </summary>
            public UpdateMode update_mode;

            /// <summary>
            /// Represents <see cref="InputSettings.compensateForScreenOrientation"/> and if true automatically
            /// adjust rotations when the screen orientation changes.
            /// </summary>
            public bool compensate_for_screen_orientation;

            /// <summary>
            /// Represents <see cref="InputSettings.backgroundBehavior"/> which determines what happens when application
            /// focus changes and how the system handle input while running in the background.
            /// </summary>
            public BackgroundBehavior background_behavior;

            // Note: InputSettings.filterNoiseOnCurrent not present since already deprecated when these analytics
            //       where added.

            /// <summary>
            /// Represents <see cref="InputSettings.defaultDeadzoneMin"/>
            /// </summary>
            public float default_deadzone_min;

            /// <summary>
            /// Represents <see cref="InputSettings.defaultDeadzoneMax"/>
            /// </summary>
            public float default_deadzone_max;

            /// <summary>
            /// Represents <see cref="InputSettings.defaultButtonPressPoint"/>
            /// </summary>
            public float default_button_press_point;

            /// <summary>
            /// Represents <see cref="InputSettings.buttonReleaseThreshold"/>
            /// </summary>
            public float button_release_threshold;

            /// <summary>
            /// Represents <see cref="InputSettings.defaultSlowTapTime"/>
            /// </summary>
            public float default_tap_time;

            /// <summary>
            /// Represents <see cref="InputSettings.defaultSlowTapTime"/>
            /// </summary>
            public float default_slow_tap_time;

            /// <summary>
            /// Represents <see cref="InputSettings.defaultHoldTime"/>
            /// </summary>
            public float default_hold_time;

            /// <summary>
            /// Represents <see cref="InputSettings.tapRadius"/>
            /// </summary>
            public float tap_radius;

            /// <summary>
            /// Represents <see cref="InputSettings.multiTapDelayTime"/>
            /// </summary>
            public float multi_tap_delay_time;

            /// <summary>
            /// Represents <see cref="InputSettings.editorInputBehaviorInPlayMode"/>
            /// </summary>
            public EditorInputBehaviorInPlayMode editor_input_behavior_in_playmode;

            /// <summary>
            /// Represents <see cref="InputSettings.inputActionPropertyDrawerMode"/>
            /// </summary>
            public InputActionPropertyDrawerMode input_action_property_drawer_mode;

            /// <summary>
            /// Represents <see cref="InputSettings.maxEventBytesPerUpdate"/>
            /// </summary>
            public int max_event_bytes_per_update;

            /// <summary>
            /// Represents <see cref="InputSettings.maxQueuedEventsPerUpdate"/>
            /// </summary>
            public int max_queued_events_per_update;

            /// <summary>
            /// Represents <see cref="InputSettings.supportedDevices"/>
            /// </summary>
            public string[] supported_devices;

            /// <summary>
            /// Represents <see cref="InputSettings.disableRedundantEventsMerging"/>
            /// </summary>
            public bool disable_redundant_events_merging;

            /// <summary>
            /// Represents <see cref="InputSettings.shortcutKeysConsumeInput"/>
            /// </summary>
            public bool shortcut_keys_consume_input;

            #endregion

            #region Feature flag settings

            /// <summary>
            /// Represents internal feature flag <see cref="InputFeatureNames.kUseOptimizedControls"/> as defined
            /// in Input System 1.8.x.
            /// </summary>
            public bool feature_optimized_controls_enabled;

            /// <summary>
            /// Represents internal feature flag <see cref="InputFeatureNames.kUseReadValueCaching" /> as defined
            /// in Input System 1.8.x.
            /// </summary>
            public bool feature_read_value_caching_enabled;

            /// <summary>
            /// Represents internal feature flag <see cref="InputFeatureNames.kParanoidReadValueCachingChecks" />
            /// as defined in InputSystem 1.8.x.
            /// </summary>
            public bool feature_paranoid_read_value_caching_checks_enabled;

            /// <summary>
            /// Represents internal feature flag <see cref="InputFeatureNames.kUseIMGUIEditorForAssets" />
            /// as defined in InputSystem 1.8.x.
            /// </summary>
            public bool feature_use_imgui_editor_for_assets;

            /// <summary>
            /// Represents internal feature flag <see cref="InputFeatureNames.kDisableUnityRemoteSupport" />
            /// as defined in InputSystem 1.8.x.
            /// </summary>
            public bool feature_disable_unity_remote_support;

            /// <summary>
            /// Represents internal feature flag <see cref="InputFeatureNames.kRunPlayerUpdatesInEditMode" />
            /// as defined in InputSystem 1.8.x.
            /// </summary>
            public bool feature_run_player_updates_in_editmode;

            #endregion

            #region

            /// <summary>
            /// Specifies whether the project is using a project-wide input actions asset or not.
            /// </summary>
            public bool has_projectwide_input_action_asset;

            /// <summary>
            /// Specifies whether the project is using a user-provided settings asset or not.
            /// </summary>
            public bool has_settings_asset;

            /// <summary>
            /// Specifies whether the settings asset (if present) of the built project is equal to default settings
            /// or not. In case of no settings asset this is also true since implicitly using default settings.
            /// </summary>
            public bool has_default_settings;

            /// <summary>
            /// A unique GUID identifying the build.
            /// </summary>
            public string build_guid;

            #endregion
        }

        /// <summary>
        /// Input System build analytics.
        /// </summary>
        internal class ReportProcessor : IPostprocessBuildWithReport
        {
            public int callbackOrder => int.MaxValue;

            public void OnPostprocessBuild(BuildReport report)
            {
                InputSystem.s_Manager?.m_Runtime?.SendAnalytic(new InputBuildAnalytic(report));
            }
        }
    }
}
#endif
