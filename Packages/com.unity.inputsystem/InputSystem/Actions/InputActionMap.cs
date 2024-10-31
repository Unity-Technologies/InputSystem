using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine.InputSystem.Utilities;

////REVIEW: given we have the global ActionPerformed callback, do we really need the per-map callback?

////TODO: remove constraint of not being able to modify bindings while enabled from both actions and maps
////      (because of the sharing of state between multiple maps in an asset, we'd have to extend that constraint
////      to all maps in an asset in order to uphold it properly)

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A mechanism for collecting a series of input actions (see <see cref="InputAction"/>)
    /// and treating them as a group.
    /// </summary>
    /// <remarks>
    /// Each action map is a named collection of bindings and actions. Both are stored
    /// as a flat list. The bindings are available through the <see cref="bindings"/>
    /// property and the actions are available through the <see cref="actions"/> property.
    ///
    /// The actions in a map are owned by the map. No action can appear in two maps
    /// at the same time. To find the action map an action belongs to, use the
    /// <see cref="InputAction.actionMap"/> property. Note that actions can also stand
    /// on their own and thus do not necessarily need to belong to a map (in which case
    /// the <see cref="InputAction.actionMap"/> property is <c>null</c>).
    ///
    /// Within a map, all actions have to have names and each action name must
    /// be unique. The <see cref="InputBinding.action"/> property of bindings in a map
    /// are resolved within the <see cref="actions"/> in the map. Looking up actions
    /// by name can be done through <see cref="FindAction(string,bool)"/>.
    ///
    /// The <see cref="name"/> of the map itself can be empty, except if the map is part of
    /// an <see cref="InputActionAsset"/> in which case it is required to have a name
    /// which also must be unique within the asset.
    ///
    /// Action maps are most useful for grouping actions that contextually
    /// belong together. For example, one common usage is to separate the actions
    /// that can be performed in the UI or in the main menu from those that can
    /// be performed during gameplay. However, even within gameplay, multiple action
    /// maps can be employed. For example, one could have different action maps for
    /// driving and for walking plus one more map for the actions shared between
    /// the two modes.
    ///
    /// Action maps are usually created in the <a href="../manual/ActionAssets.html">action
    /// editor</a> as part of <see cref="InputActionAsset"/>s. However, they can also be
    /// created standing on their own directly in code or from JSON (see <see cref="FromJson"/>).
    ///
    /// <example>
    /// <code>
    /// // Create a free-standing action map.
    /// var map = new InputActionMap();
    ///
    /// // Add some actions and bindings to it.
    /// map.AddAction("action1", binding: "&lt;Keyboard&gt;/space");
    /// map.AddAction("action2", binding: "&lt;Gamepad&gt;/buttonSouth");
    /// </code>
    /// </example>
    ///
    /// Actions in action maps, like actions existing by themselves outside of action
    /// maps, do not actively process input except if enabled. Actions can either
    /// be enabled individually (see <see cref="InputAction.Enable"/> and <see
    /// cref="InputAction.Disable"/>) or in bulk by enabling and disabling the
    /// entire map (see <see cref="Enable"/> and <see cref="Disable"/>).
    /// </remarks>
    /// <seealso cref="InputActionAsset"/>
    /// <seealso cref="InputAction"/>
    [Serializable]
    public sealed class InputActionMap : ICloneable, ISerializationCallbackReceiver, IInputActionCollection2, IDisposable
    {
        /// <summary>
        /// Name of the action map.
        /// </summary>
        /// <value>Name of the action map.</value>
        /// <remarks>
        /// For action maps that are part of <see cref="InputActionAsset"/>s, this will always be
        /// a non-null, non-empty string that is unique within the maps in the asset. For action maps
        /// that are standing on their own, this can be null or empty.
        /// </remarks>
        public string name => m_Name;

        /// <summary>
        /// If the action map is part of an asset, this refers to the asset. Otherwise it is <c>null</c>.
        /// </summary>
        /// <value>Asset to which the action map belongs.</value>
        public InputActionAsset asset => m_Asset;

        /// <summary>
        /// A stable, unique identifier for the map.
        /// </summary>
        /// <value>Unique ID for the action map.</value>
        /// <remarks>
        /// This can be used instead of the name to refer to the action map. Doing so allows referring to the
        /// map such that renaming it does not break references.
        /// </remarks>
        /// <seealso cref="InputAction.id"/>
        public Guid id
        {
            get
            {
                if (string.IsNullOrEmpty(m_Id))
                    GenerateId();
                return new Guid(m_Id);
            }
        }

        internal Guid idDontGenerate
        {
            get
            {
                if (string.IsNullOrEmpty(m_Id))
                    return default;
                return new Guid(m_Id);
            }
        }

        /// <summary>
        /// Whether any action in the map is currently enabled.
        /// </summary>
        /// <value>True if any action in <see cref="actions"/> is currently enabled.</value>
        /// <seealso cref="InputAction.enabled"/>
        /// <seealso cref="Enable"/>
        /// <seealso cref="InputAction.Enable"/>
        public bool enabled => m_EnabledActionsCount > 0;

        /// <summary>
        /// List of actions contained in the map.
        /// </summary>
        /// <value>Collection of actions belonging to the map.</value>
        /// <remarks>
        /// Actions are owned by their map. The same action cannot appear in multiple maps.
        ///
        /// Accessing this property. Note that values returned by the property become invalid if
        /// the setup of actions in a map is changed.
        /// </remarks>
        /// <seealso cref="InputAction.actionMap"/>
        public ReadOnlyArray<InputAction> actions => new ReadOnlyArray<InputAction>(m_Actions);

        /// <summary>
        /// List of bindings contained in the map.
        /// </summary>
        /// <value>Collection of bindings in the map.</value>
        /// <remarks>
        /// <see cref="InputBinding"/>s are owned by action maps and not by individual actions.
        ///
        /// Bindings that trigger actions refer to the action by <see cref="InputAction.name"/>
        /// or <see cref="InputAction.id"/>.
        ///
        /// Accessing this property does not allocate. Note that values returned by the property
        /// become invalid if the setup of bindings in a map is changed.
        /// </remarks>
        /// <seealso cref="InputAction.bindings"/>
        public ReadOnlyArray<InputBinding> bindings => new ReadOnlyArray<InputBinding>(m_Bindings);

        IEnumerable<InputBinding> IInputActionCollection2.bindings => bindings;

        /// <summary>
        /// Control schemes defined for the action map.
        /// </summary>
        /// <value>List of available control schemes.</value>
        /// <remarks>
        /// Control schemes can only be defined at the level of <see cref="InputActionAsset"/>s.
        /// For action maps that are part of assets, this property will return the control schemes
        /// from the asset. For free-standing action maps, this will return an empty list.
        /// </remarks>
        /// <seealso cref="InputActionAsset.controlSchemes"/>
        public ReadOnlyArray<InputControlScheme> controlSchemes
        {
            get
            {
                if (m_Asset == null)
                    return new ReadOnlyArray<InputControlScheme>();
                return m_Asset.controlSchemes;
            }
        }

        /// <summary>
        /// Binding mask to apply to all actions in the asset.
        /// </summary>
        /// <value>Optional mask that determines which bindings in the action map to enable.</value>
        /// <remarks>
        /// Binding masks can be applied at three different levels: for an entire asset through
        /// <see cref="InputActionAsset.bindingMask"/>, for a specific map through this property,
        /// and for single actions through <see cref="InputAction.bindingMask"/>. By default,
        /// none of the masks will be set (that is, they will be <c>null</c>).
        ///
        /// When an action is enabled, all the binding masks that apply to it are taken into
        /// account. Specifically, this means that any given binding on the action will be
        /// enabled only if it matches the mask applied to the asset, the mask applied
        /// to the map that contains the action, and the mask applied to the action itself.
        /// All the masks are individually optional.
        ///
        /// Masks are matched against bindings using <see cref="InputBinding.Matches"/>.
        ///
        /// Note that if you modify the masks applicable to an action while it is
        /// enabled, the action's <see cref="InputAction.controls"/> will get updated immediately to
        /// respect the mask. To avoid repeated binding resolution, it is most efficient
        /// to apply binding masks before enabling actions.
        ///
        /// Binding masks are non-destructive. All the bindings on the action are left
        /// in place. Setting a mask will not affect the value of the <see cref="InputAction.bindings"/>
        /// and <see cref="bindings"/> properties.
        /// </remarks>
        /// <seealso cref="InputBinding.MaskByGroup"/>
        /// <seealso cref="InputAction.bindingMask"/>
        /// <seealso cref="InputActionAsset.bindingMask"/>
        public InputBinding? bindingMask
        {
            get => m_BindingMask;
            set
            {
                if (m_BindingMask == value)
                    return;

                m_BindingMask = value;
                LazyResolveBindings(fullResolve: true);
            }
        }

        /// <summary>
        /// Set of devices that bindings in the action map can bind to.
        /// </summary>
        /// <value>Optional set of devices to use by bindings in the map.</value>
        /// <remarks>
        /// By default (with this property being <c>null</c>), bindings will bind to any of the
        /// controls available through <see cref="InputSystem.devices"/>, that is, controls from all
        /// devices in the system will be used.
        ///
        /// By setting this property, binding resolution can instead be restricted to just specific
        /// devices. This restriction can either be applied to an entire asset using <see
        /// cref="InputActionMap.devices"/> or to specific action maps by using this property. Note that
        /// if both this property and <see cref="InputActionAsset.devices"/> is set for a specific action
        /// map, the list of devices on the action map will take precedence and the list on the
        /// asset will be ignored for bindings in that action map.
        ///
        /// <example>
        /// <code>
        /// // Create an action map containing a single action with a gamepad binding.
        /// var actionMap = new InputActionMap();
        /// var fireAction = actionMap.AddAction("Fire", binding: "&lt;Gamepad&gt;/buttonSouth");
        /// asset.AddActionMap(actionMap);
        ///
        /// // Let's assume we have two gamepads connected. If we enable the
        /// // action map now, the 'Fire' action will bind to both.
        /// actionMap.Enable();
        ///
        /// // This will print two controls.
        /// Debug.Log(string.Join("\n", fireAction.controls));
        ///
        /// // To restrict the setup to just the first gamepad, we can assign
        /// // to the 'devices' property.
        /// actionMap.devices = new InputDevice[] { Gamepad.all[0] };
        ///
        /// // Now this will print only one control.
        /// Debug.Log(string.Join("\n", fireAction.controls));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputActionAsset.devices"/>
        public ReadOnlyArray<InputDevice>? devices
        {
            // Return asset's device list if we have none (only if we're part of an asset).
            get => m_Devices.Get() ?? m_Asset?.devices;
            set
            {
                if (m_Devices.Set(value))
                    LazyResolveBindings(fullResolve: false);
            }
        }

        /// <summary>
        /// Look up an action by name or ID.
        /// </summary>
        /// <param name="actionNameOrId">Name (as in <see cref="InputAction.name"/>) or ID (as in <see cref="InputAction.id"/>)
        /// of the action. Note that matching of names is case-insensitive.</param>
        /// <exception cref="ArgumentNullException"><paramref name="actionNameOrId"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException">No action with the name or ID of <paramref name="actionNameOrId"/>
        /// was found in the action map.</exception>
        /// <remarks>
        /// This method is equivalent to <see cref="FindAction(string,bool)"/> except it throws <c>KeyNotFoundException</c>
        /// if no action with the given name or ID can be found.
        /// </remarks>
        /// <seealso cref="FindAction(string,bool)"/>
        /// <seealso cref="FindAction(Guid)"/>
        /// <see cref="actions"/>
        public InputAction this[string actionNameOrId]
        {
            get
            {
                if (actionNameOrId == null)
                    throw new ArgumentNullException(nameof(actionNameOrId));
                var action = FindAction(actionNameOrId);
                if (action == null)
                    throw new KeyNotFoundException($"Cannot find action '{actionNameOrId}'");
                return action;
            }
        }

        ////REVIEW: inconsistent naming; elsewhere we use "onActionTriggered" (which in turn is inconsistent with InputAction.started etc)
        /// <summary>
        /// Add or remove a callback that is triggered when an action in the map changes its <see cref="InputActionPhase">
        /// phase</see>.
        /// </summary>
        /// <seealso cref="InputAction.started"/>
        /// <seealso cref="InputAction.performed"/>
        /// <seealso cref="InputAction.canceled"/>
        public event Action<InputAction.CallbackContext> actionTriggered
        {
            add => m_ActionCallbacks.AddCallback(value);
            remove => m_ActionCallbacks.RemoveCallback(value);
        }

        /// <summary>
        /// Construct an action map with default values.
        /// </summary>
        public InputActionMap()
        {
            s_NeedToResolveBindings = true;
        }

        /// <summary>
        /// Construct an action map with the given name.
        /// </summary>
        /// <param name="name">Name to give to the action map. By default <c>null</c>, i.e. does
        /// not assign a name to the map.</param>
        public InputActionMap(string name)
            : this()
        {
            m_Name = name;
        }

        /// <summary>
        /// Release internal state held on to by the action map.
        /// </summary>
        /// <remarks>
        /// Once actions in a map are enabled, the map will allocate a block of state internally that
        /// it will hold on to until disposed of. All actions in the map will share the same internal
        /// state. Also, if the map is part of an <see cref="InputActionAsset"/> all maps and actions
        /// in the same asset will share the same internal state.
        ///
        /// Note that the internal state holds on to GC heap memory as well as memory from the
        /// unmanaged, C++ heap.
        /// </remarks>
        public void Dispose()
        {
            m_State?.Dispose();
        }

        internal int FindActionIndex(string nameOrId)
        {
            ////REVIEW: have transient lookup table? worth optimizing this?
            ////   Ideally, this should at least be an InternedString comparison but due to serialization,
            ////   that's quite tricky.

            if (string.IsNullOrEmpty(nameOrId))
                return -1;
            if (m_Actions == null)
                return -1;

            // First time we hit this method, we populate the lookup table.
            SetUpActionLookupTable();

            var actionCount = m_Actions.Length;

            var isOldBracedFormat = nameOrId.StartsWith("{") && nameOrId.EndsWith("}");
            if (isOldBracedFormat)
            {
                var length = nameOrId.Length - 2;
                for (var i = 0; i < actionCount; ++i)
                {
                    if (string.Compare(m_Actions[i].m_Id, 0, nameOrId, 1, length) == 0)
                        return i;
                }
            }

            if (m_ActionIndexByNameOrId.TryGetValue(nameOrId, out var actionIndex))
                return actionIndex;

            for (var i = 0; i < actionCount; ++i)
            {
                var action = m_Actions[i];
                if (action.m_Id == nameOrId || string.Compare(m_Actions[i].m_Name, nameOrId, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return i;
            }

            return InputActionState.kInvalidIndex;
        }

        private void SetUpActionLookupTable()
        {
            if (m_ActionIndexByNameOrId != null || m_Actions == null)
                return;

            m_ActionIndexByNameOrId = new Dictionary<string, int>();

            var actionCount = m_Actions.Length;
            for (var i = 0; i < actionCount; ++i)
            {
                var action = m_Actions[i];

                // We want to make sure an action ID cannot change *after* we have created the table.
                // NOTE: The *name* of an action, however, *may* change.
                action.MakeSureIdIsInPlace();

                // We create two lookup paths for each action:
                // (1) By case-sensitive name.
                // (2) By GUID string.
                m_ActionIndexByNameOrId[action.name] = i;
                m_ActionIndexByNameOrId[action.m_Id] = i;
            }
        }

        internal void ClearActionLookupTable()
        {
            m_ActionIndexByNameOrId?.Clear();
        }

        private int FindActionIndex(Guid id)
        {
            if (m_Actions == null)
                return InputActionState.kInvalidIndex;
            var actionCount = m_Actions.Length;
            for (var i = 0; i < actionCount; ++i)
                if (m_Actions[i].idDontGenerate == id)
                    return i;

            return InputActionState.kInvalidIndex;
        }

        /// <summary>
        /// Find an action in the map by name or ID.
        /// </summary>
        /// <param name="actionNameOrId">Name (as in <see cref="InputAction.name"/>) or ID (as in <see cref="InputAction.id"/>)
        /// of the action. Note that matching of names is case-insensitive.</param>
        /// <param name="throwIfNotFound">If set to <see langword="true"/> will cause an exception to be thrown when the action was not found.</param>
        /// <returns>The action with the given name or ID or <c>null</c> if no matching action
        /// was found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="actionNameOrId"/> is <c>null</c>.</exception>
        /// <seealso cref="FindAction(Guid)"/>
        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
        {
            if (actionNameOrId == null)
                throw new ArgumentNullException(nameof(actionNameOrId));
            var index = FindActionIndex(actionNameOrId);
            if (index == -1)
            {
                if (throwIfNotFound)
                    throw new ArgumentException($"No action '{actionNameOrId}' in '{this}'", nameof(actionNameOrId));
                return null;
            }
            return m_Actions[index];
        }

        /// <summary>
        /// Find an action by ID.
        /// </summary>
        /// <param name="id">ID (as in <see cref="InputAction.id"/>) of the action.</param>
        /// <returns>The action with the given ID or null if no action in the map has
        /// the given ID.</returns>
        /// <seealso cref="FindAction(string,bool)"/>
        public InputAction FindAction(Guid id)
        {
            var index = FindActionIndex(id);
            if (index == -1)
                return null;
            return m_Actions[index];
        }

        /// <summary>
        /// Check whether there are any bindings in the action map that can bind to
        /// controls on the given device.
        /// </summary>
        /// <param name="device">An input device.</param>
        /// <returns>True if any of the bindings in the map can resolve to controls on the device, false otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The logic is entirely based on the contents of <see cref="bindings"/> and, more specifically,
        /// <see cref="InputBinding.effectivePath"/> of each binding. Each path is checked using <see
        /// cref="InputControlPath.Matches"/>. If any path matches, the method returns <c>true</c>.
        ///
        /// Properties such as <see cref="devices"/> and <see cref="bindingMask"/> are ignored.
        ///
        /// <example>
        /// <code>
        /// // Create action map with two actions and bindings.
        /// var actionMap = new InputActionMap();
        /// actionMap.AddAction("action1", binding: "&lt;Gamepad&gt;/buttonSouth");
        /// actionMap.AddAction("action2", binding: "&lt;XRController{LeftHand}&gt;/{PrimaryAction}");
        ///
        /// //
        /// var gamepad = InputSystem.AddDevice&lt;Gamepad&gt;();
        /// var xrController = InputSystem.AddDevice&lt;XRController&gt;();
        ///
        /// // Returns true:
        /// actionMap.IsUsableWith(gamepad);
        ///
        /// // Returns false: (the XRController does not have the LeftHand usage assigned to it)
        /// actionMap.IsUsableWith(xrController);
        /// </code>
        /// </example>
        /// </remarks>
        public bool IsUsableWithDevice(InputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            if (m_Bindings == null)
                return false;

            foreach (var binding in m_Bindings)
            {
                var path = binding.effectivePath;
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
        /// <remarks>
        /// This is equivalent to calling <see cref="InputAction.Enable"/> on each
        /// action in <see cref="actions"/>, but is more efficient as the actions
        /// will get enabled in bulk.
        /// </remarks>
        /// <seealso cref="Disable"/>
        /// <seealso cref="enabled"/>
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
        /// <remarks>
        /// This is equivalent to calling <see cref="InputAction.Disable"/> on each
        /// action in <see cref="actions"/>, but is more efficient as the actions
        /// will get disabled in bulk.
        /// </remarks>
        /// <seealso cref="Enable"/>
        /// <seealso cref="enabled"/>
        public void Disable()
        {
            if (!enabled)
                return;

            m_State.DisableAllActions(this);
        }

        /// <summary>
        /// Produce an identical copy of the action map with its actions and bindings.
        /// </summary>
        /// <returns>A copy of the action map.</returns>
        /// <remarks>
        /// If the action map is part of an <see cref="InputActionAsset"/>, the clone will <em>not</em>
        /// be. It will be a free-standing action map and <see cref="asset"/> will be <c>null</c>.
        ///
        /// Note that the IDs for the map itself as well as for its <see cref="actions"/> and
        /// <see cref="bindings"/> are not copied. Instead, new IDs will be assigned. Also, callbacks
        /// installed on actions or on the map itself will not be copied over.
        /// </remarks>
        public InputActionMap Clone()
        {
            Debug.Assert(m_SingletonAction == null, "Internal (hidden) action maps of singleton actions should not be cloned");

            var clone = new InputActionMap
            {
                m_Name = m_Name
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
                        m_Type = original.m_Type,
                        m_Interactions = original.m_Interactions,
                        m_Processors = original.m_Processors,
                        m_ExpectedControlType = original.m_ExpectedControlType,
                        m_Flags = original.m_Flags,
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
                for (var i = 0; i < bindingCount; ++i)
                    bindings[i].m_Id = default;
                clone.m_Bindings = bindings;
            }

            return clone;
        }

        /// <summary>
        /// Return an boxed instance of the action map.
        /// </summary>
        /// <returns>An boxed clone of the action map</returns>
        /// <seealso cref="Clone"/>
        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// Return <c>true</c> if the action map contains the given action.
        /// </summary>
        /// <param name="action">An input action. Can be <c>null</c>.</param>
        /// <returns>True if the action map contains <paramref name="action"/>, false otherwise.</returns>
        public bool Contains(InputAction action)
        {
            if (action == null)
                return false;

            return action.actionMap == this;
        }

        /// <summary>
        /// Return a string representation of the action map useful for debugging.
        /// </summary>
        /// <returns>A string representation of the action map.</returns>
        /// <remarks>
        /// For unnamed action maps, this will always be <c>"&lt;Unnamed Action Map&gt;"</c>.
        /// </remarks>
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

        /// <summary>
        /// Enumerate the actions in the map.
        /// </summary>
        /// <returns>An enumerator going over the actions in the map.</returns>
        /// <seealso cref="GetEnumerator"/>
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
        /// <seealso cref="SetUpPerActionControlAndBindingArrays"/>
        [NonSerialized] private InputBinding[] m_BindingsForEachAction;

        [NonSerialized] private InputControl[] m_ControlsForEachAction;

        /// <summary>
        /// Number of actions currently enabled in the map.
        /// </summary>
        /// <remarks>
        /// This should only be written to by <see cref="InputActionState"/>.
        /// </remarks>
        [NonSerialized] internal int m_EnabledActionsCount;

        // Action maps that are created internally by singleton actions to hold their data
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
        [NonSerialized] internal InputBinding? m_BindingMask;
        [NonSerialized] private Flags m_Flags;
        [NonSerialized] internal int m_ParameterOverridesCount;
        [NonSerialized] internal InputActionRebindingExtensions.ParameterOverride[] m_ParameterOverrides;

        [NonSerialized] internal DeviceArray m_Devices;

        [NonSerialized] internal CallbackArray<Action<InputAction.CallbackContext>> m_ActionCallbacks;

        [NonSerialized] internal Dictionary<string, int> m_ActionIndexByNameOrId;

        private bool needToResolveBindings
        {
            get => (m_Flags & Flags.NeedToResolveBindings) != 0;
            set
            {
                if (value)
                    m_Flags |= Flags.NeedToResolveBindings;
                else
                    m_Flags &= ~Flags.NeedToResolveBindings;
            }
        }

        private bool bindingResolutionNeedsFullReResolve
        {
            get => (m_Flags & Flags.BindingResolutionNeedsFullReResolve) != 0;
            set
            {
                if (value)
                    m_Flags |= Flags.BindingResolutionNeedsFullReResolve;
                else
                    m_Flags &= ~Flags.BindingResolutionNeedsFullReResolve;
            }
        }

        private bool controlsForEachActionInitialized
        {
            get => (m_Flags & Flags.ControlsForEachActionInitialized) != 0;
            set
            {
                if (value)
                    m_Flags |= Flags.ControlsForEachActionInitialized;
                else
                    m_Flags &= ~Flags.ControlsForEachActionInitialized;
            }
        }

        private bool bindingsForEachActionInitialized
        {
            get => (m_Flags & Flags.BindingsForEachActionInitialized) != 0;
            set
            {
                if (value)
                    m_Flags |= Flags.BindingsForEachActionInitialized;
                else
                    m_Flags &= ~Flags.BindingsForEachActionInitialized;
            }
        }

        [Flags]
        private enum Flags
        {
            NeedToResolveBindings = 1 << 0,
            BindingResolutionNeedsFullReResolve = 1 << 1,
            ControlsForEachActionInitialized = 1 << 2,
            BindingsForEachActionInitialized = 1 << 3,
        }

        internal static int s_DeferBindingResolution;
        internal static bool s_NeedToResolveBindings;

        internal struct DeviceArray
        {
            private bool m_HaveValue;
            private int m_DeviceCount;
            private InputDevice[] m_DeviceArray; // May have extra capacity; we won't let go once allocated.

            public int IndexOf(InputDevice device)
            {
                return m_DeviceArray.IndexOfReference(device, m_DeviceCount);
            }

            public bool Remove(InputDevice device)
            {
                var index = IndexOf(device);
                if (index < 0)
                    return false;
                m_DeviceArray.EraseAtWithCapacity(ref m_DeviceCount, index);
                return true;
            }

            public ReadOnlyArray<InputDevice>? Get()
            {
                if (!m_HaveValue)
                    return null;
                return new ReadOnlyArray<InputDevice>(m_DeviceArray, 0, m_DeviceCount);
            }

            public bool Set(ReadOnlyArray<InputDevice>? devices)
            {
                if (!devices.HasValue)
                {
                    if (!m_HaveValue)
                        return false; // No change.
                    if (m_DeviceCount > 0)
                        Array.Clear(m_DeviceArray, 0, m_DeviceCount);
                    m_DeviceCount = 0;
                    m_HaveValue = false;
                }
                else
                {
                    // See if the array actually changes content. Avoids re-resolving when there
                    // is no need to.
                    var array = devices.Value;
                    if (m_HaveValue && array.Count == m_DeviceCount && array.HaveEqualReferences(m_DeviceArray, m_DeviceCount))
                        return false;

                    if (m_DeviceCount > 0)
                        m_DeviceArray.Clear(ref m_DeviceCount);
                    m_HaveValue = true;
                    m_DeviceCount = 0;
                    ArrayHelpers.AppendListWithCapacity(ref m_DeviceArray, ref m_DeviceCount, array);
                }

                return true;
            }
        }

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
            if (!bindingsForEachActionInitialized)
                SetUpPerActionControlAndBindingArrays();

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

            if (!controlsForEachActionInitialized)
                SetUpPerActionControlAndBindingArrays();

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
        private unsafe void SetUpPerActionControlAndBindingArrays()
        {
            // Handle case where we don't have any bindings.
            if (m_Bindings == null)
            {
                m_ControlsForEachAction = null;
                m_BindingsForEachAction = null;
                controlsForEachActionInitialized = true;
                bindingsForEachActionInitialized = true;
                return;
            }

            if (m_SingletonAction != null)
            {
                // Dead simple case: map is internally owned by action. The entire
                // list of bindings is specific to the action.

                Debug.Assert(m_Bindings == m_SingletonAction.m_SingletonActionBindings,
                    "For singleton action, bindings array must match that of the action");

                m_BindingsForEachAction = m_Bindings;
                m_ControlsForEachAction = m_State?.controls;

                m_SingletonAction.m_BindingsStartIndex = 0;
                m_SingletonAction.m_BindingsCount = m_Bindings.Length;
                m_SingletonAction.m_ControlStartIndex = 0;
                m_SingletonAction.m_ControlCount = m_State?.totalControlCount ?? 0;

                // Only complication, InputActionState allows a control to appear multiple times
                // on the same action and InputAction.controls[] doesn't.
                if (m_ControlsForEachAction.HaveDuplicateReferences(0, m_SingletonAction.m_ControlCount))
                {
                    var numControls = 0;
                    var controls = new InputControl[m_SingletonAction.m_ControlCount];
                    for (var i = 0; i < m_SingletonAction.m_ControlCount; ++i)
                    {
                        if (!controls.ContainsReference(m_ControlsForEachAction[i]))
                        {
                            controls[numControls] = m_ControlsForEachAction[i];
                            ++numControls;
                        }
                    }

                    m_ControlsForEachAction = controls;
                    m_SingletonAction.m_ControlCount = numControls;
                }
            }
            else
            {
                ////REVIEW: now that we have per-action binding information in UnmanagedMemory, this here can likely be done more easily

                // Go through all bindings and slice them out to individual actions.

                Debug.Assert(m_Actions != null, "Action map is associated with action but action map has no array of actions"); // Action isn't a singleton so this has to be true.
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
                    var action = FindAction(m_Bindings[i].action);
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
                    var currentAction = FindAction(m_Bindings[currentBindingIndex].action);
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
                        if (FindAction(m_Bindings[sourceBindingToCopy].action) != currentAction)
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
                            while (FindAction(m_Bindings[sourceBindingToCopy].action) != currentAction);
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
                            ref var bindingState = ref m_State.bindingStates[mapIndices.bindingStartIndex + sourceBindingToCopy];

                            var controlCountForBinding = bindingState.controlCount;
                            if (controlCountForBinding > 0)
                            {
                                // Internally, we allow several bindings on a given action to resolve to the same control.
                                // Externally, however, InputAction.controls[] is a set and thus should not contain duplicates.
                                // So, instead of just doing a straight copy here, we copy controls one by one.

                                var controlStartIndexForBinding = bindingState.controlStartIndex;
                                for (var n = 0; n < controlCountForBinding; ++n)
                                {
                                    var control = m_State.controls[controlStartIndexForBinding + n];
                                    if (!m_ControlsForEachAction.ContainsReference(currentAction.m_ControlStartIndex,
                                        currentAction.m_ControlCount, control))
                                    {
                                        m_ControlsForEachAction[currentControlIndex] = control;
                                        ++currentControlIndex;
                                        ++currentAction.m_ControlCount;
                                    }
                                }
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

            controlsForEachActionInitialized = true;
            bindingsForEachActionInitialized = true;
        }

        internal void OnWantToChangeSetup()
        {
            if (asset != null)
            {
                foreach (var assetMap in asset.actionMaps)
                    if (assetMap.enabled)
                        throw new InvalidOperationException(
                            $"Cannot add, remove, or change elements of InputActionAsset {asset} while one or more of its actions are enabled");
            }
            else if (enabled)
            {
                throw new InvalidOperationException(
                    $"Cannot add, remove, or change elements of InputActionMap {this} while one or more of its actions are enabled");
            }
        }

        internal void OnSetupChanged()
        {
            if (m_Asset != null)
            {
                m_Asset.MarkAsDirty();
                foreach (var map in m_Asset.actionMaps)
                    map.m_State = default;
            }
            else
            {
                m_State = default;
            }
            ClearCachedActionData();
            LazyResolveBindings(fullResolve: true);
        }

        internal void OnBindingModified()
        {
            ClearCachedActionData();
            LazyResolveBindings(fullResolve: true);
        }

        ////TODO: re-use allocations such that only grow the arrays and hit zero GC allocs when we already have enough memory
        internal void ClearCachedActionData(bool onlyControls = false)
        {
            if (!onlyControls)
            {
                bindingsForEachActionInitialized = false;
                m_BindingsForEachAction = default;
                m_ActionIndexByNameOrId = default;
            }

            controlsForEachActionInitialized = false;
            m_ControlsForEachAction = default;
        }

        internal void GenerateId()
        {
            m_Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Resolve bindings right away if we have to. Otherwise defer it to when we next need
        /// the bindings.
        /// </summary>
        internal bool LazyResolveBindings(bool fullResolve)
        {
            // Clear cached controls for actions. Don't need to necessarily clear m_BindingsForEachAction.
            m_ControlsForEachAction = null;
            controlsForEachActionInitialized = false;

            // Indicate that there is at least one action map that has a change
            s_NeedToResolveBindings = true;

            // If we haven't had to resolve bindings yet, we can wait until when we
            // actually have to.
            if (m_State == null)
                return false;

            // We used to defer binding resolution here in case the map had no enabled actions. That behavior,
            // however, leads to rather unpredictable BoundControlsChanged notifications (especially for
            // rebinding UIs), so now we just always re-resolve anything that ever had an InputActionState
            // created. Unfortunately, this can lead to some unnecessary re-resolving.

            needToResolveBindings = true;
            bindingResolutionNeedsFullReResolve |= fullResolve;

            if (s_DeferBindingResolution > 0)
                return false;

            // Have to do it straight away.
            ResolveBindings();
            return true;
        }

        internal bool ResolveBindingsIfNecessary()
        {
            // NOTE: We only check locally for the current map here. When there are multiple maps
            //       in an asset, we may have maps that require re-resolution while others don't.
            //       We only resolve if a map is used that needs resolution to happen. Note that
            //       this will still resolve bindings for *all* maps in the asset.

            if (m_State == null || needToResolveBindings)
            {
                if (m_State != null && m_State.isProcessingControlStateChange)
                {
                    Debug.Assert(s_DeferBindingResolution > 0, "While processing control state changes, binding resolution should be suppressed");
                    return false;
                }

                ResolveBindings();
                return true;
            }

            return false;
        }

        // We have three different starting scenarios for binding resolution:
        //
        // (1) From scratch.
        //     There is no InputActionState and we resolve everything from a completely fresh start. This happens when
        //     we either have not resolved bindings at all yet or when something touches the action setup (e.g. adds
        //     or removes an action or binding) and we thus throw away the existing InputActionState.
        //     NOTE:
        //      * Actions can be in enabled state.
        //      * No action can be in an in-progress state (since binding resolution is needed for actions to
        //        be processed, no action processing can have happened yet)
        //
        // (2) From an existing InputActionState when a device has been added or removed.
        //     There is an InputActionState and the action setup (maps, actions, bindings, binding masks) has not changed. However,
        //     the set of devices usable with the action has changed (either the per-asset/map device list or the global
        //     list, if we're using it).
        //     NOTE:
        //      * Actions can be in enabled state.
        //      * Actions *can* be in an in-progress state.
        //        IF the control currently driving the action is on a device that is no longer usable with the action, the
        //        action is CANCELLED. OTHERWISE, the action will be left as is and keep being in progress from its active control.
        //      * A device CONFIGURATION change will NOT go down this path (e.g. changing the Keyboard layout). This is because
        //        any binding path involving display names may now resolve to different controls -- which may impact currently
        //        active controls of in-progress actions.
        //      * A change in the USAGES of a device will NOT go down this path either. This is for the same reason -- i.e. an
        //        active control may no longer match the binding path it matched before. If, for example, we switch the left-hand
        //        and right-hand roles of two controllers, will will go down path (3) and not (2).
        //
        // (3) From an existing InputActionState on any other change not covered before.
        //     There is an InputActionState and the action setup (maps, actions, bindings, binding masks) may have changed. Also,
        //     any change may have happened in the set of usable devices and targeted controls. This includes binding overrides
        //     having been applied.
        //     NOTE:
        //      * Action can be in enabled state.
        //      * Actions *can* be in an in-progress state.
        //        Any such action will be CANCELLED as part of the re-resolution process.
        //
        // Both (1) and (3) are considered a "full resolve". (2) is not.

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
        /// <see cref="InputActionAsset.devices"/>, <see cref="InputActionAsset.bindingMask"/>). Doing so will
        /// not affect the enable state of actions and, as much as possible, will try to take current
        /// action states across.
        /// </remarks>
        internal void ResolveBindings()
        {
            // Make sure that if we trigger callbacks as part of disabling and re-enabling actions,
            // we don't trigger a re-resolve while we're already resolving bindings.
            using (InputActionRebindingExtensions.DeferBindingResolution())
            {
                // In case we have actions that are currently enabled, we temporarily retain the
                // UnmanagedMemory of our InputActionState so that we can sync action states after
                // we have re-resolved bindings.
                var oldMemory = new InputActionState.UnmanagedMemory();
                try
                {
                    OneOrMore<InputActionMap, ReadOnlyArray<InputActionMap>> actionMaps;

                    // Start resolving.
                    var resolver = new InputBindingResolver();

                    // If we're part of an asset, we share state and thus binding resolution with
                    // all maps in the asset.
                    var needFullResolve = m_State == null;
                    if (m_Asset != null)
                    {
                        actionMaps = m_Asset.actionMaps;
                        Debug.Assert(actionMaps.Count > 0, "Asset referred to by action map does not have action maps");

                        // If there's a binding mask set on the asset, apply it.
                        resolver.bindingMask = m_Asset.m_BindingMask;

                        foreach (var map in actionMaps)
                        {
                            needFullResolve |= map.bindingResolutionNeedsFullReResolve;
                            map.needToResolveBindings = false;
                            map.bindingResolutionNeedsFullReResolve = false;
                            map.controlsForEachActionInitialized = false;
                        }
                    }
                    else
                    {
                        // Standalone action map (possibly a hidden one created for a singleton action).
                        // Gets its own private state.

                        actionMaps = this;
                        needFullResolve |= bindingResolutionNeedsFullReResolve;
                        needToResolveBindings = false;
                        bindingResolutionNeedsFullReResolve = false;
                        controlsForEachActionInitialized = false;
                    }

                    // If we already have a state, re-use the arrays we have already allocated.
                    // NOTE: We will install the arrays on the very same InputActionState instance below. In the
                    //       case where we didn't have to grow the arrays, we should end up with zero GC allocations
                    //       here.
                    var hasEnabledActions = false;
                    InputControlList<InputControl> activeControls = default;
                    if (m_State != null)
                    {
                        // Grab a clone of the current memory. We clone because disabling all the actions
                        // in the map will alter the memory state and we want the state before we start
                        // touching it.
                        oldMemory = m_State.memory.Clone();

                        m_State.PrepareForBindingReResolution(needFullResolve, ref activeControls, ref hasEnabledActions);

                        // Reuse the arrays we have so that we can avoid managed memory allocations, if possible.
                        resolver.StartWithPreviousResolve(m_State, isFullResolve: needFullResolve);

                        // Throw away old memory.
                        m_State.memory.Dispose();
                    }

                    // Resolve all maps in the asset.
                    foreach (var map in actionMaps)
                        resolver.AddActionMap(map);

                    // Install state.
                    if (m_State == null)
                    {
                        m_State = new InputActionState();
                        m_State.Initialize(resolver);
                    }
                    else
                    {
                        m_State.ClaimDataFrom(resolver);
                    }
                    if (m_Asset != null)
                    {
                        foreach (var map in actionMaps)
                            map.m_State = m_State;
                        m_Asset.m_SharedStateForAllMaps = m_State;
                    }

                    m_State.FinishBindingResolution(hasEnabledActions, oldMemory, activeControls, isFullResolve: needFullResolve);
                }
                finally
                {
                    oldMemory.Dispose();
                }
            }
        }

        /// <inheritdoc/>
        public int FindBinding(InputBinding mask, out InputAction action)
        {
            var index = FindBindingRelativeToMap(mask);
            if (index == -1)
            {
                action = null;
                return -1;
            }

            action = m_SingletonAction ?? FindAction(bindings[index].action);
            return action.BindingIndexOnMapToBindingIndexOnAction(index);
        }

        /// <summary>
        /// Find the index of the first binding that matches the given mask.
        /// </summary>
        /// <param name="mask">A binding. See <see cref="InputBinding.Matches"/> for details.</param>
        /// <returns>Index into <see cref="InputAction.bindings"/> of <paramref name="action"/> of the binding
        /// that matches <paramref name="mask"/>. If no binding matches, will return -1.</returns>
        /// <remarks>
        /// For details about matching bindings by a mask, see <see cref="InputBinding.Matches"/>.
        ///
        /// <example>
        /// <code>
        /// var index = playerInput.actions.FindBindingRelativeToMap(
        ///     new InputBinding { path = "&lt;Gamepad&gt;/buttonSouth" });
        ///
        /// if (index != -1)
        ///     Debug.Log($"Found binding with index {index}");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.Matches"/>
        /// <seealso cref="bindings"/>
        internal int FindBindingRelativeToMap(InputBinding mask)
        {
            var bindings = m_Bindings;
            var bindingsCount = bindings.LengthSafe();

            for (var i = 0; i < bindingsCount; ++i)
            {
                ref var binding = ref bindings[i];
                if (mask.Matches(ref binding))
                    return i;
            }

            return -1;
        }

        #region Serialization

        ////REVIEW: when GetParameter/SetParameter is coming, should these also be considered part of binding override data?

        [Serializable]
        internal struct BindingOverrideListJson
        {
            public List<BindingOverrideJson> bindings;
        }

        [Serializable]
        internal struct BindingOverrideJson
        {
            // We save both the "map/action" path of the action as well as the binding ID.
            // This gives us two avenues into finding our target binding to apply the override
            // to.
            public string action;
            public string id;
            public string path;
            public string interactions;
            public string processors;

            public static BindingOverrideJson FromBinding(InputBinding binding, string actionName)
            {
                return new BindingOverrideJson
                {
                    action = actionName,
                    id = binding.id.ToString() ,
                    path = binding.overridePath ?? "null",
                    interactions = binding.overrideInteractions ?? "null",
                    processors = binding.overrideProcessors ?? "null"
                };
            }

            public static BindingOverrideJson FromBinding(InputBinding binding)
            {
                return FromBinding(binding, binding.action);
            }

            public static InputBinding ToBinding(BindingOverrideJson bindingOverride)
            {
                return new InputBinding
                {
                    overridePath = bindingOverride.path != "null" ? bindingOverride.path : null,
                    overrideInteractions = bindingOverride.interactions != "null" ? bindingOverride.interactions : null,
                    overrideProcessors = bindingOverride.processors != "null" ? bindingOverride.processors : null,
                };
            }
        }

        // Action maps are serialized in two different ways. For storage as imported assets in Unity's Library/ folder
        // and in player data and asset bundles as well as for surviving domain reloads, InputActionMaps are serialized
        // directly by Unity. For storage as source data in user projects, InputActionMaps are serialized indirectly
        // as JSON by setting up a separate set of structs that are then read and written using Unity's JSON serializer.

        [Serializable]
        internal struct BindingJson
        {
            public string name;
            public string id;
            public string path;
            public string interactions;
            public string processors;
            public string groups;
            public string action;
            public bool isComposite;
            public bool isPartOfComposite;

            public InputBinding ToBinding()
            {
                return new InputBinding
                {
                    name = string.IsNullOrEmpty(name) ? null : name,
                    m_Id = string.IsNullOrEmpty(id) ? null : id,
                    path = path,
                    action = string.IsNullOrEmpty(action) ? null : action,
                    interactions = string.IsNullOrEmpty(interactions) ? null : interactions,
                    processors = string.IsNullOrEmpty(processors) ? null : processors,
                    groups = string.IsNullOrEmpty(groups) ? null : groups,
                    isComposite = isComposite,
                    isPartOfComposite = isPartOfComposite,
                };
            }

            public static BindingJson FromBinding(ref InputBinding binding)
            {
                return new BindingJson
                {
                    name = binding.name,
                    id = binding.m_Id,
                    path = binding.path,
                    action = binding.action,
                    interactions = binding.interactions,
                    processors = binding.processors,
                    groups = binding.groups,
                    isComposite = binding.isComposite,
                    isPartOfComposite = binding.isPartOfComposite,
                };
            }
        }

        // Backwards-compatible read format.
        [Serializable]
        internal struct ReadActionJson
        {
            public string name;
            public string type;
            public string id;
            public string expectedControlType;
            public string expectedControlLayout;
            public string processors;
            public string interactions;
            public bool passThrough;
            public bool initialStateCheck;

            // Bindings can either be on the action itself (in which case the action name
            // for each binding is implied) or listed separately in the action file.
            public BindingJson[] bindings;

            public InputAction ToAction(string actionName = null)
            {
                // FormerlySerializedAs doesn't seem to work as expected so manually
                // handling the rename here.
                if (!string.IsNullOrEmpty(expectedControlLayout))
                    expectedControlType = expectedControlLayout;

                // Determine type.
                InputActionType actionType = default;
                if (!string.IsNullOrEmpty(type))
                    actionType = (InputActionType)Enum.Parse(typeof(InputActionType), type, true);
                else
                {
                    // Old format that doesn't have type. Try to infer from settings.

                    if (passThrough)
                        actionType = InputActionType.PassThrough;
                    else if (initialStateCheck)
                        actionType = InputActionType.Value;
                    else if (!string.IsNullOrEmpty(expectedControlType) &&
                             (expectedControlType == "Button" || expectedControlType == "Key"))
                        actionType = InputActionType.Button;
                }

                return new InputAction(actionName ?? name, actionType)
                {
                    m_Id = string.IsNullOrEmpty(id) ? null : id,
                    m_ExpectedControlType = !string.IsNullOrEmpty(expectedControlType)
                        ? expectedControlType
                        : null,
                    m_Processors = processors,
                    m_Interactions = interactions,
                    wantsInitialStateCheck = initialStateCheck,
                };
            }
        }

        [Serializable]
        internal struct WriteActionJson
        {
            public string name;
            public string type;
            public string id;
            public string expectedControlType;
            public string processors;
            public string interactions;
            public bool initialStateCheck;

            public static WriteActionJson FromAction(InputAction action)
            {
                return new WriteActionJson
                {
                    name = action.m_Name,
                    type = action.m_Type.ToString(),
                    id = action.m_Id,
                    expectedControlType = action.m_ExpectedControlType,
                    processors = action.processors,
                    interactions = action.interactions,
                    initialStateCheck = action.wantsInitialStateCheck,
                };
            }
        }

        [Serializable]
        internal struct ReadMapJson
        {
            public string name;
            public string id;
            public ReadActionJson[] actions;
            public BindingJson[] bindings;
        }

        [Serializable]
        internal struct WriteMapJson
        {
            public string name;
            public string id;
            public WriteActionJson[] actions;
            public BindingJson[] bindings;

            public static WriteMapJson FromMap(InputActionMap map)
            {
                WriteActionJson[] jsonActions = null;
                BindingJson[] jsonBindings = null;

                var actions = map.m_Actions;
                if (actions != null)
                {
                    var actionCount = actions.Length;
                    jsonActions = new WriteActionJson[actionCount];

                    for (var i = 0; i < actionCount; ++i)
                        jsonActions[i] = WriteActionJson.FromAction(actions[i]);
                }

                var bindings = map.m_Bindings;
                if (bindings != null)
                {
                    var bindingCount = bindings.Length;
                    jsonBindings = new BindingJson[bindingCount];

                    for (var i = 0; i < bindingCount; ++i)
                        jsonBindings[i] = BindingJson.FromBinding(ref bindings[i]);
                }

                return new WriteMapJson
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
            public WriteMapJson[] maps;

            public static WriteFileJson FromMap(InputActionMap map)
            {
                return new WriteFileJson
                {
                    maps = new[] {WriteMapJson.FromMap(map)}
                };
            }

            public static WriteFileJson FromMaps(IEnumerable<InputActionMap> maps)
            {
                var mapCount = maps.Count();
                if (mapCount == 0)
                    return new WriteFileJson();

                var mapsJson = new WriteMapJson[mapCount];
                var index = 0;
                foreach (var map in maps)
                    mapsJson[index++] = WriteMapJson.FromMap(map);

                return new WriteFileJson {maps = mapsJson};
            }
        }

        // A JSON representation of one or more sets of actions.
        // Contains a list of actions. Each action may specify the set it belongs to
        // as part of its name ("set/action").
        [Serializable]
        internal struct ReadFileJson
        {
            public ReadActionJson[] actions;
            public ReadMapJson[] maps;

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
                        throw new InvalidOperationException($"Action number {i + 1} has no name");

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
                            throw new InvalidOperationException(
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
                    var action = jsonAction.ToAction(actionName);
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
                        throw new InvalidOperationException($"Map number {i + 1} has no name");

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
                            throw new InvalidOperationException($"Action number {i + 1} in map '{mapName}' has no name");

                        // Create action.
                        var action = jsonAction.ToAction();
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

        /// <summary>
        /// Load one or more action maps from JSON.
        /// </summary>
        /// <param name="json">JSON representation of the action maps. Can be empty.</param>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
        /// <returns>The array of action maps (may be empty) read from the given JSON string. Will not be
        /// <c>null</c>.</returns>
        /// <remarks>
        /// Note that the format used by this method is different than what you
        /// get if you call <c>JsonUtility.ToJson</c> on an InputActionMap instance. In other
        /// words, the JSON format is not identical to the Unity serialized object representation
        /// of the asset.
        ///
        /// <example>
        /// <code>
        /// var maps = InputActionMap.FromJson(@"
        ///     {
        ///         ""maps"" : [
        ///             {
        ///                 ""name"" : ""Gameplay"",
        ///                 ""actions"" : [
        ///                     { ""name"" : ""fire"", ""type"" : ""button"" }
        ///                 ],
        ///                 ""bindings"" : [
        ///                     { ""path"" : ""&lt;Gamepad&gt;/leftTrigger"", ""action"" : ""fire"" }
        ///                 ],
        ///             }
        ///         ]
        ///     }
        /// ");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputActionAsset.FromJson"/>
        /// <seealso cref="ToJson(IEnumerable{InputActionMap})"/>
        public static InputActionMap[] FromJson(string json)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));
            var fileJson = JsonUtility.FromJson<ReadFileJson>(json);
            return fileJson.ToMaps();
        }

        /// <summary>
        /// Convert a set of action maps to JSON format.
        /// </summary>
        /// <param name="maps">List of action maps to serialize.</param>
        /// <exception cref="ArgumentNullException"><paramref name="maps"/> is <c>null</c>.</exception>
        /// <returns>JSON representation of the given action maps.</returns>
        /// <remarks>
        /// The result of this method can be loaded with <see cref="FromJson"/>.
        ///
        /// Note that the format used by this method is different than what you
        /// get if you call <c>JsonUtility.ToJson</c> on an InputActionMap instance. In other
        /// words, the JSON format is not identical to the Unity serialized object representation
        /// of the asset.
        /// </remarks>
        /// <seealso cref="FromJson"/>
        public static string ToJson(IEnumerable<InputActionMap> maps)
        {
            if (maps == null)
                throw new ArgumentNullException(nameof(maps));
            var fileJson = WriteFileJson.FromMaps(maps);
            return JsonUtility.ToJson(fileJson, true);
        }

        /// <summary>
        /// Convert the action map to JSON format.
        /// </summary>
        /// <returns>A JSON representation of the action map.</returns>
        /// <remarks>
        /// The result of this method can be loaded with <see cref="FromJson"/>.
        ///
        /// Note that the format used by this method is different than what you
        /// get if you call <c>JsonUtility.ToJson</c> on an InputActionMap instance. In other
        /// words, the JSON format is not identical to the Unity serialized object representation
        /// of the asset.
        /// </remarks>
        public string ToJson()
        {
            var fileJson = WriteFileJson.FromMap(this);
            return JsonUtility.ToJson(fileJson, true);
        }

        /// <summary>
        /// Called by Unity before the action map is serialized using Unity's
        /// serialization system.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// Called by Unity after the action map has been deserialized using Unity's
        /// serialization system.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // Indicate that there is at least one action map that has a change
            s_NeedToResolveBindings = true;

            m_State = null;
            m_MapIndexInState = InputActionState.kInvalidIndex;
            m_EnabledActionsCount = 0;

            // Restore references of actions linking back to us.
            if (m_Actions != null)
            {
                var actionCount = m_Actions.Length;
                for (var i = 0; i < actionCount; ++i)
                    m_Actions[i].m_ActionMap = this;
            }

            // Make sure we don't retain any cached per-action data when using serialization
            // to doctor around in action map configurations in the editor.
            ClearCachedActionData();
            ClearActionLookupTable();
        }

        #endregion
    }
}
