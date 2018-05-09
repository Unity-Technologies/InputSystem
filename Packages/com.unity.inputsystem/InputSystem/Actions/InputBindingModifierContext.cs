using System;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Information passed to <see cref="IInputBindingModifier">binding modifiers</see>
    /// when their associated controls trigger.
    /// </summary>
    public struct InputBindingModifierContext
    {
        /// <summary>
        /// The action associated with the binding.
        /// </summary>
        /// <remarks>
        /// If the binding is not associated with an action, this is <c>null</c>.
        /// </remarks>
        public InputAction action
        {
            get
            {
                var actionIndex = m_ActionMap.m_State.bindingStates[bindingIndex].actionIndex;
                if (actionIndex == InputActionMapState.kInvalidIndex)
                    return null;
                return m_ActionMap.m_Actions[actionIndex];
            }
        }

        public InputControl control
        {
            get
            {
                var controlIndex = m_TriggerState.controlIndex;
                Debug.Assert(controlIndex != InputActionMapState.kInvalidIndex);
                return m_ActionMap.m_State.controls[controlIndex];
            }
        }

        public InputActionPhase phase
        {
            get { return m_TriggerState.phase; }
        }

        public double time
        {
            get { return m_TriggerState.time; }
        }

        public double startTime
        {
            get { return m_TriggerState.startTime; }
        }

        //how should this be handled for timer expired calls
        public bool controlHasDefaultValue
        {
            get
            {
                throw new NotImplementedException();
                return (m_Flags & Flags.ControlHasDefaultValue) == Flags.ControlHasDefaultValue;
            }
            internal set
            {
                if (value)
                    m_Flags |= Flags.ControlHasDefaultValue;
                else
                    m_Flags &= ~Flags.ControlHasDefaultValue;
            }
        }

        public bool timerHasExpired
        {
            get { return (m_Flags & Flags.TimerHasExpired) == Flags.TimerHasExpired; }
            internal set
            {
                if (value)
                    m_Flags |= Flags.TimerHasExpired;
                else
                    m_Flags &= ~Flags.TimerHasExpired;
            }
        }

        public bool isWaiting
        {
            get { return phase == InputActionPhase.Waiting; }
        }

        public bool isStarted
        {
            get { return phase == InputActionPhase.Started; }
        }

        public void Started()
        {
            m_TriggerState.startTime = time;
            m_ActionMap.m_State.ChangePhaseOfModifier(InputActionPhase.Started, ref m_TriggerState, m_ActionMap);
        }

        public void Performed()
        {
            m_ActionMap.m_State.ChangePhaseOfModifier(InputActionPhase.Performed, ref m_TriggerState, m_ActionMap);
        }

        public void Cancelled()
        {
            m_ActionMap.m_State.ChangePhaseOfModifier(InputActionPhase.Cancelled, ref m_TriggerState, m_ActionMap);
        }

        public void SetTimeout(float seconds)
        {
            m_ActionMap.AddStateChangeTimeout(controlIndex, bindingIndex, modifierIndex, seconds);
        }

        internal InputActionMap m_ActionMap;
        internal Flags m_Flags;
        internal InputActionMapState.TriggerState m_TriggerState;

        internal int controlIndex
        {
            get { return m_TriggerState.controlIndex; }
        }

        internal int bindingIndex
        {
            get { return m_TriggerState.bindingIndex; }
        }

        internal int modifierIndex
        {
            get { return m_TriggerState.modifierIndex; }
        }


        [Flags]
        internal enum Flags
        {
            ControlHasDefaultValue = 1 << 0,
            TimerHasExpired = 1 << 1,
        }
    }
}
