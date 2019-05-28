#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// Performs the action when a control is actuated past the button press point and then does not perform
    /// again until the button is first released and then pressed again.
    /// </summary>
    /// <remarks>
    /// The exact trigger behavior can be determined via <see cref="behavior"/>.
    ///
    /// Note that this interaction is not restricted to controls of type <c>float</c>. Actuation is measured
    /// through magnitudes (<see cref="InputControl.EvaluateMagnitude()"/> and will thus work with any control
    /// that can return a magnitude of actuation.
    /// </remarks>
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

        [Tooltip("Determines how button presses trigger the action. By default (PressOnly), the action is performed on press. "
            + "With ReleaseOnly, the action is performed on release. With PressAndRelease, the action is performed on press and "
            + "canceled on release.")]
        public PressBehavior behavior;

        private float pressPointOrDefault => pressPoint > 0 ? pressPoint : InputSystem.settings.defaultButtonPressPoint;
        private bool m_WaitingForRelease;

        public void Process(ref InputInteractionContext context)
        {
            var isActuated = context.ControlIsActuated(pressPointOrDefault);

            switch (behavior)
            {
                case PressBehavior.PressOnly:
                    if (m_WaitingForRelease)
                    {
                        if (isActuated)
                        {
                            if (context.continuous)
                                context.PerformedAndStayPerformed();
                        }
                        else
                        {
                            m_WaitingForRelease = false;
                            // We need to reset the action to waiting state in order to stop it from triggering
                            // continuously. However, we do not want to cancel here as that will trigger the action.
                            // So go back directly to waiting here.
                            context.Waiting();
                        }
                    }
                    else if (isActuated)
                    {
                        ////REVIEW: should this trigger Started?
                        if (context.continuous)
                            context.PerformedAndStayPerformed();
                        else
                            context.PerformedAndGoBackToWaiting();

                        m_WaitingForRelease = true;
                    }
                    break;

                case PressBehavior.ReleaseOnly:
                    if (m_WaitingForRelease && !isActuated)
                    {
                        m_WaitingForRelease = false;
                        context.PerformedAndGoBackToWaiting();

                        // No support for continuous mode.
                    }
                    else if (isActuated)
                    {
                        context.Started();
                        m_WaitingForRelease = true;
                    }
                    break;

                case PressBehavior.PressAndRelease:
                    if (m_WaitingForRelease)
                    {
                        if (!isActuated)
                            context.PerformedAndGoBackToWaiting();
                        // No support for continuous mode.

                        m_WaitingForRelease = isActuated;
                    }
                    else if (isActuated)
                    {
                        context.PerformedAndGoBackToWaiting();
                        // No support for continuous mode.

                        m_WaitingForRelease = true;
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
        // ReSharper disable once UnusedMember.Global
        PressOnly = 0,

        /// <summary>
        /// Perform the action when the button is released.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        ReleaseOnly = 1,

        /// <summary>
        /// Perform the action when the button is pressed and cancel the action when the button is released.
        /// </summary>
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
            target.behavior = (PressBehavior)EditorGUILayout.EnumPopup(s_PressBehaviorLabel, target.behavior);
            m_PressPointSetting.OnGUI();
        }

        private CustomOrDefaultSetting m_PressPointSetting;

        private static readonly GUIContent s_PressBehaviorLabel = EditorGUIUtility.TrTextContent("Trigger Behavior",
            "Determines how button presses trigger the action. By default (PressOnly), the action is performed on press. "
            + "With ReleaseOnly, the action is performed on release. With PressAndRelease, the action is performed on press and "
            + "canceled on release.");
    }
    #endif
}
