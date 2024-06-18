using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.InputSystem.Experimental
{
    interface IOp<T>
        where T : struct
    {
        public IDisposable[] SubscribeToDependencies(Context context);
        public bool Process(T value);
    }
    
    /*internal struct UnaryOp<T, TSource> : IOp<T> 
        where T : struct
        where TSource : IObservableInput<T>
    {
        private readonly TSource m_First;
        
        public IDisposable[] SubscribeToDependencies(Context context)
        {
            return new [] { m_First.Subscribe(context, this) };
        }
        
        public IDisposable Subscribe(IObserver<T> observer)
        {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(Context context, IObserver<T> observer)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public bool Process(T value)
        {
            throw new NotImplementedException();
        }
    }*/

    public abstract class Operation<TIn, TOut> : IObservableInput<TOut>, IDependencyGraphNode , IObserver<TIn>
        where TIn : struct
        where TOut : struct
        //where TSource : IObservableInput<TIn>, IDependencyGraphNode
    {
        private ObserverList2<TOut> m_ObserverList;   // Downstream dependencies
        //private TSource m_Source;
        
        protected Operation([NotNull] string displayName)
        {
            this.displayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        }
        
        #region IObservableInput<T>
        
        public IDisposable Subscribe([NotNull] IObserver<TOut> observer)
        {
            return Subscribe(Context.instance, observer);
        }

        public IDisposable Subscribe([NotNull] Context context, [NotNull] IObserver<TOut> observer)
        {
            if (m_ObserverList == null)
                m_ObserverList = new ObserverList2<TOut>(CreateSubscription(context)); // TODO Impl.CreateSubscription()
                //m_ObserverList = new ObserverList2<TOut>(m_Source.Subscribe(context, this));
            return m_ObserverList.Subscribe(context, observer);
        }
        
        #endregion

        public bool Equals(IDependencyGraphNode other)
        {
            if (ReferenceEquals(null, other))
                return false;
            return false; // TODO Compare dependency chains
        }

        public int nodeId => 0; // TODO Remove?!
        public string displayName { get; } // TODO Impl.displayName
        public abstract int childCount { get; } // TODO Impl.childCount
        public abstract IDependencyGraphNode GetChild(int index); // TODO Impl.childCount

        public void OnCompleted() => m_ObserverList.OnCompleted();
        public void OnError(Exception error) => m_ObserverList.OnError(error);

        protected abstract IDisposable[] CreateSubscription(Context context);
        protected abstract bool Process(IObserver<TOut> observer, TIn value);
        public void OnNext(TIn value)
        {
            if (Process(m_ObserverList, value))
                m_ObserverList.OnNext(new TOut());
        }
    }

    public abstract class UnaryOperation<TIn, TOut, TSource> : Operation<TIn, TOut> 
        where TIn : struct 
        where TOut : struct
        where TSource : IDependencyGraphNode, IObservableInput<TIn>
    {
        private readonly TSource m_Source;
        
        protected UnaryOperation(string displayName, TSource source) 
            : base(displayName)
        {
            m_Source = source;
        }

        public override int childCount => 1;
        public override IDependencyGraphNode GetChild(int index)
        {
            if (index == 0) return m_Source;
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        protected override IDisposable[] CreateSubscription(Context context)
        {
            return new [] { m_Source.Subscribe(context, this) };
        }
    }
}