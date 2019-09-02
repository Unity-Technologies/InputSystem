using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem.Utilities;

////TODO: control schemes, like actions and maps, should have stable IDs so that they can be renamed

////REVIEW: have some way of expressing 'contracts' on action maps? I.e. something like
////        "I expect a 'look' and a 'move' action in here"

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
    /// Be aware that input action assets do not separate between static (configuration) data and dynamic
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
    public class InputActionAsset : ScriptableObject, IInputActionCollection
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
        /// <value>Action maps contained in the asset.</value>
        /// <seealso cref="AddActionMap"/>
        /// <seealso cref="RemoveActionMap(InputActionMap)"/>
        /// <seealso cref="FindActionMap"/>
        public ReadOnlyArray<InputActionMap> actionMaps => new ReadOnlyArray<InputActionMap>(m_ActionMaps);

        /// <summary>
        /// List of control schemes defined in the asset.
        /// </summary>
        /// <seealso cref="AddControlScheme"/>
        /// <seealso cref="RemoveControlScheme"/>
        public ReadOnlyArray<InputControlScheme> controlSchemes => new ReadOnlyArray<InputControlScheme>(m_ControlSchemes);

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

        /// <summary>
        /// Look up an action by name or ID.
        /// </summary>
        /// <param name="actionNameOrId">Name of the action as either a "map/action" combination (e.g. "gameplay/fire") or
        /// a simple name. In the former case, the name is split at the '/' slash and the first part is used to find
        /// a map with that name and the second part is used to find an action with that name inside the map. In the
        /// latter case, all maps are searched in order and the first action that has the given name in any of the maps
        /// is returned. Note that name comparisons are case-insensitive.
        ///
        /// Alternatively, the given string can be a GUID as given by <see cref="InputAction.id"/>.</param>
        /// <returns>The action with the corresponding name or null if no matching action could be found.</returns>
        /// <remarks>
        /// This method is equivalent to <see cref="FindAction(string)"/> except that it throws
        /// <see cref="KeyNotFoundException"/> if no action with the given name or ID
        /// could be found.
        /// </remarks>
        /// <exception cref="KeyNotFoundException">No action was found matching <paramref name="actionNameOrId"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="actionNameOrId"/> is <c>null</c> or empty.</exception>
        /// <seealso cref="FindAction(string)"/>
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
            var fileJson = new WriteFileJson
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
        /// <param name="json">JSON contents of an <c>.inputactions</c> asset.</param>
        /// <remarks>
        /// <c>.inputactions</c> assets are stored in JSON format. This method allows reading
        /// the JSON source text of such an asset into an existing <c>InputActionMap</c> instance.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.CreateInstance&lt;InputActionAsset&gt;();
        /// asset.LoadFromJson(@"
        /// {
        ///     ""maps"" : [
        ///         {
        ///             ""name"" : ""gameplay"",
        ///             ""actions"" : [
        ///                 { ""name"" : ""fire"", ""type"" : ""button"" },
        ///                 { ""name"" : ""look"", ""type"" : ""value"" },
        ///                 { ""name"" : ""move"", ""type"" : ""value"" }
        ///             ],
        ///             ""bindings"" : [
        ///                 { ""path"" : ""&lt;Gamepad&gt;/buttonSouth"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""&lt;Gamepad&gt;/leftTrigger"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""&lt;Gamepad&gt;/leftStick"", ""action"" : ""move"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""&lt;Gamepad&gt;/rightStick"", ""action"" : ""look"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""dpad"", ""action"" : ""move"", ""groups"" : ""Gamepad"", ""isComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/a"", ""name"" : ""left"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/d"", ""name"" : ""right"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/w"", ""name"" : ""up"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/s"", ""name"" : ""down"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Mouse&gt;/delta"", ""action"" : ""look"", ""groups"" : ""Keyboard&amp;Mouse"" },
        ///                 { ""path"" : ""&lt;Mouse&gt;/leftButton"", ""action"" : ""fire"", ""groups"" : ""Keyboard&amp;Mouse"" }
        ///             ]
        ///         },
        ///         {
        ///             ""name"" : ""ui"",
        ///             ""actions"" : [
        ///                 { ""name"" : ""navigate"" }
        ///             ],
        ///             ""bindings"" : [
        ///                 { ""path"" : ""&lt;Gamepad&gt;/dpad"", ""action"" : ""navigate"", ""groups"" : ""Gamepad"" }
        ///             ]
        ///         }
        ///     ],
        ///     ""controlSchemes"" : [
        ///         {
        ///             ""name"" : ""Gamepad"",
        ///             ""bindingGroup"" : ""Gamepad"",
        ///             ""devices"" : [
        ///                 { ""devicePath"" : ""&lt;Gamepad&gt;"" }
        ///             ]
        ///         },
        ///         {
        ///             ""name"" : ""Keyboard&amp;Mouse"",
        ///             ""bindingGroup"" : ""Keyboard&amp;Mouse"",
        ///             ""devices"" : [
        ///                 { ""devicePath"" : ""&lt;Keyboard&gt;"" },
        ///                 { ""devicePath"" : ""&lt;Mouse&gt;"" }
        ///             ]
        ///         }
        ///     ]
        /// }");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c> or empty.</exception>
        /// <seealso cref="FromJson"/>
        /// <seealso cref="ToJson"/>
        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));

            var parsedJson = JsonUtility.FromJson<ReadFileJson>(json);
            parsedJson.ToAsset(this);
        }

        /// <summary>
        /// Replace the contents of the asset with the data in the given JSON string.
        /// </summary>
        /// <param name="json">JSON contents of an <c>.inputactions</c> asset.</param>
        /// <returns>The InputActionAsset instance created from the given JSON string.</returns>
        /// <remarks>
        /// <c>.inputactions</c> assets are stored in JSON format. This method allows turning
        /// the JSON source text of such an asset into a new <c>InputActionMap</c> instance.
        ///
        /// <example>
        /// <code>
        /// var asset = InputActionAsset.FromJson(@"
        /// {
        ///     ""maps"" : [
        ///         {
        ///             ""name"" : ""gameplay"",
        ///             ""actions"" : [
        ///                 { ""name"" : ""fire"", ""type"" : ""button"" },
        ///                 { ""name"" : ""look"", ""type"" : ""value"" },
        ///                 { ""name"" : ""move"", ""type"" : ""value"" }
        ///             ],
        ///             ""bindings"" : [
        ///                 { ""path"" : ""&lt;Gamepad&gt;/buttonSouth"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""&lt;Gamepad&gt;/leftTrigger"", ""action"" : ""fire"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""&lt;Gamepad&gt;/leftStick"", ""action"" : ""move"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""&lt;Gamepad&gt;/rightStick"", ""action"" : ""look"", ""groups"" : ""Gamepad"" },
        ///                 { ""path"" : ""dpad"", ""action"" : ""move"", ""groups"" : ""Gamepad"", ""isComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/a"", ""name"" : ""left"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/d"", ""name"" : ""right"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/w"", ""name"" : ""up"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Keyboard&gt;/s"", ""name"" : ""down"", ""action"" : ""move"", ""groups"" : ""Keyboard&amp;Mouse"", ""isPartOfComposite"" : true },
        ///                 { ""path"" : ""&lt;Mouse&gt;/delta"", ""action"" : ""look"", ""groups"" : ""Keyboard&amp;Mouse"" },
        ///                 { ""path"" : ""&lt;Mouse&gt;/leftButton"", ""action"" : ""fire"", ""groups"" : ""Keyboard&amp;Mouse"" }
        ///             ]
        ///         },
        ///         {
        ///             ""name"" : ""ui"",
        ///             ""actions"" : [
        ///                 { ""name"" : ""navigate"" }
        ///             ],
        ///             ""bindings"" : [
        ///                 { ""path"" : ""&lt;Gamepad&gt;/dpad"", ""action"" : ""navigate"", ""groups"" : ""Gamepad"" }
        ///             ]
        ///         }
        ///     ],
        ///     ""controlSchemes"" : [
        ///         {
        ///             ""name"" : ""Gamepad"",
        ///             ""bindingGroup"" : ""Gamepad"",
        ///             ""devices"" : [
        ///                 { ""devicePath"" : ""&lt;Gamepad&gt;"" }
        ///             ]
        ///         },
        ///         {
        ///             ""name"" : ""Keyboard&amp;Mouse"",
        ///             ""bindingGroup"" : ""Keyboard&amp;Mouse"",
        ///             ""devices"" : [
        ///                 { ""devicePath"" : ""&lt;Keyboard&gt;"" },
        ///                 { ""devicePath"" : ""&lt;Mouse&gt;"" }
        ///             ]
        ///         }
        ///     ]
        /// }");
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c> or empty.</exception>
        /// <seealso cref="LoadFromJson"/>
        /// <seealso cref="ToJson"/>
        public static InputActionAsset FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));

            var asset = CreateInstance<InputActionAsset>();
            asset.LoadFromJson(json);
            return asset;
        }

        /// <summary>
        /// Find an <see cref="InputAction"/> by its name in one of the <see cref="InputActionMap"/>s
        /// in the asset.
        /// </summary>
        /// <param name="actionNameOrId">Name of the action as either a "map/action" combination (e.g. "gameplay/fire") or
        /// a simple name. In the former case, the name is split at the '/' slash and the first part is used to find
        /// a map with that name and the second part is used to find an action with that name inside the map. In the
        /// latter case, all maps are searched in order and the first action that has the given name in any of the maps
        /// is returned. Note that name comparisons are case-insensitive.
        ///
        /// Alternatively, the given string can be a GUID as given by <see cref="InputAction.id"/>.</param>
        /// <param name="throwIfNotFound">If <c>true</c>, instead of returning <c>null</c> when the action
        /// cannot be found, throw <c>ArgumentException</c>.</param>
        /// <returns>The action with the corresponding name or <c>null</c> if no matching action could be found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="actionNameOrId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="throwIfNotFound"/> is true and the
        /// action could not be found. -Or- If <paramref name="actionNameOrId"/> contains a slash but is missing
        /// either the action or the map name.</exception>
        /// <remarks>
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
        /// // Search by unique action ID.
        /// asset.FindAction(action1.id.ToString()) // Returns action1.
        /// asset.FindAction(action2.id.ToString()) // Returns action2.
        /// asset.FindAction(action3.id.ToString()) // Returns action3.
        /// </code>
        /// </example>
        /// </remarks>
        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
        {
            if (actionNameOrId == null)
                throw new ArgumentNullException(nameof(actionNameOrId));

            if (m_ActionMaps != null)
            {
                // Check if we have a "map/action" path.
                var indexOfSlash = actionNameOrId.IndexOf('/');
                if (indexOfSlash == -1)
                {
                    // No slash so it's just a simple action name.
                    for (var i = 0; i < m_ActionMaps.Length; ++i)
                    {
                        var action = m_ActionMaps[i].FindAction(actionNameOrId);
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
                            if (Substring.Compare(action.name, actionName,
                                StringComparison.InvariantCultureIgnoreCase) == 0)
                                return action;
                        }

                        break;
                    }
                }
            }

            if (throwIfNotFound)
                throw new ArgumentException($"No action '{actionNameOrId}' in '{this}'");

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
            if (FindActionMap(map.name) != null)
                throw new InvalidOperationException(
                    $"An action map called '{map.name}' already exists in the asset");

            ArrayHelpers.Append(ref m_ActionMaps, map);
            map.m_Asset = this;
        }

        /// <summary>
        /// Remove the given action map from the asset.
        /// </summary>
        /// <param name="map">An action map. If the given map is not part of the asset, the method
        /// does nothing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="map"/> is currently enabled (see <see
        /// cref="InputActionMap.enabled"/>).</exception>
        /// <seealso cref="RemoveActionMap(string)"/>
        /// <seealso cref="actionMaps"/>
        public void RemoveActionMap(InputActionMap map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (map.enabled)
                throw new InvalidOperationException("Cannot remove an action map from the asset while it is enabled");

            // Ignore if not part of this asset.
            if (map.m_Asset != this)
                return;

            ArrayHelpers.Erase(ref m_ActionMaps, map);
            map.m_Asset = null;
        }

        /// <summary>
        /// Remove the action map with the given name or ID from the asset.
        /// </summary>
        /// <param name="nameOrId">The name or ID (see <see cref="InputActionMap.id"/>) of a map in the
        /// asset. Note that lookup is case-insensitive. If no map with the given name or ID is found,
        /// the method does nothing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="nameOrId"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The map referenced by <paramref name="nameOrId"/> is currently enabled
        /// (see <see cref="InputActionMap.enabled"/>).</exception>
        /// <seealso cref="RemoveActionMap(string)"/>
        /// <seealso cref="actionMaps"/>
        public void RemoveActionMap(string nameOrId)
        {
            if (nameOrId == null)
                throw new ArgumentNullException(nameof(nameOrId));
            var map = FindActionMap(nameOrId);
            if (map != null)
                RemoveActionMap(map);
        }

        /// <summary>
        /// Find an <see cref="InputActionMap"/> in the asset by its name or ID.
        /// </summary>
        /// <param name="nameOrId">Name or ID (see <see cref="InputActionMap.id"/>) of the action map
        /// to look for. Matching is case-insensitive.</param>
        /// <param name="throwIfNotFound">If true, instead of returning <c>null</c>, throw <c>ArgumentException</c>.</param>
        /// <returns>The <see cref="InputActionMap"/> with a name or ID matching <paramref name="nameOrId"/> or
        /// <c>null</c> if no matching map could be found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="nameOrId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="throwIfNotFound"/> is <c>true</c>, thrown if
        /// the action map cannot be found.</exception>
        /// <seealso cref="actionMaps"/>
        /// <seealso cref="FindActionMap(System.Guid)"/>
        public InputActionMap FindActionMap(string nameOrId, bool throwIfNotFound = false)
        {
            if (nameOrId == null)
                throw new ArgumentNullException(nameof(nameOrId));

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

            if (throwIfNotFound)
                throw new ArgumentException($"Cannot find action map '{nameOrId}' in '{this}'");

            return null;
        }

        /// <summary>
        /// Find an <see cref="InputActionMap"/> in the asset by its ID.
        /// </summary>
        /// <param name="id">ID (see <see cref="InputActionMap.id"/>) of the action map
        /// to look for.</param>
        /// <returns>The <see cref="InputActionMap"/> with ID matching <paramref name="id"/> or
        /// <c>null</c> if no map in the asset has the given ID.</returns>
        /// <seealso cref="actionMaps"/>
        /// <seealso cref="FindActionMap"/>
        public InputActionMap FindActionMap(Guid id)
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

        public InputAction FindAction(Guid guid)
        {
            if (m_ActionMaps == null)
                return null;

            for (var i = 0; i < m_ActionMaps.Length; ++i)
            {
                var map = m_ActionMaps[i];
                var action = map.FindAction(guid);
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
                throw new ArgumentException($"No control scheme called '{name}' in '{this.name}'", nameof(name));
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
        internal struct WriteFileJson
        {
            public string name;
            public InputActionMap.WriteMapJson[] maps;
            public InputControlScheme.SchemeJson[] controlSchemes;
        }

        [Serializable]
        internal struct ReadFileJson
        {
            public string name;
            public InputActionMap.ReadMapJson[] maps;
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
