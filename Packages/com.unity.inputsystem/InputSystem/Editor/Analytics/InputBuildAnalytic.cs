#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

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
        public const string kEventName = "inputBuild";
        public const int kMaxEventsPerHour = 100; // default: 1000
        public const int kMaxNumberOfElements = 100; // default: 1000

        private readonly BuildReport m_BuildReport;

        public InputBuildAnalytic(BuildReport buildReport)
        {
            m_BuildReport = buildReport;
        }

        public InputAnalytics.InputAnalyticInfo info { get; }

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
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
                        updateMode = UpdateMode.ProcessEventsInBothFixedAndDynamicUpdate;
                        break;
                    case InputSettings.UpdateMode.ProcessEventsManually:
                        updateMode = UpdateMode.ProcessEventsManually;
                        break;
                    case InputSettings.UpdateMode.ProcessEventsInDynamicUpdate:
                        updateMode = UpdateMode.ProcessEventsInDynamicUpdate;
                        break;
                    case InputSettings.UpdateMode.ProcessEventsInFixedUpdate:
                        updateMode = UpdateMode.ProcessEventsInFixedUpdate;
                        break;
                    default:
                        throw new Exception("Unsupported updateMode");
                }

                switch (settings.backgroundBehavior)
                {
                    case InputSettings.BackgroundBehavior.IgnoreFocus:
                        backgroundBehavior = BackgroundBehavior.IgnoreFocus;
                        break;
                    case InputSettings.BackgroundBehavior.ResetAndDisableAllDevices:
                        backgroundBehavior = BackgroundBehavior.ResetAndDisableAllDevices;
                        break;
                    case InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices:
                        backgroundBehavior = BackgroundBehavior.ResetAndDisableNonBackgroundDevices;
                        break;
                    default:
                        throw new Exception("Unsupported background behavior");
                }

                switch (settings.editorInputBehaviorInPlayMode)
                {
                    case InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus:
                        editorInputBehaviorInPlayMode = EditorInputBehaviorInPlayMode
                            .PointersAndKeyboardsRespectGameViewFocus;
                        break;
                    case InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus:
                        editorInputBehaviorInPlayMode = EditorInputBehaviorInPlayMode
                            .AllDevicesRespectGameViewFocus;
                        break;
                    case InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView:
                        editorInputBehaviorInPlayMode = EditorInputBehaviorInPlayMode
                            .AllDeviceInputAlwaysGoesToGameView;
                        break;
                    default:
                        throw new Exception("Unsupported editor background behavior");
                }

                switch (settings.inputActionPropertyDrawerMode)
                {
                    case InputSettings.InputActionPropertyDrawerMode.Compact:
                        inputActionPropertyDrawerMode = InputActionPropertyDrawerMode.Compact;
                        break;
                    case InputSettings.InputActionPropertyDrawerMode.MultilineBoth:
                        inputActionPropertyDrawerMode = InputActionPropertyDrawerMode.MultilineBoth;
                        break;
                    case InputSettings.InputActionPropertyDrawerMode.MultilineEffective:
                        inputActionPropertyDrawerMode = InputActionPropertyDrawerMode.MultilineEffective;
                        break;
                    default:
                        throw new Exception("Unsupported editor property drawer mode");
                }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
                var inputSystemActions = InputSystem.actions;
                var actionsPath = inputSystemActions == null ? null : AssetDatabase.GetAssetPath(inputSystemActions);
                hasProjectWideInputActionAsset = !string.IsNullOrEmpty(actionsPath);
#else
                hasActions = false;
#endif

                var settingsPath = settings == null ? null : AssetDatabase.GetAssetPath(settings);
                hasSettingsAsset = !string.IsNullOrEmpty(settingsPath);

                compensateForScreenOrientation = settings.compensateForScreenOrientation;
                defaultDeadzoneMin = settings.defaultDeadzoneMin;
                defaultDeadzoneMax = settings.defaultDeadzoneMax;
                defaultButtonPressPoint = settings.defaultButtonPressPoint;
                buttonReleaseThreshold = settings.buttonReleaseThreshold;
                defaultTapTime = settings.defaultTapTime;
                defaultSlowTapTime = settings.defaultSlowTapTime;
                defaultHoldTime = settings.defaultHoldTime;
                tapRadius = settings.tapRadius;
                multiTapDelayTime = settings.multiTapDelayTime;
                maxEventBytesPerUpdate = settings.maxEventBytesPerUpdate;
                maxQueuedEventsPerUpdate = settings.maxQueuedEventsPerUpdate;
                supportedDevices = settings.supportedDevices.ToArray();
                disableRedundantEventsMerging = settings.disableRedundantEventsMerging;
                shortcutKeysConsumeInput = settings.shortcutKeysConsumeInput;

                featureOptimizedControlsEnabled = settings.IsFeatureEnabled(InputFeatureNames.kUseOptimizedControls);
                featureReadValueCachingEnabled = settings.IsFeatureEnabled(InputFeatureNames.kUseReadValueCaching);
                featureParanoidReadValueCachingChecksEnabled =
                    settings.IsFeatureEnabled(InputFeatureNames.kParanoidReadValueCachingChecks);

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
                featureUseIMGUIEditorForAssets =
                    settings.IsFeatureEnabled(InputFeatureNames.kUseIMGUIEditorForAssets);
#else
                featureUseIMGUIEditorForAssets = false;
