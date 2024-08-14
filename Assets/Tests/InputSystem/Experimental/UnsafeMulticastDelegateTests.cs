using System;
using NUnit.Framework;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem.Experimental
{
    [Category("Experimental")]
    public class UnsafeMulticastDelegateTests
    {
        private struct Self : IDisposable
        {
            public UnsafeEventHandler Handler;
            public UnsafeDelegate Callback;
            public int Counter;
            
            public void Dispose() => Handler.Dispose();
        }

        private static unsafe void DoRemoveSelf(void* state)
        {
            var remove = (Self*)state;
            ++remove->Counter;
            remove->Handler.Remove(remove->Callback);
        }
        
        private static unsafe void DoAddSelf(void* state)
        {
            var add = (Self*)state;
            ++add->Counter;
            add->Handler.Add(add->Callback);
        }

        private static void DoCount0(ref int x) => ++x;
        private static readonly unsafe delegate*<ref int, void> Count0 = &DoCount0;
        
        private static void DoSum(ref int x, int y) => x += y;
        private static readonly unsafe delegate*<ref int, int, void> Sum = &DoSum;
        
        private static unsafe void DoUnsafeSum(int x, void* s) => *((int*)s) += x;
        private static readonly unsafe delegate*<int, void*, void> UnsafeSum = &DoUnsafeSum;
        
        private static unsafe void DoUnsafeSum2(int x, int y, void* s) => *((int*)s) += (x+y);
        private static readonly unsafe delegate*<int, int, void*, void> UnsafeSum2 = &DoUnsafeSum2;
        
        [Test]
        public void Invoke_ShouldDoNothing_IfNoCallbackHaveBeenRegistered()
        {
            using var x = new UnsafeEventHandler();
            x.Invoke();
                
            using var y = new UnsafeEventHandler<int>();
            y.Invoke(5);

            using var z = new UnsafeEventHandler<int, float>();
            z.Invoke(1, 3.14f);
        }
        
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
        
        [Test]
        public void UnsafeEventHandler_Add_ShouldAddCallback_IfInvokedFromCallback()
        {
            unsafe
            {
                var self = new Self();
                
                try
                {
                    self.Callback = UnsafeDelegate.Create(&DoAddSelf, &self);
                    self.Handler.Add(self.Callback);
                
                    self.Handler.Invoke();
                    Assert.That(self.Counter, Is.EqualTo(1));
                
                    self.Handler.Invoke();
                    Assert.That(self.Counter, Is.EqualTo(3));
                }
                finally
                {
                    self.Dispose();
                }
                
            }
        }
        
        [Test]
        public void UnsafeEventHandler_Remove_ShouldRemoveCallback_IfInvokedFromCallback()
        {
            unsafe
            {
                var self = new Self();

                try
                {
                    self.Callback = UnsafeDelegate.Create(&DoRemoveSelf, &self);
                    self.Handler.Add(self.Callback);
                
                    self.Handler.Invoke();
                    Assert.That(self.Counter, Is.EqualTo(1));
                
                    self.Handler.Invoke();
                    Assert.That(self.Counter, Is.EqualTo(1));
                }
                finally
                {
                    self.Dispose();
                }
            }
        }
        
        [Test]
        public void UnsafeEventHandler_Remove_ShouldDoNothing_IfNotPreviouslyAdded()
        {
            unsafe
            {
                var sum = 0;
                using var sut = new UnsafeEventHandler<int, int>();
                
                sut.Remove(UnsafeDelegate.Create(UnsafeSum2, &sum));
                sut.Invoke(8, 9);
                Assert.That(sum, Is.EqualTo(0));
                
                sut.Remove(default);
                sut.Invoke(8, 9);
                Assert.That(sum, Is.EqualTo(0));
            }
        }

        [Test]
        public void UnsafeEventHandler_Add_ShouldThrow_IfInvalidCallback()
        {
            using var sut = new UnsafeEventHandler();
            Assert.Throws<ArgumentException>(() => sut.Add(default));
        }
    }
}