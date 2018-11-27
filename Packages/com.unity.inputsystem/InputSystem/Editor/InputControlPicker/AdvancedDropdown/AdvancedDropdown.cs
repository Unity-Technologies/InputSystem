#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEngine.Experimental.Input.Editor
{
    internal abstract class AdvancedDropdown
    {
        private Vector2 m_MinimumSize;
        protected Vector2 minimumSize
        {
            get { return m_MinimumSize; }
            set { m_MinimumSize = value; }
        }

        private Vector2 m_MaximumSize;
        protected Vector2 maximumSize
        {
            get { return m_MaximumSize; }
            set { m_MaximumSize = value; }
        }

        internal AdvancedDropdownWindow m_WindowInstance;
        internal AdvancedDropdownState m_State;
        internal AdvancedDropdownDataSource m_DataSource;
        internal AdvancedDropdownGUI m_Gui;

        public AdvancedDropdown(AdvancedDropdownState state)
        {
            m_State = state;
        }

        public void Show(Rect rect)
        {
            if (m_WindowInstance != null)
            {
                m_WindowInstance.Close();
                m_WindowInstance = null;
            }
            if (m_DataSource == null)
            {
                m_DataSource = new CallbackDataSource(BuildRoot);
            }
            if (m_Gui == null)
            {
                m_Gui = new AdvancedDropdownGUI();
            }

            m_WindowInstance = ScriptableObject.CreateInstance<AdvancedDropdownWindow>();
            if (m_MinimumSize != Vector2.zero)
                m_WindowInstance.minSize = m_MinimumSize;
            if (m_MaximumSize != Vector2.zero)
                m_WindowInstance.maxSize = m_MaximumSize;
            m_WindowInstance.state = m_State;
            m_WindowInstance.dataSource = m_DataSource;
            m_WindowInstance.gui = m_Gui;
            m_WindowInstance.windowClosed += (w) => ItemSelected(w.GetSelectedItem());
            m_WindowInstance.Init(rect);
        }

        internal void SetFilter(string searchString)
        {
            m_WindowInstance.searchString = searchString;
        }

        protected abstract AdvancedDropdownItem BuildRoot();

        protected virtual void ItemSelected(AdvancedDropdownItem item)
        {
        }
    }
}
#endif
