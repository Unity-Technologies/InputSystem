using System;
using UnityEngine.InputSystem.Utilities;

////FIXME: Whether a control from a binding that's part of a composite appears on an action is currently not consistently enforced.
////       If it mentions the action, it appears on the action. Otherwise it doesn't. The controls should consistently appear on the
////       action based on what action the *composite* references.

////REVIEW: should continuous actions *always* trigger as long as they are enabled? (even if no control is actuated)

////REVIEW: I think the action system as it is today offers too many ways to shoot yourself in the foot. It has
////        flexibility but at the same time has abundant opportunity for ending up with dysfunction. Common setups
////        have to come preconfigured and work robustly for the user without requiring much understanding of how
////        the system fits together.

////REVIEW: have single delegate instead of separate performed/started/canceled callbacks?

////REVIEW: remove everything on InputAction that isn't about being an endpoint? (i.e. 'controls' and 'bindings')

////REVIEW: might have to revisit when we fire actions in relation to Update/FixedUpdate

////REVIEW: Do we need to have separate display names for actions? They should definitely be allowed to contain '/' and whatnot

////REVIEW: the entire 'lastXXX' API section is shit and needs a pass

////TODO: allow changing bindings without having to disable

////REVIEW: what about having the concept of "consumed" on the callback context?

////REVIEW: should actions basically be handles to data that is stored in an array in the map?
////        (with this, we could also implement more efficient duplication where we duplicate all the binding data but not the action data)

////REVIEW: have "Always Enabled" toggle on actions?

// An issue that has come up repeatedly is the request for having a polling-based API that allows actions to be used the same
// way UnityEngine.Input allows axes to be used. Here's my thoughts. While such an API is a bad fit for how actions operate,
// the request is definitely reasonable and a simple polling-based API could be created in a relatively straightforward way. It'd
// have to drop some details on the floor and do some aggregation of state, but where someone reaches the limits, there would always
// be a possible migration to the callback-based API.
//
// However, before launching into creating an entirely separate API to interface with actions, I would first like to try and see
// if something can be done to obsolete the need for it. The main obstacle with the callback-based API is that setting up and managing
// the callbacks is very tedious and requires a lot of duct tape. What if instead the setup was trivial and something you never have
// to worry about? Would the need for a polling-based API still be there? That's what I would like to find out first.

