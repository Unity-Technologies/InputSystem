using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Profiling;

////TODO: explore UnityEvents as an option to hook up action responses right in the inspector

////TODO: survive domain reloads

////TODO: allow individual bindings to be enabled/disabled

////TODO: allow querying controls *without* requiring actions to be enabled

////TODO: give every action in the system a stable unique ID; use this also to reference actions in InputActionReferences
////      (this mechanism will likely come in handy for giving jobs access to actions)

////TODO: event-based processing of input actions

////TODO: do not hardcode the transition from performed->waiting; allow an action to be performed over and over again inside
////      a single start cycle

// So, actions are set up to not have a contract. They just monitor state changes and then fire
// in response to those.
//
// However, as a user, this is only half the story I'm interested in. Yeah, I want to monitor
// state changes but I also want to control what values come in as a result.
//
// Actions don't carry values themselves. As such they don't have a value type. As a user, however,
// in by far most of the cases, I will think of an action as giving me a specific type of value.
// A "move" action, for example, is likely top represent a 2D planar motion vector. It can come from
// a gamepad thumbstick, from pointer deltas, or from a combination of keyboard keys (usually WASD).
// So the "move" action already has an aspect about it that's very much on my mind as a user but which
// is not represented anywhere in the action itself.
//
// There are probably cases where I want an action to be "polymorphic" but those I think are far and
// few between.
//
// Right now, actions just have a flat list of bindings. This works sufficiently well for bindings that
// are going to controls that already generate values that both match the expected value as well as
// the expected value *characteristics* (even with the right value type, if the value ranges and change
// rates are not what's expected, binding to a control may have undesired behavior).
//
// When bindings are supposed to work in unison (as with WASD, for example), a flat list of bindings
// is insufficient. A WASD setup is four distinct bindings that together form a single value. Also, even
// when bindings are independent, to properly work across devices of different types, it is often necessary
// to apply custom processing to values coming in through one binding and not to values coming in through
// a different binding.
//
// It is possible to offload all this responsibility to the code running in action callbacks but I think
// this will make for a very hard to use system at best. The promise of actions is that they abstract away
// from the types of devices being used. If actions are to live up to that promise, they need to be able
// to handle the above cases internally in their processing.

namespace UnityEngine.Experimental.Input
{
    ////REVIEW: I'd like to pass the context as ref but that leads to ugliness on the lambdas
    public delegate void InputActionListener(InputAction.CallbackContext context);

    /// <summary>
    /// A named input signal that can flexibly decide which input data to tap.
    /// </summary>
    /// <remarks>
    /// Unlike controls, actions signal value changes rather than the values themselves.
    /// They sit on top of controls (and each single action may reference several controls
    /// collectively) and monitor the system for change.
    ///
    /// Unlike InputControls, InputActions are not passive. They will actively perform
    /// processing each frame they are active whereas InputControls just sit there as
    /// long as no one is asking them directly for a value.
    ///
    /// Processors on controls are *NOT* taken into account by actions. A state is
    /// considered changed if its underlying memory changes not if the final processed
    /// value changes.
    ///
    /// Actions are agnostic to update types. They trigger in whatever update detects
    /// a change in value.
    ///
    /// Actions are not supported in edit mode.
    /// </remarks>
    [Serializable]
    public class InputAction : ICloneable
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

        /// <summary>
        /// Name of the action.
        /// </summary>
        /// <remarks>
        /// Can be null for anonymous actions created in code.
        ///
        /// If the action is part of a set, it will have a name and the name
        /// will be unique in the set.
        ///
        /// The name is just the name of the action alone, not a "setName/actionName"
        /// combination.
        /// </remarks>
        public string name
        {
            get { return m_Name; }
        }

        public Phase phase
        {
            get { return m_CurrentPhase; }
        }

        /// <summary>
        /// The set the action belongs to.
        /// </summary>
        /// <remarks>
        /// If the action is a lose action created in code, this will be null.
        /// </remarks>
        public InputActionSet set
        {
            get { return isSingletonAction ? null : m_ActionSet; }
        }

        ////TODO: add support for turning binding array into displayable info
        ////      (allow to constrain by sets of devics set on action set)

