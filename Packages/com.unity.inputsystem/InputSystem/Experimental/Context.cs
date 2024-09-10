using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Shouldly;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering.VirtualTexturing;

namespace UnityEngine.InputSystem.Experimental
{
    internal struct InternalEvent
    {
        
    }

    internal struct EventStream
    {
        
    }
    
    public partial class Context : IDisposable
    {
        public const int InvalidNodeId = 0;

        private const int kMaxContexts = 4;      // Maximum number of concurrent contexts
        
        private struct Globals
        {
            public Context Instance;             // Singleton instance for simplified API usage
            public Context[] Contexts;           // Global instances
            public int ContextCounter;           // Global counter for unique IDs
        }

        private static Globals _globals = new Globals()
        {
            Instance = null,
            Contexts = new Context[kMaxContexts],
            ContextCounter = 0
        };
        
        private readonly int m_Handle;
        
        private AllocatorManager.AllocatorHandle m_Allocator;
        private readonly Dictionary<Usage, StreamContext> m_StreamContexts; // Tracks observed usages/streams.
        private readonly Dictionary<Usage, IStream> m_Streams;              // Tracks available streams (typed StreamContexts).
        private readonly EventStream m_Events;                              // Shared system queue of observable events.
        private int m_NodeId;                                               // Context specific node ID counter.
        private readonly TimerManager m_TimerManager;

        private unsafe UnsafeContext* m_UnsafeContext;
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct UnsafeContext
        {
            private AllocatorManager.AllocatorHandle m_Allocator;

            public UnsafeContext(AllocatorManager.AllocatorHandle allocator)
            {
                m_Allocator = allocator;
            }

            public AllocatorManager.AllocatorHandle allocator => m_Allocator;
            
            public unsafe void* Allocate(int sizeOf, int alignOf, int items = 1)
            {
                return m_Allocator.Allocate(sizeOf, alignOf, items);
            }

            public unsafe T* Allocate<T>() where T : unmanaged
            {
                return (T*)Allocate(sizeof(T), UnsafeUtility.AlignOf<T>());
            }
            
            public unsafe void Free(void* ptr, int sizeOf, int alignOf, int items)
            {
                AllocatorManager.Free(m_Allocator, ptr, sizeOf, alignOf, items);
            }

            public unsafe void Free<T>(T* ptr) where T : unmanaged
            {
                Free(ptr, sizeof(T), UnsafeUtility.AlignOf<T>(), 1);
            }
        }

        internal unsafe ref UnsafeContext unsafeContext => ref *m_UnsafeContext;
        
        /*private readonly List<Type> m_RegisteredNodeTypes;
        private readonly List<RegisteredNode> m_RegisteredNodes;
        private int m_RegisteredNodeCount;

        internal void RegisterNode<T>(T node)
        {
            var type = typeof(T);
            var n = m_RegisteredNodes.Count;
            var index = IndexOfNodeObject(node);
            if (index == -1)
                throw new Exception($"Node {node} has already been registered.");
            
            m_RegisteredNodes.Add(new RegisteredNode{ node = node, refCount = 1 });
        }

        internal void UnregisterNode<T>(T node)
        {
            var index = IndexOfNodeObject(node);
            if (index == -1)
                throw new Exception($"Node {node} has not been previously registered.");

            m_RegisteredNodes[index].refCount;
            m_RegisteredNodes.RemoveAt(index);
        }

        internal int IndexOfNodeObject(object node)
        {
            var n = m_RegisteredNodes.Count;
            for (var i = 0; i < n; ++i)
            {
                if (ReferenceEquals(m_RegisteredNodes[i], node))
                    return i;
            }
            return -1;
        }*/

        // TODO Consider exposing only allocator or only allocation functions

        private struct RegisteredNode
        {
            public object node;
            public int refCount;
        }
        
        private readonly Dictionary<IDependencyGraphNode, RegisteredNode> m_Nodes;

        internal int NodeCount => m_Nodes.Count;

        // TODO If we really want cached nodes we need to rethink this since we must record sources. A lot would be solved if nodes are classes but this drives GC.
        // TODO We probably do not gain anything by keeping this centralized, consider distributing it for type safety
        // TODO This is an anti-pattern at the moment where source will be boxed
        internal T GetNodeImpl<T>(IDependencyGraphNode source)
            where T : class
        {
            return (m_Nodes.TryGetValue(source, out RegisteredNode reg) && reg.node is T impl) ? impl : default;
        }

        internal void RegisterNodeImpl(IDependencyGraphNode node, object impl)
        {
            m_Nodes.Add(node, new RegisteredNode{node = node, refCount = 1});
        }

        internal void RemoveNodeImpl(IDependencyGraphNode node)
        {
            m_Nodes.Remove(node);
        }

        public static Context instance
        {
            get => _globals.Instance ??= new Context();
            internal set => _globals.Instance = value;
        }
        
