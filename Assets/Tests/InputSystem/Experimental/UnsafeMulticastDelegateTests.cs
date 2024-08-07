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
                /*using var x = new UnsafeMulticastDelegate();
                x.Invoke(null);*/

                using var x = new UnsafeEventHandler();
                x.Invoke();
                
                using var y = new UnsafeEventHandler<int>();
                y.Invoke(5);

                using var z = new UnsafeEventHandler<int, float>();
                z.Invoke(1, 3.14f);
            }
        }

        [Test]
        public void Callback()
        {
            UnsafeDelegate<int> d = new UnsafeDelegate<int>();
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

        private static void DoCount(int x, ref int y)
        {
            y += x;
        }
        
        private static readonly unsafe delegate*<int, ref int, void> Count = &DoCount;

        private static void DoCount0(ref int x) => ++x;
        private static readonly unsafe delegate*<ref int, void> Count0 = &DoCount0;
        private static void DoSum(ref int x, int y) => x += y;
        private static readonly unsafe delegate*<ref int, int, void> Sum = &DoSum;
        private static unsafe void DoUnsafeSum(int x, void* s) => *((int*)s) += x;
        private static readonly unsafe delegate*<int, void*, void> UnsafeSum = &DoUnsafeSum;
        private static unsafe void DoUnsafeSum2(int x, int y, void* s) => *((int*)s) += (x+y);
        private static readonly unsafe delegate*<int, int, void*, void> UnsafeSum2 = &DoUnsafeSum2;
        /*[Test]
        public void Xxxx()
        {
            unsafe
            {
                int counter = 0;
                var d = UnsafeStatefulDelegate.Create(Count0, &counter);
                d.Invoke(5);
                d.Invoke(2);
                Assert.That(counter, Is.EqualTo(7));
            }
        }*/
        
        [Test]
        public void UnsafeStatefulEventHandler_ShouldInvokeCallback_IfTakingZeroArguments()
        {
            unsafe
            {
                int counter = 0;
                using var sut = new UnsafeStatefulEventHandler<int>();
                var d = UnsafeStatefulDelegate.Create(Count0, &counter);
                
                sut.Add(d);
                sut.Invoke();  // 0+1 = 1
                Assert.That(counter, Is.EqualTo(1));
                
                sut.Add(d);
                sut.Invoke();  // 1+2 = 3
                Assert.That(counter, Is.EqualTo(3));
                
                sut.Remove(d);
                sut.Invoke();  // 3+1 = 4
                Assert.That(counter, Is.EqualTo(4));
                
                sut.Remove(d);
                sut.Invoke();  // 4+0 = 4
                Assert.That(counter, Is.EqualTo(4));

                sut.Remove(d);
                sut.Invoke();  // 4+0 = 4
                Assert.That(counter, Is.EqualTo(4));

            }
        }
        
        [Test]
        public void UnsafeStatefulEventHandler_ShouldInvokeCallback_IfTakingTwoArguments()
        {
            unsafe
            {
                var sum = 0;
                using var sut = new UnsafeStatefulEventHandler<int, int>();
                var d = UnsafeStatefulDelegate.Create(Sum, &sum);
                
                sut.Add(d);
                sut.Invoke(1);
                Assert.That(sum, Is.EqualTo(1));
                
                sut.Add(d);
                sut.Invoke(2);
                Assert.That(sum, Is.EqualTo(5));
                
                sut.Remove(d);
                sut.Invoke(3);
                Assert.That(sum, Is.EqualTo(8));
                
                sut.Remove(d);
                sut.Invoke(4);
                Assert.That(sum, Is.EqualTo(8));
                
                sut.Remove(d);
                sut.Invoke(4);
                Assert.That(sum, Is.EqualTo(8));
            }
        }
        
        [Test]
        public void UnsafeEventHandler_ShouldInvokeCallback_IfTakingOneArgument()
        {
            unsafe
            {
                var sum = 0;
                using var sut = new UnsafeEventHandler<int>();
                var d = UnsafeDelegate.Create(UnsafeSum, &sum);
                
                sut.Add(d);
                sut.Invoke(1);
                Assert.That(sum, Is.EqualTo(1));
                
                sut.Add(d);
                sut.Invoke(2);
                Assert.That(sum, Is.EqualTo(5));
                
                sut.Remove(d);
                sut.Invoke(3);
                Assert.That(sum, Is.EqualTo(8));
                
                sut.Remove(d);
                sut.Invoke(4);
                Assert.That(sum, Is.EqualTo(8));
                
                sut.Remove(d);
                sut.Invoke(4);
                Assert.That(sum, Is.EqualTo(8));
            }
        }
        
        [Test]
        public void UnsafeEventHandler_ShouldInvokeCallback_IfTakingTwoArguments()
        {
            unsafe
            {
                var sum = 0;
                using var sut = new UnsafeEventHandler<int, int>();
                var d = UnsafeDelegate.Create(UnsafeSum2, &sum);
                
                sut.Add(d);
                sut.Invoke(1, 2);
                Assert.That(sum, Is.EqualTo(3));
                
                sut.Add(d);
                sut.Invoke(2, 3);
                Assert.That(sum, Is.EqualTo(13));
                
                sut.Remove(d);
                sut.Invoke(4, 5);
                Assert.That(sum, Is.EqualTo(22));
                
                sut.Remove(d);
                sut.Invoke(6, 7);
                Assert.That(sum, Is.EqualTo(22));
                
                sut.Remove(d);
                sut.Invoke(8, 9);
                Assert.That(sum, Is.EqualTo(22));
            }
        }
    }
}