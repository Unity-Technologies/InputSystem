using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;

////FIXME: Whether a control from a binding that's part of a composite appears on an action is currently not consistently enforced.
////       If it mentions the action, it appears on the action. Otherwise it doesn't. The controls should consistently appear on the
////       action based on what action the *composite* references.

////REVIEW: I think the action system as it is today offers too many ways to shoot yourself in the foot. It has
////        flexibility but at the same time has abundant opportunity for ending up with dysfunction. Common setups
////        have to come preconfigured and work robustly for the user without requiring much understanding of how
////        the system fits together.

////REVIEW: have single delegate instead of separate performed/started/canceled callbacks?

////REVIEW: Do we need to have separate display names for actions?

////TODO: allow changing bindings without having to disable

////REVIEW: what about having the concept of "consumed" on the callback context?

////REVIEW: have "Always Enabled" toggle on actions?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A named input signal that can flexibly decide which input data to tap.
    /// </summary>
    /// <remarks>
    /// An input action is an abstraction over the source of input(s) it receives. They are
    /// most useful for representing input as "logical" concepts (e.g. "jump") rather than
    /// as "physical" inputs (e.g. "space bar on keyboard pressed").
    ///
    /// In its most basic form, an action is simply an object along with a collection of
    /// bindings that trigger the action.
    ///
    /// <example>
    /// <code>
    /// // A simple action can be created directly using `new`. If desired, a binding
    /// // can be specified directly as part of construction.
    /// var action = new InputAction(binding: "&lt;Gamepad&gt;/buttonSouth");
    ///
    /// // Additional bindings can be added using `AddBinding`.
    /// action.AddBinding("&lt;Mouse&gt;/leftButton");
    /// </code>
    /// </example>
    ///
    /// Bindings use control path expressions to reference controls. See <see cref="InputBinding"/>
    /// for more details. There may be arbitrary many bindings targeting a single action. The
    /// list of bindings targeting an action can be obtained through <see cref="bindings"/>.
    ///
    /// By itself an action does not do anything until it is enabled:
    ///
    /// <example>
    /// <code>
    /// action.Enable();
    /// </code>
    /// </example>
    ///
    /// Once enabled, the action will actively monitor all controls on devices present
    /// in the system (see <see cref="InputSystem.devices"/>) that match any of the binding paths
    /// associated with the action. If you want to restrict the set of bindings used at runtime
    /// or restrict the set of devices which controls are chosen from, you can do so using
    /// <see cref="bindingMask"/> or, if the action is part of an <see cref="InputActionMap"/>,
    /// by setting the <see cref="InputActionMap.devices"/> property of the action map. The
    /// controls that an action uses can be queried using the <see cref="controls"/> property.
    ///
    /// When input is received on controls bound to an action, the action will trigger callbacks
    /// in response. These callbacks are <see cref="started"/>, <see cref="performed"/>, and
    /// <see cref="canceled"/>. The callbacks are triggered as part of input system updates
    /// (see <see cref="InputSystem.Update"/>), i.e. they happen before the respective
    /// <see cref="MonoBehaviour.Update"/> or <see cref="MonoBehaviour.FixedUpdate"/> methods
    /// get executed (depending on which <see cref="InputSettings.updateMode"/> the system is
    /// set to).
    ///
    /// In what order and how those callbacks get triggered depends on both the <see cref="type"/>
    /// of the action as well as on the interactions (see <see cref="IInputInteraction"/>) present
    /// on the bindings of the action. The default behavior is that when a control is actuated
    /// (i.e. moving away from its resting position), <see cref="started"/> is called and then
    /// <see cref="performed"/>. Subsequently, whenever the a control further changes value to
    /// anything other than its default value, <see cref="performed"/> will be called again.
    /// Finally, when the control moves back to its default value (i.e. resting position),
    /// <see cref="canceled"/> is called.
    ///
    /// To hook into the callbacks, there are several options available to you. The most obvious
    /// one is to hook directly into <see cref="started"/>, <see cref="performed"/>, and/or
    /// <see cref="canceled"/>. In these callbacks, you will receive a <see cref="CallbackContext"/>
    /// with information about how the action got triggered. For example, you can use <see
    /// cref="CallbackContext.ReadValue{TValue}"/> to read the value from the binding that triggered
    /// or use <see cref="CallbackContext.interaction"/> to find the interaction that is in progress.
    ///
    /// <example>
    /// <code>
    /// action.started += context => Debug.Log($"{context.action} started");
    /// action.performed += context => Debug.Log($"{context.action} performed");
    /// action.canceled += context => Debug.Log($"{context.action} canceled");
    /// </code>
    /// </example>
    ///
    /// Alternatively, you can use the <see cref="InputActionMap.actionTriggered"/> callback for
    /// actions that are part of an action map or the global <see cref="InputSystem.onActionChange"/>
    /// callback to globally listen for action activity. To simply record action activity instead
    /// of responding to it directly, you can use <see cref="InputActionTrace"/>.
    ///
    /// If you prefer to poll an action directly as part of your <see cref="MonoBehaviour.Update"/>
    /// or <see cref="MonoBehaviour.FixedUpdate"/> logic, you can do so using the <see cref="triggered"/>
    /// and <see cref="ReadValue{TValue}"/> methods.
    ///
    /// <example>
    /// <code>
    /// protected void Update()
    /// {
    ///     // For a button type action.
    ///     if (action.triggered)
    ///         /* ... */;
    ///
    ///     // For a value type action.
    ///     // (Vector2 is just an example; pick the value type that is the right
    ///     // one according to the bindings you have)
    ///     var v = action.ReadValue&lt;Vector2&gt;();
    /// }
    /// </code>
    /// </example>
    ///
    /// Note that actions are not generally frame-based. What this means is that an action
    /// will observe any value change on its connected controls, even if the control changes
    /// value multiple times in the same frame. In practice, this means that, for example,
    /// no button press will get missed.
    ///
    /// Please note that actions are a player-only feature. They are not supported in
    /// edit mode.
    /// </remarks>
    /// <seealso cref="InputActionMap"/>
    /// <seealso cref="InputActionAsset"/>
    /// <seealso cref="InputBinding"/>
    /// <seealso cref="InputSystem.ListEnabledActions()"/>
    [Serializable]
    public sealed class InputAction : ICloneable, IDisposable
    {
        /// <summary>
        /// Name of the action.
        /// </summary>
        /// <value>Plain-text name of the action.</value>
        /// <remarks>
        /// Can be null for anonymous actions created in code.
        ///
        /// If the action is part of an <see cref="InputActionMap"/>, it will have a name and the name
        /// will be unique in the set. The name is just the name of the action alone, not a "mapName/actionName"
        /// combination.
        ///
        /// The name should not contain slashes or dots but can contain spaces and punctuation.
        /// </remarks>
        public string name => m_Name;

        /// <summary>
        /// Behavior type of the action.
        /// </summary>
        /// <value>General behavior type of the action.</value>
        /// <remarks>
        /// Determines how the action gets triggered in response to control value changes.
        /// </remarks>
        public InputActionType type => m_Type;

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
        public string expectedControlType
        {
            get => m_ExpectedControlType;
            set => m_ExpectedControlType = value;
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

        /// <summary>
        /// The current phase of the action.
        /// </summary>
        /// <remarks>
        /// When listening for control input and when responding to control value changes,
        /// actions will go through several possible phases. TODO
        /// </remarks>
        public InputActionPhase phase => currentState.phase;

        /// <summary>
        /// Whether the action is currently enabled, i.e. responds to input, or not.
        /// </summary>
        /// <value>True if the action is currently enabled.</value>
        /// <remarks>
        /// An action is enabled by either calling <see cref="Enable"/> on it directly or by calling
        /// <see cref="InputActionMap.Enable"/> on the <see cref="InputActionMap"/> containing the action.
        /// When enabled, an action will listen for changes on the controls it is bound to and trigger
        /// callbacks such as <see cref="started"/>, <see cref="performed"/>, and <see cref="canceled"/>
        /// in response.
        /// </remarks>
        /// <seealso cref="Enable"/>
        /// <seealso cref="Disable"/>
        /// <seealso cref="InputActionMap.Enable"/>
        /// <seealso cref="InputActionMap.Disable"/>
        /// <seealso cref="InputSystem.ListEnabledActions()"/>
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

        /// <summary>
        /// Whether the action was triggered (i.e. had <see cref="performed"/> called) this frame.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="ReadValue{TValue}"/>, which will reset when the action goes back to waiting
        /// state, this property will stay true for the duration of the current frame (i.e. until the next
        /// <see cref="InputSystem.Update"/> runs) as long as the action was triggered at least once.
        ///
        /// <example>
        /// <code>
        /// if (myControls.gameplay.fire.triggered)
        ///     Fire();
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputActionType.Button"/>
        /// <seealso cref="ReadValue{TValue}"/>
        public unsafe bool triggered
        {
            get
            {
                var map = GetOrCreateActionMap();
                if (map.m_State == null)
                    return false;

                var lastTriggeredInUpdate = map.m_State.actionStates[m_ActionIndexInState].lastTriggeredInUpdate;
                return lastTriggeredInUpdate != 0 && lastTriggeredInUpdate == InputUpdate.s_UpdateStepCount;
            }
        }

        /// <summary>
        /// The currently active control that is driving the action. Null while the action
        /// is in waiting (<see cref="InputActionPhase.Waiting"/>) or canceled (<see cref="InputActionPhase.Canceled"/>)
        /// state. Otherwise the control that last had activity on it which wasn't ignored.
        /// </summary>
        /// <remarks>
        /// Note that the control's value does not necessarily correspond to the value of the
        /// action (<see cref="ReadValue{TValue}"/>) as the control may be part of a composite.
        /// </remarks>
        /// <seealso cref="CallbackContext.control"/>
        public unsafe InputControl activeControl
        {
            get
            {
                var state = GetOrCreateActionMap().m_State;
                if (state != null)
                {
                    var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                    var controlIndex = actionStatePtr->controlIndex;
                    if (controlIndex != InputActionState.kInvalidIndex)
                        return state.controls[controlIndex];
                }
                return null;
            }
        }

        /// <summary>
        /// Whether the action wants a state check on its bound controls as soon as it is enabled.
        /// </summary>
        internal bool wantsInitialStateCheck => type == InputActionType.Value;

        public InputAction()
        {
        }

        // Construct a disabled action targeting the given sources.
        // NOTE: This constructor is *not* used for actions added to sets. These are constructed
        //       by sets themselves.
        public InputAction(string name = null, InputActionType type = default, string binding = null, string interactions = null, string processors = null, string expectedControlType = null)
        {
            m_Name = name;
            m_Type = type;

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

            m_ExpectedControlType = expectedControlType;
        }

        public void Dispose()
        {
            m_ActionMap?.m_State?.Dispose();
        }

        public override string ToString()
        {
            string str;
            if (m_Name == null)
                str = "<Unnamed>";
            else if (m_ActionMap != null && !isSingletonAction && !string.IsNullOrEmpty(m_ActionMap.name))
                str = $"{m_ActionMap.name}/{m_Name}";
            else
                str = m_Name;

            var controls = this.controls;
            if (controls.Count > 0)
            {
                str += "[";
                var isFirst = true;
                foreach (var control in controls)
                {
                    if (!isFirst)
                        str += ",";
                    str += control.path;
                    isFirst = false;
                }
                str += "]";
            }

            return str;
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

        /// <summary>
        /// Read the current value of the action. This is the last value received on <see cref="started"/>,
        /// or <see cref="performed"/>. If the action is in canceled or waiting phase, returns default(TValue).
        /// </summary>
        /// <typeparam name="TValue">Value type to read. Must match the value type of the binding/control that triggered.</typeparam>
        /// <returns>The current value of the action or <c>default(TValue)</c> if the action is not currently in-progress.</returns>
        /// <remarks>
        /// This method can be used as an alternative to hooking into <see cref="started"/>, <see cref="performed"/>,
        /// and/or <see cref="canceled"/> and reading out the value using <see cref="CallbackContext.ReadValue{TValue}"/>
        /// there. Instead, this API acts more like a polling API that can be called, for example, as part of
        /// <see cref="MonoBehaviour.Update"/>.
        ///
        /// <example>
        /// <code>
        /// // Let's say you have a MyControls.inputactions file with "Generate C# Class" enabled
        /// // and it has an action map called "gameplay" with a "move" action of type Vector2.
        /// public class MyBehavior : MonoBehaviour
        /// {
        ///     public MyControls controls;
        ///     public float moveSpeed = 4;
        ///
        ///     protected void Awake()
        ///     {
        ///         controls = new MyControls();
        ///     }
        ///
        ///     protected void OnEnable()
        ///     {
        ///         controls.gameplay.Enable();
        ///     }
        ///
        ///     protected void OnDisable()
        ///     {
        ///         controls.gameplay.Disable();
        ///     }
        ///
        ///     protected void Update()
        ///     {
        ///         var moveVector = controls.gameplay.move.ReadValue&lt;Vector2&gt;() * (moveSpeed * Time.deltaTime);
        ///         //...
        ///     }
        /// }
        /// </code>
        /// </example>
        ///
        /// If the action has button-like behavior, then <see cref="triggered"/> is usually a better alternative to
        /// reading out a float and checking if it is above the button press point.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The given <typeparamref name="TValue"/> type does not match
        /// the value type of the control or composite currently driving the action.</exception>
        /// <seealso cref="triggered"/>
        /// <seealso cref="ReadValueAsObject"/>
        /// <seealso cref="CallbackContext.ReadValue{TValue}"/>
        public unsafe TValue ReadValue<TValue>()
            where TValue : struct
        {
            var result = default(TValue);

            var state = GetOrCreateActionMap().m_State;
            if (state != null)
            {
                var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                var controlIndex = actionStatePtr->controlIndex;
                if (controlIndex != InputActionState.kInvalidIndex)
                    result = state.ReadValue<TValue>(actionStatePtr->bindingIndex, controlIndex);
            }

            return result;
        }

        /// <summary>
        /// Same as <see cref="ReadValue{TValue}"/> but read the value without having to know the value type
        /// of the action.
        /// </summary>
        /// <returns>The current value of the action or null if the action is not currently in <see cref="InputActionPhase.Started"/>
        /// or <see cref="InputActionPhase.Performed"/> phase.</returns>
        /// <remarks>
        /// This method allocates GC memory and is thus not a good choice for getting called as part of gameplay
        /// logic.
        /// </remarks>
        /// <seealso cref="ReadValue{TValue}"/>
        public unsafe object ReadValueAsObject()
        {
            var state = GetOrCreateActionMap().m_State;
            if (state == null)
                return null;

            var actionStatePtr = &state.actionStates[m_ActionIndexInState];
            var controlIndex = actionStatePtr->controlIndex;
            if (controlIndex != InputActionState.kInvalidIndex)
                return state.ReadValueAsObject(actionStatePtr->bindingIndex, controlIndex);

            return null;
        }

        ////REVIEW: it would be best if these were InternedStrings; however, for serialization, it has to be strings
        [Tooltip("Human readable name of the action. Must be unique within its action map (case is ignored). Can be changed "
            + "without breaking references to the action.")]
        [SerializeField] internal string m_Name;
        [SerializeField] internal InputActionType m_Type;
        [FormerlySerializedAs("m_ExpectedControlLayout")]
        [Tooltip("Type of control expected by the action (e.g. \"Button\" or \"Stick\"). This will limit the controls shown "
            + "when setting up bindings in the UI and will also limit which controls can be bound interactively to the action.")]
        [SerializeField] internal string m_ExpectedControlType;
        [Tooltip("Unique ID of the action (GUID). Used to reference the action from bindings such that actions can be renamed "
            + "without breaking references.")]
        [SerializeField] internal string m_Id; // Can't serialize System.Guid and Unity's GUID is editor only.
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
        [NonSerialized] internal int m_ActionIndexInState = InputActionState.kInvalidIndex;

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
                if (m_ActionIndexInState == InputActionState.kInvalidIndex)
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

            throw new ArgumentOutOfRangeException(nameof(indexOfBindingOnAction),
                $"Binding index {indexOfBindingOnAction} is out of range for action '{this}' with {currentBindingIndexOnAction + 1} bindings");
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

            ////TODO: need ability to read as button

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

            public override string ToString()
            {
                return $"{{ action={action} phase={phase} time={time} control={control} value={ReadValueAsObject()} interaction={interaction} }}";
            }
        }
    }
}
