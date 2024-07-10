using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Unity.Collections;
using UnityEditor.Build.Reporting;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;
using UnityEngine.TestTools;
using Usages = UnityEngine.InputSystem.Experimental.Devices.Usages;
using Vector2 = UnityEngine.Vector2;

// TODO Do we need a FixedInput type?

namespace Tests.InputSystem
{
    public interface INode
    {
        public string DoIt();
    }

    public struct C<T> : INode where T : INode
    {
        private readonly T m_Node;
        public C(T node) { this.m_Node = node; }
        public string DoIt() => $"C({m_Node.DoIt()})";
    }

    public struct B<T> : INode where T : INode
    {
        private readonly T m_Node;
        public B(T node) { this.m_Node = node; }
        public string DoIt() => $"B({m_Node.DoIt()})";
    }

    public struct A : INode
    {
        public string DoIt() => "A";
    }

    public static class NodeExtensions
    {
        public static B<T> b<T>(this T obj) where T : INode
        {
            return new B<T>();
        }
        
        public static C<T> c<T>(this T obj) where T : INode
        {
            return new C<T>();
        }
    }
    
    internal static class ContextExtensions
    {
        public static void GivenStreamWithData<T>(this Context context, Usage key, params T[] values) where T : struct
        {
            var s = context.CreateStream(key, values[0]);
            for (var i = 1; i < values.Length; ++i)
                s.OfferByRef(ref values[i]);
        }
    }

    [Category("Experimental")]
    internal class Experimental_Tests
    {
        private Context m_Context;
        private int m_MoveCount;
        private int m_JumpCount;

        [SetUp]
        public void SetUp()
        {
            m_Context = new Context();
        }

        [TearDown]
        public void TearDown()
        {
            m_Context?.Dispose();
        }

        private void Move(Vector2 value)
        {
            ++m_MoveCount;
        }

        private void Jump(InputEvent evt)
        {
            ++m_JumpCount;
        }

        private struct ControlModel<T>
        {
            
        }
        
        private struct ButtonControlModel
        {
            
        }
        
        private struct Source<T, TControlModel>
        {
            private TControlModel m_ControlModel;
        }
        
        /*private struct TestJob : IJob
        {
            public float x;
            public float y;
            public NativeArray<float> result;   // target stream
            
            public void Execute()
            {
                result[0] = x + y;
            }
        }*/
        
        /*[Test]
        public void Dots()
        {
            //using NativeArray<int> data = new NativeArray<int>(new int[]{1,2,3,4,5}, Allocator.TempJob);
            using var result = new NativeArray<float>(1, Allocator.TempJob);
            var job = new TestJob
            {
                result = result,
                x = 1,
                y = 4
            };
            var handle = job.Schedule();
            handle.Complete();
            Assert.That(handle.IsCompleted, Is.True);
        }*/

        private static readonly object[] EndpointTestCases = new object[]
        {
            new object[]{ Endpoint.FromUsage(Usages.GamepadUsages.ButtonEast), Usages.GamepadUsages.ButtonEast, Endpoint.AnySource, SourceType.Device },
            new object[]{ Endpoint.FromDeviceAndUsage(13, Usages.GamepadUsages.ButtonSouth), Usages.GamepadUsages.ButtonSouth, 13, SourceType.Device }
        };
        
        [Test]
        [TestCaseSource(nameof(EndpointTestCases))]
        public void Endpoint_Test(Endpoint endPoint, Usage expectedUsage, int expectedSourceId, SourceType expectedSourceType)
        {
            Assert.That(endPoint.usage, Is.EqualTo(expectedUsage));
            Assert.That(endPoint.sourceId, Is.EqualTo(expectedSourceId));
            Assert.That(endPoint.sourceType, Is.EqualTo(expectedSourceType));
        }

        [Test]
        public void Chain()
        {
            C<B<A>> chain1;
            A a;
            var chain2 = a.b().c();
            Assert.That(chain1.DoIt(), Is.EqualTo("C(B(A))"));
            Assert.That(chain2.DoIt(), Is.EqualTo("C(B(A))"));
        }
        
        [Test]
        public void Stream()
        {
            using var s = new Stream<int>(Usages.GamepadUsages.LeftStick, 100);

            // Even though stream is initialized with an initial value this value should not be part of changed values
            {
                var values = s.ToArray();
                Assert.That(values.Length, Is.EqualTo(0));
                Assert.That(s.Previous, Is.EqualTo(100));
                
                s.Advance();
            }
            
            // Still no new values
            {
                var values = s.ToArray();
                Assert.That(values.Length, Is.EqualTo(0));
            }
            
            // Offer some values resulting in changes
            {
                s.OfferByValue(1);
                s.OfferByValue(2);
                s.OfferByValue(3);
                s.OfferByValue(4);
                s.OfferByValue(5);

                Assert.That(s.AsSpan().Length, Is.EqualTo(5));
                Assert.That(s.AsExtendedSpan().Length, Is.EqualTo(6));
            }
        }
        
        [Test]
        public void Observe()
        {
            // Composite.Create(Keyboard.w.value, Keyboard.d.value, (w, d, v) => v = w);
            //Gamepad.LeftStick.Map(x => x.magnitude > 0.5f).Pressed()
        }

