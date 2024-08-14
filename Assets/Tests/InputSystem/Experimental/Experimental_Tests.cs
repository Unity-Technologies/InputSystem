using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using Tests.InputSystem.Experimental;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UIElements;
using InputEvent = UnityEngine.InputSystem.Experimental.InputEvent;
using Usages = UnityEngine.InputSystem.Experimental.Devices.Usages;
using Vector2 = UnityEngine.Vector2;
using Is = UnityEngine.TestTools.Constraints.Is;

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
        public void CachedNode()
        {
            using var s0 = Gamepad.LeftStick.Subscribe(action: (Vector2 _) => { }, context: m_Context);
            using var s1 = Gamepad.RightStick.Subscribe(action: (Vector2 _) => { }, context: m_Context);
            //Assert.Equals(m_Context.RegisteredNodeCount, Is.EqualTo(1));
        }

        // TODO Each operation should have equality and inequality and has tests
        [Test]
        public void NodeEquality()
        {
            Assert.That(Gamepad.LeftStick.Equals(null), Is.False);
            Assert.That(Gamepad.LeftStick.Equals(Gamepad.RightStick), Is.False);
            
            Assert.That(Gamepad.LeftStick.Equals(Gamepad.LeftStick), Is.True);
            Assert.That(Gamepad.RightStick.Equals(Gamepad.RightStick), Is.True);
            
            Assert.That(Gamepad.ButtonEast.Pressed().Equals(null), Is.False);
            Assert.That(Gamepad.ButtonEast.Pressed().Equals(Gamepad.ButtonEast.Pressed()), Is.True);
            Assert.That(Gamepad.ButtonEast.Pressed().Equals(Gamepad.ButtonSouth.Pressed()), Is.False);
        }

        private enum MessageType
        {
            Invalid,
            DeviceArrival,
            DeviceRemoval,
            
            Keyboard,
            Gamepad
        }

        [StructLayout(LayoutKind.Explicit, Size = 16, Pack = 1)]
        private unsafe struct Message
        {
            [FieldOffset(0)] public MessageType type;
            [FieldOffset(4)] public ulong endpoint;

            public static Message CreateDeviceArrivalMessage()
            {
                return new Message() { type = MessageType.DeviceArrival, endpoint = 0 };
            }
        }

        private unsafe struct UnsafeDelegatePool
        {
            public delegate*<void*, void>* Rent(int n)
            {
                return null;
            }
        }

        // Shared pointer is 2 pointers in size which is why it cannot be atomic
        // Hence, for thread safe implementation we need to lock. What if we fetch add pointer, then 
        
        [Test]
        public void MessageBufferConcept()
        {
            // What we want is to register delegates to be invoked for a certain
            
            var map = new NativeHashMap<ulong, UnsafeEventHandler>(100, AllocatorManager.Persistent);
            using var messageQueue = new UnsafeRingQueue<Message>(100, AllocatorManager.Temp);
            messageQueue.Enqueue(new Message{ type = MessageType.Gamepad, endpoint = 12 });

            unsafe
            {
                while (messageQueue.TryDequeue(out var message))
                {
                    if (map.TryGetValue(message.endpoint, out var handlers))
                    {
                        handlers.Invoke();
                    }
                }    
            }
        }

        public interface IObservableThing<T>
        {
            public void Subscribe<TObserver>(TObserver observer) where TObserver : IObserver<T>;
        }

        public struct Thing : IObservableThing<int>
        {
            public void Subscribe<TObserver>(TObserver observer) where TObserver : IObserver<int>
            {
                
            }
        }

        public struct Curious : IObserver<int>
        {
            public void OnCompleted()
            {
                throw new NotImplementedException();
            }

            public void OnError(Exception error)
            {
                throw new NotImplementedException();
            }

            public void OnNext(int value)
            {
                throw new NotImplementedException();
            }
        }
        
        [Test]
        public void WillItWork()
        {
            Assert.That(() =>
            {
                Curious y;     
                Thing x;       
                x.Subscribe(y);
            }, Is.Not.AllocatingGCMemory());
            
            // TODO Transform a trigger into a button
        }

        // TODO Verify initial state, e.g. is button already actuated, release triggers release event
        // TODO Verify that initial state is properly recorded so we e.g. may start in actuated state on first sync
        // TODO Local multiplayer, basically a binding filter, but probably good to let sources get assigned to players since physical control makes sense to assign to players. Let devices have a flag.
        // TODO Cover scenarios similar to Value, PassThrough, Button, e.g.
        // TODO Trigger once vs trigger once a frame, vs trigger on received value
        // TODO Phases are related to interactions. Phases associated with data makes no sense, but how do we handle touch?
        // TODO Disambuigitation is better solved on binding level where one can either merge or select?
        // TODO Verify that shortcut and press interactions where they overlap mutes press interaction
        
        // TODO Verify that we may extract underlying ObservableInput instances in a dependency graph to support rebinding them.
    }
}
