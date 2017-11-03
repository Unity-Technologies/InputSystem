using System;
using System.Collections.Generic;
using UnityEngine;

namespace ISX
{
    // Used to override a default binding on an action. Can be stored externally and then
    // applied to an action or action set to alter its bindings.
    //
    // NOTE: These structs are what should be stored in user profiles to preserve
    //       user rebindings.
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

        public static InputBindingOverride[] FromJson(string json)
        {
            var overrides = JsonUtility.FromJson<InputBindingOverride[]>(json);
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
