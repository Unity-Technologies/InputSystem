using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Profiling;

////TODO: split off action response code

////TODO: explore UnityEvents as an option to hook up action responses right in the inspector

////TODO: survive domain reloads

////REVIEW: allow individual bindings to be enabled/disabled?

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

        /// <summary>
        /// The current phase of the action.
        /// </summary>
        /// <remarks>
        /// When listening for control input and when responding to control value changes,
        /// actions will go through several possible phases. TODO
        /// </remarks>
        public InputActionPhase phase
        {
            get { return m_CurrentPhase; }
        }

        /// <summary>
        /// The map the action belongs to.
        /// </summary>
        /// <remarks>
        /// If the action is a lose action created in code, this will be <c>null</c>.
        /// </remarks>
        public InputActionMap map
        {
            get { return isSingletonAction ? null : m_ActionMap; }
        }

        ////TODO: add support for turning binding array into displayable info
        ////      (allow to constrain by sets of devics set on action set)

        /// <summary>
        /// The list of bindings associated with the action.
        /// </summary>
        /// <remarks>
        /// This will include only bindings that directly trigger the action. If the action is part of a
        /// <see cref="InputActionMap">set</see> that triggers the action through a combination of bindings,
        /// for example, only the bindings that ultimately trigger the action are included in the list.
        ///
        /// May allocate memory on first hit.
        /// </remarks>
        public ReadOnlyArray<InputBinding> bindings
        {
            get
            {
                // If m_ActionSet is null, we're a singleton action that has had no bindings added
                // to it yet.
                if (m_ActionMap == null)
                {
                    Debug.Assert(isSingletonAction);
                    return new ReadOnlyArray<InputBinding>();
                }

                return m_ActionMap.GetBindingsForSingleAction(this);
            }
        }


        /// <summary>
        /// The set of controls to which the action's bindings resolve.
        /// </summary>
        public ReadOnlyArray<InputControl> controls
        {
            get
            {
                if (m_ActionMap == null)
                    CreateInternalActionSetForSingletonAction();
                return m_ActionMap.GetControlsForSingleAction(this);
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

        public InputAction(InternedString name = new InternedString())
        {
            m_Name = name;
        }

        // Construct a disabled action targeting the given sources.
        // NOTE: This constructor is *not* used for actions added to sets. These are constructed
        //       by sets themselves.
        public InputAction(string name = null, string binding = null, string modifiers = null)
            : this(new InternedString(name))
        {
            if (binding == null && modifiers != null)
                throw new ArgumentException("Cannot have modifier without binding", "modifiers");

            if (binding != null)
            {
                m_SingletonActionBindings = new[] {new InputBinding {path = binding, modifiers = modifiers}};
                m_BindingsStartIndex = 0;
                m_BindingsCount = 1;
            }
            m_CurrentPhase = InputActionPhase.Disabled;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(m_Name))
                return "<unnamed>";

            if (m_ActionMap != null && !isSingletonAction && !string.IsNullOrEmpty(m_ActionMap.name))
                return string.Format("{0}/{1}", m_ActionMap.name, m_Name);

            return m_Name;
        }

        public void Enable()
        {
            if (enabled)
                return;

            // For singleton actions, we create an internal-only InputActionMap
            // private to the action.
            if (m_ActionMap == null)
                CreateInternalActionSetForSingletonAction();

            // First time we're enabled, find all controls.
            /*if (m_ActionMap.m_Controls == null)*/////FIXME
            m_ActionMap.ResolveBindings();

            // Go live.
            m_ActionMap.TellAboutActionChangingEnabledStatus(this, true);
            //InstallStateChangeMonitors();

            enabled = true;
            m_CurrentPhase = InputActionPhase.Waiting;
        }

        ////TODO: need to cancel action if it's in started state
        public void Disable()
        {
            if (!enabled)
                return;

            // Remove global state.
            m_ActionMap.TellAboutActionChangingEnabledStatus(this, false);
            //UninstallStateChangeMonitors();

            enabled = false;

            m_CurrentPhase = InputActionPhase.Disabled;
            m_LastTrigger = new InputActionMapState.TriggerState();

            ////TODO: reset all modifier states
        }

        ////TODO: support for removing bindings

        public void ApplyBindingOverride(int bindingIndex, string path)
        {
            if (enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot change overrides on action '{0}' while the action is enabled", this));

            if (bindingIndex < 0 || bindingIndex >= m_BindingsCount)
                throw new IndexOutOfRangeException(
                    string.Format("Binding index {0} is out of range for action '{1}' which has {2} bindings",
                        bindingIndex, this, m_BindingsCount));

            m_SingletonActionBindings[m_BindingsStartIndex + bindingIndex].overridePath = path;
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

            m_SingletonActionBindings[m_BindingsStartIndex + bindingIndex].overridePath = bindingOverride.binding;
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
                m_SingletonActionBindings[m_BindingsStartIndex + i].overridePath = null;
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
            clone.m_SingletonActionBindings = bindings.ToArray();
            clone.m_BindingsCount = m_BindingsCount;
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        [SerializeField] internal InternedString m_Name;

        // For singleton actions, we serialize the bindings directly as part of the action.
        // For any other type of action, this is null.
        [SerializeField] internal InputBinding[] m_SingletonActionBindings;

        [NonSerialized] internal int m_BindingsStartIndex;
        [NonSerialized] internal int m_BindingsCount;

        [NonSerialized] internal bool m_Enabled;

        /// <summary>
        /// The action map that owns the action.
        /// </summary>
        /// <remarks>
        /// This is not serialized. The action map will restore this back references after deserialization.
        /// </remarks>
        [NonSerialized] internal InputActionMap m_ActionMap;

        // Listeners. No array allocations if only a single listener.
        [NonSerialized] internal InlinedArray<InputActionListener> m_OnStarted;
        [NonSerialized] internal InlinedArray<InputActionListener> m_OnCancelled;
        [NonSerialized] internal InlinedArray<InputActionListener> m_OnPerformed;

        ////TODO: move this out of here and into InputActionMap
        // State we keep for enabling/disabling. This is volatile and not put on disk.
        // NOTE: m_Controls and m_ResolvedBinding array are stored on InputActionMap.
        [NonSerialized] internal ReadOnlyArray<InputControl> m_Controls;
        [NonSerialized] internal ReadOnlyArray<InputActionMapState.BindingState> m_ResolvedBindings;

        // State releated to phase shifting and triggering of action.
        // Most of this state we lazily reset as we have to keep it available for
        // one frame but don't want to actively reset between frames.
        [NonSerialized] private InputActionPhase m_CurrentPhase;
        [NonSerialized] private InputActionMapState.TriggerState m_LastTrigger;

        internal bool isSingletonAction
        {
            get { return m_ActionMap == null || ReferenceEquals(m_ActionMap.m_SingletonAction, this); }
        }

        internal InputActionMap internalMap
        {
            get
            {
                if (m_ActionMap == null)
                    CreateInternalActionSetForSingletonAction();
                return m_ActionMap;
            }
        }

        private void CreateInternalActionSetForSingletonAction()
        {
            m_ActionMap = new InputActionMap {m_SingletonAction = this, m_Bindings = m_SingletonActionBindings};
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
                    string.Compare(m_SingletonActionBindings[m_BindingsStartIndex].group, group,
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
                    if (string.Compare(m_SingletonActionBindings[m_BindingsStartIndex + i].group, 0, group, 0, groupStringLength, true) == 0)
                    {
                        if (currentIndexInGroup == indexInGroup)
                            return i;

                        ++currentIndexInGroup;
                    }
            }

            return -1;
        }

        // Perform a phase change on the action. Visible to observers.
        private void ChangePhaseOfAction(InputActionPhase newPhase, ref InputActionMapState.TriggerState trigger)
        {
            ThrowIfPhaseTransitionIsInvalid(m_CurrentPhase, newPhase, trigger.bindingIndex, trigger.modifierIndex);

            m_CurrentPhase = newPhase;

            // Store trigger info.
            m_LastTrigger = trigger;
            m_LastTrigger.phase = newPhase;

            // Let listeners know.
            switch (newPhase)
            {
                case InputActionPhase.Started:
                    CallListeners(ref m_OnStarted);
                    break;

                case InputActionPhase.Performed:
                    CallListeners(ref m_OnPerformed);
                    m_CurrentPhase = InputActionPhase.Waiting; // Go back to waiting after performing action.
                    break;

                case InputActionPhase.Cancelled:
                    CallListeners(ref m_OnCancelled);
                    m_CurrentPhase = InputActionPhase.Waiting; // Go back to waiting after cancelling action.
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
        private void ChangePhaseOfModifier(InputActionPhase newPhase, ref InputActionMapState.TriggerState trigger)
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
            newModifierState.triggerControl = trigger.control;
            if (newPhase == InputActionPhase.Started)
                newModifierState.startTime = trigger.time;
            modifiersForBinding[trigger.modifierIndex] = newModifierState;

            // See if it affects the phase of the action itself.
            if (m_CurrentPhase == InputActionPhase.Waiting)
            {
                // We're the first modifier to go to the start phase.
                ChangePhaseOfAction(newPhase, ref trigger);
            }
            else if (newPhase == InputActionPhase.Cancelled && m_LastTrigger.modifierIndex == trigger.modifierIndex)
            {
                // We're cancelling but maybe there's another modifier ready
                // to go into start phase.

                ChangePhaseOfAction(newPhase, ref trigger);

                for (var i = 0; i < modifiersForBinding.Count; ++i)
                    if (i != trigger.modifierIndex && modifiersForBinding[i].phase == InputActionPhase.Started)
                    {
                        var triggerForModifier = new InputActionMapState.TriggerState
                        {
                            phase = InputActionPhase.Started,
                            control = modifiersForBinding[i].triggerControl,
                            bindingIndex = trigger.bindingIndex,
                            modifierIndex = i,
                            time = trigger.time,
                            startTime = modifiersForBinding[i].startTime
                        };
                        ChangePhaseOfAction(InputActionPhase.Started, ref triggerForModifier);
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
                if (newPhase == InputActionPhase.Performed)
                {
                    for (var i = 0; i < modifiersForBinding.Count; ++i)
                        if (i != trigger.bindingIndex)
                            ResetModifier(trigger.bindingIndex, i);
                }
            }

            // If the modifier performed or cancelled, go back to waiting.
            if (newPhase == InputActionPhase.Performed || newPhase == InputActionPhase.Cancelled)
                ResetModifier(trigger.bindingIndex, trigger.modifierIndex);
            ////TODO: reset entire chain
        }

        // Notify observers that we have changed state.
        private void CallListeners(ref InlinedArray<InputActionListener> listeners)
        {
            // Should always have a control that triggered the state change.
            Debug.Assert(m_LastTrigger.control != null);

            // If there's no listeners, don't bother with anything else.
            if (listeners.length == 0)
                return;

            // If the binding that triggered is part of a composite, fetch the composite.
            object composite = null;
            var bindingIndex = m_LastTrigger.bindingIndex;
            if (m_ResolvedBindings[bindingIndex].isPartOfComposite)
                composite = m_ResolvedBindings[bindingIndex].composite;

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
                for (var i = 0; i < listeners.length - 1; ++i)
                    listeners.additionalValues[i](context);
            }

            Profiler.EndSample();
        }

        private void ThrowIfPhaseTransitionIsInvalid(InputActionPhase currentPhase, InputActionPhase newPhase, int bindingIndex, int modifierIndex)
        {
            if (newPhase == InputActionPhase.Started && currentPhase != InputActionPhase.Waiting)
                throw new InvalidOperationException(
                    string.Format("Cannot go from '{0}' to '{1}'; must be '{2}' (action: {3}, modifier: {4})",
                        m_CurrentPhase, InputActionPhase.Started, InputActionPhase.Waiting, this, GetModifier(bindingIndex, modifierIndex)));
            if (newPhase == InputActionPhase.Performed && currentPhase != InputActionPhase.Waiting && currentPhase != InputActionPhase.Started)
                throw new InvalidOperationException(
                    string.Format("Cannot go from '{0}' to '{1}'; must be '{2}' or '{3}' (action: {4}, modifier: {5})",
                        m_CurrentPhase, InputActionPhase.Performed, InputActionPhase.Waiting, InputActionPhase.Started, this,
                        GetModifier(bindingIndex, modifierIndex)));
            if (newPhase == InputActionPhase.Cancelled && currentPhase != InputActionPhase.Started)
                throw new InvalidOperationException(
                    string.Format("Cannot go from '{0}' to '{1}'; must be '{2}' (action: {3}, modifier: {4})",
                        m_CurrentPhase, InputActionPhase.Cancelled, InputActionPhase.Started, this, GetModifier(bindingIndex, modifierIndex)));
        }

        private InputBinding GetBinding(int bindingIndex)
        {
            if (bindingIndex == -1)
                return new InputBinding();

            return m_SingletonActionBindings[m_BindingsStartIndex + bindingIndex];
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
                new InputActionMapState.ModifierState
            {
                modifier = oldState.modifier,
                phase = InputActionPhase.Waiting
            };
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
                new InputActionMapState.TriggerState
            {
                control = modifierState.triggerControl,
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
            internal InputActionMapState.TriggerState m_Trigger;
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

            public InputActionPhase phase
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
                get { return phase == InputActionPhase.Waiting; }
            }

            public bool isStarted
            {
                get { return phase == InputActionPhase.Started; }
            }

            public void Started()
            {
                m_Trigger.startTime = time;
                m_Action.ChangePhaseOfModifier(InputActionPhase.Started, ref m_Trigger);
            }

            public void Performed()
            {
                m_Action.ChangePhaseOfModifier(InputActionPhase.Performed, ref m_Trigger);
            }

            public void Cancelled()
            {
                m_Action.ChangePhaseOfModifier(InputActionPhase.Cancelled, ref m_Trigger);
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
    }
}
