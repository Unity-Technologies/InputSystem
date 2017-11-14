using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISX
{
    // An asset containing one or more action sets.
    // Usually imported from JSON using InputActionImporter.
    // Names of each action set in the asset ust be unique.
    // Allows applying overrides in bulk to all sets in the asset.
    //
    // NOTE: You don't have to use action sets this way. InputActionAsset
    //       is a ready-made way to use Unity's default serialization and
    //       have action sets go into the asset database. However, you can
    //       just as well have action sets directly as JSON in your game.
    public class InputActionAsset : ScriptableObject, ICloneable
    {
        ////REVIEW: simply call ".input" instead of ".inputactions"?
        public const string kExtension = "inputactions";

        public ReadOnlyArray<InputActionSet> actionSets => new ReadOnlyArray<InputActionSet>(m_ActionSets);

        // Return a JSON representation of the asset.
        public string ToJson()
        {
            return InputActionSet.ToJson(m_ActionSets);
        }

        // Replace the contents of the asset with the action sets in the
        // given JSON string.
        public void FromJson(string json)
        {
            m_ActionSets = InputActionSet.FromJson(json);
        }

        public void AddActionSet(InputActionSet set)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));
            if (string.IsNullOrEmpty(set.name))
                throw new InvalidOperationException("Sets added to an input action asset must be named");
            ////REVIEW: some of the rules here seem stupid; just replace?
            if (TryGetActionSet(set.name) != null)
                throw new InvalidOperationException($"An action set called '{set.name}' already exists in the asset");

            ArrayHelpers.Append(ref m_ActionSets, set);
        }

        public void RemoveActionSet(InputActionSet set)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));

            ArrayHelpers.Erase(ref m_ActionSets, set);
        }

        public void RemoveActionSet(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var set = TryGetActionSet(name);
            if (set != null)
                RemoveActionSet(set);
        }

        public InputActionSet TryGetActionSet(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            if (m_ActionSets == null)
                return null;

            for (var i = 0; i < m_ActionSets.Length; ++i)
            {
                var set = m_ActionSets[i];
                if (string.Compare(name, set.name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return set;
            }

            return null;
        }

        public InputActionSet GetActionSet(string name)
        {
            var set = TryGetActionSet(name);
            if (set == null)
                throw new KeyNotFoundException($"Could not find an action set called '{name}' in asset '{this}'");
            return set;
        }

        public InputActionAsset Clone()
        {
            // Can't MemberwiseClone() ScriptableObject. Unfortunatly, Unity doesn't
            // prevent the call. Result will be a duplicate wrapper object, though.
            var clone = (InputActionAsset)CreateInstance(GetType());
            clone.m_ActionSets = ArrayHelpers.Clone(m_ActionSets);
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        ////TODO: ApplyOverrides, RemoveOverrides, RemoveAllBindingOverrides

        [SerializeField] internal InputActionSet[] m_ActionSets;
    }
}
