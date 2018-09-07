using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Utilities;

////REVIEW: have some way of expressing 'contracts' on action maps? I.e. something like
////        "I expect a 'look' and a 'move' action in here"

////TODO: nuke Clone()

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// An asset containing action maps and control schemes.
    /// </summary>
    /// <remarks>
    /// Usually imported from JSON using <see cref="Editor.InputActionImporter"/>.
    /// </remarks>
    public class InputActionAsset : ScriptableObject, ICloneable
    {
        public const string kExtension = "inputactions";

        /// <summary>
        /// List of action maps defined in the asset.
        /// </summary>
        public ReadOnlyArray<InputActionMap> actionMaps
        {
            get { return new ReadOnlyArray<InputActionMap>(m_ActionMaps); }
        }

        /// <summary>
        /// List of control schemes defined in the asset.
        /// </summary>
        public ReadOnlyArray<InputControlScheme> controlSchemes
        {
            get { return new ReadOnlyArray<InputControlScheme>(m_ControlSchemes); }
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
                throw new ArgumentNullException("json");

            var parsedJson = JsonUtility.FromJson<FileJson>(json);
            parsedJson.ToAsset(this);
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
                throw new ArgumentNullException("map");
            if (string.IsNullOrEmpty(map.name))
                throw new InvalidOperationException("Maps added to an input action asset must be named");
            ////REVIEW: some of the rules here seem stupid; just replace?
            if (TryGetActionMap(map.name) != null)
                throw new InvalidOperationException(
                    string.Format("An action map called '{0}' already exists in the asset", map.name));

            ArrayHelpers.Append(ref m_ActionMaps, map);
        }

        public void RemoveActionMap(InputActionMap map)
        {
            if (map == null)
                throw new ArgumentNullException("map");

            ArrayHelpers.Erase(ref m_ActionMaps, map);
        }

        public void RemoveActionMap(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            var set = TryGetActionMap(name);
            if (set != null)
                RemoveActionMap(set);
        }

        public InputActionMap TryGetActionMap(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name");

            if (m_ActionMaps == null)
                return null;

            for (var i = 0; i < m_ActionMaps.Length; ++i)
            {
                var map = m_ActionMaps[i];
                if (string.Compare(name, map.name, StringComparison.InvariantCultureIgnoreCase) == 0)
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
                throw new KeyNotFoundException(string.Format("Could not find an action map called '{0}' in asset '{1}'",
                    name, this));
            return map;
        }

        public InputActionMap GetActionMap(Guid id)
        {
            var map = TryGetActionMap(id);
            if (map == null)
                throw new KeyNotFoundException(string.Format("Could not find an action map with ID '{0}' in asset '{1}'",
                    id, this));
            return map;
        }

        public void AddControlScheme(InputControlScheme controlScheme)
        {
            if (string.IsNullOrEmpty(controlScheme.name))
                throw new ArgumentException("Cannot add control scheme without name to asset " + name);
            if (TryGetControlScheme(controlScheme.name) != null)
                throw new InvalidOperationException(string.Format("Asset '{0}' already contains a control scheme called '{1}'",
                    name, controlScheme.name));

            ArrayHelpers.Append(ref m_ControlSchemes, controlScheme);
        }

        public InputControlScheme? TryGetControlScheme(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (m_ControlSchemes == null)
                return null;

            for (var i = 0; i < m_ControlSchemes.Length; ++i)
                if (string.Compare(name, m_ControlSchemes[i].name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return m_ControlSchemes[i];

            return null;
        }

        public InputControlScheme GetControlScheme(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            var scheme = TryGetControlScheme(name);
            if (!scheme.HasValue)
                throw new Exception(string.Format("No control scheme called '{0}' in '{1}'", name, this.name));

            return scheme.Value;
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

        ////TODO: ApplyBindingOverrides, RemoveBindingOverrides, RemoveAllBindingOverrides

        [SerializeField] internal InputActionMap[] m_ActionMaps;
        [SerializeField] internal InputControlScheme[] m_ControlSchemes;

        ////TODO: make this one happen and also persist it across domain reloads
        /// <summary>
        /// Shared state for all action maps in the asset.
        /// </summary>
        [NonSerialized] internal InputActionMapState m_ActionMapState;

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
            }
        }
    }
}
