using System;
using System.Collections.Generic;
using System.Threading;
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
        
        private readonly Dictionary<Usage, StreamContext> m_StreamContexts = new(); // Tracks observed usages
        private readonly Dictionary<Usage, IStream> m_Streams = new();              // Tracks available streams
        private readonly EventStream m_Events = new();                              // MPMC queue of observable events
        private int m_NodeId;
        
        public static Context instance
        {
            get => _globals.Instance ??= new Context();
            internal set => _globals.Instance = value;
        }
        
        public Context()
        {
            // Attempt to assign this context to a global slot
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
            {
                throw new Exception(
                    $"Maximum number of concurrently existing {nameof(Context)} instances reached ({kMaxContexts}. Did you forget to dispose previous instances?");
            }
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

        public Stream<T> CreateDefaultInitializedStream<T>(ObservableInput<T> source) where T : struct
        {
            return CreateStream<T>(source.Usage, default);
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
            
            return (StreamContext<T>)context;
        }

        public void Update()
        {
            // TODO Eliminate, this should be perspective
            // TODO Replace with filtered call based on incoming. Note that a stream context always has subscriptions.
            //      If not, it doesn't exist.
            foreach (var kvp in m_StreamContexts)
            {
                kvp.Value.Process(); // TODO Requires indirection, consider supporting built-in type explicitly?
            }
            foreach (var kvp in m_StreamContexts)
            {
                kvp.Value.Advance(); // TODO Requires indirection, consider supporting built-in type explicitly?
            }
        }

        public void Dispose()
        {
            foreach (var kvp in m_StreamContexts)
            {
                kvp.Value.Dispose();
            }

            foreach (var kvp in m_Streams)
            {
                kvp.Value.Dispose();
            }
            
            // Release global slot
            _globals.Contexts[m_Handle - 1] = null;
        }
    }
}