        /// <summary>
        /// The list of bindings associated with the action.
        /// </summary>
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

        ////REVIEW: expose this as a struct?
        public InputControl lastTriggerControl
        {
            get { return m_LastTrigger.control; }
        }

        public double lastTriggerTime
        {
            get { return m_LastTrigger.time; }
        }

        public double lastTriggerStartTime
        {
            get { return m_LastTrigger.startTime; }
        }

        public double lastTriggerDuration
        {
            get { return m_LastTrigger.time - m_LastTrigger.startTime; }
        }

        public InputBinding lastTriggerBinding
        {
            get { return GetBinding(m_LastTrigger.bindingIndex); }
        }

        public IInputBindingModifier lastTriggerModifier
        {
            get { return GetModifier(m_LastTrigger.bindingIndex, m_LastTrigger.modifierIndex); }
        }

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

        // Constructor we use for serialization and for actions that are part
        // of sets.
        internal InputAction()
        {
        }

        // Construct a disabled action targeting the given sources.
        public InputAction(string name = null, string binding = null, string modifiers = null)
        {
            if (binding == null && modifiers != null)
                throw new ArgumentException("Cannot have modifier without binding", "modifiers");

            m_Name = name;
            if (binding != null)
            {
                m_Bindings = new[] {new InputBinding {path = binding, modifiers = modifiers}};
                m_BindingsStartIndex = 0;
                m_BindingsCount = 1;
            }
            m_CurrentPhase = Phase.Disabled;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(m_Name))
                return "<unnamed>";

            if (m_ActionSet != null && !isSingletonAction && !string.IsNullOrEmpty(m_ActionSet.name))
                return string.Format("{0}/{1}", m_ActionSet.name, m_Name);

