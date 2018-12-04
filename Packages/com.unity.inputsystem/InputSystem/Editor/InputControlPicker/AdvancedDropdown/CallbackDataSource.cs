#if UNITY_EDITOR
using System;
using UnityEngine;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class CallbackDataSource : AdvancedDropdownDataSource
    {
        Func<AdvancedDropdownItem> m_BuildCallback;

        internal CallbackDataSource(Func<AdvancedDropdownItem> buildCallback)
        {
            m_BuildCallback = buildCallback;
        }

        protected override AdvancedDropdownItem FetchData()
        {
            return m_BuildCallback();
        }
    }
}

#endif // UNITY_EDITOR
