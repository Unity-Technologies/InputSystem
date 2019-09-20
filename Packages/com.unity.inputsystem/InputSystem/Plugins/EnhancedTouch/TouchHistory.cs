using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.EnhancedTouch
{
    /// <summary>
    /// A fixed-size buffer of <see cref="Touch"/> records used to trace the history of touches.
    /// </summary>
    /// <remarks>
    /// This struct provides access to a recorded list of touches.
    /// </remarks>
    public struct TouchHistory : IReadOnlyList<Touch>
    {
        private readonly InputStateHistory<TouchState> m_History;
        private readonly Finger m_Finger;
        private readonly int m_Count;
        private readonly int m_StartIndex;
        private readonly uint m_Version;

        internal TouchHistory(Finger finger, InputStateHistory<TouchState> history, int startIndex = -1, int count = -1)
        {
            m_Finger = finger;
            m_History = history;
            m_Version = history.version;
            m_Count = count >= 0 ? count : m_History.Count;
            m_StartIndex = startIndex >= 0 ? startIndex : m_History.Count - 1;
        }

        /// <summary>
        /// Enumerate touches in the history. Goes from newest records to oldest.
        /// </summary>
        /// <returns>Enumerator over the touches in the history.</returns>
        public IEnumerator<Touch> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Number of history records available.
        /// </summary>
        public int Count => m_Count;

        /// <summary>
        /// Return a history record by index. Indexing starts at 0 == newest to <see cref="Count"/> - 1 == oldest.
        /// </summary>
        /// <param name="index">Index of history record.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or >= <see cref="Count"/>.</exception>
        public Touch this[int index]
        {
            get
            {
                CheckValid();
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(
                        $"Index {index} is out of range for history with {Count} entries", nameof(index));

                // History records oldest-first but we index newest-first.
                return new Touch(m_Finger, m_History[m_StartIndex - index]);
            }
        }

        internal void CheckValid()
        {
            if (m_Finger == null || m_History == null)
                throw new InvalidOperationException("Touch history not initialized");
            if (m_History.version != m_Version)
                throw new InvalidOperationException(
                    "Touch history is no longer valid; the recorded history has been changed");
        }

        private class Enumerator : IEnumerator<Touch>
        {
            private readonly TouchHistory m_Owner;
            private int m_Index;

            internal Enumerator(TouchHistory owner)
            {
                m_Owner = owner;
                m_Index = -1;
            }

            public bool MoveNext()
            {
                if (m_Index >= m_Owner.Count - 1)
                    return false;
                ++m_Index;
                return true;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            public Touch Current => m_Owner[m_Index];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}
