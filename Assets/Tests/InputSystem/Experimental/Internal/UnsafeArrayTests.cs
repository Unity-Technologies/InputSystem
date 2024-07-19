using System;
using NUnit.Framework;
using Unity.Collections;

namespace Tests.InputSystem.Experimental
{
    [Category("Experimental")]
    public class UnsafeArrayTests
    {
        [Test]
        public void Constructor_ShouldNotAllocate_IfConstructedWithoutInitialSize()
        {
            unsafe
            {
                using var a = new UnsafeArray<int>(AllocatorManager.Temp);
                Assert.That(a.length, Is.EqualTo(0));
                Assert.That(a.capacity, Is.EqualTo(0));
                Assert.That((IntPtr)a.data, Is.EqualTo((IntPtr)null));
            }
        }
        
        [Test]
        public void Resize_ShouldAllocate_IfInitiallyNotAllocated()
        {
            unsafe
            {
                using var a = new UnsafeArray<int>(AllocatorManager.Temp);
                a.Resize(5);
                Assert.That(a.length, Is.EqualTo(5));
                Assert.That(a.capacity, Is.GreaterThanOrEqualTo(5));
                Assert.That((IntPtr)a.data, Is.Not.Null);
                a[0] = 0;
                a[1] = 1;
                a[2] = 2;
                a[3] = 3;
                a[4] = 4;
            }
        }
        
        [Test]
        public void SubscriptOperator_ShouldAssign_IfValidRangeAndHavingSufficientCapacity()
        {
            unsafe
            {
                using var a = new UnsafeArray<int>(AllocatorManager.Temp);
                a.Resize(3);
                a[0] = 0;
                a[1] = 1;
                a[2] = 2;
                a[3] = 3;
                a[4] = 4;
            }
        }
    }
}