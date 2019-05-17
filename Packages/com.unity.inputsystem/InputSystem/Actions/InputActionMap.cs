using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Utilities;

////REVIEW: given we have the global ActionPerformed callback, do we really need the per-map callback?

////TODO: remove constraint of not being able to modify bindings while enabled from both actions and maps
////      (because of the sharing of state between multiple maps in an asset, we'd have to extend that constraint
////      to all maps in an asset in order to uphold it properly)

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A mapping of <see cref="InputBinding">input bindings</see> to <see cref="InputAction">
    /// input actions</see>.
    /// </summary>
    /// <remarks>
    /// Also stores data for actions. All actions have to have an associated
    /// action map. "Lose" actions constructed without a map will internally
    /// create their own map to hold their data.
    ///
    /// A common usage pattern for action maps is to use them to group action
    /// "contexts". So one map could hold "menu" actions, for example, whereas
    /// another set holds "gameplay" actions. This kind of splitting can be
    /// made arbitrarily complex. Like, you could have separate "driving" and
    /// "walking" action maps, for example, that you enable and disable depending
    /// on whether the player is walking or driving around.
    /// </remarks>
    [Serializable]
    public class InputActionMap : ICloneable, ISerializationCallbackReceiver, IInputActionCollection, IDisposable
    {
        /// <summary>
        /// Name of the action map.
        /// </summary>
        public string name => m_Name;

        /// <summary>
        /// If the action map is part of an asset, this refers to the asset. Otherwise it is <c>null</c>.
        /// </summary>
        public InputActionAsset asset => m_Asset;

        /// <summary>
        /// A stable, unique identifier for the map.
        /// </summary>
        /// <remarks>
        /// This can be used instead of the name to refer to the action map. Doing so allows referring to the
        /// map such that renaming it does not break references.
        /// </remarks>
        public Guid id
        {
            get
            {
                if (m_Guid == Guid.Empty)
                {
                    if (m_Id == null)
                    {
                        GenerateId();
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
        /// Whether any action in the map is currently enabled.
        /// </summary>
        public bool enabled => m_EnabledActionsCount > 0;

        /// <summary>
        /// List of actions contained in the map.
        /// </summary>
        /// <remarks>
        /// Actions are owned by their map. The same action cannot appear in multiple maps.
        ///
        /// Does not allocate. Note that values returned by the property become invalid if
        /// the setup of actions in a set is changed.
        /// </remarks>
        public ReadOnlyArray<InputAction> actions => new ReadOnlyArray<InputAction>(m_Actions);

        ////REVIEW: what about explicitly grouping bindings into named sets?

        /// <summary>
        /// List of bindings contained in the map.
        /// </summary>
        /// <remarks>
        /// <see cref="InputBinding">InputBindings</see> are owned by action maps and not by individual
        /// actions. The bindings in a map can form a tree and conceptually, this array represents a depth-first
        /// traversal of the tree.
        ///
        /// Bindings that trigger actions refer to the action by name.
        /// </remarks>
        public ReadOnlyArray<InputBinding> bindings => new ReadOnlyArray<InputBinding>(m_Bindings);

        public ReadOnlyArray<InputControlScheme> controlSchemes
        {
            get
            {
                if (m_Asset == null)
                    return new ReadOnlyArray<InputControlScheme>();
                return m_Asset.controlSchemes;
            }
        }

        /// <inheritdoc />
        public InputBinding? bindingMask
        {
            get => m_BindingMask;
            set
            {
                if (m_BindingMask == value)
                    return;

                m_BindingMask = value;
                LazyResolveBindings();
            }
        }

        /// <inheritdoc />
        public ReadOnlyArray<InputDevice>? devices
        {
            get
            {
                // Return asset's device list if we have none (only if we're part of an asset).
                if (m_Devices == null && asset != null)
                    return asset.devices;

                return m_Devices;
            }
            set
            {
                if (value == null)
                {
                    if (m_DevicesArray != null)
                        Array.Clear(m_DevicesArray, 0, m_DevicesCount);
                    m_DevicesCount = 0;
                    m_Devices = null;
                }
                else
                {
                    ArrayHelpers.Clear(m_DevicesArray, ref m_DevicesCount);
                    ArrayHelpers.AppendListWithCapacity(ref m_DevicesArray, ref m_DevicesCount, value.Value);
                    m_Devices = new ReadOnlyArray<InputDevice>(m_DevicesArray, 0, m_DevicesCount);
                }

                ////TODO: determine if this has *actually* changed things before firing off a re-resolve
                LazyResolveBindings();
            }
        }

        ////REVIEW: should this operate by binding path or by action name?
        public InputAction this[string actionNameOrId]
        {
            get
            {
                if (string.IsNullOrEmpty(actionNameOrId))
                    throw new ArgumentNullException(nameof(actionNameOrId));
                return GetAction(actionNameOrId);
            }
        }

        /// <summary>
        /// Add or remove a callback that is triggered when an action in the map changes its <see cref="InputActionPhase">
        /// phase</see>.
        /// </summary>
        /// <seealso cref="InputAction.started"/>
        /// <seealso cref="InputAction.performed"/>
        /// <seealso cref="InputAction.canceled"/>
        public event Action<InputAction.CallbackContext> actionTriggered
        {
            add => m_ActionCallbacks.AppendWithCapacity(value);
            remove => m_ActionCallbacks.RemoveByMovingTailWithCapacity(value); ////FIXME: Changes callback ordering.
        }

        public InputActionMap(string name = null)
        {
            m_Name = name;
        }

        public void Dispose()
        {
            m_State?.Dispose();
        }

        internal int TryGetActionIndex(string nameOrId)
        {
            ////REVIEW: have transient lookup table? worth optimizing this?
            ////   Ideally, this should at least be an InternedString comparison but due to serialization,
            ////   that's quite tricky.

            if (string.IsNullOrEmpty(nameOrId))
                return -1;

            if (m_Actions == null)
                return InputActionState.kInvalidIndex;
            var actionCount = m_Actions.Length;

            // If it contains hyphens, it may be a GUID so try looking up that way.
            if (nameOrId.Contains('-') && Guid.TryParse(nameOrId, out var id))
            {
                for (var i = 0; i < actionCount; ++i)
                    if (m_Actions[i].idDontGenerate == id)
                        return i;
            }

            // Default search goes by name (case insensitive).
            for (var i = 0; i < actionCount; ++i)
                if (string.Compare(m_Actions[i].m_Name, nameOrId, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return i;

            return InputActionState.kInvalidIndex;
        }

        private int TryGetActionIndex(Guid id)
        {
            if (m_Actions == null)
                return InputActionState.kInvalidIndex;
            var actionCount = m_Actions.Length;
            for (var i = 0; i < actionCount; ++i)
                if (m_Actions[i].idDontGenerate == id)
                    return i;

            return InputActionState.kInvalidIndex;
        }

        public InputAction TryGetAction(string nameOrId)
        {
            var index = TryGetActionIndex(nameOrId);
            if (index == -1)
                return null;
            return m_Actions[index];
        }

        public InputAction TryGetAction(Guid id)
        {
            var index = TryGetActionIndex(id);
            if (index == -1)
                return null;
            return m_Actions[index];
        }

        public InputAction GetAction(string nameOrId)
        {
            var action = TryGetAction(nameOrId);
            if (action == null)
                throw new KeyNotFoundException($"Could not find action '{nameOrId}' in map '{name}'");
            return action;
        }

        public InputAction GetAction(Guid id)
        {
            var action = TryGetAction(id);
            if (action == null)
                throw new KeyNotFoundException($"Could not find action with ID '{id}' in map '{name}'");
            return action;
        }

        public bool IsUsableWithDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            if (m_Bindings == null)
                return false;

            foreach (var binding in m_Bindings)
            {
                var path = binding.path;
                if (string.IsNullOrEmpty(path))
                    continue;

                if (InputControlPath.Matches(path, device))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Enable all the actions in the map.
        /// </summary>
        public void Enable()
        {
            if (m_Actions == null || m_EnabledActionsCount == m_Actions.Length)
                return;

            ResolveBindingsIfNecessary();
            m_State.EnableAllActions(this);
        }

        /// <summary>
        /// Disable all the actions in the map.
        /// </summary>
        public void Disable()
        {
            if (!enabled)
                return;

            m_State.DisableAllActions(this);
        }

        ////REVIEW: right now the Clone() methods aren't overridable; do we want that?
        public InputActionMap Clone()
        {
            // Internal action sets from singleton actions should not be visible outside of
            // them. Cloning them is not allowed.
            if (m_SingletonAction != null)
                throw new InvalidOperationException(
                    $"Cloning internal map of singleton action '{m_SingletonAction}' is not allowed");

            var clone = new InputActionMap
            {
                m_Name = m_Name,
            };

            // Clone actions.
            if (m_Actions != null)
            {
                var actionCount = m_Actions.Length;
                var actions = new InputAction[actionCount];
                for (var i = 0; i < actionCount; ++i)
                {
                    var original = m_Actions[i];
                    actions[i] = new InputAction
                    {
                        m_Name = original.m_Name,
                        m_ActionMap = clone,
                    };
                }
                clone.m_Actions = actions;
            }

            // Clone bindings.
            if (m_Bindings != null)
            {
                var bindingCount = m_Bindings.Length;
                var bindings = new InputBinding[bindingCount];
                Array.Copy(m_Bindings, 0, bindings, 0, bindingCount);
                clone.m_Bindings = bindings;
            }

            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public bool Contains(InputAction action)
        {
            if (action == null)
                return false;

            return action.actionMap == this;
        }

        public override string ToString()
        {
            if (m_Asset != null)
                return $"{m_Asset}:{m_Name}";
            if (!string.IsNullOrEmpty(m_Name))
                return m_Name;
            return "<Unnamed Action Map>";
        }

        /// <summary>
        /// Enumerate the actions in the map.
        /// </summary>
        /// <returns>An enumerator going over the actions in the map.</returns>
        /// <remarks>
        /// This method supports to generically iterate over the actions in a map. However, it will usually
        /// lead to GC allocation. Iterating directly over <see cref="actions"/> avoids allocating GC memory.
        /// </remarks>
        public IEnumerator<InputAction> GetEnumerator()
        {
            return actions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // The state we persist is pretty much just a name, a flat list of actions, and a flat
        // list of bindings. The rest is state we keep at runtime when a map is in use.

        [SerializeField] internal string m_Name;
        [SerializeField] internal string m_Id; // Can't serialize System.Guid and Unity's GUID is editor only.
        [SerializeField] internal InputActionAsset m_Asset;

        /// <summary>
        /// List of actions in this map.
        /// </summary>
        [SerializeField] internal InputAction[] m_Actions;

        /// <summary>
        /// List of bindings in this map.
        /// </summary>
        /// <remarks>
        /// For singleton actions, we ensure this is always the same as <see cref="InputAction.m_SingletonActionBindings"/>.
        /// </remarks>
        [SerializeField] internal InputBinding[] m_Bindings;

        // These fields are caches. If m_Bindings is modified, these are thrown away
        // and re-computed only if needed.
        // NOTE: Because InputBindings are structs, m_BindingsForEachAction actually duplicates each binding
        //       (only in the case where m_Bindings has scattered references to actions).
        ////REVIEW: this will lead to problems when overrides are thrown into the mix

        /// <summary>
        /// For each entry in <see cref="m_Actions"/>, a slice of this array corresponds to the
        /// action's bindings.
        /// </summary>
        /// <remarks>
        /// Ideally, this array is the same as <see cref="m_Bindings"/> (the same as in literally reusing the
        /// same array). However, we have no guarantee that <see cref="m_Bindings"/> is sorted by actions. In case it
        /// isn't, we create a separate array with the bindings sorted by action and have each action reference
        /// a slice through <see cref="InputAction.m_BindingsStartIndex"/> and <see cref="InputAction.m_BindingsCount"/>.
        /// </remarks>
        /// <seealso cref="SetUpPerActionCachedBindingData"/>
        [NonSerialized] private InputBinding[] m_BindingsForEachAction;

        [NonSerialized] private InputControl[] m_ControlsForEachAction;

        /// <summary>
        /// Number of actions currently enabled in the map.
        /// </summary>
        /// <remarks>
        /// This should only be written to by <see cref="InputActionState"/>.
        /// </remarks>
        [NonSerialized] internal int m_EnabledActionsCount;

        /// <summary>
        /// GUID converted from <see cref="m_Id"/>.
        /// </summary>
        [NonSerialized] private Guid m_Guid;

        // Action sets that are created internally by singleton actions to hold their data
        // are never exposed and never serialized so there is no point allocating an m_Actions
        // array.
        [NonSerialized] internal InputAction m_SingletonAction;

        [NonSerialized] internal int m_MapIndexInState = InputActionState.kInvalidIndex;

        /// <summary>
        /// Current execution state.
        /// </summary>
        /// <remarks>
        /// Initialized when map (or any action in it) is first enabled.
        /// </remarks>
        [NonSerialized] internal InputActionState m_State;
        [NonSerialized] private bool m_NeedToResolveBindings;
        [NonSerialized] internal InputBinding? m_BindingMask;

        [NonSerialized] private ReadOnlyArray<InputDevice>? m_Devices;
        [NonSerialized] private int m_DevicesCount;
        [NonSerialized] private InputDevice[] m_DevicesArray;

        [NonSerialized] internal InlinedArray<Action<InputAction.CallbackContext>> m_ActionCallbacks;

        /// <summary>
        /// Return the list of bindings for just the given actions.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        /// <remarks>
        /// The bindings for a single action may be contiguous in <see cref="m_Bindings"/> or may be scattered
        /// around. We don't keep persistent storage for these and instead set up a transient
        /// array if and when bindings are queried directly from an action. In the simple case,
        /// we don't even need a separate array but rather just need to find out which slice in the
        /// bindings array corresponds to which action.
        ///
        /// NOTE: Bindings for individual actions aren't queried by the system itself during normal
        ///       runtime operation so we only do this for cases where the user asks for the
        ///       information. If the user never asks for bindings or controls on a per-action basis,
        ///       none of this data gets initialized.
        /// </remarks>
        internal ReadOnlyArray<InputBinding> GetBindingsForSingleAction(InputAction action)
        {
            Debug.Assert(action != null, "Action cannot be null");
            Debug.Assert(action.m_ActionMap == this, "Action must be in action map");
            Debug.Assert(!action.isSingletonAction || m_SingletonAction == action, "Action is not a singleton action");

            // See if we need to refresh.
            if (m_BindingsForEachAction == null)
                SetUpPerActionCachedBindingData();

            return new ReadOnlyArray<InputBinding>(m_BindingsForEachAction, action.m_BindingsStartIndex,
                action.m_BindingsCount);
        }

        internal ReadOnlyArray<InputControl> GetControlsForSingleAction(InputAction action)
        {
            Debug.Assert(m_State != null);
            Debug.Assert(m_MapIndexInState != InputActionState.kInvalidIndex);
            Debug.Assert(m_Actions != null);
            Debug.Assert(action != null);
            Debug.Assert(action.m_ActionMap == this);
            Debug.Assert(!action.isSingletonAction || m_SingletonAction == action);

            if (m_ControlsForEachAction == null)
                SetUpPerActionCachedBindingData();

            return new ReadOnlyArray<InputControl>(m_ControlsForEachAction, action.m_ControlStartIndex,
                action.m_ControlCount);
        }

        /// <summary>
        /// Collect data from <see cref="m_Bindings"/> and <see cref="m_Actions"/> such that we can
        /// we can cleanly expose it from <see cref="InputAction.bindings"/> and <see cref="InputAction.controls"/>.
        /// </summary>
        /// <remarks>
        /// We set up per-action caches the first time their information is requested. Internally, we do not
        /// use those arrays and thus they will not get set up by default.
        ///
        /// Note that it is important to allow to call this method at a point where we have not resolved
        /// controls yet (i.e. <see cref="m_State"/> is <c>null</c>). Otherwise, using <see cref="InputAction.bindings"/>
        /// may trigger a control resolution which would be surprising.
        /// </remarks>
        private unsafe void SetUpPerActionCachedBindingData()
        {
            // Handle case where we don't have any bindings.
            if (m_Bindings == null)
                return;

            if (m_SingletonAction != null)
            {
                // Dead simple case: map is internally owned by action. The entire
                // list of bindings is specific to the action.

                Debug.Assert(m_Bindings == m_SingletonAction.m_SingletonActionBindings);

                m_BindingsForEachAction = m_Bindings;
                m_ControlsForEachAction = m_State?.controls;

                m_SingletonAction.m_BindingsStartIndex = 0;
                m_SingletonAction.m_BindingsCount = m_Bindings.Length;
                m_SingletonAction.m_ControlStartIndex = 0;
                m_SingletonAction.m_ControlCount = m_State?.totalControlCount ?? 0;
            }
            else
            {
                ////REVIEW: now that we have per-action binding information in UnmanagedMemory, this here can likely be done more easily

                // Go through all bindings and slice them out to individual actions.

                Debug.Assert(m_Actions != null); // Action isn't a singleton so this has to be true.
                var mapIndices = m_State?.FetchMapIndices(this) ?? new InputActionState.ActionMapIndices();

                // Reset state on each action. Important if we have actions that are no longer
                // referred to by bindings.
                for (var i = 0; i < m_Actions.Length; ++i)
                {
                    var action = m_Actions[i];
                    action.m_BindingsCount = 0;
                    action.m_BindingsStartIndex = -1;
                    action.m_ControlCount = 0;
                    action.m_ControlStartIndex = -1;
                }

                // Count bindings on each action.
                // After this loop, we can have one of two situations:
                // 1) The bindings for any action X start at some index N and occupy the next m_BindingsCount slots.
                // 2) The bindings for some or all actions are scattered across non-contiguous chunks of the array.
                var bindingCount = m_Bindings.Length;
                for (var i = 0; i < bindingCount; ++i)
                {
                    var action = TryGetAction(m_Bindings[i].action);
                    if (action != null)
                        ++action.m_BindingsCount;
                }

                // Collect the bindings and controls and bundle them into chunks.
                var newBindingsArrayIndex = 0;
                if (m_State != null && (m_ControlsForEachAction == null || m_ControlsForEachAction.Length != mapIndices.controlCount))
                {
                    if (mapIndices.controlCount == 0)
                        m_ControlsForEachAction = null;
                    else
                        m_ControlsForEachAction = new InputControl[mapIndices.controlCount];
                }
                InputBinding[] newBindingsArray = null;
                var currentControlIndex = 0;
                for (var currentBindingIndex = 0; currentBindingIndex < m_Bindings.Length;)
                {
                    var currentAction = TryGetAction(m_Bindings[currentBindingIndex].action);
                    if (currentAction == null || currentAction.m_BindingsStartIndex != -1)
                    {
                        // Skip bindings not targeting an action or bindings we have already processed
                        // (when gathering bindings for a single actions scattered across the array we may have
                        // skipping ahead).
                        ++currentBindingIndex;
                        continue;
                    }

                    // Bindings for current action start at current index.
                    currentAction.m_BindingsStartIndex = newBindingsArray != null
                        ? newBindingsArrayIndex
                        : currentBindingIndex;
                    currentAction.m_ControlStartIndex = currentControlIndex;

                    // Collect all bindings for the action. As part of that, also copy the controls
                    // for each binding over to m_ControlsForEachAction.
                    var bindingCountForCurrentAction = currentAction.m_BindingsCount;
                    Debug.Assert(bindingCountForCurrentAction > 0);
                    var sourceBindingToCopy = currentBindingIndex;
                    for (var i = 0; i < bindingCountForCurrentAction; ++i)
                    {
                        // See if we've come across a binding that doesn't belong to our currently looked at action.
                        if (TryGetAction(m_Bindings[sourceBindingToCopy].action) != currentAction)
                        {
                            // Yes, we have. Means the bindings for our actions are scattered in m_Bindings and
                            // we need to collect them.

                            // If this is the first action that has its bindings scattered around, switch to
                            // having a separate bindings array and copy whatever bindings we already processed
                            // over to it.
                            if (newBindingsArray == null)
                            {
                                newBindingsArray = new InputBinding[m_Bindings.Length];
                                newBindingsArrayIndex = sourceBindingToCopy;
                                Array.Copy(m_Bindings, 0, newBindingsArray, 0, sourceBindingToCopy);
                            }

                            // Find the next binding belonging to the action. We've counted bindings for
                            // the action in the previous pass so we know exactly how many bindings we
                            // can expect.
                            do
                            {
                                ++sourceBindingToCopy;
                                Debug.Assert(sourceBindingToCopy < m_Bindings.Length);
                            }
                            while (TryGetAction(m_Bindings[sourceBindingToCopy].action) != currentAction);
                        }
                        else if (currentBindingIndex == sourceBindingToCopy)
                            ++currentBindingIndex;

                        // Copy binding over to new bindings array, if need be.
                        if (newBindingsArray != null)
                            newBindingsArray[newBindingsArrayIndex++] = m_Bindings[sourceBindingToCopy];

                        // Copy controls for binding, if we have resolved controls already and if the
                        // binding isn't a composite (they refer to the controls from all of their part bindings
                        // but do not really resolve to controls themselves).
                        if (m_State != null && !m_Bindings[sourceBindingToCopy].isComposite)
                        {
                            var controlCountForBinding = m_State
                                .bindingStates[mapIndices.bindingStartIndex + sourceBindingToCopy].controlCount;
                            if (controlCountForBinding > 0)
                            {
                                Array.Copy(m_State.controls,
                                    m_State.bindingStates[mapIndices.bindingStartIndex + sourceBindingToCopy]
                                        .controlStartIndex,
                                    m_ControlsForEachAction, currentControlIndex, controlCountForBinding);

                                currentControlIndex += controlCountForBinding;
                                currentAction.m_ControlCount += controlCountForBinding;
                            }
                        }

                        ++sourceBindingToCopy;
                    }
                }

                if (newBindingsArray == null)
                {
                    // Bindings are already clustered by action in m_Bindings
                    // so we can just stick to having one array only.
                    m_BindingsForEachAction = m_Bindings;
                }
                else
                {
                    // Bindings are not clustered by action in m_Bindings so
                    // we had to allocate a separate array where the bindings are sorted.
                    m_BindingsForEachAction = newBindingsArray;
                }
            }
        }

        ////TODO: re-use allocations such that only grow the arrays and hit zero GC allocs when we already have enough memory
        internal void ClearPerActionCachedBindingData()
        {
            m_BindingsForEachAction = null;
            m_ControlsForEachAction = null;
        }

        ////TODO: allow this; just disable temporarily
        internal void ThrowIfModifyingBindingsIsNotAllowed()
        {
            if (enabled)
                throw new InvalidOperationException(
                    $"Cannot modify bindings on action map '{this}' while the map is enabled");
        }

        internal void GenerateId()
        {
            m_Guid = Guid.NewGuid();
            m_Id = m_Guid.ToString();
        }

        /// <summary>
        /// Resolve bindings right away if we have to. Otherwise defer it to when we next need
        /// the bindings.
        /// </summary>
        internal bool LazyResolveBindings()
        {
            // If we haven't had to resolve bindings yet, we can wait until when we
            // actually have to.
            if (m_State == null)
                return false;

            // If we don't have enabled actions, defer binding resolution to when we next
            // enable actions.
            if (m_EnabledActionsCount == 0)
            {
                m_NeedToResolveBindings = true;
                return false;
            }

            // Have to do it straight away.
            ResolveBindings();
            return true;
        }

        internal void ResolveBindingsIfNecessary()
        {
            // NOTE: We only check locally for the current map here. When there are multiple maps
            //       in an asset, we may have maps that require re-resolution while others don't.
            //       We only resolve if a map is used that needs resolution to happen. Note that
            //       this will still resolve bindings for *all* maps in the asset.

            if (m_State == null || m_NeedToResolveBindings)
                ResolveBindings();
        }

        /// <summary>
        /// Resolve all bindings to their controls and also add any action interactions
        /// from the bindings.
        /// </summary>
        /// <remarks>
        /// This is the core method of action binding resolution. All binding resolution goes through here.
        ///
        /// The best way is for binding resolution to happen once for each action map at the beginning of the game
        /// and to then enable and disable the maps as needed. However, the system will also re-resolve
        /// bindings if the control setup in the system changes (i.e. if devices are added or removed
        /// or if layouts in the system are changed).
        ///
        /// Bindings can be re-resolved while actions are enabled. This happens changing device or binding
        /// masks on action maps or assets (<see cref="devices"/>, <see cref="bindingMask"/>, <see cref="InputAction.bindingMask"/>,
        /// <see cref="InputActionAsset.devices"/>, <see cref="InputActionAsset.bindingMask"/>). When this happens,
        /// we temporarily disable and then reenable actions. Note that this is visible to observers.
        /// </remarks>
        internal void ResolveBindings()
        {
            // In case we have actions that are currently enabled, we temporarily retain the
            // UnmanagedMemory of our InputActionState so that we can sync action states after
            // we have re-resolved bindings.
            var tempMemory = new InputActionState.UnmanagedMemory();
            try
            {
                OneOrMore<InputActionMap, ReadOnlyArray<InputActionMap>> actionMaps;

                // Start resolving.
                var resolver = new InputBindingResolver();

                // If we're part of an asset, we share state and thus binding resolution with
                // all maps in the asset.
                if (m_Asset != null)
                {
                    actionMaps = m_Asset.actionMaps;
                    Debug.Assert(actionMaps.Count > 0, "Asset referred to by action map does not have action maps");

                    // If there's a binding mask set on the asset, apply it.
                    resolver.bindingMask = m_Asset.m_BindingMask;
                }
                else
                {
                    // Standalone action map (possibly a hidden one created for a singleton action).
                    // Gets its own private state.

                    actionMaps = this;
                }

                // If we already have a state, re-use the arrays we have already allocated.
                // NOTE: We will install the arrays on the very same InputActionState instance below. In the
                //       case where we didn't have to grow the arrays, we should end up with zero GC allocations
                //       here.
                var hasEnabledActions = false;
                if (m_State != null)
                {
                    // Grab a clone of the current memory. We clone because disabling all the actions
                    // in the map will alter the memory state and we want the state before we start
                    // touching it.
                    //
                    // Technically, ATM we only need the phase values in the action states but duplicating
                    // the unmanaged memory is cheap and avoids having to add yet more complication to the
                    // code paths here.
                    tempMemory = m_State.memory.Clone();

                    // If the state has enabled actions, temporarily disable them.
                    hasEnabledActions = m_State.HasEnabledActions();
                    for (var i = 0; i < actionMaps.Count; ++i)
                    {
                        var map = actionMaps[i];
                        if (hasEnabledActions)
                            m_State.DisableAllActions(map);

                        // Let listeners know we are about to modify bindings. Do this *after* we disabled the
                        // actions so that cancellations happen first.
                        if (map.m_SingletonAction != null)
                            InputActionState.NotifyListenersOfActionChange(InputActionChange.BoundControlsAboutToChange, map.m_SingletonAction);
                        else
                            InputActionState.NotifyListenersOfActionChange(InputActionChange.BoundControlsAboutToChange, map);
                    }

                    // Reuse the arrays we have so that we can avoid managed memory allocations, if possible.
                    resolver.StartWithArraysFrom(m_State);

                    // Throw away old memory.
                    m_State.memory.Dispose();
                }

                // Resolve all maps in the asset.
                for (var i = 0; i < actionMaps.Count; ++i)
                    resolver.AddActionMap(actionMaps[i]);

                // Install state.
                if (m_State == null)
                {
                    if (m_Asset != null)
                    {
                        var state = new InputActionState();
                        for (var i = 0; i < actionMaps.Count; ++i)
                            actionMaps[i].m_State = state;
                        m_Asset.m_SharedStateForAllMaps = state;
                    }
                    else
                    {
                        m_State = new InputActionState();
                    }
                    m_State.Initialize(resolver);
                }
                else
                {
                    m_State.ClaimDataFrom(resolver);
                }

                // Wipe caches.
                for (var i = 0; i < actionMaps.Count; ++i)
                {
                    var map = actionMaps[i];
                    map.m_NeedToResolveBindings = false;

                    ////TODO: determine whether we really need to wipe this; keep them if nothing has changed
                    map.m_ControlsForEachAction = null;

                    if (map.m_SingletonAction != null)
                        InputActionState.NotifyListenersOfActionChange(InputActionChange.BoundControlsChanged, map.m_SingletonAction);
                    else
                        InputActionState.NotifyListenersOfActionChange(InputActionChange.BoundControlsChanged, map);
                }

                // Re-enable actions.
                if (hasEnabledActions)
                    m_State.RestoreActionStates(tempMemory);
            }
            finally
            {
                tempMemory.Dispose();
            }
        }

        internal int FindBinding(InputBinding match)
        {
            var numBindings = m_Bindings.LengthSafe();
            for (var i = 0; i < numBindings; ++i)
            {
                ref var binding = ref m_Bindings[i];
                if (match.Matches(ref binding))
                    return i;
            }
            return -1;
        }

        #region Serialization

        // Action maps are serialized in two different ways. For storage as imported assets in Unity's Library/ folder
        // and in player data and asset bundles as well as for surviving domain reloads, InputActionMaps are serialized
        // directly by Unity. For storage as source data in user projects, InputActionMaps are serialized indirectly
        // as JSON by setting up a separate set of structs that are then read and written using Unity's JSON serializer.

        [Serializable]
        public struct BindingJson
        {
            public string name;
            public string id;
            public string path;
            public string interactions;
            public string processors;
            public string groups;
            public string action;
            ////TODO: re-enable when chained bindings are implemented
            //public bool chainWithPrevious;
            public bool isComposite;
            public bool isPartOfComposite;

            // This is for backwards compatibility with existing serialized action data as of 0.0.1-preview.
            // Ideally we should be able to nuke this before 1.0.
            public string modifiers;

            public InputBinding ToBinding()
            {
                return new InputBinding
                {
                    name = string.IsNullOrEmpty(name) ? null : name,
                    m_Id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id,
                    path = string.IsNullOrEmpty(path) ? null : path,
                    action = string.IsNullOrEmpty(action) ? null : action,
                    interactions = string.IsNullOrEmpty(interactions) ? (!string.IsNullOrEmpty(modifiers) ? modifiers : null) : interactions,
                    processors = string.IsNullOrEmpty(processors) ? null : processors,
                    groups = string.IsNullOrEmpty(groups) ? null : groups,
                    //chainWithPrevious = chainWithPrevious,
                    isComposite = isComposite,
                    isPartOfComposite = isPartOfComposite,
                };
            }

            public static BindingJson FromBinding(InputBinding binding)
            {
                return new BindingJson
                {
                    name = binding.name,
                    id = string.IsNullOrEmpty(binding.m_Id) ? Guid.NewGuid().ToString() : binding.m_Id,
                    path = binding.path,
                    action = binding.action,
                    interactions = binding.interactions,
                    processors = binding.processors,
                    groups = binding.groups,
                    //chainWithPrevious = binding.chainWithPrevious,
                    isComposite = binding.isComposite,
                    isPartOfComposite = binding.isPartOfComposite,
                };
            }
        }

        [Serializable]
        internal struct ActionJson
        {
            public string name;
            public string id;
            public string expectedControlLayout;
            public bool continuous;
            public bool passThrough;
            public bool initialStateCheck;
            public string processors;
            public string interactions;

            // Bindings can either be on the action itself (in which case the action name
            // for each binding is implied) or listed separately in the action file.
            public BindingJson[] bindings;

            public static ActionJson FromAction(InputAction action)
            {
                // Bindings don't go on the actions when we write them.
                return new ActionJson
                {
                    name = action.m_Name,
                    id = action.id.ToString(),
                    expectedControlLayout = action.m_ExpectedControlLayout,
                    continuous = action.continuous,
                    passThrough = action.passThrough,
                    initialStateCheck = action.initialStateCheck,
                    processors = action.processors,
                    interactions = action.interactions,
                };
            }
        }

        [Serializable]
        internal struct MapJson
        {
            public string name;
            public string id;
            public ActionJson[] actions;
            public BindingJson[] bindings;

            public static MapJson FromMap(InputActionMap map)
            {
                ActionJson[] jsonActions = null;
                BindingJson[] jsonBindings = null;

                var actions = map.m_Actions;
                if (actions != null)
                {
                    var actionCount = actions.Length;
                    jsonActions = new ActionJson[actionCount];

                    for (var i = 0; i < actionCount; ++i)
                        jsonActions[i] = ActionJson.FromAction(actions[i]);
                }

                var bindings = map.m_Bindings;
                if (bindings != null)
                {
                    var bindingCount = bindings.Length;
                    jsonBindings = new BindingJson[bindingCount];

                    for (var i = 0; i < bindingCount; ++i)
                        jsonBindings[i] = BindingJson.FromBinding(bindings[i]);
                }

                return new MapJson
                {
                    name = map.name,
                    id = map.id.ToString(),
                    actions = jsonActions,
                    bindings = jsonBindings,
                };
            }
        }

        // We write JSON in a less flexible format than we allow to be read. JSON files
        // we read can just be flat lists of actions with the map name being contained in
        // the action name and containing their own bindings directly. JSON files we write
        // go map by map and separate bindings and actions.
        [Serializable]
        internal struct WriteFileJson
        {
            public MapJson[] maps;

            public static WriteFileJson FromMap(InputActionMap map)
            {
                return new WriteFileJson
                {
                    maps = new[] {MapJson.FromMap(map)}
                };
            }

            public static WriteFileJson FromMaps(IEnumerable<InputActionMap> maps)
            {
                var mapCount = maps.Count();
                if (mapCount == 0)
                    return new WriteFileJson();

                var mapsJson = new MapJson[mapCount];
                var index = 0;
                foreach (var map in maps)
                    mapsJson[index++] = MapJson.FromMap(map);

                return new WriteFileJson {maps = mapsJson};
            }
        }

        // A JSON representation of one or more sets of actions.
        // Contains a list of actions. Each action may specify the set it belongs to
        // as part of its name ("set/action").
        [Serializable]
        internal struct ReadFileJson
        {
            public ActionJson[] actions;
            public MapJson[] maps;

            public InputActionMap[] ToMaps()
            {
                var mapList = new List<InputActionMap>();
                var actionLists = new List<List<InputAction>>();
                var bindingLists = new List<List<InputBinding>>();

                // Process actions listed at toplevel.
                var actionCount = actions?.Length ?? 0;
                for (var i = 0; i < actionCount; ++i)
                {
                    var jsonAction = actions[i];

                    if (string.IsNullOrEmpty(jsonAction.name))
                        throw new Exception($"Action number {i + 1} has no name");

                    ////REVIEW: make sure all action names are unique?

                    // Determine name of action map.
                    string mapName = null;
                    var actionName = jsonAction.name;
                    var indexOfFirstSlash = actionName.IndexOf('/');
                    if (indexOfFirstSlash != -1)
                    {
                        mapName = actionName.Substring(0, indexOfFirstSlash);
                        actionName = actionName.Substring(indexOfFirstSlash + 1);

                        if (string.IsNullOrEmpty(actionName))
                            throw new Exception(
                                $"Invalid action name '{jsonAction.name}' (missing action name after '/')");
                    }

                    // Try to find existing map.
                    InputActionMap map = null;
                    var mapIndex = 0;
                    for (; mapIndex < mapList.Count; ++mapIndex)
                    {
                        if (string.Compare(mapList[mapIndex].name, mapName, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            map = mapList[mapIndex];
                            break;
                        }
                    }

                    // Create new map if it's the first action in the map.
                    if (map == null)
                    {
                        // NOTE: No map IDs supported on this path.
                        map = new InputActionMap(mapName);
                        mapIndex = mapList.Count;
                        mapList.Add(map);
                        actionLists.Add(new List<InputAction>());
                        bindingLists.Add(new List<InputBinding>());
                    }

                    // Create action.
                    var action = new InputAction(actionName)
                    {
                        m_Id = string.IsNullOrEmpty(jsonAction.id) ? null : jsonAction.id,
                        m_ExpectedControlLayout = !string.IsNullOrEmpty(jsonAction.expectedControlLayout)
                            ? jsonAction.expectedControlLayout
                            : null,
                        continuous = jsonAction.continuous,
                        passThrough = jsonAction.passThrough,
                        initialStateCheck = jsonAction.initialStateCheck,
                        m_Processors = jsonAction.processors,
                        m_Interactions = jsonAction.interactions,
                    };
                    actionLists[mapIndex].Add(action);

                    // Add bindings.
                    if (jsonAction.bindings != null)
                    {
                        var bindingsForMap = bindingLists[mapIndex];
                        for (var n = 0; n < jsonAction.bindings.Length; ++n)
                        {
                            var jsonBinding = jsonAction.bindings[n];
                            var binding = jsonBinding.ToBinding();
                            binding.action = action.m_Name;
                            bindingsForMap.Add(binding);
                        }
                    }
                }

                // Process maps.
                var mapCount = maps?.Length ?? 0;
                for (var i = 0; i < mapCount; ++i)
                {
                    var jsonMap = maps[i];

                    var mapName = jsonMap.name;
                    if (string.IsNullOrEmpty(mapName))
                        throw new Exception($"Map number {i + 1} has no name");

                    // Try to find existing map.
                    InputActionMap map = null;
                    var mapIndex = 0;
                    for (; mapIndex < mapList.Count; ++mapIndex)
                    {
                        if (string.Compare(mapList[mapIndex].name, mapName, StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            map = mapList[mapIndex];
                            break;
                        }
                    }

                    // Create new map if we haven't seen it before.
                    if (map == null)
                    {
                        map = new InputActionMap(mapName)
                        {
                            m_Id = string.IsNullOrEmpty(jsonMap.id) ? null : jsonMap.id
                        };
                        mapIndex = mapList.Count;
                        mapList.Add(map);
                        actionLists.Add(new List<InputAction>());
                        bindingLists.Add(new List<InputBinding>());
                    }

                    // Process actions in map.
                    var actionCountInMap = jsonMap.actions?.Length ?? 0;
                    for (var n = 0; n < actionCountInMap; ++n)
                    {
                        var jsonAction = jsonMap.actions[n];

                        if (string.IsNullOrEmpty(jsonAction.name))
                            throw new Exception($"Action number {i + 1} in map '{mapName}' has no name");

                        // Create action.
                        var action = new InputAction(jsonAction.name)
                        {
                            m_Id = string.IsNullOrEmpty(jsonAction.id) ? null : jsonAction.id,
                            m_ExpectedControlLayout = !string.IsNullOrEmpty(jsonAction.expectedControlLayout)
                                ? jsonAction.expectedControlLayout
                                : null,
                            continuous = jsonAction.continuous,
                            passThrough = jsonAction.passThrough,
                            initialStateCheck = jsonAction.initialStateCheck,
                            m_Processors = jsonAction.processors,
                            m_Interactions = jsonAction.interactions,
                        };
                        actionLists[mapIndex].Add(action);

                        // Add bindings.
                        if (jsonAction.bindings != null)
                        {
                            var bindingList = bindingLists[mapIndex];
                            for (var k = 0; k < jsonAction.bindings.Length; ++k)
                            {
                                var jsonBinding = jsonAction.bindings[k];
                                var binding = jsonBinding.ToBinding();
                                binding.action = action.m_Name;
                                bindingList.Add(binding);
                            }
                        }
                    }

                    // Process bindings in map.
                    var bindingCountInMap = jsonMap.bindings?.Length ?? 0;
                    var bindingsForMap = bindingLists[mapIndex];
                    for (var n = 0; n < bindingCountInMap; ++n)
                    {
                        var jsonBinding = jsonMap.bindings[n];
                        var binding = jsonBinding.ToBinding();
                        bindingsForMap.Add(binding);
                    }
                }

                // Finalize arrays.
                for (var i = 0; i < mapList.Count; ++i)
                {
                    var map = mapList[i];

                    var actionArray = actionLists[i].ToArray();
                    var bindingArray = bindingLists[i].ToArray();

                    map.m_Actions = actionArray;
                    map.m_Bindings = bindingArray;

                    for (var n = 0; n < actionArray.Length; ++n)
                    {
                        var action = actionArray[n];
                        action.m_ActionMap = map;
                    }
                }

                return mapList.ToArray();
            }
        }

        // Load one or more action sets from JSON.
        public static InputActionMap[] FromJson(string json)
        {
            var fileJson = JsonUtility.FromJson<ReadFileJson>(json);
            return fileJson.ToMaps();
        }

        public static string ToJson(IEnumerable<InputActionMap> sets)
        {
            var fileJson = WriteFileJson.FromMaps(sets);
            return JsonUtility.ToJson(fileJson, true);
        }

        public string ToJson()
        {
            var fileJson = WriteFileJson.FromMap(this);
            return JsonUtility.ToJson(fileJson, true);
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            m_State = null;
            m_MapIndexInState = InputActionState.kInvalidIndex;

            // Restore references of actions linking back to us.
            if (m_Actions != null)
            {
                var actionCount = m_Actions.Length;
                for (var i = 0; i < actionCount; ++i)
                    m_Actions[i].m_ActionMap = this;
            }

            // Make sure we don't retain any cached per-action data when using serialization
            // to doctor around in action map configurations in the editor.
            ClearPerActionCachedBindingData();
        }

        #endregion
    }
}
