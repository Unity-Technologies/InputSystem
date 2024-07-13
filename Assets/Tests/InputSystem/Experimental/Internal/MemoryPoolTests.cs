using System;
using System.Buffers;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Pool;

namespace Tests.InputSystem.Experimental
{
    internal sealed class MemoryPool
    {
        private readonly AllocatorManager.AllocatorHandle m_Allocator;

        public MemoryPool(AllocatorManager.AllocatorHandle allocator)
        {
            m_Allocator = allocator;
        }
        
        public unsafe void* Allocate(int sizeBytes, int alignmentInBytes)
        {
            return AllocatorManager.Allocate(m_Allocator, sizeBytes, alignmentInBytes, 1);
        }

        public unsafe void Free(void* ptr)
        {
            AllocatorManager.Free(m_Allocator, ptr);
        }
    }

    internal sealed class ManagedObjectPool<T> : IDisposable
    {
        private T[] m_Objects;
        
        public ManagedObjectPool(int initialCapacity)
        {
            m_Objects = new T[initialCapacity];
        }

        public void Dispose()
        {
            m_Objects = null;
        }
    }

    internal sealed class UnmanagedObjectPool<T> 
    {
        
    }

    internal sealed class MultiTypeArrayPool
    {
        private readonly Dictionary<Type, object> m_ArrayPools = new();

        public T[] Rent<T>(int n)
        {
            if (n <= 0)
                throw new ArgumentOutOfRangeException(nameof(n));
            
            return GetPool<T>().Rent(n);
        }

        public void Return<T>(T[] value)
        {
            GetPool<T>().Return(value);               
        }

        private ArrayPool<T> GetPool<T>()
        {
            if (m_ArrayPools.TryGetValue(typeof(T), out var opaquePool))
                return (ArrayPool<T>)opaquePool;
            
            var pool = ArrayPool<T>.Create();    
            m_ArrayPools.Add(typeof(T), pool);
            return pool;
        }
    }

    [Category("Experimental")]
    internal class MultiTypeArrayPoolTests
    {
        [Test]
        public void RentReturn()
        {
            var sut = new MultiTypeArrayPool();
            
            var x = sut.Rent<int>(3);
            Assert.That(x.Length, Is.GreaterThanOrEqualTo(3));
            sut.Return(x);
            var x3 = sut.Rent<int>(3);
            Assert.True(ReferenceEquals(x3, x));
        }
    }
}