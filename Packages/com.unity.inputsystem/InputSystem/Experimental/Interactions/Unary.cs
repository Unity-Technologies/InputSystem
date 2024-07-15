using System;

// TODO Consider Unary to use a cached action instead, however this doesn't fit well with IObservable since we need to keep state.

namespace UnityEngine.InputSystem.Experimental
{
    public interface IUnaryFunc<in TIn, TOut> 
        where TIn : struct 
        where TOut : struct
    {
        bool Process(TIn arg0, ref TOut result);
    }
    
    public struct Unary<TIn, TSource, TOut, TFunc> : IObservableInput<TOut>, IDependencyGraphNode
        where TSource : IObservableInput<TIn>, IDependencyGraphNode 
        where TIn : struct
        where TOut : struct
        where TFunc : IUnaryFunc<TIn, TOut>
    {
        private sealed class Impl : IObserver<TIn> // TODO Why not let this object we subscription? Ref count could sit in state
        {
            private TFunc m_Func; // Note, must be mutable since may be stateful
            private readonly ObserverList2<TOut> m_Observers;
        
            public Impl(Context context, TSource source, TFunc func)
            {
                m_Observers = new ObserverList2<TOut>(source.Subscribe(context, this));
                m_Func = func;
            }

            public IDisposable Subscribe(IObserver<TOut> observer)
            {
                return Subscribe(Context.instance, observer);
            }

            public IDisposable Subscribe(Context context, IObserver<TOut> observer)
            {
                return m_Observers.Subscribe(context, observer);
            }

            public void OnCompleted()
            {
                m_Observers.OnCompleted();   
            }

            public void OnError(Exception error)
            {
                m_Observers.OnError(error);   
            }

            public void OnNext(TIn value)
            {
                TOut temp = default;
                if (m_Func.Process(value, ref temp))
                    m_Observers.OnNext(temp);   
            }
        }

        private readonly TSource m_Source;
        private readonly TFunc m_Func;
        private Impl m_Impl;
        
        internal Unary(string displayName, TSource source, TFunc func)
        {
            this.displayName = displayName;
            m_Source = source;
            m_Impl = null;
            m_Func = func;
        }

        public IDisposable Subscribe(IObserver<TOut> observer) => Subscribe(Context.instance, observer);

        public IDisposable Subscribe(Context context, IObserver<TOut> observer) => 
            (m_Impl ??= new Impl(context, m_Source, m_Func)).Subscribe(context, observer);
        
        public bool Equals(IDependencyGraphNode other) => this.CompareDependencyGraphs(other);
        
        public string displayName { get; }
        public int childCount => 1;
        public IDependencyGraphNode GetChild(int index) => index == 0 ? m_Source : throw new ArgumentOutOfRangeException(nameof(index));
    }

    public struct ReleaseFn : IUnaryFunc<bool, InputEvent>
    {
        private bool m_PreviousValue;
        
        public bool Process(bool arg0, ref InputEvent result)
        {
            if (m_PreviousValue != arg0)
            {
                m_PreviousValue = arg0;
                if (!arg0)
                {
                    result = new InputEvent();
                    return true;    
                }
            }
            return false;
        }
    }

    public static class UnaryExtensions
    {
        private const string kReleaseDisplayName = "Release";
        
        public static Unary<bool, TSource, InputEvent, ReleaseFn> Released<TSource>(this TSource source)
            where TSource : IObservableInput<bool>, IDependencyGraphNode
        {
            return new Unary<bool, TSource, InputEvent, ReleaseFn>(kReleaseDisplayName, source, new ReleaseFn());
        }
    }
}