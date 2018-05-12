using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Utilities;

////TODO: give every action in the system a stable unique ID; use this also to reference actions in InputActionReferences

////TODO: explore UnityEvents as an option to hook up action responses right in the inspector

////REVIEW: allow individual bindings to be enabled/disabled?

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
        /// The map the action belongs to.
        /// </summary>
        /// <remarks>
        /// If the action is a loose action created in code, this will be <c>null</c>.
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
                if (m_ActionMap == null)
                {
                    if (m_SingletonActionBindings == null)
                        return new ReadOnlyArray<InputBinding>();
                    CreateInternalActionMapForSingletonAction();
                }

                return m_ActionMap.GetBindingsForSingleAction(this);
            }
        }


        /// <summary>
        /// The set of controls to which the action's bindings resolve.
        /// </summary>
        /// <remarks>
        /// May allocate memory on first and also whenever the control setup in the system has changed
        /// (e.g. when devices are added or removed).
        /// </remarks>
        public ReadOnlyArray<InputControl> controls
        {
            get
            {
                if (m_ActionMap == null)
                    CreateInternalActionMapForSingletonAction();
                ////REVIEW: resolving as a side-effect is pretty heavy handed
                ////FIXME: these don't get re-resolved if the control setup in the system changes
                m_ActionMap.ResolveBindingsIfNecessary();
                return m_ActionMap.GetControlsForSingleAction(this);
            }
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
            get { return currentState.phase; }
        }

        ////REVIEW: expose these as a struct?
        ////REVIEW: do we need/want the lastTrigger stuff at all?

        public InputControl lastTriggerControl
        {
            get
            {
                if (m_ActionIndex == InputActionMapState.kInvalidIndex)
                    return null;
                var controlIndex = currentState.controlIndex;
                if (controlIndex == InputActionMapState.kInvalidIndex)
                    return null;
                Debug.Assert(m_ActionMap != null);
                Debug.Assert(m_ActionMap.m_State != null);
                return m_ActionMap.m_State.controls[controlIndex];
            }
        }

        public double lastTriggerTime
        {
            get { return currentState.time; }
        }

        public double lastTriggerStartTime
        {
            get { return currentState.startTime; }
        }

        public double lastTriggerDuration
        {
            get
            {
                var state = currentState;
                return state.time - state.startTime;
            }
        }

        public InputBinding lastTriggerBinding
        {
            get
            {
                if (m_ActionIndex == InputActionMapState.kInvalidIndex)
                    return default(InputBinding);
                var bindingIndex = currentState.bindingIndex;
                if (bindingIndex == InputActionMapState.kInvalidIndex)
                    return default(InputBinding);
                Debug.Assert(m_ActionMap != null);
                Debug.Assert(m_ActionMap.m_State != null);
                var bindingStartIndex = m_ActionMap.m_State.mapIndices[m_ActionMap.m_MapIndex].bindingStartIndex;
                return m_ActionMap.m_Bindings[bindingIndex - bindingStartIndex];
            }
        }

        public IInputBindingModifier lastTriggerModifier
        {
            get
            {
                if (m_ActionIndex == InputActionMapState.kInvalidIndex)
                    return null;
                var modifierIndex = currentState.modifierIndex;
                if (modifierIndex == InputActionMapState.kInvalidIndex)
                    return null;
                Debug.Assert(m_ActionMap != null);
                Debug.Assert(m_ActionMap.m_State != null);
                return m_ActionMap.m_State.modifiers[modifierIndex];
            }
        }

        /// <summary>
        /// Whether the action is currently enabled or not.
        /// </summary>
        /// <remarks>
        /// An action is enabled by either calling <see cref="Enable"/> on it directly or by calling
        /// <see cref="InputActionMap.Enable"/> on the <see cref="InputActionMap"/> containing the action.
        /// When enabled, an action will listen for changes on the controls it is bound to and trigger
        /// ...
        /// </remarks>
        public bool enabled
        {
            get { return phase != InputActionPhase.Disabled; }
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
                m_SingletonActionBindings = new[] {new InputBinding {path = binding, modifiers = modifiers, action = m_Name}};
                m_BindingsStartIndex = 0;
                m_BindingsCount = 1;
            }
        }

        public override string ToString()
        {
            if (m_Name.IsEmpty())
                return "<Unnamed>";

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
                CreateInternalActionMapForSingletonAction();

            // First time we're enabled, find all controls.
            m_ActionMap.ResolveBindingsIfNecessary();

            // Go live.
            m_ActionMap.m_State.EnableSingleAction(this);
            ++m_ActionMap.m_EnabledActionsCount;
        }

        public void Disable()
        {
            if (!enabled)
                return;

            m_ActionMap.m_State.DisableSingleAction(this);
            --m_ActionMap.m_EnabledActionsCount;
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
        [NonSerialized] internal int m_ControlStartIndex;
        [NonSerialized] internal int m_ControlCount;

        [NonSerialized] internal int m_ActionIndex = InputActionMapState.kInvalidIndex;

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

        internal bool isSingletonAction
        {
            get { return m_ActionMap == null || ReferenceEquals(m_ActionMap.m_SingletonAction, this); }
        }

        internal InputActionMap internalMap
        {
            get
            {
                if (m_ActionMap == null)
                    CreateInternalActionMapForSingletonAction();
                return m_ActionMap;
            }
        }

        private InputActionMapState.TriggerState currentState
        {
            get
            {
                if (m_ActionIndex == InputActionMapState.kInvalidIndex)
                    return new InputActionMapState.TriggerState();
                Debug.Assert(m_ActionMap != null);
                Debug.Assert(m_ActionMap.m_State != null);
                return m_ActionMap.m_State.FetchActionState(this);
            }
        }

        private void CreateInternalActionMapForSingletonAction()
        {
            m_ActionMap = new InputActionMap
            {
                m_Actions = new[] { this },
                m_SingletonAction = this,
                m_Bindings = m_SingletonActionBindings
            };
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
                    var context = new InputBindingCompositeContext();
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
