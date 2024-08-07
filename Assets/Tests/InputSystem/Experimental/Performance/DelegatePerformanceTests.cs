using System;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem.Experimental.Performance
{
    public class DelegatePerformanceTests
    {
        private const int delegateCallCount = 10;
        private const int result = 170085;
        private const int measurementCount = 10000;
        
        private static int counter;
    
        private static void Count1() => ++counter;
        private static void Count2() => counter += 2;
        private static unsafe void Count1X(void* ptr) => ++(*(int*)ptr);
        private static unsafe void Count2X(void* ptr) => (*(int*)ptr) += 2;
        private static unsafe void Count1Y(void* ptr) => ++counter;
        private static unsafe void Count2Y(void* ptr) => counter += 2;
        
        private static readonly unsafe delegate*<void> Count1Delegate = &Count1;
        private static readonly unsafe delegate*<void> Count2Delegate = &Count2;
        private static unsafe delegate*<void*, void> _count1XDelegate = &Count1X;
        private static unsafe delegate*<void*, void> _count2XDelegate = &Count2X;
        private static unsafe delegate*<void*, void> _count1YDelegate = &Count1Y;
        private static unsafe delegate*<void*, void> _count2YDelegate = &Count2Y;

        
        private class Container
        {
            public int counter;
        }

        [SetUp]
        public void SetUp()
        {
            counter = 0;
        }

        private void Benchmark(Action action)
        {
            Measure.Method(action)
                .MeasurementCount(measurementCount)
                .SampleGroup(new SampleGroup("Time", SampleUnit.Nanosecond))
                .WarmupCount(5)
                .Run();
        }

        private static T[] Setup<T>(T first, T second)
        {
            var delegates = new T[delegateCallCount];
            var random = new System.Random(0);
            for (var i = 0; i < delegateCallCount; ++i)
            {
                delegates[i] = random.Next(2) switch
                {
                    0 => first,
                    1 => second,
                    _ => throw new Exception()
                };
            }
            return delegates;
        }
        
        [Test, Performance]
        [Category("Experimental")]
        public void StatelessDelegateCallbacks()
        {
            var delegates = Setup<Action>(Count1, Count2);
            Benchmark(() =>
            {
                for (var i = 0; i < delegateCallCount; ++i)
                    delegates[i].Invoke();
            });
        
            Assert.That(counter, Is.EqualTo(result));
        }
        
        [Test, Performance]
        [Category("Experimental")]
        public void StatefulDelegateCallbacks()
        {
            var container = new Container();
            var delegates = Setup<Action>(() => ++container.counter, () => container.counter += 2);
            Benchmark(() =>
                {
                    for (var i=0; i < delegateCallCount; ++i) 
                        delegates[i].Invoke();
                });
        
            Assert.That(container.counter, Is.EqualTo(result));
        }
        
        [Test, Performance]
        [Category("Experimental")]
        public unsafe void StatelessUnsafeDelegateCallbacks()
        {
            var delegates = Setup(new UnsafeStatelessDelegate(Count1Delegate), 
                new UnsafeStatelessDelegate(Count2Delegate));
            
            Benchmark(() =>
                {
                    for (var i=0; i < delegateCallCount; ++i) 
                        delegates[i].Invoke();
                });
        
            Assert.That(counter, Is.EqualTo(result));
        }
        
        [Test, Performance]
        [Category("Experimental")]
        public unsafe void StatefulUnsafeDelegateCallbacks()
        {
            using var x = TempPointer<int>.Create();
            x.value = 0;

            var delegates = Setup(new UnsafeDelegate(_count1XDelegate, x.pointer),
                new UnsafeDelegate(_count2XDelegate, x.pointer));

            Benchmark(() =>
            {
                for (var i = 0; i < delegateCallCount; ++i)
                    delegates[i].Invoke();
            });
        
            Assert.That(x.value, Is.EqualTo(result));
        }
        
        private event EventHandler handler;
        
        [Test, Performance]
        [Category("Experimental")]
        public unsafe void EventHandler()
        {
            var random = new System.Random(0);
            for (var i = 0; i < delegateCallCount; ++i)
            {
                handler += random.Next(2) switch
                {
                    0 => (sender, args) => { ++counter; },
                    1 => (sender, args) => { counter += 2; },
                    _ => throw new Exception()
                };
            }

            Benchmark(() =>
            {
                handler?.Invoke(this, EventArgs.Empty);
            });
        
            Assert.That(counter, Is.EqualTo(result));
        }
        
        [Test, Performance]
        [Category("Experimental")]
        public unsafe void UnsafeEventHandler()
        {
            var handler = new UnsafeEventHandler();
            var random = new System.Random(0);
            for (var i = 0; i < delegateCallCount; ++i)
            {
                handler.Add(random.Next(2) switch
                {
                    0 => new UnsafeDelegate(_count1YDelegate),
                    1 => new UnsafeDelegate(_count2YDelegate),
                    _ => throw new Exception()
                });
            }

            Benchmark(() =>
            {
                handler.Invoke();
            });
        
            Assert.That(counter, Is.EqualTo(result));
        }
    }
}