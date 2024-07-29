using NUnit.Framework;
using Unity.Collections;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem.Experimental
{
    [Category("Experimental")]
    public class UnsafeMulticastDelegateTests
    {
        static unsafe void AddFn(void* p)
        {
            if (p != null)
                ++(*(int*)p);
        }

        [Test]
        public void Invoke_ShouldDoNothing_IfNoCallbackHaveBeenRegistered()
        {
            unsafe
            {
                using var x = new UnsafeMulticastDelegate(AllocatorManager.Persistent);
                x.Invoke(null);
            }
        }
        
        [Test]
        public void Remove_ShouldRemoveCallback_IfPreviouslyAdded()
        {
            unsafe
            {
                int counter = 0;
                using var x = new UnsafeMulticastDelegate(AllocatorManager.Persistent);
                x.Add(&AddFn);
                x.Remove(&AddFn);
                x.Invoke(&counter);
                
                Assert.That(counter, Is.EqualTo(0));
            }
        }
        
        [Test]
        public void Remove_ShouldRemoveSingleCallback_IfPreviouslyAddedTwice()
        {
            unsafe
            {
                int counter = 0;
                using var x = new UnsafeMulticastDelegate(AllocatorManager.Persistent);
                x.Add(&AddFn);
                x.Add(&AddFn);
                x.Remove(&AddFn);
                x.Remove(&AddFn);
                x.Invoke(&counter);
                
                Assert.That(counter, Is.EqualTo(0));
            }
        }
        
        [Test]
        public void Remove_ShouldDoNothing_IfNotPreviouslyAdded()
        {
            unsafe
            {
                using var x = new UnsafeMulticastDelegate(AllocatorManager.Persistent);
                x.Remove(&AddFn);
            }
        }
        
        [Test]
        public void Add_ShouldRegisterCallback_IfAddedOnce()
        {
            unsafe
            {
                int counter = 0;
                using var x = new UnsafeMulticastDelegate(AllocatorManager.Persistent);
                x.Add(&AddFn);
                x.Invoke(&counter);
                
                Assert.That(counter, Is.EqualTo(1));
            }
        }
        
        [Test]
        public void Add_ShouldRegisterCallback_IfAddedTwice()
        {
            unsafe
            {
                int counter = 0;
                using var x = new UnsafeMulticastDelegate(AllocatorManager.Persistent);
                x.Add(&AddFn);
                x.Add(&AddFn);
                x.Invoke(&counter);
                
                Assert.That(counter, Is.EqualTo(2));
            }
        }
    }
}