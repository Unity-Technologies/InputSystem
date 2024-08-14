using System;

namespace UnityEngine.InputSystem.Experimental
{
    internal class MagnitudeObservable<T> : IObserver<T>
    {
        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(T value)
        {
            //NumericPolicies.Instance.Magnitude(value);
        }
    }
    
    // https://stackoverflow.com/questions/32664/is-there-a-constraint-that-restricts-my-generic-method-to-numeric-types/4834066#4834066
    public readonly struct Magnitude<TSource, T> : IObservableInputNode<T>, IEquatable<Magnitude<TSource, T>> 
        where T : struct, IComparable<T>, IEquatable<T>
    {
        private readonly TSource m_Source;
        
        public Magnitude([InputPort] TSource source)
        {
            m_Source = source;
        }
        
        public IDisposable Subscribe(IObserver<T> observer)
        {
            throw new NotImplementedException();
        }

        public bool Equals(IDependencyGraphNode other) => other is Magnitude<TSource, T> pressed && Equals(pressed);
        public bool Equals(Magnitude<TSource, T> other) => m_Source.Equals(other.m_Source);

        public string displayName => "Magnitude";   // TODO Could be fetched from attribute or class name
        public int childCount => 1; // TODO Wouldn't be necessary if generated based on attributed TSource
        public IDependencyGraphNode GetChild(int index) // TODO Could be generated if based on attributed TSource
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer) where TObserver : IObserver<T>
        {
            throw new NotImplementedException();
        }
    }

    public static class MagnitudeExtensionMethods
    {
        public static Magnitude<TSource, float> Magnitude<TSource>(this TSource source)
            where TSource : IObservableInputNode<float>, IDependencyGraphNode
        {
            return new Magnitude<TSource, float>(source);
        }
    }
}