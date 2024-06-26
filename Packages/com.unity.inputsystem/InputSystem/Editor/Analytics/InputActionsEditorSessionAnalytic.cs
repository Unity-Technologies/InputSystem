#if UNITY_EDITOR
using System;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Analytics record for tracking engagement with Input Action Asset editor(s).
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UnityEngine.Analytics.AnalyticInfo(eventName: kEventName, maxEventsPerHour: kMaxEventsPerHour,
        maxNumberOfElements: kMaxNumberOfElements, vendorKey: UnityEngine.InputSystem.InputAnalytics.kVendorKey)]
#endif // UNITY_2023_2_OR_NEWER
    internal class InputActionsEditorSessionAnalytic : UnityEngine.InputSystem.InputAnalytics.IInputAnalytic
    {
        public const string kEventName = "inputActionEditorWindowSession";
        public const int kMaxEventsPerHour = 100;     // default: 1000
        public const int kMaxNumberOfElements = 100;     // default: 1000

        /// <summary>
        /// Construct a new <c>InputActionsEditorSession</c> record of the given <para>type</para>.
        /// </summary>
        /// <param name="kind">The editor type for which this record is valid.</param>
        public InputActionsEditorSessionAnalytic(Data.Kind kind)
        {
            if (kind == Data.Kind.Invalid)
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
                return;     // No pending session

            ++m_Data.explicitSaveCount;
        }

        /// <summary>
        /// Register a user-event related to implicitly saving in the editor, e.g.
        /// by having auto-save enabled and indirectly saving the associated asset.
        /// </summary>
        public void RegisterAutoSave()
        {
            if (!hasSession)
                return;     // No pending session

            ++m_Data.autoSaveCount;
        }

        /// <summary>
        /// Register a user-event related to resetting the editor action configuration to defaults.
        /// </summary>
        public void RegisterReset()
        {
            if (!hasSession)
                return;     // No pending session

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
                return;     // Session already started.

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
                return;     // No pending session

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

        private void Initialize(Data.Kind kind)
        {
            m_FocusStart = float.NaN;
            m_SessionStart = float.NaN;

            m_Data = new Data(kind);
        }

        private bool ImplicitFocus()
        {
            if (!hasSession)
                return false;
            if (!hasFocus)
                RegisterEditorFocusIn();
            return true;
        }

        private Data m_Data;
        private double m_FocusStart;
        private double m_SessionStart;

        private static IInputRuntime runtime => InputSystem.s_Manager.m_Runtime;
        private bool hasFocus => !double.IsNaN(m_FocusStart);
        private bool hasSession => !double.IsNaN(m_SessionStart);
        // Returns current time since startup. Note that IInputRuntime explicitly defines in interface that
        // IInputRuntime.currentTime corresponds to EditorApplication.timeSinceStartup in editor.
        private double currentTime => runtime.currentTime;
        private bool isValid => m_Data.sessionDurationSeconds >= 0;

        [Serializable]
        public struct Data : UnityEngine.InputSystem.InputAnalytics.IInputAnalyticData
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
                EditorWindow = 1,
                EmbeddedInProjectSettings = 2
            }

            /// <summary>
            /// Constructs a <c>InputActionsEditorSessionData</c>.
            /// </summary>
            /// <param name="kind">Specifies the kind of editor metrics is being collected for.</param>
            public Data(Kind kind)
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
    }
}
#endif
