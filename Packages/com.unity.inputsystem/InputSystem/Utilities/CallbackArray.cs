namespace UnityEngine.InputSystem.Utilities
{
    // Keeps a copy of the callback list while executing so that the callback list can safely
    // be mutated from within callbacks.
    internal struct CallbackArray<TDelegate>
        where TDelegate : System.Delegate
    {
        private InlinedArray<TDelegate> m_Callbacks;
        private InlinedArray<TDelegate> m_ExecutingCallbacks;

        public int length => m_Callbacks.length;

        public void Clear()
        {
            m_Callbacks.Clear();
            m_ExecutingCallbacks.Clear();
        }

        public void AddCallback(TDelegate dlg)
        {
            if (!m_Callbacks.Contains(dlg))
                m_Callbacks.AppendWithCapacity(dlg, capacityIncrement: 4);
        }

        public void RemoveCallback(TDelegate dlg)
        {
            var index = m_Callbacks.IndexOf(dlg);
            if (index >= 0)
                m_Callbacks.RemoveAtWithCapacity(index);
        }

        public InlinedArray<TDelegate> PrepareExecution()
        {
            m_ExecutingCallbacks.AssignWithCapacity(m_Callbacks);
            return m_ExecutingCallbacks;
        }
    }
}
