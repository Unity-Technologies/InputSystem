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
        public void LinkedSegment()
        {
            unsafe
            {
                var segment = LinkedSegment<int>.Create(3, AllocatorManager.Temp);
                LinkedSegment<int>.Destroy(segment, AllocatorManager.Temp);    
            }
        }
        
        //[Ignore("Implementation needs fixing")]
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
        
        public static IEnumerable<(IDependencyGraphNode, string)> DisplayNameCases()
        {
            yield return (Gamepad.LeftStick, "Gamepad.LeftStick");
            yield return (Gamepad.ButtonEast, "Gamepad.ButtonEast");
            yield return (Gamepad.ButtonEast.Pressed(), "Pressed( Gamepad.ButtonEast )");
        }
        
        // [TestCaseSource(nameof(DivideCases))]
        
        [Test]
        [TestCaseSource(nameof(DisplayNameCases))]
        public void Describe((IDependencyGraphNode node, string expectedDisplayName) td)
        {
            Assert.That(td.node.Describe(), Is.EqualTo(td.expectedDisplayName));
            
            //if (node is IDisposable)
              //  ((IDisposable)node).Dispose();

            //using var s1 = Gamepad.LeftStick.Subscribe();
            // TODO Gamepad.leftStick.Subscribe();
            // TODO Gamepad.leftStick.Player(1).Subscribe();	       // Filter for gamepad assigned to player 1
            // TODO Gamepad.leftStick.Filter((x) => x >= 0.5f);	       // Convert to boolean
            // TODO InputBinding.Max( Gamepad.leftStick, Gamepad.rightStick); // Contains merged data from left or right stick
            // TODO InputBinding.First( Gamepad.leftStick, Gamepad.rightStick); // Contains data form first applicable binding
            // TODO InputBinding.Max( Gamepad.buttonSouth.Pressed(), Gamepad.buttonEast.Pressed() ).Once();

            //Assert.That(Gamepad.buttonSouth.Describe(), Is.EqualTo("Gamepad.buttonSouth"));
            //Assert.That(Gamepad.buttonSouth.Pressed().Describe(), Is.EqualTo("Press( Gamepad.buttonSouth )"));

            // https://www.youtube.com/watch?v=bFHvgqLUDbE

            // TODO Handle the following conceptual things:
            // - Gamepad.LeftStick.Continuous();
            // - Gamepad.LeftStick.OncePerFrame().Subscribe((v) => MoveRelative(v * Time.deltaTime));
            // - Gamepad.LeftStick.Value; // Direct access to value

            //using var r = Gamepad.buttonSouth.Pressed().Subscribe(m_Context);
            //r.Describe();
        }

        [Test]
        public void Dot()
        {
            var buffer = new StringBuilder();
            var g = new Digraph(Gamepad.ButtonSouth)
            {
                name = "G",
                title = "Title",
                fontSize = 9,
                font = "Arial"
            };
            Assert.That(g.Build(), Is.EqualTo(@"digraph G {
   label=""Title""
   rankdir=""LR""
   node [shape=rect]
   graph [fontname=""Arial"" fontsize=9]
   node [fontname=""Arial"" fontsize=9]
   edge [fontname=""Arial"" fontsize=9]
   node0 [label=""Gamepad.ButtonSouth""]
}"));

            const string commonPrefix = @"digraph {
   rankdir=""LR""
   node [shape=rect]
   graph [fontname=""Source Code Pro"" fontsize=12]
   node [fontname=""Source Code Pro"" fontsize=12]
   edge [fontname=""Source Code Pro"" fontsize=12]";
            
            Assert.That(Gamepad.ButtonSouth.ToDot(), Is.EqualTo(commonPrefix + @"
   node0 [label=""Gamepad.buttonSouth""]
}"));
            
            Assert.That(Gamepad.ButtonSouth.Pressed().ToDot(), Is.EqualTo(commonPrefix + @"
   node0 [label=""Pressed""]
   node1 [label=""Gamepad.ButtonSouth""]
   node0 -> node1
}"));
        }

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
        
        [Test]
        public void CombineLatest()
        {
            var button0 = Gamepad.ButtonEast.Stub(m_Context);
            var button1 = Gamepad.ButtonSouth.Stub(m_Context);
            var observer = new ListObserver<ValueTuple<bool, bool>>();
            using var subscription = Combine.Latest(Gamepad.ButtonEast, Gamepad.ButtonSouth).Subscribe(m_Context, observer);
            
            button0.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
            Assert.That(observer.Next[0], Is.EqualTo(new ValueTuple<bool, bool>(true, false)));
            
            button1.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
            Assert.That(observer.Next[1], Is.EqualTo(new ValueTuple<bool, bool>(true, true)));
            
            button0.Release();
            button1.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(4));
            Assert.That(observer.Next[2], Is.EqualTo(new ValueTuple<bool, bool>(false, true)));
            Assert.That(observer.Next[3], Is.EqualTo(new ValueTuple<bool, bool>(false, false)));
        }

        [Test]
        public void Chord()
        {
            var button0 = Gamepad.ButtonEast.Stub(m_Context);
            var button1 = Gamepad.ButtonSouth.Stub(m_Context);
            var observer = new ListObserver<bool>();
            using var subscription = Combine.Chord(Gamepad.ButtonEast, Gamepad.ButtonSouth).Subscribe(m_Context, observer);
            
            button0.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(0));
            
            button1.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
            Assert.That(observer.Next[0], Is.EqualTo(true));
            
            button0.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
            Assert.That(observer.Next[1], Is.EqualTo(false));
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
        
        [Test]
        public void Shortcut_Test()
        {
            var button0 = Gamepad.ButtonEast.Stub(m_Context);
            var button1 = Gamepad.ButtonSouth.Stub(m_Context);
            var observer = new ListObserver<bool>();
            using var subscription = Shortcut.Create(Gamepad.ButtonEast, Gamepad.ButtonSouth).Subscribe(m_Context, observer);
                
            button0.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(0));
            
            button1.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
            Assert.That(observer.Next[0], Is.EqualTo(true));
            
            button0.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
            Assert.That(observer.Next[1], Is.EqualTo(false));
            
            button1.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
            
            button1.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
            
            button0.Press(); // Should not trigger if button0 (modifier) is pressed after button1
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
        }
        
        [Test]
        public void Press()
        {
            var button = Gamepad.ButtonEast.Stub(m_Context);
            var observer = new ListObserver<InputEvent>();
            using var subscription = Gamepad.ButtonEast.Pressed().Subscribe(m_Context, observer);
            
            // Press should trigger event
            button.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
            
            // Press should trigger event also when released afterwards
            button.Release();
            button.Press();
            button.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));

            // Do not expect event when unsubscribed
            subscription.Dispose();
            button.Press();
            button.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(2));
        }

        [Test]
        public void Release()
        {
            var button = Gamepad.ButtonNorth.Stub(m_Context);
            var observer = new ListObserver<InputEvent>();
            using var subscription = Gamepad.ButtonNorth.Released().Subscribe(m_Context, observer);
            
            // Press should not trigger event
            button.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(0));
            
            // Release (should trigger event)
            button.Release();
            button.Press();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));

            // Do not expect event when unsubscribed
            subscription.Dispose();
            button.Press();
            button.Release();
            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
        }
        
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

        [Test]
        public void DebugObserver()
        {
            var button = Gamepad.ButtonEast.Stub(m_Context);
            using var s = Gamepad.ButtonEast.Pressed().Subscribe(m_Context, new DebugObserver<InputEvent>());
            
            button.Press();
            m_Context.Update();
            
            LogAssert.Expect($"OnNext: {new InputEvent().ToString()}");
        }

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

        [Test]
        public void Action_Test()
        {
            var subscription = Gamepad.ButtonSouth.Pressed().Subscribe((InputEvent evt) => { /* Jump */  });
            subscription.Dispose();
        }
        
        // See https://www.google.com/url?sa=t&source=web&rct=j&opi=89978449&url=https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers&ved=2ahUKEwiTqPXjhZCHAxWjT6QEHYuMCzoQFnoECBQQAQ&usg=AOvVaw1KAsN4qvHk0_AnbLat7S6M
        /*[Test]
        public void UnsafeFp()
        {
            unsafe
            {
                // NOT possible in current C# version
                delegate*<void> a1 = &Function();
            }
        }*/

        [Test]
        public void Filter_Test()
        {
            var stick = Gamepad.LeftStick.Stub(m_Context);
            
            var observer = new ListObserver<Vector2>();
            using var opaqueSubscription = Gamepad.LeftStick.Filter((v) => v.x >= 0.5f).Subscribe(m_Context, observer);
            //using var subscription = Gamepad.LeftStick.Filter<Vector2>(v => v.x >= 0.5f).Subscribe(m_Context, observer);
            
            stick.Change(new Vector2(0.4f, 0.0f));
            stick.Change(new Vector2(0.5f, 0.0f));
            stick.Change(new Vector2(0.6f, 0.1f));
            stick.Change(new Vector2(0.3f, 0.2f));
            
            m_Context.Update();
            
            Assert.That(observer.Next.Count, Is.EqualTo(2));
            Assert.That(observer.Next[0], Is.EqualTo(new Vector2(0.5f, 0.0f)));
            Assert.That(observer.Next[1], Is.EqualTo(new Vector2(0.6f, 0.1f)));
        }
        
        [Test]
        public void ValueFilter_Test()
        {
            var stick = Gamepad.LeftTrigger.Stub(m_Context);
            
            var observer = new ListObserver<float>();
            using var subscription = Gamepad.LeftTrigger.LowPassFilter().Subscribe(m_Context, observer);
            
            stick.Change(0.0f);
            stick.Change(0.1f);
            stick.Change(0.2f);
            stick.Change(0.9f);
            stick.Change(0.5f);
            
            m_Context.Update();
            
            Assert.That(observer.Next.Count, Is.EqualTo(5));
            Assert.That(observer.Next[0], Is.EqualTo(0.0f));
        }
        
        [Test]
        public void Merge()
        {
            var east = Gamepad.ButtonEast.Stub(m_Context);
            var north = Gamepad.ButtonNorth.Stub(m_Context);
            
            var output = new ListObserver<bool>();
            using var mux = Combine.Merge(Gamepad.ButtonEast, Gamepad.ButtonNorth).Subscribe(m_Context, output);

            m_Context.Update();

            Assert.That(output.Next.Count, Is.EqualTo(0));

            east.Press();
            m_Context.Update();
            Assert.That(output.Next.Count, Is.EqualTo(1));
            Assert.That(output.Next[0], Is.EqualTo(true));
            
            north.Press();
            m_Context.Update();
            Assert.That(output.Next.Count, Is.EqualTo(2));
            Assert.That(output.Next[1], Is.EqualTo(true));
            
        }

        // TODO Verify that initial state is properly recorded so we e.g. may start in actuated state on first sync
        // TODO Local multiplayer, basically a binding filter, but probably good to let sources get assigned to players since physical control makes sense to assign to players. Let devices have a flag.
        // TODO Cover scenarios similar to Value, PassThrough, Button, e.g.
        // TODO Trigger once vs trigger once a frame, vs trigger on received value
        // TODO Phases are related to interactions. Phases associated with data makes no sense, but how do we handle touch?
        // TODO Disambuigitation is better solved on binding level where one can either merge or select?
    }
}
