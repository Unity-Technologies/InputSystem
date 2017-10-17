using System;
using UnityEngine;

////TODO: explore UnityEvents as an option to hook up action responses right in the inspector

namespace ISX
{
    ////REVIEW: omit InputControl fro the callback and have users use InputAction.lastSource?
    using ActionListener = Action<InputAction, InputControl>;

    // A named input signal that can flexibly decide which input data to tap.
    // Unlike controls, actions signal value *changes* rather than the values themselves.
    // They sit on top of controls (and each single action may reference several controls
    // collectively) and monitor the system for change.
    //
    // NOTE: Unlike InputControls, InputActions are not passive! They will actively perform
    //       processing each frame they are active whereas InputControls just sit there as
    //       long as no one is asking them directly for a value.
    //
    // NOTE: Processors on controls are *NOT* taken into account by actions. A state is
    //       considered changed if its underlying memory changes not if the final processed
    //       value changes.
    //
    // NOTE: Actions are agnostic to update types. They trigger in whatever update detects
    //       a change in value.
    [Serializable]
    public class InputAction
        ////REVIEW: should this class be IDisposable? how do we guarantee that actions are disabled in time?
    {
        public enum Phase
        {
            Disabled,
            Waiting,
            Started,
            Performed,
            Cancelled
        }

        public string name => m_Name;

        public Phase phase => m_CurrentPhase;

        public InputActionSet actionSet => isSingletonAction ? null : m_ActionSet;

        ////TODO: add hasBeenPerformedThisFrame which compares a m_LastPerformedFrame to current frame counter (same for hasBeenStarted)

        ////TODO: add support for turning binding array into displayable info
        ////      (allow to constrain by sets of devics set on action set)

        public ReadOnlyArray<InputBinding> bindings => new ReadOnlyArray<InputBinding>(m_Bindings, m_BindingsStartIndex, m_BindingsCount);

        ////REVIEW: is this useful? control lists are per-binding, this munges them all together
        // The set of controls to which the bindings resolve. May change over time.
        public ReadOnlyArray<InputControl> controls
        {
            get
            {
                if (!m_Enabled)
                    throw new InvalidOperationException("Cannot list controls of action when the action is not enabled.");
                return m_Controls;
            }
        }

        ////REVIEW: this would technically allow a GetValue<TValue>() method
        public InputControl lastSource => m_LastSource;

        public bool enabled => m_Enabled;

        ////REVIEW: have single delegate that just gives you an InputAction and you get the control and phase from the action?

        ////REVIEW: pass Context or Context-like struct to action listeners?

        public event ActionListener started
        {
            add { m_OnStarted.Append(value); }
            remove { m_OnStarted.Remove(value); }
        }

        public event ActionListener cancelled
        {
            add { m_OnCancelled.Append(value); }
            remove { m_OnCancelled.Remove(value); }
        }

        // Listeners that are called when the action has been fully performed.
        // Passes along the control that triggered the state change and the action
        // object iself as well.
        public event ActionListener performed
        {
            add { m_OnPerformed.Append(value); }
            remove { m_OnPerformed.Remove(value); }
        }

        // Constructor we use for serialization and for actions that are part
        // of sets.
        internal InputAction()
        {
        }

        ////REVIEW: single modifier?
        // Construct a disabled action targeting the given sources.
        public InputAction(string name = null, string binding = null, string modifiers = null)
        {
            if (binding == null && modifiers != null)
                throw new ArgumentException("Cannot have modifier without binding", nameof(modifiers));

            m_Name = name;
            if (binding != null)
            {
                m_Bindings = new[] {new InputBinding {path = binding, modifiers = modifiers}};
                m_BindingsStartIndex = 0;
                m_BindingsCount = 1;
            }
            m_CurrentPhase = Phase.Disabled;
        }

        public void Enable()
        {
            if (m_Enabled)
                return;

            if (m_ActionSet == null)
                CreateInternalActionSetForSingletonAction();

            if (m_ActionSet.m_Controls == null)
                m_ActionSet.ResolveBindings();

            // Let set know we're changing state.
            m_ActionSet.TellAboutActionChangingEnabledStatus(this, true);

            // Hook up state monitors for all our controls.
            var manager = InputSystem.s_Manager;
            for (var i = 0; i < m_ResolvedBindings.Count; ++i)
            {
                var controls = m_ResolvedBindings[i].controls;
                for (var n = 0; n < controls.Count; ++n)
                    manager.AddStateChangeMonitor(controls[n], this, i);
            }

            // Done.
            m_Enabled = true;
            m_CurrentPhase = Phase.Waiting;
        }

        public void Disable()
        {
            ////TODO: remove state change monitors and action timeouts
            throw new NotImplementedException();
        }

        ////REVIEW: what if this is called after the action has been enabled? throw?
        public AddBindingSyntax AddBinding(string path, string modifiers = null)
        {
            if (isSingletonAction)
            {
                // Simple case. We're a singleton action and own m_Bindings.
                var index = ArrayHelpers.Append(ref m_Bindings, new InputBinding {path = path, modifiers = modifiers});
                ++m_BindingsCount;
                return new AddBindingSyntax(this, index);
            }
            else
            {
                // Less straightfoward case. We're part of an m_Bindings set owned
                // by our m_ActionSet.
                throw new NotImplementedException();
            }
        }

        [SerializeField] private string m_Name;

        // This should be a ReadOnlyArray<InputBinding> but we can't serialize that because
        // Unity can't serialize generic types. So we explode the structure here and turn
        // it into a ReadOnlyArray whenever needed.
        [SerializeField] internal InputBinding[] m_Bindings;
        [SerializeField][HideInInspector] internal int m_BindingsStartIndex;
        [SerializeField][HideInInspector] internal int m_BindingsCount;

        // The action set that owns us.
        [NonSerialized] internal InputActionSet m_ActionSet;

        // Listeners. No array allocations if only a single listener.
        [NonSerialized] private InlinedArray<ActionListener> m_OnStarted;
        [NonSerialized] private InlinedArray<ActionListener> m_OnCancelled;
        [NonSerialized] private InlinedArray<ActionListener> m_OnPerformed;

        // State we keep for enabling/disabling. This is volatile and not put on disk.
        [NonSerialized] internal bool m_Enabled;
        [NonSerialized] private Phase m_CurrentPhase;
        [NonSerialized] private InputControl m_LastSource;
        [NonSerialized] internal ReadOnlyArray<InputControl> m_Controls;
        [NonSerialized] internal ReadOnlyArray<InputActionSet.ResolvedBinding> m_ResolvedBindings;

        private bool isSingletonAction => m_ActionSet == null || ReferenceEquals(m_ActionSet.m_SingletonAction, this);

        private void CreateInternalActionSetForSingletonAction()
        {
            m_ActionSet = new InputActionSet {m_SingletonAction = this};
        }

        private void GoToPhase(Phase newPhase, InputControl triggerControl)
        {
            m_CurrentPhase = newPhase;
            switch (newPhase)
            {
                case Phase.Started:
                    CallListeners(ref m_OnStarted, triggerControl);
                    m_LastSource = triggerControl;
                    break;

                ////TODO: need to cancel pending timers
                case Phase.Performed:
                    CallListeners(ref m_OnPerformed, triggerControl);
                    m_LastSource = triggerControl;
                    m_CurrentPhase = Phase.Waiting;
                    break;

                case Phase.Cancelled:
                    CallListeners(ref m_OnCancelled, triggerControl);
                    m_LastSource = triggerControl;
                    m_CurrentPhase = Phase.Waiting;
                    break;
            }
        }

        private void CallListeners(ref InlinedArray<ActionListener> listeners, InputControl triggerControl)
        {
            if (listeners.firstValue == null)
                return;

            listeners.firstValue(this, triggerControl);
            if (listeners.additionalValues != null)
            {
                for (var i = 0; i < listeners.additionalValues.Length; ++i)
                    listeners.additionalValues[i](this, triggerControl);
            }
        }

        // Called from InputManager when one of our state change monitors has fired.
        // Tells us the time of the change *according to the state events coming in*.
        // Also tells us which control of the controls we are binding to triggered the
        // change and relays the binding index we gave it when we called AddStateChangeMonitor.
        internal void NotifyControlValueChanged(InputControl control, int bindingIndex, double time)
        {
            var isAtDefault = control.CheckStateIsAllZeroes();

            // If we have modifiers, let them do all the processing. The precense of a modifier
            // essentially bypasses the default phase progression logic of an action.
            var modifiers = m_ResolvedBindings[bindingIndex].modifiers;
            if (modifiers.Count > 0)
            {
                Context context;
                context.m_Action = this;
                context.m_TriggerControl = control;
                context.m_Time = time;
                context.m_ControlIsAtDefaultValue = isAtDefault;
                context.m_TimerHasExpired = false;

                for (var i = 0; i < modifiers.Count; ++i)
                {
                    var modifier = modifiers[i];
                    context.m_CurrentModifier = modifier;

                    modifier.Process(ref context);
                }
            }
            else
            {
                ////TODO: default should be to go to performed on press but only go back to waiting on release
                // Default logic has no support for cancellations and won't ever go into started
                // phase. Will go from waiting straight to performed and then straight to waiting
                // again.
                if (phase == Phase.Waiting && !isAtDefault)
                    GoToPhase(Phase.Performed, control);
            }
        }

        internal void NotifyTimerExpired(IInputActionModifier modifier, double time)
        {
            Context context;

            context.m_Action = this;
            context.m_TriggerControl = null;
            context.m_CurrentModifier = modifier;
            context.m_Time = time;
            context.m_ControlIsAtDefaultValue = false; ////REVIEW: how should this be handled?
            context.m_TimerHasExpired = true;

            modifier.Process(ref context);
        }

        // Data we pass to modifiers during processing. Encapsulates all the context
        // they have access to and allows us to extend that functionality without
        // changing the IInputActionModifier interface.
        public struct Context
        {
            // These are all set by NotifyControlValueChanged.
            internal InputAction m_Action;
            internal InputControl m_TriggerControl;
            internal double m_Time;
            internal IInputActionModifier m_CurrentModifier;
            internal bool m_ControlIsAtDefaultValue;
            internal bool m_TimerHasExpired;

            public InputAction action => m_Action;
            public InputControl control => m_TriggerControl;
            public Phase phase => action.phase;
            public double time => m_Time;
            public bool controlHasDefaultValue => m_ControlIsAtDefaultValue;
            public bool timerHasExpired => m_TimerHasExpired;

            public bool isWaiting => phase == Phase.Waiting;
            public bool isStarted => phase == Phase.Started;

            ////TODO: when a modifier initiates a phase shift, remember the modifier and if another modifier wants to
            ////      phase shift, either reset the original modifier or prevent the phase shift

            public void Started()
            {
                m_Action.GoToPhase(Phase.Started, m_TriggerControl);
            }

            public void Performed()
            {
                m_Action.GoToPhase(Phase.Performed, m_TriggerControl);
            }

            public void Cancelled()
            {
                m_Action.GoToPhase(Phase.Cancelled, m_TriggerControl);
            }

            public void SetTimeout(double seconds)
            {
                var manager = InputSystem.s_Manager;
                manager.AddActionTimeout(m_Action, Time.time + seconds, m_CurrentModifier);
            }
        }

        public struct AddBindingSyntax
        {
            public InputAction action;
            private int m_BindingIndex;

            internal AddBindingSyntax(InputAction action, int bindingIndex)
            {
                this.action = action;
                m_BindingIndex = bindingIndex;
            }

            public AddBindingSyntax CombinedWith()
            {
                throw new NotImplementedException();
            }
        }
    }
}
