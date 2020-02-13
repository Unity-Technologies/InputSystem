using System;
using UnityEngine.InputSystem.LowLevel;

////REVIEW: should timer expiration be a separate method on IInputInteraction?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Information passed to <see cref="IInputInteraction">interactions</see>
    /// when their associated controls trigger.
    /// </summary>
    /// <seealso cref="IInputInteraction.Process"/>
    public struct InputInteractionContext
    {
        /// <summary>
        /// The action associated with the binding.
        /// </summary>
        /// <remarks>
        /// If the binding is not associated with an action, this is <c>null</c>.
        /// </remarks>
        /// <seealso cref="InputBinding.action"/>
        public InputAction action => m_State.GetActionOrNull(ref m_TriggerState);

        /// <summary>
        /// The bound control that changed its state to trigger the binding associated
        /// with the interaction.
        /// </summary>
        /// <remarks>
        /// In case the binding associated with the interaction is a composite, this is
        /// one of the controls that are part of the composite.
        /// </remarks>
        /// <seealso cref="InputBinding.path"/>
        public InputControl control => m_State.GetControl(ref m_TriggerState);

        public InputActionPhase phase => m_TriggerState.phase;

        /// <summary>
        /// Time stamp of the input event that caused <see cref="control"/> to trigger a change in the
        /// state of <see cref="action"/>.
        /// </summary>
        /// <seealso cref="InputEvent.time"/>
        public double time => m_TriggerState.time;

        public double startTime => m_TriggerState.startTime;

        public bool timerHasExpired
        {
            get => (m_Flags & Flags.TimerHasExpired) != 0;
            internal set
            {
                if (value)
                    m_Flags |= Flags.TimerHasExpired;
                else
                    m_Flags &= ~Flags.TimerHasExpired;
            }
        }

        /// <summary>
        /// True if the interaction is waiting for input
        /// </summary>
        /// <remarks>
        /// By default, an interaction will return this this phase after every time it has been performed
        /// (<see cref="InputActionPhase.Performed"/>). This can be changed by using <see cref="PerformedAndStayStarted"/>
        /// or <see cref="PerformedAndStayPerformed"/>.
        /// </remarks>
        /// <seealso cref="InputActionPhase.Waiting"/>
        public bool isWaiting => phase == InputActionPhase.Waiting;

        /// <summary>
        /// True if the interaction has been started.
        /// </summary>
        /// <seealso cref="InputActionPhase.Started"/>
        /// <seealso cref="Started"/>
        public bool isStarted => phase == InputActionPhase.Started;

        /// <summary>
        /// Return true if the control that triggered the interaction has been actuated beyond the given threshold.
        /// </summary>
        /// <param name="threshold">Threshold that must be reached for the control to be considered actuated. If this is zero,
        /// the threshold must be exceeded. If it is any positive value, the value must be at least matched.</param>
        /// <returns>True if the trigger control is actuated.</returns>
        /// <seealso cref="InputControlExtensions.IsActuated"/>
        public bool ControlIsActuated(float threshold = 0)
        {
            return m_State.IsActuated(ref m_TriggerState, threshold);
        }

        /// <summary>
        /// Mark the interaction has having begun.
        /// </summary>
        /// <remarks>
        /// Note that this affects the current interaction only. There may be multiple interactions on a binding
        /// and arbitrary many interactions may concurrently be in started state. However, only one interaction
        /// (usually the one that starts first) is allowed to drive the action's state as a whole. If an interaction
        /// that is currently driving an action is canceled, however, the next interaction in the list that has
        /// been started will take over and continue driving the action.
        ///
        /// <example>
        /// <code>
        /// public class MyInteraction : IInputInteraction&lt;float&gt;
        /// {
        ///     public void Process(ref IInputInteractionContext context)
        ///     {
        ///         if (context.isWaiting && context.ControlIsActuated())
        ///         {
        ///             // We've waited for input and got it. Start the interaction.
        ///             context.Started();
        ///         }
        ///         else if (context.isStarted && !context.ControlIsActuated())
        ///         {
        ///             // Interaction has been completed.
        ///             context.Performed();
        ///         }
        ///     }
        ///
        ///     public void Reset()
        ///     {
        ///         // No reset code needed. We're not keeping any state locally in the interaction.
        ///     }
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        public void Started()
        {
            m_TriggerState.startTime = time;
            m_State.ChangePhaseOfInteraction(InputActionPhase.Started, ref m_TriggerState);
        }

        public void Performed()
        {
            if (m_TriggerState.phase == InputActionPhase.Waiting)
                m_TriggerState.startTime = time;
            m_State.ChangePhaseOfInteraction(InputActionPhase.Performed, ref m_TriggerState);
        }

        public void PerformedAndStayStarted()
        {
            if (m_TriggerState.phase == InputActionPhase.Waiting)
                m_TriggerState.startTime = time;
            m_State.ChangePhaseOfInteraction(InputActionPhase.Performed, ref m_TriggerState,
                phaseAfterPerformed: InputActionPhase.Started);
        }

        public void PerformedAndStayPerformed()
        {
            if (m_TriggerState.phase == InputActionPhase.Waiting)
                m_TriggerState.startTime = time;
            m_State.ChangePhaseOfInteraction(InputActionPhase.Performed, ref m_TriggerState,
                phaseAfterPerformed: InputActionPhase.Performed);
        }

        public void Canceled()
        {
            if (m_TriggerState.phase != InputActionPhase.Canceled)
                m_State.ChangePhaseOfInteraction(InputActionPhase.Canceled, ref m_TriggerState);
        }

        public void Waiting()
        {
            if (m_TriggerState.phase != InputActionPhase.Waiting)
                m_State.ChangePhaseOfInteraction(InputActionPhase.Waiting, ref m_TriggerState);
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

        internal InputActionState m_State;
        internal Flags m_Flags;
        internal InputActionState.TriggerState m_TriggerState;

        internal int mapIndex => m_TriggerState.mapIndex;

        internal int controlIndex => m_TriggerState.controlIndex;

        internal int bindingIndex => m_TriggerState.bindingIndex;

        internal int interactionIndex => m_TriggerState.interactionIndex;

        [Flags]
        internal enum Flags
        {
            TimerHasExpired = 1 << 1
        }
    }
}
