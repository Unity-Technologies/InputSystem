#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Analytics;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Serialization;

namespace UnityEngine.InputSystem.Editor
{
    internal static class InputEditorAnalytics
    {
        // TODO Consider splitting into component editor engagement with string encoded component type
        //      and instead handle possible future component specific analytics separately to avoid
        //      code bloat.

        public struct PlayerInputInspectorEditorUserEngagementData : UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
        {
            /// <summary>
            /// Identifies the number of OnValidate() calls related to the component editor.
            /// </summary>
            public int onValidateCount;

            /// <summary>
            /// Represents the number of OnDestroy() calls related to the component editor.
            /// </summary>
            public int onDestroyCount;
        }

        public struct InputActionInspectorEditorUserEngagementData : UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
        {
            /// <summary>
            /// Identifies the number of OnValidate() calls related to the component editor.
            /// </summary>
            public int onValidateCount;

            /// <summary>
            /// Represents the number of OnDestroy() calls related to the component editor.
            /// </summary>
            public int onDestroyCount;
        }

        /// <summary>
        /// Analytics for tracking Player Input component user engagement in the editor.
        /// </summary>
#if UNITY_2023_2_OR_NEWER
        [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
            maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
        public class PlayerInputEditorUserEngagementAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
        {
            public const string kEventName = "playerInputInspectorEditorUserEngagement";
            public const int kMaxEventsPerHour = 100; // default: 1000
            public const int kMaxNumberOfElements = 100; // default: 1000

            private readonly PlayerInputEditor m_Editor;

            public PlayerInputEditorUserEngagementAnalytic(PlayerInputEditor editor)
            {
                m_Editor = editor;
            }

            public InputAnalytics.InputAnalyticInfo info { get; }

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
            public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
            public bool TryGatherData(out InputAnalytics.IInputAnalyticData data, out Exception error)
#endif
            {
                data = m_Editor.m_AnalyticsData;
                error = null;
                return true;
            }
        }

        /// <summary>
        /// Analytics for tracking Player Input component user engagement in the editor.
        /// </summary>
#if UNITY_2023_2_OR_NEWER
        [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
            maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
        public class InputActionInspectorEditorUserEngagementAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
        {
            public const string kEventName = "inputActionInspectorEditorUserEngagement";
            public const int kMaxEventsPerHour = 100; // default: 1000
            public const int kMaxNumberOfElements = 100; // default: 1000

            private readonly InputActionDrawer m_Editor;

            public InputActionInspectorEditorUserEngagementAnalytic(InputActionDrawer editor)
            {
                m_Editor = editor;
            }

            public InputAnalytics.InputAnalyticInfo info { get; }

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
            public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
            public bool TryGatherData(out InputAnalytics.IInputAnalyticData data, out Exception error)
#endif
            {
                data = m_Editor.m_AnalyticsData;
                error = null;
                return true;
            }
        }

        [Serializable]
        public struct InputActionsEditorSessionData : UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
        {
            /// <summary>
            /// Represents an editor type.
            /// </summary>
            /// <remarks>
            /// This may be added to in the future but items may never be removed.
            /// </remarks>
            [Serializable]
            public enum Kind
            {
                Invalid = 0,
                FreeFloatingEditorWindow = 1,
                EmbeddedInProjectSettings = 2
            }

            /// <summary>
            /// Constructs a <c>InputActionsEditorSessionData</c>.
            /// </summary>
            /// <param name="kind">Specifies the kind of editor metrics is being collected for.</param>
            public InputActionsEditorSessionData(Kind kind)
            {
                this.kind = kind;
                sessionDurationSeconds = 0;
                sessionFocusDurationSeconds = 0;
                sessionFocusDurationSeconds = 0;
                sessionFocusSwitchCount = 0;
                actionMapModificationCount = 0;
                actionModificationCount = 0;
                bindingModificationCount = 0;
                explicitSaveCount = 0;
                autoSaveCount = 0;
                resetCount = 0;
                controlSchemeModificationCount = 0;
            }

            /// <summary>
            /// Specifies what kind of Input Actions editor this event represents.
            /// </summary>
            public Kind kind;

            /// <summary>
            /// The total duration for the session, i.e. the duration during which the editor window was open.
            /// </summary>
            public float sessionDurationSeconds;

            /// <summary>
            /// The total duration for which the editor window was open and had focus.
            /// </summary>
            public float sessionFocusDurationSeconds;

            /// <summary>
            /// Specifies the number of times the window has transitioned from not having focus to having focus in a single session.
            /// </summary>
            public int sessionFocusSwitchCount;

            /// <summary>
            /// The total number of action map modifications during the session.
            /// </summary>
            public int actionMapModificationCount;

            /// <summary>
            /// The total number of action modifications during the session.
            /// </summary>
            public int actionModificationCount;

            /// <summary>
            /// The total number of binding modifications during the session.
            /// </summary>
            public int bindingModificationCount;

            /// <summary>
            /// The total number of controls scheme modifications during the session.
            /// </summary>
            public int controlSchemeModificationCount;

            /// <summary>
            /// The total number of explicit saves during the session, i.e. as in user-initiated save.
            /// </summary>
            public int explicitSaveCount;

            /// <summary>
            /// The total number of automatic saves during the session, i.e. as in auto-save on close or focus-lost.
            /// </summary>
            public int autoSaveCount;

            /// <summary>
            /// The total number of user-initiated resets during the session, i.e. as in using Reset option in menu.
            /// </summary>
            public int resetCount;

            public bool isValid => kind != Kind.Invalid && sessionDurationSeconds >= 0;

            public override string ToString()
            {
                return $"{nameof(kind)}: {kind}, " +
                    $"{nameof(sessionDurationSeconds)}: {sessionDurationSeconds} seconds, " +
                    $"{nameof(sessionFocusDurationSeconds)}: {sessionFocusDurationSeconds} seconds, " +
                    $"{nameof(sessionFocusSwitchCount)}: {sessionFocusSwitchCount}, " +
                    $"{nameof(actionMapModificationCount)}: {actionMapModificationCount}, " +
                    $"{nameof(actionModificationCount)}: {actionModificationCount}, " +
                    $"{nameof(bindingModificationCount)}: {bindingModificationCount}, " +
                    $"{nameof(controlSchemeModificationCount)}: {controlSchemeModificationCount}, " +
                    $"{nameof(explicitSaveCount)}: {explicitSaveCount}, " +
                    $"{nameof(autoSaveCount)}: {autoSaveCount}" +
                    $"{nameof(resetCount)}: {resetCount}";
            }
        }

        /// <summary>
        /// Analytics record for tracking engagement with Input Action Asset editor(s).
        /// </summary>
#if UNITY_2023_2_OR_NEWER
        [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
            maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
        public class InputActionsEditorSessionAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
        {
            public const string kEventName = "inputActionEditorWindowSession";
            public const int kMaxEventsPerHour = 100; // default: 1000
            public const int kMaxNumberOfElements = 100; // default: 1000

            /// <summary>
            /// Construct a new <c>InputActionsEditorSession</c> record of the given <para>type</para>.
            /// </summary>
            /// <param name="kind">The editor type for which this record is valid.</param>
            public InputActionsEditorSessionAnalytic(InputActionsEditorSessionData.Kind kind)
            {
                if (kind == InputActionsEditorSessionData.Kind.Invalid)
                    throw new ArgumentException(nameof(kind));

                Initialize(kind);
            }

            /// <summary>
            /// Register that an action map edit has occurred.
            /// </summary>
            public void RegisterActionMapEdit()
            {
                if (ImplicitFocus())
                    ++m_Data.actionMapModificationCount;
            }

            /// <summary>
            /// Register that an action edit has occurred.
            /// </summary>
            public void RegisterActionEdit()
            {
                if (ImplicitFocus())
                    ++m_Data.actionModificationCount;
            }

            /// <summary>
            /// Register than a binding edit has occurred.
            /// </summary>
            public void RegisterBindingEdit()
            {
                if (ImplicitFocus())
                    ++m_Data.bindingModificationCount;
            }

            /// <summary>
            /// Register that a control scheme edit has occurred.
            /// </summary>
            public void RegisterControlSchemeEdit()
            {
                if (ImplicitFocus())
                    ++m_Data.controlSchemeModificationCount;
            }

            /// <summary>
            /// Register that the editor has received focus which is expected to reflect that the user
            /// is currently exploring or editing it.
            /// </summary>
            public void RegisterEditorFocusIn()
            {
                if (!hasSession || hasFocus)
                    return;

                m_FocusStart = currentTime;
            }

            /// <summary>
            /// Register that the editor has lost focus which is expected to reflect that the user currently
            /// has the attention elsewhere.
            /// </summary>
            /// <remarks>
            /// Calling this method without having an ongoing session and having focus will not have any effect.
            /// </remarks>
            public void RegisterEditorFocusOut()
            {
                if (!hasSession || !hasFocus)
                    return;

                var duration = currentTime - m_FocusStart;
                m_FocusStart = float.NaN;
                m_Data.sessionFocusDurationSeconds += (float)duration;
                ++m_Data.sessionFocusSwitchCount;
            }

            /// <summary>
            /// Register a user-event related to explicitly saving in the editor, e.g.
            /// using a button, menu or short-cut to trigger the save command.
            /// </summary>
            public void RegisterExplicitSave()
            {
                if (!hasSession)
                    return; // No pending session

                ++m_Data.explicitSaveCount;
            }

            /// <summary>
            /// Register a user-event related to implicitly saving in the editor, e.g.
            /// by having auto-save enabled and indirectly saving the associated asset.
            /// </summary>
            public void RegisterAutoSave()
            {
                if (!hasSession)
                    return; // No pending session

                ++m_Data.autoSaveCount;
            }

            /// <summary>
            /// Register a user-event related to resetting the editor action configuration to defaults.
            /// </summary>
            public void RegisterReset()
            {
                if (!hasSession)
                    return; // No pending session

                ++m_Data.resetCount;
            }

            /// <summary>
            /// Begins a new session if the session has not already been started.
            /// </summary>
            /// <remarks>
            /// If the session has already been started due to a previous call to <see cref="Begin()"/> without
            /// a call to <see cref="End()"/> this method has no effect.
            /// </remarks>
            public void Begin()
            {
                if (hasSession)
                    return; // Session already started.

                m_SessionStart = currentTime;
            }

            /// <summary>
            /// Ends the current session.
            /// </summary>
            /// <remarks>
            /// If the session has not previously been started via a call to <see cref="Begin()"/> calling this
            /// method has no effect.
            /// </remarks>
            public void End()
            {
                if (!hasSession)
                    return; // No pending session

                // Make sure we register focus out if failed to capture or not invoked
                if (hasFocus)
                    RegisterEditorFocusOut();

                // Compute and record total session duration
                var duration = currentTime - m_SessionStart;
                m_Data.sessionDurationSeconds += (float)duration;

                // Send analytics event
                runtime.SendAnalytic(this);

                // Reset to allow instance to be reused
                Initialize(m_Data.kind);
            }

            #region IInputAnalytic Interface

#if UNITY_EDITOR && UNITY_2023_2_OR_NEWER
            public bool TryGatherData(out UnityEngine.Analytics.IAnalytic.IData data, out Exception error)
#else
            public bool TryGatherData(out InputAnalytics.IInputAnalyticData data, out Exception error)
#endif
            {
                if (!isValid)
                {
                    data = null;
                    error = new Exception("Unable to gather data without a valid session");
                    return false;
                }

                data = this.m_Data;
                error = null;
                return true;
            }

            public InputAnalytics.InputAnalyticInfo info => new InputAnalytics.InputAnalyticInfo(kEventName, kMaxEventsPerHour, kMaxNumberOfElements);

            #endregion

            private void Initialize(InputActionsEditorSessionData.Kind kind)
            {
                m_FocusStart = float.NaN;
                m_SessionStart = float.NaN;

                m_Data = new InputActionsEditorSessionData(kind);
            }

            private bool ImplicitFocus()
            {
                if (!hasSession)
                    return false;
                if (!hasFocus)
                    RegisterEditorFocusIn();
                return true;
            }

            private InputActionsEditorSessionData m_Data;
            private double m_FocusStart;
            private double m_SessionStart;

            private static IInputRuntime runtime => InputSystem.s_Manager.m_Runtime;
            private bool hasFocus => !double.IsNaN(m_FocusStart);
            private bool hasSession => !double.IsNaN(m_SessionStart);
            // Returns current time since startup. Note that IInputRuntime explicitly defines in interface that
            // IInputRuntime.currentTime corresponds to EditorApplication.timeSinceStartup in editor.
            private double currentTime => runtime.currentTime;
            private bool isValid => m_Data.sessionDurationSeconds >= 0;
        }

        /// <summary>
        /// Analytics for tracking Player Input component user engagement in the editor.
        /// </summary>
#if UNITY_2023_2_OR_NEWER
        [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
            maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
        public class InputBuildAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
        {
            public const string kEventName = "inputSystemBuildInsights";
            public const int kMaxEventsPerHour = 100; // default: 1000
            public const int kMaxNumberOfElements = 100; // default: 1000

            public InputBuildAnalytic()
            {}

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
                    data = GatherData(defaultSettings);
                    error = null;
                    return true;
                }
                catch (Exception e)
                {
                    if (defaultSettings != null)
                        Object.DestroyImmediate(defaultSettings);
                    data = null;
                    error = e;
                    return false;
                }
            }

            private static bool CompareFloats(float a, float b)
            {
                return (a - b) <= float.Epsilon;
            }

            private static bool CompareSets<T>(IReadOnlyList<T> a, IReadOnlyList<T> b)
            {
                if (ReferenceEquals(null, a))
                    return ReferenceEquals(null, b);
                if (ReferenceEquals(null, b))
                    return false;
                for (var i = 0; i < a.Count; ++i)
                {
                    bool existsInB = false;
                    for (var j = 0; j < b.Count; ++j)
                    {
                        if (a[i].Equals(b[j]))
                        {
                            existsInB = true;
                            break;
                        }
                    }

                    if (!existsInB)
                        return false;
                }

                return true;
            }

            private static bool CompareFeatureFlag(InputSettings a, InputSettings b, string featureName)
            {
                return a.IsFeatureEnabled(featureName) == b.IsFeatureEnabled(featureName);
            }

            private static bool EqualSettings(InputSettings a, InputSettings b)
            {
                return (a.updateMode == b.updateMode) &&
                    (a.compensateForScreenOrientation == b.compensateForScreenOrientation) &&
                    // Ignoring filterNoiseOnCurrent since deprecated
                    CompareFloats(a.defaultDeadzoneMin, b.defaultDeadzoneMin) &&
                    CompareFloats(a.defaultDeadzoneMax, b.defaultDeadzoneMax) &&
                    CompareFloats(a.defaultButtonPressPoint, b.defaultButtonPressPoint) &&
                    CompareFloats(a.buttonReleaseThreshold, b.buttonReleaseThreshold) &&
                    CompareFloats(a.defaultTapTime, b.defaultTapTime) &&
                    CompareFloats(a.defaultSlowTapTime, b.defaultSlowTapTime) &&
                    CompareFloats(a.defaultHoldTime, b.defaultHoldTime) &&
                    CompareFloats(a.tapRadius, b.tapRadius) &&
                    CompareFloats(a.multiTapDelayTime, b.multiTapDelayTime) &&
                    a.backgroundBehavior == b.backgroundBehavior &&
                    a.editorInputBehaviorInPlayMode == b.editorInputBehaviorInPlayMode &&
                    a.inputActionPropertyDrawerMode == b.inputActionPropertyDrawerMode &&
                    a.maxEventBytesPerUpdate == b.maxEventBytesPerUpdate &&
                    a.maxQueuedEventsPerUpdate == b.maxQueuedEventsPerUpdate &&
                    CompareSets(a.supportedDevices, b.supportedDevices) &&
                    a.disableRedundantEventsMerging == b.disableRedundantEventsMerging &&
                    a.shortcutKeysConsumeInput == b.shortcutKeysConsumeInput &&

                    CompareFeatureFlag(a, b, InputFeatureNames.kUseOptimizedControls) &&
                    CompareFeatureFlag(a, b, InputFeatureNames.kUseReadValueCaching) &&
                    CompareFeatureFlag(a, b, InputFeatureNames.kParanoidReadValueCachingChecks) &&
                    CompareFeatureFlag(a, b, InputFeatureNames.kUseWindowsGamingInputBackend) &&
                    CompareFeatureFlag(a, b, InputFeatureNames.kDisableUnityRemoteSupport) &&
                    CompareFeatureFlag(a, b, InputFeatureNames.kRunPlayerUpdatesInEditMode) &&
                        #if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
                    && CompareFeatureFlag(a, b, InputFeatureNames.kUseIMGUIEditorForAssets);
                        #else
                    true;     // Improves formatting
                        #endif
            }

            private InputBuildAnalyticData GatherData(InputSettings defaultSettings)
            {
                // Fetch settings (may be default, but will never be null)
                var settings = InputSystem.settings;

                // Update mode
                InputBuildAnalyticData.UpdateMode updateMode;
                switch (settings.updateMode)
                {
                    case 0: // ProcessEventsInBothFixedAndDynamicUpdate (deprecated/removed)
                        updateMode = InputBuildAnalyticData.UpdateMode.ProcessEventsInBothFixedAndDynamicUpdate;
                        break;
                    case InputSettings.UpdateMode.ProcessEventsManually:
                        updateMode = InputBuildAnalyticData.UpdateMode.ProcessEventsManually;
                        break;
                    case InputSettings.UpdateMode.ProcessEventsInDynamicUpdate:
                        updateMode = InputBuildAnalyticData.UpdateMode.ProcessEventsInDynamicUpdate;
                        break;
                    case InputSettings.UpdateMode.ProcessEventsInFixedUpdate:
                        updateMode = InputBuildAnalyticData.UpdateMode.ProcessEventsInFixedUpdate;
                        break;
                    default:
                        throw new Exception("Unsupported updateMode");
                }

                // Background behavior
                InputBuildAnalyticData.BackgroundBehavior backgroundBehavior;
                switch (settings.backgroundBehavior)
                {
                    case InputSettings.BackgroundBehavior.IgnoreFocus:
                        backgroundBehavior = InputBuildAnalyticData.BackgroundBehavior.IgnoreFocus;
                        break;
                    case InputSettings.BackgroundBehavior.ResetAndDisableAllDevices:
                        backgroundBehavior = InputBuildAnalyticData.BackgroundBehavior.ResetAndDisableAllDevices;
                        break;
                    case InputSettings.BackgroundBehavior.ResetAndDisableNonBackgroundDevices:
                        backgroundBehavior = InputBuildAnalyticData.BackgroundBehavior.ResetAndDisableNonBackgroundDevices;
                        break;
                    default:
                        throw new Exception("Unsupported background behavior");
                }

                // Input behavior in play-mode
                InputBuildAnalyticData.EditorInputBehaviorInPlayMode editorInputBehaviorInPlayMode;
                switch (settings.editorInputBehaviorInPlayMode)
                {
                    case InputSettings.EditorInputBehaviorInPlayMode.PointersAndKeyboardsRespectGameViewFocus:
                        editorInputBehaviorInPlayMode = InputBuildAnalyticData.EditorInputBehaviorInPlayMode
                            .PointersAndKeyboardsRespectGameViewFocus;
                        break;
                    case InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus:
                        editorInputBehaviorInPlayMode = InputBuildAnalyticData.EditorInputBehaviorInPlayMode
                            .AllDevicesRespectGameViewFocus;
                        break;
                    case InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView:
                        editorInputBehaviorInPlayMode = InputBuildAnalyticData.EditorInputBehaviorInPlayMode
                            .AllDeviceInputAlwaysGoesToGameView;
                        break;
                    default:
                        throw new Exception("Unsupported editor background behavior");
                }

                // Property drawer mode
                InputBuildAnalyticData.InputActionPropertyDrawerMode propertyDrawerMode;
                switch (settings.inputActionPropertyDrawerMode)
                {
                    case InputSettings.InputActionPropertyDrawerMode.Compact:
                        propertyDrawerMode = InputBuildAnalyticData.InputActionPropertyDrawerMode.Compact;
                        break;
                    case InputSettings.InputActionPropertyDrawerMode.MultilineBoth:
                        propertyDrawerMode = InputBuildAnalyticData.InputActionPropertyDrawerMode.MultilineBoth;
                        break;
                    case InputSettings.InputActionPropertyDrawerMode.MultilineEffective:
                        propertyDrawerMode = InputBuildAnalyticData.InputActionPropertyDrawerMode.MultilineEffective;
                        break;
                    default:
                        throw new Exception("Unsupported editor property drawer mode");
                }

                #if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
                var inputSystemActions = InputSystem.actions;
                var actionsPath = inputSystemActions == null ? null : AssetDatabase.GetAssetPath(inputSystemActions);
                var hasActions = !string.IsNullOrEmpty(actionsPath);
                #else
                var hasActions = false;
                #endif

                var settingsPath = settings == null ? null : AssetDatabase.GetAssetPath(settings);
                var hasSettings = !string.IsNullOrEmpty(settingsPath);

                return new InputBuildAnalyticData()
                {
                    updateMode = updateMode,
                    compensateForScreenOrientation =  settings.compensateForScreenOrientation,
                    defaultDeadzoneMin = settings.defaultDeadzoneMin,
                    defaultDeadzoneMax = settings.defaultDeadzoneMax,
                    defaultButtonPressPoint = settings.defaultButtonPressPoint,
                    buttonReleaseThreshold = settings.buttonReleaseThreshold,
                    defaultTapTime = settings.defaultTapTime,
                    defaultSlowTapTime = settings.defaultSlowTapTime,
                    defaultHoldTime = settings.defaultHoldTime,
                    tapRadius = settings.tapRadius,
                    multiTapDelayTime = settings.multiTapDelayTime,
                    backgroundBehavior = backgroundBehavior,
                    editorInputBehaviorInPlayMode = editorInputBehaviorInPlayMode,
                    inputActionPropertyDrawerMode = propertyDrawerMode,
                    maxEventBytesPerUpdate = settings.maxEventBytesPerUpdate,
                    maxQueuedEventsPerUpdate = settings.maxQueuedEventsPerUpdate,
                    supportedDevices = settings.supportedDevices.ToArray(),
                    disableRedundantEventsMerging = settings.disableRedundantEventsMerging,
                    shortcutKeysConsumeInput = settings.shortcutKeysConsumeInput,

                    featureOptimizedControlsEnabled = settings.IsFeatureEnabled(InputFeatureNames.kUseOptimizedControls),
                    featureReadValueCachingEnabled = settings.IsFeatureEnabled(InputFeatureNames.kUseReadValueCaching),
                    featureParanoidReadValueCachingChecksEnabled = settings.IsFeatureEnabled(InputFeatureNames.kParanoidReadValueCachingChecks),

                    #if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
                    featureUseIMGUIEditorForAssets = settings.IsFeatureEnabled(InputFeatureNames.kUseIMGUIEditorForAssets),
                    #else
                    featureUseIMGUIEditorForAssets = false,
                    #endif
                    featureUseWindowsGamingInputBackend = settings.IsFeatureEnabled(InputFeatureNames.kUseWindowsGamingInputBackend),
                    featureDisableUnityRemoteSupport = settings.IsFeatureEnabled(InputFeatureNames.kDisableUnityRemoteSupport),
                    featureRunPlayerUpdatesInEditMode = settings.IsFeatureEnabled(InputFeatureNames.kRunPlayerUpdatesInEditMode),

                    hasProjectWideInputActionAsset = hasActions,
                    hasSettingsAsset = hasSettings,
                    hasDefaultSettings = EqualSettings(settings, defaultSettings)
                };
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
            /// Represents internal feature flag <see cref="InputFeatureNames.kUseWindowsGamingInputBackend" />
            /// as defined in InputSystem 1.8.x.
            /// </summary>
            public bool featureUseWindowsGamingInputBackend;

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

            #endregion
        }

        /// <summary>
        /// Input System build analytics.
        /// </summary>
        internal class InputBuildAnalyticsReportProcessor : IPostprocessBuildWithReport
        {
            public int callbackOrder => int.MaxValue;

            public void OnPostprocessBuild(BuildReport report)
            {
                InputSystem.s_Manager?.m_Runtime?.SendAnalytic(new InputBuildAnalytic());
            }
        }
    }
}

#endif