        [Ignore("Implementation needs fixing")]
        [Test]
        public void Varying()
        {
            //using var buf = new VaryingBuffer(128, AllocatorManager.Persistent);
            //for (var i=0; i < 100; ++i)
            //    buf.Push(5);
        }

        

        // TODO What if all bindable operations where picked up by a code generator which made them detectable via registration?!
        // [RegisterInputBinding] private InputBinding<> binding;
        // ...and in OnEnable we use the binding.

        public class TestData
        {
            public static object[] DisplayNameCases =
            {
                new object[] { Gamepad.LeftStick, "Gamepad" },
                new object[] { 12, 2, 6 },
                new object[] { 12, 4, 3 }
            };
        }
        
        public static object[] DivideCases =
        {
            new object[] { 12, 3, 4 },
            new object[] { 12, 2, 6 },
            new object[] { 12, 4, 3 }
        };
        
        
        
        // [TestCaseSource(nameof(DivideCases))]
        
        

        [Test]
        public void Concept()
        {
            var data = new ListObserver<Vector2>();
            using var subscription = Gamepad.LeftStick.Subscribe(m_Context, data);

            var stick = Gamepad.LeftStick.Stub(m_Context);
            stick.Change(Vector2.zero);
            stick.Change(Vector2.left);
            stick.Change(Vector2.right);
            m_Context.Update();

            // Note: Initial state not reported via regular stream inspection
            Assert.That(data.Next.Count, Is.EqualTo(3));
            Assert.That(data.Next[0], Is.EqualTo(Vector2.zero));
            Assert.That(data.Next[1], Is.EqualTo(Vector2.left));
            Assert.That(data.Next[2], Is.EqualTo(Vector2.right));
        }

        // TODO Consider EnumerableInput, separated from ObservableInput
        [Test]
        public void Reader()
        {
            var data = new ListObserver<Vector2>();
            
            using var reader = Gamepad.ButtonEast.Subscribe(m_Context);
            Assert.That(reader.ToArray().Length, Is.EqualTo(0));

            var button = Gamepad.ButtonEast.Stub(m_Context);
            button.Press();
            button.Release();
            //m_Context.Update(); // TODO Incorrect with this pattern

            var values = reader.ToArray();
            Assert.That(values.Length, Is.EqualTo(2));
            Assert.That(values[0], Is.EqualTo(true));
            Assert.That(values[1], Is.EqualTo(false));
        }

        class CustomConstraint : Constraint
        {
            public override ConstraintResult ApplyTo(object actual)
            {
                throw new NotImplementedException();
            }
        }

        /*private class Is : NUnit.Framework.Is
        {
            public Constraint IsObserving(IEnumerable<T> sequence)    
        }*/
        
        
        
        // TODO Consider how we want this to work, basically should we omit initial value directly on Subscribe?
        [Test]
        public void UseCase_DirectAccess()
        {
            var button = Gamepad.ButtonSouth.Stub(m_Context, initialValue: true);
            
            var data = new ListObserver<bool>();
            using var subscription = Gamepad.ButtonSouth.Subscribe(m_Context, data);

            m_Context.Update();
            Assert.That(data.Next.Count, Is.EqualTo(1));
            Assert.That(data.Next[0], Is.EqualTo(true));
            
            button.Press();
            button.Release();
            m_Context.Update();
            
            Assert.That(data.Next.Count, Is.EqualTo(3));
            Assert.That(data.Next[1], Is.EqualTo(true));
            Assert.That(data.Next[2], Is.EqualTo(false));
        }

        /*[Test]
        public void ObservableWrapper() // TODO FIX
        {
            var button = Gamepad.ButtonEast.Stub(m_Context);
            using var s = Gamepad.ButtonEast.Pressed().Subscribe(m_Context, new DebugLogObserver<InputEvent>());
            
            button.Press();
            m_Context.Update();
            
            LogAssert.Expect($"OnNext: {new InputEvent().ToString()}");
        }*/

        [Test]
        public void Output_Direct()
        {
            Gamepad.RumbleHaptic.Offer(1.0f);
            
            // TODO Assert from provider perspective
        }

        [Test]
        public void Output_Indirect()
        {
            var rumble = new BindableOutput<float>(Gamepad.RumbleHaptic);
            rumble.Offer(1.0f);
        }
        
        // TODO Verify initial state, e.g. is button already actuated, release triggers release event
        // TODO Verify that initial state is properly recorded so we e.g. may start in actuated state on first sync
        // TODO Local multiplayer, basically a binding filter, but probably good to let sources get assigned to players since physical control makes sense to assign to players. Let devices have a flag.
        // TODO Cover scenarios similar to Value, PassThrough, Button, e.g.
        // TODO Trigger once vs trigger once a frame, vs trigger on received value
        // TODO Phases are related to interactions. Phases associated with data makes no sense, but how do we handle touch?
        // TODO Disambuigitation is better solved on binding level where one can either merge or select?
        // TODO Verify that shortcut and press interactions where they overlap mutes press interaction
    }
}
