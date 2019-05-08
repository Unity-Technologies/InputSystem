#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Editor
{
    internal class AdvancedDropdownItem : IComparable
    {
        internal readonly List<AdvancedDropdownItem> m_Children = new List<AdvancedDropdownItem>();

        public string name { get; set; }
        public Texture2D icon { get; set; }
        public int id { get; set; }
        public bool enabled { get; set; } = true;
        public int indent { get; set; }

        internal int elementIndex { get; set; } = -1;

        public IEnumerable<AdvancedDropdownItem> children => m_Children;

        protected string m_SearchableName;
        public virtual string searchableName => string.IsNullOrEmpty(m_SearchableName) ? name : m_SearchableName;

        public void AddChild(AdvancedDropdownItem child)
        {
            m_Children.Add(child);
        }

        static readonly AdvancedDropdownItem k_SeparatorItem = new SeparatorDropdownItem();

        public AdvancedDropdownItem(string name)
        {
            this.name = name;
            id = name.GetHashCode();
        }

        public virtual int CompareTo(object o)
        {
            return name.CompareTo((o as AdvancedDropdownItem).name);
        }

        public void AddSeparator(string label = null)
        {
            if (string.IsNullOrEmpty(label))
                AddChild(k_SeparatorItem);
            else
                AddChild(new SeparatorDropdownItem(label));
        }

        internal bool IsSeparator()
        {
            return this is SeparatorDropdownItem;
        }

        public override string ToString()
        {
            return name;
        }

        private class SeparatorDropdownItem : AdvancedDropdownItem
        {
            public SeparatorDropdownItem(string label = "")
                : base(label)
            {
            }
        }
    }
}

#endif // UNITY_EDITOR
