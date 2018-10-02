using System;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: have single delegate instead of separate performed/started/cancelled callbacks?

////REVIEW: remove everything on InputAction that isn't about being an endpoint? (i.e. 'controls' and 'bindings')

////REVIEW: should the enable/disable API actually sit on InputSystem?

////REVIEW: might have to revisit when we fire actions in relation to Update/FixedUpdate

////REVIEW: Do we need to have separate display names for actions? They should definitely be allowed to contain '/' and whatnot

////REVIEW: the entire 'lastXXX' API section is shit and needs a pass

////REVIEW: resolving as a side-effect of 'controls' and 'devices' seems pretty heavy handed

////TODO: do not hardcode the transition from performed->waiting; allow an action to be performed over and over again inside
////      a single start cycle

////TODO: allow changing bindings without having to disable

////REVIEW: should actions basically be handles to data that is stored in an array in the map?
////        (with this, we could also implement more efficient duplication where we duplicate all the binding data but not the action data)

namespace UnityEngine.Experimental.Input
{
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
        /// A stable, unique identifier for the action.
        /// </summary>
        /// <remarks>
        /// This can be used instead of the name to refer to the action. Doing so allows referring to the
        /// action such that renaming the action does not break references.
        /// </remarks>
        public Guid id
        {
            get
            {
                if (m_Guid == Guid.Empty)
                {
                    if (m_Id == null)
                    {
                        m_Guid = Guid.NewGuid();
                        m_Id = m_Guid.ToString();
                    }
                    else
                    {
                        m_Guid = new Guid(m_Id);
                    }
                }
                return m_Guid;
            }
        }

        internal Guid idDontGenerate
        {
            get
            {
                if (m_Guid == Guid.Empty && !string.IsNullOrEmpty(m_Id))
                    m_Guid = new Guid(m_Id);
                return m_Guid;
            }
        }

        /// <summary>
        /// Name of control layout expected for controls bound to this action.
        /// </summary>
        /// <remarks>
        /// This is optional and is null by default.
        ///
        /// Constraining an action to a particular control layout allows determine the value
        /// type and expected input behavior of an action without being reliant on any particular
        /// binding.
        /// </remarks>
        public string expectedControlLayout
        {
            get { return m_ExpectedControlLayout; }
            set { m_ExpectedControlLayout = value; }
        }

        /// <summary>
        /// The map the action belongs to.
        /// </summary>
        /// <remarks>
        /// If the action is a loose action created in code, this will be <c>null</c>.
        /// </remarks>
        public InputActionMap actionMap
        {
            get { return isSingletonAction ? null : m_ActionMap; }
        }

        public InputBinding? bindingMask
        {
            get
            {
                if (m_BindingMask.isEmpty)
                    return null;
                return m_BindingMask;
            }
        }

