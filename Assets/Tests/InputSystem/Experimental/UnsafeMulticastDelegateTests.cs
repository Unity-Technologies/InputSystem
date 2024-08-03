using System;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem.Experimental
{
    [Category("Experimental")]
    public class UnsafeMulticastDelegateTests
    {
        static unsafe void CounterFn(void* p)
        {
            if (p != null)
                ++(*(int*)p);
        }
        
        static unsafe void CounterFn2(ref int p)
        {
            ++p;
        }
        
        static unsafe void RemoveFn(void* p)
        {
            if (p != null)
            {
                var s = (State*)p;
                ++s->counter;
                
                s->d.Remove(RemoveDelegate);
                //s->d.Remove(RemoveDelegate);
            }
        }

        [Test]
        public void Invoke_ShouldDoNothing_IfNoCallbackHaveBeenRegistered()
        {
            unsafe
            {
                //using var x = new UnsafeMulticastDelegate();
                //x.Invoke(null);

                using var sut = new UnsafeEventHandler<int>();
                sut.Invoke(5);
            }
        }
        
        // TODO Reports leak
        [Test]
        public void Remove_ShouldRemoveCallback_IfPreviouslyAdded()
        {
            unsafe
            {
                int counter = 0;
                using var x = new UnsafeMulticastDelegate();
                //using var x = new UnsafeEventHandler<ref int>();
                x.Add(&CounterFn);
                x.Remove(&CounterFn);
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
                using var x = new UnsafeMulticastDelegate();
                x.Add(&CounterFn);
                x.Add(&CounterFn);
                x.Remove(&CounterFn);
                x.Remove(&CounterFn);
                x.Invoke(&counter);
                
                Assert.That(counter, Is.EqualTo(0));
            }
        }
        
        [Test]
        public void Remove_ShouldDoNothing_IfNotPreviouslyAdded()
        {
            unsafe
            {
                using var x = new UnsafeMulticastDelegate();
                x.Remove(&CounterFn);
            }
        }
        
        [Test]
        public void Add_ShouldRegisterCallback_IfAddedOnce()
        {
            unsafe
            {
                int counter = 0;
                using var x = new UnsafeMulticastDelegate();
                x.Add(&CounterFn);
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
                using var x = new UnsafeMulticastDelegate();
                x.Add(&CounterFn);
                x.Add(&CounterFn);
                x.Invoke(&counter);
                
                Assert.That(counter, Is.EqualTo(2));
            }
        }

        struct State : IDisposable
        {
            public UnsafeMulticastDelegate d;
            public int counter;

            public void Dispose()
            {
                d.Dispose();
            }
        }

        private static readonly unsafe delegate*<void*, void> RemoveDelegate = &RemoveFn;
        
        [Test]
        public void Remove_ShouldRemoveCallback_IfCalledFromCallback()
        {
            unsafe
            {
                using var s = TempPointer<State>.Create();
                s.value.d = new UnsafeMulticastDelegate();
                s.value.counter = 0;
                s.value.d.Add(RemoveDelegate); // TODO Requires static cached callback, it seems, sort out
                s.value.d.Invoke(s.pointer); // Note: Callback unregisters itself
                s.value.d.Invoke(s.pointer); // Note: No callback since removed from first callback
                Assert.That(s.value.counter, Is.EqualTo(1));
                
                s.value.d.Dispose();
            }
        }
    }
}