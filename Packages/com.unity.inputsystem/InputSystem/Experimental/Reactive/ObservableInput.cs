using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Consider if this is an anti-pattern due to the facts that it creates a binding with an
    // operation on it that prevents seeing observables as a single type if though implementing interfacees.
    // Instead consider an object defining inputs and outputs which decouples this and removes the restriction
    // of single input/output. It basically needs to implement filter


    // TODO This needs to be hidden somehow, otherwise we are forced to use an interface and
    //      proxies become less useful since they require heap memory allocation.
    //      It might be difficult to avoid this unless we derive a custom ObservableBindingSource.

    // TODO Multiple IObservable<T> inputs may be solved by internal instances or via custom interface providing source tracking

    // TODO If we cannot obtain the flexibility we need with this struct consider making it a class and store with device
    // TODO This makes sense when usage uniquely identifies a stream.
    //      This makes less sense for a derived binding source unless cached.
    //      However, cached behavior is enforced if done via DOTS so maybe
    //      we should compute into memory by default and avoid complexity?
    //      Should likely compare the two variants in a benchmark.
    //      Stream memory could easily be allocated from a native memory pool.


    public interface IMagnitude
    {
        public float Magnitude();
    }

    // TODO The benefit of IObserver<T> is that we can inspect the type that is subscribing to us, basically we can iterate bottom-up and build the graph. Explore this and see if it can help us.
    /*public struct ObservableVector2Input : IMagnitude 
    {
        public float Magnitude() =>
    }*/

    /*public struct ObservableNumberInput<T> 
        where T : struct, IComparable<T>, struct
    {
        
    }*/
    
    // TODO This is the proxy for stream context, hence we should follow the same pattern?!
    // TODO Should really have key and not usage.... Not necessarily, if this represents a blue print. But makes more sense to let this be specific and let deviceId = 0 represents the set.
    // TODO A binding source associated with a specific usage but not with a specific context. Not clear anymore since it might be required to trace whether this is a source that may be shared in a graph.  
    // TODO See if we can make this readonly again (This isn't really a node, but a node proxy)
    // Note that ObservableInput is equatable based on underlying usage.
    [Serializable]
    public struct ObservableInput<T> : IObservableInputNode<T>, IEquatable<ObservableInput<T>>, IUnsafeObservable<T>
        where T : struct
    {
        [SerializeField] private Endpoint m_Endpoint;
        [SerializeField] private Field m_Field;
        
        public ObservableInput(Endpoint endpoint, string displayName = null, Field field = default)
        {
            if (endpoint == Endpoint.Invalid)
                throw new ArgumentException(nameof(this.endpoint) + " is not a valid usage.");
                
            this.m_Endpoint = endpoint;
            this.m_Field = field;
            this.displayName = displayName ?? throw new ArgumentNullException(nameof(displayName) + " is required"); // TODO Consider getting rid of it. Replace with symbol backed by optional assembly?!.
        }

        public readonly Endpoint endpoint => m_Endpoint;
        public readonly Field field => m_Field;

        public readonly IDisposable Subscribe<TObserver>(Context context, TObserver observer) 
            where TObserver : IObserver<T>
        {
            // Subscribe to source by subscribing to a stream context. Note that the stream context 
            // is consistent throughout the life-time of any existing associated subscriptions but
            // may change dynamically based on the availability of underlying streams (devices and/or controls).
            
            // One option is to here create an intermediary node that simply does any of the following:
            // 1) Checks bit(s) indicated by field and generate a boolean (generalized) - can avoid indirect call if source stream has history
            // 2) Explicitly calls GetKey(usage) on KeyboardState (specific) - cannot avoid indirect call
            return context.GetOrCreateStreamContext<T>(m_Endpoint).Subscribe(observer);
        }

        #region IObservable<T>
        
        public IDisposable Subscribe([NotNull] IObserver<T> observer)
        {
            return Subscribe(Context.instance, observer);
        }
        
        #endregion

        public SubscriptionReader<T> Subscribe()
        {
            return Subscribe(Context.instance);
        }
        
        public readonly SubscriptionReader<T> Subscribe(Context context)
        {
            //m_NodeId = context.RegisterNode();
            return new SubscriptionReader<T>(context.GetOrCreateStreamContext<T>(m_Endpoint)); // Subscription reader need to 
        }
        
        #region IUnsafeObservable
        
        public UnsafeSubscription Subscribe([NotNull] Context context, UnsafeDelegate<T> observer)
        {
            return context.GetOrCreateStreamContext<T>(m_Endpoint).Subscribe(context, observer);
        }
        
        #endregion
        
        #region IDependencyGraphNode
        
        /// <inheritDoc />
        public string displayName { get; }
        
        /// <inheritDoc />
        public int childCount => 0;
        /// <inheritDoc />
        public IDependencyGraphNode GetChild(int index) => throw new ArgumentOutOfRangeException(nameof(index));
        
        #endregion

        #region IEquatable<T>
        
        /// <inheritDoc />
        public bool Equals(IDependencyGraphNode other) => other is ObservableInput<T> otherNode && Equals(otherNode);

        /// <inheritDoc />
        public bool Equals(ObservableInput<T> other) => m_Endpoint.Equals(other.m_Endpoint);   

        /// <inheritDoc />
        public override bool Equals(object obj) => obj is ObservableInput<T> other && Equals(other);
        /// <inheritDoc />
        public override int GetHashCode() => m_Endpoint.GetHashCode();

        #endregion
    }
}
