using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Utilities;

////TODO: add hierarchical set of binding overrides to asset

////REVIEW: have some way of expressing 'contracts' on action maps? I.e. something like
////        "I expect a 'look' and a 'move' action in here"

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// An asset containing one or more action maps.
    /// </summary>
    /// <remarks>
    /// Usually imported from JSON using InputActionImporter.
    /// Names of each action set in the asset ust be unique.
    /// Allows applying overrides in bulk to all sets in the asset.
    ///
    /// NOTE: You don't have to use action sets this way. InputActionAsset
    ///       is a ready-made way to use Unity's default serialization and
    ///       have action sets go into the asset database. However, you can
    ///       just as well have action sets directly as JSON in your game.
    /// </remarks>
    public class InputActionAsset : ScriptableObject, ICloneable
    {
        public const string kExtension = "inputactions";

        public ReadOnlyArray<InputActionMap> actionMaps
        {
            get { return new ReadOnlyArray<InputActionMap>(m_ActionMaps); }
        }

        // Return a JSON representation of the asset.
        public string ToJson()
        {
            return InputActionMap.ToJson(m_ActionMaps);
        }

        // Replace the contents of the asset with the action sets in the
        // given JSON string.
        public void LoadFromJson(string json)
        {
            m_ActionMaps = InputActionMap.FromJson(json);
        }

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
                var set = m_ActionMaps[i];
                if (string.Compare(name, set.name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return set;
            }

            return null;
        }

        public InputActionMap GetActionMap(string name)
        {
            var set = TryGetActionMap(name);
            if (set == null)
                throw new KeyNotFoundException(string.Format("Could not find an action map called '{0}' in asset '{1}'",
                    name, this));
            return set;
        }

        public InputActionAsset Clone()
        {
            // Can't MemberwiseClone() ScriptableObject. Unfortunatly, Unity doesn't
            // prevent the call. Result will be a duplicate wrapper object, though.
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

        ////TODO: make this one happen and also persist it across domain reloads
        /// <summary>
        /// Shared state for all action maps in the asset.
        /// </summary>
        [NonSerialized] internal InputActionMapState m_ActionMapState;
    }
}
