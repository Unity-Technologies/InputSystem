using System;
using NUnit.Framework;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem.Experimental
{
    [Category("Experimental")]
    public class FixedPoolTests
    {
        [Test]
        public void RentAndReturn_ShouldAllowReusingMemory_IfSufficientCapacity()
        {
            var pool = new FixedObjectPool<IDisposable>(10);
            
            var a0 = pool.Rent(3);
            Assert.That(a0, Is.Not.Null);
            Assert.That(a0.Count, Is.EqualTo(3));
            
            var a1 = pool.Rent(1);
            Assert.That(a1, Is.Not.Null);
            Assert.That(a1.Count, Is.EqualTo(1));
            
            var a2 = pool.Rent(2);
            Assert.That(a2, Is.Not.Null);
            Assert.That(a2.Count, Is.EqualTo(2));
            
            var a3 = pool.Rent(3);
            Assert.That(a3, Is.Not.Null);
            Assert.That(a3.Count, Is.EqualTo(3));
            
            Assert.Throws<Exception>(() => pool.Rent(2));
            
            pool.Return(a0);
            pool.Return(a1); // Except a0 and a1 to be merged (left)

            // Note: This wouldn't be possible without defragmentation
            var a4 = pool.Rent(4);
            Assert.That(a4, Is.Not.Null);
            Assert.That(a4.Count, Is.EqualTo(4));
            
            pool.Return(a3);

            pool.Return(a4);
            pool.Return(a2);

            var a5 = pool.Rent(10);
            Assert.That(a5, Is.Not.Null);
        }

        [Test]
        public void Return_ShouldThrow_IfSegmentDoesntBelongToPool()
        {
            var pool = new FixedObjectPool<object>(10);
            Assert.Throws<ArgumentException>(() => pool.Return(new ArraySegment<object>()));
        }
    }
}