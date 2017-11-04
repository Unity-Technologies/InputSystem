using System;
using UnityEngine;

namespace ISX
{
    // An asset containing one or more action sets.
    // Usually imported from JSON using InputActionImporter.
    // Names of each action set in the asset ust be unique.
    // Allows applying overrides in bulk to all sets in the asset.
    public class InputActionAsset : ScriptableObject
    {
        public ReadOnlyArray<InputActionSet> actionSets => new ReadOnlyArray<InputActionSet>(m_ActionSets);

        public string ToJson()
        {
            return InputActionSet.ToJson(m_ActionSets);
        }

        public static InputActionAsset FromJson()
        {
            throw new NotImplementedException();
        }

        public void AddActionSet(InputActionSet set)
        {
            if (set == null)
                throw new ArgumentNullException(nameof(set));
            if (string.IsNullOrEmpty(set.name))
                throw new InvalidOperationException("Sets added to an input action asset must be named");
            ////REVIEW: some of the rules here seem stupid; just replace?
            if (TryFindActionSet(set.name) != null)
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

            var set = TryFindActionSet(name);
            if (set != null)
                RemoveActionSet(set);
        }

        public InputActionSet TryFindActionSet(string name)
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

        ////TODO: ApplyOverrides, RemoveOverrides, RemoveAllOverrides

        [SerializeField] internal InputActionSet[] m_ActionSets;
    }
}
