using System;

namespace UnityEngine.InputSystem.Experimental
{
    public delegate TResult ConvertFunc<in T, out TResult>(T arg);
    
    internal class ConvertNode<TSource, TIn, TOut> : IObservableInput<TOut>, IObserver<TIn>
        where TSource : IObservableInput<TIn>
    {
        private readonly ConvertFunc<TIn, TOut> m_Converter;
        private readonly ObserverList2<TOut> m_Observers;

        public ConvertNode(Context context, TSource source, ConvertFunc<TIn, TOut> converter)
        {
            m_Observers = new ObserverList2<TOut>(source.Subscribe(context, this));
            m_Converter = converter;
        }
        
        public IDisposable Subscribe(IObserver<TOut> observer) => Subscribe(Context.instance, observer);

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer) 
            where TObserver : IObserver<TOut>
        {
            return m_Observers.Subscribe(context, observer);
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(TIn value)
        {
            var next = m_Converter.Invoke(value);
            m_Observers.OnNext(next);
        }
    }

    public struct Convert<TSource, TIn, TOut> : IObservableInputNode<TOut>, IUnsafeObservable<bool> 
        where TSource : IObservableInput<TIn>, IDependencyGraphNode
        where TOut : struct
    {
        private TSource m_Source;
        private ConvertFunc<TIn, TOut> m_Converter;

        public Convert(TSource source, ConvertFunc<TIn, TOut> converter)
        {
            m_Source = source;
            m_Converter = converter;
        }

        public IDisposable Subscribe(IObserver<TOut> observer) =>
            Subscribe(Context.instance, observer);

        public bool Equals(IDependencyGraphNode other) => other is Convert<TSource, TIn, TOut> node && Equals(node);
        public bool Equals(Convert<TSource, TIn, TOut> other) => m_Source.Equals(other.m_Source);

        public string displayName => "Convert";
        public int childCount => 1;
        public IDependencyGraphNode GetChild(int index)
        {
            switch (index)
            {
                case 0: return m_Source;
                default: throw new Exception();
            }
        }

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer) where TObserver : IObserver<TOut>
        {
            return new ConvertNode<TSource, TIn, TOut>(context, m_Source, m_Converter).Subscribe(observer);
        }

        public UnsafeSubscription Subscribe(Context context, UnsafeDelegate<bool> observer)
        {
            throw new NotImplementedException();
        }
    }

    public static class ConvertExtensions
    {
        public static Convert<TSource, TIn, TOut> Convert<TSource, TIn, TOut>(this TSource source, ConvertFunc<TIn, TOut> converter) 
            where TOut : struct 
            where TSource : IObservableInput<TIn>, IDependencyGraphNode
        {
            return new Convert<TSource, TIn, TOut>(source, converter);
        }
    }
}