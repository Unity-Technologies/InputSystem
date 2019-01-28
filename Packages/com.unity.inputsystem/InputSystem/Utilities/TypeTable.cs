using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Experimental.Input.Utilities
{
    /// <summary>
    /// A table mapping names to types in a case-insensitive mapping.
    /// </summary>
    internal struct TypeTable
    {
        public Dictionary<InternedString, Type> table;

        public IEnumerable<string> names => table.Keys.Select(x => x.ToString());
        public IEnumerable<InternedString> internedNames => table.Keys;

        // In the editor, we want to keep track of when the same type gets registered multiple times
        // with different names so that we can keep the aliases out of the UI.
        #if UNITY_EDITOR
        public HashSet<InternedString> aliases;
        #endif

        public void Initialize()
        {
            table = new Dictionary<InternedString, Type>();
            #if UNITY_EDITOR
            aliases = new HashSet<InternedString>();
            #endif
        }

        public InternedString FindNameForType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            foreach (var pair in table)
                if (pair.Value == type)
                    return pair.Key;
            return new InternedString();
        }

        public void AddTypeRegistration(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var internedName = new InternedString(name);

            #if UNITY_EDITOR
            if (table.ContainsValue(type))
                aliases.Add(internedName);
            #endif

            table[internedName] = type;
        }

        public Type LookupTypeRegistration(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            var internedName = new InternedString(name);
            if (table.TryGetValue(internedName, out var type))
                return type;
            return null;
        }
    }
}