        public Context()
        {
            m_Nodes = new(32);
                
            // Attempt to assign this context to a global slot. This allows referencing the context with
            // a handle (non reference type) when necessary via lookup.
            var handle = 0;
            for (var i = 0; i < kMaxContexts; ++i)
            {
                if (_globals.Contexts[i] == null)
                {
                    _globals.Contexts[i] = this;
                    handle = i + 1;
                    break;
                }
            }
            if (handle == 0)
                throw new Exception($"Maximum number of concurrently existing {nameof(Context)} instances reached ({kMaxContexts}. Did you forget to dispose previous instances?");
            
            m_StreamContexts = new();
            m_Streams = new();
            m_Events = new EventStream();
            m_Handle = handle;
            m_TimerManager = new TimerManager();
            
            m_Deferred = new List<DeferredOnNext>();
            m_Deferred2 = new List<DeferredOnNext2>();
            unsafe
            {
                m_DeferredBuffer = null;
            }
            m_DeferredBufferCapacity = 0;
            m_DeferredBufferLength = 0;
            
            var allocator = AllocatorManager.Persistent;
            unsafe
            {
                m_UnsafeContext = (UnsafeContext*)allocator.Allocate(sizeof(UnsafeContext), UnsafeUtility.AlignOf<UnsafeContext>());
                *m_UnsafeContext = new UnsafeContext(allocator);
            }
        }
        
        public static Context GetContext(int handle)
        {
            if (handle <= 0 || handle > kMaxContexts)
                throw new ArgumentException($"Invalid context handle: {handle}");
            return _globals.Contexts[handle - 1];
        }

        internal TimerManager timerManager => m_TimerManager;

        public int RegisterNode()
        {
            var nextNodeId = ++m_NodeId;
            return nextNodeId;
        }
        
        public Stream<T> CreateStream<T>(Usage key, T initialValue) where T : struct
        {
            if (m_Streams.ContainsKey(key))
                throw new Exception("Stream already exist");

            var stream = new Stream<T>(key, ref initialValue);
            m_Streams.Add(key, stream);

            if (m_StreamContexts.TryGetValue(key, out StreamContext streamContext))
                ((StreamContext<T>)streamContext).SetStream(stream);

            return stream;
        }

        /*internal StreamContext<T> GetStreamContext<T>(Usage key) where T : struct
        {
            if (!m_StreamContexts.TryGetValue(key, out StreamContext context))
                return null;
            return context;
        }*/

        internal Stream<T> GetStream<T>(Usage key) where T : struct
        {
            if (m_Streams.TryGetValue(key, out var stream))
                return (Stream<T>)stream;
            return null;
        }
        
        internal bool TryGetStreamContext<T>(Usage key, out StreamContext<T> streamContext) where T : struct
        {
            if (m_StreamContexts.TryGetValue(key, out StreamContext context))
            {
                streamContext = (StreamContext<T>)context;
                return true;
            }
            streamContext = null;
            return false;
        }

        internal StreamContext<T> GetOrCreateStreamContext<T>(Usage key) where T : struct
        {
            // Attempt to fetch existing stream context
            if (!m_StreamContexts.TryGetValue(key, out StreamContext context))
            {
                // Construct a new stream context if no context exists.
                context = new StreamContext<T>(key);
                m_StreamContexts.Add(key, context);
            }

            // Attempt to associate stream context with underlying stream if available.
            // If no underlying stream exist we need to reattempt later when availability changes.
            var streamContext = (StreamContext<T>)context;
            if (m_Streams.TryGetValue(key, out var stream))
                streamContext.SetStream(stream as Stream<T>);

            return streamContext;
        }

        public void Update()
        {
            // TODO Eliminate, this should be perspective
            // TODO Replace with filtered call based on incoming. Note that a stream context always has subscriptions.
            //      If not, it doesn't exist.
            // TODO Make this properly order from inter-stream perspective
            foreach (var kvp in m_StreamContexts)
                kvp.Value.Process(); // TODO Requires indirection, consider supporting built-in type explicitly?
            foreach (var kvp in m_StreamContexts)
                kvp.Value.Advance(); // TODO Requires indirection, consider supporting built-in type explicitly?
        }

        public void Dispose()
        {
            // Dispose stream contexts and streams
            foreach (var kvp in m_StreamContexts)
                kvp.Value.Dispose();
            foreach (var kvp in m_Streams)
                kvp.Value.Dispose();
            
            // Release global slot
            _globals.Contexts[m_Handle - 1] = null;

            // TODO Check and warn for any unsubscribed items
            
            // Deallocate associated unsafe context
            unsafe
            {
                // Deallocate and resset data associated with deferred events
                if (m_Deferred2 != null)
                {
                    UnsafeUtility.Free(m_DeferredBuffer, Allocator.Persistent);
                    m_DeferredBuffer = null;
                    m_DeferredBufferCapacity = 0;
                    m_DeferredBufferLength = 0;
                }
                
                // Deallocate the unsafe context
                var allocator = m_UnsafeContext->allocator;
                AllocatorManager.Free(allocator, m_UnsafeContext, sizeof(UnsafeContext), UnsafeUtility.AlignOf<UnsafeContext>());
                m_UnsafeContext = null;
            }
        }

