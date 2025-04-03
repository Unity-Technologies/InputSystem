using System;
using UnityEngine.InputSystem.LowLevel;

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

        /// <summary>
        /// The phase the interaction is currently in.
        /// </summary>
        /// <remarks>
        /// Each interaction on a binding has its own phase independent of the action the binding is applied to.
        /// If an interaction gets to "drive" an action at a particular point in time, its phase will determine
        /// the phase of the action.
        /// </remarks>
        /// <seealso cref="InputAction.phase"/>
        /// <seealso cref="Started"/>
        /// <seealso cref="Waiting"/>
        /// <seealso cref="Performed"/>
        /// <seealso cref="Canceled"/>
        public InputActionPhase phase => m_TriggerState.phase;

        /// <summary>
        /// Time stamp of the input event that caused <see cref="control"/> to trigger a change in the
        /// state of <see cref="action"/>.
        /// </summary>
        /// <seealso cref="InputEvent.time"/>
        public double time => m_TriggerState.time;

        /// <summary>
        /// Timestamp of the <see cref="InputEvent"/> that caused the interaction to transition
        /// to <see cref="InputActionPhase.Started"/>.
        /// </summary>
        /// <seealso cref="InputEvent.time"/>
        public double startTime => m_TriggerState.startTime;

        /// <summary>
        /// Whether the interaction's <see cref="IInputInteraction.Process"/> method has been called because
        /// a timer set by <see cref="SetTimeout"/> has expired.
        /// </summary>
        /// <seealso cref="SetTimeout"/>
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
        /// Compute the current level of control actuation.
        /// </summary>
        /// <returns>The current level of control actuation (usually [0..1]) or -1 if the control is actuated
        /// but does not support computing magnitudes.</returns>
        /// <seealso cref="ControlIsActuated"/>
        /// <seealso cref="InputControl.EvaluateMagnitude()"/>
        public float ComputeMagnitude()
        {
            return m_TriggerState.magnitude;
        }

        /// <summary>
        /// Return true if the control that triggered the interaction has been actuated beyond the given threshold.
        /// </summary>
        /// <param name="threshold">Threshold that must be reached for the control to be considered actuated. If this is zero,
        /// the threshold must be exceeded. If it is any positive value, the value must be at least matched.</param>
        /// <returns>True if the trigger control is actuated.</returns>
        /// <seealso cref="InputControlExtensions.IsActuated"/>
        /// <seealso cref="ComputeMagnitude"/>
        public bool ControlIsActuated(float threshold = 0)
        {
            return InputActionState.IsActuated(ref m_TriggerState, threshold);
        }

        /// <summary>
        /// Mark the interaction has having begun.
        /// </summary>
        /// <remarks>
        /// This affects the current interaction only. There might be multiple interactions on a binding
        /// and arbitrary many interactions might concurrently be in started state. However, only one interaction
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
        ///         if (context.isWaiting &amp;&amp; context.ControlIsActuated())
        ///         {
        ///             // We've waited for input and got it. Start the interaction.
        ///             context.Started();
        ///         }
        ///         else if (context.isStarted &amp;&amp; !context.ControlIsActuated())
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

        /// <summary>
        /// Marks the interaction as being performed and then transitions back to <see cref="InputActionPhase.Waiting"/>
        /// to wait for input. This behavior is desirable for interaction events that are instant and reflect
        /// a transitional interaction pattern such as <see cref="Interactions.PressInteraction"/> or <see cref="Interactions.TapInteraction"/>.
        /// </summary>
        /// <remarks>
        /// Note that this affects the current interaction only. There might be multiple interactions on a binding
        /// and arbitrary many interactions might concurrently be in started state. However, only one interaction
        /// (usually the one that starts first) is allowed to drive the action's state as a whole. If an interaction
        /// that is currently driving an action is canceled, however, the next interaction in the list that has
        /// been started will take over and continue driving the action.
        /// </remarks>
        public void Performed()
        {
            if (m_TriggerState.phase == InputActionPhase.Waiting)
                m_TriggerState.startTime = time;
            m_State.ChangePhaseOfInteraction(InputActionPhase.Performed, ref m_TriggerState);
        }

        /// <summary>
        /// Marks the interaction as being performed and then transitions into I <see cref="InputActionPhase.Started"/>
        /// to wait for an initial trigger condition to be true before being performed again. This behavior
        /// may be desirable for interaction events that reflect transitional interaction patterns but should
        /// be considered as started until a cancellation condition is true, such as releasing a button.
        /// </summary>
        public void PerformedAndStayStarted()
        {
            if (m_TriggerState.phase == InputActionPhase.Waiting)
                m_TriggerState.startTime = time;
            m_State.ChangePhaseOfInteraction(InputActionPhase.Performed, ref m_TriggerState,
                phaseAfterPerformed: InputActionPhase.Started);
        }

        /// <summary>
        /// Marks the interaction as being performed and then stays in that state waiting for an input to
        /// cancel the interactions active state. This behavior is desirable for interaction events that
        /// are active for a duration until a cancellation condition is true, such as <see cref="Interactions.HoldInteraction"/> or <see cref="Interactions.TapInteraction"/> where releasing
        /// the associated button cancels the interaction..
        /// </summary>
        public void PerformedAndStayPerformed()
        {
            if (m_TriggerState.phase == InputActionPhase.Waiting)
                m_TriggerState.startTime = time;
            m_State.ChangePhaseOfInteraction(InputActionPhase.Performed, ref m_TriggerState,
                phaseAfterPerformed: InputActionPhase.Performed);
        }

        /// <summary>
        ///  Marks the interaction as being interrupted or aborted. This is relevant to signal that the interaction
        ///  pattern was not completed, for example, the user pressed and then released a button before the minimum
        ///  time required for a <see cref="Interactions.HoldInteraction"/> to complete.
        /// </summary>
        /// <remarks>
        /// This is used by most existing interactions to cancel the transitions in the interaction state machine
        /// when a condition required to proceed turned false or other indirect requirements were not met, such as
        /// time-based conditions.
        /// </remarks>
        public void Canceled()
        {
            if (m_TriggerState.phase != InputActionPhase.Canceled)
                m_State.ChangePhaseOfInteraction(InputActionPhase.Canceled, ref m_TriggerState);
        }

        /// <summary>
        /// Put the interaction back into <see cref="InputActionPhase.Waiting"/> state.
        /// </summary>
        /// <seealso cref="InputAction.phase"/>
        /// <seealso cref="InputActionPhase"/>
        /// <seealso cref="Started"/>
        /// <seealso cref="Performed"/>
        /// <seealso cref="Canceled"/>
        public void Waiting()
        {
            if (m_TriggerState.phase != InputActionPhase.Waiting)
                m_State.ChangePhaseOfInteraction(InputActionPhase.Waiting, ref m_TriggerState);
        }

        /// <summary>
        /// Start a timeout that triggers within <paramref name="seconds"/>.
        /// </summary>
        /// <param name="seconds">Number of seconds before the timeout is triggered.</param>
        /// <remarks>
        /// An interaction might wait a set amount of time for something to happen and then
        /// do something depending on whether it did or did not happen. By calling this method,
        /// a timeout is installed such that in the input update that the timer expires in, the
        /// interaction's <see cref="IInputInteraction.Process"/> method is called with <see cref="timerHasExpired"/>
        /// being true.
        ///
        /// Changing the phase of the interaction while a timeout is running will implicitly cancel
        /// the timeout.
        ///
        /// <example>
        /// <code>
        /// // Let's say we're writing a Process() method for an interaction that,
        /// // after a control has been actuated, waits for 1 second for it to be
        /// // released again. If that happens, the interaction performs. If not,
        /// // it cancels.
        /// public void Process(ref InputInteractionContext context)
        /// {
        ///     // timerHasExpired will be true if we get called when our timeout
        ///     // has expired.
        ///     if (context.timerHasExpired)
        ///     {
        ///         // The user did not release the control quickly enough.
        ///         // Our interaction is not successful, so cancel.
        ///         context.Canceled();
        ///         return;
        ///     }
        ///
        ///     if (context.ControlIsActuated())
        ///     {
        ///         if (!context.isStarted)
        ///         {
        ///             // The control has been actuated. We want to give the user a max
        ///             // of 1 second to release it. So we start the interaction now and then
        ///             // set the timeout.
        ///             context.Started();
        ///             context.SetTimeout(1);
        ///         }
        ///     }
        ///     else
        ///     {
        ///         // Control has been released. If we're currently waiting for a release,
        ///         // it has come in time before out timeout expired. In other words, the
        ///         // interaction has been successfully performed. We call Performed()
        ///         // which implicitly removes our ongoing timeout.
        ///         if (context.isStarted)
        ///             context.Performed();
        ///     }
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="timerHasExpired"/>
        public void SetTimeout(float seconds)
        {
            m_State.StartTimeout(seconds, ref m_TriggerState);
        }

        /// <summary>
        /// Override the default timeout value used by <see cref="InputAction.GetTimeoutCompletionPercentage"/>.
        /// </summary>
        /// <param name="seconds">Amount of total successive timeouts TODO</param>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// By default, timeout completion will be entirely determine by the timeout that is currently
        /// running, if any. However, some interactions (such as <see cref="Interactions.MultiTapInteraction"/>)
        /// will have to run multiple timeouts in succession. Thus, completion of a single timeout is not
        /// the same as completion of the interaction.
        ///
        /// You can use this method to account for this.
        ///
        /// Whenever a timeout completes, the timeout duration will automatically be accumulated towards
        /// the total timeout completion time.
        ///
        /// <example>
        /// <code>
        /// // Let's say we're starting our first timeout and we know that we will run three timeouts
        /// // in succession of 2 seconds each. By calling SetTotalTimeoutCompletionTime(), we can account for this.
        /// SetTotalTimeoutCompletionTime(3 * 2);
        ///
        /// // Start the first timeout. When this timeout expires, it will automatically
        /// // count one second towards the total timeout completion time.
        /// SetTimeout(2);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputAction.GetTimeoutCompletionPercentage"/>
        public void SetTotalTimeoutCompletionTime(float seconds)
        {
            if (seconds <= 0)
                throw new ArgumentException("Seconds must be a positive value", nameof(seconds));

            m_State.SetTotalTimeoutCompletionTime(seconds, ref m_TriggerState);
        }

        /// <summary>
        /// Read the value of the binding that triggered processing of the interaction.
        /// </summary>
        /// <typeparam name="TValue">Type of value to read from the binding. Must match the value type of the control
        /// or composite in effect for the binding.</typeparam>
        /// <returns>Value read from the binding.</returns>
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
