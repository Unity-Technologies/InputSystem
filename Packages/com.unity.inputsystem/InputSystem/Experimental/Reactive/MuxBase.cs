using System;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO LIttle value of this base?!
    internal class MuxBase<T, TSource, TReceiver> : ObserverList2<T>, IForwardReceiver<T>
        where TSource : IObservableInput<T>
        where TReceiver : IForwardReceiver<T> 
        where T : struct
    {
        private readonly ObserverList2<T> m_Observers;
        private readonly ForwardingObserver<T, TReceiver> m_FirstObserver;
        private readonly ForwardingObserver<T, TReceiver> m_SecondObserver;

        private int m_Completed;
        
        public MuxBase(Context context, TReceiver receiver, TSource source0, TSource source1)
            : base(source0.Subscribe(context, new ForwardingObserver<T, TReceiver>(receiver)),
                source1.Subscribe(context, new ForwardingObserver<T, TReceiver>(receiver)))
        { }

        public void ForwardOnCompleted()
        {
            if (++m_Completed == 2)
                OnCompleted();
        }

        public void ForwardOnError(Exception error)
        {
            OnError(error);
        }

        public void ForwardOnNext(T value)
        {
            OnNext(value);
        }
    }
    
    internal abstract class IndexedMuxBase<T, TSource> : ObserverList2<T>, IIndexedForwardReceiver<T>
        where TSource : IObservableInput<T>
        where T : struct
    {
        private readonly ObserverList2<T> m_Observers;
        private readonly IndexedForwardingObserver<T, IndexedMuxBase<T, TSource>> m_FirstObserver;
        private readonly IndexedForwardingObserver<T, IndexedMuxBase<T, TSource>> m_SecondObserver;

        private int m_Completed;

        public IndexedMuxBase(Context context, TSource source0, TSource source1)
        {
            Initialize(
                source0.Subscribe(context, new IndexedForwardingObserver<T, IndexedMuxBase<T, TSource>>(0, this)),
                source1.Subscribe(context, new IndexedForwardingObserver<T, IndexedMuxBase<T, TSource>>(1, this)));
        }

        public void ForwardOnCompleted(int index)
        {
            if (++m_Completed == 2)
                OnCompleted();
        }

        public void ForwardOnError(int index, Exception error)
        {
            OnError(error);
        }

        public void ForwardOnNext(int index, T value)
        {
            OnNext(value);
        }
    }
}