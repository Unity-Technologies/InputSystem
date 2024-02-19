using System;
using UnityEngine.Profiling;

namespace UnityEngine.InputSystem.Utilities
{
    // Similar to EventHandler<TDelegate> class but instead of using atomics to implement a copy-on-write semantic
    // to allow concurrent modification this keeps a copy of the callback list while executing so that the callback
    // list can safely be mutated from within callbacks after callbacks have finished.
    // Note that this implementation is not thread-safe compared to EventHandler<TDelegate>.
    internal struct CallbackArray<TDelegate>
        where TDelegate : System.Delegate
    {
        private bool m_Locked;
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
            if (m_Locked)
            {
                Modify(ref m_CallbacksToAdd, ref m_CallbacksToRemove, dlg);
            }
            else if (!m_Callbacks.Contains(dlg))
            {
                m_Callbacks.AppendWithCapacity(dlg, capacityIncrement: 4);
            }
        }

        public void RemoveCallback(TDelegate dlg)
        {
            if (m_Locked)
            {
                Modify(ref m_CallbacksToRemove, ref m_CallbacksToAdd, dlg);
            }
            else
            {
                var index = m_Callbacks.IndexOf(dlg);
                if (index >= 0)
                    m_Callbacks.RemoveAtWithCapacity(index);
            }
        }

        public void LockForChanges()
        {
            m_Locked = true;
        }

        public void UnlockForChanges()
        {
            m_Locked = false;

            // Process mutations that have happened while we were executing callbacks.
            for (var i = 0; i < m_CallbacksToRemove.length; ++i)
                RemoveCallback(m_CallbacksToRemove[i]);
            for (var i = 0; i < m_CallbacksToAdd.length; ++i)
                AddCallback(m_CallbacksToAdd[i]);

            m_CallbacksToAdd.Clear();
            m_CallbacksToRemove.Clear();
        }

        private static void Modify(ref InlinedArray<TDelegate> deferred, ref InlinedArray<TDelegate> inverseDeferred, TDelegate dlg)
        {
            // If the list of deferred
            if (deferred.Contains(dlg))
                return;
            var index = inverseDeferred.IndexOf(dlg);
            if (index != -1)
                inverseDeferred.RemoveAtByMovingTailWithCapacity(index);
            deferred.AppendWithCapacity(dlg);
        }
    }
}
