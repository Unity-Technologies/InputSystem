#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class AdvancedDropdownItem : IComparable
    {
        string m_Name;
        Texture2D m_Icon;
        int m_Id;
        int m_ElementIndex = -1;
        bool m_Enabled = true;
        List<AdvancedDropdownItem> m_Children = new List<AdvancedDropdownItem>();

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public Texture2D icon
        {
            get { return m_Icon; }
            set { m_Icon = value; }
        }

        public int id
        {
            get
            {
                return m_Id;
            }
            set { m_Id = value; }
        }

        internal int elementIndex
        {
            get { return m_ElementIndex; }
            set { m_ElementIndex = value; }
        }

        public bool enabled
        {
            get { return m_Enabled; }
            set { m_Enabled = value; }
        }

        public IEnumerable<AdvancedDropdownItem> children
        {
            get { return m_Children; }
        }

        protected string m_SearchableName;
        public virtual string searchableName
        {
            get
            {
                return string.IsNullOrEmpty(m_SearchableName) ? name : m_SearchableName;
            }
        }

        public void AddChild(AdvancedDropdownItem child)
        {
            m_Children.Add(child);
        }

        static readonly AdvancedDropdownItem k_SeparatorItem = new SeparatorDropdownItem();

        public AdvancedDropdownItem(string name)
        {
            m_Name = name;
            m_Id = name.GetHashCode();
        }

        public virtual int CompareTo(object o)
        {
            return name.CompareTo((o as AdvancedDropdownItem).name);
        }

        public void AddSeparator()
        {
            AddChild(k_SeparatorItem);
        }

        internal bool IsSeparator()
        {
            return k_SeparatorItem == this;
        }

        public override string ToString()
        {
            return m_Name;
        }

        class SeparatorDropdownItem : AdvancedDropdownItem
        {
            public SeparatorDropdownItem() : base("SEPARATOR")
            {
            }
        }
    }
}

#endif // UNITY_EDITOR
