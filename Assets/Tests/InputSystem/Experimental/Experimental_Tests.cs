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
        
        // https://stackoverflow.com/questions/3522361/add-delegate-to-event-thread-safety
        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers
        // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code
        private struct UnsafeMulticastDelegate : IDisposable
        {
            private AllocatorManager.AllocatorHandle m_Allocator;
            private IntPtr m_Delegates;

            public UnsafeMulticastDelegate(AllocatorManager.AllocatorHandle allocator)
            {
                m_Delegates = IntPtr.Zero;
                m_Allocator = allocator;
            }

            public unsafe void Dispose()
            {
                if (m_Delegates == IntPtr.Zero) 
                    return;
                
                AllocatorManager.Free(m_Allocator, m_Delegates.ToPointer());
                m_Delegates = IntPtr.Zero;
            }
/*
            private static unsafe delegate*<void*, void>* Allocate(int capacity, AllocatorManager.AllocatorHandle allocator)
            {
                return (delegate*<void*, void>*)allocator.Allocate(sizeof(delegate*<void*, void>), 8, capacity);
            }
*/

            /*private static IntPtr Create(ref AllocatorManager.AllocatorHandle allocator)
            {
                var ptr = Allocate(2, allocator);
                *((int*)ptr) = 1;
                ptr[1] = callback;
                temp = (IntPtr)ptr;
            }*/

            /*private unsafe struct Delegate
            {
                private delegate*<void*, void>* m_Ptr;

                public static Delegate Combine(ref Delegate existing, delegate*<void*, void> callback, AllocatorManager.AllocatorHandle allocator)
                {
                    var sizeOf = sizeof(delegate*<void*, void>);

                    delegate*<void*, void>* ptr;
                    if (existing.m_Ptr == null)
                    {
                        ptr = (delegate*<void*, void>*)allocator.Allocate(sizeOf, sizeOf, 2);
                        var p = (uint*)ptr;
                        p[0]
                        ptr[1] = callback;
                    }
                    else
                    {
                        var oldCount = (int)existing.m_Ptr[0];
                        var newSize = oldCount + 1;
                        ptr = (delegate*<void*, void>*)allocator.Allocate(sizeOf, sizeOf, newSize + 1);
                        var p = (ushort*)ptr;
                        p[0] = newSize;
                        UnsafeUtility.MemCpy(ptr + 1, existing.m_Ptr + 2, sizeOf * oldCount);
                        ptr[newSize] = callback;
                    }
                    
                    return new Delegate() { m_Ptr = ptr };
                }    
            }*/

            private unsafe IntPtr Combine(IntPtr existing, delegate*<void*, void> callback)
            {
                var sizeOf = sizeof(delegate*<void*, void>);
                delegate*<void*, void>* ptr;
                
                if (existing == IntPtr.Zero)
                {
                    ptr = (delegate*<void*, void>*)m_Allocator.Allocate(sizeOf, 8, 2);
                    ptr[1] = callback;

                    void* raw = callback;
                    
                    var p = (ushort*)ptr;
                    p[0] = 1;
                    
                    return (IntPtr)ptr;
                }
                else
                {
                    var oldPtr = (delegate*<void*, void>*)existing;
                    var oldSize = *(ushort*)existing!;
                    var newSize = oldSize + 1;
                    
                    ptr = (delegate*<void*, void>*)m_Allocator.Allocate(sizeOf, 8, newSize + 1);
                    UnsafeUtility.MemCpy(ptr + 1, oldPtr + 2, sizeOf * oldSize);
                    ptr[newSize] = callback;
                    
                    var p = (ushort*)ptr;
                    p[0] = (ushort)newSize;
                    
                    return (IntPtr)ptr;    
                }
            }
            
            public unsafe void Add(delegate*<void*, void> callback)
            {;
                IntPtr handler = m_Delegates;
                for(;;)
                {
                    var handler2 = handler;
                    var temp = Combine(handler, callback);
                    handler = Interlocked.CompareExchange(ref m_Delegates, temp, handler2);
                    if (handler == handler2)
                    {
                        AllocatorManager.Free(m_Allocator, (void*)handler);
                        break;
                    }
                    AllocatorManager.Free(m_Allocator, (void*)temp);
                        
                     // TODO Only deallocate if size increased, otherwise we may reuse existing buffer
                }
            }

            public unsafe void Remove(delegate*<void*, void>* callback)
            {
                // TODO Implement    
            }

            public unsafe void Invoke(void* arg)
            {
                void* raw = (void*)m_Delegates;
                var handlers = (delegate*<void*, void>*)m_Delegates;
                if (handlers == null)
                    return;

                // TODO If we encode handlers into the unused bits of the pointer itself up to max point we can avoid
                //      the null check since n will be zero for null pointer if converted to number
                int n = *(ushort*)handlers;
                for (var i=0; i < n; ++i)
                {
                    handlers[i+1](arg);
                }
            }
        }

        private static int m_Counter;
        
        static unsafe void AddFn(void* p)
        {
            ++m_Counter;
        }

        [Test]
        public void MulticastDelegate_NoDelegate()
        {
            unsafe
            {
                using var x = new UnsafeMulticastDelegate(AllocatorManager.Persistent);
                x.Invoke(null);
            }
        }
        
        [Test]
        public void MulticastDelegate_SingleDelegate()
        {
            
            unsafe
            {
                Assert.That(sizeof(delegate*<void*, void>), Is.EqualTo(8));
                Assert.That(sizeof(delegate*<void*, int, int, int, void>), Is.EqualTo(8));
                
                using var x = new UnsafeMulticastDelegate(AllocatorManager.Persistent);
                //x.Dummy();
                //Assert.That(() => { x.Dummy(); }, NUnit.Framework.Is.Not.AllocatingGCMemory());

                x.Add(&AddFn);
                //x.Remove(&AddFn);
                
                //Assert.That(() => { x.Add(&AddFn); }, Is.Not.AllocatingGCMemory());
                
                //x.Add(&Add);
                x.Invoke(null);
                
                Assert.That(m_Counter, Is.EqualTo(1));
                
                
            }
        }
        
        [Test]
        public void MessageBufferConcept()
        {
            var demux = new Dictionary<ulong, MulticastDelegate>();
            using var messageQueue = new UnsafeRingQueue<Message>(100, AllocatorManager.Temp);
            messageQueue.Enqueue(new Message(){ });

            while (messageQueue.TryDequeue(out Message message))
            {
                switch (message.type)
                {
                    case MessageType.DeviceArrival:
                        break;
                    case MessageType.DeviceRemoval:
                        break;
                    case MessageType.Keyboard:
                        break;
                    case MessageType.Gamepad:
                        if (demux.TryGetValue(message.endpoint, out MulticastDelegate handler))
                        {
                            
                        }
                        break;
                    default:
                        // TODO Handle custom messages
                        break;
                }
            }
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
