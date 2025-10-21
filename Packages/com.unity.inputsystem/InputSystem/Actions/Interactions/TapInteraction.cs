using System;
using System.ComponentModel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
#endif

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// Performs the action if the control is pressed held for at least the set
    /// duration (which defaults to <see cref="InputSettings.defaultTapTime"/>)
    /// and then released.
    /// </summary>
    [DisplayName("Tap")]
    public class TapInteraction : IInputInteraction
    {
        ////REVIEW: this should be called tapTime
        /// <summary>
        /// The time in seconds within which the control needs to be pressed and released to perform the interaction.
        /// </summary>
        /// <remarks>
        /// If this value is equal to or smaller than zero, the input system will use (<see cref="InputSettings.defaultTapTime"/>) instead.
        /// </remarks>
        public float duration;

        /// <summary>
        /// The press point required to perform the interaction.
        /// </summary>
        /// <remarks>
        /// For analog controls (such as trigger axes on a gamepad), the control needs to be engaged by at least this
        /// value to perform the interaction.
        /// If this value is equal to or smaller than zero, the input system will use (<see cref="InputSettings.defaultButtonPressPoint"/>) instead.
        /// </remarks>
        public float pressPoint;

        private float durationOrDefault => duration > 0.0 ? duration : InputSystem.settings.defaultTapTime;
        private float pressPointOrDefault => pressPoint > 0 ? pressPoint : ButtonControl.s_GlobalDefaultButtonPressPoint;
        private float releasePointOrDefault => pressPointOrDefault * ButtonControl.s_GlobalDefaultButtonReleaseThreshold;

        private double m_TapStartTime;
        bool canceledFromTimerExpired;

        ////TODO: make sure 2d doesn't move too far

        public void Process(ref InputInteractionContext context)
        {
            if (context.timerHasExpired)
            {
                context.Canceled();
                // Cache the fact that we canceled the interaction due to a timer expiration.
                canceledFromTimerExpired = true;
                return;
            }

            // Check if the control is actuated but avoid starting the interaction if it was canceled due to a timeout.
            // Otherwise, we would start the interaction again immediately after it is canceled due to timeout,
            // particularly in analog controls such as Gamepad stick or triggers. (ISXB-627)
            if (context.isWaiting && context.ControlIsActuated(pressPointOrDefault) && !canceledFromTimerExpired)
            {
                m_TapStartTime = context.time;
                // Set timeout slightly after duration so that if tap comes in exactly at the expiration
                // time, it still counts as a valid tap.
                context.Started();
                context.SetTimeout(durationOrDefault + 0.00001f);
                return;
            }

            if (context.isStarted && !context.ControlIsActuated(releasePointOrDefault))
            {
                if (context.time - m_TapStartTime <= durationOrDefault)
                {
                    context.Performed();
                }
                else
                {
                    ////REVIEW: does it matter to cancel right after expiration of 'duration' or is it enough to cancel on button up like here?
                    context.Canceled();
                }
            }

            // Once the control is released, we allow the interaction to be started again.
            if (!context.ControlIsActuated(releasePointOrDefault))
            {
                canceledFromTimerExpired = false;
            }
        }

        public void Reset()
        {
            m_TapStartTime = 0;
        }
    }

    #if UNITY_EDITOR
    internal class TapInteractionEditor : InputParameterEditor<TapInteraction>
    {
        protected override void OnEnable()
        {
            m_DurationSetting.Initialize("Max Tap Duration",
                "Time (in seconds) within with a control has to be released again for it to register as a tap. If the control is held "
                + "for longer than this time, the tap is canceled.",
                "Default Tap Time",
                () => target.duration, x => target.duration = x, () => InputSystem.settings.defaultTapTime);
            m_PressPointSetting.Initialize("Press Point",
                "The amount of actuation a control requires before being considered pressed. If not set, default to "
                + "'Default Button Press Point' in the global input settings.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v,
                () => InputSystem.settings.defaultButtonPressPoint);
        }

        public override void OnGUI()
        {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            if (!InputSystem.settings.useIMGUIEditorForAssets) return;
#endif
            m_DurationSetting.OnGUI();
            m_PressPointSetting.OnGUI();
        }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            m_DurationSetting.OnDrawVisualElements(root, onChangedCallback);
            m_PressPointSetting.OnDrawVisualElements(root, onChangedCallback);
        }

#endif

        private CustomOrDefaultSetting m_DurationSetting;
        private CustomOrDefaultSetting m_PressPointSetting;
    }
    #endif
}
