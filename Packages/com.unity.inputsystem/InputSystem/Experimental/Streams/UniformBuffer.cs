using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

// TODO Add safety checks
// TODO Make compliant with Job system and burst
// TODO Implement readonly variant
// TODO Benchmark copying value vs dereferencing pointer in enumerator
// TODO Find a good way to expose enumeration over segments as ReadOnlySpan<T>

// TODO Test with TypeEncoded

// TODO FIX Some kind of access violation currently happening

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// A resizeable buffer holding elements of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// The buffer utilizes a greedy allocation scheme where data is organized into linked segments in memory.
    /// Hence the buffer is similar to a deque when pressure is high on the container and is similar to a fixed
    /// array when pressure is low and capacity is sufficient.
    /// The buffer automatically allocates new segments with an exponential growth allocation strategy when running
    /// out of capacity but do not deallocate until Clear() is called, in which case all but the last segment
    /// are dropped and deallocated. 
    /// </remarks>
    /// <typeparam name="T">The element type.</typeparam>
    public unsafe struct UniformBuffer<T> : IDisposable, IEnumerable<T>
        where T : unmanaged 
    {
        private LinkedSegment<T>* m_Head;
        private LinkedSegment<T>* m_Tail;
        private readonly AllocatorManager.AllocatorHandle m_Allocator;

        public UniformBuffer(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Head = LinkedSegment<T>.Create(capacity, allocator);
            m_Tail = m_Head;
            m_Allocator = allocator;
        }
        
        public void Dispose()
        {
            CheckCreated(); 
            m_Head = Prune(null);
            m_Tail = null;
        }

        public void Push(T value)
        {
            Push(ref value);
        }
        
        public void Push(ref T value)
        {
            CheckCreated();
            
            // If we run out of capacity, we allocate a new segment, link the current tail segment to the new 
            // segment and let the newly allocated segment become the new tail segment.
            if (m_Tail->Length == m_Tail->Capacity)
            {
                var segment = LinkedSegment<T>.Create(m_Tail->Capacity * 2, m_Allocator); 
                m_Tail->Next = segment;
                m_Tail = segment;
            }   
            
            // Finally, write the new element to the underlying memory
            UnsafeUtility.WriteArrayElement(m_Tail->Ptr, m_Tail->Length++, value);
        }

        public void Clear()
        {
            CheckCreated();
            
            m_Head = Prune(m_Tail);
            m_Head->Next = null;
            m_Head->Length = 0;
        }

        private LinkedSegment<T>* Prune(LinkedSegment<T>* end)
        {
            for (var current = m_Head; current != end; /* no-op */)
            {
                var temp = current;
                current = current->Next;
                LinkedSegment<T>.Destroy(temp, m_Allocator);
            }
            return end;
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckCreated()
        {
            if ((IntPtr)m_Head == IntPtr.Zero)
                throw new Exception("Container not created");

            //AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
        }
        
        public struct Enumerator : IEnumerator<T>
        {
            private readonly UniformBuffer<T> m_Container;  // Note: This is a copy
            private LinkedSegment<T> m_CurrentSegment;      // Note: This is a copy
            private int m_Index;
            private T m_Value;
            
            public Enumerator(ref UniformBuffer<T> container)
            {
                m_Container = container;
                m_CurrentSegment = *m_Container.m_Head;
                m_Index = -1;
                m_Value = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                // Attempt to move to next item in current segment
                if (++m_Index < m_CurrentSegment.Length)
                {
                    //AtomicSafetyHandle.CheckReadAndThrow(this.m_Array.m_Safety);
                    m_Value = UnsafeUtility.ReadArrayElement<T>(m_CurrentSegment.Ptr, m_Index);
                    return true;
                }
                
                // Move to next segment if such a segment exist 
                if (m_CurrentSegment.Next != null)
                {
                    m_CurrentSegment = *m_CurrentSegment.Next;
                    if (m_CurrentSegment.Length > 0)
                    {
                        m_Value = UnsafeUtility.ReadArrayElement<T>(m_CurrentSegment.Ptr, 0);
                        m_Index = 0;
                        return true;
                    }
                }
                
                // End of enumerable range reached
                m_Value = default;
                return false;
            }

            public void Reset()
            {
                m_Index = -1;
                m_CurrentSegment = *m_Container.m_Head; // copy
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Value;
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_Value;
            }

            public void Dispose() { }
        }
        
        public Enumerator GetEnumerator() => new (container: ref this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(container: ref this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}