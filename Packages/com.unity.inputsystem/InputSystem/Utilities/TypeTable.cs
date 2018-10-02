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

        public IEnumerable<string> names
        {
            get { return table.Keys.Select(x => x.ToString()); }
        }

        public IEnumerable<InternedString> internedNames
        {
            get { return table.Keys; }
        }

        public void Initialize()
        {
            table = new Dictionary<InternedString, Type>();
        }

        public InternedString FindNameForType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            foreach (var pair in table)
                if (pair.Value == type)
                    return pair.Key;
            return new InternedString();
        }

        public void AddTypeRegistration(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name");
            if (type == null)
                throw new ArgumentNullException("type");

            var internedName = new InternedString(name);
            table[internedName] = type;
        }

        public Type LookupTypeRegistration(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name");

            Type type;
            var internedName = new InternedString(name);
            if (table.TryGetValue(internedName, out type))
                return type;
            return null;
        }
    }
}
