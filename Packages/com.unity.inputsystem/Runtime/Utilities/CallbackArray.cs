namespace UnityEngine.InputSystem.Utilities
{
    // Keeps a copy of the callback list while executing so that the callback list can safely
    // be mutated from within callbacks.
    internal struct CallbackArray<TDelegate>
        where TDelegate : System.Delegate
    {
        private bool m_CannotMutateCallbacksArray;
        private InlinedArray<TDelegate> m_Callbacks;
        private InlinedArray<TDelegate> m_CallbacksToAdd;
        private InlinedArray<TDelegate> m_CallbacksToRemove;

        public int length => m_Callbacks.length;

        public TDelegate this[int index] => m_Callbacks[index];

        public void Clear()
        {
            m_Callbacks.Clear();
            m_CallbacksToAdd.Clear();
            m_CallbacksToRemove.Clear();
        }

        public void AddCallback(TDelegate dlg)
        {
            if (m_CannotMutateCallbacksArray)
            {
                if (m_CallbacksToAdd.Contains(dlg))
                    return;
                var removeIndex = m_CallbacksToRemove.IndexOf(dlg);
                if (removeIndex != -1)
                    m_CallbacksToRemove.RemoveAtByMovingTailWithCapacity(removeIndex);
                m_CallbacksToAdd.AppendWithCapacity(dlg);
                return;
            }

            if (!m_Callbacks.Contains(dlg))
                m_Callbacks.AppendWithCapacity(dlg, capacityIncrement: 4);
        }

        public void RemoveCallback(TDelegate dlg)
        {
            if (m_CannotMutateCallbacksArray)
            {
                if (m_CallbacksToRemove.Contains(dlg))
                    return;
                var addIndex = m_CallbacksToAdd.IndexOf(dlg);
                if (addIndex != -1)
                    m_CallbacksToAdd.RemoveAtByMovingTailWithCapacity(addIndex);
                m_CallbacksToRemove.AppendWithCapacity(dlg);
                return;
            }

            var index = m_Callbacks.IndexOf(dlg);
            if (index >= 0)
                m_Callbacks.RemoveAtWithCapacity(index);
        }

        public void LockForChanges()
        {
            m_CannotMutateCallbacksArray = true;
        }

        public void UnlockForChanges()
        {
            m_CannotMutateCallbacksArray = false;

            // Process mutations that have happened while we were executing callbacks.
            for (var i = 0; i < m_CallbacksToRemove.length; ++i)
                RemoveCallback(m_CallbacksToRemove[i]);
            for (var i = 0; i < m_CallbacksToAdd.length; ++i)
                AddCallback(m_CallbacksToAdd[i]);

            m_CallbacksToAdd.Clear();
            m_CallbacksToRemove.Clear();
        }
    }
}
