using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
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
        
        private readonly Dictionary<Usage, StreamContext> m_StreamContexts; // Tracks observed usages/streams.
        private readonly Dictionary<Usage, IStream> m_Streams;              // Tracks available streams (typed StreamContexts).
        private readonly EventStream m_Events;                              // Shared system queue of observable events.
        private int m_NodeId;                                               // Context specific node ID counter.
        
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

        private struct RegisteredNode
        {
            public object node;
            public int refCount;
        }
        
        private readonly Dictionary<IDependencyGraphNode, RegisteredNode> m_Nodes;

        internal int NodeCount => m_Nodes.Count;

        // TODO We probably do not gain anything by keeping this centralized, consider distributing it for type safety
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
            // a handle (non reference type) when necessary.
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
        }
        
        public static Context GetContext(int handle)
        {
            if (handle <= 0 || handle > kMaxContexts)
                throw new ArgumentException($"Invalid context handle: {handle}");
            return _globals.Contexts[handle - 1];
        }

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
            foreach (var kvp in m_StreamContexts)
                kvp.Value.Process(); // TODO Requires indirection, consider supporting built-in type explicitly?
            foreach (var kvp in m_StreamContexts)
                kvp.Value.Advance(); // TODO Requires indirection, consider supporting built-in type explicitly?
        }

        public void Dispose()
        {
            foreach (var kvp in m_StreamContexts)
                kvp.Value.Dispose();
            foreach (var kvp in m_Streams)
                kvp.Value.Dispose();
            
            // Release global slot
            _globals.Contexts[m_Handle - 1] = null;
        }
    }
}