        public ReadOnlySpan<T> GetDevices<T>()
        {
            throw new NotImplementedException();
        }

        internal object RentNode(Type type)
        {
            return null; // TODO Implement pooling
        }

        public T RentNode<T>() => (T)RentNode(typeof(T));
        
        #region Scheduler

        // Currently "event consumption" is solved by deferring events when a node has an associated Event Group.
        // Deferring events implies that instead of propagating forwarded calls to OnNext down the chain, we
        // store tentative events in a buffer that we reconsider after we are done executing a certain underlying
        // event ID that triggered the events. This is to avoid firing multiple events when there is an associated
        // Event Group and events have been differentiated by non equal Priorities. 
        //
        // This might require another approach in case the dependency graph would be executed in an asynchronous
        // fashion. Then this would potentially have been to inject events back into the main event buffer directly after
        // the source event ID or at the end, and sort that buffer before initiating callbacks by processing the
        // buffer a second time without evaluation. 
        
        internal interface IDeferred
        {
            internal unsafe void DeferredForwardOnNext(void* data);
        }
        
        private struct DeferredOnNext
        {
            public Action OnNext;
            public int Priority;
        }

        private struct DeferredOnNext2
        {
            public IDeferred Deferred;
            public int Offset;
            public int Priority;
        }

        private readonly List<DeferredOnNext> m_Deferred;
        private readonly List<DeferredOnNext2> m_Deferred2;
        
        private const int kDeferredBufferAlignment = 16;
        private unsafe void* m_DeferredBuffer;
        private int m_DeferredBufferLength;
        private int m_DeferredBufferCapacity;
        
        public void Defer(Action deferred, int priority)
        {
            if (m_Deferred.Count > 0 && m_Deferred[0].Priority < priority)
                m_Deferred.Clear(); // No need to keep events that would never be fired anyway
            m_Deferred.Add(new DeferredOnNext() { OnNext = deferred, Priority = priority});
        }

        private unsafe void Defer(IDeferred deferredEventContext, int priority, void* value, int sizeOf, int alignment)
        {
            if (m_Deferred2.Count > 0)
            {
                // Ignore deferral request if the event has lower priority than an already deferred event.
                if (m_Deferred2[0].Priority > priority)
                    return;
                
                // If the request has higher priority than the already deferred events we may drop the current ones.
                if (m_Deferred2[0].Priority < priority)
                {
                    m_Deferred2.Clear();
                    m_DeferredBufferLength = 0;
                }
            }
                
            // Compute offset (and reallocate if necessary) for the deferred event buffer to store associated data
            var offset = CollectionHelper.Align(m_DeferredBufferLength, alignment);
            if (offset > m_DeferredBufferCapacity)
                ReallocateDeferredBuffer(offset + sizeOf);

            // Push (append) data to the deferred buffer
            var ptr = UnsafeUtils.Offset(m_DeferredBuffer, offset);
            UnsafeUtility.MemCpy(ptr, value, sizeOf);
            m_DeferredBufferLength = offset + sizeOf;
            
            // Push node to the list of deferred event nodes
            m_Deferred2.Add(new DeferredOnNext2(){ Deferred = deferredEventContext, Offset = offset, Priority = priority});
        }

        private unsafe void ReallocateDeferredBuffer(int minimumSizeBytes)
        {
            var newCapacity = Math.Max(m_DeferredBufferCapacity * 2, minimumSizeBytes);
            var buffer = UnsafeUtility.Malloc(newCapacity, kDeferredBufferAlignment, Allocator.Persistent);
            if (m_DeferredBuffer != null)
            {
                UnsafeUtility.MemCpy(buffer, m_DeferredBuffer, m_DeferredBufferLength);
                UnsafeUtility.Free(m_DeferredBuffer, Allocator.Persistent);
            }
            m_DeferredBufferCapacity = newCapacity;
        }

        internal unsafe void Defer<T>(IDeferred deferredEventContext, int priority, T value) where T : unmanaged
        {
            Defer(deferredEventContext, priority, &value, sizeof(T), UnsafeUtility.AlignOf<T>());
        }

        public void InvokeDeferred()
        {
            var n = m_Deferred.Count;
            if (n == 0)
                return;

            var priority = m_Deferred[0].Priority;
            m_Deferred[0].OnNext.Invoke();
            for (var i=1; i < n && priority == m_Deferred[i].Priority; ++i)
                m_Deferred[i].OnNext.Invoke();
            m_Deferred.Clear();
        }

        
        internal unsafe void InvokeDeferred2()
        {
            if (m_Deferred2.Count == 0)
                return;
            
            for (var i = 0; i < m_Deferred2.Count; ++i)
            {
                var dataPtr = UnsafeUtils.Offset(m_DeferredBuffer, m_Deferred2[i].Offset);
                m_Deferred2[i].Deferred.DeferredForwardOnNext(dataPtr);
            }
            
            m_Deferred2.Clear();
            m_DeferredBufferLength = 0;
        }
        
        #endregion
    }
}
