using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem.Utilities;

////TODO: control schemes, like actions and maps, should have stable IDs so that they can be renamed

////REVIEW: have some way of expressing 'contracts' on action maps? I.e. something like
////        "I expect a 'look' and a 'move' action in here"

////TODO: nuke Clone()

////REVIEW: rename this from "InputActionAsset" to something else that emphasizes the asset aspect less
////        and instead emphasizes the map collection aspect more?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// An asset containing action maps and control schemes.
    /// </summary>
    /// <remarks>
    /// Usually imported from JSON using <see cref="Editor.InputActionImporter"/>.
    ///
    /// Be aware that input action assets do not separate between static data and dynamic
    /// (instance) data. For audio, for example, <see cref="AudioClip"/> represents the static,
    /// shared data portion of audio playback whereas <see cref="AudioSource"/> represents the
    /// dynamic, per-instance audio playback portion (referencing the clip through <see
    /// cref="AudioSource.clip"/>.
    ///
    /// For input, such a split is less beneficial as the same input is generally not exercised
    /// multiple times in parallel. Keeping both static and dynamic data together simplifies
    /// using the system.
    ///
    /// However, there are scenarios where you indeed want to take the same input action and
    /// exercise it multiple times in parallel. A prominent example of such a use case is
    /// local multiplayer where each player gets the same set of actions but is controlling
    /// them with a different device (or devices) each. This is easily achieved by simply
    /// <see cref="UnityEngine.Object.Instantiate">instantiating</see> the input action
    /// asset multiple times.
    ///
    /// Note also that all action maps in an asset share binding state. This means that if
    /// one map in an asset has to resolve its bindings, all maps in the asset have to.
    /// </remarks>
    public class InputActionAsset : ScriptableObject, ICloneable, IInputActionCollection
    {
        public const string Extension = "inputactions";

        /// <summary>
        /// True if any action in the asset is currently enabled.
        /// </summary>
        /// <seealso cref="InputAction.enabled"/>
        /// <seealso cref="InputActionMap.enabled"/>
        /// <seealso cref="InputAction.Enable"/>
        /// <seealso cref="InputActionMap.Enable"/>
        /// <seealso cref="Enable"/>
        public bool enabled
        {
            get
            {
                foreach (var actionMap in actionMaps)
                    if (actionMap.enabled)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// List of action maps defined in the asset.
        /// </summary>
        /// <seealso cref="AddActionMap"/>
        /// <seealso cref="RemoveActionMap(InputActionMap)"/>
        public ReadOnlyArray<InputActionMap> actionMaps => new ReadOnlyArray<InputActionMap>(m_ActionMaps);

        /// <summary>
        /// List of control schemes defined in the asset.
        /// </summary>
        /// <seealso cref="AddControlScheme"/>
        /// <seealso cref="RemoveControlScheme"/>
        public ReadOnlyArray<InputControlScheme> controlSchemes => new ReadOnlyArray<InputControlScheme>(m_ControlSchemes);

        /// <inheritdoc />
        public InputBinding? bindingMask
        {
            get => m_BindingMask;
            set
            {
                if (m_BindingMask == value)
                    return;

                m_BindingMask = value;

                ReResolveIfNecessary();
            }
        }

        /// <inheritdoc />
        public ReadOnlyArray<InputDevice>? devices
        {
            get => m_Devices;
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
                ReResolveIfNecessary();
            }
        }

        public InputAction this[string actionNameOrId]
        {
            get
            {
                var action = FindAction(actionNameOrId);
                if (action == null)
                    throw new KeyNotFoundException($"Cannot find action '{actionNameOrId}' in '{this}'");
                return action;
            }
        }

        /// <summary>
        /// Return a JSON representation of the asset.
        /// </summary>
        /// <returns>A string in JSON format that represents the static/configuration data present
        /// in the asset.</returns>
        /// <remarks>
        /// This will not save dynamic execution state such as callbacks installed on
        /// <see cref="InputAction">actions</see> or enabled/disabled states of individual
        /// maps and actions.
        ///
        /// Use <see cref="LoadFromJson"/> to deserialize the JSON data back into an InputActionAsset.
        /// </remarks>
        public string ToJson()
        {
            var fileJson = new FileJson
            {
                name = name,
                maps = InputActionMap.WriteFileJson.FromMaps(m_ActionMaps).maps,
                controlSchemes = InputControlScheme.SchemeJson.ToJson(m_ControlSchemes),
            };

            return JsonUtility.ToJson(fileJson, true);
        }

        /// <summary>
        /// Replace the contents of the asset with the data in the given JSON string.
        /// </summary>
        /// <param name="json"></param>
        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));

            var parsedJson = JsonUtility.FromJson<FileJson>(json);
            parsedJson.ToAsset(this);
        }

        public static InputActionAsset FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));

            var asset = CreateInstance<InputActionAsset>();
            asset.LoadFromJson(json);
            return asset;
        }

        /// <summary>
        /// Find an <see cref="InputAction">action</see> by its name in of of the <see cref="InputActionMap">
        /// action maps</see> in the asset.
        /// </summary>
        /// <param name="actionNameOrId">Name of the action as either a "map/action" combination (e.g. "gameplay/fire") or
        /// a simple name. In the former case, the name is split at the '/' slash and the first part is used to find
        /// a map with that name and the second part is used to find an action with that name inside the map. In the
        /// latter case, all maps are searched in order and the first action that has the given name in any of the maps
        /// is returned. Note that name comparisons are case-insensitive.
        ///
        /// Alternatively, the given string can be a GUID as given by <see cref="InputAction.id"/>.</param>
        /// <returns>The action with the corresponding name or null if no matching action could be found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="actionNameOrId"/> is null or empty.</exception>
        /// <remarks>
        /// Does not allocate.
        /// </remarks>
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.CreateInstance&lt;InputActionAsset&gt;();
        ///
        /// var map1 = new InputActionMap("map1");
        /// var map2 = new InputActionMap("map2");
        ///
        /// asset.AddActionMap(map1);
        /// asset.AddActionMap(map2);
        ///
        /// var action1 = map1.AddAction("action1");
        /// var action2 = map1.AddAction("action2");
        /// var action3 = map2.AddAction("action3");
        ///
        /// // Search all maps in the asset for any action that has the given name.
        /// asset.FindAction("action1") // Returns action1.
        /// asset.FindAction("action2") // Returns action2
        /// asset.FindAction("action3") // Returns action3.
        ///
        /// // Search for a specific action in a specific map.
        /// asset.FindAction("map1/action1") // Returns action1.
        /// asset.FindAction("map2/action2") // Returns action2.
        /// asset.FindAction("map3/action3") // Returns action3.
        ///
        /// Search by unique action ID.
        /// asset.FindAction(action1.id.ToString()) // Returns action1.
        /// asset.FindAction(action2.id.ToString()) // Returns action2.
        /// asset.FindAction(action3.id.ToString()) // Returns action3.
        /// </code>
        /// </example>
        public InputAction FindAction(string actionNameOrId)
        {
            if (string.IsNullOrEmpty(actionNameOrId))
                throw new ArgumentNullException(nameof(actionNameOrId));

            if (m_ActionMaps == null)
                return null;

            // Check if we have a "map/action" path.
            var indexOfSlash = actionNameOrId.IndexOf('/');
            if (indexOfSlash == -1)
            {
                // No slash so it's just a simple action name.
                for (var i = 0; i < m_ActionMaps.Length; ++i)
                {
                    var action = m_ActionMaps[i].TryGetAction(actionNameOrId);
                    if (action != null)
                        return action;
                }
            }
            else
            {
                // Have a path. First search for the map, then for the action.
                var mapName = new Substring(actionNameOrId, 0, indexOfSlash);
                var actionName = new Substring(actionNameOrId, indexOfSlash + 1);

                if (mapName.isEmpty || actionName.isEmpty)
                    throw new ArgumentException("Malformed action path: " + actionNameOrId, nameof(actionNameOrId));

                for (var i = 0; i < m_ActionMaps.Length; ++i)
                {
                    var map = m_ActionMaps[i];
                    if (Substring.Compare(map.name, mapName, StringComparison.InvariantCultureIgnoreCase) != 0)
                        continue;

                    var actions = map.m_Actions;
                    for (var n = 0; n < actions.Length; ++n)
                    {
                        var action = actions[n];
                        if (Substring.Compare(action.name, actionName, StringComparison.InvariantCultureIgnoreCase) == 0)
                            return action;
                    }

                    break;
                }
            }

            return null;
        }

        /// <summary>
        /// Add an action map to the asset.
        /// </summary>
        /// <param name="map">A named action map.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="map"/> has no name or asset already contains a
        /// map with the same name.</exception>
        public void AddActionMap(InputActionMap map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (string.IsNullOrEmpty(map.name))
                throw new InvalidOperationException("Maps added to an input action asset must be named");
            if (map.asset != null)
                throw new InvalidOperationException(
                    $"Cannot add map '{map}' to asset '{this}' as it has already been added to asset '{map.asset}'");
            ////REVIEW: some of the rules here seem stupid; just replace?
            if (TryGetActionMap(map.name) != null)
                throw new InvalidOperationException(
                    $"An action map called '{map.name}' already exists in the asset");

            ArrayHelpers.Append(ref m_ActionMaps, map);
            map.m_Asset = this;
        }

        public void RemoveActionMap(InputActionMap map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            // Ignore if not part of this asset.
            if (map.m_Asset != this)
                return;

            ArrayHelpers.Erase(ref m_ActionMaps, map);
            map.m_Asset = null;
        }

        public void RemoveActionMap(string nameOrId)
        {
            if (string.IsNullOrEmpty(nameOrId))
                throw new ArgumentNullException(nameof(nameOrId));

            var map = TryGetActionMap(nameOrId);
            if (map != null)
                RemoveActionMap(map);
        }

        public InputActionMap TryGetActionMap(string nameOrId)
        {
            if (string.IsNullOrEmpty(nameOrId))
                throw new ArgumentException("Name cannot be null or empty", nameof(nameOrId));

            if (m_ActionMaps == null)
                return null;

            // If the name contains a hyphen, it may be a GUID.
            if (nameOrId.Contains('-') && Guid.TryParse(nameOrId, out var id))
            {
                for (var i = 0; i < m_ActionMaps.Length; ++i)
                {
                    var map = m_ActionMaps[i];
                    if (map.idDontGenerate == id)
                        return map;
                }
            }

            // Default lookup is by name (case-insensitive).
            for (var i = 0; i < m_ActionMaps.Length; ++i)
            {
                var map = m_ActionMaps[i];
                if (string.Compare(nameOrId, map.name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return map;
            }

            return null;
        }

        public InputActionMap TryGetActionMap(Guid id)
        {
            if (m_ActionMaps == null)
                return null;

            for (var i = 0; i < m_ActionMaps.Length; ++i)
            {
                var map = m_ActionMaps[i];
                if (map.idDontGenerate == id)
                    return map;
            }

            return null;
        }

        public InputActionMap GetActionMap(string name)
        {
            var map = TryGetActionMap(name);
            if (map == null)
                throw new KeyNotFoundException($"Could not find an action map called '{name}' in asset '{this}'");
            return map;
        }

        public InputActionMap GetActionMap(Guid id)
        {
            var map = TryGetActionMap(id);
            if (map == null)
                throw new KeyNotFoundException($"Could not find an action map with ID '{id}' in asset '{this}'");
            return map;
        }

        public InputAction TryGetAction(Guid guid)
        {
            if (m_ActionMaps == null)
                return null;

            for (var i = 0; i < m_ActionMaps.Length; ++i)
            {
                var map = m_ActionMaps[i];
                var action = map.TryGetAction(guid);
                if (action != null)
                    return action;
            }

            return null;
        }

        public void AddControlScheme(InputControlScheme controlScheme)
        {
            if (string.IsNullOrEmpty(controlScheme.name))
                throw new ArgumentException("Cannot add control scheme without name to asset " + name, nameof(controlScheme));
            if (TryGetControlScheme(controlScheme.name) != null)
                throw new InvalidOperationException(
                    $"Asset '{name}' already contains a control scheme called '{controlScheme.name}'");

            ArrayHelpers.Append(ref m_ControlSchemes, controlScheme);
        }

        public int TryGetControlSchemeIndex(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (m_ControlSchemes == null)
                return -1;

            for (var i = 0; i < m_ControlSchemes.Length; ++i)
                if (string.Compare(name, m_ControlSchemes[i].name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return i;

            return -1;
        }

        public int GetControlSchemeIndex(string name)
        {
            var index = TryGetControlSchemeIndex(name);
            if (index == -1)
                throw new Exception($"No control scheme called '{name}' in '{this.name}'");
            return index;
        }

        public InputControlScheme? TryGetControlScheme(string name)
        {
            var index = TryGetControlSchemeIndex(name);
            if (index == -1)
                return null;

            return m_ControlSchemes[index];
        }

        public void RemoveControlScheme(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            ArrayHelpers.EraseAt(ref m_ControlSchemes, GetControlSchemeIndex(name));
        }

        public InputControlScheme GetControlScheme(string name)
        {
            var index = GetControlSchemeIndex(name);
            return m_ControlSchemes[index];
        }

        /// <summary>
        /// Duplicate the asset.
        /// </summary>
        /// <returns>A new asset that contains a duplicate of all action maps and actions in the asset.</returns>
        /// <remarks>
        /// Unlike calling <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>, cloning an asset will not
        /// duplicate data such as unique <see cref="InputActionMap.id">map IDs</see> and <see cref="InputAction.id">action
        /// IDs</see>.
        /// </remarks>
        public InputActionAsset Clone()
        {
            var clone = (InputActionAsset)CreateInstance(GetType());
            clone.m_ActionMaps = ArrayHelpers.Clone(m_ActionMaps);
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public void Enable()
        {
            foreach (var map in actionMaps)
                map.Enable();
        }

        public void Disable()
        {
            foreach (var map in actionMaps)
                map.Disable();
        }

        public bool Contains(InputAction action)
        {
            if (action == null)
                return false;

            var map = action.actionMap;
            if (map == null)
                return false;

            return map.asset == this;
        }

        public IEnumerator<InputAction> GetEnumerator()
        {
            if (m_ActionMaps == null)
                yield break;

            for (var i = 0; i < m_ActionMaps.Length; ++i)
            {
                var actions = m_ActionMaps[i].actions;
                var actionCount = actions.Count;

                for (var n = 0; n < actionCount; ++n)
                    yield return actions[n];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void ReResolveIfNecessary()
        {
            if (m_SharedStateForAllMaps == null)
                return;

            Debug.Assert(m_ActionMaps != null && m_ActionMaps.Length > 0);
            // State is share between all action maps in the asset. Resolving bindings for the
            // first map will resolve them for all maps.
            m_ActionMaps[0].ResolveBindings();
        }

        private void OnDestroy()
        {
            Disable();
            if (m_SharedStateForAllMaps != null)
            {
                m_SharedStateForAllMaps.Dispose(); // Will clean up InputActionMap state.
                m_SharedStateForAllMaps = null;
            }
        }

        ////TODO: ApplyBindingOverrides, RemoveBindingOverrides, RemoveAllBindingOverrides

        [SerializeField] internal InputActionMap[] m_ActionMaps;
        [SerializeField] internal InputControlScheme[] m_ControlSchemes;

        ////TODO: make this persistent across domain reloads
        /// <summary>
        /// Shared state for all action maps in the asset.
        /// </summary>
        [NonSerialized] internal InputActionState m_SharedStateForAllMaps;
        [NonSerialized] internal InputBinding? m_BindingMask;

        [NonSerialized] private ReadOnlyArray<InputDevice>? m_Devices;
        [NonSerialized] private int m_DevicesCount;
        [NonSerialized] private InputDevice[] m_DevicesArray;

        [Serializable]
        internal struct FileJson
        {
            public string name;
            public InputActionMap.MapJson[] maps;
            public InputControlScheme.SchemeJson[] controlSchemes;

            public void ToAsset(InputActionAsset asset)
            {
                asset.name = name;
                asset.m_ActionMaps = new InputActionMap.ReadFileJson {maps = maps}.ToMaps();
                asset.m_ControlSchemes = InputControlScheme.SchemeJson.ToSchemes(controlSchemes);

                // Link maps to their asset.
                if (asset.m_ActionMaps != null)
                    foreach (var map in asset.m_ActionMaps)
                        map.m_Asset = asset;
            }
        }
    }
}
