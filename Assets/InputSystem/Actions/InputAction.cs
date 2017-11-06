using System;
using System.Collections.Generic;
using UnityEngine;

////TODO: explore UnityEvents as an option to hook up action responses right in the inspector

////TODO: survive domain reloads

////REVIEW: callbacks may not be a good fit if we want to jobify the system; polling works better with that as we have a
////        clear point where to put syncs on fences

////TODO: allow individual bindings to be enabled/disabled

////TODO: allow querying controls *without* requiring actions to be enabled

////TODO: give every action in the system a stable unique ID; use this also to reference actions in InputActionReferences
////      (this mechanism will likely come in handy for giving jobs access to actions)

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
    //
    // NOTE: Actions are not supported in edit mode.
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

        ////TODO: add support for turning binding array into displayable info
        ////      (allow to constrain by sets of devics set on action set)

        public ReadOnlyArray<InputBinding> bindings
        {
            get
            {
                ////REVIEW: is there a better way to deal with the two different serializations? (singleton actions vs action sets)
                if (m_Bindings == null && m_ActionSet != null)
                    m_Bindings = m_ActionSet.m_Bindings;
                return new ReadOnlyArray<InputBinding>(m_Bindings, m_BindingsStartIndex, m_BindingsCount);
            }
        }

        ////REVIEW: is this useful? control lists are per-binding, this munges them all together
        // The set of controls to which the bindings resolve. May change over time.
        public ReadOnlyArray<InputControl> controls
        {
            get
            {
                ////REVIEW: just return an empty array?
                if (!enabled)
                    throw new InvalidOperationException("Cannot list controls of action when the action is not enabled.");
                return m_Controls;
            }
        }

        ////REVIEW: this would technically allow a GetValue<TValue>() method
        ////REVIEW: expose this as a struct?
        public InputControl lastTriggerControl => m_LastTrigger.control;
        public double lastTriggerTime => m_LastTrigger.time;
        //public InputBinding lastTriggerBinding
        //public IInputActionModifier lastTriggerModifier

        public bool enabled
        {
            get { return m_Enabled; }
            internal set { m_Enabled = value; }
        }

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

        public bool wasPerformed => triggeredInCurrentUpdate && m_LastTrigger.phase == Phase.Performed;
        public bool wasStarted => triggeredInCurrentUpdate && m_LastTrigger.phase == Phase.Started;
        public bool wasCancelled => triggeredInCurrentUpdate && m_LastTrigger.phase == Phase.Cancelled;

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
            if (enabled)
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
                ////TODO: need to make sure that change monitors of combined bindings are in the right order
                var controls = m_ResolvedBindings[i].controls;
                for (var n = 0; n < controls.Count; ++n)
                    manager.AddStateChangeMonitor(controls[n], this, i);
            }

            // Done.
            enabled = true;
            m_CurrentPhase = Phase.Waiting;
        }

        public void Disable()
        {
            if (!enabled)
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

            enabled = false;

            m_CurrentPhase = Phase.Disabled;
            m_LastTrigger = new TriggerState();
        }

        // Add a new binding to the action. This works both with action that are part of
        // action set as well as with actions that aren't.
        // Returns a fluent-style syntax structure that allows performing additional modifications
        // based on the new binding.
        // NOTE: Actions must be disabled while altering their binding sets.
        public AddBindingSyntax AddBinding(string path, string modifiers = null, string groups = null)
        {
            if (enabled)
                throw new InvalidOperationException($"Cannot add binding to action '{this}' while the action is enabled");

            var binding = new InputBinding {path = path, modifiers = modifiers, group = groups};

            var bindingIndex = 0;
            if (isSingletonAction)
            {
                // Simple case. We're a singleton action and own m_Bindings.
                bindingIndex = ArrayHelpers.Append(ref m_Bindings, binding);
            }
            else
            {
                // Less straightfoward case. We're part of an m_Bindings set owned
                // by our m_ActionSet.
                var set = m_ActionSet;
                var actions = set.m_Actions;
                var actionCount = actions.Length;

                if (m_BindingsCount == 0 || m_BindingsStartIndex + m_BindingsCount == set.m_Bindings.Length)
                {
                    // This is either our first binding or we're at the end of our set's binding array which makes
                    // it simpler. We just put our binding at the end of the set's bindings array.
                    if (set.m_Bindings != null)
                        bindingIndex = set.m_Bindings.Length;
                    if (m_BindingsCount == 0)
                        m_BindingsStartIndex = bindingIndex;
                    ArrayHelpers.Append(ref set.m_Bindings, binding);
                }
                else
                {
                    // More involved case where we are sitting somewhere within the set's bindings array
                    // and inserting new bindings will thus affect other actions in the set.
                    bindingIndex = m_BindingsStartIndex + m_BindingsCount;
                    ArrayHelpers.Insert(ref set.m_Bindings, bindingIndex, binding);

                    // Shift binding start indices of all actions that come after us up by one.
                    for (var i = 0; i < actionCount; ++i)
                    {
                        var action = actions[i];
                        if (action.m_BindingsStartIndex >= bindingIndex)
                            ++action.m_BindingsStartIndex;
                    }
                }

                // Update all bindings array references on all actions in the set.
                var bindingsArray = set.m_Bindings;
                for (var i = 0; i < actionCount; ++i)
                    actions[i].m_Bindings = bindingsArray;
            }

            ++m_BindingsCount;
            return new AddBindingSyntax(this, bindingIndex);
        }

        ////TODO: support for removing bindings

        public void ApplyBindingOverride(string binding, string group = null)
        {
            ApplyBindingOverride(new InputBindingOverride {binding = binding, group = group});
        }

        // Apply the given override to the action.
        //
        // NOTE: Ignores the action name in the override.
        // NOTE: Action must be disabled while applying overrides.
        // NOTE: If there's already an override on the respective binding, replaces the override.
        public void ApplyBindingOverride(InputBindingOverride bindingOverride)
        {
            if (enabled)
                throw new InvalidOperationException($"Cannot change overrides on action '{this}' while the action is enabled");

            if (bindingOverride.binding == string.Empty)
                bindingOverride.binding = null;

            var bindingIndex = FindBindingIndexForOverride(bindingOverride);
            if (bindingIndex == -1)
                return;

            m_Bindings[m_BindingsStartIndex + bindingIndex].overridePath = bindingOverride.binding;
        }

        public void RemoveBindingOverride(InputBindingOverride bindingOverride)
        {
            var undoBindingOverride = bindingOverride;
            undoBindingOverride.binding = null;

            // Simply apply but with a null binding.
            ApplyBindingOverride(undoBindingOverride);
        }

        // Restore all bindings to their default paths.
        public void RemoveAllBindingOverrides()
        {
            if (enabled)
                throw new InvalidOperationException($"Cannot removed overrides from action '{this}' while the action is enabled");

            for (var i = 0; i < m_BindingsCount; ++i)
                m_Bindings[m_BindingsStartIndex + i].overridePath = null;
        }

        // Add all overrides that have been applied to this action to the given list.
        // Returns the number of overrides found.
        public int GetBindingOverrides(List<InputBindingOverride> overrides)
        {
            throw new NotImplementedException();
        }

        [SerializeField] private string m_Name;

        // This should be a ReadOnlyArray<InputBinding> but we can't serialize that because
        // Unity can't serialize generic types. So we explode the structure here and turn
        // it into a ReadOnlyArray whenever needed.
        // NOTE: InputActionSet will null out this field for serialization
        [SerializeField] internal InputBinding[] m_Bindings;
        [SerializeField][HideInInspector] internal int m_BindingsStartIndex;
        [SerializeField][HideInInspector] internal int m_BindingsCount;

        [NonSerialized] private bool m_Enabled;

        // The action set that owns us.
        [NonSerialized] internal InputActionSet m_ActionSet;

        // Listeners. No array allocations if only a single listener.
        [NonSerialized] private InlinedArray<InputActionListener> m_OnStarted;
        [NonSerialized] private InlinedArray<InputActionListener> m_OnCancelled;
        [NonSerialized] private InlinedArray<InputActionListener> m_OnPerformed;

        // State we keep for enabling/disabling. This is volatile and not put on disk.
        // NOTE: m_Controls and m_ResolvedBinding array are stored on InputActionSet.
        [NonSerialized] internal ReadOnlyArray<InputControl> m_Controls;
        [NonSerialized] internal ReadOnlyArray<InputActionSet.ResolvedBinding> m_ResolvedBindings;

        // State releated to phase shifting and triggering of action.
        // Most of this state we lazily reset as we have to keep it available for
        // one frame but don't want to actively reset between frames.
        [NonSerialized] private Phase m_CurrentPhase;
        [NonSerialized] private TriggerState m_LastTrigger;

        // Information about what triggered an action and how.
        private struct TriggerState
        {
            public Phase phase;
            public double time;
            public InputControl control;
            public int bindingIndex;
            public int modifierIndex;
            public uint dynamicUpdateCount;
            public uint fixedUpdateCount;

            public TriggerState(Phase phase, double time, InputControl control, int bindingIndex, int modifierIndex)
            {
                this.phase = phase;
                this.time = time;
                this.control = control;
                this.bindingIndex = bindingIndex;
                this.modifierIndex = modifierIndex;

                ////REVIEW: move this logic into InputManager itself?
                var manager = InputSystem.s_Manager;
                if (manager.m_CurrentUpdate == InputUpdateType.Fixed)
                {
                    // We're in fixed update so for dynamic update, goes into upcoming one.
                    dynamicUpdateCount = manager.m_CurrentDynamicUpdateCount + 1;
                    fixedUpdateCount = manager.m_CurrentFixedUpdateCount;
                }
                else
                {
                    // We're in dynamic update so far fixed update, goes into upcoming one.
                    dynamicUpdateCount = manager.m_CurrentDynamicUpdateCount;
                    fixedUpdateCount = manager.m_CurrentFixedUpdateCount + 1;
                }
            }
        }

        private bool isSingletonAction => m_ActionSet == null || ReferenceEquals(m_ActionSet.m_SingletonAction, this);

        private bool triggeredInCurrentUpdate
        {
            get
            {
                var manager = InputSystem.s_Manager;
                var currentUpdate = manager.m_CurrentUpdate;

                if (currentUpdate == InputUpdateType.Dynamic)
                    return manager.m_CurrentDynamicUpdateCount == m_LastTrigger.dynamicUpdateCount;

                if (currentUpdate == InputUpdateType.Fixed)
                    return manager.m_CurrentFixedUpdateCount == m_LastTrigger.fixedUpdateCount;

                return false;
            }
        }

        private void CreateInternalActionSetForSingletonAction()
        {
            m_ActionSet = new InputActionSet {m_SingletonAction = this};
        }

        // Find the binding tha tthe given override addresses.
        // Return -1 if no corresponding binding is found.
        private int FindBindingIndexForOverride(InputBindingOverride bindingOverride)
        {
            var group = bindingOverride.group;
            var haveGroup = !string.IsNullOrEmpty(group);

            if (m_BindingsCount == 1)
            {
                // Simple case where we have only a single binding on the action.

                if (!haveGroup ||
                    string.Compare(m_Bindings[m_BindingsStartIndex].group, group,
                        StringComparison.InvariantCultureIgnoreCase) == 0)
                    return 0;
            }
            else if (m_BindingsCount > 1)
            {
                // Trickier case where we need to select from a set of bindings.

                if (!haveGroup)
                    // Group is required to disambiguate.
                    throw new InvalidOperationException(
                        $"Action {this} has multiple bindings; overriding binding requires the use of binding groups so the action knows which binding to override. Set 'group' property on InputBindingOverride.");

                int groupStringLength;
                var indexInGroup = bindingOverride.GetIndexInGroup(out groupStringLength);
                var currentIndexInGroup = 0;

                for (var i = 0; i < m_BindingsCount; ++i)
                    if (string.Compare(m_Bindings[m_BindingsStartIndex + i].group, 0, group, 0, groupStringLength, true) == 0)
                    {
                        if (currentIndexInGroup == indexInGroup)
                            return i;

                        ++currentIndexInGroup;
                    }
            }

            return -1;
        }

        // Perform a phase change on the action. Visible to observers.
        private void ChangePhaseOfAction(Phase newPhase, InputControl triggerControl, int triggerBindingIndex, int triggerModifierIndex, double triggerTime)
        {
            ThrowIfPhaseTransitionIsInvalid(m_CurrentPhase, newPhase, triggerBindingIndex, triggerModifierIndex);

            m_CurrentPhase = newPhase;

            // Capture state of the phase change.
            m_LastTrigger = new TriggerState(newPhase, triggerTime, triggerControl, triggerBindingIndex, triggerModifierIndex);

            // Let listeners know.
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
            int triggerModifierIndex, double triggerTime)
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
            if (newPhase == Phase.Started)
                newModifierState.startTime = triggerTime;
            modifiersForBinding[triggerModifierIndex] = newModifierState;

            // See if it affects the phase of the action itself.
            if (m_CurrentPhase == Phase.Waiting)
            {
                // We're the first modifier to go to the start phase.
                ChangePhaseOfAction(newPhase, triggerControl, triggerBindingIndex, triggerModifierIndex, triggerTime);
            }
            else if (newPhase == Phase.Cancelled && m_LastTrigger.modifierIndex == triggerModifierIndex)
            {
                // We're cancelling but maybe there's another modifier ready
                // to go into start phase.

                ChangePhaseOfAction(newPhase, triggerControl, triggerBindingIndex, triggerModifierIndex, triggerTime);

                for (var i = 0; i < modifiersForBinding.Count; ++i)
                    if (i != triggerModifierIndex && modifiersForBinding[i].phase == Phase.Started)
                    {
                        ChangePhaseOfAction(Phase.Started, modifiersForBinding[i].control, triggerBindingIndex, i,
                            triggerTime);
                        break;
                    }
            }
            else if (m_LastTrigger.modifierIndex == triggerModifierIndex)
            {
                // Any other phase change goes to action if we're the modifier driving
                // the current phase.
                ChangePhaseOfAction(newPhase, triggerControl, triggerBindingIndex, triggerModifierIndex, triggerTime);

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
            Debug.Assert(m_LastTrigger.control != null);

            if (listeners.firstValue == null)
                return;

            IInputActionModifier modifier = null;
            var startTime = 0.0;

            if (m_LastTrigger.modifierIndex != -1)
            {
                var modifierState = m_ResolvedBindings[m_LastTrigger.bindingIndex].modifiers[m_LastTrigger.modifierIndex];
                modifier = modifierState.modifier;
                startTime = modifierState.startTime;
            }

            // We store the relevant state directly on the context instead of looking it
            // up lazily on the action to shield the context from value changes. This prevents
            // surprises on the caller side (e.g. in tests).
            var context = new CallbackContext
            {
                m_Action = this,
                m_Control = m_LastTrigger.control,
                m_Time = m_LastTrigger.time,
                m_Modifier = modifier,
                m_StartTime = startTime
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
                    ChangePhaseOfAction(Phase.Performed, control, -1, -1, time);
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
                m_Action.ChangePhaseOfModifier(Phase.Started, m_Control, m_BindingIndex, m_ModifierIndex, m_Time);
            }

            public void Performed()
            {
                m_Action.ChangePhaseOfModifier(Phase.Performed, m_Control, m_BindingIndex, m_ModifierIndex, m_Time);
            }

            public void Cancelled()
            {
                m_Action.ChangePhaseOfModifier(Phase.Cancelled, m_Control, m_BindingIndex, m_ModifierIndex, m_Time);
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
            internal double m_Time;
            internal double m_StartTime;

            public InputAction action => m_Action;
            public InputControl control => m_Control;
            public IInputActionModifier modifier => m_Modifier;

            public TValue GetValue<TValue>()
            {
                return ((InputControl<TValue>)control).value;
            }

            public double time => m_Time;
            public double startTime => m_StartTime;
            public double duration => m_Time - m_StartTime;
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

            public AddBindingSyntax CombinedWith(string binding, string modifiers = null, string group = null)
            {
                if (action.m_BindingsCount - 1 != m_BindingIndex)
                    throw new InvalidOperationException("Must not add other bindings in-between calling AddBindings() and CombinedWith()");

                var result = action.AddBinding(binding, modifiers: modifiers, groups: group);
                action.m_Bindings[action.m_BindingsStartIndex + result.m_BindingIndex].flags |=
                    InputBinding.Flags.ThisAndPreviousCombine;

                return result;
            }
        }
    }
}
