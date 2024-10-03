using System;
using System.ComponentModel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

////TODO: protect against the control *hovering* around the press point; this should not fire the press repeatedly; probably need a zone around the press point
////TODO: also, for analog controls, we probably want a deadzone that gives just a tiny little buffer at the low end before the action starts

////REVIEW: shouldn't it use Canceled for release on PressAndRelease instead of triggering Performed again?

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// Performs the action at specific points in a button press-and-release sequence according top <see cref="behavior"/>.
    /// </summary>
    /// <remarks>
    /// By default, uses <see cref="PressBehavior.PressOnly"/> which performs the action as soon as the control crosses the
    /// button press threshold defined by <see cref="pressPoint"/>. The action then will not trigger again until the control
    /// is first released.
    ///
    /// Can be set to instead trigger on release (that is, when the control goes back below the button press threshold) using
    /// <see cref="PressBehavior.ReleaseOnly"/> or can be set to trigger on both press and release using <see cref="PressBehavior.PressAndRelease"/>).
    ///
    /// Note that using an explicit press interaction is only necessary if the goal is to either customize the press behavior
    /// of a button or when binding to controls that are not buttons as such (the press interaction compares magnitudes to
    /// <see cref="pressPoint"/> and thus any type of control that can deliver a magnitude can act as a button). The default
    /// behavior available out of the box when binding <see cref="InputActionType.Button"/> type actions to button-type controls
    /// (<see cref="UnityEngine.InputSystem.Controls.ButtonControl"/>) corresponds to using a press modifier with <see cref="behavior"/>
    /// set to <see cref="PressBehavior.PressOnly"/> and <see cref="pressPoint"/> left at default.
    /// </remarks>
    [DisplayName("Press")]
    public class PressInteraction : IInputInteraction
    {
        /// <summary>
        /// Amount of actuation required before a control is considered pressed.
        /// </summary>
        /// <remarks>
        /// If zero (default), defaults to <see cref="InputSettings.defaultButtonPressPoint"/>.
        /// </remarks>
        [Tooltip("The amount of actuation a control requires before being considered pressed. If not set, default to "
            + "'Default Press Point' in the global input settings.")]
        public float pressPoint;

        ////REVIEW: this should really be named "pressBehavior"
        /// <summary>
        /// Determines how button presses trigger the action.
        /// </summary>
        /// <remarks>
        /// By default (PressOnly), the action is performed on press.
        /// With ReleaseOnly, the action is performed on release. With PressAndRelease, the action is
        /// performed on press and on release.
        /// </remarks>
        [Tooltip("Determines how button presses trigger the action. By default (PressOnly), the action is performed on press. "
            + "With ReleaseOnly, the action is performed on release. With PressAndRelease, the action is performed on press and release.")]
        public PressBehavior behavior;

        private float pressPointOrDefault => pressPoint > 0 ? pressPoint : ButtonControl.s_GlobalDefaultButtonPressPoint;
        private float releasePointOrDefault => pressPointOrDefault * ButtonControl.s_GlobalDefaultButtonReleaseThreshold;
        private bool m_WaitingForRelease;

        public void Process(ref InputInteractionContext context)
        {
            var actuation = context.ComputeMagnitude();
            switch (behavior)
            {
                case PressBehavior.PressOnly:
                    if (m_WaitingForRelease)
                    {
                        if (actuation <= releasePointOrDefault)
                        {
                            m_WaitingForRelease = false;
                            if (Mathf.Approximately(0f, actuation))
                                context.Canceled();
                            else
                                context.Started();
                        }
                    }
                    else if (actuation >= pressPointOrDefault)
                    {
                        m_WaitingForRelease = true;
                        // Stay performed until release.
                        context.PerformedAndStayPerformed();
                    }
                    else if (actuation > 0 && !context.isStarted)
                    {
                        context.Started();
                    }
                    else if (Mathf.Approximately(0f, actuation) && context.isStarted)
                    {
                        context.Canceled();
                    }
                    break;

                case PressBehavior.ReleaseOnly:
                    if (m_WaitingForRelease)
                    {
                        if (actuation <= releasePointOrDefault)
                        {
                            m_WaitingForRelease = false;
                            context.Performed();
                            context.Canceled();
                        }
                    }
                    else if (actuation >= pressPointOrDefault)
                    {
                        m_WaitingForRelease = true;
                        if (!context.isStarted)
                            context.Started();
                    }
                    else
                    {
                        var started = context.isStarted;
                        if (actuation > 0 && !started)
                            context.Started();
                        else if (Mathf.Approximately(0, actuation) && started)
                            context.Canceled();
                    }
                    break;

                case PressBehavior.PressAndRelease:
                    if (m_WaitingForRelease)
                    {
                        if (actuation <= releasePointOrDefault)
                        {
                            m_WaitingForRelease = false;
                            context.Performed();
                            if (Mathf.Approximately(0, actuation))
                                context.Canceled();
                        }
                    }
                    else if (actuation >= pressPointOrDefault)
                    {
                        m_WaitingForRelease = true;
                        context.PerformedAndStayPerformed();
                    }
                    else
                    {
                        var started = context.isStarted;
                        if (actuation > 0 && !started)
                            context.Started();
                        else if (Mathf.Approximately(0, actuation) && started)
                            context.Canceled();
                    }
                    break;
            }
        }

        public void Reset()
        {
            m_WaitingForRelease = false;
        }
    }

    /// <summary>
    /// Determines how to trigger an action based on button presses.
    /// </summary>
    /// <seealso cref="PressInteraction.behavior"/>
    public enum PressBehavior
    {
        /// <summary>
        /// Perform the action when the button is pressed.
        /// </summary>
        /// <remarks>
        /// Triggers <see cref="InputAction.performed"/> when a control crosses the button press threshold.
        /// </remarks>
        // ReSharper disable once UnusedMember.Global
        PressOnly = 0,

        /// <summary>
        /// Perform the action when the button is released.
        /// </summary>
        /// <remarks>
        /// Triggers <see cref="InputAction.started"/> when a control crosses the button press threshold and
        /// <see cref="InputAction.performed"/> when the control goes back below the button press threshold.
        /// </remarks>
        // ReSharper disable once UnusedMember.Global
        ReleaseOnly = 1,

        /// <summary>
        /// Perform the action when the button is pressed and when the button is released.
        /// </summary>
        /// <remarks>
        /// Triggers <see cref="InputAction.performed"/> when a control crosses the button press threshold
        /// and triggers <see cref="InputAction.performed"/> again when it goes back below the button press
        /// threshold.
        /// </remarks>
        // ReSharper disable once UnusedMember.Global
        PressAndRelease = 2,
    }

    #if UNITY_EDITOR
    /// <summary>
    /// UI that is displayed when editing <see cref="PressInteraction"/> in the editor.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    internal class PressInteractionEditor : InputParameterEditor<PressInteraction>
    {
        protected override void OnEnable()
        {
            m_PressPointSetting.Initialize("Press Point",
                "The amount of actuation a control requires before being considered pressed. If not set, default to "
                + "'Default Button Press Point' in the global input settings.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v,
                () => InputSystem.settings.defaultButtonPressPoint);
        }

        public override void OnGUI()
        {
            EditorGUILayout.HelpBox(s_HelpBoxText);
            target.behavior = (PressBehavior)EditorGUILayout.EnumPopup(s_PressBehaviorLabel, target.behavior);
            m_PressPointSetting.OnGUI();
        }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            root.Add(new HelpBox(s_HelpBoxText.text, HelpBoxMessageType.None));

            var behaviourDropdown = new EnumField(s_PressBehaviorLabel.text, target.behavior)
            {
                tooltip = s_PressBehaviorLabel.tooltip
            };
            behaviourDropdown.RegisterValueChangedCallback(evt =>
            {
                target.behavior = (PressBehavior)evt.newValue;
                onChangedCallback?.Invoke();
            });
            root.Add(behaviourDropdown);

            m_PressPointSetting.OnDrawVisualElements(root, onChangedCallback);
        }

#endif

        private CustomOrDefaultSetting m_PressPointSetting;

        private static readonly GUIContent s_HelpBoxText = EditorGUIUtility.TrTextContent("Note that the 'Press' interaction is only "
            + "necessary when wanting to customize button press behavior. For default press behavior, simply set the action type to 'Button' "
            + "and use the action without interactions added to it.");

        private static readonly GUIContent s_PressBehaviorLabel = EditorGUIUtility.TrTextContent("Trigger Behavior",
            "Determines how button presses trigger the action. By default (PressOnly), the action is performed on press. "
            + "With ReleaseOnly, the action is performed on release. With PressAndRelease, the action is performed on press and "
            + "canceled on release.");
    }
    #endif
}
