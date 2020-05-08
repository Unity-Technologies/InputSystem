using System.ComponentModel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

////REVIEW: this is confusing when considered next to HoldInteraction

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// Performs the action if the control is pressed held for at least the set
    /// duration (which defaults to <see cref="InputSettings.defaultSlowTapTime"/>)
    /// and then released.
    /// </summary>
    [Preserve]
    [DisplayName("Long Tap")]
    public class SlowTapInteraction : IInputInteraction
    {
        /// <summary>
        /// The time in seconds within which the control needs to be pressed and released to perform the interaction.
        /// </summary>
        /// <remarks>
        /// If this value is equal to or smaller than zero, the input system will use (<see cref="InputSettings.defaultSlowTapTime"/>) instead.
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

        ////REVIEW: this seems stupid; shouldn't a slow tap just be anything that takes longer than TapTime?
        private float durationOrDefault => duration > 0.0f ? duration : InputSystem.settings.defaultSlowTapTime;
        private float pressPointOrDefault => pressPoint > 0 ? pressPoint : ButtonControl.s_GlobalDefaultButtonPressPoint;

        private double m_SlowTapStartTime;

        public void Process(ref InputInteractionContext context)
        {
            if (context.isWaiting && context.ControlIsActuated(pressPointOrDefault))
            {
                m_SlowTapStartTime = context.time;
                context.Started();
                return;
            }

            if (context.isStarted && !context.ControlIsActuated(pressPointOrDefault))
            {
                if (context.time - m_SlowTapStartTime >= durationOrDefault)
                    context.Performed();
                else
                    ////REVIEW: does it matter to cancel right after expiration of 'duration' or is it enough to cancel on button up like here?
                    context.Canceled();
            }
        }

        public void Reset()
        {
            m_SlowTapStartTime = 0.0;
        }
    }

    #if UNITY_EDITOR
    internal class SlowTapInteractionEditor : InputParameterEditor<SlowTapInteraction>
    {
        protected override void OnEnable()
        {
            m_DurationSetting.Initialize("Min Tap Duration",
                "Minimum time (in seconds) that a control has to be held for it to register as a slow tap. If the control is released "
                + "before this time, the slow tap is canceled.",
                "Default Slow Tap Time",
                () => target.duration, x => target.duration = x, () => InputSystem.settings.defaultSlowTapTime);
            m_PressPointSetting.Initialize("Press Point",
                "The amount of actuation a control requires before being considered pressed. If not set, default to "
                + "'Default Button Press Point' in the global input settings.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v,
                () => InputSystem.settings.defaultButtonPressPoint);
        }

        public override void OnGUI()
        {
            m_DurationSetting.OnGUI();
            m_PressPointSetting.OnGUI();
        }

        private CustomOrDefaultSetting m_DurationSetting;
        private CustomOrDefaultSetting m_PressPointSetting;
    }
    #endif
}
