using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem
{
    internal unsafe struct UnsafeListObserver<T> : IDisposable
        where T : unmanaged
    {
        private struct State
        {
            public NativeList<T> List;
            public AllocatorManager.AllocatorHandle Allocator;
        }

        private State* m_Ptr;
        
        public UnsafeListObserver(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Ptr = (State*)allocator.Allocate(sizeof(State), UnsafeUtility.AlignOf<State>());
            m_Ptr->List = new NativeList<T>(initialCapacity, allocator);
            m_Ptr->Allocator = allocator;
        }

        public void Dispose()
        {
            if (m_Ptr == null)
                return;
            
            m_Ptr->List.Dispose();
            AllocatorManager.Free(m_Ptr->Allocator, m_Ptr, sizeof(State), UnsafeUtility.AlignOf<State>());
            m_Ptr = null;
        }
        
        public UnsafeDelegate<T> ToDelegate()
        {
            if (m_Ptr == null)
                throw new Exception("Not created");
            
            return new UnsafeDelegate<T>(&OnNext, m_Ptr);
        }

        private static void OnNext(T value, void* state)
        {
            var ptr = (State*)state;
            ptr->List.Add(value);
        }

        public ReadOnlySpan<T> next => m_Ptr->List.AsReadOnly().AsReadOnlySpan();
    }
}