            return m_Name;
        }

        public void Enable()
        {
            if (enabled)
                return;

            // For singleton actions, we create an internal-only InputActionSet
            // private to the action.
            if (m_ActionSet == null)
                CreateInternalActionSetForSingletonAction();

            // First time we're enabled, find all controls.
            if (m_ActionSet.m_Controls == null)
                m_ActionSet.ResolveBindings();

            // Go live.
            m_ActionSet.TellAboutActionChangingEnabledStatus(this, true);
            InstallStateChangeMonitors();

            enabled = true;
            m_CurrentPhase = Phase.Waiting;
        }

        ////TODO: need to cancel action if it's in started state
        public void Disable()
        {
            if (!enabled)
                return;

            // Remove global state.
            m_ActionSet.TellAboutActionChangingEnabledStatus(this, false);
            UninstallStateChangeMonitors();

            enabled = false;

            m_CurrentPhase = Phase.Disabled;
            m_LastTrigger = new TriggerState();

            ////TODO: reset all modifier states
        }

        internal void InstallStateChangeMonitors()
        {
            var manager = InputSystem.s_Manager;
            for (var i = 0; i < m_ResolvedBindings.Count; ++i)
            {
                ////TODO: need to make sure that change monitors of combined bindings are in the right order
                var controls = m_ResolvedBindings[i].controls;
                for (var n = 0; n < controls.Count; ++n)
                    manager.AddStateChangeMonitor(controls[n], this, i);
            }
        }

        internal void UninstallStateChangeMonitors()
        {
            var manager = InputSystem.s_Manager;
            for (var i = 0; i < m_ResolvedBindings.Count; ++i)
            {
                var controls = m_ResolvedBindings[i].controls;
                for (var n = 0; n < controls.Count; ++n)
                    manager.RemoveStateChangeMonitor(controls[n], this);
            }
        }

        // Add a new binding to the action. This works both with actions that are part of
        // action set as well as with actions that aren't.
        // Returns a fluent-style syntax structure that allows performing additional modifications
        // based on the new binding.
        // NOTE: Actions must be disabled while altering their binding sets.
        public AddBindingSyntax AddBinding(string path, string modifiers = null, string groups = null)
        {
            var binding = new InputBinding {path = path, modifiers = modifiers, group = groups};
            var bindingIndex = AddBindingInternal(binding);
            return new AddBindingSyntax(this, bindingIndex);
        }

        public AddCompositeSyntax AddCompositeBinding(string composite)
        {
            ////REVIEW: use 'name' instead of 'path' field here?
            var binding = new InputBinding {path = composite, flags = InputBinding.Flags.Composite};
            var bindingIndex = AddBindingInternal(binding);
            return new AddCompositeSyntax(this, bindingIndex);
        }

        private int AddBindingInternal(InputBinding binding)
        {
            if (enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot add bindings to action '{0}' while the action is enabled", this));

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
                    ArrayHelpers.InsertAt(ref set.m_Bindings, bindingIndex, binding);

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
            return bindingIndex;
        }

        ////TODO: support for removing bindings

        public void ApplyBindingOverride(int bindingIndex, string path)
        {
            if (enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot change overrides on action '{0}' while the action is enabled", this));

            if (bindingIndex < 0 || bindingIndex > m_BindingsCount)
                throw new IndexOutOfRangeException(
                    string.Format("Binding index {0} is out of range for action '{1}' which has {2} bindings",
                        bindingIndex, this, m_BindingsCount));

            m_Bindings[m_BindingsStartIndex + bindingIndex].overridePath = path;
        }

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
                throw new InvalidOperationException(
                    string.Format("Cannot change overrides on action '{0}' while the action is enabled", this));

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
                throw new InvalidOperationException(
                    string.Format("Cannot removed overrides from action '{0}' while the action is enabled", this));

            for (var i = 0; i < m_BindingsCount; ++i)
                m_Bindings[m_BindingsStartIndex + i].overridePath = null;
        }

        // Add all overrides that have been applied to this action to the given list.
        // Returns the number of overrides found.
        public int GetBindingOverrides(List<InputBindingOverride> overrides)
        {
            throw new NotImplementedException();
        }

        ////REVIEW: right now the Clone() methods aren't overridable; do we want that?
        // If you clone an action from a set, you get a singleton action in return.
        public InputAction Clone()
        {
            var clone = new InputAction(name: m_Name);
            clone.m_Bindings = bindings.ToArray();
            clone.m_BindingsCount = m_BindingsCount;
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
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
        internal struct TriggerState
        {
            public Phase phase;
            public double time;
            public double startTime;
            public InputControl control;
            public int bindingIndex;
            public int modifierIndex;
        }

        private bool isSingletonAction
        {
            get { return m_ActionSet == null || ReferenceEquals(m_ActionSet.m_SingletonAction, this); }
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
                        string.Format(
                            "Action {0} has multiple bindings; overriding binding requires the use of binding groups so the action knows which binding to override. Set 'group' property on InputBindingOverride.",
                            this));

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
        private void ChangePhaseOfAction(Phase newPhase, ref TriggerState trigger)
        {
            ThrowIfPhaseTransitionIsInvalid(m_CurrentPhase, newPhase, trigger.bindingIndex, trigger.modifierIndex);

            m_CurrentPhase = newPhase;

            // Store trigger info.
            m_LastTrigger = trigger;
            m_LastTrigger.phase = newPhase;

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
        private void ChangePhaseOfModifier(Phase newPhase, ref TriggerState trigger)
        {
            Debug.Assert(trigger.bindingIndex != -1);
            Debug.Assert(trigger.modifierIndex != -1);

            ////TODO: need to make sure that performed and cancelled phase changes happen on the *same* binding&control
            ////      as the start of the phase

            var modifiersForBinding = m_ResolvedBindings[trigger.bindingIndex].modifiers;
            var currentModifierState = modifiersForBinding[trigger.modifierIndex];
            var newModifierState = currentModifierState;

            // Update modifier state.
            ThrowIfPhaseTransitionIsInvalid(currentModifierState.phase, newPhase, trigger.bindingIndex, trigger.modifierIndex);
            newModifierState.phase = newPhase;
            newModifierState.control = trigger.control;
            if (newPhase == Phase.Started)
                newModifierState.startTime = trigger.time;
            modifiersForBinding[trigger.modifierIndex] = newModifierState;

            // See if it affects the phase of the action itself.
            if (m_CurrentPhase == Phase.Waiting)
            {
                // We're the first modifier to go to the start phase.
                ChangePhaseOfAction(newPhase, ref trigger);
            }
            else if (newPhase == Phase.Cancelled && m_LastTrigger.modifierIndex == trigger.modifierIndex)
            {
                // We're cancelling but maybe there's another modifier ready
                // to go into start phase.

                ChangePhaseOfAction(newPhase, ref trigger);

                for (var i = 0; i < modifiersForBinding.Count; ++i)
                    if (i != trigger.modifierIndex && modifiersForBinding[i].phase == Phase.Started)
                    {
                        var triggerForModifier = new TriggerState
                        {
                            phase = Phase.Started,
                            control = modifiersForBinding[i].control,
                            bindingIndex = trigger.bindingIndex,
                            modifierIndex = i,
                            time = trigger.time,
                            startTime = modifiersForBinding[i].startTime
                        };
                        ChangePhaseOfAction(Phase.Started, ref triggerForModifier);
                        break;
                    }
            }
            else if (m_LastTrigger.modifierIndex == trigger.modifierIndex)
            {
                // Any other phase change goes to action if we're the modifier driving
                // the current phase.
                ChangePhaseOfAction(newPhase, ref trigger);

                // We're the modifier driving the action and we performed the action,
                // so reset any other modifier to waiting state.
                if (newPhase == Phase.Performed)
                {
                    for (var i = 0; i < modifiersForBinding.Count; ++i)
                        if (i != trigger.bindingIndex)
                            ResetModifier(trigger.bindingIndex, i);
                }
            }

            // If the modifier performed or cancelled, go back to waiting.
            if (newPhase == Phase.Performed || newPhase == Phase.Cancelled)
                ResetModifier(trigger.bindingIndex, trigger.modifierIndex);
            ////TODO: reset entire chain
        }

        // Notify observers that we have changed state.
        private void CallListeners(ref InlinedArray<InputActionListener> listeners)
        {
            // Should always have a control that triggered the state change.
            Debug.Assert(m_LastTrigger.control != null);

            // If there's no listeners, don't bother with anything else.
            if (listeners.firstValue == null)
                return;

            // If the binding that triggered is part of a composite, fetch the composite.
            object composite = null;
            var bindingIndex = m_LastTrigger.bindingIndex;
            if (m_ResolvedBindings[bindingIndex].isPartOfComposite)
            {
                var compositeIndex = m_ResolvedBindings[bindingIndex].compositeIndex;
                composite = m_ActionSet.m_Composites[compositeIndex];
            }

            // If we got triggered under the control of a modifier, fetch its state.
            IInputBindingModifier modifier = null;
            var startTime = 0.0;
            if (m_LastTrigger.modifierIndex != -1)
            {
                var modifierState = m_ResolvedBindings[bindingIndex].modifiers[m_LastTrigger.modifierIndex];
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
                m_StartTime = startTime,
                m_Composite = composite,
            };

            Profiler.BeginSample("InputActionCallback");

            listeners.firstValue(context);
            if (listeners.additionalValues != null)
            {
                for (var i = 0; i < listeners.additionalValues.Length; ++i)
                    listeners.additionalValues[i](context);
            }

            Profiler.EndSample();
        }

        private void ThrowIfPhaseTransitionIsInvalid(Phase currentPhase, Phase newPhase, int bindingIndex, int modifierIndex)
        {
            if (newPhase == Phase.Started && currentPhase != Phase.Waiting)
                throw new InvalidOperationException(
                    string.Format("Cannot go from '{0}' to '{1}'; must be '{2}' (action: {3}, modifier: {4})",
                        m_CurrentPhase, Phase.Started, Phase.Waiting, this, GetModifier(bindingIndex, modifierIndex)));
            if (newPhase == Phase.Performed && currentPhase != Phase.Waiting && currentPhase != Phase.Started)
                throw new InvalidOperationException(
                    string.Format("Cannot go from '{0}' to '{1}'; must be '{2}' or '{3}' (action: {4}, modifier: {5})",
                        m_CurrentPhase, Phase.Performed, Phase.Waiting, Phase.Started, this,
                        GetModifier(bindingIndex, modifierIndex)));
            if (newPhase == Phase.Cancelled && currentPhase != Phase.Started)
                throw new InvalidOperationException(
                    string.Format("Cannot go from '{0}' to '{1}'; must be '{2}' (action: {3}, modifier: {4})",
                        m_CurrentPhase, Phase.Cancelled, Phase.Started, this, GetModifier(bindingIndex, modifierIndex)));
        }

        private InputBinding GetBinding(int bindingIndex)
        {
            if (bindingIndex == -1)
                return new InputBinding();

            return m_Bindings[m_BindingsStartIndex + bindingIndex];
        }

        private IInputBindingModifier GetModifier(int bindingIndex, int modifierIndex)
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
            // If we have modifiers, let them do all the processing. The precense of a modifier
            // essentially bypasses the default phase progression logic of an action.
            var modifiers = m_ResolvedBindings[bindingIndex].modifiers;
            if (modifiers.Count > 0)
            {
                ModifierContext context;

                ////REVIEW: defer this check?
                var isAtDefault = control.CheckStateIsAllZeros();

                context.m_Action = this;
                context.m_Trigger = new TriggerState {control = control, time = time, bindingIndex = bindingIndex};
                context.m_ControlIsAtDefaultValue = isAtDefault;
                context.m_TimerHasExpired = false;

                for (var i = 0; i < modifiers.Count; ++i)
                {
                    var state = modifiers[i];
                    var modifier = state.modifier;

                    context.m_Trigger.phase = state.phase;
                    context.m_Trigger.startTime = state.startTime;
                    context.m_Trigger.modifierIndex = i;

                    modifier.Process(ref context);
                }
            }
            else
            {
                // Default logic has no support for cancellations and won't ever go into started
                // phase. Will go from waiting straight to performed and then straight to waiting
                // again.
                //
                // Also, we perform the action on *any* value change. For buttons, this means that
                // if you use the default logic without a modifier, the action will be performed
                // both when you press and when you release the button.

                var trigger = new TriggerState
                {
                    phase = Phase.Performed,
                    control = control,
                    modifierIndex = -1,
                    bindingIndex = bindingIndex,
                    time = time,
                    startTime = time
                };
                ChangePhaseOfAction(Phase.Performed, ref trigger);
            }
        }

        internal void NotifyTimerExpired(int bindingIndex, int modifierIndex, double time)
        {
            ModifierContext context;

            var modifiersForBinding = m_ResolvedBindings[bindingIndex].modifiers;
            var modifierState = modifiersForBinding[modifierIndex];

            context.m_Action = this;
            context.m_ControlIsAtDefaultValue = false; ////REVIEW: how should this be handled?
            context.m_TimerHasExpired = true;
            context.m_Trigger =
                new TriggerState
            {
                control = modifierState.control,
                phase = modifierState.phase,
                time = time,
                bindingIndex = bindingIndex,
                modifierIndex = modifierIndex
            };

            modifierState.isTimerRunning = false;
            modifiersForBinding[modifierIndex] = modifierState;

            modifierState.modifier.Process(ref context);
        }

        // Data we pass to modifiers during processing. Encapsulates all the context
        // they have access to and allows us to extend that functionality without
        // changing the IInputBindingModifier interface.
        public struct ModifierContext
        {
            // These are all set by NotifyControlValueChanged.
            internal InputAction m_Action;
            internal TriggerState m_Trigger;
            internal bool m_ControlIsAtDefaultValue;
            internal bool m_TimerHasExpired;

            internal int bindingIndex
            {
                get { return m_Trigger.bindingIndex; }
            }

            internal int modifierIndex
            {
                get { return m_Trigger.modifierIndex; }
            }

            public InputAction action
            {
                get { return m_Action; }
            }

            public InputControl control
            {
                get { return m_Trigger.control; }
            }

            public Phase phase
            {
                get { return m_Trigger.phase; }
            }

            public double time
            {
                get { return m_Trigger.time; }
            }

            public double startTime
            {
                get { return m_Trigger.startTime; }
            }

            public bool controlHasDefaultValue
            {
                get { return m_ControlIsAtDefaultValue; }
            }

            public bool timerHasExpired
            {
                get { return m_TimerHasExpired; }
            }

            public bool isWaiting
            {
                get { return phase == Phase.Waiting; }
            }

            public bool isStarted
            {
                get { return phase == Phase.Started; }
            }

            public void Started()
            {
                m_Trigger.startTime = time;
                m_Action.ChangePhaseOfModifier(Phase.Started, ref m_Trigger);
            }

            public void Performed()
            {
                m_Action.ChangePhaseOfModifier(Phase.Performed, ref m_Trigger);
            }

            public void Cancelled()
            {
                m_Action.ChangePhaseOfModifier(Phase.Cancelled, ref m_Trigger);
            }

            public void SetTimeout(double seconds)
            {
                var modifiersForBinding = m_Action.m_ResolvedBindings[bindingIndex].modifiers;
                var modifierState = modifiersForBinding[modifierIndex];
                if (modifierState.isTimerRunning)
                    throw new NotImplementedException("cancel current timer");

                var manager = InputSystem.s_Manager;
                manager.AddActionTimeout(m_Action, Time.time + seconds, bindingIndex, modifierIndex);

                modifierState.isTimerRunning = true;
                modifiersForBinding[modifierIndex] = modifierState;
            }
        }

        public struct CompositeBindingContext
        {
        }

        public struct CallbackContext
        {
            internal InputAction m_Action;
            internal InputControl m_Control;
            internal IInputBindingModifier m_Modifier;
            internal object m_Composite;
            internal double m_Time;
            internal double m_StartTime;

            public InputAction action
            {
                get { return m_Action; }
            }

            public InputControl control
            {
                get { return m_Control; }
            }

            public IInputBindingModifier modifier
            {
                get { return m_Modifier; }
            }

            ////REVIEW: rename to ReadValue?
            public TValue GetValue<TValue>()
            {
                ////TODO: instead of straight casting, perform 'as' casts and throw better exceptions than just InvalidCastException

                // If the binding that triggered the action is part of a composite, let
                // the composite determine the value we return.
                if (m_Composite != null)
                {
                    var composite = (IInputBindingComposite<TValue>)m_Composite;
                    var context = new CompositeBindingContext();
                    return composite.ReadValue(ref context);
                }

                return ((InputControl<TValue>)control).ReadValue();
            }

            public double time
            {
                get { return m_Time; }
            }

            public double startTime
            {
                get { return m_StartTime; }
            }

            public double duration
            {
                get { return m_Time - m_StartTime; }
            }
        }

        public struct AddBindingSyntax
        {
            public InputAction action;
            internal int m_BindingIndex;

            internal AddBindingSyntax(InputAction action, int bindingIndex)
            {
                this.action = action;
                m_BindingIndex = bindingIndex;
            }

            ////REVIEW: remove and replace with composite?
            public AddBindingSyntax CombinedWith(string binding, string modifiers = null, string group = null)
            {
                if (action.m_BindingsCount - 1 != m_BindingIndex)
                    throw new InvalidOperationException("Must not add other bindings in-between calling AddBindings() and CombinedWith()");

                var result = action.AddBinding(binding, modifiers: modifiers, groups: group);
                action.m_Bindings[action.m_BindingsStartIndex + result.m_BindingIndex].flags |=
                    InputBinding.Flags.ThisAndPreviousCombine;

                return result;
            }

            public AddBindingSyntax WithModifiers(string modifiers)
            {
                action.m_Bindings[action.m_BindingsStartIndex + m_BindingIndex].modifiers = modifiers;
                return this;
            }
        }

        public struct AddCompositeSyntax
        {
            public InputAction action;
            internal int m_CompositeIndex;
            internal int m_BindingIndex;

            internal AddCompositeSyntax(InputAction action, int compositeIndex)
            {
                this.action = action;
                m_CompositeIndex = compositeIndex;
                m_BindingIndex = -1;
            }

            public AddCompositeSyntax With(string name, string binding, string modifiers = null)
            {
                ////TODO: check whether non-composite bindings have been added in-between

                var result = action.AddBinding(path: binding, modifiers: modifiers);

                var bindingIndex = action.m_BindingsStartIndex + result.m_BindingIndex;
                action.m_Bindings[bindingIndex].name = name;
                action.m_Bindings[bindingIndex].isPartOfComposite = true;

                return this;
            }
        }
    }
}
