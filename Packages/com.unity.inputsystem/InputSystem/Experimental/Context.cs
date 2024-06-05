using System;
using System.Collections.Generic;
using System.Threading;

namespace UnityEngine.InputSystem.Experimental
{
    public partial class Context : IDisposable
    {
        // Basically just conceptual for testing

        /*public Stream<T> GivenStream<T>(Usage key, ref T initialValue) where T : struct
        {
            if (m_Streams.ContainsKey(key))
                throw new Exception("Stream already present");
            m_Streams.Add(Usages.Gamepad.buttonSouth);
        }*/

        private static Context _instance;

        public static Context instance
        {
            get => _instance ??= new Context();
            internal set => _instance = value;
        }

        public Stream<T> CreateStream<T>(Usage key, T initialValue) where T : struct
        {
            if (m_Streams.ContainsKey(key))
                throw new Exception("Stream already registered");

            var stream = new Stream<T>(key, ref initialValue);
            m_Streams.Add(key, stream);

            if (m_StreamContexts.TryGetValue(key, out StreamContext streamContext))
                ((StreamContext<T>)streamContext).SetStream(stream);

            return stream;
        }

        public Stream<T> CreateDefaultInitializedStream<T>(InputBindingSource<T> source) where T : struct
        {
            return CreateStream<T>(source.Usage, default);
        }

        /*
        public void Offer<T>(Usage key, T value) where T : struct
        {
            // TODO Debug assert T is small enough
            Offer(key, ref value);
        }

        public void Offer<T>(Usage key, ref T value) where T : struct
        {
            if (!m_Streams.TryGetValue(key, out IStream stream))
                throw new Exception("Stream do not exist");

            ((Stream<T>)stream).Offer(ref value);
            //var streamContext = m_StreamContexts[key] as StreamContext<T>; // TODO Incorrect, should write to stream
            //streamContext?.Offer(ref value);
        }*/

        // TODO Should not take stream
        /*public void GivenData<T>(Usage key, Stream<T> stream) where T : struct
        {
            if (m_Streams.ContainsKey(key))
                throw new Exception("Stream already present");

            // Register stream
            m_Streams.Add(key, stream);

            // If there is an associated stream context, associate the stream with the context
            if (m_StreamContexts.TryGetValue(key, out StreamContext streamContext))
                ((StreamContext<T>)streamContext).SetStream(stream);
        }*/

        internal StreamContext<T> GetOrCreateStreamContext<T>(Usage key) where T : struct
        {
            // Fetch existing stream context if already exists
            if (m_StreamContexts.TryGetValue(key, out StreamContext context))
            {
                var streamContext = (StreamContext<T>)context;

                // Associate stream context with underlying stream if available
                if (m_Streams.TryGetValue(key, out var stream))
                    streamContext.SetStream(stream as Stream<T>);

                return streamContext;
            }

            // Create a new stream context without any associated underlying stream
            var newContext = new StreamContext<T>(key);
            m_StreamContexts.Add(key, newContext);
            if (m_Streams.TryGetValue(key, out var stream2))
                newContext.SetStream(stream2 as Stream<T>);
            return newContext;
        }

        /*public Stream<T> GetStream<T>(Usage key) where T : struct
        {
            return m_Streams[key].GetStream<T>();
        }*/

        //public delegate void StreamDataReceived(IStream stream);

        // This map is populated when a stream is subscribed to
        private readonly Dictionary<Usage, StreamContext> m_StreamContexts = new();

        // This map is populated based on addressable endpoints
        private readonly Dictionary<Usage, IStream> m_Streams = new();

        //private readonly StreamContext<Button>[] m_ButtonStreams;

        // THis map is populated when a usage is subscribed to
        //private readonly Dictionary<Usage, List<Object>> m_Observers = new();

        public void Update()
        {
            // TODO Eliminate, this should be perspective
            // TODO Replace with filtered call based on incoming. Note that a stream context always has subscriptions.
            //      If not, it doesn't exist.
            foreach (var kvp in m_StreamContexts)
            {
                kvp.Value.Process(); // TODO Requires indirection, consider supporting built-in type explicitly?
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
        }
    }
}