        ////TODO: add support for turning binding array into displayable info
        ////      (allow to constrain by sets of devices set on action set)

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
            get { return GetOrCreateActionMap().GetBindingsForSingleAction(this); }
        }

        /// <summary>
        /// The set of controls to which the action's bindings resolve.
        /// </summary>
        /// <remarks>
        /// May allocate memory each time the control setup changes on the action.
        /// </remarks>
        public ReadOnlyArray<InputControl> controls
        {
            get
            {
                var map = GetOrCreateActionMap();
                map.ResolveBindingsIfNecessary();
                return map.GetControlsForSingleAction(this);
            }
        }

        public bool required
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
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

        ////REVIEW: when looking at this, you're probably interested in the last value more than anything
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
                var bindingStartIndex = m_ActionMap.m_State.mapIndices[m_ActionMap.m_MapIndexInState].bindingStartIndex;
                return m_ActionMap.m_Bindings[bindingIndex - bindingStartIndex];
            }
        }

        public IInputInteraction lastTriggerInteraction
        {
            get
            {
                if (m_ActionIndex == InputActionMapState.kInvalidIndex)
                    return null;
                var interactionIndex = currentState.interactionIndex;
                if (interactionIndex == InputActionMapState.kInvalidIndex)
                    return null;
                Debug.Assert(m_ActionMap != null);
                Debug.Assert(m_ActionMap.m_State != null);
                return m_ActionMap.m_State.interactions[interactionIndex];
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

        /// <summary>
        /// Event that is triggered when the action has been started.
        /// </summary>
        /// <see cref="InputActionPhase.Started"/>
        public event Action<CallbackContext> started
        {
            add { m_OnStarted.Append(value); }
            remove { m_OnStarted.Remove(value); }
        }

        /// <summary>
        /// Event that is triggered when the action has been <see cref="started"/>
        /// but then cancelled before being fully <see cref="performed"/>.
        /// </summary>
        /// <see cref="InputActionPhase.Cancelled"/>
        public event Action<CallbackContext> cancelled
        {
            add { m_OnCancelled.Append(value); }
            remove { m_OnCancelled.Remove(value); }
        }

        /// <summary>
        /// Event that is triggered when the action has been fully performed.
        /// </summary>
        /// <see cref="InputActionPhase.Performed"/>
        public event Action<CallbackContext> performed
        {
            add { m_OnPerformed.Append(value); }
            remove { m_OnPerformed.Remove(value); }
        }

        // Constructor we use for serialization and for actions that are part
        // of sets.
        internal InputAction()
        {
        }

        public InputAction(string name = null)
        {
            m_Name = name;
        }

        // Construct a disabled action targeting the given sources.
        // NOTE: This constructor is *not* used for actions added to sets. These are constructed
        //       by sets themselves.
        public InputAction(string name = null, string binding = null, string interactions = null, string expectedControlLayout = null)
            : this(name)
        {
            if (binding == null && interactions != null)
                throw new ArgumentException("Cannot have interaction without binding", "interactions");

            if (binding != null)
            {
                m_SingletonActionBindings = new[] {new InputBinding {path = binding, interactions = interactions, action = m_Name}};
                m_BindingsStartIndex = 0;
                m_BindingsCount = 1;
            }

            this.expectedControlLayout = expectedControlLayout;
        }

        public override string ToString()
        {
            if (m_Name == null)
                return "<Unnamed>";

            if (m_ActionMap != null && !isSingletonAction && !String.IsNullOrEmpty(m_ActionMap.name))
                return String.Format("{0}/{1}", m_ActionMap.name, m_Name);

            return m_Name;
        }

        public void Enable()
        {
            if (enabled)
                return;

            // For singleton actions, we create an internal-only InputActionMap
            // private to the action.
            var map = GetOrCreateActionMap();

            // First time we're enabled, find all controls.
            map.ResolveBindingsIfNecessary();

            // Go live.
            map.m_State.EnableSingleAction(this);
            ++map.m_EnabledActionsCount;
        }

        public void Disable()
        {
            if (!enabled)
                return;

            m_ActionMap.m_State.DisableSingleAction(this);
            --m_ActionMap.m_EnabledActionsCount;
        }

        public void SetBindingMask(InputBinding bindingMask)
        {
            if (m_BindingMask == bindingMask)
                return;

            m_BindingMask = bindingMask;
            if (!m_BindingMask.isEmpty)
                m_BindingMask.action = name;

            var map = GetOrCreateActionMap();
            if (map.m_State != null)
                map.ResolveBindings();
        }

        public void SetBindingMask(string bindingGroups)
        {
            if (string.IsNullOrEmpty(bindingGroups))
                bindingGroups = null;
            SetBindingMask(new InputBinding {groups = bindingGroups});
        }

        public void ClearBindingMask()
        {
            SetBindingMask(new InputBinding());
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

        ////REVIEW: it would be best if these were InternedStrings; however, for serialization, it has to be strings
        [SerializeField] internal string m_Name;
        [SerializeField] internal string m_ExpectedControlLayout;
        [SerializeField] internal string m_Id; // Can't serialize System.Guid and Unity's GUID is editor only.

        // For singleton actions, we serialize the bindings directly as part of the action.
        // For any other type of action, this is null.
        [SerializeField] internal InputBinding[] m_SingletonActionBindings;

        [NonSerialized] internal InputBinding m_BindingMask;
        [NonSerialized] internal int m_BindingsStartIndex;
        [NonSerialized] internal int m_BindingsCount;
        [NonSerialized] internal int m_ControlStartIndex;
        [NonSerialized] internal int m_ControlCount;
        [NonSerialized] internal Guid m_Guid;

        /// <summary>
        /// Index of the action in the <see cref="InputActionMapState"/> associated with the
        /// action's <see cref="InputActionMap"/>.
        /// </summary>
        /// <remarks>
        /// This is not necessarily the same as the index of the action in its map.
        /// </remarks>
        /// <seealso cref="actionMap"/>
        [NonSerialized] internal int m_ActionIndex = InputActionMapState.kInvalidIndex;

        /// <summary>
        /// The action map that owns the action.
        /// </summary>
        /// <remarks>
        /// This is not serialized. The action map will restore this back references after deserialization.
        /// </remarks>
        [NonSerialized] internal InputActionMap m_ActionMap;

        // Listeners. No array allocations if only a single listener.
        [NonSerialized] internal InlinedArray<Action<CallbackContext>> m_OnStarted;
        [NonSerialized] internal InlinedArray<Action<CallbackContext>> m_OnCancelled;
        [NonSerialized] internal InlinedArray<Action<CallbackContext>> m_OnPerformed;

        /// <summary>
        /// Whether the action is a loose action created in code (e.g. as a property on a component).
        /// </summary>
        /// <remarks>
        /// Singleton actions are not contained in maps visible to the user. Internally, we do create
        /// a map for them that contains just the singleton action. To the action system, there are no
        /// actions without action maps.
        /// </remarks>
        internal bool isSingletonAction
        {
            get { return m_ActionMap == null || ReferenceEquals(m_ActionMap.m_SingletonAction, this); }
        }

        private InputActionMapState.TriggerState currentState
        {
            get
            {
                if (m_ActionIndex == InputActionMapState.kInvalidIndex)
                    return new InputActionMapState.TriggerState();
                Debug.Assert(m_ActionMap != null);
                Debug.Assert(m_ActionMap.m_State != null);
                return m_ActionMap.m_State.FetchTriggerState(this);
            }
        }

        internal bool HaveBindingFilterMatching(ref InputBinding bindingFilter)
        {
            throw new NotImplementedException();
        }

        internal InputActionMap GetOrCreateActionMap()
        {
            if (m_ActionMap == null)
                CreateInternalActionMapForSingletonAction();
            return m_ActionMap;
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

        internal void ThrowIfModifyingBindingsIsNotAllowed()
        {
            if (enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot modify bindings on action '{0}' while the action is enabled", this));
            if (GetOrCreateActionMap().enabled)
                throw new InvalidOperationException(
                    string.Format("Cannot modify bindings on action '{0}' while its action map is enabled", this));
        }

        /// <summary>
        /// Information provided to action callbacks about what triggered an action.
        /// </summary>
        /// <seealso cref="performed"/>
        /// <seealso cref="started"/>
        /// <seealso cref="cancelled"/>
        /// <seealso cref="InputActionMap.actionTriggered"/>
        public struct CallbackContext
        {
            internal InputActionMapState m_State;
            internal int m_ControlIndex;
            internal int m_BindingIndex;
            internal int m_InteractionIndex;
            internal double m_Time;

            internal int actionIndex
            {
                get
                {
                    if (m_State == null)
                        return InputActionMapState.kInvalidIndex;
                    return m_State.bindingStates[m_BindingIndex].actionIndex;
                }
            }

            public InputActionPhase phase
            {
                get
                {
                    if (m_State == null)
                        return InputActionPhase.Disabled;
                    return m_State.triggerStates[actionIndex].phase;
                }
            }

            /// <summary>
            /// The action that got triggered.
            /// </summary>
            public InputAction action
            {
                get
                {
                    if (m_State == null)
                        return null;
                    return m_State.GetActionOrNull(m_BindingIndex);
                }
            }

            /// <summary>
            /// The control that triggered the action.
            /// </summary>
            /// <remarks>
            /// In case of a composite binding, this is the control of the composite that activated the
            /// composite as a whole. For example, in case of a WASD-style binding, it could be the W key.
            /// </remarks>
            public InputControl control
            {
                get
                {
                    if (m_State == null)
                        return null;
                    return m_State.controls[m_ControlIndex];
                }
            }

            /// <summary>
            /// The interaction that triggered the action or <c>null</c> if the binding that triggered does not
            /// have any particular interaction set on it.
            /// </summary>
            public IInputInteraction interaction
            {
                get
                {
                    if (m_State == null)
                        return null;
                    if (m_InteractionIndex == InputActionMapState.kInvalidIndex)
                        return null;
                    return m_State.interactions[m_InteractionIndex];
                }
            }

            /// <summary>
            /// The time at which the action got triggered.
            /// </summary>
            /// <remarks>
            /// This is usually determined by the timestamp of the input event that activated a control
            /// bound to the action.
            /// </remarks>
            public double time
            {
                get { return m_Time; }
            }

            /// <summary>
            /// Time at which the action was started.
            /// </summary>
            /// <remarks>
            /// This is only relevant for actions that go through distinct a <see cref="InputActionPhase.Started"/>
            /// cycle as driven by <see cref="IInputInteraction">interactions</see>.
            /// </remarks>
            public double startTime
            {
                get
                {
                    if (m_State == null)
                        return 0;
                    if (m_InteractionIndex == InputActionMapState.kInvalidIndex)
                        return time;
                    return m_State.interactionStates[m_InteractionIndex].startTime;
                }
            }

            /// <summary>
            /// Time difference between <see cref="time"/> and <see cref="startTime"/>.
            /// </summary>
            public double duration
            {
                get { return time - startTime; }
            }

            public Type valueType
            {
                get
                {
                    if (m_State == null)
                        return null;

                    return m_State.GetValueType(m_BindingIndex, m_ControlIndex);
                }
            }

            public int valueSizeInBytes
            {
                get
                {
                    if (m_State == null)
                        return 0;

                    return m_State.GetValueSizeInBytes(m_BindingIndex, m_ControlIndex);
                }
            }

            public unsafe void ReadValue(void* buffer, int bufferSize)
            {
                if (m_State == null)
                    return;
                m_State.ReadValue(m_BindingIndex, m_ControlIndex, buffer, bufferSize);
            }

            public TValue ReadValue<TValue>()
                where TValue : struct
            {
                var value = default(TValue);
                if (m_State != null)
                    value = m_State.ReadValue<TValue>(m_BindingIndex, m_ControlIndex);
                return value;
            }

            // really read previous value, not value from last frame
            public TValue ReadPreviousValue<TValue>()
            {
                throw new NotImplementedException();
            }
        }
    }
}
