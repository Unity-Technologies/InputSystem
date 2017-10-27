using System;
using UnityEngine;

////TODO: explore UnityEvents as an option to hook up action responses right in the inspector

////TODO: survive domain reloads

namespace ISX
{
    ////REVIEW: I'd like to pass the context as ref but that leads to ugliness on the lambdas
    public delegate void InputActionListener(InputAction.CallbackContext context);

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
        public InputControl lastSource => m_TriggerControl;

        public bool enabled => m_Enabled;

        ////REVIEW: have single delegate that just gives you an InputAction and you get the control and phase from the action?

        public event InputActionListener started
        {
            add { m_OnStarted.Append(value); }
            remove { m_OnStarted.Remove(value); }
        }

        public event InputActionListener cancelled
        {
            add { m_OnCancelled.Append(value); }
            remove { m_OnCancelled.Remove(value); }
        }

        // Listeners that are called when the action has been fully performed.
        // Passes along the control that triggered the state change and the action
        // object iself as well.
        public event InputActionListener performed
        {
            add { m_OnPerformed.Append(value); }
            remove { m_OnPerformed.Remove(value); }
        }

        // Constructor we use for serialization and for actions that are part
        // of sets.
        internal InputAction()
        {
        }

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
                //need to make sure that change monitors of combined bindings are in the right order
                var controls = m_ResolvedBindings[i].controls;
                for (var n = 0; n < controls.Count; ++n)
                    manager.AddStateChangeMonitor(controls[n], this, i);
            }

            // Done.
            m_Enabled = true;
            m_CurrentPhase = Phase.Waiting;
            m_TriggerBindingIndex = -1;
            m_TriggerModifierIndex = -1;
            m_TriggerControl = null;
        }

        public void Disable()
        {
            if (!m_Enabled)
                return;

            // Let set know.
            m_ActionSet.TellAboutActionChangingEnabledStatus(this, false);

            // Delete state change monitors.
            var manager = InputSystem.s_Manager;
            for (var i = 0; i < m_ResolvedBindings.Count; ++i)
            {
                var controls = m_ResolvedBindings[i].controls;
                for (var n = 0; n < controls.Count; ++n)
                    manager.RemoveStateChangeMonitor(controls[n], this);
            }

            m_Enabled = false;
            m_CurrentPhase = Phase.Disabled;
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
        // NOTE: When we are part of an action set, the set will null out m_Bindings for
        //       serialization.
        [SerializeField] internal InputBinding[] m_Bindings;
        [SerializeField][HideInInspector] internal int m_BindingsStartIndex;
        [SerializeField][HideInInspector] internal int m_BindingsCount;

        // The action set that owns us.
        [NonSerialized] internal InputActionSet m_ActionSet;

        // Listeners. No array allocations if only a single listener.
        [NonSerialized] private InlinedArray<InputActionListener> m_OnStarted;
        [NonSerialized] private InlinedArray<InputActionListener> m_OnCancelled;
        [NonSerialized] private InlinedArray<InputActionListener> m_OnPerformed;

        // State we keep for enabling/disabling. This is volatile and not put on disk.
        [NonSerialized] internal bool m_Enabled;
        [NonSerialized] private Phase m_CurrentPhase;
        [NonSerialized] private InputControl m_TriggerControl;
        [NonSerialized] private int m_TriggerBindingIndex;
        [NonSerialized] private int m_TriggerModifierIndex;
        [NonSerialized] internal ReadOnlyArray<InputControl> m_Controls;
        [NonSerialized] internal ReadOnlyArray<InputActionSet.ResolvedBinding> m_ResolvedBindings;

        private bool isSingletonAction => m_ActionSet == null || ReferenceEquals(m_ActionSet.m_SingletonAction, this);

        private void CreateInternalActionSetForSingletonAction()
        {
            m_ActionSet = new InputActionSet {m_SingletonAction = this};
        }

        // Perform a phase change on the action. Visible to observers.
        private void ChangePhaseOfAction(Phase newPhase, InputControl triggerControl, int triggerBindingIndex, int triggerModifierIndex)
        {
            ThrowIfPhaseTransitionIsInvalid(m_CurrentPhase, newPhase, triggerBindingIndex, triggerModifierIndex);

            m_CurrentPhase = newPhase;
            m_TriggerControl = triggerControl;
            m_TriggerBindingIndex = triggerBindingIndex;
            m_TriggerModifierIndex = triggerModifierIndex;

            switch (newPhase)
            {
                case Phase.Started:
                    CallListeners(ref m_OnStarted);
                    break;

                case Phase.Performed:
                    CallListeners(ref m_OnPerformed);
                    m_CurrentPhase = Phase.Waiting; // Go back to waiting after performing action.
                    break;

                case Phase.Cancelled:
                    CallListeners(ref m_OnCancelled);
                    m_CurrentPhase = Phase.Waiting; // Go back to waiting after cancelling action.
                    break;
            }

            if (m_CurrentPhase == Phase.Waiting)
            {
                m_TriggerControl = null;
                m_TriggerBindingIndex = -1;
                m_TriggerModifierIndex = -1;
            }
        }

        // Perform a phase change on the given modifier. Only visible to observers
        // if it happens to change the phase of the action, too.
        //
        // Multiple modifiers can be started concurrently but the first modifier that
        // starts will get to drive the action until it either cancels or performs the
        // action.
        //
        // If a modifier driving an action performs it, all modifiers will reset and
        // go back waiting.
        //
        // If a modifier driving an action cancels it, the next modifier in the list which
        // has already started will get to drive the action (example: a TapModifier and a
        // SlowTapModifier both start and the TapModifier gets to drive the action because
        // it comes first; then the TapModifier cancels because the button is held for too
        // long and the SlowTapModifier will get to drive the action next).
        private void ChangePhaseOfModifier(Phase newPhase, InputControl triggerControl, int triggerBindingIndex,
            int triggerModifierIndex)
        {
            Debug.Assert(triggerBindingIndex != -1);
            Debug.Assert(triggerModifierIndex != -1);

            ////TODO: need to somehow make sure that performed and cancelled phase changes happen on the *same* binding&control
            ////      as the start of the phase

            var modifiersForBinding = m_ResolvedBindings[triggerBindingIndex].modifiers;
            var currentModifierState = modifiersForBinding[triggerModifierIndex];
            var newModifierState = currentModifierState;

            // Update modifier state.
            ThrowIfPhaseTransitionIsInvalid(currentModifierState.phase, newPhase, triggerBindingIndex, triggerModifierIndex);
            newModifierState.phase = newPhase;
            newModifierState.control = triggerControl;
            modifiersForBinding[triggerModifierIndex] = newModifierState;

            // See if it affects the phase of the action itself.
            if (m_CurrentPhase == Phase.Waiting)
            {
                // We're the first modifier to go to the start phase.
                ChangePhaseOfAction(newPhase, triggerControl, triggerBindingIndex, triggerModifierIndex);
            }
            else if (newPhase == Phase.Cancelled && m_TriggerModifierIndex == triggerModifierIndex)
            {
                // We're cancelling but maybe there's another modifier ready
                // to go into start phase.

                ChangePhaseOfAction(newPhase, triggerControl, triggerBindingIndex, triggerModifierIndex);

                for (var i = 0; i < modifiersForBinding.Count; ++i)
                    if (i != triggerModifierIndex && modifiersForBinding[i].phase == Phase.Started)
                    {
                        ChangePhaseOfAction(Phase.Started, modifiersForBinding[i].control, triggerBindingIndex, i);
                        break;
                    }
            }
            else if (m_TriggerModifierIndex == triggerModifierIndex)
            {
                // Any other phase change goes to action if we're the modifier driving
                // the current phase.
                ChangePhaseOfAction(newPhase, triggerControl, triggerBindingIndex, triggerModifierIndex);

                // We're the modifier driving the action and we performed the action,
                // so reset any other modifier to waiting state.
                if (newPhase == Phase.Performed)
                {
                    for (var i = 0; i < modifiersForBinding.Count; ++i)
                        if (i != triggerBindingIndex)
                            ResetModifier(triggerBindingIndex, i);
                }
            }

            // If the modifier performed or cancelled, go back to waiting.
            if (newPhase == Phase.Performed || newPhase == Phase.Cancelled)
                ResetModifier(triggerBindingIndex, triggerModifierIndex);
        }

        // Notify observers that we have changed state.
        private void CallListeners(ref InlinedArray<InputActionListener> listeners)
        {
            // Should always have a control that triggered the state change.
            Debug.Assert(m_TriggerControl != null);

            if (listeners.firstValue == null)
                return;

            IInputActionModifier modifier = null;
            if (m_TriggerModifierIndex != -1)
                modifier = m_ResolvedBindings[m_TriggerBindingIndex].modifiers[m_TriggerModifierIndex].modifier;

            // We store the relevant state directly on the context instead of looking it
            // up lazily on the action to shield the context from value changes. This prevents
            // surprises on the caller side (e.g. in tests).
            var context = new CallbackContext
            {
                m_Action = this,
                m_Control = m_TriggerControl,
                m_Modifier = modifier
            };

            listeners.firstValue(context);
            if (listeners.additionalValues != null)
            {
                for (var i = 0; i < listeners.additionalValues.Length; ++i)
                    listeners.additionalValues[i](context);
            }
        }

        private void ThrowIfPhaseTransitionIsInvalid(Phase currentPhase, Phase newPhase, int bindingIndex, int modifierIndex)
        {
            if (newPhase == Phase.Started && currentPhase != Phase.Waiting)
                throw new InvalidOperationException(
                    $"Cannot go from '{m_CurrentPhase}' to '{Phase.Started}'; must be '{Phase.Waiting}' (action: {this}, modifier: {GetModifier(bindingIndex, modifierIndex)})");
            if (newPhase == Phase.Performed && currentPhase != Phase.Waiting && currentPhase != Phase.Started)
                throw new InvalidOperationException(
                    $"Cannot go from '{m_CurrentPhase}' to '{Phase.Performed}'; must be '{Phase.Waiting}' or '{Phase.Started}' (action: {this}, modifier: {GetModifier(bindingIndex, modifierIndex)})");
            if (newPhase == Phase.Cancelled && currentPhase != Phase.Started)
                throw new InvalidOperationException(
                    $"Cannot go from '{m_CurrentPhase}' to '{Phase.Cancelled}'; must be '{Phase.Started}' (action: {this}, modifier: {GetModifier(bindingIndex, modifierIndex)})");
        }

        private IInputActionModifier GetModifier(int bindingIndex, int modifierIndex)
        {
            if (bindingIndex == -1)
                return null;

            return m_ResolvedBindings[bindingIndex].modifiers[modifierIndex].modifier;
        }

        private void ResetModifier(int bindingIndex, int modifierIndex)
        {
            var modifiersForBinding = m_ResolvedBindings[bindingIndex].modifiers;
            var oldState = modifiersForBinding[modifierIndex];

            oldState.modifier.Reset();
            if (oldState.isTimerRunning)
            {
                var manager = InputSystem.s_Manager;
                manager.RemoveActionTimeout(this, bindingIndex, modifierIndex);
            }

            modifiersForBinding[modifierIndex] =
                new InputActionSet.ModifierState
            {
                modifier = oldState.modifier,
                phase = Phase.Waiting
            };
        }

        // Called from InputManager when one of our state change monitors has fired.
        // Tells us the time of the change *according to the state events coming in*.
        // Also tells us which control of the controls we are binding to triggered the
        // change and relays the binding index we gave it when we called AddStateChangeMonitor.
        internal void NotifyControlValueChanged(InputControl control, int bindingIndex, double time)
        {
            var isAtDefault = control.CheckStateIsAllZeros();

            // If we have modifiers, let them do all the processing. The precense of a modifier
            // essentially bypasses the default phase progression logic of an action.
            var modifiers = m_ResolvedBindings[bindingIndex].modifiers;
            if (modifiers.Count > 0)
            {
                ModifierContext context;

                context.m_Action = this;
                context.m_Control = control;
                context.m_Time = time;
                context.m_ControlIsAtDefaultValue = isAtDefault;
                context.m_TimerHasExpired = false;
                context.m_BindingIndex = bindingIndex;

                for (var i = 0; i < modifiers.Count; ++i)
                {
                    var state = modifiers[i];
                    var modifier = state.modifier;

                    context.m_Phase = state.phase;
                    context.m_ModifierIndex = i;

                    modifier.Process(ref context);
                }
            }
            else
            {
                // Default logic has no support for cancellations and won't ever go into started
                // phase. Will go from waiting straight to performed and then straight to waiting
                // again.
                if (phase == Phase.Waiting && !isAtDefault)
                {
                    ChangePhaseOfAction(Phase.Performed, control, -1, -1);
                }
            }
        }

        internal void NotifyTimerExpired(int bindingIndex, int modifierIndex, double time)
        {
            ModifierContext context;

            var modifiersForBinding = m_ResolvedBindings[bindingIndex].modifiers;
            var modifierState = modifiersForBinding[modifierIndex];

            context.m_Action = this;
            context.m_Time = time;
            context.m_ControlIsAtDefaultValue = false; ////REVIEW: how should this be handled?
            context.m_TimerHasExpired = true;
            context.m_BindingIndex = bindingIndex;
            context.m_ModifierIndex = modifierIndex;
            context.m_Phase = modifierState.phase;
            context.m_Control = modifierState.control;

            modifierState.isTimerRunning = false;
            modifiersForBinding[modifierIndex] = modifierState;

            modifierState.modifier.Process(ref context);
        }

        // Data we pass to modifiers during processing. Encapsulates all the context
        // they have access to and allows us to extend that functionality without
        // changing the IInputActionModifier interface.
        public struct ModifierContext
        {
            // These are all set by NotifyControlValueChanged.
            internal InputAction m_Action;
            internal Phase m_Phase;
            internal InputControl m_Control;
            internal double m_Time;
            internal bool m_ControlIsAtDefaultValue;
            internal bool m_TimerHasExpired;
            internal int m_BindingIndex;
            internal int m_ModifierIndex;

            public InputAction action => m_Action;
            public InputControl control => m_Control;
            public Phase phase => m_Phase;
            public double time => m_Time;
            public bool controlHasDefaultValue => m_ControlIsAtDefaultValue;
            public bool timerHasExpired => m_TimerHasExpired;

            public bool isWaiting => phase == Phase.Waiting;
            public bool isStarted => phase == Phase.Started;

            public void Started()
            {
                m_Action.ChangePhaseOfModifier(Phase.Started, m_Control, m_BindingIndex, m_ModifierIndex);
            }

            public void Performed()
            {
                m_Action.ChangePhaseOfModifier(Phase.Performed, m_Control, m_BindingIndex, m_ModifierIndex);
            }

            public void Cancelled()
            {
                m_Action.ChangePhaseOfModifier(Phase.Cancelled, m_Control, m_BindingIndex, m_ModifierIndex);
            }

            public void SetTimeout(double seconds)
            {
                var modifiersForBinding = m_Action.m_ResolvedBindings[m_BindingIndex].modifiers;
                var modifierState = modifiersForBinding[m_ModifierIndex];
                if (modifierState.isTimerRunning)
                    throw new NotImplementedException("cancel current timer");

                var manager = InputSystem.s_Manager;
                manager.AddActionTimeout(m_Action, Time.time + seconds, m_BindingIndex, m_ModifierIndex);

                modifierState.isTimerRunning = true;
                modifiersForBinding[m_ModifierIndex] = modifierState;
            }
        }

        public struct CallbackContext
        {
            internal InputAction m_Action;
            internal InputControl m_Control;
            internal IInputActionModifier m_Modifier;

            public InputAction action => m_Action;
            public InputControl control => m_Control;
            public IInputActionModifier modifier => m_Modifier;

            public TValue GetValue<TValue>()
            {
                return ((InputControl<TValue>)control).value;
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

            public AddBindingSyntax CombinedWith(string binding, string modifiers = null)
            {
                if (action.m_BindingsCount - 1 != m_BindingIndex)
                    throw new InvalidOperationException("Must not add other bindings in-between calling AddBindings() and CombinedWith()");

                var result = action.AddBinding(binding, modifiers);
                action.m_Bindings[action.m_BindingsStartIndex + result.m_BindingIndex].flags |=
                    InputBinding.Flags.ThisAndPreviousCombine;

                return result;
            }
        }
    }
}
