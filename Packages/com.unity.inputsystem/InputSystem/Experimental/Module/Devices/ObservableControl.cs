using System;
using UnityEngine.InputSystem.Experimental.Internal;

namespace UnityEngine.InputSystem.Experimental.Devices
{
    // TODO
    // We want this node to be possible to inject decoder prior to indirect callbacks, an interval map is likely a good choice.
    // For now, just handle it here
    public class ObservableControlNode<T> : ObserverBase<T> 
        where T : unmanaged
    {
        
    }
    
    [Serializable]
    public struct ObservableControl<T> : IObservableInputNode<T>, IUnsafeObservable<T> 
        where T : struct
    {
        [SerializeField] private Endpoint endpoint;
        [SerializeField] private Field field;
        
        public ObservableControl(Endpoint endpoint, Field field)
        {
            if (endpoint == Endpoint.Invalid)
                throw new ArgumentException(nameof(endpoint));
            // TODO If endpoint defines a fixed type, assert that field is a valid field for that type and for field type.
            
            this.endpoint = endpoint;
            this.field = field;
        }

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer) 
            where TObserver : IObserver<T>
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            // TODO Need to take endpoint context.GetOrCreateStreamContext<T>();
            // TODO We should pass field into stream context to allow pre-filtering based on field mask
            
            // TODO Implement node sharing (multi-cast)
            /*var impl = ObjectPool<ForwardNode>.shared.Rent();
            impl.Initialize(
                source.Subscribe(context, impl)
            );
            impl.AddObserver(observer);
            return new Subscription<bool>(impl, observer);*/
            
            // TODO Check if field is set, if not subscribe to source stream.
            // TODO If field is set, subscribe to source stream with a decoding function such that forwarding only happens if changed.
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IObserver<T> observer) => Subscribe(Context.instance, observer);
        
        public bool Equals(IDependencyGraphNode other) => other is ObservableControl<T> node && Equals(node);
        public bool Equals(ObservableControl<T> other) => endpoint.Equals(other.endpoint) && field.Equals(other.field);
        public string displayName => nameof(ObservableControl<T>);
        public int childCount => 0;
        public IDependencyGraphNode GetChild(int index) => throw new ArgumentOutOfRangeException(nameof(index));
        public UnsafeSubscription Subscribe(Context context, UnsafeDelegate<T> observer)
        {
            throw new NotImplementedException();
        }
    }
}