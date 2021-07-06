using System;

namespace UnityEngine.InputSystem.Utilities
{
    // Keeps a copy of the callback list while executing so that the callback list can safely
    // be mutated from within callbacks.
    internal struct CallbackArray<TDelegate>
        where TDelegate : System.Delegate
    {
        private bool m_IsExecuting;
        private InlinedArray<TDelegate> m_Callbacks;
        private InlinedArray<TDelegate> m_BackupCallbacks;

        public int length => m_IsExecuting && m_BackupCallbacks.length > 0
        ? m_BackupCallbacks.length
        : m_Callbacks.length;

        public void Clear()
        {
            m_Callbacks.Clear();
            m_BackupCallbacks.Clear();
        }

        public void AddCallback(TDelegate dlg)
        {
            if (m_IsExecuting && m_BackupCallbacks.length ==  0)
                m_BackupCallbacks.AssignWithCapacity(m_Callbacks);
            if (!m_Callbacks.Contains(dlg))
                m_Callbacks.AppendWithCapacity(dlg, capacityIncrement: 4);
        }

        public void RemoveCallback(TDelegate dlg)
        {
            if (m_IsExecuting && m_BackupCallbacks.length ==  0)
                m_BackupCallbacks.AssignWithCapacity(m_Callbacks);
            var index = m_Callbacks.IndexOf(dlg);
            if (index >= 0)
                m_Callbacks.RemoveAtWithCapacity(index);
        }

        public void StartExecuting()
        {
            m_IsExecuting = true;
        }

        public void FinishExecuting()
        {
            m_IsExecuting = false;
            m_BackupCallbacks = default; // Becomes garbage.
        }

        public TDelegate this[int index]
        {
            get
            {
                if (m_IsExecuting && m_BackupCallbacks.length > 0)
                    return m_BackupCallbacks[index];
                return m_Callbacks[index];
            }
        }
    }
}
