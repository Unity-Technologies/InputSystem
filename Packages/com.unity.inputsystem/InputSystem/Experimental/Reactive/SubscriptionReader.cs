using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Implement a dependency graph walker
    
    public struct SubscriptionReader<T> : IDisposable 
        where T : struct
    {
        private readonly Context.StreamContext<T> m_StreamContext;

        internal SubscriptionReader([NotNull] Context.StreamContext<T> streamContext)
        {
            m_StreamContext = streamContext;
        }

        internal SubscriptionReader(Context context, Usage usage)
        {
            // TODO We need to handle here somehow whether there is only a single subscription to our data, or react/adapt based on it being a shared evaluation dependency chain.
            //      We should likely have a node representation which all implements comparable so we can construct this dependency tree.
            m_StreamContext = context.GetOrCreateStreamContext<T>(usage);
            
            // TODO We need to monitor changes to stream context stream
            // TODO Keep a reference to currently associated stream
            
            
        }

        // Dependency chain:
        // Press(Gamepad.buttonSouth)
        
        //internal IDependencyGraphNode Node => m_StreamContext;

        public T Read()
        {
            return m_StreamContext.Stream?.GetLast() ?? default; // TODO Can we do better?
        }

        public string Describe()
        {
            return string.Empty;
        }
        
        public void Dispose()
        {
            // TODO Fix
        }
    }
}