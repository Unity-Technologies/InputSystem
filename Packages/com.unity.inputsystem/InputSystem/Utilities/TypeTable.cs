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

        public SavedState SaveState()
        {
            var count = table.Count;
            var entries = new SavedState.Entry[count];

            var i = 0;
            foreach (var entry in table)
                entries[i++] = new SavedState.Entry
                {
                    name = entry.Key,
                    typeName = entry.Value.AssemblyQualifiedName
                };

            return new SavedState { entries = entries };
        }

        public void RestoreState(SavedState state, string displayName)
        {
            if (state.entries == null)
                return;

            foreach (var entry in state.entries)
            {
                var name = new InternedString(entry.name);
                if (table.ContainsKey(name))
                    continue;
                var type = Type.GetType(entry.typeName, false);
                if (type != null)
                    table[name] = Type.GetType(entry.typeName, true);
                else
                    Debug.Log(string.Format("{0} '{1}' has been removed (type '{2}' cannot be found)",
                        displayName, entry.name, entry.typeName));
            }
        }

        [Serializable]
        public struct SavedState
        {
            public struct Entry
            {
                public string name;
                public string typeName;
            }

            public Entry[] entries;
        }
    }
}