#endif
                featureDisableUnityRemoteSupport =
                    settings.IsFeatureEnabled(InputFeatureNames.kDisableUnityRemoteSupport);
                featureRunPlayerUpdatesInEditMode =
                    settings.IsFeatureEnabled(InputFeatureNames.kRunPlayerUpdatesInEditMode);

                hasDefaultSettings = InputSettings.AreEqual(settings, defaultSettings);

                buildGuid = report != null ? report.summary.guid.ToString() : string.Empty; // Allows testing
            }

            /// <summary>
            /// Represents <see cref="InputSettings.updateMode"/> and indicates how the project handles updates.
            /// </summary>
            public UpdateMode updateMode;

            /// <summary>
            /// Represents <see cref="InputSettings.compensateForScreenOrientation"/> and if true automatically
            /// adjust rotations when the screen orientation changes.
            /// </summary>
            public bool compensateForScreenOrientation;

            /// <summary>
            /// Represents <see cref="InputSettings.backgroundBehavior"/> which determines what happens when application
            /// focus changes and how the system handle input while running in the background.
            /// </summary>
            public BackgroundBehavior backgroundBehavior;

            // Note: InputSettings.filterNoiseOnCurrent not present since already deprecated when these analytics
            //       where added.
            public float defaultDeadzoneMin;

            /// <summary>
            /// Represents <see cref="InputSettings.defaultDeadzoneMax"/>
            /// </summary>
            public float defaultDeadzoneMax;

            /// <summary>
            /// Represents <see cref="InputSettings.defaultButtonPressPoint"/>
            /// </summary>
            public float defaultButtonPressPoint;

            /// <summary>
            /// Represents <see cref="InputSettings.buttonReleaseThreshold"/>
            /// </summary>
            public float buttonReleaseThreshold;

            /// <summary>
            /// Represents <see cref="InputSettings.defaultSlowTapTime"/>
            /// </summary>
            public float defaultTapTime;

            /// <summary>
            /// Represents <see cref="InputSettings.defaultSlowTapTime"/>
            /// </summary>
            public float defaultSlowTapTime;

            /// <summary>
            /// Represents <see cref="InputSettings.defaultHoldTime"/>
            /// </summary>
            public float defaultHoldTime;

            /// <summary>
            /// Represents <see cref="InputSettings.tapRadius"/>
            /// </summary>
            public float tapRadius;

            /// <summary>
            /// Represents <see cref="InputSettings.multiTapDelayTime"/>
            /// </summary>
            public float multiTapDelayTime;

            /// <summary>
            /// Represents <see cref="InputSettings.editorInputBehaviorInPlayMode"/>
            /// </summary>
            public EditorInputBehaviorInPlayMode editorInputBehaviorInPlayMode;

            /// <summary>
            /// Represents <see cref="InputSettings.inputActionPropertyDrawerMode"/>
            /// </summary>
            public InputActionPropertyDrawerMode inputActionPropertyDrawerMode;

            /// <summary>
            /// Represents <see cref="InputSettings.maxEventBytesPerUpdate"/>
            /// </summary>
            public int maxEventBytesPerUpdate;

            /// <summary>
            /// Represents <see cref="InputSettings.maxQueuedEventsPerUpdate"/>
            /// </summary>
            public int maxQueuedEventsPerUpdate;

            /// <summary>
            /// Represents <see cref="InputSettings.supportedDevices"/>
            /// </summary>
            public string[] supportedDevices;

            /// <summary>
            /// Represents <see cref="InputSettings.disableRedundantEventsMerging"/>
            /// </summary>
            public bool disableRedundantEventsMerging;

            /// <summary>
            /// Represents <see cref="InputSettings.shortcutKeysConsumeInput"/>
            /// </summary>
            public bool shortcutKeysConsumeInput;

            #endregion

            #region Feature flag settings

            /// <summary>
            /// Represents internal feature flag <see cref="InputFeatureNames.kUseOptimizedControls"/> as defined
            /// in Input System 1.8.x.
            /// </summary>
            public bool featureOptimizedControlsEnabled;

            /// <summary>
            /// Represents internal feature flag <see cref="InputFeatureNames.kUseReadValueCaching" /> as defined
            /// in Input System 1.8.x.
            /// </summary>
            public bool featureReadValueCachingEnabled;

            /// <summary>
            /// Represents internal feature flag <see cref="InputFeatureNames.kParanoidReadValueCachingChecks" />
            /// as defined in InputSystem 1.8.x.
            /// </summary>
            public bool featureParanoidReadValueCachingChecksEnabled;

            /// <summary>
            /// Represents internal feature flag <see cref="InputFeatureNames.kUseIMGUIEditorForAssets" />
            /// as defined in InputSystem 1.8.x.
            /// </summary>
            public bool featureUseIMGUIEditorForAssets;

            /// <summary>
            /// Represents internal feature flag <see cref="InputFeatureNames.kDisableUnityRemoteSupport" />
            /// as defined in InputSystem 1.8.x.
            /// </summary>
            public bool featureDisableUnityRemoteSupport;

            /// <summary>
            /// Represents internal feature flag <see cref="InputFeatureNames.kRunPlayerUpdatesInEditMode" />
            /// as defined in InputSystem 1.8.x.
            /// </summary>
            public bool featureRunPlayerUpdatesInEditMode;

            #endregion

            #region

            /// <summary>
            /// Specifies whether the project is using a project-wide input actions asset or not.
            /// </summary>
            public bool hasProjectWideInputActionAsset;

            /// <summary>
            /// Specifies whether the project is using a user-provided settings asset or not.
            /// </summary>
            public bool hasSettingsAsset;

            /// <summary>
            /// Specifies whether the settings asset (if present) of the built project is equal to default settings
            /// or not. In case of no settings asset this is also true since implicitly using default settings.
            /// </summary>
            public bool hasDefaultSettings;

            /// <summary>
            /// A unique GUID identifying the build.
            /// </summary>
            public string buildGuid;

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
