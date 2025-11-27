using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#if UNITY_EDITOR
using System.ComponentModel;
#endif

namespace UnityEngine.InputSystem.Utilities
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

        // Strong coupling to Input Manager which is always the owner
        private InputManager m_Manager;

        public void Initialize(InputManager manager)
        {
            table = new Dictionary<InternedString, Type>();
            #if UNITY_EDITOR
            aliases = new HashSet<InternedString>();
            #endif
            m_Manager = manager;
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
            // First check if the name has already been added to the table
            // This allows safely calling this function multiple times with same name
            // and prevents adding an alias to itself
            Type originalType;
            if (table.TryGetValue(internedName, out originalType))
            {
                // We don't warn about overriding any existing name/type mapping as that was previously supported
                table.Remove(internedName);
            }
            else
            {
                if (table.ContainsValue(type))
                    aliases.Add(internedName);
            }
            #endif

            table[internedName] = type;
        }

        public Type LookupTypeRegistration(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (table == null)
                throw new InvalidOperationException("Input System not yet initialized");

            return TryLookupTypeRegistration(new InternedString(name));
        }

        private Type TryLookupTypeRegistration(InternedString internedName)
        {
            if (!table.TryGetValue(internedName, out var type))
            {
                // Failed to look-up type, either type do not exist or it is a custom type that has not been registered.
                // Check whether we have attempted to load custom types and otherwise lazily load  types only when
                // relevant and reattempt looking up type by name. (ISXB-1766)
                if (m_Manager != null)
                {
                    if (m_Manager.RegisterCustomTypes())
                        table.TryGetValue(internedName, out type);
                }
            }

            return type;
        }

        #if UNITY_EDITOR
        public bool ShouldHideInUI(string name)
        {
            // Always hide aliases.
            if (aliases.Contains(new InternedString(name)))
                return true;

            // Hide entries that have [DesignTimeVisible(false)] on the type.
            var type = LookupTypeRegistration(name);
            var attribute = type?.GetCustomAttribute<DesignTimeVisibleAttribute>();
            return !(attribute?.Visible ?? true);
        }

        #endif
    }
}
