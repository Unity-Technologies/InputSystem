using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// An opaque base class for scriptable input bindings required to support the serialization engine.
    /// </summary>
    public abstract class ScriptableInputBinding : ScriptableObject, IObservableInput
    {
        /// <summary>
        /// Returns the associated binding type.
        /// </summary>
        /// <returns></returns>
        public abstract Type GetBindingType();
    }

    /// <summary>
    /// Represents an abstract scriptable input binding that is also an observable input.
    /// </summary>
    /// <typeparam name="T">The observable value type.</typeparam>
    public abstract class ScriptableInputBinding<T> : ScriptableInputBinding, IObservableInput<T>, IObservable<T> 
        where T : struct
    {
        public override Type GetBindingType() => typeof(T);
        public IDisposable Subscribe(IObserver<T> observer) => Subscribe(Context.instance, observer);
        public abstract IDisposable Subscribe<TObserver>(Context context, TObserver observer) where TObserver : IObserver<T>;
    }
    
    // https://discussions.unity.com/t/serialized-interface-fields/871555/13
    
    // TODO Remove
    public abstract class ScriptableInputWrapperBinding<T> : ScriptableInputBinding, IObservableInput<T>, IObservable<T> 
        where T : struct
    {
        private IObservableInputNode<T> node; // TODO We cannot use an interface, so we need to use TObservableInput instead to make it concrete. This implies we need to build a concrete chain.
        
        public override Type GetBindingType() => typeof(T);
        public IDisposable Subscribe(IObserver<T> observer) => Subscribe(Context.instance, observer);
        public IDisposable Subscribe<TObserver>(Context context, TObserver observer) 
            where TObserver : IObserver<T>
        {
            return node.Subscribe(context, observer);
        }
    }
}