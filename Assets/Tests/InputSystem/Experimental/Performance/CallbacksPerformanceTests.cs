using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace Tests.InputSystem.Experimental.Performance
{
    // This is a test fixture for micro-benchmarking various approaches to callbacks.
    // Out of these it currently looks like multicast delegate is 3 times faster but should
    // be tested on optimized build. Problem with multicast delegate is its GC pressure related
    // to copy-on-write semantics.
    [Category("Experimental")]
    [TestFixture]
    public class CallbacksPerformanceTests
    {
        private const int kSequenceLength = 100;
        private const int kHandlerCount = 10;
        
        private static int[] _sequence;
        private static bool[] _add;
        private static int _expected;
        private static int _actual;

        public interface IInvokable
        {
            public void Invoke();

            public void Setup();
        }
        
        private static class Operations
        {
            public static void Add(int x) => _actual += x;
            public static void Subtract(int x) => _actual -= x;
        }
        
        private sealed class MulticastDelegateSubject : IInvokable
        {
            private delegate void Aggregate(int x);
            private Aggregate m_Callbacks;
            private static readonly Aggregate Add = Operations.Add;
            private static readonly Aggregate Subtract = Operations.Subtract;

            public void Invoke()
            {
                for (var i=0; i < kSequenceLength; ++i)
                    m_Callbacks.Invoke(_sequence[i]);
            }

            public void Setup()
            {
                m_Callbacks = _add[0] ? Add : Subtract;
                for (var i = 1; i < _add.Length; ++i)
                {
                    if (_add[i])
                        m_Callbacks += Add;
                    else
                        m_Callbacks += Subtract;
                }
            }
        }
        
        private sealed class DelegateSubject : IInvokable
        {
            private delegate void Aggregate(int x);
            private readonly List<Aggregate> m_Callbacks = new List<Aggregate>();
            private static readonly Aggregate Add = Operations.Add;
            private static readonly Aggregate Subtract = Operations.Subtract;

            public void Invoke()
            {
                for (var i = 0; i < kSequenceLength; ++i)
                {
                    for (var j = 0; j < kHandlerCount; ++j)
                        m_Callbacks[j].Invoke(_sequence[i]);
                }
            }

            public void Setup()
            {
                // For each entry of sequence we want to perform
                for (var i=0; i < _add.Length; ++i)
                    m_Callbacks.Add(_add[i] ? Add : Subtract);
            }
        }

        private sealed class InterfaceSubject : IInvokable
        {
            public interface ICallback
            {
                public void Aggregate(int x);
            }

            private class AddCallback : ICallback
            {
                public void Aggregate(int x) => _actual += x;
            }

            private class SubtractCallback : ICallback
            {
                public void Aggregate(int x) => _actual -= x;
            }

            private static readonly AddCallback Add = new AddCallback();
            private static readonly SubtractCallback Subtract = new SubtractCallback();
            private readonly List<ICallback> m_Callbacks = new List<ICallback>();
            
            public void Invoke()
            {
                for (var i = 0; i < kSequenceLength; ++i)
                {
                    for (var j = 0; j < kHandlerCount; ++j)
                        m_Callbacks[j].Aggregate(_sequence[i]);
                }
            }

            public void Setup()
            {
                for (var i=0; i < _add.Length; ++i)
                    m_Callbacks.Add(_add[i] ? Add : Subtract);
            }
        }
        
        [OneTimeSetUp]
        public static void OneTimeSetUp()
        {
            var random = new Random(0);
            
            _sequence = new int[kSequenceLength];
            for (var i = 0; i < kSequenceLength; ++i)
                _sequence[i] = random.Next(10);
            
            _add = new bool[kHandlerCount];
            for (var i = 0; i < kHandlerCount; ++i)
                _add[i] = random.Next(2) > 0;

            _expected = 0;
            for (var i = 0; i < kSequenceLength; ++i)
            {
                for (var j = 0; j < kHandlerCount; ++j)
                {
                    if (_add[j])
                        _expected += _sequence[i];
                    else
                        _expected -= _sequence[i];    
                }
            }
        }
        
        public static object[] CallbackCases =
        {
            new object[] { new MulticastDelegateSubject() },
            new object[] { new DelegateSubject() },
            new object[] { new InterfaceSubject() },
        };

        [Test, Performance]
        [TestCaseSource(nameof(CallbackCases))]
        public void Callbacks(IInvokable subject)
        {
            subject.Setup();
            
            Measure.Method(() =>
                {
                    _actual = 0;
                    subject.Invoke();
                })
                .MeasurementCount(100)
                .WarmupCount(5)
                .Run();
            
            Assert.That(_actual, Is.EqualTo(_expected));
        }
    }
}