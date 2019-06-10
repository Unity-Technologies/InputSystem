#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Editor
{
    internal class CallbackDataSource : AdvancedDropdownDataSource
    {
        private readonly Func<AdvancedDropdownItem> m_BuildCallback;
        private readonly Func<string, IEnumerable<AdvancedDropdownItem>, AdvancedDropdownItem>
        m_SearchCallback;

        internal CallbackDataSource(Func<AdvancedDropdownItem> buildCallback,
                                    Func<string, IEnumerable<AdvancedDropdownItem>, AdvancedDropdownItem> searchCallback = null)
        {
            m_BuildCallback = buildCallback;
            m_SearchCallback = searchCallback;
        }

        protected override AdvancedDropdownItem FetchData()
        {
            return m_BuildCallback();
        }

        protected override AdvancedDropdownItem PerformCustomSearch(string searchString)
        {
            return m_SearchCallback?.Invoke(searchString, m_SearchableElements);
        }
    }
}

#endif // UNITY_EDITOR
