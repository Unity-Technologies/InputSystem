using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace Tests.InputSystem.Experimental.Performance
{
    public class CallbacksPerformanceTests
    {
        private static int[] _sequence;

        interface IInvokable
        {
            public int Invoke();

            public void Setup(Random random);
        }
        
        private sealed class EventHandlerSubject
        {
            
        }

        public static class Operations
        {
            public static void Add(ref int sum, int x) => sum += x;
            public static void Subtract(ref int sum, int x) => sum -= x;
        }

        private sealed class DelegateSubject : IInvokable
        {
            public delegate void Aggregate(ref int sum, int x);
            public List<Aggregate> callbacks = new List<Aggregate>();

            private static readonly Aggregate add = Operations.Add;
            private static readonly Aggregate subtract = Operations.Subtract;

            public int Invoke()
            {
                var sum = 0;
                for (var i = 0; i < callbacks.Count; ++i)
                    callbacks[i].Invoke(ref sum, _sequence[i]);
                return sum;
            }

            public void Setup(Random random)
            {
                var doAdd = random.Next(2) > 0;
                callbacks.Add(doAdd ? add : subtract);
            }
        }

        private sealed class InterfaceSubject : IInvokable
        {
            public interface ICallback
            {
                public void Aggregate(ref int sum, int x);
            }

            public class Add : ICallback
            {
                public void Aggregate(ref int sum, int x) => sum += x;
            }

            public class Subtract : ICallback
            {
                public void Aggregate(ref int sum, int x) => sum += x;
            }

            private static readonly Add add = new Add();
            private static readonly Subtract subtract = new Subtract();
            public List<ICallback> callbacks = new List<ICallback>();
            
            public int Invoke()
            {
                var sum = 0;
                for (var i = 0; i < callbacks.Count; ++i)
                    callbacks[i].Aggregate(ref sum, _sequence[i]);
                return sum;
            }

            public void Setup(Random random)
            {
                var doAdd = random.Next(2) > 0;
                callbacks.Add(doAdd ? add : subtract);
            }
        }
        
        [OneTimeSetUp]
        void OneTimeSetUp()
        {
            const int n = 100;
            var random = new Random(0);
            _sequence = new int[n];
            for (var i = 0; i < n; ++i)
            {
                _sequence[i] = random.Next(10);
            }
        }
        
        public static object[] CallbackCases =
        {
            new object[] { new InterfaceSubject() },
        };
        
        [Test, Performance]
        [TestCaseSource(nameof(CallbackCases))]
        [Category("Experimental")]
        void Callbacks(IInvokable subject)
        {
            var random = new Random(0);
            subject.Setup(random);
            var result = subject.Invoke();
            Assert.That(result, Is.EqualTo(123));
        }
    }
}