namespace UnityEngine.InputSystem
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
    public class InputAction : ICloneable, IDisposable
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
        public string name => m_Name;

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
                MakeSureIdIsInPlace();
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
            get => m_ExpectedControlLayout;
            set => m_ExpectedControlLayout = value;
        }

        public string processors => m_Processors;

        public string interactions => m_Interactions;

        /// <summary>
        /// The map the action belongs to.
        /// </summary>
        /// <remarks>
        /// If the action is a loose action created in code, this will be <c>null</c>.
        /// </remarks>
        public InputActionMap actionMap => isSingletonAction ? null : m_ActionMap;

        public InputBinding? bindingMask
        {
            ////REVIEW: if no mask is set on the action but one is set on the map, should we return that one?
            get => m_BindingMask;
            set
            {
                if (value == m_BindingMask)
                    return;

                if (value != null)
                {
                    var v = value.Value;
                    v.action = name;
                    value = v;
                }

                m_BindingMask = value;

                var map = GetOrCreateActionMap();
                if (map.m_State != null)
                    map.LazyResolveBindings();
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
        public ReadOnlyArray<InputBinding> bindings => GetOrCreateActionMap().GetBindingsForSingleAction(this);

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

        public bool initialStateCheck
        {
            get => (m_Flags & ActionFlags.InitialStateCheck) != 0;
            set
            {
                if (enabled)
                    throw new InvalidOperationException(
                        $"Cannot change the 'initialStateCheck' flag of action '{this} while the action is enabled");

                if (value)
                    m_Flags |= ActionFlags.InitialStateCheck;
                else
                    m_Flags &= ~ActionFlags.InitialStateCheck;
            }
        }

        /// <summary>
        /// If true, the action will continuously trigger <see cref="performed"/> on every input update
        /// while the action is in the <see cref="InputActionPhase.Performed"/> phase.
        /// </summary>
        /// <remarks>
        /// This is off by default.
        ///
        /// An action must be disabled when setting this property.
        ///
        /// Continuous actions are useful when otherwise it would be necessary to manually set up an
        /// action response to run a piece of logic every update. Instead, the fact that input already
        /// updates in sync with the player loop can be leveraged to have actions triggered continuously.
        ///
        /// A typical use case is "move" and "look" functionality tied to gamepad sticks. Even if the gamepad
        /// stick is not moved in a particular update, the current value of the stick should be applied. A
        /// simple way to achieve this is by toggling on "continuous" mode through this property.
        ///
        /// Note that continuous mode does not affect phases other than <see cref="InputActionPhase.Performed"/>.
        /// This means that, for example, <see cref="InputActionPhase.Started"/> (and the associated <see cref="started"/>)
        /// will not be triggered repeatedly even if continuous mode is toggled on for an action.
        ///
        /// <example>
        /// <code>
        /// // Set up an action that will be performed continuously while the right stick on the gamepad
        /// // is moved out of its deadzone.
        /// var action = new InputAction("Look", binding: "&lt;Gamepad&gt;/rightStick);
        /// action.continuous = true;
        /// action.performed = ctx => Look(ctx.ReadValue&lt;Vector2&gt;());
        /// action.Enable();
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="InvalidOperationException">The action is <see cref="enabled"/>. Continuous
        /// mode can only be changed while an action is disabled.</exception>
        /// <seealso cref="phase"/>
        /// <seealso cref="performed"/>
        /// <seealso cref="InputActionPhase.Performed"/>
        public bool continuous
        {
            get => (m_Flags & ActionFlags.Continuous) != 0;
            set
            {
                if (enabled)
                    throw new InvalidOperationException(
                        $"Cannot change the 'continuous' flag of action '{this} while the action is enabled");

                if (value)
                    m_Flags |= ActionFlags.Continuous;
                else
                    m_Flags &= ~ActionFlags.Continuous;
            }
        }

        /// <summary>
        /// If enabled, the action will not gate any control changes but will instead pass through
        /// any change on any of the bound controls as is.
        /// </summary>
        /// <remarks>
        /// This behavior is useful for actions that are not meant to model any kind of interaction but
        /// should rather just listen for input of any kind. By default, an action will be driven based
        /// on the amount of actuation on the bound controls. Any control with the highest amount of
        /// actuation gets to drive an action. This can be undesirable. For example, an action may
        /// want to listen for any kind of activity on any of the bound controls. In this case, set
        /// this property to true.
        ///
        /// This behavior is disabled by default.
        /// </remarks>
        public bool passThrough
        {
            get => (m_Flags & ActionFlags.PassThrough) != 0;
            set
            {
                if (enabled)
                    throw new InvalidOperationException(
                        $"Cannot change the 'passThrough' flag of action '{this} while the action is enabled");

                if (value)
                    m_Flags |= ActionFlags.PassThrough;
                else
                    m_Flags &= ~ActionFlags.PassThrough;
            }
        }

        /// <summary>
        /// The current phase of the action.
        /// </summary>
        /// <remarks>
        /// When listening for control input and when responding to control value changes,
        /// actions will go through several possible phases. TODO
        /// </remarks>
        public InputActionPhase phase => currentState.phase;

        ////REVIEW: expose these as a struct?

        /// <summary>
        /// Whether the action is currently enabled or not.
        /// </summary>
        /// <remarks>
        /// An action is enabled by either calling <see cref="Enable"/> on it directly or by calling
        /// <see cref="InputActionMap.Enable"/> on the <see cref="InputActionMap"/> containing the action.
        /// When enabled, an action will listen for changes on the controls it is bound to and trigger
        /// ...
        /// </remarks>
        public bool enabled => phase != InputActionPhase.Disabled;

        /// <summary>
        /// Event that is triggered when the action has been started.
        /// </summary>
        /// <see cref="InputActionPhase.Started"/>
        public event Action<CallbackContext> started
        {
            add => m_OnStarted.Append(value);
            remove => m_OnStarted.Remove(value);
        }

        /// <summary>
        /// Event that is triggered when the action has been <see cref="started"/>
        /// but then canceled before being fully <see cref="performed"/>.
        /// </summary>
        /// <see cref="InputActionPhase.Canceled"/>
        public event Action<CallbackContext> canceled
        {
            add => m_OnCanceled.Append(value);
            remove => m_OnCanceled.Remove(value);
        }

        /// <summary>
        /// Event that is triggered when the action has been fully performed.
        /// </summary>
        /// <see cref="InputActionPhase.Performed"/>
        public event Action<CallbackContext> performed
        {
            add => m_OnPerformed.Append(value);
            remove => m_OnPerformed.Remove(value);
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
        public InputAction(string name = null, string binding = null, string interactions = null, string processors = null, string expectedControlLayout = null)
            : this(name)
        {
            if (!string.IsNullOrEmpty(binding))
            {
                m_SingletonActionBindings = new[] {new InputBinding {path = binding, interactions = interactions, processors = processors, action = m_Name}};
                m_BindingsStartIndex = 0;
                m_BindingsCount = 1;
            }
            else
            {
                m_Interactions = interactions;
                m_Processors = processors;
            }

            m_ExpectedControlLayout = expectedControlLayout;
        }

        public void Dispose()
        {
            m_ActionMap?.m_State?.Dispose();
        }

        public override string ToString()
        {
            if (m_Name == null)
                return "<Unnamed>";

            ////REVIEW: should we cache this?
            if (m_ActionMap != null && !isSingletonAction && !String.IsNullOrEmpty(m_ActionMap.name))
                return $"{m_ActionMap.name}/{m_Name}";

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
        }

        public void Disable()
        {
            if (!enabled)
                return;

            m_ActionMap.m_State.DisableSingleAction(this);
        }

        ////REVIEW: right now the Clone() methods aren't overridable; do we want that?
        // If you clone an action from a set, you get a singleton action in return.
        public InputAction Clone()
        {
            var clone = new InputAction(name: m_Name)
            {
                m_SingletonActionBindings = bindings.ToArray(),
                m_BindingsCount = m_BindingsCount
            };
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        [Flags]
        internal enum ActionFlags
        {
            None = 0,
            Continuous = 1 << 1,
            PassThrough = 1 << 2,
            InitialStateCheck = 1 << 3,
        }

        ////REVIEW: it would be best if these were InternedStrings; however, for serialization, it has to be strings
        [Tooltip("Human readable name of the action. Must be unique within its action map (case is ignored). Can be changed "
            + "without breaking references to the action.")]
        [SerializeField] internal string m_Name;
        [Tooltip("Type of control expected by the action (e.g. \"Button\" or \"Stick\"). This will limit the controls shown "
            + "when setting up bindings in the UI and will also limit which controls can be bound interactively to the action.")]
        [SerializeField] internal string m_ExpectedControlLayout;
        [Tooltip("Unique ID of the action (GUID). Used to reference the action from bindings such that actions can be renamed "
            + "without breaking references.")]
        [SerializeField] internal string m_Id; // Can't serialize System.Guid and Unity's GUID is editor only.
        [SerializeField] internal ActionFlags m_Flags;
        [SerializeField] internal string m_Processors;
        [SerializeField] internal string m_Interactions;

        // For singleton actions, we serialize the bindings directly as part of the action.
        // For any other type of action, this is null.
        [SerializeField] internal InputBinding[] m_SingletonActionBindings;

        [NonSerialized] internal InputBinding? m_BindingMask;
        [NonSerialized] internal int m_BindingsStartIndex;
        [NonSerialized] internal int m_BindingsCount;
        [NonSerialized] internal int m_ControlStartIndex;
        [NonSerialized] internal int m_ControlCount;
        [NonSerialized] internal Guid m_Guid;

        /// <summary>
        /// Index of the action in the <see cref="InputActionState"/> associated with the
        /// action's <see cref="InputActionMap"/>.
        /// </summary>
        /// <remarks>
        /// This is not necessarily the same as the index of the action in its map.
        /// </remarks>
        /// <seealso cref="actionMap"/>
        [NonSerialized] internal int m_ActionIndex = InputActionState.kInvalidIndex;

        /// <summary>
        /// The action map that owns the action.
        /// </summary>
        /// <remarks>
        /// This is not serialized. The action map will restore this back references after deserialization.
        /// </remarks>
        [NonSerialized] internal InputActionMap m_ActionMap;

        // Listeners. No array allocations if only a single listener.
        [NonSerialized] internal InlinedArray<Action<CallbackContext>> m_OnStarted;
        [NonSerialized] internal InlinedArray<Action<CallbackContext>> m_OnCanceled;
        [NonSerialized] internal InlinedArray<Action<CallbackContext>> m_OnPerformed;

        /// <summary>
        /// Whether the action is a loose action created in code (e.g. as a property on a component).
        /// </summary>
        /// <remarks>
        /// Singleton actions are not contained in maps visible to the user. Internally, we do create
        /// a map for them that contains just the singleton action. To the action system, there are no
        /// actions without action maps.
        /// </remarks>
        internal bool isSingletonAction => m_ActionMap == null || ReferenceEquals(m_ActionMap.m_SingletonAction, this);

        private InputActionState.TriggerState currentState
        {
            get
            {
                if (m_ActionIndex == InputActionState.kInvalidIndex)
                    return new InputActionState.TriggerState();
                Debug.Assert(m_ActionMap != null);
                Debug.Assert(m_ActionMap.m_State != null);
                return m_ActionMap.m_State.FetchActionState(this);
            }
        }

        internal void MakeSureIdIsInPlace()
        {
            if (m_Guid != Guid.Empty)
                return;

            if (string.IsNullOrEmpty(m_Id))
            {
                GenerateId();
            }
            else
            {
                m_Guid = new Guid(m_Id);
            }
        }

        internal void GenerateId()
        {
            m_Guid = Guid.NewGuid();
            m_Id = m_Guid.ToString();
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
                    $"Cannot modify bindings on action '{this}' while the action is enabled");
            if (GetOrCreateActionMap().enabled)
                throw new InvalidOperationException(
                    $"Cannot modify bindings on action '{this}' while its action map is enabled");
        }

        internal int BindingIndexOnActionToBindingIndexOnMap(int indexOfBindingOnAction)
        {
            // We don't want to hit InputAction.bindings here as this requires setting up per-action
            // binding info which we then nuke as part of the override process. Calling ApplyBindingOverride
            // repeatedly with an index would thus cause the same data to be computed and thrown away
            // over and over.
            // Instead we manually search through the map's bindings to find the right binding index
            // in the map.

            var actionMap = GetOrCreateActionMap();
            var bindingsInMap = actionMap.m_Bindings;
            var bindingCountInMap = bindingsInMap.LengthSafe();
            var actionName = name;

            var currentBindingIndexOnAction = -1;
            for (var i = 0; i < bindingCountInMap; ++i)
            {
                ref var binding = ref bindingsInMap[i];

                // Match both name and ID on binding.
                if (string.Compare(binding.action, actionName, StringComparison.InvariantCultureIgnoreCase) != 0 &&
                    binding.action != m_Id)
                    continue;

                ++currentBindingIndexOnAction;
                if (currentBindingIndexOnAction == indexOfBindingOnAction)
                    return i;
            }

            throw new ArgumentOutOfRangeException(
                $"Binding index {indexOfBindingOnAction} is out of range for action '{this}' with {currentBindingIndexOnAction + 1} bindings",
                nameof(indexOfBindingOnAction));
        }

        /// <summary>
        /// Information provided to action callbacks about what triggered an action.
        /// </summary>
        /// <seealso cref="performed"/>
        /// <seealso cref="started"/>
        /// <seealso cref="canceled"/>
        /// <seealso cref="InputActionMap.actionTriggered"/>
        public struct CallbackContext
        {
            internal InputActionState m_State;
            internal int m_ActionIndex;

            internal int actionIndex => m_ActionIndex;
            internal unsafe int bindingIndex => m_State.actionStates[actionIndex].bindingIndex;
            internal unsafe int controlIndex => m_State.actionStates[actionIndex].controlIndex;
            internal unsafe int interactionIndex => m_State.actionStates[actionIndex].interactionIndex;

            public unsafe InputActionPhase phase
            {
                get
                {
                    if (m_State == null)
                        return InputActionPhase.Disabled;
                    return m_State.actionStates[actionIndex].phase;
                }
            }

            public bool started => phase == InputActionPhase.Started;

            public bool performed => phase == InputActionPhase.Performed;

            public bool canceled => phase == InputActionPhase.Canceled;

            /// <summary>
            /// The action that got triggered.
            /// </summary>
            public InputAction action => m_State?.GetActionOrNull(bindingIndex);

            /// <summary>
            /// The control that triggered the action.
            /// </summary>
            /// <remarks>
            /// In case of a composite binding, this is the control of the composite that activated the
            /// composite as a whole. For example, in case of a WASD-style binding, it could be the W key.
            /// </remarks>
            public InputControl control => m_State?.controls[controlIndex];

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
                    var index = interactionIndex;
                    if (index == InputActionState.kInvalidIndex)
                        return null;
                    return m_State.interactions[index];
                }
            }

            /// <summary>
            /// The time at which the action got triggered.
            /// </summary>
            /// <remarks>
            /// This is usually determined by the timestamp of the input event that activated a control
            /// bound to the action.
            /// </remarks>
            public unsafe double time
            {
                get
                {
                    if (m_State == null)
                        return 0;
                    return m_State.actionStates[actionIndex].time;
                }
            }

            /// <summary>
            /// Time at which the action was started.
            /// </summary>
            /// <remarks>
            /// This is only relevant for actions that go through distinct a <see cref="InputActionPhase.Started"/>
            /// cycle as driven by <see cref="IInputInteraction">interactions</see>.
            /// </remarks>
            public unsafe double startTime
            {
                get
                {
                    if (m_State == null)
                        return 0;
                    return m_State.actionStates[actionIndex].startTime;
                }
            }

            /// <summary>
            /// Time difference between <see cref="time"/> and <see cref="startTime"/>.
            /// </summary>
            public double duration => time - startTime;

            public Type valueType => m_State?.GetValueType(bindingIndex, controlIndex);

            public int valueSizeInBytes
            {
                get
                {
                    if (m_State == null)
                        return 0;

                    return m_State.GetValueSizeInBytes(bindingIndex, controlIndex);
                }
            }

            public unsafe void ReadValue(void* buffer, int bufferSize)
            {
                m_State?.ReadValue(bindingIndex, controlIndex, buffer, bufferSize);
            }

            public TValue ReadValue<TValue>()
                where TValue : struct
            {
                var value = default(TValue);
                if (m_State != null)
                    value = m_State.ReadValue<TValue>(bindingIndex, controlIndex);
                return value;
            }

            public object ReadValueAsObject()
            {
                return m_State?.ReadValueAsObject(bindingIndex, controlIndex);
            }

            ////TODO: really read previous value, not value from last frame
            /*
            public TValue ReadPreviousValue<TValue>()
            {
                throw new NotImplementedException();
            }
            */

            public override string ToString()
            {
                return $"{{ action={action} phase={phase} time={time} control={control} value={ReadValueAsObject()} interaction={interaction} }}";
            }
        }
    }
}
