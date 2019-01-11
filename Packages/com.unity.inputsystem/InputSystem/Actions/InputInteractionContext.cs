using System;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Information passed to <see cref="IInputInteraction">interactions</see>
    /// when their associated controls trigger.
    /// </summary>
    public struct InputInteractionContext
    {
        /// <summary>
        /// The action associated with the binding.
        /// </summary>
        /// <remarks>
        /// If the binding is not associated with an action, this is <c>null</c>.
        /// </remarks>
        public InputAction action => m_State.GetActionOrNull(ref m_TriggerState);

        /// <summary>
        /// The bound control that changed its state to trigger the binding associated
        /// with the interaction.
        /// </summary>
        public InputControl control => m_State.GetControl(ref m_TriggerState);

        public InputActionPhase phase => m_TriggerState.phase;

        public double time => m_TriggerState.time;

        public double startTime => m_TriggerState.startTime;

        ////REVIEW: how should this be handled for timer expired calls
        public bool controlHasDefaultValue
        {
            get
            {
                if ((m_Flags & Flags.ControlHasDefaultValueInitialized) != Flags.ControlHasDefaultValueInitialized)
                {
                    var triggerControl = control;
                    if (triggerControl.CheckStateIsAtDefault())
                        m_Flags |= Flags.ControlHasDefaultValue;
                    m_Flags |= Flags.ControlHasDefaultValueInitialized;
                }
                return (m_Flags & Flags.ControlHasDefaultValue) == Flags.ControlHasDefaultValue;
            }
        }

        public bool timerHasExpired
        {
            get => (m_Flags & Flags.TimerHasExpired) == Flags.TimerHasExpired;
            internal set
            {
                if (value)
                    m_Flags |= Flags.TimerHasExpired;
                else
                    m_Flags &= ~Flags.TimerHasExpired;
            }
        }

        public bool isWaiting => phase == InputActionPhase.Waiting;

        public bool isStarted => phase == InputActionPhase.Started;

        public void Started()
        {
            m_TriggerState.startTime = time;
            m_State.ChangePhaseOfInteraction(InputActionPhase.Started, ref m_TriggerState);
        }

        public void Performed()
        {
            m_State.ChangePhaseOfInteraction(InputActionPhase.Performed, ref m_TriggerState);
        }

        public void PerformedAndStayStarted()
        {
            m_State.ChangePhaseOfInteraction(InputActionPhase.Performed, ref m_TriggerState, remainStartedAfterPerformed: true);
        }

        public void Cancelled()
        {
            m_State.ChangePhaseOfInteraction(InputActionPhase.Cancelled, ref m_TriggerState);
        }

        public void SetTimeout(float seconds)
        {
            m_State.StartTimeout(seconds, ref m_TriggerState);
        }

        public TValue ReadValue<TValue>()
            where TValue : struct
        {
            return m_State.ReadValue<TValue>(m_TriggerState.bindingIndex, m_TriggerState.controlIndex);
        }

        internal InputActionMapState m_State;
        internal Flags m_Flags;
        internal InputActionMapState.TriggerState m_TriggerState;

        internal int mapIndex => m_TriggerState.mapIndex;

        internal int controlIndex => m_TriggerState.controlIndex;

        internal int bindingIndex => m_TriggerState.bindingIndex;

        internal int interactionIndex => m_TriggerState.interactionIndex;


        [Flags]
        internal enum Flags
        {
            ControlHasDefaultValue = 1 << 0,
            ControlHasDefaultValueInitialized = 1 << 1,
            TimerHasExpired = 1 << 2,
        }
    }
}
