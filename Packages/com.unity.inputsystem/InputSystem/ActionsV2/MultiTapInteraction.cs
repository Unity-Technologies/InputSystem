using System;
using UnityEngine.InputSystem.Controls;

namespace UnityEngine.InputSystem.ActionsV2
{
	public interface IInputInteraction<TValue, TControl> 
		where TValue : struct
		where TControl : struct
	{
		void ProcessInput(ref CallbackContext<TValue, TControl> callbackContext);
	}

    public class MultiTapInteraction : IInputInteraction<float, float>
    {
	    public event Action Tapped;

		/// <summary>
        /// The time in seconds within which the control needs to be pressed and released to perform the interaction.
        /// </summary>
        /// <remarks>
        /// If this value is equal to or smaller than zero, the input system will use (<see cref="InputSettings.defaultTapTime"/>) instead.
        /// </remarks>
        [Tooltip("The maximum time (in seconds) allowed to elapse between pressing and releasing a control for it to register as a tap.")]
        public float tapTime;

        /// <summary>
        /// The time in seconds which is allowed to pass between taps.
        /// </summary>
        /// <remarks>
        /// If this time is exceeded, the multi-tap interaction is canceled.
        /// If this value is equal to or smaller than zero, the input system will use the duplicate value of <see cref="tapTime"/> instead.
        /// </remarks>
        [Tooltip("The maximum delay (in seconds) allowed between each tap. If this time is exceeded, the multi-tap is canceled.")]
        public float tapDelay;

        /// <summary>
        /// The number of taps required to perform the interaction.
        /// </summary>
        /// <remarks>
        /// How many taps need to be performed in succession. Two means double-tap, three means triple-tap, and so on.
        /// </remarks>
        [Tooltip("How many taps need to be performed in succession. Two means double-tap, three means triple-tap, and so on.")]
        public int tapCount = 2;

        /// <summary>
        /// Magnitude threshold that must be crossed by an actuated control for the control to
        /// be considered pressed.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="InputSettings.defaultButtonPressPoint"/> is used instead.
        /// </remarks>
        /// <seealso cref="InputControl.EvaluateMagnitude()"/>
        public float pressPoint;

        private float tapTimeOrDefault => tapTime > 0.0 ? tapTime : InputSystem.settings.defaultTapTime;
        internal float tapDelayOrDefault => tapDelay > 0.0 ? tapDelay : InputSystem.settings.multiTapDelayTime;
        private float pressPointOrDefault => pressPoint > 0 ? pressPoint : ButtonControl.s_GlobalDefaultButtonPressPoint;
        private float releasePointOrDefault => pressPointOrDefault * ButtonControl.s_GlobalDefaultButtonReleaseThreshold;

        public MultiTapInteraction()
        {
	        // TODO: set up callback for update call so we can run timeouts and such things
        }

        public void Update(double time)
        {

        }

	    public void ProcessInput(ref CallbackContext<float, float> callbackContext)
	    {
		    var time = callbackContext.time;
		    var control = callbackContext.inputControl;

            switch (m_CurrentTapPhase)
            {
                case TapPhase.None:
                    if (control.IsActuated(pressPointOrDefault))
                    {
                        m_CurrentTapPhase = TapPhase.WaitingForNextRelease;
                        m_CurrentTapStartTime = time;

                        var maxTapTime = tapTimeOrDefault;
                        var maxDelayInBetween = tapDelayOrDefault;
                        
                        // TODO: timeout
                        // context.SetTimeout(maxTapTime);

                        // We'll be using multiple timeouts so set a total completion time that
                        // effects the result of InputAction.GetTimeoutCompletionPercentage()
                        // such that it accounts for the total time we allocate for the interaction
                        // rather than only the time of one single timeout.
                        // context.SetTotalTimeoutCompletionTime(maxTapTime * tapCount + (tapCount - 1) * maxDelayInBetween);
                    }
                    break;

                case TapPhase.WaitingForNextRelease:
                    if (!control.IsActuated(releasePointOrDefault))
                    {
                        if (time - m_CurrentTapStartTime <= tapTimeOrDefault)
                        {
                            ++m_CurrentTapCount;
                            if (m_CurrentTapCount >= tapCount)
                            {
                                Tapped?.Invoke();
                            }
                            else
                            {
                                m_CurrentTapPhase = TapPhase.WaitingForNextPress;
                                m_LastTapReleaseTime = time;
                                // context.SetTimeout(tapDelayOrDefault);
                            }
                        }
                    }
                    break;

                case TapPhase.WaitingForNextPress:
                    if (control.IsActuated(pressPointOrDefault))
                    {
                        if (time - m_LastTapReleaseTime <= tapDelayOrDefault)
                        {
                            m_CurrentTapPhase = TapPhase.WaitingForNextRelease;
                            m_CurrentTapStartTime = time;
                            // context.SetTimeout(tapTimeOrDefault);
                        }
                    }
                    break;
            }
	    }

	    private TapPhase m_CurrentTapPhase;
	    private int m_CurrentTapCount;
	    private double m_CurrentTapStartTime;
	    private double m_LastTapReleaseTime;

	    private enum TapPhase
	    {
		    None,
		    WaitingForNextRelease,
		    WaitingForNextPress,
	    }
    }
}
