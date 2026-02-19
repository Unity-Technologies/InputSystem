#if UNITY_EDITOR
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Editor
{
    internal abstract class AdvancedDropdown
    {
        protected Vector2 minimumSize { get; set; }
        protected Vector2 maximumSize { get; set; }

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
                m_DataSource = new CallbackDataSource(BuildRoot, BuildCustomSearch);
            }
            if (m_Gui == null)
            {
                m_Gui = new AdvancedDropdownGUI();
            }

            m_WindowInstance = ScriptableObject.CreateInstance<AdvancedDropdownWindow>();
            if (minimumSize != Vector2.zero)
                m_WindowInstance.minSize = minimumSize;
            if (maximumSize != Vector2.zero)
                m_WindowInstance.maxSize = maximumSize;
            m_WindowInstance.state = m_State;
            m_WindowInstance.dataSource = m_DataSource;
            m_WindowInstance.gui = m_Gui;
            m_WindowInstance.windowClosed +=
                w => { ItemSelected(w.GetSelectedItem()); };
            m_WindowInstance.windowDestroyed += OnDestroy;
            m_WindowInstance.Init(rect);
        }

        public void Reload()
        {
            m_WindowInstance?.ReloadData();
        }

        public void Repaint()
        {
            m_WindowInstance?.Repaint();
        }

        protected abstract AdvancedDropdownItem BuildRoot();

        protected virtual AdvancedDropdownItem BuildCustomSearch(string searchString,
            IEnumerable<AdvancedDropdownItem> elements)
        {
            return null;
        }

        protected virtual void ItemSelected(AdvancedDropdownItem item)
        {
        }

        protected virtual void OnDestroy()
        {
        }
    }
}
#endif
