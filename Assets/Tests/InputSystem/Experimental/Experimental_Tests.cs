using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tests.InputSystem.Experimental;
using Unity.Collections;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;
using UnityEngine.InputSystem.Utilities;
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
            DeviceRemoval
        }

        private struct Message
        {
            public MessageType type;
            public Endpoint endpoint;
        }

        private struct EventQueue
        {
            private UniformBuffer<Message> m_Messages;

            public void Enqueue(ref Message msg)
            {
                
            }
        }

        /*public struct GamepadWriter
        {
            public 
            
            public void Publish()
            {
                
            }
        }*/
        
        /// <summary>
        /// A naively implemented fixed size pool with first-fit search and free-list defragmentation.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        public struct FixedPool<T>
        {
            private struct Chunk
            {
                public int Offset;
                public int Length;
            }
            
            private T[] m_Array;
            private Chunk[] m_FreeList;
            private int m_Count;
            
            public FixedPool(int size)
            {
                m_Array = new T[size];
                m_FreeList = new Chunk[size];
                m_FreeList[0] = new Chunk { Offset = 0, Length = size };
                m_Count = 1;
            }

            public ArraySegment<T> Rent(int count)
            {
                return RentFirstFit(count);
                //return RentOrderedBestFit(count);
            }

            public void Return(ArraySegment<T> segment)
            {
                ReturnFirstFit(segment);
            }

            private ArraySegment<T> RentFirstFit(int count)
            {
                for (var i = 0; i < m_Count; ++i)
                {
                    if (m_FreeList[i].Length >= count)
                    {
                        var offset = m_FreeList[i].Offset;
                        m_FreeList[i].Offset += count;
                        m_FreeList[i].Length -= count;
                        return new ArraySegment<T>(m_Array, offset, count);
                    }
                }

                throw new Exception();
            }

            public void ReturnFirstFit(ArraySegment<T> segment)
            {
                if (segment.Array != m_Array)
                    throw new ArgumentException($"{nameof(segment)} is not part of this pool");

                var upperBound = segment.Offset + segment.Count;
                for (var i = 0; i < m_Count; ++i)
                {
                    // Continue scanning for insertion point
                    if (m_FreeList[i].Offset <= segment.Offset) 
                        continue;

                    // Merge left
                    var merged = false;
                    var prevIndex = i - 1;
                    if (prevIndex >= 0)
                    {
                        ref var prev = ref m_FreeList[prevIndex];
                        if (prev.Offset + prev.Length == segment.Offset)
                        {
                            prev.Length += segment.Count;
                            merged = true;
                        }
                    }

                    // Merge right
                    ref var next = ref m_FreeList[i];
                    if (segment.Offset + segment.Count == next.Offset)
                    {
                        if (merged)
                        {
                            m_FreeList[prevIndex].Length += next.Length;
                        }
                        else
                        {
                            next.Length += segment.Count;
                            next.Offset = segment.Offset;    
                        }
                        return;
                    }
                    
                    // No merge so insert instead
                    if (merged) 
                        return;
                    
                    Array.Copy(m_FreeList, i, m_FreeList, i + 1, m_Count - i);
                    m_FreeList[i] = new Chunk() { Offset = segment.Offset, Length = segment.Count };
                    ++m_Count;
                    return;
                }

                // Insert at end
                m_FreeList[m_Count++] = new Chunk() { Offset = segment.Offset, Length = segment.Count };
            }

            public ArraySegment<T> RentOrderedBestFit(int count)
            {
                // Search free-list for best-fit element
                int index = -1;
                for (var i = 0; i < m_Count; ++i)
                {
                    if (m_FreeList[i].Length >= count)
                    {
                        index = i;
                        continue;
                    }
                    break;
                }

                if (index < 0)
                    throw new Exception();                
                
                ref var item = ref m_FreeList[index];
                var offset = item.Offset;
                if (item.Length > count)
                {
                    item.Offset += count;
                    item.Length -= count;
                }
                else
                {
                    Array.Copy(m_FreeList, index + 1, m_FreeList, index, m_Count - index - 1);
                    --m_Count;
                }

                return new ArraySegment<T>(m_Array, offset, count);
            }

            public void ReturnBestFit(ArraySegment<T> segment)
            {
                if (segment.Array != m_Array)
                    throw new ArgumentException($"{nameof(segment)} is not part of this pool");

                // Reinsert based on size constraint
                for (var i = 0; i < m_Count; ++i)
                {
                    if (m_FreeList[i].Length <= segment.Count)
                    {
                        Array.Copy(m_FreeList, i, m_FreeList, i + 1, m_Count - i);
                        m_FreeList[i] = new Chunk() { Offset = segment.Offset, Length = segment.Count };
                        ++m_Count;
                        return;
                    }
                }

                m_FreeList[m_Count++] = new Chunk() { Offset = segment.Offset, Length = segment.Count };
            }
        }
        
        [Test]
        public void PoolTest()
        {
            var pool = new FixedPool<IDisposable>(10);
            
            var a0 = pool.Rent(3);
            Assert.That(a0, Is.Not.Null);
            Assert.That(a0.Count, Is.EqualTo(3));
            
            var a1 = pool.Rent(1);
            Assert.That(a1, Is.Not.Null);
            Assert.That(a1.Count, Is.EqualTo(1));
            
            var a2 = pool.Rent(2);
            Assert.That(a2, Is.Not.Null);
            Assert.That(a2.Count, Is.EqualTo(2));
            
            var a3 = pool.Rent(3);
            Assert.That(a3, Is.Not.Null);
            Assert.That(a3.Count, Is.EqualTo(3));
            
            Assert.Throws<Exception>(() => pool.Rent(2));
            
            pool.Return(a0);
            pool.Return(a1); // Except a0 and a1 to be merged (left)

            // Note: This wouldn't be possible without defragmentation
            var a4 = pool.Rent(4);
            Assert.That(a4, Is.Not.Null);
            Assert.That(a4.Count, Is.EqualTo(4));
            
            pool.Return(a3);

            pool.Return(a4);
            pool.Return(a2);

            var a5 = pool.Rent(10);
            Assert.That(a5, Is.Not.Null);
        }
        
        [Test]
        public void MessageBufferConcept()
        {
            using var q = new UniformBuffer<Message>();

            var msg = new Message
            {
                type = MessageType.Invalid
            };
            
            q.Push(ref msg);
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
