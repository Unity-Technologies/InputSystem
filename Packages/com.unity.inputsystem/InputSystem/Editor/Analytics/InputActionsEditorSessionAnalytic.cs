#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Serialization;

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
        public const string kEventName = "input_actionasset_editor_closed";
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
                ++m_Data.action_map_modification_count;
        }

        /// <summary>
        /// Register that an action edit has occurred.
        /// </summary>
        public void RegisterActionEdit()
        {
            if (ImplicitFocus() && ComputeDuration() > 0.5) // Avoid logging actions triggered via UI initialization
                ++m_Data.action_modification_count;
        }

        /// <summary>
        /// Register than a binding edit has occurred.
        /// </summary>
        public void RegisterBindingEdit()
        {
            if (ImplicitFocus())
                ++m_Data.binding_modification_count;
        }

        /// <summary>
        /// Register that a control scheme edit has occurred.
        /// </summary>
        public void RegisterControlSchemeEdit()
        {
            if (ImplicitFocus())
                ++m_Data.control_scheme_modification_count;
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
            m_Data.session_focus_duration_seconds += (float)duration;
            ++m_Data.session_focus_switch_count;
        }

        /// <summary>
        /// Register a user-event related to explicitly saving in the editor, e.g.
        /// using a button, menu or short-cut to trigger the save command.
        /// </summary>
        public void RegisterExplicitSave()
        {
            if (!hasSession)
                return;     // No pending session

            ++m_Data.explicit_save_count;
        }

        /// <summary>
        /// Register a user-event related to implicitly saving in the editor, e.g.
        /// by having auto-save enabled and indirectly saving the associated asset.
        /// </summary>
        public void RegisterAutoSave()
        {
            if (!hasSession)
                return;     // No pending session

            ++m_Data.auto_save_count;
        }

        /// <summary>
        /// Register a user-event related to resetting the editor action configuration to defaults.
        /// </summary>
        public void RegisterReset()
        {
            if (!hasSession)
                return;     // No pending session

            ++m_Data.reset_count;
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
            var duration = ComputeDuration();
            m_Data.session_duration_seconds += duration;

            // Sanity check data, if less than a second its likely a glitch so avoid sending incorrect data
            // Send analytics event
            if (duration >= 1.0)
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

        private double ComputeDuration() => hasSession ? currentTime - m_SessionStart : 0.0;

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
        private bool isValid => m_Data.session_duration_seconds >= 0;

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
                session_duration_seconds = 0;
                session_focus_duration_seconds = 0;
                session_focus_switch_count = 0;
                action_map_modification_count = 0;
                action_modification_count = 0;
                binding_modification_count = 0;
                explicit_save_count = 0;
                auto_save_count = 0;
                reset_count = 0;
                control_scheme_modification_count = 0;
            }

            /// <summary>
            /// Specifies what kind of Input Actions editor this event represents.
            /// </summary>
            public Kind kind;

            /// <summary>
            /// The total duration for the session, i.e. the duration during which the editor window was open.
            /// </summary>
            public double session_duration_seconds;

            /// <summary>
            /// The total duration for which the editor window was open and had focus.
            /// </summary>
            public double session_focus_duration_seconds;

            /// <summary>
            /// Specifies the number of times the window has transitioned from not having focus to having focus in a single session.
            /// </summary>
            public int session_focus_switch_count;

            /// <summary>
            /// The total number of action map modifications during the session.
            /// </summary>
            public int action_map_modification_count;

            /// <summary>
            /// The total number of action modifications during the session.
            /// </summary>
            public int action_modification_count;

            /// The total number of binding modifications during the session.
            /// </summary>
            public int binding_modification_count;

            /// <summary>
            /// The total number of controls scheme modifications during the session.
            /// </summary>
            public int control_scheme_modification_count;

            /// <summary>
            /// The total number of explicit saves during the session, i.e. as in user-initiated save.
            /// </summary>
            public int explicit_save_count;

            /// <summary>
            /// The total number of automatic saves during the session, i.e. as in auto-save on close or focus-lost.
            /// </summary>
            public int auto_save_count;

            /// <summary>
            /// The total number of user-initiated resets during the session, i.e. as in using Reset option in menu.
            /// </summary>
            public int reset_count;

            public bool isValid => kind != Kind.Invalid && session_duration_seconds >= 0;
        }
    }
}
#endif
