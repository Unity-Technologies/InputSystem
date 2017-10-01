using System;
using System.Collections.Generic;

////REVIEW: rename InputControlUsage?

namespace InputSystem
{
	// Usages determine the meaning of controls. Naming of controls
	// may differ between devices but many of the controls between
	// devices have common patterns of use.
	// Every usage has a unique name and an associated value type.
    public struct InputUsage
    {
        public string name { get; private set; }
        public string template { get; private set; }

        public InputUsage(string name, string template)
        {
            this.name = name;
            this.template = template;
        }

        // This dictionary is owned and managed by InputManager.
        internal static Dictionary<string, InputUsage> s_Usages = new Dictionary<string, InputUsage>();

        internal static InputUsage? TryGetUsage(string name)
        {
            InputUsage usage;
            if (s_Usages.TryGetValue(name, out usage))
                return usage;
            return null;
        }

        internal static InputUsage GetUsage(string name)
        {
            InputUsage usage;
            if (!s_Usages.TryGetValue(name, out usage))
                throw new Exception($"No input usage called '{name}' has been registered");
            return usage;
        }
    }
}