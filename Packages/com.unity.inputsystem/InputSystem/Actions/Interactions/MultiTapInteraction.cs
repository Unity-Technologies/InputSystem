namespace UnityEngine.Experimental.Input.Interactions
{
    /// <summary>
    /// Interaction that requires multiple taps (press and release within <see cref="tapTime"/>) spaced no more
    /// than <see cref="tapDelay"/> seconds apart.
    /// </summary>
    /// <remarks>
    /// The interaction goes into <see cref="InputActionPhase.Started"/> on the first press and then will not
    /// trigger again until either the full tap sequence is performed (in which case the interaction triggers
    /// <see cref="InputActionPhase.Performed"/>) or the multi-tap is aborted by a timeout being hit (in which
    /// case the interaction will trigger <see cref="InputActionPhase.Cancelled"/>).
    /// </remarks>
    public class MultiTapInteraction : IInputInteraction<float>
    {
        [Tooltip("The maximum time (in seconds) allowed to elapse between pressing and releasing a control for it to register as a tap.")]
        public float tapTime;

        [Tooltip("The maximum delay (in seconds) allowed between each tap. If this time is exceeded, the multi-tap is cancelled.")]
        public float tapDelay;

        [Tooltip("How many taps need to be performed in succession. Two means double-tap, three means triple-tap, and so on.")]
        public int tapCount = 2;

        private float tapTimeOrDefault => tapTime > 0.0 ? tapTime : InputSystem.settings.defaultTapTime;
        private float tapDelayOrDefault => tapDelay > 0.0 ? tapDelay : tapTimeOrDefault * 2;

        public void Process(ref InputInteractionContext context)
        {
            if (context.timerHasExpired)
            {
                // We use timers multiple times but no matter what, if they expire it means
                // that we didn't get input in time.
                context.Cancelled();
                return;
            }

            switch (m_CurrentTapPhase)
            {
                case TapPhase.None:
                    if (context.ControlIsActuated())
                    {
                        m_CurrentTapPhase = TapPhase.WaitingForNextRelease;
                        m_CurrentTapStartTime = context.time;
                        context.Started();
                        context.SetTimeout(tapTimeOrDefault);
                    }
                    break;

                case TapPhase.WaitingForNextRelease:
                    if (!context.ControlIsActuated())
                    {
                        if (context.time - m_CurrentTapStartTime <= tapTimeOrDefault)
                        {
                            ++m_CurrentTapCount;
                            if (m_CurrentTapCount >= tapCount)
                            {
                                context.PerformedAndGoBackToWaiting();
                            }
                            else
                            {
                                m_CurrentTapPhase = TapPhase.WaitingForNextPress;
                                m_LastTapReleaseTime = context.time;
                                context.SetTimeout(tapDelayOrDefault);
                            }
                        }
                        else
                        {
                            context.Cancelled();
                        }
                    }
                    break;

                case TapPhase.WaitingForNextPress:
                    if (context.ControlIsActuated())
                    {
                        if (context.time - m_LastTapReleaseTime <= tapDelayOrDefault)
                        {
                            m_CurrentTapPhase = TapPhase.WaitingForNextRelease;
                            m_CurrentTapStartTime = context.time;
                            context.SetTimeout(tapTimeOrDefault);
                        }
                        else
                        {
                            context.Cancelled();
                        }
                    }
                    break;
            }
        }

        public void Reset()
        {
            m_CurrentTapPhase = TapPhase.None;
            m_CurrentTapCount = 0;
            m_CurrentTapStartTime = 0;
            m_LastTapReleaseTime = 0;
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
