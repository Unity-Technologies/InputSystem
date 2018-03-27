using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Used to override a default binding on an action.
    /// </summary>
    /// <remarks>
    /// Can be stored externally and then applied to an action or action set
    /// to alter its bindings.
    ///
    /// These structs are what should be stored in user profiles to preserve
    /// user rebindings.
    /// </remarks>
    [Serializable]
    public struct InputBindingOverride
    {
        // Name of the action. Can either be a plain name or a "set/action" combination.
        public string action;

        // New binding path.
        public string binding;

        // There may be multiple bindings on the given action. If so, we can choose
        // to override a specific one using this field. If there are multiple bindings
        // and this field is *not* set, the override will insert itself as an additional
        // binding before any other binding.
        public string group;

        ////REVIEW: I think this is nonsense; seems brittle and non-obvious
        // The group may have a '[number]' suffix to address a specific binding when multiple
        // bindings use the same group. This method returns the index.
        // If no index is set for the group, returns 0.
        public int GetIndexInGroup(out int groupStringLength)
        {
            groupStringLength = group.Length;
            if (group == null || group[groupStringLength - 1] != ']')
                return 0;

            var indexOfLeftBracket = group.IndexOf('[');
            if (indexOfLeftBracket == -1)
                return 0;

            groupStringLength = indexOfLeftBracket;
            return StringHelpers.ParseInt(group, indexOfLeftBracket + 1);
        }

        public static InputBindingOverride[] FromJson(string json)
        {
            //var overrides = JsonUtility.FromJson<InputBindingOverride[]>(json);
            throw new NotImplementedException();
        }

        public static string ToJson(IEnumerable<InputBindingOverride> overrides)
        {
            throw new NotImplementedException();
        }

        [Serializable]
        private struct BindingFileJson
        {
            public InputBindingOverride[] overrides;
        }
    }
}
