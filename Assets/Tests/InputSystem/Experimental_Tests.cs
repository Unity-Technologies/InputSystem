using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TreeEditor;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;
using UnityEngine.InputSystem.Utilities;
using Vector2 = UnityEngine.Vector2;

// TODO Do we need a FixedInput type?

namespace Tests.InputSystem
{
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
            new object[]{ Endpoint.FromUsage(Usages.GamepadUsages.buttonEast), Usages.GamepadUsages.buttonEast, Endpoint.kAnySource, SourceType.Device },
            new object[]{ Endpoint.FromDeviceAndUsage(13, Usages.GamepadUsages.buttonSouth), Usages.GamepadUsages.buttonSouth, 13, SourceType.Device }
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
        public void Stream()
        {
            using var s = new Stream<int>(Usages.GamepadUsages.leftStick, 100);

            {
                var values = s.ToArray();
                Assert.That(values.Length, Is.EqualTo(0));
                Assert.That(s.Previous, Is.EqualTo(100));
                
                s.Advance();
            }
            {
                var values = s.ToArray();
                Assert.That(values.Length, Is.EqualTo(0));
            }
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
            //Gamepad.leftStick.Select(v => v.magnitude > 0.5f);
            //Gamepad.leftStick.Sum()
            //Gamepad.leftStick.whenPressed()
        }

        [Test]
        public void Concept()
        {
            var data = new ListObserver<Vector2>();
            using var subscription = Gamepad.leftStick.Subscribe(m_Context, data);

            var s = m_Context.CreateStream(Usages.GamepadUsages.leftStick, Vector2.zero);
            s.OfferByValue(Vector2.zero);
            s.OfferByValue(Vector2.left);
            s.OfferByValue(Vector2.right);
            m_Context.Update();

            // Note: Initial state not reported via regular stream inspection
            Assert.That(data.Next.Count, Is.EqualTo(3));
            Assert.That(data.Next[0], Is.EqualTo(Vector2.zero));
            Assert.That(data.Next[1], Is.EqualTo(Vector2.left));
            Assert.That(data.Next[2], Is.EqualTo(Vector2.right));
        }

        // TODO Verify that initial state is properly recorded so we e.g. may start in actuated state on first sync

        [Test]
        public void UseCase_DirectAccess()
        {
            var s = m_Context.CreateStream(Usages.GamepadUsages.buttonSouth, true);

            var data = new ListObserver<bool>();
            using var subscription = Gamepad.buttonSouth.Subscribe(m_Context, data);
            //Gamepad.buttonSouth.Subscribe((bool x) => Debug.Log(x));
            
            m_Context.Update();

            Assert.That(data.Next.Count, Is.EqualTo(1));
            Assert.That(data.Next[0], Is.EqualTo(true));
            
            s.OfferByValue(true);
            s.OfferByValue(false);
            m_Context.Update();
            
            Assert.That(data.Next.Count, Is.EqualTo(3));
            Assert.That(data.Next[1], Is.EqualTo(true));
            Assert.That(data.Next[2], Is.EqualTo(false));
        }

        [Test]
        public void UseCase_DirectBindingsShouldBeCompileTimeTypeSafe()
        {
            m_Context.GivenStreamWithData(Usages.GamepadUsages.buttonSouth, false, true);

            using var move = new BindableInput<Vector2>(callback : Move);
            move.Bind(Gamepad.leftStick);

            // Act
            using BindableInput<InputEvent> jump = new(callback : Jump);
            jump.Bind(Gamepad.buttonSouth.Pressed());
            jump.Bind(Keyboard.space.Pressed());

            jump.OnNext(new InputEvent());
            m_Context.Update();

            Assert.That(m_JumpCount, Is.EqualTo(1));
        }

        /*[Test]
        public void UseCase_ShouldBeAbleToStartFromPreset()
        {
            m_Context = new Context();
            m_Context.GivenStreamWithData(Usages.Gamepad.buttonSouth, false, true);

            // Act
            using BindableInput<InputEvent> jump = new(callback: Jump, source: Presets.Jump());

            m_Context.Update();
            Assert.That(m_JumpCount, Is.EqualTo(1));
        }*/

        public void Example()
        {
            // React to Gamepad button press event
            using var subscription = Gamepad.buttonSouth.Pressed()
                    .Subscribe(DebugObserver<InputEvent>.Create());


            // TODO filter Gamepad.leftStick.filter(x => x > 0.5f); and return new binding source?
            // TODO PoC a temporal filter in the same way, if passed type is not an interface/lambda
            //      there is an oppertunity to avoid the indirect call.
        }

        [Test]
        public void Press()
        {
            var s = m_Context.CreateStream(Usages.GamepadUsages.buttonEast, false);
            s.OfferByValue(true);
            s.OfferByValue(false);

            // React to Gamepad button press event
            var observer = new ListObserver<InputEvent>();
            using var subscription = Gamepad.buttonEast.Pressed(m_Context)
                    .Subscribe(m_Context, observer);

            m_Context.Update();
            Assert.That(observer.Next.Count, Is.EqualTo(1));
        }

        [Test]
        public void Output_Direct()
        {
            Gamepad.rumbleHaptic.Offer(1.0f);
        }

        [Test]
        public void Output_Indirect()
        {
            var rumble = new BindableOutput<float>(Gamepad.rumbleHaptic);
            rumble.Offer(1.0f);
        }

        [Test]
        public void Mux_Test()
        {
            
        }
        
        [Test]
        public void Multiplex()
        {
            var east = m_Context.CreateDefaultInitializedStream(Gamepad.buttonEast);
            var north = m_Context.CreateDefaultInitializedStream(Gamepad.buttonNorth);
            
            var output = new ListObserver<bool>();
            var mux = new Multiplexer<bool>(Gamepad.buttonEast, Gamepad.buttonNorth);
            using var sub = mux.Subscribe(m_Context, output);

            m_Context.Update();

            Assert.That(output.Next.Count, Is.EqualTo(0));

            east.OfferByValue(true);
            m_Context.Update();

            Assert.That(output.Next.Count, Is.EqualTo(1));
            Assert.That(output.Next[0], Is.EqualTo(true));
            
            north.OfferByValue(false);
            m_Context.Update();
            
            Assert.That(output.Next.Count, Is.EqualTo(2));
            Assert.That(output.Next[1], Is.EqualTo(false));
            
            east.OfferByValue(false);
            north.OfferByValue(false);
            east.OfferByValue(false);
            north.OfferByValue(true);
        }

        // TODO Local multiplayer, basically a binding filter, but probably good to let sources get assigned to players since physical control makes sense to assign to players. Let devices have a flag.
        // TODO Cover scenarios similar to Value, PassThrough, Button, e.g.
        // TODO Trigger once vs trigger once a frame, vs trigger on received value
        // TODO Phases are related to interactions. Phases associated with data makes no sense, but how do we handle touch?
        // TODO Disambuigitation is better solved on binding level where one can either merge or select?
    }
}
