using System;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem.Experimental
{
    [Category("Experimental")]
    public class UniformBufferTests
    {
        [Test]
        public void LinkedSegment()
        {
            unsafe
            {
                var segment = LinkedSegment<int>.Create(3, AllocatorManager.Temp);
                LinkedSegment<int>.Destroy(segment, AllocatorManager.Temp);    
            }
        }
        
        [Test]
        public void Uniform_Create_Dispose()
        {
            var sut = new UniformBuffer<int>(3, AllocatorManager.Persistent);
            sut.Dispose();
        }
        
        [Test]
        public void Uniform_ConstructorShouldThrow_IfGivenInvalidCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                using var s = new UniformBuffer<int>(0, AllocatorManager.Persistent);
            });
        }
        
        [Test]
        public void Uniform_PushShouldSucceed_IfSufficientCapacity()
        {
            using var sut = new UniformBuffer<int>(1, AllocatorManager.Persistent);
            sut.Push(5);
        }
        
        [Test]
        public void Uniform_PushShouldSucceed_IfInsufficientCapacity()
        {
            using var sut = new UniformBuffer<int>(1, AllocatorManager.Persistent);
            sut.Push(5);
            sut.Push(6);
        }
        
        [Test]
        public void Uniform()
        {
            using var uni = new UniformBuffer<int>(3, AllocatorManager.Persistent);

            Assert.That(uni.ToArray().Length, Is.EqualTo(0));
            
            const int n = 10;
            for (var i = 0; i < n; ++i)
            {
                // Assert push
                var length = i + 1;
                uni.Push(length);

                // Assert enumeration which is indirectly triggered by ToArray
                var values = uni.ToArray();
                Assert.That(values.Length, Is.EqualTo(length));
                for (var j = 0; j < length; ++j)
                    Assert.That(values[j], Is.EqualTo(j+1));
            }

            uni.Clear();
            Assert.That(uni.ToArray().Length, Is.EqualTo(0));
            
            uni.Push(1);
            uni.Push(2);
            uni.Push(3);
            uni.Push(4);
            
            Assert.That(uni.ToArray().Length, Is.EqualTo(4));

            using var e = uni.GetEnumerator();
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current, Is.EqualTo(1));
        }
    }
}